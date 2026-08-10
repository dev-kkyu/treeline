using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 화면을 검게 덮고 다시 여는 한 장. 죽으면 닫고 장이 다시 열리면 연다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>씬을 넘어 살아남는다.</b> 이것이 <see cref="PauseScreen"/> 과 다른 점이다 —
    /// 멈춤 화면은 씬과 함께 죽어야 <c>EventSystem</c> 이 둘이 되지 않지만, 이 장은
    /// <b>씬이 갈리는 순간을 덮는 것이 일</b>이라 살아 있어야 한다. 누를 것이 없어
    /// (<c>raycastTarget</c> 이 꺼져 있다) 겹쳐도 입력을 뺏지 않는다.
    /// </para>
    /// <para>
    /// 시간은 <see cref="Time.unscaledDeltaTime"/> 이다. 죽는 동안 시간이 멈추는 연출이
    /// 들어와도 화면은 계속 닫혀야 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: 붙잡혀 죽는 것은 <see cref="PlayerHud.Blackout(float)"/> 가 0.05 초에
    /// 딱 닫는다. 그 연출을 페이드로 바꾸지 않는다 — 「서서히 죽는다」가 되어 버린다.
    /// 여기서 하는 것은 그 뒤, <b>장이 다시 열릴 때까지 검은 화면을 이어 주는 것</b>이다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ScreenFade : MonoBehaviour
    {
        /// <summary>죽고 나서 화면이 닫히기 시작할 때까지. 무엇에 죽었는지 볼 참이다.</summary>
        private const float DeathHoldSeconds = 0.25f;

        private const float DeathFadeSeconds = 0.9f;
        private const float WakeFadeSeconds = 0.9f;

        /// <summary>장이 열리고 이만큼 두었다가 연다. 첫 프레임의 버벅임을 덮는다.</summary>
        private const float WakeDelaySeconds = 0.15f;

        private static ScreenFade instance;

        private Image veil;
        private Coroutine running;
        private PlayerHealth watched;

        /// <summary>지금 화면이 덮여 있는지. 0 보다 크면 무엇이든 가리고 있다.</summary>
        public static bool IsCovered => instance != null && instance.veil.color.a > 0.001f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("[ScreenFade]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ScreenFade>();
            instance.Build();

            SceneManager.sceneLoaded += instance.OnSceneLoaded;

            // 첫 씬에는 sceneLoaded 가 오지 않는다. 에디터에서 장을 직접 플레이할 때가 여기다.
            instance.Watch();
        }

        /// <summary>화면을 닫는다. 이미 닫혀 있으면 아무 일도 하지 않는다.</summary>
        public static void Out(float seconds)
        {
            if (instance != null)
            {
                instance.Run(instance.FadeTo(1f, seconds));
            }
        }

        /// <summary>화면을 연다.</summary>
        public static void In(float seconds)
        {
            if (instance != null)
            {
                instance.Run(instance.FadeTo(0f, seconds));
            }
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 멈춤 화면(600)보다 위다. 죽는 순간은 무엇도 이 위에 없다.
            canvas.sortingOrder = 900;
            MenuChrome.ConfigureCanvas(canvas, gameObject.AddComponent<CanvasScaler>());

            veil = MenuChrome.CreateImage("Veil", transform, null, new Color(0f, 0f, 0f, 0f));
            MenuChrome.Stretch(veil.rectTransform);
            veil.raycastTarget = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Watch();

            // 덮어 둔 채로 넘어온 것은 여기서만 연다. 안 덮여 있었다면 아무 일도
            // 없던 것이므로 굳이 검은 화면에서 시작하지 않는다.
            if (veil.color.a > 0.001f)
            {
                Run(WakeRoutine());
            }
        }

        /// <summary>이 씬의 플레이어가 죽는 것을 듣는다. 장마다 새로 만들어지므로 매번 다시 건다.</summary>
        private void Watch()
        {
            if (watched != null)
            {
                watched.Died -= OnPlayerDied;
                watched = null;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return;
            }

            watched = player.GetComponent<PlayerHealth>();
            if (watched != null)
            {
                watched.Died += OnPlayerDied;
            }
        }

        private void OnPlayerDied()
        {
            Run(DeathRoutine());
        }

        private void Run(IEnumerator routine)
        {
            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(routine);
        }

        private IEnumerator DeathRoutine()
        {
            yield return new WaitForSecondsRealtime(DeathHoldSeconds);
            yield return FadeTo(1f, DeathFadeSeconds);
        }

        private IEnumerator WakeRoutine()
        {
            yield return new WaitForSecondsRealtime(WakeDelaySeconds);
            yield return FadeTo(0f, WakeFadeSeconds);
        }

        private IEnumerator FadeTo(float target, float seconds)
        {
            Color color = veil.color;
            float from = color.a;

            if (seconds <= 0f)
            {
                color.a = target;
                veil.color = color;
                running = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                color.a = Mathf.Lerp(from, target, t * t * (3f - 2f * t));
                veil.color = color;
                yield return null;
            }

            color.a = target;
            veil.color = color;
            running = null;
        }
    }
}
