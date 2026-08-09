using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 마지막 장면을 가리지 않는 우측 엔딩 메뉴와 재시작·타이틀·음량 설정을 제공한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class EndingScreen : MonoBehaviour
    {
        private const string MasterVolumeKey = "Treeline.Audio.MasterVolume";
        private const float DefaultVolume = 0.8f;
        private const float FadeSeconds = 0.28f;

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
        private GameObject endingMenu;
        private GameObject soundMenu;
        private Button retryButton;
        private Button optionsButton;
        private Button titleButton;
        private Button muteButton;
        private Button backButton;
        private TMP_Text muteLabel;
        private TMP_Text volumeValue;
        private Slider volumeSlider;
        private float lastAudibleVolume = DefaultVolume;
        private bool isNavigating;

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
            Select(retryButton);
            StartCoroutine(FadeCanvas(0f, 1f, 0.7f));
        }

        private void Update()
        {
            if (soundMenu.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ShowEndingMenu();
            }
        }

        private void OnDestroy()
        {
            if (volumeSlider != null)
            {
                SaveVolume(volumeSlider.value);
            }
        }

        private void ConfigureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // TitleScreen 과 같은 이유로 Expand 다. 레일이 고정 크기 패널이라
            // 세로가 잘리면 버튼이 화면 밖으로 나간다. 폭 기준이면 2560x1080 에서
            // 1173px 을 차지해 1080 을 넘고, 절반도 3440x1440 에서 여유가 없다.
            // Expand = min(w/1920, h/1080) 이라 어떤 비율에서도 통째로 들어간다.
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
            GameObject root = CreateUiObject("EndingUI", transform);
            Stretch(root.GetComponent<RectTransform>());
            screenGroup = root.AddComponent<CanvasGroup>();
            screenGroup.alpha = 0f;

            Image veil = CreateImage("EndingContrastVeil", root.transform, null,
                new Color(0.012f, 0.022f, 0.028f, 0.3f));
            Stretch(veil.rectTransform);
            veil.raycastTarget = false;

            // 왼쪽에 세운다. 오른쪽은 배경의 삭도가 산으로 뻗는 자리라 겹치면
            // 「케이블은 계속 아래로 움직인다」를 그림이 가려 버린다.
            // TitleScreen 도 왼쪽이므로 두 화면의 메뉴 위치가 같아지는 이점도 있다.
            GameObject rail = CreateUiObject("LeftEndingRail", root.transform);
            SetRect(rail.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(650f, 880f));

            Image frame = CreateImage("RelayFrame", rail.transform, panelFrame, Color.white);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.82f, 0.86f, 0.86f, 0.94f);

            Image shade = CreateImage("InnerShade", frame.transform, null,
                new Color(0.018f, 0.031f, 0.039f, 0.9f));
            Stretch(shade.rectTransform, 48f, 54f, 48f, 54f);
            shade.raycastTarget = false;

            CreateText("SignalStatus", frame.transform, "삭도 운행 기록  /  수신 종료", 16f, muted,
                TextAlignmentOptions.Left, new Vector2(78f, -74f), new Vector2(470f, 28f), false);
            CreateText("EndingTitle", frame.transform, "신호 종료", 56f, frost,
                TextAlignmentOptions.Left, new Vector2(78f, -128f), new Vector2(480f, 72f), true);
            CreateText("EndingSubtitle", frame.transform,
                "케이블은 계속 아래로 움직인다", 20f, muted,
                TextAlignmentOptions.Left, new Vector2(80f, -214f), new Vector2(475f, 42f), false);

            Image titleDivider = CreateImage("EndingDivider", frame.transform, divider, Color.white);
            SetTopLeft(titleDivider.rectTransform, new Vector2(80f, -270f), new Vector2(470f, 12f));
            titleDivider.type = Image.Type.Sliced;
            titleDivider.raycastTarget = false;

            endingMenu = CreateUiObject("EndingMenu", frame.transform);
            Stretch(endingMenu.GetComponent<RectTransform>());
            retryButton = CreateMenuButton(endingMenu.transform, "RetryButton", "다시 하기",
                new Vector2(80f, -344f), RetryGame);
            optionsButton = CreateMenuButton(endingMenu.transform, "OptionsButton", "사운드 설정",
                new Vector2(80f, -458f), ShowSoundMenu);
            titleButton = CreateMenuButton(endingMenu.transform, "TitleButton", "타이틀로",
                new Vector2(80f, -572f), ReturnToTitle);

            CreateText("EndingHint", endingMenu.transform, "처음부터 다시 내려갑니다", 14f, muted,
                TextAlignmentOptions.Left, new Vector2(84f, -674f), new Vector2(450f, 28f), false);
            CreateText("BuildLabel", endingMenu.transform, "TREELINE  ·  END OF TRANSMISSION", 13f,
                new Color(muted.r, muted.g, muted.b, 0.68f), TextAlignmentOptions.Left,
                new Vector2(84f, -746f), new Vector2(450f, 24f), false);

            soundMenu = CreateUiObject("EndingSoundMenu", frame.transform);
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
            backButton = CreateMenuButton(parent, "BackButton", "뒤로", new Vector2(80f, -660f), ShowEndingMenu);
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

            Image track = CreateImage("Track", root.transform, null,
                new Color(0.035f, 0.055f, 0.065f, 0.95f));
            SetRect(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 12f));
            track.raycastTarget = false;

            GameObject fillArea = CreateUiObject("FillArea", root.transform);
            SetRect(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 8f));
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
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.86f, 0.86f, 0.9f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.62f, 0.7f, 0.72f, 1f);
            colors.selectedColor = new Color(0.92f, 0.95f, 0.94f, 1f);
            colors.disabledColor = new Color(0.32f, 0.35f, 0.36f, 0.6f);
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
            text.textWrappingMode = TextWrappingModes.NoWrap;
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
            volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
            muteLabel.text = value <= 0.001f ? "음소거 해제" : "음소거";
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
            endingMenu.SetActive(false);
            soundMenu.SetActive(true);
            Select(volumeSlider);
        }

        private void ShowEndingMenu()
        {
            SaveVolume(volumeSlider.value);
            soundMenu.SetActive(false);
            endingMenu.SetActive(true);
            Select(optionsButton);
        }

        private void RetryGame()
        {
            if (!BeginNavigation())
            {
                return;
            }
            StartCoroutine(NavigateToGame());
        }

        private void ReturnToTitle()
        {
            if (!BeginNavigation())
            {
                return;
            }
            StartCoroutine(NavigateToTitle());
        }

        private bool BeginNavigation()
        {
            if (isNavigating)
            {
                return false;
            }
            isNavigating = true;
            SaveVolume(volumeSlider.value);
            retryButton.interactable = false;
            optionsButton.interactable = false;
            titleButton.interactable = false;
            return true;
        }

        private IEnumerator NavigateToGame()
        {
            yield return FadeCanvas(1f, 0f, FadeSeconds);
            GameFlow.Instance.StartNewGame();
        }

        private IEnumerator NavigateToTitle()
        {
            yield return FadeCanvas(1f, 0f, FadeSeconds);
            GameFlow.Instance.ReturnToTitle();
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
