using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 시점 회전. 좌우는 몸체를, 상하는 카메라를 돌린다.
    /// 몸체를 돌려야 <see cref="PlayerMotor"/> 의 이동 방향이 따라온다.
    /// </summary>
    /// <remarks>
    /// 반동과 조준 흔들림도 여기서 얹는다. 카메라 회전을 만지는 곳이 둘이 되면
    /// 매 프레임 서로 덮어쓴다. 무기는 <see cref="AddRecoil"/> 로 값만 넘긴다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("상하 회전을 적용할 카메라. 보통 자식의 Camera 다.")]
        [SerializeField] private Transform cameraPivot;

        [Tooltip("입력 1 당 회전 각도(도). 마우스는 픽셀 단위 델타라 값이 작다. "
            + "플레이어가 환경 설정에서 고른 배율이 여기 곱해진다.")]
        [SerializeField] private float sensitivity = 0.08f;

        [Tooltip("올려다보고 내려다볼 수 있는 한계(도).")]
        [SerializeField] private float pitchLimit = 85f;

        [Header("조준 흔들림")]
        [Tooltip("가만히 서 있을 때 흔들리는 폭(도). 정비공이라 총구가 멎지 않는다.")]
        [SerializeField] private float idleSwayDegrees = 0.25f;

        [Tooltip("걸을 때 흔들리는 폭(도).")]
        [SerializeField] private float moveSwayDegrees = 0.8f;

        [Tooltip("흔들리는 빠르기(초당 주기).")]
        [SerializeField] private float swayFrequency = 1.6f;

        [Header("반동")]
        [Tooltip("반동이 사라지는 빠르기. 클수록 빨리 돌아온다.")]
        [SerializeField] private float recoilRecovery = 8f;

        [Header("놀람")]
        [Tooltip("적에게 발각됐을 때 화면이 흔들리는 폭(도).")]
        [SerializeField] private float shakeDegrees = 1.6f;

        [Tooltip("흔들림이 잦아드는 시간(초).")]
        [SerializeField] private float shakeSeconds = 0.7f;

        [Tooltip("떠는 빠르기(초당 주기). 조준 흔들림보다 훨씬 빠르다.")]
        [SerializeField] private float shakeFrequency = 22f;

        private float pitch;

        private float shakeUntil = float.NegativeInfinity;
        private float shakeStart;
        private float activeShakeDegrees;
        private float activeShakeSeconds;

        /// <summary>x 는 좌우, y 는 위로 튀는 양(도). 0 으로 감쇠한다.</summary>
        private Vector2 recoil;

        private float swayTime;
        private float swayAmount;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (input == null || cameraPivot == null)
            {
                Debug.LogError($"{nameof(PlayerLook)}: {nameof(input)} 또는 {nameof(cameraPivot)} 이 비어 있다.", this);
                enabled = false;
                return;
            }

            pitch = cameraPivot.localEulerAngles.x;
        }

        private void OnEnable()
        {
            LockCursor();
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>총이 한 발 나갔을 때 부른다. 위로 튀고 좌우로 조금 틀어진다.</summary>
        /// <param name="upDegrees">위로 튀는 각도.</param>
        /// <param name="sideDegrees">좌우로 틀어지는 각도. 부호는 호출한 쪽이 정한다.</param>
        /// <remarks>
        /// ponytail: 반동이 전부 제자리로 돌아온다. 조준선이 영구히 밀리지 않으므로
        /// 되돌려 당기는 조작이 없다. 그 감각이 필요해지면 복구량을 100% 미만으로 낮춘다.
        /// </remarks>
        public void AddRecoil(float upDegrees, float sideDegrees)
        {
            recoil.y += upDegrees;
            recoil.x += sideDegrees;
        }

        /// <summary>
        /// 화면을 짧게 떨게 한다. 적이 발각을 알릴 때 부른다.
        /// </summary>
        /// <remarks>
        /// 인자가 없는 것은 <b>인스펙터의 <c>UnityEvent</c> 에 그대로 걸기 위해서</b>다.
        /// 세기와 길이는 여기 값으로 두고 부르는 쪽은 「놀랐다」만 알린다.
        /// </remarks>
        public void Shake()
        {
            Shake(shakeDegrees, shakeSeconds);
        }

        /// <summary>
        /// 세기와 길이를 정해서 떨게 한다. 총을 쏠 때처럼 <b>같은 화면에 다른 크기의
        /// 흔들림</b>이 필요한 곳에서 부른다.
        /// </summary>
        /// <remarks>
        /// 이미 떨고 있으면 <b>더 센 쪽이 이긴다.</b> 발사 중에 적이 발각을 알렸다고
        /// 총 반동으로 덮어써 버리면 「놀랐다」가 사라진다. 반대로 매번 덧쌓으면
        /// 연사할 때 화면이 무한정 커진다.
        /// </remarks>
        public void Shake(float degrees, float seconds)
        {
            if (seconds <= 0f || degrees <= 0f)
            {
                return;
            }

            float remaining = shakeUntil - Time.time;
            if (remaining > 0f && activeShakeDegrees > degrees)
            {
                return;
            }

            activeShakeDegrees = degrees;
            activeShakeSeconds = seconds;
            shakeStart = Time.time;
            shakeUntil = Time.time + seconds;
        }

        private void Update()
        {
            // ponytail: 브라우저는 사용자 제스처 없이 포인터 잠금을 허용하지 않는다.
            // WebGL 에서는 첫 클릭으로 잠기고, ESC 로 풀린 뒤에도 클릭으로 되돌아온다.
            // 타이틀 화면이 생기면 거기서 한 번 잠그는 편이 낫다.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (input.AttackPressed)
                {
                    LockCursor();
                }

                return;
            }

            // 여기 있는 값은 기준이고, 플레이어가 고른 배율을 매 프레임 곱한다.
            // 설정 화면에서 슬라이더를 끌면 다음 프레임부터 바로 반영된다.
            Vector2 delta = input.Look * sensitivity * GameSettings.LookSensitivity;

            transform.Rotate(0f, delta.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);

            recoil = Vector2.Lerp(recoil, Vector2.zero, 1f - Mathf.Exp(-recoilRecovery * Time.deltaTime));

            Vector2 sway = EvaluateSway();
            Vector2 shake = EvaluateShake();

            // 흔들림과 반동은 pitch 에 누적하지 않는다. 누적하면 한계각이 조용히 밀린다.
            cameraPivot.localRotation = Quaternion.Euler(
                pitch - recoil.y + sway.y + shake.y,
                recoil.x + sway.x + shake.x,
                0f);
        }

        /// <summary>놀람 떨림. 시작할 때 가장 세고 끝에서 0 이 된다.</summary>
        private Vector2 EvaluateShake()
        {
            if (Time.time >= shakeUntil || activeShakeSeconds <= 0f)
            {
                return Vector2.zero;
            }

            float left = 1f - (Time.time - shakeStart) / activeShakeSeconds;
            float amount = activeShakeDegrees * left * left;
            float t = Time.time * shakeFrequency;

            // 조준 흔들림과 주기를 어긋나게 둬서 두 흔들림이 겹쳐 보이지 않게 한다.
            return new Vector2(
                Mathf.Sin(t) * amount,
                Mathf.Sin(t * 1.37f + 1.1f) * amount * 0.7f);
        }

        private Vector2 EvaluateSway()
        {
            bool moving = input.Move.sqrMagnitude > 0.01f;
            float target = moving ? moveSwayDegrees : idleSwayDegrees;

            // 서다 걷다 할 때 폭이 튀지 않게 부드럽게 옮긴다.
            swayAmount = Mathf.MoveTowards(swayAmount, target, Mathf.Abs(moveSwayDegrees - idleSwayDegrees) * 2f * Time.deltaTime);
            swayTime += Time.deltaTime * swayFrequency * (moving ? 2f : 1f) * Mathf.PI * 2f;

            // 가로세로 주기를 2:1 로 두면 8 자를 그려서 같은 자리를 반복하지 않는다.
            return new Vector2(
                Mathf.Sin(swayTime) * swayAmount,
                Mathf.Sin(swayTime * 2f) * swayAmount * 0.5f);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
