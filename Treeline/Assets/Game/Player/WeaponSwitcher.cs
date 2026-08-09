using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 들고 있는 총을 고른다. `1` 권총, `2` 산탄총.
    /// 「무거운 것을 들면 총을 못 쓴다」 규칙도 여기서 잠근다.
    /// </summary>
    /// <remarks>
    /// 총은 <see cref="PlayerWeapon"/> 컴포넌트를 켜고 끄는 것으로 바꾼다. 꺼진
    /// 무기는 <c>Update</c> 가 돌지 않아 입력을 보지 않으므로, 각 무기가 "내가
    /// 장착됐나" 를 확인하는 분기를 두지 않아도 된다. 사망 처리가 입력 컴포넌트
    /// 하나만 끄는 것과 같은 방식이다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WeaponSwitcher : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("가진 총. 순서는 상관없고 종류로 고른다.")]
        [SerializeField] private PlayerWeapon[] weapons = Array.Empty<PlayerWeapon>();

        [Tooltip("이미 가진 총이 있으면 시작할 때 이것을 든다. 안 가졌으면 맨손이다.")]
        [SerializeField] private WeaponKind startingKind = WeaponKind.Pistol;

        private bool carrying;

        /// <summary>지금 들고 있는 총. 맨손이면 null 이다.</summary>
        public PlayerWeapon Current { get; private set; }

        /// <summary>양손이 찼는지. 참이면 총을 쓸 수 없다.</summary>
        public bool IsCarrying => carrying;

        /// <summary>장착이 바뀔 때. 맨손이 되면 null 을 넘긴다.</summary>
        public event Action<PlayerWeapon> Equipped;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (input == null)
            {
                Debug.LogError($"{nameof(WeaponSwitcher)}: {nameof(PlayerInputReader)} 가 없다.", this);
                enabled = false;
                return;
            }

            // 인스펙터에서 비워둔 칸이 있으면 여기서 걸러야 매 프레임 null 검사를 안 한다.
            int valid = 0;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    weapons[valid++] = weapons[i];
                }
            }

            if (valid != weapons.Length)
            {
                Debug.LogWarning($"{nameof(WeaponSwitcher)}: 비어 있는 무기 칸 {weapons.Length - valid} 개를 무시한다.", this);
                Array.Resize(ref weapons, valid);
            }

            foreach (PlayerWeapon weapon in weapons)
            {
                weapon.enabled = false;
            }

            // 안 주운 총은 Equip 이 거부하므로, 가진 것이 없으면 그대로 맨손이 된다.
            Equip(startingKind);
        }

        private void Update()
        {
            if (input.Weapon1Pressed)
            {
                Equip(WeaponKind.Pistol);
            }
            else if (input.Weapon2Pressed)
            {
                Equip(WeaponKind.Shotgun);
            }
        }

        /// <summary>
        /// 총을 든다. **아직 안 주웠거나** 양손이 찼으면 아무 일도 없다.
        /// </summary>
        /// <returns>실제로 들었으면 참.</returns>
        public bool Equip(WeaponKind wanted)
        {
            if (carrying)
            {
                return false;
            }

            PlayerWeapon target = Find(wanted);
            if (target == null || !target.IsOwned || target == Current)
            {
                return false;
            }

            if (Current != null)
            {
                Current.enabled = false;
            }

            Current = target;
            Current.enabled = true;
            Equipped?.Invoke(Current);
            return true;
        }

        /// <summary>
        /// 양손 운반을 켜고 끈다. 상호작용 담당(C)이 연료통 · 축전지를 들 때 부른다.
        /// </summary>
        /// <remarks>
        /// 들면 총을 내리고, 내려놓으면 들고 있던 총으로 돌아온다. 무엇을 들고
        /// 있었는지는 <see cref="Current"/> 에 남겨두므로 복원에 인자가 필요 없다.
        /// </remarks>
        public void SetCarrying(bool value)
        {
            if (carrying == value)
            {
                return;
            }

            carrying = value;

            if (Current == null)
            {
                return;
            }

            Current.enabled = !carrying;
            Equipped?.Invoke(carrying ? null : Current);
        }

        /// <summary>
        /// 총을 줍는다. 상호작용 담당(C)이 3장 경비 캐비닛 · 5장에서 부른다.
        /// </summary>
        /// <remarks>
        /// 주우면 바로 손에 든다. 양손이 차 있으면 소지만 되고 들지는 않는다 —
        /// 내려놓을 때 <see cref="SetCarrying"/> 이 되돌린다.
        /// </remarks>
        /// <returns>그런 총이 있으면 참. 이미 주웠어도 참이다.</returns>
        public bool PickUp(WeaponKind wanted)
        {
            PlayerWeapon target = Find(wanted);
            if (target == null)
            {
                return false;
            }

            target.PickUp();
            Equip(wanted);
            return true;
        }

        // 한때 AddAmmo 가 여기 있었다. 2026-08-10 에 여분 탄약을 없애면서 지웠다 —
        // 무한한 것에 더할 것이 없다.

        private PlayerWeapon Find(WeaponKind wanted)
        {
            foreach (PlayerWeapon weapon in weapons)
            {
                if (weapon.Kind == wanted)
                {
                    return weapon;
                }
            }

            return null;
        }
    }
}
