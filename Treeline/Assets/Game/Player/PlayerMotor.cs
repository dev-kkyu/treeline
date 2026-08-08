using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 1인칭 이동. 걷기 · 달리기 · 점프 · 웅크리기. 시점 회전은 <see cref="PlayerLook"/> 가
    /// 몸체를 돌리므로 여기서는 그 방향을 그대로 따라간다.
    /// </summary>
    /// <remarks>
    /// 달리기는 기획의 도망 구간(2 · 6장) 때문에 있다. 웅크리기는 「사물함 뒤에 붙는다 ·
    /// 숨는다」 때문이다. 점프는 기획에 근거가 없고 조작 편의로 넣었다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("웅크릴 때 카메라도 같이 내린다. PlayerLook 이 쓰는 것과 같은 Transform 이다.")]
        [SerializeField] private Transform cameraPivot;

        [Header("속도")]
        [Tooltip("걷는 속도 (m/s). 정비공이라 빠르지 않다.")]
        [SerializeField] private float walkSpeed = 2.8f;

        [Tooltip("달리는 속도 (m/s). 도망칠 때만 쓴다.")]
        [SerializeField] private float sprintSpeed = 4.6f;

        [Tooltip("웅크린 채 걷는 속도 (m/s).")]
        [SerializeField] private float crouchSpeed = 1.4f;

        [Tooltip("스태미너가 바닥난 뒤 회복이 시작되기 전까지의 속도 (m/s). 숨이 차 있다.")]
        [SerializeField] private float exhaustedSpeed = 1.8f;

        [Header("중력과 점프")]
        [Tooltip("중력 가속도. 음수다.")]
        [SerializeField] private float gravity = -20f;

        [Tooltip("점프로 올라가는 높이(m). 0 이면 점프가 없는 것과 같다.")]
        [SerializeField] private float jumpHeight = 0.9f;

        [Header("웅크리기")]
        [Tooltip("서 있을 때 콜라이더 높이(m).")]
        [SerializeField] private float standHeight = 1.8f;

        [Tooltip("웅크렸을 때 콜라이더 높이(m).")]
        [SerializeField] private float crouchHeight = 1.1f;

        [Tooltip("서 있을 때 눈높이(m).")]
        [SerializeField] private float standEyeHeight = 1.65f;

        [Tooltip("웅크렸을 때 눈높이(m).")]
        [SerializeField] private float crouchEyeHeight = 0.95f;

        [Tooltip("앉고 서는 데 걸리는 시간(초).")]
        [SerializeField] private float crouchTransitionSeconds = 0.18f;

        [Header("발소리")]
        [Tooltip("비워 두면 발소리가 나지 않는다. 플레이어 몸에 붙인 3D AudioSource 다.")]
        [SerializeField] private AudioSource footstepSource;

        [Tooltip("표면별로 갈지 않는다. 장마다 기본 표면 하나를 정해 여기에 넣는다.")]
        [SerializeField] private AudioClip[] footstepClips;

        [Tooltip("한 발짝의 보폭(m). 걸은 거리가 이만큼 쌓일 때마다 한 번 난다.")]
        [SerializeField] private float stepStride = 2.1f;

        [SerializeField] private float stepVolume = 0.6f;

        [Tooltip("웅크리면 작게. 이 장의 규칙이 소리로 드러나는 지점이다.")]
        [SerializeField] private float crouchStepVolume = 0.22f;

        private float stepDistance;
        private int lastStepIndex = -1;

        [Header("스태미너")]
        [Tooltip("쉬지 않고 달릴 수 있는 시간(초).")]
        [SerializeField] private float sprintSeconds = 5f;

        [Tooltip("달리기를 멈춘 뒤 회복이 시작되기까지의 시간(초).")]
        [SerializeField] private float recoveryDelaySeconds = 1.5f;

        [Tooltip("바닥난 상태에서 가득 차기까지의 시간(초).")]
        [SerializeField] private float fullRecoverySeconds = 3f;

        [Tooltip("바닥난 뒤 다시 달리려면 이만큼은 차야 한다. Shift 연타를 막는다.")]
        [Range(0f, 1f)]
        [SerializeField] private float resumeThreshold = 0.2f;

        private CharacterController controller;
        private float verticalVelocity;
        private bool isCrouching;

        /// <summary>천장 검사에 쓸 레이어. 자기 몸과 물리가 없는 것들은 뺀다.</summary>
        /// <remarks>
        /// 공용 마스크로 만들지 않은 이유는 <c>Shared/GameLayers.cs</c> 변경이 셋의 합의를
        /// 필요로 하고, 이 마스크를 쓰는 곳이 지금 여기뿐이기 때문이다. 다른 담당도
        /// 같은 것이 필요해지면 그때 옮긴다.
        /// </remarks>
        private int ceilingMask;

        public bool IsCrouching => isCrouching;

        /// <summary>**실제로 달리는 동안에만** 참. 제자리에서 Shift 를 눌러도 거짓이다.</summary>
        public bool IsSprinting { get; private set; }

        /// <summary>남은 스태미너. 0~1 이라 HUD 가 그대로 채움 비율로 쓴다.</summary>
        public float StaminaRatio { get; private set; } = 1f;

        /// <summary>회복 대기 중인가. HUD 가 이때 다른 채움 이미지를 쓴다.</summary>
        public bool IsRecoveryLocked { get; private set; }

        /// <summary>
        /// 다 써서 바닥났고 아직 회복이 시작되지 않았다. 이 동안은 걷기보다도 느리다.
        /// </summary>
        /// <remarks>
        /// 회복이 한 번이라도 돌면 <see cref="StaminaRatio"/> 가 0 을 벗어나므로 따로
        /// 플래그를 두지 않는다. 즉 이 상태는 <see cref="recoveryDelaySeconds"/> 만큼만
        /// 이어진다.
        /// </remarks>
        public bool IsExhausted => StaminaRatio <= 0f;

        /// <summary>스태미너가 바뀌었다. 0~1 을 넘긴다.</summary>
        /// <remarks>
        /// HUD 가 매 프레임 <see cref="PlayerMotor"/> 를 찾지 않도록 이벤트로 낸다.
        /// 달리거나 회복하는 동안에는 매 프레임 오지만 값 타입 하나라 할당이 없다.
        /// </remarks>
        public event Action<float> StaminaChanged;

        /// <summary>회복 대기가 시작되거나 끝났다.</summary>
        public event Action<bool> RecoveryLockChanged;

        /// <summary>바닥난 뒤 <see cref="resumeThreshold"/> 까지 다시 달릴 수 없는 상태.</summary>
        private bool sprintBlocked;

        private float recoveryUnlockTime = float.NegativeInfinity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (input == null)
            {
                Debug.LogError($"{nameof(PlayerMotor)}: {nameof(PlayerInputReader)} 가 없다.", this);
                enabled = false;
                return;
            }

            ceilingMask = ~((1 << GameLayers.Player) | (1 << GameLayers.ViewModel) | (1 << GameLayers.Trigger));

            SetHeight(standHeight);
            if (cameraPivot != null)
            {
                SetEyeHeight(standEyeHeight);
            }
        }

        private void Update()
        {
            UpdateCrouch();

            Vector2 wish = input.Move;
            Vector3 direction = transform.right * wish.x + transform.forward * wish.y;

            // 대각선이 빨라지지 않게 자른다. 스틱 입력은 이미 1 이하라 그대로 둔다.
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            // 웅크린 채로는 못 달린다. 둘 다 누르면 웅크리기가 이긴다.
            bool wantsSprint = !isCrouching && input.SprintHeld && wish.sqrMagnitude > 0.01f;
            UpdateStamina(wantsSprint);

            // 숨이 찬 동안은 걷기보다 느리다. 달리기를 다 쓰고 나면 대가가 있어야
            // 「달릴까 말까」가 판단이 된다. 웅크리기가 더 느리므로 그쪽이 먼저다.
            float speed = isCrouching ? crouchSpeed
                : IsSprinting ? sprintSpeed
                : IsExhausted ? exhaustedSpeed
                : walkSpeed;

            // 접지 상태에서도 살짝 눌러줘야 경사면에서 계단처럼 튀지 않는다.
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            // 웅크린 채로는 못 뛴다. 천장이 낮은 곳에서 뛰어 머리를 박는 것을 막는다.
            if (input.JumpPressed && controller.isGrounded && !isCrouching && jumpHeight > 0f)
            {
                verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * speed;
            velocity.y += verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            UpdateFootsteps(speed);
        }

        /// <summary>
        /// 걸은 <b>거리</b>로 발소리를 낸다. 시간으로 재면 안 된다.
        /// </summary>
        /// <remarks>
        /// <para>
        /// 시간 간격으로 내면 웅크려 느리게 갈 때도 같은 박자로 울려 「조용히 간다」가
        /// 성립하지 않는다. 거리로 재면 느리게 갈수록 저절로 뜸해진다.
        /// </para>
        /// <para>
        /// 2 장은 소리로 위치를 들키는 장이라 이 간격이 곧 규칙이다. 웅크리면 보폭이
        /// 길어지는 것처럼 처리해 발소리 수가 줄고, 달리면 반대가 된다.
        /// </para>
        /// </remarks>
        private void UpdateFootsteps(float speed)
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0)
            {
                return;
            }

            if (!controller.isGrounded)
            {
                // 공중에서는 쌓지 않는다. 착지하자마자 한 발이 터지는 것을 막는다.
                stepDistance = stepStride * 0.5f;
                return;
            }

            Vector3 flat = controller.velocity;
            flat.y = 0f;
            float moved = flat.magnitude * Time.deltaTime;
            if (moved < 0.0001f)
            {
                return;
            }

            stepDistance += moved;
            if (stepDistance < stepStride)
            {
                return;
            }

            stepDistance = 0f;

            // 같은 소리가 연달아 나면 기계처럼 들린다. 직전 것은 피한다.
            int index = UnityEngine.Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == lastStepIndex)
            {
                index = (index + 1) % footstepClips.Length;
            }

            lastStepIndex = index;
            footstepSource.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            footstepSource.volume = isCrouching ? crouchStepVolume : stepVolume;
            footstepSource.PlayOneShot(footstepClips[index]);
        }

        /// <summary>
        /// 달리는 동안 줄고, 멈추면 잠시 뒤부터 찬다. 바닥나면 걷기로 내려간다.
        /// </summary>
        /// <param name="wantsSprint">웅크리지 않고 실제로 움직이면서 달리기를 누르고 있는가.</param>
        private void UpdateStamina(bool wantsSprint)
        {
            // 바닥난 뒤에는 일정 비율까지 차야 다시 달릴 수 있다. 없으면 Shift 를
            // 연타해 한 프레임씩 달리는 것으로 제약이 무의미해진다.
            if (sprintBlocked && StaminaRatio >= resumeThreshold)
            {
                sprintBlocked = false;
            }

            IsSprinting = wantsSprint && !sprintBlocked && StaminaRatio > 0f;

            float before = StaminaRatio;

            if (IsSprinting)
            {
                if (sprintSeconds > 0f)
                {
                    StaminaRatio -= Time.deltaTime / sprintSeconds;
                }

                if (StaminaRatio <= 0f)
                {
                    StaminaRatio = 0f;
                    sprintBlocked = true;
                    IsSprinting = false;
                }

                // 달리는 동안 계속 밀어두면, 멈춘 시점부터 이 시간만큼 대기가 된다.
                recoveryUnlockTime = Time.time + recoveryDelaySeconds;
            }
            else if (StaminaRatio < 1f && Time.time >= recoveryUnlockTime)
            {
                if (fullRecoverySeconds > 0f)
                {
                    StaminaRatio = Mathf.Min(1f, StaminaRatio + Time.deltaTime / fullRecoverySeconds);
                }
                else
                {
                    StaminaRatio = 1f;
                }
            }

            if (!Mathf.Approximately(before, StaminaRatio))
            {
                StaminaChanged?.Invoke(StaminaRatio);
            }

            bool locked = StaminaRatio < 1f && Time.time < recoveryUnlockTime;
            if (locked != IsRecoveryLocked)
            {
                IsRecoveryLocked = locked;
                RecoveryLockChanged?.Invoke(locked);
            }
        }

        private void UpdateCrouch()
        {
            // 손을 뗐어도 천장이 막혀 있으면 일어서지 않는다. 그대로 서면 지형을 뚫는다.
            bool wants = input.CrouchHeld;
            if (isCrouching && !wants && BlockedAbove())
            {
                wants = true;
            }

            isCrouching = wants;

            float targetHeight = isCrouching ? crouchHeight : standHeight;
            float targetEye = isCrouching ? crouchEyeHeight : standEyeHeight;

            if (crouchTransitionSeconds <= 0f)
            {
                SetHeight(targetHeight);
                SetEyeHeight(targetEye);
                return;
            }

            float heightStep = Mathf.Abs(standHeight - crouchHeight) / crouchTransitionSeconds * Time.deltaTime;
            SetHeight(Mathf.MoveTowards(controller.height, targetHeight, heightStep));

            if (cameraPivot == null)
            {
                return;
            }

            float eyeStep = Mathf.Abs(standEyeHeight - crouchEyeHeight) / crouchTransitionSeconds * Time.deltaTime;
            SetEyeHeight(Mathf.MoveTowards(cameraPivot.localPosition.y, targetEye, eyeStep));
        }

        /// <summary>지금 높이에서 서면 천장에 걸리는지.</summary>
        private bool BlockedAbove()
        {
            float radius = controller.radius;
            Vector3 basePoint = transform.position + Vector3.up * controller.skinWidth;
            Vector3 bottom = basePoint + Vector3.up * radius;
            Vector3 top = basePoint + Vector3.up * (standHeight - radius);

            // 반지름을 아주 조금 줄인다. 벽에 붙어 선 상태에서 벽이 천장으로 잡히는 것을 막는다.
            return Physics.CheckCapsule(bottom, top, radius - 0.01f, ceilingMask, QueryTriggerInteraction.Ignore);
        }

        private void SetHeight(float height)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
        }

        private void SetEyeHeight(float eyeHeight)
        {
            Vector3 local = cameraPivot.localPosition;
            local.y = eyeHeight;
            cameraPivot.localPosition = local;
        }
    }
}
