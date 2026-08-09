using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 화면 위아래에서 닫히는 검은 띠. 6장 엔딩이 이것으로 끝난다.
    /// </summary>
    /// <remarks>
    /// 코드로 만든다. 씬에 두면 <see cref="CableCarDeparture"/> 가 출발할 때
    /// <b>모든 <see cref="Canvas"/> 를 끄는 것</b>에 같이 걸려서, 정작 필요한 순간에
    /// 꺼진 채로 있게 된다. 그 뒤에 만들면 그 문제가 없다.
    /// <para>
    /// 비율(0 ~ 0.5)로 잡는다. 0.5 면 위아래 띠가 화면 한가운데에서 만나 완전히
    /// 검어진다. 해상도를 안 보고 앵커만 움직이므로 어떤 화면비에서도 같다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Letterbox : MonoBehaviour
    {
        /// <summary>완전히 닫힌 비율. 위아래가 각각 절반씩이다.</summary>
        public const float Closed = 0.5f;

        private RectTransform top;
        private RectTransform bottom;
        private float height;

        /// <summary>띠를 만들어 붙인다. 처음에는 열려 있다.</summary>
        public static Letterbox Create()
        {
            var go = new GameObject(nameof(Letterbox));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 남아 있을지 모르는 다른 UI 위에 덮는다. 엔딩에서 이것보다 위에
            // 그려져야 하는 것은 없다.
            canvas.sortingOrder = 500;

            var box = go.AddComponent<Letterbox>();
            box.top = box.CreateBar("Top");
            box.bottom = box.CreateBar("Bottom");
            box.Apply(0f);
            return box;
        }

        /// <summary>지금 비율에서 목표 비율까지 옮긴다.</summary>
        public IEnumerator To(float target, float seconds)
        {
            float from = height;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                Apply(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / seconds)));
                yield return null;
            }

            Apply(target);
        }

        private RectTransform CreateBar(string barName)
        {
            var go = new GameObject(barName, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            Image image = go.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            return (RectTransform)go.transform;
        }

        private void Apply(float value)
        {
            height = Mathf.Clamp(value, 0f, Closed);

            top.anchorMin = new Vector2(0f, 1f - height);
            top.anchorMax = Vector2.one;
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = new Vector2(1f, height);

            top.offsetMin = Vector2.zero;
            top.offsetMax = Vector2.zero;
            bottom.offsetMin = Vector2.zero;
            bottom.offsetMax = Vector2.zero;
        }
    }
}
