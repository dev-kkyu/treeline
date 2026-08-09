using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 챕터의 전체 작업 목록을 왼쪽 위 HUD에 유지한다. 완료한 행은 지우지 않고
    /// 취소선을 긋고, 현재 행만 강조한다. 조작 안내는 같은 패널을 잠시 빌려 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudGuideText : MonoBehaviour
    {
        private const string DatabasePath = "ChapterObjectives";
        private const float FadeSeconds = 0.18f;
        private const float HiddenOffset = 18f;
        private const float RowHeight = 46f;

        [Serializable]
        private sealed class ObjectiveDatabase
        {
            public ChapterDefinition[] chapters = Array.Empty<ChapterDefinition>();
        }

        [Serializable]
        private sealed class ChapterDefinition
        {
            public string scene = string.Empty;
            public string chapter = string.Empty;
            public string title = string.Empty;
            public string[] objectives = Array.Empty<string>();
        }

        private sealed class ObjectiveRow
        {
            public Image background;
            public Image marker;
            public Image strike;
            public TextMeshProUGUI number;
            public TextMeshProUGUI text;
        }

        private static HudGuideText instance;
        private static string pendingMessage;
        private static float pendingSeconds;
        private static Sprite pendingIcon;
        private static string pendingCategory;

        private readonly List<ObjectiveRow> objectiveRows = new List<ObjectiveRow>();

        private CanvasGroup group;
        private RectTransform panelRect;
        private GameObject objectiveRoot;
        private GameObject temporaryRoot;
        private Image controlIcon;
        private TextMeshProUGUI chapterText;
        private TextMeshProUGUI progressText;
        private TextMeshProUGUI temporaryCategory;
        private TextMeshProUGUI temporaryMessage;

        private ChapterDefinition definition;
        private int objectiveIndex = -1;
        private float targetAlpha;
        private float visibleUntil = float.PositiveInfinity;
        private bool showingObjectives;

        public static HudGuideText AttachTo(Transform hudRoot)
        {
            HudGuideText guide = hudRoot.GetComponent<HudGuideText>();
            if (guide == null)
            {
                guide = hudRoot.gameObject.AddComponent<HudGuideText>();
            }

            return guide;
        }

        public static void Show(string message, float seconds = 4f)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Hide();
                return;
            }

            ShowTemporary(null, "현장 안내", message, seconds);
        }

        public static void ShowControl(Sprite icon, string message)
        {
            ShowTemporary(icon, "조작 안내", message, 0f);
        }

        /// <summary>데이터와 같은 문구라면 그 행을 현재 목표로 선택한다.</summary>
        public static void ShowObjective(string message, float seconds = 0f)
        {
            if (instance == null)
            {
                pendingCategory = "현재 목표";
                pendingMessage = message;
                pendingSeconds = seconds;
                pendingIcon = null;
                return;
            }

            instance.SelectObjective(message);
        }

        public static void ShowObjectiveStep(int index)
        {
            if (instance != null)
            {
                instance.SelectObjective(index, true);
            }
        }

        /// <summary>임시 조작 안내를 가리지 않고 목표 진행 상태만 뒤에서 갱신한다.</summary>
        public static void TrackObjectiveStep(int index)
        {
            if (instance != null)
            {
                instance.SelectObjective(index, false);
            }
        }

        /// <summary>현재 행을 완료 처리하고 다음 행을 강조한다. 완료 행은 목록에 남는다.</summary>
        public static void AdvanceObjective()
        {
            if (instance != null)
            {
                instance.AdvanceInternal();
            }
        }

        /// <summary>임시 안내를 닫고 전체 목표 목록으로 돌아간다.</summary>
        public static void Hide()
        {
            pendingMessage = null;
            pendingIcon = null;
            pendingCategory = null;
            if (instance != null)
            {
                instance.RestoreObjectivesOrHide();
            }
        }

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
            BuildShell();
            LoadChapter();
            BuildObjectiveRows();

            if (!string.IsNullOrWhiteSpace(pendingMessage))
            {
                string category = pendingCategory;
                string message = pendingMessage;
                float seconds = pendingSeconds;
                Sprite icon = pendingIcon;
                pendingCategory = null;
                pendingMessage = null;
                pendingIcon = null;

                if (category == "현재 목표")
                {
                    SelectObjective(message);
                }
                else
                {
                    ShowTemporaryInternal(icon, category, message, seconds);
                }
            }
            else
            {
                RestoreObjectivesOrHide();
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
            if (!showingObjectives && targetAlpha > 0f && Time.unscaledTime >= visibleUntil)
            {
                RestoreObjectivesOrHide();
            }

            float step = FadeSeconds > 0f ? Time.unscaledDeltaTime / FadeSeconds : 1f;
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, step);
            SetPanelOffset(Mathf.Lerp(HiddenOffset, 0f, group.alpha));
        }

        private static void ShowTemporary(Sprite icon, string category, string message, float seconds)
        {
            if (instance == null)
            {
                pendingIcon = icon;
                pendingCategory = category;
                pendingMessage = message;
                pendingSeconds = seconds;
                return;
            }

            instance.ShowTemporaryInternal(icon, category, message, seconds);
        }

        private void ShowTemporaryInternal(Sprite icon, string category, string message, float seconds)
        {
            showingObjectives = false;
            objectiveRoot.SetActive(false);
            temporaryRoot.SetActive(true);
            controlIcon.enabled = icon != null;
            controlIcon.sprite = icon;
            temporaryCategory.text = category;
            temporaryMessage.text = message;
            LayoutTemporary(icon != null);
            progressText.text = icon != null ? "CONTROL" : "NOTICE";
            visibleUntil = seconds > 0f ? Time.unscaledTime + seconds : float.PositiveInfinity;
            targetAlpha = 1f;
        }

        private void LoadChapter()
        {
            TextAsset json = Resources.Load<TextAsset>(DatabasePath);
            if (json == null)
            {
                Debug.LogWarning($"[{nameof(HudGuideText)}] Resources/{DatabasePath}.json을 찾지 못했다.", this);
                return;
            }

            ObjectiveDatabase database = JsonUtility.FromJson<ObjectiveDatabase>(json.text);
            string sceneName = SceneManager.GetActiveScene().name;
            if (database?.chapters != null)
            {
                foreach (ChapterDefinition chapter in database.chapters)
                {
                    if (chapter != null && string.Equals(chapter.scene, sceneName, StringComparison.Ordinal))
                    {
                        definition = chapter;
                        break;
                    }
                }
            }

            chapterText.text = definition != null
                ? $"{definition.chapter}  ·  {definition.title}"
                : sceneName.ToUpperInvariant();

            if (definition?.objectives != null && definition.objectives.Length > 0)
            {
                objectiveIndex = 0;
                ResizePanel(definition.objectives.Length);
            }
        }

        private void BuildObjectiveRows()
        {
            if (definition?.objectives == null)
            {
                return;
            }

            for (int i = 0; i < definition.objectives.Length; i++)
            {
                GameObject rowObject = NewUiObject($"Objective_{i + 1:00}", objectiveRoot.transform);
                RectTransform rowRect = rowObject.GetComponent<RectTransform>();
                SetRect(rowRect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(48f, -82f - i * RowHeight), new Vector2(524f, 40f));

                ObjectiveRow row = new ObjectiveRow();
                row.background = rowObject.AddComponent<Image>();
                row.background.raycastTarget = false;

                row.number = NewText("Number", rowObject.transform, 12f, Color.white);
                row.number.text = $"{i + 1:00}";
                row.number.fontStyle = FontStyles.Bold;
                row.number.alignment = TextAlignmentOptions.Center;
                SetRect(row.number.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(32f, 28f));

                row.marker = NewImage("Marker", rowObject.transform, Color.white);
                SetRect(row.marker.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(43f, 0f), new Vector2(4f, 24f));

                row.text = NewText("Task", rowObject.transform, 17f, Color.white);
                row.text.text = definition.objectives[i];
                row.text.alignment = TextAlignmentOptions.MidlineLeft;
                row.text.textWrappingMode = TextWrappingModes.NoWrap;
                row.text.overflowMode = TextOverflowModes.Ellipsis;
                SetRect(row.text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(450f, 34f));

                row.strike = NewImage("Strike", rowObject.transform, new Color(0.56f, 0.64f, 0.64f, 0.78f));
                row.strike.enabled = false;
                SetRect(row.strike.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(120f, 1.5f));

                objectiveRows.Add(row);
            }

            RenderObjectiveList();
        }

        private void SelectObjective(string message)
        {
            if (definition?.objectives == null || string.IsNullOrWhiteSpace(message))
            {
                ShowTemporaryInternal(null, "현재 목표", message, 0f);
                return;
            }

            int matched = Array.IndexOf(definition.objectives, message);
            if (matched >= 0)
            {
                SelectObjective(matched, true);
            }
            else
            {
                ShowTemporaryInternal(null, "현재 목표", message, 0f);
            }
        }

        private void SelectObjective(int index, bool reveal)
        {
            if (definition?.objectives == null || definition.objectives.Length == 0)
            {
                return;
            }

            objectiveIndex = Mathf.Clamp(index, 0, definition.objectives.Length - 1);
            RenderObjectiveList();
            if (reveal)
            {
                ShowObjectivePanel();
            }
        }

        private void AdvanceInternal()
        {
            if (definition?.objectives == null || definition.objectives.Length == 0)
            {
                return;
            }

            // Length는 모든 행이 완료된 상태다. 목록은 그대로 화면에 남긴다.
            objectiveIndex = Mathf.Min(objectiveIndex + 1, definition.objectives.Length);
            RenderObjectiveList();
            ShowObjectivePanel();
        }

        private void RestoreObjectivesOrHide()
        {
            if (definition?.objectives != null && definition.objectives.Length > 0)
            {
                ShowObjectivePanel();
            }
            else
            {
                showingObjectives = false;
                targetAlpha = 0f;
                visibleUntil = float.PositiveInfinity;
            }
        }

        private void ShowObjectivePanel()
        {
            showingObjectives = true;
            objectiveRoot.SetActive(true);
            temporaryRoot.SetActive(false);
            int total = definition.objectives.Length;
            progressText.text = objectiveIndex >= total ? "CLEAR" : $"{objectiveIndex + 1:00} / {total:00}";
            visibleUntil = float.PositiveInfinity;
            targetAlpha = 1f;
        }

        private void RenderObjectiveList()
        {
            if (definition?.objectives == null)
            {
                return;
            }

            for (int i = 0; i < objectiveRows.Count; i++)
            {
                ObjectiveRow row = objectiveRows[i];
                bool completed = i < objectiveIndex;
                bool current = i == objectiveIndex;

                row.background.color = current
                    ? new Color(0.34f, 0.08f, 0.07f, 0.28f)
                    : new Color(0f, 0f, 0f, completed ? 0.12f : 0.04f);
                row.marker.color = current
                    ? new Color(0.65f, 0.2f, 0.17f, 1f)
                    : completed
                        ? new Color(0.44f, 0.56f, 0.55f, 0.72f)
                        : new Color(0.31f, 0.38f, 0.39f, 0.4f);
                row.number.color = current
                    ? new Color(0.84f, 0.87f, 0.83f, 1f)
                    : new Color(0.47f, 0.55f, 0.55f, completed ? 0.62f : 0.4f);
                row.text.color = current
                    ? new Color(0.92f, 0.93f, 0.89f, 1f)
                    : completed
                        ? new Color(0.48f, 0.55f, 0.54f, 0.62f)
                        : new Color(0.56f, 0.61f, 0.6f, 0.42f);
                row.text.fontStyle = current ? FontStyles.Bold : FontStyles.Normal;
                row.strike.enabled = completed;

                if (completed)
                {
                    float textWidth = row.text.GetPreferredValues(definition.objectives[i]).x;
                    float strikeWidth = Mathf.Clamp(textWidth, 36f, 448f);
                    SetRect(row.strike.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                        new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(strikeWidth, 1.5f));
                }
            }
        }

        private void BuildShell()
        {
            GameObject root = NewUiObject("ChapterObjectiveHud", transform);
            panelRect = root.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(620f, 340f);
            panelRect.anchoredPosition = new Vector2(34f, -34f);

            group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image shadow = NewImage("Shadow", root.transform, new Color(0f, 0f, 0f, 0.48f));
            Stretch(shadow.rectTransform, new Vector2(-8f, -8f), new Vector2(-8f, -8f));

            Image panel = NewImage("Panel", root.transform, new Color(0.012f, 0.019f, 0.022f, 0.92f));
            Stretch(panel.rectTransform, Vector2.zero, Vector2.zero);

            Image topRail = NewImage("TopRail", panel.transform, new Color(0.38f, 0.47f, 0.48f, 0.82f));
            SetRect(topRail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f));

            Image leftRail = NewImage("LeftRail", panel.transform, new Color(0.55f, 0.16f, 0.14f, 0.9f));
            SetRect(leftRail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f));

            chapterText = NewText("Chapter", panel.transform, 16f, new Color(0.69f, 0.76f, 0.76f, 0.96f));
            chapterText.fontStyle = FontStyles.Bold;
            chapterText.characterSpacing = 1.7f;
            chapterText.alignment = TextAlignmentOptions.MidlineLeft;
            SetRect(chapterText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(48f, -25f), new Vector2(420f, 28f));

            progressText = NewText("Progress", panel.transform, 14f, new Color(0.72f, 0.78f, 0.78f, 0.92f));
            progressText.fontStyle = FontStyles.Bold;
            progressText.alignment = TextAlignmentOptions.MidlineRight;
            SetRect(progressText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-48f, -25f), new Vector2(110f, 28f));

            Image rule = NewImage("HeaderRule", panel.transform, new Color(0.39f, 0.48f, 0.49f, 0.48f));
            SetRect(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(-92f, 2f));

            objectiveRoot = NewUiObject("ObjectiveList", panel.transform);
            Stretch(objectiveRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            temporaryRoot = NewUiObject("TemporaryGuide", panel.transform);
            Stretch(temporaryRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
            temporaryRoot.SetActive(false);

            controlIcon = NewImage("ControlIcon", temporaryRoot.transform, Color.white);
            controlIcon.preserveAspect = true;
            controlIcon.enabled = false;
            SetRect(controlIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(70f, -104f), new Vector2(104f, 78f));

            temporaryCategory = NewText("Category", temporaryRoot.transform, 14f,
                new Color(0.62f, 0.7f, 0.71f, 0.92f));
            temporaryCategory.fontStyle = FontStyles.Bold;
            temporaryCategory.characterSpacing = 2f;
            temporaryCategory.alignment = TextAlignmentOptions.MidlineLeft;

            temporaryMessage = NewText("Message", temporaryRoot.transform, 23f,
                new Color(0.91f, 0.92f, 0.89f, 1f));
            temporaryMessage.fontStyle = FontStyles.Bold;
            temporaryMessage.alignment = TextAlignmentOptions.TopLeft;
            temporaryMessage.textWrappingMode = TextWrappingModes.Normal;
            temporaryMessage.overflowMode = TextOverflowModes.Ellipsis;
            LayoutTemporary(false);

            SetPanelOffset(HiddenOffset);
        }

        private void ResizePanel(int objectiveCount)
        {
            float height = Mathf.Clamp(112f + objectiveCount * RowHeight, 250f, 390f);
            panelRect.sizeDelta = new Vector2(620f, height);
        }

        private void LayoutTemporary(bool hasIcon)
        {
            float x = hasIcon ? 198f : 70f;
            float width = hasIcon ? 350f : 478f;
            SetRect(temporaryCategory.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -101f), new Vector2(width, 22f));
            SetRect(temporaryMessage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, -130f), new Vector2(width, 92f));
        }

        private void SetPanelOffset(float offset)
        {
            if (panelRect != null)
            {
                panelRect.anchoredPosition = new Vector2(34f - offset, -34f);
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
