using UnityEngine;
using UnityEngine.InputSystem;

namespace Treeline
{
    /// <summary>
    /// 플레이어 입력을 한 곳에서 읽는다. 다른 플레이어 컴포넌트는 이것만 참조하고
    /// <c>InputAction</c> 을 직접 들고 있지 않는다.
    /// </summary>
    /// <remarks>
    /// 액션 에셋은 인스펙터에 끼우지 않고 프로젝트 전역 에셋
    /// (<see cref="InputSystem.actions"/>) 을 쓴다. 이 프로젝트는
    /// <c>Assets/InputSystem_Actions.inputactions</c> 가 이미 전역으로 지정돼 있어
    /// (<c>ProjectSettings/ProjectSettings.asset</c>), 프리팹마다 참조를 끼우다
    /// 하나를 빠뜨리면 그 프리팹만 조용히 입력을 못 받는 사고가 없어진다.
    ///
    /// IMPORTANT: 이 에셋의 소유자는 A 다. 액션을 더하거나 이름을 바꾸면 여기도 같이 고친다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string MapName = "Player";

        private InputActionMap map;
        private InputAction move;
        private InputAction look;
        private InputAction attack;
        private InputAction interact;
        private InputAction flashlight;
        private InputAction sprint;
        private InputAction jump;
        private InputAction crouch;
        private InputAction weapon1;
        private InputAction weapon2;
        private InputAction reload;

        /// <summary>이동 입력. x 는 좌우, y 는 앞뒤다. 이미 정규화돼 있지 않다.</summary>
        public Vector2 Move => move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>이번 프레임에 쌓인 시점 변화량. 마우스 델타라 <c>Time.deltaTime</c> 을 곱하지 않는다.</summary>
        public Vector2 Look => look != null ? look.ReadValue<Vector2>() : Vector2.zero;

        public bool AttackPressed => attack != null && attack.WasPressedThisFrame();

        public bool InteractPressed => interact != null && interact.WasPressedThisFrame();

        public bool FlashlightPressed => flashlight != null && flashlight.WasPressedThisFrame();

        /// <summary>누르고 있는 동안 참. 토글이 아니다.</summary>
        public bool SprintHeld => sprint != null && sprint.IsPressed();

        public bool JumpPressed => jump != null && jump.WasPressedThisFrame();

        /// <summary>누르고 있는 동안 참. 뗐을 때 천장이 막혀 있으면 못 일어서므로 토글로 두지 않는다.</summary>
        public bool CrouchHeld => crouch != null && crouch.IsPressed();

        public bool Weapon1Pressed => weapon1 != null && weapon1.WasPressedThisFrame();

        public bool Weapon2Pressed => weapon2 != null && weapon2.WasPressedThisFrame();

        public bool ReloadPressed => reload != null && reload.WasPressedThisFrame();

        private void Awake()
        {
            InputActionAsset asset = InputSystem.actions;
            if (asset == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerInputReader)}: 프로젝트 전역 입력 액션이 없다. " +
                    "Project Settings → Input System Package 에서 지정한다.", this);
                return;
            }

            map = asset.FindActionMap(MapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)}: '{MapName}' 액션 맵이 없다.", this);
                return;
            }

            move = Find("Move");
            look = Find("Look");
            attack = Find("Attack");
            interact = Find("Interact");
            flashlight = Find("Flashlight");
            sprint = Find("Sprint");
            jump = Find("Jump");
            crouch = Find("Crouch");
            weapon1 = Find("Weapon1");
            weapon2 = Find("Weapon2");
            reload = Find("Reload");
        }

        private InputAction Find(string actionName)
        {
            InputAction action = map.FindAction(actionName, throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)}: '{MapName}/{actionName}' 액션이 없다.", this);
            }

            return action;
        }

        private void OnEnable()
        {
            map?.Enable();
        }

        private void OnDisable()
        {
            // 사망하면 PlayerHealth 가 이 컴포넌트를 끈다. 맵이 꺼지면 나머지 컴포넌트는
            // 전부 0 을 읽으므로 각자 "죽었는지" 를 따로 확인하지 않아도 된다.
            map?.Disable();
        }
    }
}
