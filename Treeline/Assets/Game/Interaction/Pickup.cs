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

        /// <summary>산탄총. 4장 입구 캣워크.</summary>
        Shotgun = 3,

        /// <summary>
        /// 폐지. 권총 탄약이었다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: 2026-08-10 에 탄약 획득을 없앴다. <b>값을 지우지 않고 남겨 둔다</b> —
        /// enum 은 정수로 직렬화되므로 가운데를 지우면 뒤 항목의 번호가 밀리고,
        /// 놓친 씬 하나가 조용히 다른 물건이 된다. 주우려 하면 아무 일도 일어나지 않고
        /// 경고만 남는다.
        /// </remarks>
        PistolAmmo = 4,

        /// <summary>폐지. 산탄총 탄약이었다. <see cref="PistolAmmo"/> 참고.</summary>
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

        [Tooltip("화면에 띄울 문구.")]
        [SerializeField] private string prompt = "집어 든다";

        [Tooltip("배터리는 초, 탄약은 발수. 손전등과 총은 보지 않는다.")]
        [SerializeField] private float amount = 120f;

        [Tooltip("이미 들고 있으면 시작할 때 사라진다. 앞 장에서 얻는 것을 뒷장에 안전망으로 한 번 더 놓을 때 켠다.")]
        [SerializeField] private bool hideIfAlreadyOwned;

        [Tooltip("주웠을 때. 종류마다 다른 것을 넣는다 — 배터리 · 탄약 · 그 외.")]
        [SerializeField] private AudioClip takeClip;

        [Tooltip("가득 차 못 주울 때.")]
        [SerializeField] private AudioClip deniedClip;

        [SerializeField] private float sfxVolume = 0.85f;

        private bool taken;

        /// <summary>배터리 초 · 탄약 발수.</summary>
        public float Amount => amount;

        public string Prompt => prompt;

        public bool CanInteract => !taken;

        private void Reset()
        {
            gameObject.layer = GameLayers.Interactable;
        }

        /// <summary>안전망으로 놓인 것이면 이미 들고 있는지 보고 치운다.</summary>
        /// <remarks>
        /// <para>
        /// <c>Start</c> 여야 한다. 이전 장에서 들고 온 것은
        /// <see cref="GameFlow"/> 가 <c>sceneLoaded</c> 에서 되돌리므로, 그보다
        /// 이른 <c>Awake</c> 에서는 <b>아직 빈손</b>이다.
        /// </para>
        /// <para>
        /// 켜는 것은 <b>한 물건이 두 장에 놓인 자리</b>뿐이다 (5장 문간 산탄총 —
        /// 5장부터 단독으로 시연할 때의 안전망이고, 정주행하면 4장에서 이미 얻는다).
        /// 기본값으로 두지 않은 것은 이 규칙이 조용히 틀리는 방향이기 때문이다 —
        /// 잘못 걸리면 <b>없는 획득물</b>이 되고, 1장 손전등처럼 없으면 장이 진행되지
        /// 않는 것에서 그 증상은 원인을 짚기 어렵다.
        /// </para>
        /// </remarks>
        private void Start()
        {
            if (!hideIfAlreadyOwned || !AlreadyHeld())
            {
                return;
            }

            taken = true;
            gameObject.SetActive(false);
        }

        /// <returns>플레이어가 이것이 주는 것을 이미 들고 있으면 참.</returns>
        private bool AlreadyHeld()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return false;
            }

            switch (kind)
            {
                case PickupKind.Flashlight:
                {
                    var flashlight = player.GetComponentInChildren<Flashlight>(true);
                    return flashlight != null && flashlight.IsOwned;
                }

                case PickupKind.Pistol:
                case PickupKind.Shotgun:
                {
                    WeaponKind want = kind == PickupKind.Pistol ? WeaponKind.Pistol : WeaponKind.Shotgun;
                    foreach (PlayerWeapon weapon in player.GetComponentsInChildren<PlayerWeapon>(true))
                    {
                        if (weapon.Kind == want)
                        {
                            return weapon.IsOwned;
                        }
                    }

                    return false;
                }

                default:
                    // 배터리에는 「이미 들고 있다」가 없다. 얼마든지 더 주울 수 있다.
                    return false;
            }
        }

        public void Interact(GameObject who)
        {
            if (taken)
            {
                return;
            }

            if (!Give(who))
            {
                if (deniedClip != null)
                {
                    AudioSource.PlayClipAtPoint(deniedClip, transform.position, sfxVolume);
                }

                // 탄약이 가득 차 한 발도 안 들어간 경우다. 사라지면 없어진 것으로 보이므로
                // 그대로 둔다. 나중에 쓰고 와서 다시 주우면 된다.
                return;
            }

            taken = true;

            // 오브젝트를 끄기 **전에** 낸다. 끄고 나서 자기 AudioSource 로 내면
            // 소리가 함께 죽는다. 그 자리에서 재생하는 방식이라 안전하다.
            if (takeClip != null)
            {
                AudioSource.PlayClipAtPoint(takeClip, transform.position, sfxVolume);
            }

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
                    // 폐지된 종류다. 씬에서 지웠어야 할 것이 남아 있다는 뜻이므로
                    // 조용히 사라지게 두지 않고 어디에 있는지 알린다.
                    Debug.LogWarning($"[Pickup] '{name}' 은 폐지된 탄약 획득물이다. 씬에서 지운다.", this);
                    return false;
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
