using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 최소 HUD — 체력 3칸, 탄약, 조준선.
    /// </summary>
    /// <remarks>
    /// 값을 매 프레임 읽지 않고 이벤트로 받는다. 체력은 좀처럼 안 바뀌고 탄약도
    /// 쏠 때만 바뀌므로, <c>Update</c> 에서 폴링하면 하는 일 없이 매 프레임 문자열을
    /// 만들게 된다.
    ///
    /// 무기를 바꾸면 구독 대상도 바뀐다. 갈아탈 때 이전 무기의 구독을 반드시 끊는다 —
    /// 안 끊으면 안 든 총의 탄약이 화면에 찍힌다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerHud : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;

        [SerializeField] private WeaponSwitcher switcher;

        [Tooltip("체력 칸. 왼쪽부터 채운다.")]
        [SerializeField] private Graphic[] healthPips;

        [SerializeField] private Text ammoText;

        [Tooltip("총을 안 들었으면 숨긴다.")]
        [SerializeField] private GameObject crosshair;

        [SerializeField] private Color pipFull = new Color(0.85f, 0.87f, 0.84f, 0.92f);

        [SerializeField] private Color pipEmpty = new Color(0.85f, 0.87f, 0.84f, 0.16f);

        private PlayerWeapon subscribed;

        private void OnEnable()
        {
            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
            }

            if (switcher != null)
            {
                switcher.Equipped += OnEquipped;
            }

            Refresh();
        }

        // OnEnable 은 PlayerHealth.Awake 보다 먼저 돌 수 있다. 그러면 체력을 0 으로
        // 읽어 칸이 전부 빈 채로 시작한다. Start 는 모든 Awake 뒤라 여기서 한 번 더 읽는다.
        private void Start()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (health != null)
            {
                OnHealthChanged(health.Health);
            }

            if (switcher != null)
            {
                OnEquipped(switcher.Current);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
            }

            if (switcher != null)
            {
                switcher.Equipped -= OnEquipped;
            }

            Unsubscribe();
        }

        private void OnHealthChanged(int current)
        {
            if (healthPips == null)
            {
                return;
            }

            for (int i = 0; i < healthPips.Length; i++)
            {
                if (healthPips[i] != null)
                {
                    healthPips[i].color = i < current ? pipFull : pipEmpty;
                }
            }
        }

        private void OnEquipped(PlayerWeapon weapon)
        {
            Unsubscribe();

            subscribed = weapon;

            if (subscribed != null)
            {
                subscribed.AmmoChanged += OnAmmoChanged;
            }

            if (crosshair != null)
            {
                crosshair.SetActive(subscribed != null);
            }

            if (ammoText != null)
            {
                ammoText.enabled = subscribed != null;
            }

            if (subscribed != null)
            {
                OnAmmoChanged(subscribed.Ammo);
            }
        }

        private void OnAmmoChanged(int current)
        {
            if (ammoText == null || subscribed == null)
            {
                return;
            }

            string label = subscribed.Kind == WeaponKind.Shotgun ? "산탄총" : "권총";
            ammoText.text = $"{label}  {current} / {subscribed.MaxAmmo}";
        }

        private void Unsubscribe()
        {
            if (subscribed != null)
            {
                subscribed.AmmoChanged -= OnAmmoChanged;
                subscribed = null;
            }
        }
    }
}
