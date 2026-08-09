using UnityEngine;
using UnityEngine.AI;

namespace Treeline
{
    /// <summary>
    /// 삭도가 떠난 뒤, 그것이 <b>케이블에 매달려 따라온다.</b> 그리고 놓친다.
    /// </summary>
    /// <remarks>
    /// 기획의 「그것이 케이블을 붙잡고 따라 올라온다」이며, 클립 셋
    /// (<c>Grip_Cable</c> 42f → <c>Cable_Climb</c> loop → <c>Cable_Fall</c> 54f)은
    /// 이미 만들어져 있다. <b>여기가 그 클립들을 부르는 「엔딩 연출」이다.</b>
    /// <para>
    /// IMPORTANT: <see cref="Colossus"/> 에 상태를 더하지 않는다. 그쪽은 추격 ·
    /// 공격 · 밀림 · 묻힘만 알고, 케이블은 모른다. 연출용 분기를 적의 상태 기계에
    /// 넣으면 6장 전체가 그것을 알아야 한다 — 애니메이션 문서의 코드 계약이다.
    /// </para>
    /// <para>
    /// <b>발이 미끄러지지 않는다.</b> 클립 한 번의 당김이 몸을 0.85 m 보내고 잡는
    /// 구간이 1.12 초이므로 클립이 담은 속도는 <see cref="clipMetersPerSecond"/>
    /// 0.76 m/s 다. 세상에서 나아가는 속도를 이 값 × 재생 배속으로 정하면, 배속을
    /// 아무리 올려도 잡은 손이 케이블 위에서 흐르지 않는다. 속도와 배속을 따로
    /// 두면 반드시 어긋난다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CableChase : MonoBehaviour
    {
        private static readonly int GripHash = Animator.StringToHash("GripCable");
        private static readonly int FallHash = Animator.StringToHash("CableFall");

        [Header("무엇이 무엇을 타나")]
        [Tooltip("케이블에 매달릴 것.")]
        [SerializeField] private Colossus watcher;

        [Tooltip("케이블. 로컬 +Z 로 뻗은 상자이며 여기서 선과 길이를 읽는다.")]
        [SerializeField] private Transform cable;

        [Header("붙잡기")]
        [Tooltip("케이블 위쪽 끝에서 몇 m 지점을 잡나. 철탑 대들보를 피해 잡는다.")]
        [SerializeField] private float gripOffset = 3.2f;

        [Tooltip("클립이 상정한 케이블 높이. 손이 케이블에 닿기 전까지 이 값으로 세운다.")]
        [SerializeField] private float hangDrop = 5.25f;

        [Tooltip("붙잡는 클립의 길이(42f).")]
        [SerializeField] private float gripSeconds = 1.4f;

        [Header("따라오기")]
        [Tooltip("클립이 담은 속도(m/s). 만든 값이며 바꿀 일이 없다.")]
        [SerializeField] private float clipMetersPerSecond = 0.76f;

        [Tooltip("매달려 오는 동안의 재생 배속. 세상에서의 속도가 여기에 따라간다.")]
        [SerializeField] private float climbSpeed = 4f;

        [Header("놓치기")]
        [Tooltip("놓치는 클립에서 두 손이 다 떨어지기까지(20f). 그동안은 아직 케이블에 걸려 있다.")]
        [SerializeField] private float releaseSeconds = 0.67f;

        [Tooltip("놓치는 클립의 길이(54f). 이 뒤부터는 이쪽이 계속 떨어뜨린다.")]
        [SerializeField] private float fallClipSeconds = 1.8f;

        [Tooltip("클립이 끝난 뒤의 낙하 가속.")]
        [SerializeField] private float gravity = 18f;

        [Tooltip("이 높이 아래로 내려가면 치운다.")]
        [SerializeField] private float killY = -45f;

        private Animator animator;
        private Transform body;
        private Transform digitsLeft;
        private Transform digitsRight;

        private Vector3 cableTop;
        private Vector3 cableDirection;
        private float cableLength;

        private Vector3 standAt;
        private float distance;
        private float gripAt = float.PositiveInfinity;
        private float releasedAt = float.PositiveInfinity;
        private float fallSpeed;
        private bool running;

        /// <summary>
        /// 붙잡는다. 여기서부터 <see cref="Colossus"/> 는 손을 뗀다.
        /// </summary>
        /// <remarks>
        /// <see cref="Colossus.StandDown"/> 이 경로와 속도를 지우고 컴포넌트를 끄며,
        /// 그 다음 <see cref="NavMeshAgent"/> 를 끈다. 켜 둔 채로 두면 매달린 몸을
        /// 매 프레임 네비메시 위로 끌어내린다.
        /// </remarks>
        public void Grip()
        {
            if (running || watcher == null || cable == null)
            {
                return;
            }

            MeasureCable();

            body = watcher.transform;
            animator = watcher.GetComponent<Animator>();

            digitsLeft = FindBone("DEF_Digits.L");
            digitsRight = FindBone("DEF_Digits.R");

            watcher.StandDown();

            NavMeshAgent agent = watcher.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            standAt = body.position;
            distance = Mathf.Clamp(gripOffset, 0f, cableLength);
            gripAt = Time.time;
            running = true;

            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetTrigger(GripHash);
            }
        }

