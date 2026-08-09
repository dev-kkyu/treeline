using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 마지막 장면 위에 얹는 엔딩 메뉴. 다시 하기 · 환경 설정 · 타이틀로.
    /// </summary>
    /// <remarks>
    /// 타이틀 · 일시정지와 같은 부품을 쓴다(<see cref="MenuChrome"/>). 예전에는 이
    /// 파일이 패널과 버튼을 따로 지어서, 버튼 반응을 타이틀에만 붙였더니 여기만
    /// 반응이 없는 화면이 됐다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class EndingScreen : MonoBehaviour
    {
        private const float FadeSeconds = 0.28f;
        private const float OpenSeconds = 0.7f;

        private MenuChrome chrome;
        private MenuPanel panel;
        private MenuPager pager;
        private SettingsPage settings;

        private GameObject endingPage;
        private Button retryButton;
        private Button settingsButton;
        private Button titleButton;

        private bool isNavigating;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            MenuChrome.ConfigureCanvas(GetComponent<Canvas>(), GetComponent<CanvasScaler>());
            ClearAuthoredPlaceholder();

            chrome = MenuChrome.Create(transform);
            BuildInterface();
        }

        private void Start()
        {
            pager.ShowImmediate(endingPage);
            StartCoroutine(FadeCanvas(0f, 1f, OpenSeconds));
            StartCoroutine(SlideMenuIn());
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && !pager.IsOn(endingPage))
            {
                ShowEndingMenu();
            }
        }

        private void OnDestroy()
        {
            GameSettings.Save();
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
            // 왼쪽에 세운다. 오른쪽은 배경의 삭도가 산으로 뻗는 자리라 겹치면
            // 「케이블은 계속 아래로 움직인다」를 그림이 가려 버린다. 타이틀도
            // 왼쪽이므로 두 화면의 메뉴 자리가 같아지는 이점도 있다.
            panel = chrome.BuildPanel(transform, "EndingUI", 0.3f,
                "삭도 운행 기록  /  수신 종료", "신호 종료", 56f, "케이블은 계속 아래로 움직인다");

            endingPage = BuildEndingMenu();
            settings = new SettingsPage(chrome, panel.Frame, "EndingSettings", ShowEndingMenu);

            pager = new MenuPager(this);
            pager.Add(endingPage, () => retryButton);
            pager.Add(settings.Root, () => settings.FirstFocus);
        }

        private GameObject BuildEndingMenu()
        {
            GameObject page = chrome.CreatePage("EndingMenu", panel.Frame);
            Transform t = page.transform;

            retryButton = chrome.CreateMenuButton(t, "RetryButton", "다시 하기",
                new Vector2(80f, -336f), 86f, RetryGame);
            settingsButton = chrome.CreateMenuButton(t, "SettingsButton", "환경 설정",
                new Vector2(80f, -438f), 86f, ShowSettingsMenu);
            titleButton = chrome.CreateMenuButton(t, "TitleButton", "타이틀로",
                new Vector2(80f, -540f), 86f, ReturnToTitle);

            chrome.CreateText("EndingHint", t, "처음부터 다시 내려갑니다", 15f, MenuChrome.Muted,
                TextAlignmentOptions.Left, new Vector2(84f, -650f), new Vector2(460f, 28f), false);
            chrome.CreateText("BuildLabel", t, "TREELINE  ·  END OF TRANSMISSION", 13f,
                new Color(MenuChrome.Muted.r, MenuChrome.Muted.g, MenuChrome.Muted.b, 0.68f),
                TextAlignmentOptions.Left, new Vector2(84f, -746f), new Vector2(450f, 24f), false);

            return page;
        }

        // ---- 쪽 넘기기 --------------------------------------------------------

        private void ShowSettingsMenu()
        {
            settings.Sync();
            pager.GoTo(settings.Root);
        }

        private void ShowEndingMenu()
        {
            settings.Save();
            pager.GoTo(endingPage, settingsButton);
        }

        // ---- 연출 -------------------------------------------------------------

        private IEnumerator SlideMenuIn()
        {
            Button[] order = { retryButton, settingsButton, titleButton };
            var homes = new Vector2[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                var rect = (RectTransform)order[i].transform;
                homes[i] = rect.anchoredPosition;
                rect.anchoredPosition = homes[i] + new Vector2(-34f, 0f);
            }

            // 엔딩은 배경이 먼저 보여야 하는 화면이라 타이틀보다 느리게 들어온다.
            const float step = 0.11f;
            const float travel = 0.32f;
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

        // ---- 나가기 -----------------------------------------------------------

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
            GameSettings.Save();
            retryButton.interactable = false;
            settingsButton.interactable = false;
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
            panel.Group.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                panel.Group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            panel.Group.alpha = to;
        }
    }
}
