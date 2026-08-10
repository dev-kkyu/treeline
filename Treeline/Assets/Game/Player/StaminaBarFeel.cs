using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 스태미너 막대. 쓴 만큼이 뒤에서 붉게 따라 줄고, 바닥나면 떨고, 다 차면 한 번 훑는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>지연 막대</b>가 이 컴포넌트의 핵심이다. 막대 하나만 있으면 「지금 얼마 남았나」만
    /// 보이고 <b>방금 얼마를 썼는지</b>는 안 보인다. 뒤에 한 박자 늦게 따라오는 붉은
    /// 막대를 두면 그 차이가 그대로 길이가 되어, 달리기를 멈춘 순간 자기가 쓴 양을 본다.
    /// 줄 때는 늦게 따라오고 <b>찰 때는 곧장 따라붙는다</b> — 회복은 앞선 막대가 보여
    /// 주면 되고, 뒤에서 또 자라면 두 막대가 경주하는 것으로 보인다.
    /// </para>
    /// <para>
    /// 다 찼을 때의 <b>훑는 빛</b>은 「이제 다시 달릴 수 있다」를 알리는 자리다. 게이지는
    /// 늘 떠 있어서(<c>docs/tech-design/decisions.md</c>) 가득 찬 것이 눈에 안 띈다.
    /// <see cref="RectMask2D"/> 를 씌운 좁은 창을 왼쪽에서 오른쪽으로 밀어, 그 창 안에서만
    /// 같은 그림이 밝게 보이게 한다 — 사각형을 지나가게 하면 막대 모양을 벗어난다.
    /// </para>
    /// <para>
    /// 잠기는 것(<see cref="PlayerMotor.IsRecoveryLocked"/>)은 <see cref="PlayerHud"/> 가
    /// 스프라이트를 갈아 끼워 이미 알리고 있다. 여기서는 <b>잠기는 순간</b>만 떨어 준다 —
    /// 계속 떨면 상태가 아니라 고장으로 읽힌다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class StaminaBarFeel : MonoBehaviour
    {
        /// <summary>줄어든 뒤 지연 막대가 따라오기 시작할 때까지(초).</summary>
        private const float LagHoldSeconds = 0.28f;

        /// <summary>따라오는 속도(초당 비율). 한 칸을 대략 0.8 초에 메운다.</summary>
        private const float LagSpeed = 1.25f;

        private static readonly Color LagColor = new Color(0.78f, 0.24f, 0.20f, 0.85f);

        /// <summary>바닥난 순간 좌우로 떠는 거리(px)와 길이.</summary>
        private const float ShakeDistance = 9f;

        private const float ShakeSeconds = 0.4f;

        /// <summary>가득 찬 순간 훑는 빛. 창의 폭과 지나는 시간.</summary>
        private const float SweepWidth = 72f;

        private const float SweepSeconds = 0.5f;

        private const float SweepAlpha = 0.9f;

        /// <summary>
        /// 훑는 빛의 색. <b>흰빛으로는 안 보인다</b> — 가득 찬 막대가 이미 흰색이라
        /// 흰 띠를 지나가게 하면 아무 일도 안 일어난 것처럼 보인다. 눈 덮인 산의
        /// 푸른빛이라 색은 겉돌지 않으면서 흰 바탕에서 또렷하게 갈린다.
        /// </summary>
        private static readonly Color SweepColor = new Color(0.45f, 0.80f, 1f, 1f);

        /// <summary>가득 찬 순간 게이지 전체가 부푸는 비율. 색을 못 보는 눈에도 보인다.</summary>
        private const float ReadyPunch = 0.055f;

        private const float ReadySeconds = 0.45f;

        /// <summary>이 위를 넘으면 「가득」으로 본다. 정확히 1 을 기다리면 놓치는 프레임이 있다.</summary>
        private const float FullRatio = 0.999f;

        private Image fill;
        private RectTransform fillRect;
        private RectTransform groupRect;
        private Vector2 groupHome;
        private Image lag;
        private RectTransform sweepWindow;
        private Image sweepGlow;

        private float ratio = 1f;
        private float lagRatio = 1f;
        private float lagHoldUntil;
        private float shakeStart = float.NegativeInfinity;
        private float sweepStart = float.NegativeInfinity;
        private bool wasLocked;
        private bool wasFull = true;

        /// <summary>스태미너 채움에 붙인다. 이미 붙어 있으면 그것을 돌려준다.</summary>
        public static StaminaBarFeel AttachTo(Image staminaFill)
        {
            if (staminaFill == null)
            {
                return null;
            }

            StaminaBarFeel feel = staminaFill.GetComponent<StaminaBarFeel>();
            return feel != null ? feel : staminaFill.gameObject.AddComponent<StaminaBarFeel>();
        }

        /// <summary>남은 비율과 잠김 여부를 넘긴다. <see cref="PlayerHud"/> 가 매 프레임 부른다.</summary>
        public void Set(float current, bool locked)
        {
            current = Mathf.Clamp01(current);

            if (current < ratio)
            {
                // 아직 줄고 있는 동안에는 뒤 막대를 붙잡아 둔다. 멈춰야 차이가 보인다.
                lagHoldUntil = Time.time + LagHoldSeconds;
            }
            else if (current > lagRatio)
            {
                lagRatio = current;
            }

            bool full = current >= FullRatio;
            if (full && !wasFull && !locked)
            {
                sweepStart = Time.time;
            }

            wasFull = full;

            if (locked && !wasLocked)
            {
                shakeStart = Time.time;
            }

            wasLocked = locked;
            ratio = current;
        }

        private void Awake()
        {
            fill = GetComponent<Image>();
            fillRect = fill.rectTransform;

            // 떨림과 부풂은 게이지 전체에 준다. 채움만 흔들면 테두리는 가만히 있어
            // 안이 헐거워진 것처럼 보인다. 크기를 바꿀 것이므로 피벗을 한가운데로 옮긴다.
            groupRect = (RectTransform)fillRect.parent;
            HudFeel.SetPivot(groupRect, new Vector2(0.5f, 0.5f));
            groupHome = groupRect.anchoredPosition;

            lag = HudFeel.CreateGhost(fill, "StaminaLag");
            lag.color = LagColor;
            lag.enabled = true;

            BuildSweep();
        }

        /// <summary>훑는 빛. 좁은 창 안에 같은 그림을 두고, 창만 움직인다.</summary>
        private void BuildSweep()
        {
            var windowGo = new GameObject("StaminaSweep", typeof(RectTransform));
            sweepWindow = (RectTransform)windowGo.transform;
            sweepWindow.SetParent(fillRect.parent, false);
            sweepWindow.anchorMin = fillRect.anchorMin;
            sweepWindow.anchorMax = fillRect.anchorMax;
            sweepWindow.pivot = new Vector2(0.5f, 0.5f);
            sweepWindow.sizeDelta = new Vector2(SweepWidth, fillRect.sizeDelta.y);
            windowGo.AddComponent<RectMask2D>();

            var glowGo = new GameObject("Glow", typeof(RectTransform));
            var glowRect = (RectTransform)glowGo.transform;
            glowRect.SetParent(sweepWindow, false);
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = fillRect.sizeDelta;

            sweepGlow = glowGo.AddComponent<Image>();
            sweepGlow.sprite = fill.sprite;
            sweepGlow.type = fill.type;
            sweepGlow.fillMethod = fill.fillMethod;
            sweepGlow.fillOrigin = fill.fillOrigin;
            sweepGlow.fillAmount = 1f;
            sweepGlow.raycastTarget = false;

            windowGo.SetActive(false);
        }

        private void Update()
        {
            UpdateLag();
            UpdateShake();
            UpdateSweep();
        }

        private void UpdateLag()
        {
            if (lagRatio > ratio && Time.time >= lagHoldUntil)
            {
                lagRatio = Mathf.Max(ratio, lagRatio - LagSpeed * Time.deltaTime);
            }
            else if (lagRatio < ratio)
            {
                lagRatio = ratio;
            }

            // 앞 막대와 같아지면 그릴 것이 없다. 늘 켜 두면 테두리가 한 겹 두꺼워 보인다.
            bool draw = lagRatio > ratio + 0.001f;
            if (lag.enabled != draw)
            {
                lag.enabled = draw;
            }

            if (draw)
            {
                lag.sprite = fill.sprite;
                lag.fillAmount = lagRatio;
            }
        }

        private void UpdateShake()
        {
            float shake = HudFeel.Punch(shakeStart, ShakeDistance, ShakeSeconds, 42f, 9f);
            groupRect.anchoredPosition = groupHome + new Vector2(shake, 0f);

            float ready = HudFeel.Punch(sweepStart, ReadyPunch, ReadySeconds);
            float scale = 1f + ready;
            groupRect.localScale = new Vector3(scale, scale, 1f);
        }

        private void UpdateSweep()
        {
            float t = Time.time - sweepStart;
            bool draw = t >= 0f && t < SweepSeconds;

            if (sweepWindow.gameObject.activeSelf != draw)
            {
                sweepWindow.gameObject.SetActive(draw);
            }

            if (!draw)
            {
                return;
            }

            float k = t / SweepSeconds;
            float half = fillRect.sizeDelta.x * 0.5f + SweepWidth;
            float x = Mathf.Lerp(-half, half, k * k * (3f - 2f * k));

            sweepWindow.anchoredPosition = new Vector2(x, 0f);
            // 창 안의 그림은 제자리에 있어야 막대가 흐르지 않고 빛만 지나간다.
            ((RectTransform)sweepGlow.transform).anchoredPosition = new Vector2(-x, 0f);

            Color color = SweepColor;
            color.a = SweepAlpha * Mathf.Sin(Mathf.PI * k);
            sweepGlow.color = color;
            sweepGlow.sprite = fill.sprite;
        }
    }
}
