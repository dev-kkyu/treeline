using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 한 패널 안에서 쪽을 갈아 끼운다. 켜고 끄는 대신 투명도와 좌우 오프셋을 넘긴다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 딱딱 바뀌면 같은 자리에 다른 글자가 순간이동한 것으로 보이고, 어디서 어디로
    /// 갔는지가 안 남는다. 나가는 쪽은 왼쪽으로 빠지고 들어오는 쪽은 오른쪽에서
    /// 온다 — 방향이 「더 안쪽으로 들어간다」를 말해 준다.
    /// </para>
    /// <para>
    /// IMPORTANT: 시간은 전부 <see cref="Time.unscaledDeltaTime"/> 이다. 일시정지
    /// 화면이 이걸 쓰는데 거기서는 <c>Time.timeScale</c> 이 0 이라, 보통 시간을 쓰면
    /// 쪽이 영영 안 넘어간다.
    /// </para>
    /// </remarks>
    public sealed class MenuPager
    {
        private const float OutSeconds = 0.1f;
        private const float InSeconds = 0.16f;
        private const float Slide = 22f;

        private readonly MonoBehaviour host;
        private readonly List<Page> pages = new List<Page>();
        private Page active;
        private Coroutine switching;

        public MenuPager(MonoBehaviour host)
        {
            this.host = host;
        }

        /// <summary>지금 떠 있는 쪽. 아직 아무것도 안 띄웠으면 <c>null</c>.</summary>
        public GameObject Active => active?.Root;

        public void Add(GameObject root, Func<Selectable> firstFocus)
        {
            pages.Add(new Page(root, firstFocus));
        }

        public bool IsOn(GameObject root) => active != null && active.Root == root;

        /// <summary>연출 없이 곧바로 띄운다. 화면이 처음 열릴 때 쓴다.</summary>
        public void ShowImmediate(GameObject root)
        {
            StopSwitching();

            foreach (Page page in pages)
            {
                bool wanted = page.Root == root;
                page.Root.SetActive(wanted);
                page.Group.alpha = wanted ? 1f : 0f;
                page.Group.interactable = wanted;
                page.Group.blocksRaycasts = wanted;
                page.Rect.anchoredPosition = Vector2.zero;
                if (wanted)
                {
                    active = page;
                }
            }

            MenuChrome.Select(active?.FirstFocus?.Invoke());
        }

        /// <summary>쪽을 넘긴다. <paramref name="focus"/> 를 주면 그것을 대신 고른다.</summary>
        public void GoTo(GameObject root, Selectable focus = null)
        {
            Page next = pages.Find(p => p.Root == root);
            if (next == null || (active != null && active.Root == root))
            {
                return;
            }

            StopSwitching();
            switching = host.StartCoroutine(SwitchRoutine(next, focus));
        }

        private void StopSwitching()
        {
            if (switching != null)
            {
                host.StopCoroutine(switching);
                switching = null;
            }
        }

        private IEnumerator SwitchRoutine(Page next, Selectable focus)
        {
            Page previous = active;

            // 넘어가는 동안은 어느 쪽도 못 누르게 한다. 반쯤 투명한 버튼이 눌리면
            // 방금 떠난 쪽의 동작이 뒤늦게 실행된다.
            if (previous != null)
            {
                previous.Group.interactable = false;
                previous.Group.blocksRaycasts = false;
                yield return Slide2(previous, 1f, 0f, 0f, -Slide, OutSeconds);
                previous.Root.SetActive(false);
            }

            active = next;
            next.Root.SetActive(true);
            next.Group.interactable = true;
            next.Group.blocksRaycasts = true;
            MenuChrome.Select(focus != null ? focus : next.FirstFocus?.Invoke());
            yield return Slide2(next, 0f, 1f, Slide, 0f, InSeconds);

            switching = null;
        }

        private static IEnumerator Slide2(Page page, float fromAlpha, float toAlpha,
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

        private sealed class Page
        {
            public Page(GameObject root, Func<Selectable> firstFocus)
            {
                Root = root;
                Group = root.GetComponent<CanvasGroup>();
                Rect = (RectTransform)root.transform;
                FirstFocus = firstFocus;
            }

            public GameObject Root { get; }
            public CanvasGroup Group { get; }
            public RectTransform Rect { get; }
            public Func<Selectable> FirstFocus { get; }
        }
    }
}
