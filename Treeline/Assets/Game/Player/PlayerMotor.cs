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

        public bool IsSprinting { get; private set; }

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
            IsSprinting = !isCrouching && input.SprintHeld && wish.sqrMagnitude > 0.01f;
            float speed = isCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);

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
