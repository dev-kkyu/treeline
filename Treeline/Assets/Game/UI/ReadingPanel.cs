using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// Ch1의 근무표·무전 일지 등 읽을거리를 낡은 중계소 기록지 UI로 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReadingPanel : MonoBehaviour
    {
        private const float FadeSeconds = 0.16f;

        private static ReadingPanel instance;

        private CanvasGroup group;
        private RectTransform documentRect;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private ScrollRect scrollView;
        private PlayerInputReader input;
        private Coroutine transition;
        private int openedFrame = -1;
        private bool isOpen;

        /// <summary>지금 읽고 있는지. 읽는 동안에는 다른 읽을거리를 집지 않는다.</summary>
        public static bool IsOpen => instance != null && instance.isOpen;

        /// <summary>읽을거리를 띄운다. <see cref="Readable"/> 이 부른다.</summary>
        public static void Show(string title, string body)
        {
            Ensure();
            instance.Open(title, body);
        }

        /// <summary>닫는다. 장을 옮기거나 죽을 때 바깥에서 부를 수 있다.</summary>
        public static void Hide()
        {
            if (instance != null)
            {
                instance.Close();
            }
        }

        private static void Ensure()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("[ReadingPanel]");
            instance = go.AddComponent<ReadingPanel>();
            instance.Build();
        }

        private void Update()
        {
            if (!isOpen || Time.frameCount <= openedFrame)
            {
                return;
            }

            bool closePressed = input != null && input.InteractPressed;
            closePressed |= Keyboard.current?.escapeKey.wasPressedThisFrame == true;
            if (closePressed)
            {
                Close();
                return;
            }

            float scrollDelta = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                scrollView.verticalNormalizedPosition = Mathf.Clamp01(
                    scrollView.verticalNormalizedPosition + scrollDelta * 0.0015f);
            }

            if (Keyboard.current?.pageDownKey.wasPressedThisFrame == true)
            {
                scrollView.verticalNormalizedPosition = Mathf.Clamp01(
                    scrollView.verticalNormalizedPosition - 0.2f);
            }
            else if (Keyboard.current?.pageUpKey.wasPressedThisFrame == true)
            {
                scrollView.verticalNormalizedPosition = Mathf.Clamp01(
                    scrollView.verticalNormalizedPosition + 0.2f);
            }
        }

        private void Open(string title, string body)
        {
            if (input == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            }

            titleText.text = title;
            bodyText.text = body;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(bodyText.rectTransform);
            scrollView.verticalNormalizedPosition = 1f;

            isOpen = true;
            openedFrame = Time.frameCount;
            group.blocksRaycasts = true;
            if (transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(AnimatePanel(true));
        }

        private void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            group.blocksRaycasts = false;
            if (transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(AnimatePanel(false));
        }

        private IEnumerator AnimatePanel(bool opening)
        {
            float fromAlpha = group.alpha;
            float toAlpha = opening ? 1f : 0f;
            float fromScale = documentRect.localScale.x;
            float toScale = opening ? 1f : 0.975f;
            float elapsed = 0f;

            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FadeSeconds);
                t = 1f - (1f - t) * (1f - t);
                group.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                float scale = Mathf.Lerp(fromScale, toScale, t);
                documentRect.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            group.alpha = toAlpha;
            documentRect.localScale = new Vector3(toScale, toScale, 1f);
            transition = null;
        }

        /// <summary>
        /// 장별 프리팹 없이 공용 기록지 UI를 한 번만 만든다.
        /// </summary>
        private void Build()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image backdrop = NewImage("Backdrop", transform, new Color(0.008f, 0.014f, 0.018f, 0.82f));
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one);
            backdrop.raycastTarget = false;

            Image shadow = NewImage("DocumentShadow", backdrop.transform, new Color(0f, 0f, 0f, 0.62f));
            Center(shadow.rectTransform, new Vector2(1130f, 790f), new Vector2(18f, -20f));

            Image board = NewImage("FieldClipboard", backdrop.transform, new Color32(31, 37, 38, 255));
            documentRect = board.rectTransform;
            Center(documentRect, new Vector2(1130f, 790f), Vector2.zero);
            AddBorder(board.transform, new Color32(89, 98, 97, 255), 5f);

            Image backPaper = NewImage("BackPaper", board.transform, new Color32(126, 120, 103, 255));
            Center(backPaper.rectTransform, new Vector2(992f, 684f), new Vector2(-8f, -7f));
            backPaper.rectTransform.localEulerAngles = new Vector3(0f, 0f, -0.8f);
            backPaper.raycastTarget = false;

            Image paper = NewImage("RecordSheet", board.transform, new Color32(188, 181, 158, 255));
            Center(paper.rectTransform, new Vector2(992f, 684f), new Vector2(6f, -2f));
            AddBorder(paper.transform, new Color32(82, 78, 69, 255), 2f);

            Image headerBand = NewImage("HeaderBand", paper.transform, new Color32(53, 58, 57, 255));
            SetRect(headerBand.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 72f));
            headerBand.raycastTarget = false;

            TextMeshProUGUI category = NewText("Category", headerBand.transform, 15f,
                TextAlignmentOptions.Left, new Color32(201, 205, 198, 230));
            category.text = "TREELINE RELAY STATION  /  FIELD RECORD";
            category.characterSpacing = 2f;
            Stretch(category.rectTransform, Vector2.zero, Vector2.one);
            category.rectTransform.offsetMin = new Vector2(32f, 0f);
            category.rectTransform.offsetMax = new Vector2(-32f, 0f);

            Image marginRule = NewImage("MarginRule", paper.transform, new Color32(126, 62, 52, 175));
            SetRect(marginRule.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(92f, -20f), new Vector2(3f, -112f));
            marginRule.raycastTarget = false;

            for (int line = 0; line < 12; line++)
            {
                Image rule = NewImage($"Rule_{line:00}", paper.transform, new Color32(79, 88, 87, 42));
                SetRect(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -184f - line * 39f), new Vector2(-76f, 1f));
                rule.raycastTarget = false;
            }

            titleText = NewText("Title", paper.transform, 38f, TextAlignmentOptions.Left,
                new Color32(38, 39, 35, 255));
            titleText.fontStyle = FontStyles.Bold;
            SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(28f, -92f), new Vector2(-150f, 72f));

            TextMeshProUGUI stamp = NewText("RecordStamp", paper.transform, 14f,
                TextAlignmentOptions.Center, new Color32(116, 51, 43, 200));
            stamp.text = "기록 보존";
            stamp.fontStyle = FontStyles.Bold;
            SetRect(stamp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-34f, -96f), new Vector2(104f, 38f));
            AddBorder(stamp.transform, new Color32(116, 51, 43, 160), 2f);

            GameObject viewport = NewChild("BodyViewport", paper.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect, Vector2.zero, Vector2.one);
            viewportRect.offsetMin = new Vector2(118f, 94f);
            viewportRect.offsetMax = new Vector2(-58f, -176f);
            viewport.AddComponent<RectMask2D>();
            scrollView = viewport.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;
            scrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.inertia = true;
            scrollView.decelerationRate = 0.12f;
            scrollView.scrollSensitivity = 42f;

            bodyText = NewText("Body", viewport.transform, 25f, TextAlignmentOptions.TopLeft,
                new Color32(47, 46, 40, 255));
            bodyText.lineSpacing = 12f;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Overflow;
            bodyText.rectTransform.anchorMin = new Vector2(0f, 1f);
            bodyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            bodyText.rectTransform.pivot = new Vector2(0.5f, 1f);
            bodyText.rectTransform.anchoredPosition = Vector2.zero;
            bodyText.rectTransform.sizeDelta = Vector2.zero;
            ContentSizeFitter fitter = bodyText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollView.content = bodyText.rectTransform;

            TextMeshProUGUI scrollHint = NewText("ScrollHint", paper.transform, 13f,
                TextAlignmentOptions.Right, new Color32(62, 61, 54, 175));
            scrollHint.text = "MOUSE WHEEL  /  PAGE UP · DOWN";
            SetRect(scrollHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-60f, 42f), new Vector2(360f, 28f));

            TextMeshProUGUI closeHint = NewText("CloseHint", board.transform, 17f,
                TextAlignmentOptions.Center, new Color32(194, 201, 198, 200));
            closeHint.text = "[ E / ESC ]  기록 닫기";
            SetRect(closeHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(430f, 36f));

            Image clip = NewImage("MetalClip", board.transform, new Color32(96, 103, 102, 255));
            SetRect(clip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 19f), new Vector2(210f, 42f));
            AddBorder(clip.transform, new Color32(39, 44, 45, 255), 3f);

            group.alpha = 0f;
            documentRect.localScale = new Vector3(0.975f, 0.975f, 1f);
        }

        private static void AddBorder(Transform parent, Color color, float thickness)
        {
            Image top = NewImage("BorderTop", parent, color);
            SetRect(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, thickness));
            Image bottom = NewImage("BorderBottom", parent, color);
            SetRect(bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, thickness));
            Image left = NewImage("BorderLeft", parent, color);
            SetRect(left.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(thickness, 0f));
            Image right = NewImage("BorderRight", parent, color);
            SetRect(right.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), Vector2.zero, new Vector2(thickness, 0f));
            top.raycastTarget = bottom.raycastTarget = left.raycastTarget = right.raycastTarget = false;
        }

        private static GameObject NewChild(string childName, Transform parent)
        {
            var go = new GameObject(childName, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image NewImage(string childName, Transform parent, Color color)
        {
            Image image = NewChild(childName, parent).AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI NewText(string childName, Transform parent, float size,
            TextAlignmentOptions align, Color color)
        {
            TextMeshProUGUI text = NewChild(childName, parent).AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = align;
            text.color = color;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), position, size);
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

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