        /// <summary>놓친다. 삭도가 충분히 멀어졌을 때 연출이 부른다.</summary>
        public void LetGo()
        {
            if (!running || releasedAt < float.PositiveInfinity)
            {
                return;
            }

            releasedAt = Time.time;

            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetTrigger(FallHash);
            }
        }

        /// <summary>
        /// 애니메이터가 자세를 만든 뒤에 몸을 케이블에 얹는다.
        /// </summary>
        /// <remarks>
        /// 순서가 중요하다. <c>Update</c> 에서 옮기면 그 프레임의 애니메이션이
        /// 덮어써서 손이 케이블에서 한 프레임씩 벗어난다.
        /// </remarks>
        private void LateUpdate()
        {
            if (!running)
            {
                return;
            }

            float now = Time.time;
            bool released = now >= releasedAt;
            float sinceRelease = released ? now - releasedAt : 0f;

            if (released && sinceRelease > fallClipSeconds)
            {
                // 클립은 아직 떨어지는 중에 끝난다. 그 뒤를 이어서 떨어뜨린다.
                fallSpeed += gravity * Time.deltaTime;
                body.position += Vector3.down * (fallSpeed * Time.deltaTime);

                if (body.position.y < killY)
                {
                    body.gameObject.SetActive(false);
                    running = false;
                }

                return;
            }

            if (!released)
            {
                Advance(now);
            }

            Vector3 hold = PointOnCable(distance);
            body.position = HangPosition(hold, now);
            body.rotation = Quaternion.LookRotation(Flat(cableDirection), Vector3.up);

            LockHands(hold, LockWeight(now, released, sinceRelease));
        }

        /// <summary>붙잡은 뒤로는 케이블을 따라 내려온다. 속도는 클립이 정한다.</summary>
        private void Advance(float now)
        {
            float climbing = now - gripAt - gripSeconds;
            if (climbing <= 0f)
            {
                return;
            }

            if (animator != null && !Mathf.Approximately(animator.speed, climbSpeed))
            {
                animator.speed = climbSpeed;
            }

            distance = Mathf.Min(cableLength,
                distance + clipMetersPerSecond * climbSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 서 있던 자리에서 매달린 자리로 뛰어오른다.
        /// </summary>
        /// <remarks>
        /// 클립은 8f 에 팔을 올리기 시작해 34f 에 체중이 전부 팔에 실린다. 그 사이에
        /// 몸을 올려 주면 <b>갑판에서 케이블로 뛰어 붙는 것</b>으로 읽힌다. 처음부터
        /// 매달린 자리에 두면 1 m 남짓 공중에 뜬 채로 팔을 드는 그림이 된다.
        /// </remarks>
        private Vector3 HangPosition(Vector3 hold, float now)
        {
            Vector3 hang = hold - Vector3.up * hangDrop;

            float leap = Mathf.InverseLerp(gripSeconds * 0.19f, gripSeconds * 0.81f, now - gripAt);
            return Vector3.Lerp(standAt, hang, Mathf.SmoothStep(0f, 1f, leap));
        }

        /// <summary>
        /// 잡은 손을 케이블 선상에 붙인다. 이 프로젝트에 IK 는 이것뿐이다.
        /// </summary>
        /// <remarks>
        /// <c>Colossus</c> 는 Generic 리그라 <c>Animator</c> 의 IK 목표를 못 쓴다
        /// (그쪽은 Humanoid 전용이다). 손을 끌어오는 대신 <b>몸을 민다</b> — 두 손의
        /// 가운데가 케이블에 오도록 루트를 옮기므로, 팔을 비틀지 않고도 잡은 손이
        /// 케이블에서 벗어나지 않는다. 클립이 만든 몸통의 흔들림은 그대로 남아
        /// <b>손은 고정되고 몸이 흔들리는</b> 그림이 된다 — 매달린 것의 결이다.
        /// </remarks>
        private void LockHands(Vector3 hold, float weight)
        {
            if (weight <= 0f || digitsLeft == null || digitsRight == null)
            {
                return;
            }

            Vector3 hands = (digitsLeft.position + digitsRight.position) * 0.5f;
            body.position += (hold - hands) * weight;
        }

        /// <summary>
        /// 손이 케이블에 걸려 있는 정도. 26f 에 걸리고 놓치는 20f 에 풀린다.
        /// </summary>
        private float LockWeight(float now, bool released, float sinceRelease)
        {
            if (released)
            {
                return 1f - Mathf.Clamp01(sinceRelease / Mathf.Max(0.01f, releaseSeconds));
            }

            return Mathf.Clamp01(
                Mathf.InverseLerp(gripSeconds * 0.62f, gripSeconds, now - gripAt));
        }

        /// <summary>케이블 상자에서 선을 읽는다. 위쪽 끝이 철탑이다.</summary>
        private void MeasureCable()
        {
            cableLength = cable.lossyScale.z;
            cableDirection = cable.forward;

            cableTop = cable.position - cableDirection * (cableLength * 0.5f);

            // 어느 쪽이 위인지는 씬이 정한다. 상자를 반대로 놓아도 같게 돌아간다.
            if (cableTop.y < cable.position.y + cableDirection.y * (cableLength * 0.5f))
            {
                cableDirection = -cableDirection;
                cableTop = cable.position - cableDirection * (cableLength * 0.5f);
            }
        }

        private Vector3 PointOnCable(float along)
        {
            return cableTop + cableDirection * Mathf.Clamp(along, 0f, cableLength);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private Transform FindBone(string boneName)
        {
            foreach (Transform candidate in watcher.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == boneName)
                {
                    return candidate;
                }
            }

            Debug.LogWarning($"[{nameof(CableChase)}] 뼈 '{boneName}' 을 못 찾았다. 손이 케이블에 안 붙는다.", this);
            return null;
        }
    }
}
