using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 눈보라 속 화이트아웃을 한 자리에서 잡는다. 안개 · 하늘 · 눈발이 서로 맞물려야
    /// 「하얘서 안 보인다」가 되므로 따로 두지 않는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 5 장 기획이 요구하는 것은 「시야가 몇 미터고 위아래도 잘 구분되지 않는다」이다.
    /// R&amp;D 에서 재보니 이 셋이 하나라도 어긋나면 안 나온다. 근거는
    /// <c>docs/tech-design/world-assets.md</c> 「눈보라」절.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>가리는 일은 안개가 한다.</b> 먼 것을 파티클로 가리려면 수만 개가
    /// 필요하고 WebGL 이 죽는다. 파티클은 가까이에서 흩날리는 것만 맡는다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>하늘색과 안개색이 같은 값이어야 한다.</b> 그래서 색을 둘로 두지
    /// 않고 <see cref="air"/> 하나로 몰았다. 다르면 지평선이 선으로 남아 위아래가
    /// 구분되고, 그러면 이 장의 전제가 무너진다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>하늘은 스카이박스로 만들지 않는다.</b> <c>Skybox/Procedural</c> 로
    /// 두 번 실패했다 — 1 장 창밖용 스카이박스를 그대로 쓰니 태양 방향 때문에
    /// <b>노을</b>이 됐고, <c>AtmosphereThickness</c> 를 0 으로 내리니 산란 계산이 죽어
    /// <b>검은 하늘</b>이 됐다. 카메라를 <c>SolidColor</c> 로 지우는 것이 정답이고,
    /// 스카이박스를 아예 안 그리므로 WebGL 에서도 가장 싸다.
    /// </para>
    /// <para>
    /// 시간에 따른 강약은 <see cref="SnowGusts"/> 가 맡는다. 이 컴포넌트는 <b>바탕</b>을
    /// 잡고, 저쪽은 그 바탕에 <b>배율</b>을 곱한다. 둘을 같은 오브젝트에 붙여도 된다.
    /// </para>
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class Whiteout : MonoBehaviour
    {
        /// <summary>
        /// 지수제곱 안개에서 90% 가려지는 거리를 밀도로 바꾸는 상수.
        /// </summary>
        /// <remarks>
        /// <c>exp(-(d·density)²) = 0.1</c> 을 풀면 <c>d·density = √ln10 ≈ 1.517</c> 이다.
        /// 밀도를 직접 만지면 몇 미터인지 감이 안 오므로 거리로 받아 여기서 나눈다.
        /// </remarks>
        private const float DensityFromVisibility = 1.517f;

        [Header("공기 — 안개와 하늘을 한 색으로")]
        [Tooltip("안개색이자 하늘색. 둘이 같아야 지평선이 사라진다.")]
        [SerializeField] private Color air = new Color(0.80f, 0.83f, 0.88f);

        [Tooltip("이 거리에서 90% 가려진다(m). 5장은 17m 로 잡았다.")]
        [SerializeField, Range(4f, 120f)] private float visibility = 17f;

        [Tooltip("환경광. 눈밭은 사방에서 빛이 되돌아오므로 밝다.")]
        [SerializeField] private Color ambient = new Color(0.55f, 0.58f, 0.64f);

        [Header("카메라")]
        [Tooltip("카메라를 단색 지우기로 바꾸고 공기색을 넣는다. 비우면 MainCamera 를 찾는다.")]
        [SerializeField] private Camera view;

        [Tooltip("끄면 카메라를 건드리지 않는다. 6장처럼 하늘을 따로 쓰는 곳에서 끈다.")]
        [SerializeField] private bool driveCamera = true;

        [Header("눈발 — 크기가 밀도보다 세다")]
        [Tooltip("비우면 자식에서 전부 찾는다.")]
        [SerializeField] private ParticleSystem[] snow;

        [Tooltip("입자 하나의 크기(m). 화면 점유를 정하는 것이 사실상 이 값이다.")]
        [SerializeField] private float flakeSize = 0.26f;

        [Tooltip("초당 발생 수. SnowGusts 가 여기에 배율을 곱한다.")]
        [SerializeField] private float flakeRate = 1400f;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            // 편집 중에는 값을 계속 만지므로 따라간다. 플레이 중에는 OnEnable 한 번이면
            // 되고, 강약은 SnowGusts 가 맡는다.
            if (!Application.isPlaying)
            {
                Apply();
            }
        }

        /// <summary>지금 값으로 안개 · 하늘 · 눈발을 다시 잡는다.</summary>
        public void Apply()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = air;
            RenderSettings.fogDensity = DensityFromVisibility / Mathf.Max(1f, visibility);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;

            if (driveCamera)
            {
                Camera target = view != null ? view : Camera.main;
                if (target != null)
                {
                    target.clearFlags = CameraClearFlags.SolidColor;
                    target.backgroundColor = air;
                }

                // 스카이박스를 남겨두면 반사와 환경광에 그 색이 섞여 들어온다.
                RenderSettings.skybox = null;
            }

            ApplySnow();
        }

        /// <summary>
        /// 눈발의 바탕값을 넣는다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <b>편집 중에만 넣는다.</b> <see cref="SnowGusts"/> 가
        /// <c>Awake</c> 에서 발생 수를 읽어 «바탕»으로 삼는데, 같은 프레임에 이쪽도
        /// <c>Awake</c> 에서 쓰면 어느 쪽이 먼저인지 정해져 있지 않다. 편집 중에 넣어
        /// 씬에 저장해 두면 그 경합 자체가 없어진다.
        /// <para>
        /// 색은 여기서 만지지 않는다. 눈발 머티리얼로 정하고 프리팹에 굳혀 두었다
        /// (<c>MAT_SnowStreak_Whiteout</c>). 다만 <b>크기와 색 사이에는 순서가
        /// 있다</b> — 크기 0.12 에서는 흰 17.6% / 회색 16.1% 로 색이 안 통하고,
        /// 크기 0.26 에서는 36.4% / 46.2% 로 갈린다. 입자가 작으면 픽셀당 덮는 양이
        /// 적어 안개가 그 색을 배경색으로 끌어당기기 때문이다(안개를 끄면 편차가
        /// 0.167 → 0.535 로 뛴다). <b>크기를 먼저 키우고 그다음 색을 어둡게 한다.</b>
        /// </para>
        /// </remarks>
        private void ApplySnow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ParticleSystem[] targets = snow != null && snow.Length > 0
                ? snow
                : GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem p in targets)
            {
                if (p == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = p.main;
                main.startSize = flakeSize;

                // World 로 두어야 플레이어가 움직여도 눈이 끌려오지 않고 제 속도로
                // 흩날린다. 이미터는 카메라에 붙이고 시뮬레이션만 월드에 남긴다.
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                ParticleSystem.EmissionModule em = p.emission;
                em.rateOverTime = flakeRate;

                // 수명 동안 쌓이는 양보다 상한이 낮으면 눈이 뚝뚝 끊긴다.
                float lifetime = Mathf.Max(0.1f, main.startLifetime.constantMax);
                main.maxParticles = Mathf.CeilToInt(flakeRate * lifetime * 1.15f);
            }
        }

        /// <summary>지금 설정에서 안개 밀도가 얼마인지. 문서와 대조할 때 쓴다.</summary>
        public float Density => DensityFromVisibility / Mathf.Max(1f, visibility);
    }
}
