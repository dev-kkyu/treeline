using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 플레이 중 짧은 조작법이나 상황 안내를 HUD 상단에 표시한다.
    /// PlayerHud가 자기 Canvas에 붙이므로 장마다 별도 프리팹을 배치하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudGuideText : MonoBehaviour
    {
        private const float FadeSeconds = 0.18f;
        private const float HiddenOffset = 14f;

        private static HudGuideText instance;
        private static string pendingMessage;
        private static float pendingSeconds;
        private static Sprite pendingIcon;
        private static string pendingCategory;

        private CanvasGroup group;
        private RectTransform panelRect;
        private Image controlIcon;
        private TextMeshProUGUI categoryText;
        private TextMeshProUGUI messageText;
        private float targetAlpha;
        private float visibleUntil = float.PositiveInfinity;

        /// <summary>PlayerHud의 Canvas에 가이드 UI를 한 번만 만든다.</summary>
        public static HudGuideText AttachTo(Transform hudRoot)
        {
            HudGuideText guide = hudRoot.GetComponent<HudGuideText>();
            if (guide == null)
            {
                guide = hudRoot.gameObject.AddComponent<HudGuideText>();
            }

            return guide;
        }

        /// <summary>
        /// 안내를 표시한다. seconds가 0보다 크면 해당 시간이 지난 뒤 자동으로 닫힌다.
        /// 0 이하면 Hide를 호출할 때까지 유지한다.
        /// </summary>
        public static void Show(string message, float seconds = 4f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            ShowStyled(null, "현장 안내", message, seconds);
        }

        /// <summary>조작키 Sprite와 함께 입력 안내를 표시한다.</summary>
        public static void ShowControl(Sprite icon, string message)
        {
            ShowStyled(icon, "조작 안내", message, 0f);
        }

        /// <summary>Ch1의 다음 목적지를 짧은 텍스트로 안내한다.</summary>
        public static void ShowObjective(string message, float seconds = 7f)
        {
            ShowStyled(null, "이동 안내", message, seconds);
        }

        /// <summary>현재 안내를 페이드 아웃한다.</summary>
        public static void Hide()
        {
            pendingMessage = null;
            pendingIcon = null;
            pendingCategory = null;
            if (instance != null)
            {
                instance.targetAlpha = 0f;
                instance.visibleUntil = float.PositiveInfinity;
            }
        }

        /// <summary>컷신이나 사망 연출에서 안내를 즉시 치운다.</summary>
        public static void HideImmediate()
        {
            pendingMessage = null;
            pendingIcon = null;
            pendingCategory = null;
            if (instance == null || instance.group == null)
            {
                return;
            }

            instance.targetAlpha = 0f;
            instance.group.alpha = 0f;
            instance.SetPanelOffset(HiddenOffset);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            Build();

            if (!string.IsNullOrWhiteSpace(pendingMessage))
            {
                string message = pendingMessage;
                float seconds = pendingSeconds;
                Sprite icon = pendingIcon;
                string category = pendingCategory;
                pendingMessage = null;
                pendingIcon = null;
                pendingCategory = null;
                ShowInternal(icon, category, message, seconds);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (targetAlpha > 0f && Time.unscaledTime >= visibleUntil)
            {
                targetAlpha = 0f;
            }

            float step = FadeSeconds > 0f ? Time.unscaledDeltaTime / FadeSeconds : 1f;
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
            SetPanelOffset(Mathf.Lerp(HiddenOffset, 0f, group.alpha));
        }

        private static void ShowStyled(Sprite icon, string category, string message, float seconds)
        {
            if (instance == null)
            {
                pendingIcon = icon;
                pendingCategory = category;
                pendingMessage = message;
                pendingSeconds = seconds;
                return;
            }

            instance.ShowInternal(icon, category, message, seconds);
        }

        private void ShowInternal(Sprite icon, string category, string message, float seconds)
        {
            bool hasIcon = icon != null;
            controlIcon.enabled = hasIcon;
            controlIcon.sprite = icon;
            categoryText.enabled = !hasIcon;
            categoryText.text = category;
            messageText.text = message;
            visibleUntil = seconds > 0f
                ? Time.unscaledTime + seconds
                : float.PositiveInfinity;
            targetAlpha = 1f;
        }

        private void Build()
        {
            GameObject root = NewUiObject("GuideText", transform);
            panelRect = root.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(900f, 106f);
            panelRect.anchoredPosition = new Vector2(0f, -62f);

            group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image shadow = NewImage("Shadow", root.transform, new Color(0f, 0f, 0f, 0.38f));
            Stretch(shadow.rectTransform, new Vector2(9f, -9f), new Vector2(9f, -9f));

            Image panel = NewImage("Panel", root.transform, new Color(0.018f, 0.027f, 0.032f, 0.9f));
            Stretch(panel.rectTransform, Vector2.zero, Vector2.zero);

            Image topRule = NewImage("ColdRule", panel.transform, new Color(0.48f, 0.58f, 0.61f, 0.72f));
            AnchorLine(topRule.rectTransform, true, 3f);

            Image warningRule = NewImage("WarningRule", panel.transform, new Color(0.53f, 0.16f, 0.14f, 0.92f));
            AnchorLine(warningRule.rectTransform, false, 5f);

            controlIcon = NewImage("ControlIcon", panel.transform, Color.white);
            controlIcon.preserveAspect = true;
            controlIcon.enabled = false;
            SetRect(controlIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(92f, 0f), new Vector2(128f, 78f));

            categoryText = NewText("Category", panel.transform, 15f,
                new Color(0.62f, 0.7f, 0.71f, 0.9f));
            categoryText.text = "현장 안내";
            categoryText.fontStyle = FontStyles.Bold;
            categoryText.characterSpacing = 3f;
            SetRect(categoryText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(29f, 0f), new Vector2(138f, -20f));

            Image divider = NewImage("Divider", panel.transform, new Color(0.37f, 0.43f, 0.44f, 0.48f));
            SetRect(divider.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(162f, 0f), new Vector2(2f, 48f));

            messageText = NewText("Message", panel.transform, 26f, new Color(0.88f, 0.9f, 0.87f, 1f));
            messageText.alignment = TextAlignmentOptions.MidlineLeft;
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(messageText.rectTransform, new Vector2(186f, 12f), new Vector2(28f, 12f));

            SetPanelOffset(HiddenOffset);
        }

        private void SetPanelOffset(float offset)
        {
            if (panelRect != null)
            {
                panelRect.anchoredPosition = new Vector2(0f, -62f - offset);
            }
        }

        private static GameObject NewUiObject(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image NewImage(string objectName, Transform parent, Color color)
        {
            Image image = NewUiObject(objectName, parent).AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI NewText(string objectName, Transform parent, float size, Color color)
        {
            TextMeshProUGUI text = NewUiObject(objectName, parent).AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 insetMin, Vector2 insetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = insetMin;
            rect.offsetMax = -insetMax;
        }

        private static void AnchorLine(RectTransform rect, bool top, float thickness)
        {
            float anchor = top ? 1f : 0f;
            SetRect(rect, new Vector2(0f, anchor), new Vector2(1f, anchor),
                new Vector2(0.5f, anchor), Vector2.zero, new Vector2(0f, thickness));
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
    }
}
