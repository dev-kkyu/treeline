using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// HUD 가 반응할 때 쓰는 공통 계산. 튐 곡선 · 피벗 옮기기 · 잔상 만들기.
    /// </summary>
    /// <remarks>
    /// 같은 계산을 탄약 · 체력 · 스태미너가 각자 갖고 있으면 세 곳의 박자가 따로 논다.
    /// 화면 아래쪽이 한 몸으로 반응해야 「내 상태」로 읽히므로 곡선을 한 벌만 둔다.
    /// </remarks>
    public static class HudFeel
    {
        /// <summary>
        /// 튐. 시작 순간이 곧 정점이고, 지수로 잦아들며 한 번 되튄다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <c>cos</c> 인 것이 중요하다. <c>sin</c> 은 0 에서 시작해 커지는
        /// 과정이 보이는데, 그러면 「자란다」로 읽혀 사건이 아니라 값의 변화처럼 보인다.
        /// </remarks>
        public static float Punch(float startTime, float amp, float seconds, float wobble = 18f, float damp = 8.5f)
        {
            float t = Time.time - startTime;
            if (t < 0f || t >= seconds)
            {
                return 0f;
            }

            return amp * Mathf.Cos(t * wobble) * Mathf.Exp(-t * damp);
        }

        /// <summary>0 에서 시작해 <paramref name="width"/> 동안 부풀었다 꺼지는 한 번의 봉우리.</summary>
        public static float Bump(float elapsed, float width)
        {
            if (elapsed < 0f || elapsed > width)
            {
                return 0f;
            }

            return Mathf.Sin(Mathf.PI * elapsed / width);
        }

        /// <summary>
        /// 상자를 제자리에 둔 채 피벗만 옮긴다. 크기를 바꿀 기준점을 잡을 때 쓴다.
        /// </summary>
        /// <remarks>
        /// 피벗을 그냥 바꾸면 상자가 그만큼 움직인다. 옮긴 비율에 크기를 곱해
        /// <see cref="RectTransform.anchoredPosition"/> 으로 되돌려야 화면에서 그대로 있다.
        /// </remarks>
        public static void SetPivot(RectTransform rect, Vector2 pivot)
        {
            Vector2 size = rect.rect.size;
            Vector2 delta = pivot - rect.pivot;
            rect.pivot = pivot;
            rect.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        }

        /// <summary>
        /// 같은 그림의 잔상 한 장을 원본 <b>뒤에</b> 만든다. 평소에는 꺼 둔다.
        /// </summary>
        /// <remarks>
        /// 뒤에 두는 것은 부풀며 사라지는 동안 원본 글자·그림이 흐려지지 않게 하기
        /// 위해서다. 형제 순서가 곧 그리는 순서다.
        /// </remarks>
        public static Image CreateGhost(Image source, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            var from = source.rectTransform;

            rect.SetParent(from.parent, false);
            rect.SetSiblingIndex(from.GetSiblingIndex());
            rect.anchorMin = from.anchorMin;
            rect.anchorMax = from.anchorMax;
            rect.pivot = from.pivot;
            rect.sizeDelta = from.sizeDelta;
            rect.anchoredPosition = from.anchoredPosition;

            var ghost = go.AddComponent<Image>();
            ghost.sprite = source.sprite;
            ghost.type = source.type;
            ghost.fillMethod = source.fillMethod;
            ghost.fillOrigin = source.fillOrigin;
            ghost.fillAmount = source.fillAmount;
            ghost.preserveAspect = source.preserveAspect;
            ghost.raycastTarget = false;
            ghost.enabled = false;

            return ghost;
        }
    }
}
