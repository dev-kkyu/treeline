using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 무전 일지 · 근무표 같은 읽을거리를 화면에 띄운다.
    /// </summary>
    /// <remarks>
    /// <b>씬에 배치하지 않는다.</b> 처음 필요할 때 스스로 생긴다. 장마다 캔버스를 넣으면
    /// 병합할 수 없는 파일을 장 수만큼 건드리게 되고, 문구 하나 고칠 때마다 그 전부를
    /// 다시 열어야 한다. <see cref="GameFlow"/> 를 씬에 두지 않은 것과 같은 이유다.
    /// <para>
    /// 폰트는 <c>TMP_Settings.defaultFontAsset</c> 을 그대로 쓴다. 프로젝트 기본 폰트가
    /// 한글 폰트라 여기서 따로 참조를 들 필요가 없고, 폰트를 바꿀 때 고칠 곳이
    /// 프로젝트 설정 한 군데로 남는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ReadingPanel : MonoBehaviour
    {
        private static ReadingPanel instance;

        private CanvasGroup group;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private PlayerInputReader input;
        private int openedFrame = -1;

        /// <summary>지금 읽고 있는지. 읽는 동안에는 다른 읽을거리를 집을 수 없다.</summary>
        public static bool IsOpen => instance != null && instance.group.alpha > 0f;

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
            if (group.alpha <= 0f || Time.frameCount <= openedFrame)
            {
                // 연 프레임에는 닫지 않는다. 같은 키로 열고 닫으므로 한 번 누른 것이
                // 열기와 닫기 양쪽에 걸린다.
                return;
            }

            if (input != null && input.InteractPressed)
            {
                Close();
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
            group.alpha = 1f;
            openedFrame = Time.frameCount;
        }

        private void Close()
        {
            group.alpha = 0f;
        }

        /// <summary>
        /// 캔버스와 글자를 코드로 세운다. 프리팹을 두지 않는 이유는 씬과 같다 —
        /// 하나뿐인 것을 위해 병합 안 되는 파일을 늘리지 않는다.
        /// </summary>
        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // HUD 위에 그린다. 읽는 동안 체력 게이지가 글자를 덮으면 안 된다.
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var backdrop = NewChild("Backdrop", transform).AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.72f);
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one);

            var sheet = NewChild("Sheet", backdrop.transform).AddComponent<Image>();
            sheet.color = new Color(0.09f, 0.10f, 0.11f, 0.96f);
            var sheetRect = sheet.rectTransform;
            sheetRect.anchorMin = new Vector2(0.5f, 0.5f);
            sheetRect.anchorMax = new Vector2(0.5f, 0.5f);
            sheetRect.pivot = new Vector2(0.5f, 0.5f);
            sheetRect.sizeDelta = new Vector2(1000f, 620f);
            sheetRect.anchoredPosition = Vector2.zero;

            titleText = NewText("Title", sheet.transform, 46f, TextAlignmentOptions.TopLeft);
            Stretch(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            titleText.rectTransform.sizeDelta = new Vector2(-120f, 70f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -56f);

            bodyText = NewText("Body", sheet.transform, 32f, TextAlignmentOptions.TopLeft);
            Stretch(bodyText.rectTransform, Vector2.zero, Vector2.one);
            bodyText.rectTransform.offsetMin = new Vector2(60f, 96f);
            bodyText.rectTransform.offsetMax = new Vector2(-60f, -140f);
            bodyText.lineSpacing = 14f;

            var hint = NewText("Hint", sheet.transform, 26f, TextAlignmentOptions.Bottom);
            hint.text = "E  닫기";
            hint.color = new Color(1f, 1f, 1f, 0.45f);
            Stretch(hint.rectTransform, Vector2.zero, new Vector2(1f, 0f));
            hint.rectTransform.sizeDelta = new Vector2(-120f, 48f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        }

        private static GameObject NewChild(string childName, Transform parent)
        {
            var go = new GameObject(childName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewText(string childName, Transform parent, float size, TextAlignmentOptions align)
        {
            var text = NewChild(childName, parent).AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = align;
            text.color = new Color(0.92f, 0.92f, 0.90f);
            text.richText = false;
            return text;
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
