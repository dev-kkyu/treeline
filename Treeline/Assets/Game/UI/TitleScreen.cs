using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 설원 렌더 위에 표시되는 타이틀 메뉴를 구성하고 게임 시작·음량 설정을 처리한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class TitleScreen : MonoBehaviour
    {
        private const string MasterVolumeKey = "Treeline.Audio.MasterVolume";
        private const float DefaultVolume = 0.8f;
        private const float FadeSeconds = 0.22f;

        [Header("Menu art")]
        [SerializeField] private Sprite panelFrame;
        [SerializeField] private Sprite buttonFrame;
        [SerializeField] private Sprite divider;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset regularFont;
        [SerializeField] private TMP_FontAsset semiBoldFont;

        private readonly Color frost = new Color32(221, 228, 226, 255);
        private readonly Color muted = new Color32(140, 151, 153, 255);
        private readonly Color bruisedRed = new Color32(126, 62, 52, 255);

        private CanvasGroup screenGroup;
        private GameObject mainMenu;
        private GameObject soundMenu;
        private Button startButton;
        private Button soundButton;
        private Button backButton;
        private Button muteButton;
        private TMP_Text muteLabel;
        private TMP_Text volumeValue;
        private Slider volumeSlider;
        private float lastAudibleVolume = DefaultVolume;
        private bool isStarting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedVolumeOnBoot()
        {
            float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
            AudioListener.volume = saved * saved;
        }

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ConfigureCanvas();
            ClearAuthoredPlaceholder();
            BuildInterface();
            LoadAudioSettings();
        }

        private void Start()
        {
            Select(startButton);
            StartCoroutine(FadeCanvas(0f, 1f, FadeSeconds));
        }

        private void Update()
        {
            if (soundMenu.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            SaveVolume(volumeSlider != null ? volumeSlider.value : DefaultVolume);
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

            CreateText("SignalStatus", frame.transform, "설산 중계소  /  신호 두절", 16f, muted,
                TextAlignmentOptions.Left, new Vector2(78f, -74f), new Vector2(470f, 28f), false);
            CreateText("GameTitle", frame.transform, "TREELINE", 72f, frost,
                TextAlignmentOptions.Left, new Vector2(78f, -118f), new Vector2(480f, 90f), true);
            TMP_Text subtitle = CreateText("Subtitle", frame.transform,
                "신호는 끊겼고, 아래로 내려가야 한다", 20f, muted,
                TextAlignmentOptions.Left, new Vector2(80f, -214f), new Vector2(475f, 42f), false);
            subtitle.characterSpacing = 1.5f;

            Image titleDivider = CreateImage("TitleDivider", frame.transform, divider, Color.white);
            SetTopLeft(titleDivider.rectTransform, new Vector2(80f, -270f), new Vector2(470f, 12f));
            titleDivider.type = Image.Type.Sliced;
            titleDivider.raycastTarget = false;

            mainMenu = CreateUiObject("MainMenu", frame.transform);
            Stretch(mainMenu.GetComponent<RectTransform>());

            startButton = CreateMenuButton(mainMenu.transform, "StartButton", "게임 시작",
                new Vector2(80f, -352f), StartGame);
            soundButton = CreateMenuButton(mainMenu.transform, "SoundButton", "사운드 설정",
                new Vector2(80f, -466f), ShowSoundMenu);

            CreateText("StartHint", mainMenu.transform, "첫 번째 중계소부터 시작합니다", 15f, muted,
                TextAlignmentOptions.Left, new Vector2(84f, -568f), new Vector2(460f, 28f), false);
            CreateText("BuildLabel", mainMenu.transform, "DESKTOP WEBGL  ·  1920×1080", 13f,
                new Color(muted.r, muted.g, muted.b, 0.68f), TextAlignmentOptions.Left,
                new Vector2(84f, -746f), new Vector2(450f, 24f), false);

            soundMenu = CreateUiObject("SoundMenu", frame.transform);
            Stretch(soundMenu.GetComponent<RectTransform>());
            BuildSoundMenu(soundMenu.transform);
            soundMenu.SetActive(false);
        }

        private void BuildSoundMenu(Transform parent)
        {
            CreateText("SoundTitle", parent, "사운드", 34f, frost, TextAlignmentOptions.Left,
                new Vector2(80f, -332f), new Vector2(330f, 54f), true);
            CreateText("MasterLabel", parent, "전체 음량", 18f, muted, TextAlignmentOptions.Left,
                new Vector2(84f, -410f), new Vector2(250f, 34f), false);
            volumeValue = CreateText("VolumeValue", parent, "80%", 18f, frost, TextAlignmentOptions.Right,
                new Vector2(410f, -410f), new Vector2(135f, 34f), true);

            volumeSlider = CreateVolumeSlider(parent, new Vector2(84f, -462f), new Vector2(460f, 42f));
            volumeSlider.onValueChanged.AddListener(ApplyVolume);

            muteButton = CreateMenuButton(parent, "MuteButton", "음소거", new Vector2(80f, -546f), ToggleMute);
            muteLabel = muteButton.GetComponentInChildren<TMP_Text>(true);
            backButton = CreateMenuButton(parent, "BackButton", "뒤로", new Vector2(80f, -660f), ShowMainMenu);

            CreateText("SaveHint", parent, "변경한 음량은 브라우저에 자동 저장됩니다", 14f, muted,
                TextAlignmentOptions.Left, new Vector2(84f, -766f), new Vector2(470f, 28f), false);
        }

        private Slider CreateVolumeSlider(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = CreateUiObject("MasterVolumeSlider", parent);
            SetTopLeft(root.GetComponent<RectTransform>(), position, size);
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
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
            UnityEngine.Events.UnityAction action)
        {
            Image image = CreateImage(name, parent, buttonFrame, Color.white);
            SetTopLeft(image.rectTransform, position, new Vector2(470f, 86f));
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
            button.onClick.AddListener(action);

            TMP_Text text = CreateText("Label", image.transform, label, 25f, frost,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, true, false);
            Stretch(text.rectTransform, 34f, 10f, 34f, 10f);
            text.raycastTarget = false;
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
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
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

        private void LoadAudioSettings()
        {
            float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
            if (saved > 0.001f)
            {
                lastAudibleVolume = saved;
            }
            volumeSlider.SetValueWithoutNotify(saved);
            ApplyVolume(saved);
        }

        private void ApplyVolume(float value)
        {
            value = Mathf.Clamp01(value);
            AudioListener.volume = value * value;
            if (value > 0.001f)
            {
                lastAudibleVolume = value;
            }
            if (volumeValue != null)
            {
                volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
            if (muteLabel != null)
            {
                muteLabel.text = value <= 0.001f ? "음소거 해제" : "음소거";
            }
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }

        private static void SaveVolume(float value)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }

        private void ToggleMute()
        {
            volumeSlider.value = volumeSlider.value <= 0.001f
                ? Mathf.Max(0.1f, lastAudibleVolume)
                : 0f;
        }

        private void ShowSoundMenu()
        {
            mainMenu.SetActive(false);
            soundMenu.SetActive(true);
            Select(volumeSlider);
        }

        private void ShowMainMenu()
        {
            SaveVolume(volumeSlider.value);
            soundMenu.SetActive(false);
            mainMenu.SetActive(true);
            Select(soundButton);
        }

        private void StartGame()
        {
            if (isStarting)
            {
                return;
            }
            isStarting = true;
            SaveVolume(volumeSlider.value);
            startButton.interactable = false;
            soundButton.interactable = false;
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
