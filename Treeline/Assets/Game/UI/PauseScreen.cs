using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 장 안에서 TAB 으로 여는 일시정지 화면. 재개 · 조작키 가이드 · 챕터 다시하기 ·
    /// 환경 설정.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>씬에 배치하지 않는다.</b> 장이 열릴 때 스스로 생긴다. 여섯 씬에 같은
    /// 캔버스를 넣으면 버튼 하나 고칠 때 병합할 수 없는 파일을 여섯 개 건드리게
    /// 된다. <see cref="GameFlow"/> · <see cref="ReadingPanel"/> 과 같은 판단이다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>여는 키는 ESC 가 아니라 TAB 이다.</b> 브라우저가 ESC 를 먼저
    /// 가로채 전체 화면과 포인터 잠금을 풀어 버리고 키 입력은 게임에 안 넘긴다.
    /// 그래서 <b>잠금이 풀린 것</b>도 함께 신호로 쓴다 — ESC 를 누른 사람도, 알트탭으로
    /// 창을 벗어난 사람도 그 자리에서 멈추는 것이 맞는 동작이다.
    /// </para>
    /// <para>
    /// 멈추는 방법은 <c>Time.timeScale = 0</c> 하나가 아니다. 시점 회전은
    /// 마우스 델타라 <c>deltaTime</c> 을 안 곱하므로 시간을 멈춰도 계속 돈다.
    /// 그래서 <see cref="PlayerLook"/> 과 <see cref="PlayerInputReader"/> 를 함께
    /// 끈다 — 전자는 꺼지면서 커서까지 풀어 주고, 후자는 액션 맵을 통째로 내려
    /// 메뉴를 누르는 클릭이 총으로 새지 않게 한다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PauseScreen : MonoBehaviour
    {
        private const float OpenSeconds = 0.18f;
        private const float CloseSeconds = 0.12f;
        private const float RailSlide = 70f;

        /// <summary>재개 직후에는 잠금이 아직 안 걸려 있다. 그 사이를 신호로 읽지 않는다.</summary>
        private const float ReopenGrace = 0.4f;

        private static bool installed;

        private MenuChrome chrome;
        private MenuPanel panel;
        private MenuPager pager;
        private SettingsPage settings;
        private ControlsPage controls;
        private GameObject menuPage;
        private Button resumeButton;
        private Button controlsButton;
        private Button restartButton;
        private Button settingsButton;

        private bool isOpen;
        private bool sawCursorLock;
        private float graceUntil;
        private Coroutine fade;

        private PlayerLook suspendedLook;
        private PlayerInputReader suspendedInput;

        /// <summary>지금 멈춰 있는지. 다른 시스템이 물어볼 수 있게 열어 둔다.</summary>
        public static bool IsPaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (installed)
            {
                return;
            }
            installed = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            // TAB 은 브라우저에서 초점을 다음 요소로 넘기는 키다. 캔버스가 키보드를
            // 통째로 잡고 있어야 게임까지 온다. 기본값이 true 이긴 하지만, 여기가
            // 꺼지면 일시정지를 아예 못 여는 자리라 못 박아 둔다.
            WebGLInput.captureAllKeyboardInput = true;
#endif

            SceneManager.sceneLoaded += OnSceneLoaded;

            // 첫 씬에는 sceneLoaded 가 오지 않는다. 씬이 먼저 로드되고 이 훅이 뒤에
            // 돈다. 빌드의 첫 장과 에디터에서 장을 직접 플레이할 때가 여기다.
            SpawnIfChapter(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SpawnIfChapter(scene);
        }

        private static void SpawnIfChapter(Scene scene)
        {
            if (!scene.IsValid() || !Chapters.IsChapter(scene.name))
            {
                return;
            }

            // 씬과 함께 죽게 둔다. DontDestroyOnLoad 로 들고 다니면 타이틀 · 엔딩에도
            // 따라가서 그쪽 EventSystem 과 둘이 된다.
            var go = new GameObject("[PauseScreen]");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<PauseScreen>();
        }

        private void Awake()
        {
            IsPaused = false;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // HUD · 읽을거리(100) · 레터박스(500) 보다 위다. 멈춤 화면은 무엇에도
            // 가려지면 안 된다.
            canvas.sortingOrder = 600;
            MenuChrome.ConfigureCanvas(canvas, gameObject.AddComponent<CanvasScaler>());
            gameObject.AddComponent<GraphicRaycaster>();

            MenuChrome.EnsureEventSystem(transform);

            chrome = MenuChrome.Create(transform);
            BuildInterface();

            panel.Root.SetActive(false);
        }

        private void OnDestroy()
        {
            // 멈춘 채로 장을 다시 부르면 새 씬이 얼어붙은 채로 뜬다.
            if (IsPaused)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
                IsPaused = false;
            }
        }

        private void BuildInterface()
        {
            panel = chrome.BuildPanel(transform, "PauseUI", 0.62f,
                "중계소 기록  /  일시 정지", "일시 정지", 56f, ChapterLine());

            menuPage = chrome.CreatePage("PauseMenu", panel.Frame);
            resumeButton = chrome.CreateMenuButton(menuPage.transform, "ResumeButton", "재개",
                new Vector2(80f, -336f), 86f, Resume);
            controlsButton = chrome.CreateMenuButton(menuPage.transform, "ControlsButton", "조작키 가이드",
                new Vector2(80f, -434f), 86f, ShowControls);
            restartButton = chrome.CreateMenuButton(menuPage.transform, "RestartButton", "챕터 다시하기",
                new Vector2(80f, -532f), 86f, RestartChapter);
            settingsButton = chrome.CreateMenuButton(menuPage.transform, "SettingsButton", "환경 설정",
                new Vector2(80f, -630f), 86f, ShowSettings);

            chrome.CreateText("RestartHint", menuPage.transform,
                "다시하기는 이 장의 처음으로 돌아갑니다", 15f, MenuChrome.Muted,
                TextAlignmentOptions.Left, new Vector2(84f, -736f), new Vector2(460f, 28f), false);
            chrome.CreateText("BackHint", menuPage.transform, "TAB  ·  돌아가기", 13f,
                new Color(MenuChrome.Muted.r, MenuChrome.Muted.g, MenuChrome.Muted.b, 0.68f),
                TextAlignmentOptions.Left, new Vector2(84f, -772f), new Vector2(450f, 24f), false);

            settings = new SettingsPage(chrome, panel.Frame, "PauseSettings", ShowMenu);
            controls = new ControlsPage(chrome, panel.Frame, "PauseControls", ShowMenu);

            pager = new MenuPager(this);
            pager.Add(menuPage, () => resumeButton);
            pager.Add(settings.Root, () => settings.FirstFocus);
            pager.Add(controls.Root, () => controls.FirstFocus);
        }

        private static string ChapterLine()
        {
            string label = HudGuideText.ChapterLabel;
            return string.IsNullOrEmpty(label) ? "신호는 끊긴 채로 기다린다" : label;
        }

        private void Update()
        {
            bool pauseKey = Keyboard.current?.tabKey.wasPressedThisFrame == true;

            if (isOpen)
            {
                if (pauseKey)
                {
                    // 안쪽 쪽에 있으면 한 칸만 물러난다. 한 번에 게임으로 돌아가면
                    // 설정을 보다가 실수로 닫히는 것과 구분이 안 된다.
                    if (pager.IsOn(menuPage))
                    {
                        Resume();
                    }
                    else
                    {
                        ShowMenu();
                    }
                }
                return;
            }

            bool locked = Cursor.lockState == CursorLockMode.Locked;
            bool lostLock = sawCursorLock && !locked;
            sawCursorLock = locked;

            // 기록을 펼쳐 둔 채로 누른 TAB 은 기록을 닫는 손이다. 여기서 멈춰 버리면
            // 멈춤 화면 뒤에 읽던 종이가 그대로 깔린다. 잠금이 풀려서 오는 신호는
            // 그대로 둔다 — 창을 벗어난 것은 읽는 중이어도 멈추는 게 맞다.
            if (pauseKey && !lostLock && ReadingPanel.IsOpen)
            {
                return;
            }

            if (Time.unscaledTime < graceUntil)
            {
                return;
            }

            if ((pauseKey || lostLock) && CanPause())
            {
                Open();
            }
        }

        /// <summary>
        /// 죽어서 재시작을 기다리는 중이거나 장을 옮기는 중에는 안 멈춘다.
        /// </summary>
        /// <remarks>
        /// 죽으면 <see cref="PlayerHealth"/> 가 입력을 끄고 커서가 풀리는데, 그것을
        /// 「ESC 를 눌렀다」로 읽으면 <b>죽을 때마다 멈춤 화면이 뜬다.</b>
        /// </remarks>
        private bool CanPause()
        {
            if (GameFlow.Instance.IsTransitioning)
            {
                return false;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return false;
            }

            var health = player.GetComponent<PlayerHealth>();
            return health == null || health.IsAlive;
        }

        private void Open()
        {
            if (isOpen)
            {
                return;
            }
            isOpen = true;
            IsPaused = true;

            SuspendPlayer();

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            panel.Subtitle.text = ChapterLine();
            panel.Root.SetActive(true);
            pager.ShowImmediate(menuPage);
            settings.Sync();

            Restart(FadeIn());
        }

        private void Resume()
        {
            if (!isOpen)
            {
                return;
            }
            isOpen = false;
            settings.Save();

            Restart(FadeOutThenRun());
        }

        private void ShowSettings() => pager.GoTo(settings.Root);

        private void ShowControls() => pager.GoTo(controls.Root);

        /// <summary>어느 쪽에서 물러났든 그 쪽을 부른 버튼으로 초점을 되돌린다.</summary>
        private void ShowMenu()
        {
            Selectable focus = pager.IsOn(controls.Root) ? controlsButton : settingsButton;
            settings.Save();
            pager.GoTo(menuPage, focus);
        }

        private void RestartChapter()
        {
            if (!isOpen)
            {
                return;
            }
            isOpen = false;

            settings.Save();
            ReleasePlayer();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;

            panel.Root.SetActive(false);
            GameFlow.Instance.RestartChapterFromStart();
        }

        // ---- 멈춤과 풀기 ------------------------------------------------------

        /// <summary>
        /// 시점과 입력을 내린다. 무엇을 내렸는지 기억해 두고 그것만 되돌린다 —
        /// 이미 꺼져 있던 것을 켜면 죽은 뒤에 조종이 되살아난다.
        /// </summary>
        private void SuspendPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return;
            }

            var look = player.GetComponentInChildren<PlayerLook>(true);
            if (look != null && look.enabled)
            {
                look.enabled = false;   // 꺼지면서 커서까지 풀어 준다
                suspendedLook = look;
            }

            var input = player.GetComponentInChildren<PlayerInputReader>(true);
            if (input != null && input.enabled)
            {
                input.enabled = false;  // 액션 맵이 통째로 내려간다
                suspendedInput = input;
            }
        }

        private void ReleasePlayer()
        {
            if (suspendedInput != null)
            {
                suspendedInput.enabled = true;
                suspendedInput = null;
            }

            if (suspendedLook != null)
            {
                // 켜지면서 커서를 다시 잠근다. 브라우저는 사용자 제스처가 있어야
                // 잠가 주는데, 버튼을 누른 것이 바로 그 제스처다.
                suspendedLook.enabled = true;
                suspendedLook = null;
            }

            sawCursorLock = false;
            graceUntil = Time.unscaledTime + ReopenGrace;
        }

        // ---- 연출 -------------------------------------------------------------

        private void Restart(IEnumerator routine)
        {
            if (fade != null)
            {
                StopCoroutine(fade);
            }
            fade = StartCoroutine(routine);
        }

        private IEnumerator FadeIn()
        {
            Button[] order = { resumeButton, controlsButton, restartButton, settingsButton };
            var homes = new Vector2[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                var rect = (RectTransform)order[i].transform;
                homes[i] = rect.anchoredPosition;
                rect.anchoredPosition = homes[i] + new Vector2(-34f, 0f);
            }

            Vector2 railHome = panel.Rail.anchoredPosition;
            float elapsed = 0f;
            const float stagger = 0.05f;
            const float travel = 0.2f;
            float total = Mathf.Max(OpenSeconds, stagger * (order.Length - 1) + travel);

            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / OpenSeconds);
                panel.Group.alpha = t * t * (3f - 2f * t);
                panel.Rail.anchoredPosition = railHome + new Vector2(Mathf.Lerp(-RailSlide, 0f, panel.Group.alpha), 0f);

                for (int i = 0; i < order.Length; i++)
                {
                    float bt = Mathf.Clamp01((elapsed - stagger * i) / travel);
                    float eased = 1f - Mathf.Pow(1f - bt, 3f);
                    ((RectTransform)order[i].transform).anchoredPosition =
                        homes[i] + new Vector2(Mathf.Lerp(-34f, 0f, eased), 0f);
                }

                yield return null;
            }

            panel.Group.alpha = 1f;
            panel.Rail.anchoredPosition = railHome;
            for (int i = 0; i < order.Length; i++)
            {
                ((RectTransform)order[i].transform).anchoredPosition = homes[i];
            }
            fade = null;
        }

        private IEnumerator FadeOutThenRun()
        {
            Vector2 railHome = panel.Rail.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < CloseSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / CloseSeconds);
                panel.Group.alpha = 1f - t;
                panel.Rail.anchoredPosition = railHome + new Vector2(-RailSlide * t, 0f);
                yield return null;
            }

            panel.Group.alpha = 0f;
            panel.Rail.anchoredPosition = railHome;
            panel.Root.SetActive(false);

            // 화면이 사라진 다음에 시간을 푼다. 먼저 풀면 메뉴가 열린 채로 한
            // 프레임이 굴러가서, 재개를 누른 자리에서 총이 나가는 것처럼 보인다.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
            ReleasePlayer();

            fade = null;
        }
    }
}
