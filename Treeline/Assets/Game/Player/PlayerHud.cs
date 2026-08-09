using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// HUD — 체력 · 탄약 · 배터리 · 스태미너 · 조준점 · 상호작용 문구 · 피격 오버레이.
    /// 스프라이트와 표시 규칙은 <c>docs/ui-assets/README.md</c> 의 「HUD 동작 제안」 표를 따른다.
    /// </summary>
    /// <remarks>
    /// 값이 드물게 바뀌는 것(체력 · 탄약 · 무기 · 손전등 켜짐)은 이벤트로 받는다.
    /// 매 프레임 바뀌는 것(배터리 잔량 · 조준 대상)은 <c>Update</c> 에서 읽는다 —
    /// 이벤트로 만들어도 결국 매 프레임 오므로 이득이 없다.
    ///
    /// 무기를 바꾸면 구독 대상도 바뀐다. 갈아탈 때 이전 무기의 구독을 반드시 끊는다 —
    /// 안 끊으면 안 든 총의 탄약이 화면에 찍힌다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerHud : MonoBehaviour
    {
        /// <summary>
        /// 평상시 불투명도. `ui-assets/README.md` 의 제안값(45 · 65 · 55 · 35%)보다 높다 —
        /// 실제 화면에서 어두워 안 보인다는 지적이 있어 올렸다.
        /// </summary>
        private const float HealthIdleAlpha = 0.75f;

        private const float AmmoAlpha = 0.85f;
        private const float StaminaAlpha = 0.85f;
        private const float CrosshairAlpha = 0.5f;

        /// <summary>손전등이 켜져 있을 때의 배터리 게이지 불투명도.</summary>
        private const float BatteryOnAlpha = 0.95f;

        /// <summary>꺼져 있을 때. 게이지를 없애지 않고 흐리게 해서 켜짐을 알린다.</summary>
        private const float BatteryOffAlpha = 0.4f;

        /// <summary>꺼져 있을 때 배터리 게이지에 씌우는 색. 빛이 죽은 계기판이다.</summary>
        private static readonly Color BatteryOffTint = new Color(0.55f, 0.6f, 0.68f, 1f);

        /// <summary>피격 직후 체력을 또렷하게 두는 시간(초).</summary>
        private const float HealthHighlightSeconds = 2f;

        /// <summary>한 번 깜빡이는 주기(초). 켜짐 · 꺼짐이 절반씩이다.</summary>
        private const float BlinkPeriod = 0.3f;

        /// <summary>짧은 경고 점멸의 총 길이(초). 두 번 깜빡인다.</summary>
        private const float BlinkBurstSeconds = 0.6f;

        private const float LowAmmoRounds = 4;
        private const float LowBatteryRatio = 0.25f;
        private const float CriticalBatteryRatio = 0.1f;

        private const float DamageOverlayPeakAlpha = 0.45f;
        private const float DamageOverlayHoldSeconds = 0.35f;
        private const float DamageOverlayFadeSeconds = 1.2f;

        private static readonly Color WarningColor = new Color(0.72f, 0.24f, 0.22f, 1f);

        [Header("읽는 대상")]
        [SerializeField] private PlayerHealth health;

        [SerializeField] private WeaponSwitcher switcher;

        [SerializeField] private PlayerMotor motor;

        [SerializeField] private Flashlight flashlight;

        [SerializeField] private PlayerInteractor interactor;

        [Header("체력")]
        [SerializeField] private CanvasGroup healthGroup;

        [Tooltip("체력 칸. 왼쪽부터 채운다.")]
        [SerializeField] private Image[] healthPips;

        [SerializeField] private Sprite pipActive;

        [SerializeField] private Sprite pipEmpty;

        [Header("탄약")]
        [SerializeField] private CanvasGroup ammoGroup;

        [Tooltip("탄창 눈금. 무기의 탄창 크기만큼만 켠다.")]
        [SerializeField] private Image[] ammoTicks;

        [Tooltip("남은 여분과 재장전 표시.")]
        [SerializeField] private Text ammoReserveText;

        [Header("배터리")]
        [SerializeField] private CanvasGroup batteryGroup;

        [Tooltip("손전등이 꺼지면 프레임까지 같이 죽인다.")]
        [SerializeField] private Image batteryFrame;

        [SerializeField] private Image batteryFill;

        [SerializeField] private Sprite batteryFullSprite;

        [SerializeField] private Sprite batteryLowSprite;

        [Header("스태미너")]
        [SerializeField] private CanvasGroup staminaGroup;

        [SerializeField] private Image staminaFill;

        [SerializeField] private Sprite staminaReadySprite;

        [SerializeField] private Sprite staminaLockedSprite;

        [Header("조준점과 상호작용")]
        [SerializeField] private Image crosshair;

        [SerializeField] private Sprite crosshairSprite;

        [SerializeField] private Sprite interactSprite;

        [SerializeField] private float crosshairSize = 32f;

        [SerializeField] private float interactReticleSize = 48f;

        [SerializeField] private CanvasGroup interactGroup;

        [SerializeField] private Text interactText;

        [Header("피격")]
        [SerializeField] private Image damageOverlay;

        [Tooltip("화면 전체를 덮는 검은 판. 붙잡혀 죽는 연출이 이걸 닫는다.")]
        [SerializeField] private Image blackout;

        private PlayerWeapon subscribed;
        private RectTransform crosshairRect;

        private float healthHighlightUntil = float.NegativeInfinity;
        private float ammoBlinkStart = float.NegativeInfinity;
        private float staminaBlinkStart = float.NegativeInfinity;
        private float damageOverlayStart = float.NegativeInfinity;

        private float blackoutStart = float.NegativeInfinity;
        private float blackoutSeconds;
        private bool cutscene;

        private int lastHealth = int.MaxValue;
        private int lastLoaded = int.MaxValue;
        private bool usingInteractReticle;

        private void Awake()
        {
            HudGuideText.AttachTo(transform);
            ScreenFrostEffect.AttachTo(transform, motor != null ? motor.transform : transform.root);

            if (crosshair != null)
            {
                crosshairRect = crosshair.rectTransform;
            }
        }

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

            if (motor != null)
            {
                motor.StaminaChanged += OnStaminaChanged;
            }

            Refresh();
        }

        // OnEnable 은 PlayerHealth.Awake 보다 먼저 돌 수 있다. 그러면 체력을 0 으로
        // 읽어 칸이 전부 빈 채로 시작한다. Start 는 모든 Awake 뒤라 여기서 한 번 더 읽는다.
        private void Start()
        {
            Refresh();
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

            if (motor != null)
            {
                motor.StaminaChanged -= OnStaminaChanged;
            }

            Unsubscribe();
        }

        private void Refresh()
        {
            if (health != null)
            {
                lastHealth = health.Health;
                DrawHealthPips(health.Health);
            }

            if (switcher != null)
            {
                OnEquipped(switcher.Current);
            }

            if (motor != null)
            {
                OnStaminaChanged(motor.StaminaRatio);
            }
        }

        private void Update()
        {
            if (cutscene)
            {
                // 조작을 뺏긴 동안은 게이지가 의미가 없다. 암전만 계속 몬다.
                UpdateBlackout();
                return;
            }

            UpdateHealth();
            UpdateAmmo();
            UpdateBattery();
            UpdateStamina();
            UpdateInteraction();
            UpdateDamageOverlay();
            UpdateBlackout();
        }

        /// <summary>
        /// 화면을 검게 닫는다. 열리지 않는다 — 장이 다시 시작하며 HUD 가 새로 만들어진다.
        /// </summary>
        /// <remarks>
        /// 붙잡혀 죽는 연출이 부른다. 무는 순간에 닫아야 그 뒤의 흔드는 동작이
        /// 화면에 안 남는다. 인자가 없는 <c>UnityEvent</c> 로도 쓸 수 있게
        /// <see cref="Blackout()"/> 를 같이 둔다.
        /// </remarks>
        public void Blackout(float seconds)
        {
            blackoutStart = Time.time;
            blackoutSeconds = Mathf.Max(0f, seconds);
        }

        /// <summary>인스펙터의 <c>UnityEvent</c> 에 그대로 걸기 위한 것.</summary>
        public void Blackout()
        {
            Blackout(0.12f);
        }

        /// <summary>UnityEvent에서 호출할 수 있는 고정형 HUD 안내.</summary>
        public void ShowGuide(string message)
        {
            HudGuideText.Show(message, 0f);
        }

        /// <summary>코드에서 호출하는 자동 종료형 HUD 안내.</summary>
        public void ShowGuide(string message, float seconds)
        {
            HudGuideText.Show(message, seconds);
        }

        /// <summary>현재 HUD 안내를 닫는다.</summary>
        public void HideGuide()
        {
            HudGuideText.Hide();
        }

        /// <summary>
        /// 게이지를 전부 치운다. 조작을 뺏기는 연출이 들어올 때 부른다.
        /// </summary>
        /// <remarks>
        /// 죽는 연출 3 초 동안 체력칸과 탄창이 화면에 남아 있으면 긴장이 깨진다.
        /// 되돌리는 함수를 두지 않은 것은, 이 상태에서 빠져나오는 길이 <b>장을 다시
        /// 시작하는 것</b>뿐이고 그때 HUD 가 새로 만들어지기 때문이다.
        /// </remarks>
        public void HideForCutscene()
        {
            cutscene = true;
            HudGuideText.HideImmediate();
            ScreenFrostEffect.HideImmediate();

            SetGroupAlpha(healthGroup);
            SetGroupAlpha(ammoGroup);
            SetGroupAlpha(batteryGroup);
            SetGroupAlpha(staminaGroup);
            SetGroupAlpha(interactGroup);

            if (crosshair != null)
            {
                crosshair.enabled = false;
            }

            if (damageOverlay != null)
            {
                damageOverlay.enabled = false;
            }
        }

        private static void SetGroupAlpha(CanvasGroup group)
        {
            if (group != null)
            {
                group.alpha = 0f;
            }
        }

        private void UpdateBlackout()
        {
            if (blackout == null)
            {
                return;
            }

            float alpha = 0f;
            if (blackoutStart > float.NegativeInfinity)
            {
                float elapsed = Time.time - blackoutStart;
                alpha = blackoutSeconds > 0f ? Mathf.Clamp01(elapsed / blackoutSeconds) : 1f;
            }

            bool draw = alpha > 0f;
            if (draw)
            {
                Color color = blackout.color;
                color.a = alpha;
                blackout.color = color;
            }

            if (blackout.enabled != draw)
            {
                blackout.enabled = draw;
            }
        }

        // --- 체력 ---

        private void OnHealthChanged(int current)
        {
            // 회복은 강조하지 않는다. 강조는 「맞았다」를 알리는 신호다.
            if (current < lastHealth)
            {
                healthHighlightUntil = Time.time + HealthHighlightSeconds;
                damageOverlayStart = Time.time;
            }

            lastHealth = current;
            DrawHealthPips(current);
        }

        private void DrawHealthPips(int current)
        {
            if (healthPips == null)
            {
                return;
            }

            for (int i = 0; i < healthPips.Length; i++)
            {
                if (healthPips[i] != null)
                {
                    healthPips[i].sprite = i < current ? pipActive : pipEmpty;
                }
            }
        }

        private void UpdateHealth()
        {
            if (healthGroup != null)
            {
                healthGroup.alpha = Time.time < healthHighlightUntil ? 1f : HealthIdleAlpha;
            }
        }

        // --- 탄약 ---

        private void OnEquipped(PlayerWeapon weapon)
        {
            Unsubscribe();

            subscribed = weapon;

            if (subscribed != null)
            {
                subscribed.AmmoChanged += OnAmmoChanged;
                subscribed.ReloadingChanged += OnReloadingChanged;
            }

            lastLoaded = subscribed != null ? subscribed.Loaded : int.MaxValue;
            OnAmmoChanged();
        }

        private void OnReloadingChanged(bool reloading)
        {
            OnAmmoChanged();
        }

        private void OnAmmoChanged()
        {
            if (subscribed == null)
            {
                if (ammoReserveText != null)
                {
                    ammoReserveText.text = string.Empty;
                }

                SetTicksVisible(0, 0);
                return;
            }

            int loaded = subscribed.Loaded;

            // 다 쏴서 비었을 때만 알린다. 재장전으로 0 을 거쳐 가는 것은 알릴 일이 아니다.
            if (loaded == 0 && lastLoaded > 0 && !subscribed.IsReloading)
            {
                ammoBlinkStart = Time.time;
            }

            lastLoaded = loaded;

            SetTicksVisible(subscribed.MagazineSize, loaded);

            if (ammoReserveText != null)
            {
                // 화면 문구는 영문으로 쓴다. 기본 폰트(LegacyRuntime.ttf)에 한글 글리프가
                // 없어 에디터에서는 시스템 폰트로 대체되지만 WebGL 빌드에서 네모로 깨진다.
                ammoReserveText.text = subscribed.IsReloading
                    ? "RELOADING"
                    : "x" + subscribed.Reserve;
            }
        }

        /// <summary>탄창 크기만큼 눈금을 켜고, 그중 남은 발수만 채운 색으로 둔다.</summary>
        private void SetTicksVisible(int magazine, int loaded)
        {
            if (ammoTicks == null)
            {
                return;
            }

            for (int i = 0; i < ammoTicks.Length; i++)
            {
                if (ammoTicks[i] == null)
                {
                    continue;
                }

                bool exists = i < magazine;
                if (ammoTicks[i].gameObject.activeSelf != exists)
                {
                    ammoTicks[i].gameObject.SetActive(exists);
                }

                if (!exists)
                {
                    continue;
                }

                bool filled = i < loaded;
                Color color = loaded <= LowAmmoRounds ? WarningColor : Color.white;
                color.a = filled ? 1f : 0.18f;
                ammoTicks[i].color = color;
            }
        }

        private void UpdateAmmo()
        {
            if (ammoGroup == null)
            {
                return;
            }

            ammoGroup.alpha = subscribed == null
                ? 0f
                : AmmoAlpha * BurstBlink(ammoBlinkStart);
        }

        // --- 배터리 ---

        private void UpdateBattery()
        {
            if (batteryGroup == null)
            {
                return;
            }

            // 켜져 있을 때만 띄우지 않고 늘 띄운다. 남은 배터리는 켜기 전에 알아야
            // 쓸모가 있고, 이 게임에서 손전등을 켤지 말지가 곧 자원 관리다.
            // 다만 아직 안 주웠으면 없는 물건의 잔량이라 띄우지 않는다.
            if (flashlight == null || !flashlight.IsOwned)
            {
                batteryGroup.alpha = 0f;
                return;
            }

            float ratio = flashlight.BatteryRatio;
            bool on = flashlight.IsOn;

            // 켜짐과 꺼짐은 게이지를 없앴다 살리는 대신 밝기로 구분한다. 사라지는 UI 는
            // 「무슨 일이 생겼다」로 읽히는데, 손전등을 끄는 것은 사건이 아니라 상태다.
            float alpha = on ? BatteryOnAlpha : BatteryOffAlpha;

            if (ratio <= CriticalBatteryRatio)
            {
                // 느린 호흡. 짧은 점멸은 「방금 뭔가 일어났다」는 뜻이라 여기 안 맞는다.
                alpha *= 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
            }

            batteryGroup.alpha = alpha;

            if (batteryFrame != null)
            {
                batteryFrame.color = on ? Color.white : BatteryOffTint;
            }

            if (batteryFill != null)
            {
                batteryFill.fillAmount = ratio;
                batteryFill.color = on ? Color.white : BatteryOffTint;

                Sprite wanted = ratio <= LowBatteryRatio ? batteryLowSprite : batteryFullSprite;
                if (wanted != null && batteryFill.sprite != wanted)
                {
                    batteryFill.sprite = wanted;
                }
            }
        }

        // --- 스태미너 ---

        private void OnStaminaChanged(float ratio)
        {
            if (ratio <= 0f)
            {
                staminaBlinkStart = Time.time;
            }

            if (staminaFill != null)
            {
                staminaFill.fillAmount = ratio;
            }
        }

        private void UpdateStamina()
        {
            if (staminaGroup == null || motor == null)
            {
                return;
            }

            // 늘 띄운다. 달릴 수 있는 여유가 남았는지는 달리기 전에 알아야 쓸모가 있고,
            // 게이지가 떴다 사라지면 그 자체가 시선을 끌어 어둠을 흐트러뜨린다.
            staminaGroup.alpha = StaminaAlpha * BurstBlink(staminaBlinkStart);

            if (staminaFill == null)
            {
                return;
            }

            Sprite wanted = motor.IsRecoveryLocked ? staminaLockedSprite : staminaReadySprite;
            if (wanted != null && staminaFill.sprite != wanted)
            {
                staminaFill.sprite = wanted;
            }
        }

        // --- 조준점과 상호작용 ---

        private void UpdateInteraction()
        {
            string prompt = interactor != null ? interactor.Prompt : string.Empty;
            bool hasTarget = !string.IsNullOrEmpty(prompt);

            if (interactGroup != null)
            {
                interactGroup.alpha = hasTarget ? 1f : 0f;
            }

            if (hasTarget && interactText != null && interactText.text != prompt)
            {
                interactText.text = prompt;
            }

            if (crosshair == null)
            {
                return;
            }

            // 총을 안 들었어도 조준점은 남긴다. 화면 중앙이 어디인지 모르면 상호작용을 못 겨눈다.
            if (hasTarget != usingInteractReticle)
            {
                usingInteractReticle = hasTarget;
                crosshair.sprite = hasTarget ? interactSprite : crosshairSprite;

                if (crosshairRect != null)
                {
                    float size = hasTarget ? interactReticleSize : crosshairSize;
                    crosshairRect.sizeDelta = new Vector2(size, size);
                }
            }

            Color color = crosshair.color;
            color.a = hasTarget ? 1f : CrosshairAlpha;
            crosshair.color = color;
        }

        // --- 피격 ---

        private void UpdateDamageOverlay()
        {
            if (damageOverlay == null)
            {
                return;
            }

            float elapsed = Time.time - damageOverlayStart;
            float alpha;

            if (elapsed < 0f || elapsed >= DamageOverlayHoldSeconds + DamageOverlayFadeSeconds)
            {
                alpha = 0f;
            }
            else if (elapsed < DamageOverlayHoldSeconds)
            {
                alpha = DamageOverlayPeakAlpha;
            }
            else
            {
                float t = (elapsed - DamageOverlayHoldSeconds) / DamageOverlayFadeSeconds;
                alpha = Mathf.Lerp(DamageOverlayPeakAlpha, 0f, t);
            }

            Color color = damageOverlay.color;
            if (!Mathf.Approximately(color.a, alpha))
            {
                color.a = alpha;
                damageOverlay.color = color;
            }

            // 다 사라진 뒤에는 그리지 않는다. 화면 전체 크기라 투명해도 채우기 비용이 든다.
            bool draw = alpha > 0f;
            if (damageOverlay.enabled != draw)
            {
                damageOverlay.enabled = draw;
            }
        }

        // --- 공통 ---

        /// <summary>짧은 경고 점멸. 시작 시각부터 두 번 깜빡이고 1 로 돌아간다.</summary>
        private static float BurstBlink(float startTime)
        {
            float elapsed = Time.time - startTime;
            if (elapsed < 0f || elapsed >= BlinkBurstSeconds)
            {
                return 1f;
            }

            return Mathf.Repeat(elapsed, BlinkPeriod) < BlinkPeriod * 0.5f ? 0.15f : 1f;
        }

        private void Unsubscribe()
        {
            if (subscribed != null)
            {
                subscribed.AmmoChanged -= OnAmmoChanged;
                subscribed.ReloadingChanged -= OnReloadingChanged;
                subscribed = null;
            }
        }
    }
}
