using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 눈보라에 강약을 준다. 몰아쳤다 잦아들었다 한다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 일정한 속도로만 내리는 눈은 몇 초만 보면 <b>배경 무늬</b>가 된다. 세기가
    /// 변해야 눈보라로 보이고, 무엇보다 <b>화면이 살아 있다</b>고 느껴진다.
    /// 타이틀과 엔딩은 플레이어가 오래 보고 있는 화면이라 이 차이가 크다.
    /// </para>
    /// <para>
    /// 주기를 하나만 쓰면 규칙적으로 오르내려 기계처럼 보인다. 서로 나누어
    /// 떨어지지 않는 두 주기를 겹쳐 <b>언제 세질지 예측이 안 되게</b> 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: 파티클의 <c>velocityOverLifetime</c> 은 <b>세 축의 모드가 같아야
    /// 한다.</b> 한 축만 건드리면 인스펙터가 「Particle Velocity curves must all be
    /// in the same mode」 를 띄운다. 여기서 x 만 바꾸고 싶어도 셋을 함께 넣는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SnowGusts : MonoBehaviour
    {
        [Header("무엇을 흔드나")]
        [Tooltip("비우면 자식에서 전부 찾는다.")]
        [SerializeField] private ParticleSystem[] systems;

        [Tooltip("같이 커졌다 작아질 소리. 눈보라 앰비언스를 넣는다. 비워도 된다.")]
        [SerializeField] private AudioSource wind;

        [Header("세기")]
        [Tooltip("잦아들 때의 배율. 0.55 면 평소의 55% 까지 약해진다.")]
        [SerializeField, Range(0.2f, 1f)] private float lull = 0.55f;

        [Tooltip("몰아칠 때의 배율.")]
        [SerializeField, Range(1f, 3f)] private float gust = 1.75f;

        [Tooltip("옆으로 미는 힘의 최대치(m/s). 세질 때만 커진다.")]
        [SerializeField] private float sideWind = 2.4f;

        [Header("주기(초) — 서로 나누어떨어지지 않게 둔다")]
        [SerializeField] private Vector2 periods = new Vector2(11f, 17f);

        private float[] baseRate;
        private Vector4[] baseVelY;   // x=최소 y=최대 (낙하), z=최소 w=최대 (앞뒤)
        private float baseWindVolume;

        private void Awake()
        {
            if (systems == null || systems.Length == 0)
            {
                systems = GetComponentsInChildren<ParticleSystem>(true);
            }

            baseRate = new float[systems.Length];
            baseVelY = new Vector4[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null)
                {
                    continue;
                }

                baseRate[i] = systems[i].emission.rateOverTime.constant;

                // 씬에서 맞춰 둔 값을 읽어 두고 **거기에 배율을 곱한다.** 여기서
                // 숫자를 정해 넣으면 가까운 눈과 먼 눈의 낙하 속도 차이가 사라진다 —
                // 그 차이가 거리감을 만드는 것이라 지우면 안 된다.
                ParticleSystem.VelocityOverLifetimeModule v0 = systems[i].velocityOverLifetime;
                baseVelY[i] = new Vector4(v0.y.constantMin, v0.y.constantMax,
                                          v0.z.constantMin, v0.z.constantMax);
            }

            if (wind != null)
            {
                baseWindVolume = wind.volume;
            }
        }

        private void Update()
        {
            // 0~1. 두 파동을 겹쳐 주기가 눈에 안 띄게 한다.
            float t = Time.unscaledTime;
            float a = Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(1f, periods.x));
            float b = Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(1f, periods.y));
            float k = Mathf.Clamp01((a * 0.6f + b * 0.4f) * 0.5f + 0.5f);
            float scale = Mathf.Lerp(lull, gust, k);

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem p = systems[i];
                if (p == null)
                {
                    continue;
                }

                ParticleSystem.EmissionModule em = p.emission;
                em.rateOverTime = baseRate[i] * scale;

                ParticleSystem.VelocityOverLifetimeModule v = p.velocityOverLifetime;
                if (!v.enabled)
                {
                    continue;
                }

                // 셋을 함께 세운다. y 는 씬의 값에 배율만 곱하고, z 는 그대로 두며,
                // x 는 바람이 미는 만큼 더한다.
                float push = -sideWind * k;
                v.x = new ParticleSystem.MinMaxCurve(push - 0.5f, push + 0.2f);
                v.y = new ParticleSystem.MinMaxCurve(baseVelY[i].x * scale, baseVelY[i].y * scale);
                v.z = new ParticleSystem.MinMaxCurve(baseVelY[i].z, baseVelY[i].w);
            }

            if (wind != null)
            {
                // 소리가 같이 커져야 눈이 세진 것으로 느껴진다. 보이는 것만 바뀌면
                // 화면이 이상해 보일 뿐 바람으로 안 읽힌다.
                wind.volume = baseWindVolume * Mathf.Lerp(0.75f, 1.25f, k);
            }
        }
    }
}
