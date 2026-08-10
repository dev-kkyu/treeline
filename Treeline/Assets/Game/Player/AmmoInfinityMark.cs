using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 탄약 HUD 오른쪽 끝의 여분 표시. 이 게임의 여분은 무한이라 셀 숫자가 없고,
    /// 그 자리를 <c>∞</c> 하나가 지킨다. 이 컴포넌트가 그 글자를 살아 있게 한다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 가만히 있는 기호는 「값이 없다」나 「표시가 고장났다」로 읽힌다. 눈금이 한 칸씩
    /// 줄어드는 동안 <b>이것만 안 줄어든다</b>는 것이 「여분은 무한」의 유일한 신호이므로,
    /// 쏠 때마다 같이 튀게 해서 줄지 않는다는 사실 자체를 눈에 보이게 만든다.
    /// 탄창이 바닥나거나 다시 찰 때는 크게 뛰고 잔상을 하나 남긴다 — 플레이어가
    /// 「여분이 있나」를 실제로 궁금해하는 순간이 거기다.
    /// </para>
    /// <para>
    /// IMPORTANT: 글자를 키웠다 줄이므로 <b>피벗을 글자 한가운데로</b> 옮긴다. 상자
    /// 피벗(오른쪽 아래) 그대로 키우면 왼쪽 위로만 자라서, 튀는 것이 아니라 밀리는
    /// 것으로 보인다. 글자 폭은 문구마다 다르니 문구가 바뀔 때마다 다시 잰다. 옮긴
    /// 만큼 <see cref="RectTransform.anchoredPosition"/> 을 되돌려 상자는 제자리에 둔다.
    /// </para>
    /// <para>
    /// 시간은 <see cref="Time.deltaTime"/> 계열이다. 멈춤 화면에서 HUD 가 얼어 있어야
    /// 뒤에 멈춘 화면이라는 것이 읽힌다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AmmoInfinityMark : MonoBehaviour
    {
        /// <summary>
        /// 「x」는 작게, 「∞」는 기본 크기로. 숫자 자리에 오는 것은 ∞ 쪽이다.
        /// 사이의 공백은 크기를 따로 준다 — 붙여 두면 두 글자가 한 덩어리로 뭉쳐 보인다.
        /// </summary>
        private const string InfiniteMarkup = "<size=22>x</size><size=20> </size>∞";

        private const string ReloadingMarkup = "재장전";

        /// <summary>평상시 숨. 느리게 밝아졌다 어두워진다.</summary>
        private const float BreathPeriod = 2.6f;

        /// <summary>재장전 중에는 빠르게 뛴다. 「진행 중」은 상태가 아니라 사건이다.</summary>
        private const float ReloadBreathPeriod = 0.75f;

        private const float BreathScale = 0.022f;
        private const float BreathAlphaLow = 0.76f;

        /// <summary>한 발 쐈을 때의 튐. 눈금 하나가 꺼지는 것과 같은 무게다.</summary>
        private const float ShotPunch = 0.18f;

        /// <summary>바닥나거나 다시 찼을 때의 튐.</summary>
        private const float SurgePunch = 0.5f;

        private const float PunchSeconds = 0.55f;

        /// <summary>튐의 흔들림(rad/s)과 잦아드는 속도. 한 번 크게, 한 번 작게 흔들린다.</summary>
        private const float PunchWobble = 18f;

        private const float PunchDamp = 8.5f;

        private const float EchoSeconds = 0.42f;
        private const float EchoGrowth = 0.9f;
        private const float EchoAlpha = 0.55f;

        private const float FlashSeconds = 0.45f;

        /// <summary>강조할 때 스치는 색. 눈 덮인 산의 빛이라 흰빛에 파랑을 조금 섞었다.</summary>
        private static readonly Color SurgeTint = new Color(0.78f, 0.92f, 1f, 1f);

        private Text label;
        private RectTransform rect;
        private Text echo;
        private RectTransform echoRect;

        private Color baseColor = Color.white;
        private bool reloading;

        private float punchStart = float.NegativeInfinity;
        private float punchAmp;
        private float echoStart = float.NegativeInfinity;
        private float flashStart = float.NegativeInfinity;

        /// <summary>여분 문구에 붙인다. 이미 붙어 있으면 그것을 돌려준다.</summary>
        public static AmmoInfinityMark AttachTo(Text reserve)
        {
            if (reserve == null)
            {
                return null;
            }

            AmmoInfinityMark mark = reserve.GetComponent<AmmoInfinityMark>();
            return mark != null ? mark : reserve.gameObject.AddComponent<AmmoInfinityMark>();
        }

        /// <summary>무한 기호를 띄운다.</summary>
        public void ShowInfinite()
        {
            SetLabel(InfiniteMarkup, false);
        }

        /// <summary>재장전 중임을 띄운다.</summary>
        public void ShowReloading()
        {
            SetLabel(ReloadingMarkup, true);
        }

        /// <summary>총을 안 들었다. 아무것도 띄우지 않는다.</summary>
        public void ShowNothing()
        {
            SetLabel(string.Empty, false);
        }

        /// <summary>한 발 나갔다. 작게 튄다.</summary>
        public void Nudge()
        {
            Punch(ShotPunch);
        }

        /// <summary>바닥났거나 다시 찼다. 크게 튀고 잔상과 빛을 남긴다.</summary>
        public void Surge()
        {
            Punch(SurgePunch);
            echoStart = Time.time;
            flashStart = Time.time;
        }

        private void Awake()
        {
            label = GetComponent<Text>();
            rect = (RectTransform)transform;
            baseColor = label.color;

            BuildEcho();
            SetLabel(InfiniteMarkup, false);
        }

        /// <summary>
        /// 잔상용 복제본. 뒤에 그려야 원본 글자가 흐려지지 않으므로 형제 순서를 앞에 둔다.
        /// </summary>
        private void BuildEcho()
        {
            var go = new GameObject("ReserveEcho", typeof(RectTransform));
            echoRect = (RectTransform)go.transform;
            echoRect.SetParent(rect.parent, false);
            echoRect.SetSiblingIndex(rect.GetSiblingIndex());

            echo = go.AddComponent<Text>();
            echo.font = label.font;
            echo.fontSize = label.fontSize;
            echo.fontStyle = label.fontStyle;
            echo.alignment = label.alignment;
            echo.supportRichText = label.supportRichText;
            echo.horizontalOverflow = label.horizontalOverflow;
            echo.verticalOverflow = label.verticalOverflow;
            echo.lineSpacing = label.lineSpacing;
            echo.raycastTarget = false;
            echo.enabled = false;
        }

        private void SetLabel(string markup, bool isReloading)
        {
            reloading = isReloading;

            if (label.text != markup)
            {
                label.text = markup;
                echo.text = markup;
                Recenter();
            }
        }

        /// <summary>글자 한가운데로 피벗을 옮긴다. 상자가 움직이지 않도록 이동분을 되돌린다.</summary>
        private void Recenter()
        {
            Vector2 size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            // 아래 오른쪽 정렬이라 글자의 오른쪽 아래가 상자의 오른쪽 아래에 붙는다.
            // 그래서 글자 한가운데는 상자 오른쪽에서 폭의 절반, 바닥에서 높이의 절반이다.
            var pivot = new Vector2(
                Mathf.Clamp01(1f - label.preferredWidth / (2f * size.x)),
                Mathf.Clamp01(label.preferredHeight / (2f * size.y)));

            Vector2 delta = pivot - rect.pivot;
            rect.pivot = pivot;
            rect.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);

            echoRect.anchorMin = rect.anchorMin;
            echoRect.anchorMax = rect.anchorMax;
            echoRect.pivot = rect.pivot;
            echoRect.sizeDelta = rect.sizeDelta;
            echoRect.anchoredPosition = rect.anchoredPosition;
        }

        private void Update()
        {
            float period = reloading ? ReloadBreathPeriod : BreathPeriod;
            float breath = Mathf.Sin(Time.time * (Mathf.PI * 2f / period));

            float scale = 1f + BreathScale * breath + PunchScale();
            rect.localScale = new Vector3(scale, scale, 1f);

            float flash = Flash();
            Color color = Color.Lerp(baseColor, SurgeTint, flash);
            color.a = Mathf.Max(Mathf.Lerp(BreathAlphaLow, 1f, 0.5f + 0.5f * breath), flash);
            label.color = color;

            UpdateEcho(scale);
        }

        private void Punch(float amp)
        {
            // 겹쳐 맞으면 더 크게. 다만 새로 시작하는 편이 잔물결이 남는 것보다 선명하다.
            punchAmp = Mathf.Min(SurgePunch, Mathf.Max(amp, punchAmp * 0.5f));
            punchStart = Time.time;
        }

        private float PunchScale()
        {
            float t = Time.time - punchStart;
            if (t < 0f || t >= PunchSeconds)
            {
                return 0f;
            }

            // cos 라 첫 프레임이 곧 정점이다. 커지는 과정을 보여 주면 「자란다」가 되고,
            // 그건 총을 쏜 것이 아니라 값이 올라간 것으로 읽힌다. 그 뒤 지수로 잦아들며
            // 한 번 되튄다.
            return punchAmp * Mathf.Cos(t * PunchWobble) * Mathf.Exp(-t * PunchDamp);
        }

        private float Flash()
        {
            float t = Time.time - flashStart;
            if (t < 0f || t >= FlashSeconds)
            {
                return 0f;
            }

            float k = 1f - t / FlashSeconds;
            return k * k;
        }

        private void UpdateEcho(float baseScale)
        {
            float t = Time.time - echoStart;
            bool draw = t >= 0f && t < EchoSeconds;

            if (echo.enabled != draw)
            {
                echo.enabled = draw;
            }

            if (!draw)
            {
                return;
            }

            float k = t / EchoSeconds;
            float grown = baseScale * (1f + EchoGrowth * (1f - (1f - k) * (1f - k)));
            echoRect.localScale = new Vector3(grown, grown, 1f);

            Color color = SurgeTint;
            color.a = EchoAlpha * (1f - k) * (1f - k);
            echo.color = color;
        }
    }
}
