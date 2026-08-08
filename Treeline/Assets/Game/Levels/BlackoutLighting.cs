using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 정전된 실내의 조명 환경. 씬에 하나 두면 앰비언트와 안개를 그 장의 값으로 덮는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 2장 숙소동처럼 **손전등만 있는 장**에서 쓴다. 회색 박스가 허술해 보이는
    /// 이유는 형태가 단순해서가 아니라 앰비언트가 전부를 고르게 비춰서다. 앰비언트를
    /// 죽이면 손전등 원뿔 밖이 실제로 안 보이고, 그때부터 「무엇을 비출지」가
    /// 플레이어의 선택이 된다.
    /// </para>
    /// <para>
    /// 조명 설정은 씬에 저장되지만 <c>RenderSettings</c> 는 실수로 덮어쓰기 쉽다.
    /// 컴포넌트로 두면 값이 어디서 왔는지 인스펙터에서 보이고, 장마다 다른 값을
    /// 프리팹처럼 들고 다닐 수 있다.
    /// </para>
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BlackoutLighting : MonoBehaviour
    {
        [Header("앰비언트")]
        [Tooltip("정전이므로 거의 검정이다. 완전한 0 은 피한다 — 실루엣조차 안 보이면 길을 못 찾는다.")]
        [SerializeField] private Color ambient = new Color(0.007f, 0.009f, 0.013f);

        [Header("안개 — 손전등 원뿔에 형태를 준다")]
        [SerializeField] private bool useFog = true;
        [SerializeField] private Color fogColor = new Color(0.015f, 0.018f, 0.025f);

        [Tooltip("복도 한 칸이 4m 다. 0.08 이면 두세 칸 앞이 묻힌다.")]
        [SerializeField] private float fogDensity = 0.085f;

        [Header("하늘")]
        [Tooltip("실내라 하늘이 보이지 않는다. 스카이박스를 끄면 앰비언트가 새어 들어오지 않는다.")]
        [SerializeField] private bool clearSkybox = true;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            fogDensity = Mathf.Max(0f, fogDensity);

            if (isActiveAndEnabled)
            {
                Apply();
            }
        }

        /// <summary>지금 값을 <see cref="RenderSettings"/> 에 적용한다.</summary>
        public void Apply()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;

            if (clearSkybox)
            {
                RenderSettings.skybox = null;
            }

            RenderSettings.fog = useFog;
            if (useFog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
            }
        }
    }
}
