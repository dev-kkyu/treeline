using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 1인칭 이동. 시점 회전은 <see cref="PlayerLook"/> 가 몸체를 돌리므로
    /// 여기서는 그 방향을 그대로 따라간다.
    /// </summary>
    /// <remarks>
    /// 달리기와 점프는 없다. 기획의 「무서운 이유」가 좁고 느린 이동에 기대고 있어
    /// 지금 넣으면 나중에 빼야 한다. 웅크리기는 2장 회피가 들어올 때 판단한다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("걷는 속도 (m/s). 정비공이라 빠르지 않다.")]
        [SerializeField] private float walkSpeed = 2.8f;

        [Tooltip("중력 가속도. 음수다.")]
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private float verticalVelocity;

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
            }
        }

        private void Update()
        {
            Vector2 wish = input.Move;
            Vector3 direction = transform.right * wish.x + transform.forward * wish.y;

            // 대각선이 빨라지지 않게 자른다. 스틱 입력은 이미 1 이하라 그대로 둔다.
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            // 접지 상태에서도 살짝 눌러줘야 경사면에서 계단처럼 튀지 않는다.
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * walkSpeed;
            velocity.y += verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
