using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 한 번 번쩍이고 사그라드는 빛. <see cref="HydrogenPocket"/> 이 붙인다.
    /// </summary>
    /// <remarks>
    /// 터뜨린 쪽에서 코루틴으로 돌리지 않는 이유가 있다. <b>가스는 터지는 순간
    /// 자기 오브젝트를 끄고</b>, 꺼진 오브젝트의 코루틴은 그 자리에서 죽는다.
    /// 그러면 빛이 최대 밝기로 굳은 채 남는다. 사그라드는 일은 빛 자신이 한다.
    /// </remarks>
    [RequireComponent(typeof(Light))]
    public sealed class FlashFade : MonoBehaviour
    {
        private Light lamp;
        private float peak;
        private float seconds;
        private float started = float.NegativeInfinity;

        private void Awake()
        {
            lamp = GetComponent<Light>();
        }

        public void Begin(float intensity, float overSeconds)
        {
            if (lamp == null)
            {
                lamp = GetComponent<Light>();
            }

            peak = intensity;
            seconds = Mathf.Max(0.01f, overSeconds);
            started = Time.time;

            lamp.enabled = true;
            lamp.intensity = peak;
        }

        private void Update()
        {
            if (float.IsNegativeInfinity(started))
            {
                return;
            }

            float t = (Time.time - started) / seconds;

            if (t >= 1f)
            {
                lamp.intensity = 0f;
                lamp.enabled = false;
                started = float.NegativeInfinity;
                return;
            }

            // 제곱으로 떨군다. 선형으로 주면 번쩍이 아니라 조명이 켜졌다 꺼진 것으로 보인다.
            float left = 1f - t;
            lamp.intensity = peak * left * left;
        }
    }
}
