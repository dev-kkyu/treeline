using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 설원 렌더 위에 얹는 타이틀 메뉴. 게임 시작 · 환경 설정 · 크레딧 세 쪽을
    /// 한 패널 안에서 갈아 끼운다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 화면을 코드로 짓는다. 씬에 미리 그려 두지 않는 이유는 <b>엔딩 화면과 같은
    /// 모양</b>이어야 하는데 씬이 둘이라, 손으로 맞추면 반드시 어긋나기 때문이다.
    /// </para>
    /// <para>
    /// 쪽(page)은 켜고 끄는 것이 아니라 <b>겹쳐 두고 투명도를 넘긴다.</b> 딱딱
    /// 바뀌면 같은 자리에 다른 글자가 순간이동한 것으로 보이고, 어디서 어디로
    /// 갔는지가 안 남는다.
    /// </para>
    /// <para>
    /// 설정값 자체는 여기 없다. <see cref="GameSettings"/> 가 들고 있고 이 화면은
    /// 손잡이만 그린다 — 같은 값을 엔딩 화면과 <see cref="PlayerLook"/> 도 본다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class TitleScreen : MonoBehaviour
    {
        private const float FadeSeconds = 0.22f;
        private const float PageOutSeconds = 0.1f;
        private const float PageInSeconds = 0.16f;
        private const float PageSlide = 22f;

        [Header("Menu art")]
        [SerializeField] private Sprite panelFrame;
        [SerializeField] private Sprite buttonFrame;
        [SerializeField] private Sprite divider;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset semiBoldFont;

        [Header("Sound")]
        [Tooltip("버튼을 고르고 누를 때 나는 소리. 비워도 화면은 돈다.")]
        [SerializeField] private AudioClip clickClip;

        private readonly Color frost = new Color32(221, 228, 226, 255);
        private readonly Color muted = new Color32(140, 151, 153, 255);
        private readonly Color bruisedRed = new Color32(126, 62, 52, 255);

        private CanvasGroup screenGroup;
        private AudioSource speaker;

        private MenuPage mainPage;
        private MenuPage settingsPage;
        private MenuPage creditsPage;
        private MenuPage activePage;
        private Coroutine pageSwitch;

        private Button startButton;
        private Button settingsButton;
        private Button creditsButton;
        private Button creditsBackButton;
        private Button muteButton;
        private TMP_Text muteLabel;

        private Slider sensitivitySlider;
        private TMP_Text sensitivityValue;
        private Slider volumeSlider;
        private TMP_Text volumeValue;

        private TMP_Text gameTitle;
        private TMP_Text signalStatus;
        private Vector2 gameTitleHome;

        private float lastAudibleVolume = GameSettings.DefaultMasterVolume;
        private bool isStarting;
        private bool syncingWidgets;

        /// <summary>겹쳐 둔 쪽 하나. 켜고 끄는 대신 투명도와 좌우 오프셋을 넘긴다.</summary>
        private readonly struct MenuPage
        {
            public readonly GameObject Root;
            public readonly CanvasGroup Group;
            public readonly RectTransform Rect;
            public readonly Func<Selectable> FirstFocus;

            public MenuPage(GameObject root, Func<Selectable> firstFocus)
            {
                Root = root;
                Group = root.GetComponent<CanvasGroup>();
                Rect = root.GetComponent<RectTransform>();
                FirstFocus = firstFocus;
            }

            public bool IsValid => Root != null;
        }

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ConfigureCanvas();
            ClearAuthoredPlaceholder();
            BuildSpeaker();
            BuildInterface();
            SyncWidgetsFromSettings();
        }

        private void Start()
        {
            ShowPageImmediate(mainPage);
            StartCoroutine(FadeCanvas(0f, 1f, FadeSeconds));
            StartCoroutine(SlideMenuIn());
            StartCoroutine(SignalFlicker());
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true
                && activePage.IsValid && activePage.Root != mainPage.Root)
            {
                ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            GameSettings.Save();
        }

        private void ConfigureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // IMPORTANT: MatchWidthOrHeight 가 아니라 Expand 다. 레일이 650 x 880 짜리
            // 고정 패널이라 세로가 잘리면 버튼이 화면 밖으로 나간다. 폭 기준(0)이면
            // 2560x1080 에서 레일이 1173px 을 차지해 1080 화면을 넘고, 절반(0.5)도
            // 3440x1440 에서 1360/1440 이라 여유가 없다.
            // Expand = min(w/1920, h/1080) 이라 어떤 비율에서도 기준 화면이 통째로 들어간다.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        private void ClearAuthoredPlaceholder()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject child = transform.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private void BuildSpeaker()
        {
            var go = new GameObject("MenuAudio");
            go.transform.SetParent(transform, false);
            speaker = go.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            speaker.loop = false;
        }

        private void BuildInterface()
        {
            GameObject root = CreateUiObject("MainMenuUI", transform);
            Stretch(root.GetComponent<RectTransform>());
            screenGroup = root.AddComponent<CanvasGroup>();
            screenGroup.alpha = 0f;

            Image veil = CreateImage("SnowContrastVeil", root.transform, null,
                new Color(0.015f, 0.027f, 0.035f, 0.22f));
            Stretch(veil.rectTransform);
            veil.raycastTarget = false;

            GameObject rail = CreateUiObject("LeftMenuRail", root.transform);
            SetRect(rail.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(650f, 880f));

            Image frame = CreateImage("RelayFrame", rail.transform, panelFrame, Color.white);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.82f, 0.86f, 0.86f, 0.94f);

            Image innerShade = CreateImage("InnerShade", frame.transform, null,
                new Color(0.018f, 0.031f, 0.039f, 0.88f));
            Stretch(innerShade.rectTransform, 48f, 54f, 48f, 54f);
            innerShade.raycastTarget = false;

            signalStatus = CreateText("SignalStatus", frame.transform, "설산 중계소  /  신호 두절", 16f, muted,
                TextAlignmentOptions.Left, new Vector2(78f, -74f), new Vector2(470f, 28f), false);
            gameTitle = CreateText("GameTitle", frame.transform, "TREELINE", 72f, frost,
                TextAlignmentOptions.Left, new Vector2(78f, -118f), new Vector2(480f, 90f), true);
            gameTitleHome = gameTitle.rectTransform.anchoredPosition;
            TMP_Text subtitle = CreateText("Subtitle", frame.transform,
                "신호는 끊겼고, 아래로 내려가야 한다", 20f, muted,
                TextAlignmentOptions.Left, new Vector2(80f, -214f), new Vector2(475f, 42f), false);
            subtitle.characterSpacing = 1.5f;

            Image titleDivider = CreateImage("TitleDivider", frame.transform, divider, Color.white);
            SetTopLeft(titleDivider.rectTransform, new Vector2(80f, -270f), new Vector2(470f, 12f));
            titleDivider.type = Image.Type.Sliced;
            titleDivider.raycastTarget = false;

            mainPage = new MenuPage(BuildMainMenu(frame.transform), () => startButton);
            settingsPage = new MenuPage(BuildSettingsMenu(frame.transform), () => sensitivitySlider);
            creditsPage = new MenuPage(BuildCreditsMenu(frame.transform), () => creditsBackButton);
        }

        private GameObject BuildMainMenu(Transform parent)
        {
            GameObject page = CreatePage("MainMenu", parent);

            startButton = CreateMenuButton(page.transform, "StartButton", "게임 시작",
                new Vector2(80f, -336f), 86f, StartGame);
            settingsButton = CreateMenuButton(page.transform, "SettingsButton", "환경 설정",
                new Vector2(80f, -438f), 86f, ShowSettingsMenu);
            creditsButton = CreateMenuButton(page.transform, "CreditsButton", "크레딧",
                new Vector2(80f, -540f), 86f, ShowCreditsMenu);

            CreateText("StartHint", page.transform, "첫 번째 중계소부터 시작합니다", 15f, muted,
                TextAlignmentOptions.Left, new Vector2(84f, -650f), new Vector2(460f, 28f), false);
            CreateText("BuildLabel", page.transform, "DESKTOP WEBGL  ·  1920×1080", 13f,
                new Color(muted.r, muted.g, muted.b, 0.68f), TextAlignmentOptions.Left,
                new Vector2(84f, -746f), new Vector2(450f, 24f), false);

            return page;
        }

        private GameObject BuildSettingsMenu(Transform parent)
        {
            GameObject page = CreatePage("SettingsMenu", parent);
            Transform t = page.transform;

            CreateText("SettingsTitle", t, "환경 설정", 34f, frost, TextAlignmentOptions.Left,
                new Vector2(80f, -318f), new Vector2(330f, 54f), true);

            sensitivityValue = CreateSliderRow(t, "Sensitivity", "마우스 감도",
                "값이 클수록 시점이 빨리 돈다", -382f,
                GameSettings.MinLookSensitivity, GameSettings.MaxLookSensitivity,
                ApplySensitivity, out sensitivitySlider);

            volumeValue = CreateSliderRow(t, "Volume", "전체 음량",
                "고른 값은 브라우저에 저장된다", -516f, 0f, 1f,
                ApplyVolume, out volumeSlider);

            muteButton = CreateMenuButton(t, "MuteButton", "음소거", new Vector2(80f, -646f), 74f,
                ToggleMute, 225f);
            muteLabel = muteButton.transform.Find("Label").GetComponent<TMP_Text>();
            CreateMenuButton(t, "ResetButton", "기본값", new Vector2(325f, -646f), 74f,
                ResetSettings, 225f);
            CreateMenuButton(t, "SettingsBackButton", "뒤로", new Vector2(80f, -738f), 74f,
                ShowMainMenu);

            return page;
        }

        private GameObject BuildCreditsMenu(Transform parent)
        {
            GameObject page = CreatePage("CreditsMenu", parent);
            Transform t = page.transform;

            CreateText("CreditsTitle", t, "크레딧", 34f, frost, TextAlignmentOptions.Left,
                new Vector2(80f, -316f), new Vector2(330f, 54f), true);

            CreateSectionLabel(t, "TeamHeading", "TEAM SBJ", -366f);
            CreateCreditRow(t, "이정범", "플레이어 · 전투 · 진행 UI", -392f);
            CreateCreditRow(t, "김도한", "적 · 에셋 파이프라인", -424f);
            CreateCreditRow(t, "임성규", "상호작용 · 레벨 · 진행", -456f);

            Image rule = CreateImage("CreditsDivider", t, divider, Color.white);
            SetTopLeft(rule.rectTransform, new Vector2(84f, -494f), new Vector2(462f, 10f));
            rule.type = Image.Type.Sliced;
            rule.color = new Color(1f, 1f, 1f, 0.75f);
            rule.raycastTarget = false;

            CreateSectionLabel(t, "MadeHeading", "만든 것", -510f);
            CreateBodyText(t, "MadeBody", "모델 · 텍스처 · 사운드 · 음악", -534f, 30f);

            CreateSectionLabel(t, "ToolHeading", "쓴 도구", -574f);
            CreateBodyText(t, "ToolBody",
                "Claude Code · Codex · Meshy AI · ElevenLabs\nBlender · Unity 6.3 LTS", -598f, 52f);

            CreateSectionLabel(t, "FontHeading", "글꼴", -658f);
            CreateBodyText(t, "FontBody",
                "IBM Plex Sans KR — SIL Open Font License 1.1", -682f, 28f);

            CreateText("EventLabel", t, "NHN GAME × AI HACKATHON (NAN2026) 사전 과제", 13f,
                new Color(muted.r, muted.g, muted.b, 0.68f), TextAlignmentOptions.Left,
                new Vector2(84f, -710f), new Vector2(470f, 24f), false);

            creditsBackButton = CreateMenuButton(t, "CreditsBackButton", "뒤로",
                new Vector2(80f, -736f), 74f, ShowMainMenu);

            return page;
        }

        private GameObject CreatePage(string name, Transform parent)
        {
            GameObject page = CreateUiObject(name, parent);
            Stretch(page.GetComponent<RectTransform>());
            page.AddComponent<CanvasGroup>();
            page.SetActive(false);
            return page;
        }

        private void CreateSectionLabel(Transform parent, string name, string value, float y)
        {
            TMP_Text label = CreateText(name, parent, value, 13f, bruisedRed,
                TextAlignmentOptions.Left, new Vector2(84f, y), new Vector2(462f, 22f), true);
            label.characterSpacing = 6f;
        }

        private void CreateCreditRow(Transform parent, string person, string role, float y)
        {
            CreateText($"Name_{person}", parent, person, 20f, frost, TextAlignmentOptions.Left,
                new Vector2(84f, y), new Vector2(140f, 30f), true);
            CreateText($"Role_{person}", parent, role, 16f, muted, TextAlignmentOptions.Left,
                new Vector2(210f, y + 2f), new Vector2(336f, 28f), false);
        }

        private void CreateBodyText(Transform parent, string name, string value, float y, float height)
        {
            TMP_Text body = CreateText(name, parent, value, 15f, frost, TextAlignmentOptions.TopLeft,
                new Vector2(84f, y), new Vector2(462f, height), false);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 12f;
            body.color = new Color(frost.r, frost.g, frost.b, 0.82f);
        }

        /// <summary>이름 · 값 · 손잡이 · 설명 한 줄을 묶어 놓는다. 값 표시를 돌려준다.</summary>
        private TMP_Text CreateSliderRow(Transform parent, string name, string label, string hint,
            float y, float min, float max, UnityEngine.Events.UnityAction<float> onChanged,
            out Slider slider)
        {
            CreateText($"{name}Label", parent, label, 18f, muted, TextAlignmentOptions.Left,
                new Vector2(84f, y), new Vector2(250f, 34f), false);
            TMP_Text value = CreateText($"{name}Value", parent, "—", 18f, frost,
                TextAlignmentOptions.Right, new Vector2(410f, y), new Vector2(136f, 34f), true);

            slider = CreateSlider(parent, $"{name}Slider", new Vector2(84f, y - 44f),
                new Vector2(462f, 40f), min, max);
            slider.onValueChanged.AddListener(onChanged);

            CreateText($"{name}Hint", parent, hint, 13f,
                new Color(muted.r, muted.g, muted.b, 0.7f), TextAlignmentOptions.Left,
                new Vector2(84f, y - 94f), new Vector2(462f, 22f), false);

            return value;
        }

        private Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size,
            float min, float max)
        {
            GameObject root = CreateUiObject(name, parent);
            SetTopLeft(root.GetComponent<RectTransform>(), position, size);
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.direction = Slider.Direction.LeftToRight;

            Image track = CreateImage("Track", root.transform, null, new Color(0.035f, 0.055f, 0.065f, 0.95f));
            SetRect(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 12f));
            track.raycastTarget = false;

            GameObject fillArea = CreateUiObject("FillArea", root.transform);
            SetRect(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(-18f, 8f));
            Image fill = CreateImage("Fill", fillArea.transform, null, bruisedRed);
            Stretch(fill.rectTransform);

            GameObject handleArea = CreateUiObject("HandleArea", root.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), 9f, 0f, 9f, 0f);
            Image handle = CreateImage("Handle", handleArea.transform, null, frost);
            SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 30f));

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        private Button CreateMenuButton(Transform parent, string name, string label, Vector2 position,
            float height, UnityEngine.Events.UnityAction action, float width = 470f)
        {
            Image image = CreateImage(name, parent, buttonFrame, Color.white);
            SetTopLeft(image.rectTransform, position, new Vector2(width, height));
            image.type = Image.Type.Sliced;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.86f, 0.86f, 0.9f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.62f, 0.7f, 0.72f, 1f);
            colors.selectedColor = new Color(0.92f, 0.95f, 0.94f, 1f);
            colors.disabledColor = new Color(0.32f, 0.35f, 0.36f, 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                PlayClick();
                action();
            });

            Image mark = CreateImage("FocusMark", image.transform, null, bruisedRed);
            SetRect(mark.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(3f, -32f));
            mark.raycastTarget = false;

            const float labelLeft = 34f;
            TMP_Text text = CreateText("Label", image.transform, label, 25f, frost,
                TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, true, false);
            Stretch(text.rectTransform, labelLeft, 8f, 24f, 8f);
            text.raycastTarget = false;

            var feel = image.gameObject.AddComponent<MenuButtonFeel>();
            feel.Bind(button, mark, text, labelLeft, speaker, clickClip);
            return button;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, bool bold,
            bool topLeft = true)
        {
            GameObject go = CreateUiObject(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = bold ? semiBoldFont : regularFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            if (topLeft)
            {
                SetTopLeft(text.rectTransform, position, dimensions);
            }
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = CreateUiObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        // ---- 설정 ------------------------------------------------------------

        /// <summary>저장된 값을 손잡이에 되비춘다. 되비추는 동안 콜백이 다시 돌지 않게 막는다.</summary>
        private void SyncWidgetsFromSettings()
        {
            syncingWidgets = true;

            float volume = GameSettings.MasterVolume;
            if (volume > 0.001f)
            {
                lastAudibleVolume = volume;
            }
            volumeSlider.SetValueWithoutNotify(volume);
            sensitivitySlider.SetValueWithoutNotify(GameSettings.LookSensitivity);

            syncingWidgets = false;

            RedrawVolume(volume);
            RedrawSensitivity(GameSettings.LookSensitivity);
        }

        private void ApplyVolume(float value)
        {
            if (syncingWidgets)
            {
                return;
            }
            GameSettings.MasterVolume = value;
            if (value > 0.001f)
            {
                lastAudibleVolume = value;
            }
            RedrawVolume(value);
        }

        private void ApplySensitivity(float value)
        {
            if (syncingWidgets)
            {
                return;
            }
            GameSettings.LookSensitivity = value;
            RedrawSensitivity(value);
        }

        private void RedrawVolume(float value)
        {
            volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
            if (muteLabel != null)
            {
                muteLabel.text = value <= 0.001f ? "음소거 해제" : "음소거";
            }
        }

        private void RedrawSensitivity(float value)
        {
            sensitivityValue.text = $"{value:0.00}×";
        }

        private void ToggleMute()
        {
            volumeSlider.value = volumeSlider.value <= 0.001f
                ? Mathf.Max(0.1f, lastAudibleVolume)
                : 0f;
        }

        private void ResetSettings()
        {
            GameSettings.ResetToDefaults();
            SyncWidgetsFromSettings();
        }

        // ---- 쪽 넘기기 --------------------------------------------------------

        private void ShowMainMenu() => GoTo(mainPage, () => activePage.Root == settingsPage.Root
            ? settingsButton
            : creditsButton);

        private void ShowSettingsMenu() => GoTo(settingsPage, null);

        private void ShowCreditsMenu() => GoTo(creditsPage, null);

        private void GoTo(MenuPage next, Func<Selectable> focusOverride)
        {
            if (!next.IsValid || (activePage.IsValid && activePage.Root == next.Root))
            {
                return;
            }

            Selectable focus = focusOverride != null ? focusOverride() : null;

            if (pageSwitch != null)
            {
                StopCoroutine(pageSwitch);
            }
            pageSwitch = StartCoroutine(SwitchPage(next, focus));
        }

        private void ShowPageImmediate(MenuPage page)
        {
            foreach (MenuPage candidate in new[] { mainPage, settingsPage, creditsPage })
            {
                if (!candidate.IsValid)
                {
                    continue;
                }
                bool wanted = candidate.Root == page.Root;
                candidate.Root.SetActive(wanted);
                candidate.Group.alpha = wanted ? 1f : 0f;
                candidate.Rect.anchoredPosition = Vector2.zero;
            }

            activePage = page;
            Select(page.FirstFocus?.Invoke());
        }

        private IEnumerator SwitchPage(MenuPage next, Selectable focus)
        {
            MenuPage previous = activePage;

            // 넘어가는 동안은 어느 쪽도 못 누르게 한다. 반쯤 투명한 버튼이 눌리면
            // 방금 떠난 쪽의 동작이 뒤늦게 실행된다.
            if (previous.IsValid)
            {
                previous.Group.interactable = false;
                previous.Group.blocksRaycasts = false;
                yield return Slide(previous, 1f, 0f, 0f, -PageSlide, PageOutSeconds);
                previous.Root.SetActive(false);
            }

            activePage = next;
            next.Root.SetActive(true);
            next.Group.interactable = true;
            next.Group.blocksRaycasts = true;
            Select(focus != null ? focus : next.FirstFocus?.Invoke());
            yield return Slide(next, 0f, 1f, PageSlide, 0f, PageInSeconds);

            pageSwitch = null;
        }

        private static IEnumerator Slide(MenuPage page, float fromAlpha, float toAlpha,
            float fromX, float toX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                page.Group.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                page.Rect.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, eased), 0f);
                yield return null;
            }
            page.Group.alpha = toAlpha;
            page.Rect.anchoredPosition = new Vector2(toX, 0f);
        }

        // ---- 연출 -------------------------------------------------------------

        /// <summary>버튼이 왼쪽에서 차례로 밀려 들어온다. 셋이 동시에 뜨면 카드 한 장으로 보인다.</summary>
        private IEnumerator SlideMenuIn()
        {
            Button[] order = { startButton, settingsButton, creditsButton };
            var homes = new Vector2[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                var rect = (RectTransform)order[i].transform;
                homes[i] = rect.anchoredPosition;
                rect.anchoredPosition = homes[i] + new Vector2(-34f, 0f);
            }

            const float step = 0.07f;
            const float travel = 0.24f;
            float elapsed = 0f;
            float total = step * (order.Length - 1) + travel;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < order.Length; i++)
                {
                    float t = Mathf.Clamp01((elapsed - step * i) / travel);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    ((RectTransform)order[i].transform).anchoredPosition =
                        homes[i] + new Vector2(Mathf.Lerp(-34f, 0f, eased), 0f);
                }
                yield return null;
            }

            for (int i = 0; i < order.Length; i++)
            {
                ((RectTransform)order[i].transform).anchoredPosition = homes[i];
            }
        }

        /// <summary>
        /// 제목이 이따금 튄다. 「신호 두절」이라고 써 놓고 글자가 멀쩡히 붙어 있으면
        /// 그 문장이 배경 설명으로만 남는다.
        /// </summary>
        private IEnumerator SignalFlicker()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(4.5f, 11f));

                int bursts = UnityEngine.Random.Range(2, 5);
                for (int i = 0; i < bursts; i++)
                {
                    gameTitle.rectTransform.anchoredPosition = gameTitleHome + new Vector2(
                        UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(-2f, 2f));
                    gameTitle.color = i % 2 == 0
                        ? new Color(bruisedRed.r, bruisedRed.g, bruisedRed.b, 0.85f)
                        : frost;
                    signalStatus.color = new Color(muted.r, muted.g, muted.b, 0.35f);
                    yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.03f, 0.07f));
                }

                gameTitle.rectTransform.anchoredPosition = gameTitleHome;
                gameTitle.color = frost;
                signalStatus.color = muted;
            }
        }

        private void PlayClick()
        {
            if (speaker != null && clickClip != null)
            {
                speaker.pitch = 1f;
                speaker.PlayOneShot(clickClip, 0.8f);
            }
        }

        // ---- 시작 -------------------------------------------------------------

        private void StartGame()
        {
            if (isStarting)
            {
                return;
            }
            isStarting = true;
            GameSettings.Save();
            startButton.interactable = false;
            settingsButton.interactable = false;
            creditsButton.interactable = false;
            StartCoroutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            yield return FadeCanvas(1f, 0f, FadeSeconds);
            GameFlow.Instance.StartNewGame();
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            float elapsed = 0f;
            screenGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                screenGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            screenGroup.alpha = to;
        }

        private static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetRect(rect, Vector2.up, Vector2.up, Vector2.up, position, size);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
