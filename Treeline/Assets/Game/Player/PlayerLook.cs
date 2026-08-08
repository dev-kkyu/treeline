using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 시점 회전. 좌우는 몸체를, 상하는 카메라를 돌린다.
    /// 몸체를 돌려야 <see cref="PlayerMotor"/> 의 이동 방향이 따라온다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("상하 회전을 적용할 카메라. 보통 자식의 Camera 다.")]
        [SerializeField] private Transform cameraPivot;

        [Tooltip("입력 1 당 회전 각도(도). 마우스는 픽셀 단위 델타라 값이 작다.")]
        [SerializeField] private float sensitivity = 0.08f;

        [Tooltip("올려다보고 내려다볼 수 있는 한계(도).")]
        [SerializeField] private float pitchLimit = 85f;

        private float pitch;

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

            Vector2 delta = input.Look * sensitivity;

            transform.Rotate(0f, delta.x, 0f, Space.Self);

            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
