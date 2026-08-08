using UnityEngine;

namespace Treeline
{
    /// <summary>주울 수 있는 것. 하나에 하나씩만 담는다.</summary>
    public enum PickupKind
    {
        /// <summary>손전등 본체. 1장에서 줍기 전에는 켜지지 않는다.</summary>
        Flashlight = 0,

        /// <summary>손전등 배터리. <see cref="Pickup.Amount"/> 는 초다.</summary>
        Battery = 1,

        /// <summary>권총. 3장 경비 캐비닛.</summary>
        Pistol = 2,

        /// <summary>산탄총. 5장.</summary>
        Shotgun = 3,

        /// <summary>권총 탄약. <see cref="Pickup.Amount"/> 는 발수다.</summary>
        PistolAmmo = 4,

        /// <summary>산탄총 탄약. <see cref="Pickup.Amount"/> 는 발수다.</summary>
        ShotgunAmmo = 5,
    }

    /// <summary>
    /// 주우면 플레이어에게 무언가를 주는 물건.
    /// </summary>
    /// <remarks>
    /// 획득물마다 클래스를 만들지 않는다. 장치를 세 패턴으로 수렴시킨 것과 같은 기준이고,
    /// 여섯 종류의 차이는 <b>A 의 어느 함수를 부르는가</b>뿐이다.
    /// <para>
    /// 「주워야 쓴다」는 손전등 · 권총 · 산탄총 셋 모두에 해당한다. 기획이 장마다 무기를
    /// 하나씩 얻게 해둔 것과 맞춘 규칙이고, <b>총과 탄약은 따로다</b> — 총을 안 주웠어도
    /// 탄약은 쌓이므로 「탄약만 먼저 발견」 같은 배치가 된다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Pickup : MonoBehaviour
        , IInteractable
    {
        [Tooltip("무엇을 주는가.")]
        [SerializeField] private PickupKind kind = PickupKind.Battery;

        [Tooltip("화면에 띄울 문구. 기본 폰트에 한글 글리프가 없어 영문으로 쓴다.")]
        [SerializeField] private string prompt = "Pick up";

        [Tooltip("배터리는 초, 탄약은 발수. 손전등과 총은 보지 않는다.")]
        [SerializeField] private float amount = 120f;

        private bool taken;

        /// <summary>배터리 초 · 탄약 발수.</summary>
        public float Amount => amount;

        public string Prompt => prompt;

        public bool CanInteract => !taken;

        private void Reset()
        {
            gameObject.layer = GameLayers.Interactable;
        }

        public void Interact(GameObject who)
        {
            if (taken)
            {
                return;
            }

            if (!Give(who))
            {
                // 탄약이 가득 차 한 발도 안 들어간 경우다. 사라지면 없어진 것으로 보이므로
                // 그대로 둔다. 나중에 쓰고 와서 다시 주우면 된다.
                return;
            }

            taken = true;
            gameObject.SetActive(false);
        }

        /// <returns>실제로 무언가 주었으면 참.</returns>
        private bool Give(GameObject who)
        {
            switch (kind)
            {
                case PickupKind.Flashlight:
                {
                    var flashlight = who.GetComponentInParent<Flashlight>();
                    if (flashlight == null)
                    {
                        return Missing(nameof(Flashlight));
                    }

                    flashlight.PickUp();
                    return true;
                }

                case PickupKind.Battery:
                {
                    var flashlight = who.GetComponentInParent<Flashlight>();
                    if (flashlight == null)
                    {
                        return Missing(nameof(Flashlight));
                    }

                    flashlight.AddBattery(amount);
                    return true;
                }

                case PickupKind.Pistol:
                case PickupKind.Shotgun:
                {
                    var weapons = who.GetComponentInParent<WeaponSwitcher>();
                    if (weapons == null)
                    {
                        return Missing(nameof(WeaponSwitcher));
                    }

                    return weapons.PickUp(kind == PickupKind.Pistol ? WeaponKind.Pistol : WeaponKind.Shotgun);
                }

                case PickupKind.PistolAmmo:
                case PickupKind.ShotgunAmmo:
                {
                    var weapons = who.GetComponentInParent<WeaponSwitcher>();
                    if (weapons == null)
                    {
                        return Missing(nameof(WeaponSwitcher));
                    }

                    WeaponKind target = kind == PickupKind.PistolAmmo ? WeaponKind.Pistol : WeaponKind.Shotgun;
                    return weapons.AddAmmo(target, Mathf.RoundToInt(amount)) > 0;
                }

                default:
                    return false;
            }
        }

        private bool Missing(string what)
        {
            Debug.LogWarning($"[Pickup] '{name}' 을 주웠는데 플레이어에 {what} 이 없다.", this);
            return false;
        }
    }
}
