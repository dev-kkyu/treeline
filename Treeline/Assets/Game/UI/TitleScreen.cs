using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 설원 렌더 위에 얹는 타이틀 메뉴. 게임 시작 · 환경 설정 · 크레딧 세 쪽을
    /// 한 패널 안에서 갈아 끼운다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 화면을 코드로 짓는다. 씬에 미리 그려 두지 않는 이유는 <b>엔딩 · 일시정지
    /// 화면과 같은 모양</b>이어야 하는데 그것들이 다른 씬에 있거나 아예 씬에 없어서,
    /// 손으로 맞추면 반드시 어긋나기 때문이다. 부품은 <see cref="MenuChrome"/> 이
    /// 찍고 여기서는 배치만 정한다.
    /// </para>
    /// <para>
    /// 설정값 자체는 여기 없다. <see cref="GameSettings"/> 가 들고 있고 손잡이는
    /// <see cref="SettingsPage"/> 가 그린다 — 같은 것을 세 화면이 함께 쓴다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public sealed class TitleScreen : MonoBehaviour
    {
        private const float FadeSeconds = 0.22f;

        private MenuChrome chrome;
        private MenuPanel panel;
        private MenuPager pager;
        private SettingsPage settings;

        private GameObject mainPage;
        private GameObject creditsPage;
        private Button startButton;
        private Button settingsButton;
        private Button creditsButton;
        private Button creditsBackButton;

        private Vector2 gameTitleHome;
        private bool isStarting;

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
            pager.ShowImmediate(mainPage);
            StartCoroutine(FadeCanvas(0f, 1f, FadeSeconds));
            StartCoroutine(SlideMenuIn());
            StartCoroutine(SignalFlicker());
        }

        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && !pager.IsOn(mainPage))
            {
                ShowMainMenu();
            }
        }

        private void OnDestroy()
        {
            GameSettings.Save();
        }

        /// <summary>씬에 손으로 그려 둔 자리표시를 걷어낸다. 화면은 전부 코드가 짓는다.</summary>
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
            panel = chrome.BuildPanel(transform, "MainMenuUI", 0.22f,
                "설산 중계소  /  신호 두절", "TREELINE", 72f, "신호는 끊겼고, 아래로 내려가야 한다");
            gameTitleHome = panel.Title.rectTransform.anchoredPosition;

            mainPage = BuildMainMenu();
            creditsPage = BuildCreditsMenu();
            settings = new SettingsPage(chrome, panel.Frame, "SettingsMenu", ShowMainMenu);

            pager = new MenuPager(this);
            pager.Add(mainPage, () => startButton);
            pager.Add(settings.Root, () => settings.FirstFocus);
            pager.Add(creditsPage, () => creditsBackButton);
        }

        private GameObject BuildMainMenu()
        {
            GameObject page = chrome.CreatePage("MainMenu", panel.Frame);
            Transform t = page.transform;

            startButton = chrome.CreateMenuButton(t, "StartButton", "게임 시작",
                new Vector2(80f, -336f), 86f, StartGame);
            settingsButton = chrome.CreateMenuButton(t, "SettingsButton", "환경 설정",
                new Vector2(80f, -438f), 86f, ShowSettingsMenu);
            creditsButton = chrome.CreateMenuButton(t, "CreditsButton", "크레딧",
                new Vector2(80f, -540f), 86f, ShowCreditsMenu);

            chrome.CreateText("StartHint", t, "첫 번째 중계소부터 시작합니다", 15f, MenuChrome.Muted,
                TextAlignmentOptions.Left, new Vector2(84f, -650f), new Vector2(460f, 28f), false);
            chrome.CreateText("BuildLabel", t, "DESKTOP WEBGL  ·  1920×1080", 13f,
                new Color(MenuChrome.Muted.r, MenuChrome.Muted.g, MenuChrome.Muted.b, 0.68f),
                TextAlignmentOptions.Left, new Vector2(84f, -746f), new Vector2(450f, 24f), false);

            return page;
        }

        private GameObject BuildCreditsMenu()
        {
            GameObject page = chrome.CreatePage("CreditsMenu", panel.Frame);
            Transform t = page.transform;

            chrome.CreateText("CreditsTitle", t, "크레딧", 34f, MenuChrome.Frost,
                TextAlignmentOptions.Left, new Vector2(80f, -316f), new Vector2(330f, 54f), true);

            chrome.CreateSectionLabel(t, "TeamHeading", "TEAM SBJ", -366f);
            CreateCreditRow(t, "이정범", "플레이어 · 전투 · 진행 UI", -392f);
            CreateCreditRow(t, "김도한", "적 · 에셋 파이프라인", -424f);
            CreateCreditRow(t, "임성규", "상호작용 · 레벨 · 진행", -456f);

            chrome.CreateDivider("CreditsDivider", t, new Vector2(84f, -494f), 462f, 0.75f);

            chrome.CreateSectionLabel(t, "MadeHeading", "만든 것", -510f);
            CreateBodyText(t, "MadeBody", "모델 · 텍스처 · 사운드 · 음악", -534f, 30f);

            chrome.CreateSectionLabel(t, "ToolHeading", "쓴 도구", -574f);
            CreateBodyText(t, "ToolBody",
                "Claude Code · Codex · Meshy AI · ElevenLabs\nBlender · Unity 6.3 LTS", -598f, 52f);

            chrome.CreateSectionLabel(t, "FontHeading", "글꼴", -658f);
            CreateBodyText(t, "FontBody",
                "IBM Plex Sans KR — SIL Open Font License 1.1", -682f, 28f);

            chrome.CreateText("EventLabel", t, "NHN GAME × AI HACKATHON (NAN2026) 사전 과제", 13f,
                new Color(MenuChrome.Muted.r, MenuChrome.Muted.g, MenuChrome.Muted.b, 0.68f),
                TextAlignmentOptions.Left, new Vector2(84f, -710f), new Vector2(470f, 24f), false);

            creditsBackButton = chrome.CreateMenuButton(t, "CreditsBackButton", "뒤로",
                new Vector2(80f, -736f), 74f, ShowMainMenu);

            return page;
        }

        private void CreateCreditRow(Transform parent, string person, string role, float y)
        {
            chrome.CreateText($"Name_{person}", parent, person, 20f, MenuChrome.Frost,
                TextAlignmentOptions.Left, new Vector2(84f, y), new Vector2(140f, 30f), true);
            chrome.CreateText($"Role_{person}", parent, role, 16f, MenuChrome.Muted,
                TextAlignmentOptions.Left, new Vector2(210f, y + 2f), new Vector2(336f, 28f), false);
        }

        private void CreateBodyText(Transform parent, string name, string value, float y, float height)
        {
            TMP_Text body = chrome.CreateText(name, parent, value, 15f,
                new Color(MenuChrome.Frost.r, MenuChrome.Frost.g, MenuChrome.Frost.b, 0.82f),
                TextAlignmentOptions.TopLeft, new Vector2(84f, y), new Vector2(462f, height), false);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 12f;
        }

        // ---- 쪽 넘기기 --------------------------------------------------------

        private void ShowMainMenu()
        {
            Selectable focus = pager.IsOn(settings.Root) ? settingsButton : creditsButton;
            settings.Save();
            pager.GoTo(mainPage, focus);
        }

        private void ShowSettingsMenu()
        {
            settings.Sync();
            pager.GoTo(settings.Root);
        }

        private void ShowCreditsMenu() => pager.GoTo(creditsPage);

        // ---- 연출 -------------------------------------------------------------

        /// <summary>버튼이 왼쪽에서 차례로 밀려 들어온다. 셋이 동시에 뜨면 카드 한 장으로 보인다.</summary>
        private IEnumerator SlideMenuIn()
        {
            Button[] order = { startButton, settingsButton, creditsButton };
            var homes = new Vector2[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                var rect = (RectTransform)order[i].transform;
                homes[i] = rect.anchoredPosition;
                rect.anchoredPosition = homes[i] + new Vector2(-34f, 0f);
            }

            const float step = 0.07f;
            const float travel = 0.24f;
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

        /// <summary>
        /// 제목이 이따금 튄다. 「신호 두절」이라고 써 놓고 글자가 멀쩡히 붙어 있으면
        /// 그 문장이 배경 설명으로만 남는다.
        /// </summary>
        private IEnumerator SignalFlicker()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(4.5f, 11f));

                int bursts = Random.Range(2, 5);
                for (int i = 0; i < bursts; i++)
                {
                    panel.Title.rectTransform.anchoredPosition = gameTitleHome + new Vector2(
                        Random.Range(-4f, 4f), Random.Range(-2f, 2f));
                    panel.Title.color = i % 2 == 0
                        ? new Color(MenuChrome.BruisedRed.r, MenuChrome.BruisedRed.g,
                            MenuChrome.BruisedRed.b, 0.85f)
                        : MenuChrome.Frost;
                    panel.Status.color = new Color(MenuChrome.Muted.r, MenuChrome.Muted.g,
                        MenuChrome.Muted.b, 0.35f);
                    yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.07f));
                }

                panel.Title.rectTransform.anchoredPosition = gameTitleHome;
                panel.Title.color = MenuChrome.Frost;
                panel.Status.color = MenuChrome.Muted;
            }
        }

        // ---- 시작 -------------------------------------------------------------

        private void StartGame()
        {
            if (isStarting)
            {
                return;
            }
            isStarting = true;
            GameSettings.Save();
            startButton.interactable = false;
            settingsButton.interactable = false;
            creditsButton.interactable = false;
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
