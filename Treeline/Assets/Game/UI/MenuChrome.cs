using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 메뉴 화면의 부품을 찍어내는 틀. 타이틀 · 일시정지 · 엔딩이 함께 쓴다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 세 화면이 같은 패널 · 같은 버튼 · 같은 손잡이를 쓴다. 각자 짓게 두면 색과
    /// 여백이 조금씩 갈라지고, 버튼 반응 하나를 고칠 때 세 파일을 고쳐야 한다.
    /// 실제로 그렇게 갈라져 있었고, 일시정지가 세 번째가 되는 자리에서 모았다.
    /// </para>
    /// <para>
    /// 화면마다 다른 것 — 무슨 글이 어디 놓이는지 — 은 여기 없다. 이 틀은
    /// <b>부품만</b> 만들고 배치는 부르는 쪽이 정한다.
    /// </para>
    /// </remarks>
    public sealed class MenuChrome
    {
        public static readonly Color Frost = new Color32(221, 228, 226, 255);
        public static readonly Color Muted = new Color32(140, 151, 153, 255);
        public static readonly Color BruisedRed = new Color32(126, 62, 52, 255);

        /// <summary>레일 하나의 크기. 세 화면이 같아야 자리가 안 튄다.</summary>
        public static readonly Vector2 RailSize = new Vector2(650f, 880f);

        private readonly MenuTheme theme;
        private readonly AudioSource speaker;

        private MenuChrome(MenuTheme theme, AudioSource speaker)
        {
            this.theme = theme;
            this.speaker = speaker;
        }

        /// <summary>틀을 만들고 소리 낼 자리를 <paramref name="owner"/> 밑에 붙인다.</summary>
        public static MenuChrome Create(Transform owner)
        {
            MenuTheme theme = MenuTheme.Load();

            var go = new GameObject("MenuAudio");
            go.transform.SetParent(owner, false);
            var speaker = go.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            speaker.loop = false;

            // 일시정지는 AudioListener 를 멈춘다. 그러면 메뉴 소리까지 같이 죽어서
            // 버튼을 눌러도 아무 반응이 없는 화면이 된다.
            speaker.ignoreListenerPause = true;

            return new MenuChrome(theme, speaker);
        }

        public void PlayClick()
        {
            if (speaker != null && theme != null && theme.ClickClip != null)
            {
                speaker.pitch = 1f;
                speaker.PlayOneShot(theme.ClickClip, 0.8f);
            }
        }

        // ---- 패널 -------------------------------------------------------------

        /// <summary>왼쪽 레일과 그 안의 머리글까지 세운다. 세 화면의 골격이 같다.</summary>
        public MenuPanel BuildPanel(Transform canvasRoot, string rootName, float veilAlpha,
            string status, string title, float titleSize, string subtitle)
        {
            GameObject root = CreateUiObject(rootName, canvasRoot);
            Stretch(root.GetComponent<RectTransform>());
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Image veil = CreateImage("ContrastVeil", root.transform, null,
                new Color(0.015f, 0.027f, 0.035f, veilAlpha));
            Stretch(veil.rectTransform);
            veil.raycastTarget = false;

            GameObject rail = CreateUiObject("LeftMenuRail", root.transform);
            SetRect(rail.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(72f, 0f), RailSize);

            Image frame = CreateImage("RelayFrame", rail.transform, theme != null ? theme.PanelFrame : null,
                Color.white);
            Stretch(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.82f, 0.86f, 0.86f, 0.94f);

            Image shade = CreateImage("InnerShade", frame.transform, null,
                new Color(0.018f, 0.031f, 0.039f, 0.9f));
            Stretch(shade.rectTransform, 48f, 54f, 48f, 54f);
            shade.raycastTarget = false;

            TMP_Text statusText = CreateText("SignalStatus", frame.transform, status, 16f, Muted,
                TextAlignmentOptions.Left, new Vector2(78f, -74f), new Vector2(470f, 28f), false);
            TMP_Text titleText = CreateText("ScreenTitle", frame.transform, title, titleSize, Frost,
                TextAlignmentOptions.Left, new Vector2(78f, -118f), new Vector2(480f, 90f), true);
            TMP_Text subtitleText = CreateText("Subtitle", frame.transform, subtitle, 20f, Muted,
                TextAlignmentOptions.Left, new Vector2(80f, -214f), new Vector2(475f, 42f), false);
            subtitleText.characterSpacing = 1.5f;

            CreateDivider("TitleDivider", frame.transform, new Vector2(80f, -270f), 470f, 1f);

            return new MenuPanel(root, group, (RectTransform)rail.transform, frame.transform,
                titleText, statusText, subtitleText);
        }

        /// <summary>겹쳐 두고 갈아 끼울 한 쪽. 처음에는 꺼져 있다.</summary>
        public GameObject CreatePage(string name, Transform parent)
        {
            GameObject page = CreateUiObject(name, parent);
            Stretch(page.GetComponent<RectTransform>());
            page.AddComponent<CanvasGroup>();
            page.SetActive(false);
            return page;
        }

        public Image CreateDivider(string name, Transform parent, Vector2 position, float width, float alpha)
        {
            Image rule = CreateImage(name, parent, theme != null ? theme.Divider : null, Color.white);
            SetTopLeft(rule.rectTransform, position, new Vector2(width, 12f));
            rule.type = Image.Type.Sliced;
            rule.color = new Color(1f, 1f, 1f, alpha);
            rule.raycastTarget = false;
            return rule;
        }

        // ---- 부품 -------------------------------------------------------------

        public Button CreateMenuButton(Transform parent, string name, string label, Vector2 position,
            float height, UnityEngine.Events.UnityAction action, float width = 470f)
        {
            Image image = CreateImage(name, parent, theme != null ? theme.ButtonFrame : null, Color.white);
            SetTopLeft(image.rectTransform, position, new Vector2(width, height));
            image.type = Image.Type.Sliced;

            var button = image.gameObject.AddComponent<Button>();
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

            Image mark = CreateImage("FocusMark", image.transform, null, BruisedRed);
            SetRect(mark.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(3f, -32f));
            mark.raycastTarget = false;

            const float labelLeft = 34f;
            TMP_Text text = CreateText("Label", image.transform, label, 25f, Frost,
                TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, true, false);
            Stretch(text.rectTransform, labelLeft, 8f, 24f, 8f);

            var feel = image.gameObject.AddComponent<MenuButtonFeel>();
            feel.Bind(button, mark, text, labelLeft, speaker, theme != null ? theme.ClickClip : null);
            return button;
        }

        public Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size,
            float min, float max)
        {
            GameObject root = CreateUiObject(name, parent);
            SetTopLeft(root.GetComponent<RectTransform>(), position, size);
            var slider = root.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.direction = Slider.Direction.LeftToRight;

            Image track = CreateImage("Track", root.transform, null, new Color(0.035f, 0.055f, 0.065f, 0.95f));
            SetRect(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 12f));
            track.raycastTarget = false;

            GameObject fillArea = CreateUiObject("FillArea", root.transform);
            SetRect(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 8f));
            Image fill = CreateImage("Fill", fillArea.transform, null, BruisedRed);
            Stretch(fill.rectTransform);

            GameObject handleArea = CreateUiObject("HandleArea", root.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), 9f, 0f, 9f, 0f);
            Image handle = CreateImage("Handle", handleArea.transform, null, Frost);
            SetRect(handle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 30f));

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
        }

        public TMP_Text CreateText(string name, Transform parent, string value, float size, Color color,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, bool bold,
            bool topLeft = true)
        {
            GameObject go = CreateUiObject(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = theme == null ? null : (bold ? theme.SemiBoldFont : theme.RegularFont);
            if (text.font == null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
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

        /// <summary>작고 붉은 구역 이름. 자간을 벌려 본문과 구분한다.</summary>
        public TMP_Text CreateSectionLabel(Transform parent, string name, string value, float y)
        {
            TMP_Text label = CreateText(name, parent, value, 13f, BruisedRed,
                TextAlignmentOptions.Left, new Vector2(84f, y), new Vector2(462f, 22f), true);
            label.characterSpacing = 6f;
            return label;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = CreateUiObject(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        public static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        // ---- 자리 잡기 ---------------------------------------------------------

        public static void ConfigureCanvas(Canvas canvas, CanvasScaler scaler)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // IMPORTANT: MatchWidthOrHeight 가 아니라 Expand 다. 레일이 650 x 880 짜리
            // 고정 패널이라 세로가 잘리면 버튼이 화면 밖으로 나간다. 폭 기준(0)이면
            // 2560x1080 에서 레일이 1173px 을 차지해 1080 화면을 넘고, 절반(0.5)도
            // 3440x1440 에서 1360/1440 이라 여유가 없다.
            // Expand = min(w/1920, h/1080) 이라 어떤 비율에서도 기준 화면이 통째로 들어간다.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        public static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        public static void SetTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetRect(rect, Vector2.up, Vector2.up, Vector2.up, position, size);
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f,
            float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>씬에 <see cref="EventSystem"/> 이 없으면 세운다. 있으면 손대지 않는다.</summary>
        /// <remarks>
        /// 장 씬에는 <see cref="EventSystem"/> 이 없다 — 여태 UI 로 누를 것이 없었다.
        /// 둘이 되면 유니티가 경고를 내고 입력이 한쪽으로만 가므로 먼저 확인한다.
        /// </remarks>
        public static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    /// <summary>레일 하나. <see cref="MenuChrome.BuildPanel"/> 이 돌려준다.</summary>
    public sealed class MenuPanel
    {
        public MenuPanel(GameObject root, CanvasGroup group, RectTransform rail, Transform frame,
            TMP_Text title, TMP_Text status, TMP_Text subtitle)
        {
            Root = root;
            Group = group;
            Rail = rail;
            Frame = frame;
            Title = title;
            Status = status;
            Subtitle = subtitle;
        }

        public GameObject Root { get; }
        public CanvasGroup Group { get; }

        /// <summary>패널만 밀어 넣고 싶을 때 쓴다. 배경 장막은 따라오지 않는다.</summary>
        public RectTransform Rail { get; }

        /// <summary>쪽을 붙이는 자리.</summary>
        public Transform Frame { get; }

        public TMP_Text Title { get; }
        public TMP_Text Status { get; }
        public TMP_Text Subtitle { get; }
    }
}
