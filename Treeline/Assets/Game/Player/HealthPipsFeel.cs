using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 체력 칸 셋. 한 칸이 꺼질 때 그 칸이 터지듯 튀고, 마지막 한 칸은 심장처럼 뛴다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 칸이 스프라이트만 조용히 바뀌면 <b>맞은 순간에 아무 일도 안 일어난다.</b> 화면
    /// 왼쪽 아래는 시선이 없는 자리라, 거기서 값이 바뀐 것은 눈에 띄지 않는다. 그래서
    /// 꺼지는 칸을 한 번 크게 튀기고 잔상을 남겨, 「방금 하나 없어졌다」를 사건으로 만든다.
    /// </para>
    /// <para>
    /// 마지막 한 칸의 <b>심장 박동</b>이 이 컴포넌트의 요점이다. 숫자를 안 쓰는 HUD 라
    /// 「한 대 더 맞으면 죽는다」를 말할 자리가 여기밖에 없다. 두근-두근 두 번씩 뛰는
    /// 리듬이라 규칙적인 점멸과 구분되고, 공포에서 규칙적인 점멸은 기계가 내는 신호로
    /// 읽힌다 — 이건 몸이 내는 신호여야 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: 칸은 원래 왼쪽 가운데가 피벗이라 그대로 키우면 오른쪽으로만 자란다.
    /// 붙을 때 <see cref="HudFeel.SetPivot"/> 로 한가운데로 옮긴다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HealthPipsFeel : MonoBehaviour
    {
        /// <summary>칸이 꺼질 때의 튐과 그 잔상.</summary>
        private const float LossPunch = 0.45f;

        private const float LossSeconds = 0.55f;
        private const float GhostSeconds = 0.4f;
        private const float GhostGrowth = 0.85f;
        private const float GhostAlpha = 0.55f;

        /// <summary>회복으로 켜지는 칸. 왼쪽부터 이 간격으로 하나씩 터진다.</summary>
        private const float GainPunch = 0.35f;

        private const float GainStagger = 0.08f;

        /// <summary>맞은 순간 칸 전체가 밀리는 거리(px)와 되돌아오는 시간.</summary>
        private const float ShoveDistance = 14f;

        private const float ShoveSeconds = 0.45f;

        /// <summary>심장 한 박자. 두근(0.00) · 두근(0.24) 뒤 쉰다.</summary>
        private const float HeartPeriod = 1.15f;

        private const float HeartFirstWidth = 0.17f;
        private const float HeartSecondWidth = 0.15f;
        private const float HeartSecondAt = 0.24f;
        private const float HeartScale = 0.15f;

        /// <summary>꺼질 때 스치는 색. 붉은 것은 피가 아니라 경고다 — 눈금 경고색과 같다.</summary>
        private static readonly Color LossTint = new Color(0.86f, 0.26f, 0.22f, 1f);

        /// <summary>마지막 한 칸이 뛸 때의 색.</summary>
        private static readonly Color HeartTint = new Color(0.92f, 0.34f, 0.30f, 1f);

        private RectTransform groupRect;
        private Vector2 groupHome;
        private Image[] pips;
        private RectTransform[] pipRects;
        private Image[] ghosts;
        private Sprite activeSprite;
        private Sprite emptySprite;

        private float[] punchStart;
        private float[] punchAmp;
        private float[] ghostStart;
        private float shoveStart = float.NegativeInfinity;
        private int shown = -1;

        /// <summary>체력 칸 묶음에 붙인다. 이미 붙어 있으면 그것을 돌려준다.</summary>
        public static HealthPipsFeel AttachTo(RectTransform group, Image[] pips, Sprite active, Sprite empty)
        {
            if (group == null || pips == null || pips.Length == 0)
            {
                return null;
            }

            HealthPipsFeel feel = group.GetComponent<HealthPipsFeel>();
            if (feel == null)
            {
                feel = group.gameObject.AddComponent<HealthPipsFeel>();
            }

            feel.Build(group, pips, active, empty);
            return feel;
        }

        /// <summary>애니메이션 없이 현재 값으로 맞춘다. 장이 시작할 때 쓴다.</summary>
        public void Show(int current)
        {
            shown = current;
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] != null)
                {
                    pips[i].sprite = i < current ? activeSprite : emptySprite;
                }
            }
        }

        /// <summary>값을 바꾼다. 줄어든 칸은 터지고, 늘어난 칸은 차례로 켜진다.</summary>
        public void Set(int current)
        {
            int before = shown < 0 ? current : shown;
            Show(current);

            if (current < before)
            {
                // 없어진 칸들. 오른쪽 것부터 사라지므로 그 자리에서 터뜨린다.
                for (int i = current; i < before && i < pips.Length; i++)
                {
                    punchStart[i] = Time.time;
                    punchAmp[i] = LossPunch;
                    ghostStart[i] = Time.time;
                }

                shoveStart = Time.time;
            }
            else if (current > before)
            {
                for (int i = before; i < current && i < pips.Length; i++)
                {
                    punchStart[i] = Time.time + (i - before) * GainStagger;
                    punchAmp[i] = GainPunch;
                }
            }
        }

        private void Build(RectTransform group, Image[] sourcePips, Sprite active, Sprite empty)
        {
            if (pips != null)
            {
                return;
            }

            groupRect = group;
            groupHome = group.anchoredPosition;
            pips = sourcePips;
            activeSprite = active;
            emptySprite = empty;

            pipRects = new RectTransform[pips.Length];
            ghosts = new Image[pips.Length];
            punchStart = new float[pips.Length];
            punchAmp = new float[pips.Length];
            ghostStart = new float[pips.Length];

            for (int i = 0; i < pips.Length; i++)
            {
                punchStart[i] = float.NegativeInfinity;
                punchAmp[i] = LossPunch;
                ghostStart[i] = float.NegativeInfinity;

                if (pips[i] == null)
                {
                    continue;
                }

                pipRects[i] = pips[i].rectTransform;
                HudFeel.SetPivot(pipRects[i], new Vector2(0.5f, 0.5f));
                ghosts[i] = HudFeel.CreateGhost(pips[i], "PipGhost");
                ghosts[i].sprite = activeSprite;
            }
        }

        private void Update()
        {
            float heart = Heartbeat();

            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null)
                {
                    continue;
                }

                bool lit = i < shown;
                float punch = HudFeel.Punch(punchStart[i], punchAmp[i], LossSeconds);

                // 심장은 켜져 있는 칸만 뛴다. 마지막 한 칸일 때만 값이 0 이 아니다.
                float beat = lit ? heart : 0f;

                float scale = 1f + punch + HeartScale * beat;
                pipRects[i].localScale = new Vector3(scale, scale, 1f);

                Color color = Color.white;
                float flare = punchAmp[i] > 0f ? Mathf.Abs(punch) / punchAmp[i] : 0f;
                if (!lit)
                {
                    color = Color.Lerp(color, LossTint, flare * 0.8f);
                }
                else
                {
                    color = Color.Lerp(color, HeartTint, Mathf.Max(beat * 0.55f, flare * 0.5f));
                }

                pips[i].color = color;

                UpdateGhost(i, scale);
            }

            float shove = HudFeel.Punch(shoveStart, ShoveDistance, ShoveSeconds);
            groupRect.anchoredPosition = groupHome + new Vector2(-shove, -shove * 0.35f);
        }

        private void UpdateGhost(int index, float baseScale)
        {
            Image ghost = ghosts[index];
            if (ghost == null)
            {
                return;
            }

            float t = Time.time - ghostStart[index];
            bool draw = t >= 0f && t < GhostSeconds;

            if (ghost.enabled != draw)
            {
                ghost.enabled = draw;
            }

            if (!draw)
            {
                return;
            }

            float k = t / GhostSeconds;
            float grown = baseScale * (1f + GhostGrowth * (1f - (1f - k) * (1f - k)));
            ghost.rectTransform.localScale = new Vector3(grown, grown, 1f);

            Color color = LossTint;
            color.a = GhostAlpha * (1f - k) * (1f - k);
            ghost.color = color;
        }

        /// <summary>마지막 한 칸일 때만 뛴다. 두근-두근 뒤 쉬는 리듬이다.</summary>
        private float Heartbeat()
        {
            if (shown != 1)
            {
                return 0f;
            }

            float p = Mathf.Repeat(Time.time, HeartPeriod);
            return HudFeel.Bump(p, HeartFirstWidth)
                + 0.55f * HudFeel.Bump(p - HeartSecondAt, HeartSecondWidth);
        }
    }
}
