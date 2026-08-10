using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 「조작키 가이드」 쪽. 키캡 그림 한 줄에 하는 일 하나씩.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 타이틀과 일시정지가 <b>같은 것</b>을 띄운다. <see cref="SettingsPage"/> 와
    /// 같은 판단이다 — 화면마다 따로 적으면 키를 하나 바꿀 때 두 곳을 고쳐야 하고,
    /// 반드시 한쪽을 빠뜨린다.
    /// </para>
    /// <para>
    /// 키캡 그림은 1 · 2장 튜토리얼이 쓰는 것을 그대로 쓴다
    /// (<c>Resources/Tutorial/keycap-*</c>). 게임 안에서 본 그림이 메뉴에도 있어야
    /// 「아까 그거」로 이어진다.
    /// </para>
    /// <para>
    /// IMPORTANT: 여기 적힌 키는 <c>InputSystem_Actions</c> 의 바인딩과 손으로
    /// 맞춰 둔 것이다. 액션 바인딩을 바꾸면 이 표도 같이 고친다.
    /// </para>
    /// </remarks>
    public sealed class ControlsPage
    {
        private const float FirstRowY = -352f;
        private const float RowPitch = 62f;
        private const float RowHeight = 44f;
        private const float RowWidth = 226f;

        /// <summary>한 줄에 열이 둘이다. 열 하나에 이만큼 담고 나머지는 옆으로 넘어간다.</summary>
        private const int RowsPerColumn = 6;
        private const float ColumnGap = 236f;

        /// <summary>
        /// 키캡 그림이 들어갈 칸. 가장 넓은 SPACE(144×56) 가 시원하게 들어가는 폭이라,
        /// 여기서 더 줄이면 넓은 키부터 작아진다.
        /// </summary>
        private static readonly Vector2 KeyBox = new Vector2(100f, 40f);

        private static readonly (string Icon, string Action)[] Bindings =
        {
            ("keycap-wasd", "이동"),
            ("mouse-look", "시점"),
            ("keycap-shift", "달리기"),
            ("keycap-c", "웅크리기"),
            ("keycap-space", "점프"),
            ("keycap-e", "상호작용"),
            ("keycap-f", "손전등"),
            ("mouse-left-click", "사격"),
            ("keycap-r", "재장전"),
            ("keycap-1-2", "무기 바꾸기"),
            ("keycap-tab", "일시 정지"),
        };

        public ControlsPage(MenuChrome chrome, Transform frame, string name,
            UnityEngine.Events.UnityAction back)
        {
            Root = chrome.CreatePage(name, frame);
            Transform t = Root.transform;

            // 다른 쪽(-318)보다 위다. 표가 한 줄 더 들어가야 해서 제목을 올려 자리를 냈다.
            chrome.CreateText("ControlsTitle", t, "조작키 가이드", 34f, MenuChrome.Frost,
                TextAlignmentOptions.Left, new Vector2(80f, -296f), new Vector2(330f, 54f), true);

            var feel = Root.AddComponent<ControlsPageReveal>();
            var rows = new ControlsPageReveal.Row[Bindings.Length];
            for (int i = 0; i < Bindings.Length; i++)
            {
                rows[i] = BuildRow(chrome, t, i, Bindings[i].Icon, Bindings[i].Action);
            }

            BackButton = chrome.CreateMenuButton(t, "ControlsBackButton", "뒤로",
                new Vector2(80f, -738f), 74f, back);

            feel.Bind(rows);
        }

        public GameObject Root { get; }

        public Button BackButton { get; }

        /// <summary>읽기만 하는 쪽이라 처음 초점은 나가는 문에 둔다.</summary>
        public Selectable FirstFocus => BackButton;

        private static ControlsPageReveal.Row BuildRow(MenuChrome chrome, Transform parent, int index,
            string iconName, string action)
        {
            GameObject row = MenuChrome.CreateUiObject($"Row{index}", parent);
            MenuChrome.SetTopLeft(row.GetComponent<RectTransform>(),
                new Vector2(84f + ColumnGap * (index / RowsPerColumn),
                    FirstRowY - RowPitch * (index % RowsPerColumn)),
                new Vector2(RowWidth, RowHeight));
            var group = row.AddComponent<CanvasGroup>();

            Image tick = MenuChrome.CreateImage("Tick", row.transform, null, MenuChrome.BruisedRed);
            MenuChrome.SetRect(tick.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(2f, -10f));
            tick.raycastTarget = false;

            // 키캡은 폭이 제각각이다(C 56px, SPACE 144px). 같은 칸 안에 가운데로
            // 넣고 비율을 지켜야 글자 열이 안 흔들린다.
            Image icon = MenuChrome.CreateImage("Key", row.transform,
                Resources.Load<Sprite>($"Tutorial/{iconName}"), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            MenuChrome.SetTopLeft(icon.rectTransform,
                new Vector2(10f, -(RowHeight - KeyBox.y) * 0.5f), KeyBox);

            TMP_Text label = chrome.CreateText("Action", row.transform, action, 17f, MenuChrome.Frost,
                TextAlignmentOptions.Left, new Vector2(122f, -11f), new Vector2(104f, 24f), false);

            return new ControlsPageReveal.Row(row.GetComponent<RectTransform>(), group,
                icon.rectTransform, tick);
        }
    }

    /// <summary>
    /// 쪽이 열릴 때 줄이 차례로 밀려 들어오게 한다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 조작표는 글자만 있는 쪽이라 통째로 뜨면 <b>읽을 것이 한 번에 쏟아진다.</b>
    /// 위에서 아래로 훑어 주면 눈이 따라갈 순서가 생기고, 표가 「나타나는」 것으로
    /// 보인다.
    /// </para>
    /// <para>
    /// IMPORTANT: 시간은 <see cref="Time.unscaledDeltaTime"/> 이다. 일시정지
    /// 화면에서는 <c>Time.timeScale</c> 이 0 이라 보통 시간을 쓰면 첫 줄에서 멈춘다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ControlsPageReveal : MonoBehaviour
    {
        private const float Stagger = 0.035f;
        private const float Travel = 0.24f;
        private const float Slide = 26f;

        private Row[] rows;

        public void Bind(Row[] items)
        {
            rows = items;
            Settle(0f);
        }

        private void OnEnable()
        {
            if (rows == null)
            {
                return;
            }

            Settle(0f);
            StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            float elapsed = 0f;
            float total = Stagger * (rows.Length - 1) + Travel;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                Settle(elapsed);
                yield return null;
            }

            Settle(total);
        }

        /// <summary>줄마다 제 시간을 잘라 쓴다. 한곳에서 그려야 중간에 끊겨도 자리가 안 남는다.</summary>
        private void Settle(float elapsed)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                float t = Mathf.Clamp01((elapsed - Stagger * i) / Travel);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rows[i].Draw(eased);
            }
        }

        /// <summary>한 줄이 제 자리로 오는 방법. 자리 · 투명도 · 키캡 크기 · 눈금 셋을 함께 움직인다.</summary>
        public sealed class Row
        {
            private readonly RectTransform rect;
            private readonly CanvasGroup group;
            private readonly RectTransform key;
            private readonly Image tick;
            private readonly Vector2 home;

            public Row(RectTransform rect, CanvasGroup group, RectTransform key, Image tick)
            {
                this.rect = rect;
                this.group = group;
                this.key = key;
                this.tick = tick;
                home = rect.anchoredPosition;
            }

            public void Draw(float eased)
            {
                rect.anchoredPosition = home + new Vector2(Mathf.Lerp(-Slide, 0f, eased), 0f);
                group.alpha = eased;

                // 키캡은 조금 크게 왔다가 제자리로 눌린다 — 누른 것처럼 보인다.
                float pop = 1f + 0.14f * Mathf.Sin(eased * Mathf.PI);
                key.localScale = new Vector3(pop, pop, 1f);

                // 왼쪽 눈금은 줄이 다 선 뒤에야 진해진다. 훑고 지나간 자국이 된다.
                Color color = tick.color;
                tick.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0f, 0.85f, eased * eased));
            }
        }
    }
}
