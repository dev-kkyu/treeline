using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 얼음이 <see cref="TimedDevice"/> 의 진행도에 맞춰 <b>아래에서부터</b> 물러난다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 2 장의 기다리는 구간은 히터가 도는 22 초다. 그 22 초 동안 화면이 안 변하면
    /// 플레이어는 자기가 무엇을 기다리는지 모르고, 걸리긴 한 건지도 모른다.
    /// 다 녹은 순간에 문이 통째로 사라지는 것도 같은 문제의 반대편이다 —
    /// <b>사건이 하나뿐이면 기다림이 빈 시간</b>이 된다.
    /// </para>
    /// <para>
    /// 셰이더로 디졸브하는 대신 얼음을 높이별 띠로 나눠 순서대로 줄인다. WebGL
    /// 이라 커스텀 셰이더 하나가 빌드 용량과 변형 컴파일로 돌아오고, 무엇보다
    /// 띠를 쓰면 <b>어디까지 녹았는지가 형태로 남는다.</b> 반투명이 옅어지는
    /// 것보다 얼음이 걷혀 루버가 드러나는 편이 멀리서도 읽힌다.
    /// </para>
    /// <para>
    /// 각 띠의 원점은 Blender 에서 <b>윗면</b>에 두었다. 그래서 Y 를 줄이면
    /// 아래에서 위로 물러난다 — 바닥의 히터가 녹이는 방향이다. 원점이 가운데면
    /// 위아래로 같이 오므라들어 「쪼그라든다」로 보인다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class IceMelt : MonoBehaviour
    {
        [Header("무엇에 맞춰 녹나")]
        [Tooltip("이 장치의 Progress 를 따라간다. 2장은 히터다.")]
        [SerializeField] private TimedDevice device;

        [Header("아래부터 녹는 띠")]
        [Tooltip("0 번이 제일 아래. 순서가 곧 녹는 순서다.")]
        [SerializeField] private Transform[] bands;

        [Tooltip("한 띠가 녹는 데 걸리는 몫. 0.30 이면 전체 시간의 30% 다.")]
        [SerializeField, Range(0.05f, 1f)] private float bandDuration = 0.32f;

        [Tooltip("다 녹았을 때 남기는 두께. 0 이면 완전히 사라진다.")]
        [SerializeField, Range(0f, 0.2f)] private float residue = 0f;

        [Tooltip("가로도 같이 줄어드는 정도. 세로만 줄면 종이처럼 납작해진다.")]
        [SerializeField, Range(0f, 1f)] private float lateralShrink = 0.45f;

        [Header("연출")]
        [Tooltip("녹는 동안 나는 김. 진행도에 따라 방출량을 올린다.")]
        [SerializeField] private ParticleSystem vapour;

        [SerializeField] private float vapourRateMax = 26f;

        [Tooltip("띠 하나가 다 녹을 때마다 한 번. 조용히 사라지면 사건이 안 된다.")]
        [SerializeField] private AudioSource crackSource;

        [SerializeField] private AudioClip crackClip;

        private Vector3[] baseScale;
        private int lastCracked = -1;

        private void Awake()
        {
            baseScale = new Vector3[bands.Length];
            for (int i = 0; i < bands.Length; i++)
            {
                baseScale[i] = bands[i] != null ? bands[i].localScale : Vector3.one;
            }

            if (device == null)
            {
                Debug.LogWarning($"{name}: 따라갈 TimedDevice 가 없다. 얼음이 그대로 남는다.", this);
            }
        }

        private void OnDisable()
        {
            // 문이 통째로 꺼질 때(다 녹은 뒤) 다음에 켜질 것에 대비해 되돌려 둔다.
            if (baseScale == null)
            {
                return;
            }

            for (int i = 0; i < bands.Length; i++)
            {
                if (bands[i] != null)
                {
                    bands[i].localScale = baseScale[i];
                    bands[i].gameObject.SetActive(true);
                }
            }

            lastCracked = -1;
        }

        private void Update()
        {
            if (device == null || bands.Length == 0)
            {
                return;
            }

            float progress = device.IsActive ? 1f : (device.IsRunning ? device.Progress : 0f);
            Apply(progress);
        }

        private void Apply(float progress)
        {
            // 마지막 띠가 진행도 1 에 정확히 끝나도록 시작 간격을 역산한다.
            float step = bands.Length > 1
                ? Mathf.Max(0f, (1f - bandDuration) / (bands.Length - 1))
                : 0f;

            for (int i = 0; i < bands.Length; i++)
            {
                if (bands[i] == null)
                {
                    continue;
                }

                float t = Mathf.Clamp01((progress - i * step) / bandDuration);
                float remain = Mathf.Lerp(1f, residue, t);

                Vector3 s = baseScale[i];
                bands[i].localScale = new Vector3(
                    s.x * Mathf.Lerp(1f, remain, lateralShrink),
                    s.y * remain,
                    s.z * Mathf.Lerp(1f, remain, lateralShrink));

                bool gone = t >= 1f && residue <= 0f;
                if (bands[i].gameObject.activeSelf == gone)
                {
                    bands[i].gameObject.SetActive(!gone);
                }

                if (t >= 1f && i > lastCracked)
                {
                    lastCracked = i;
                    if (crackSource != null && crackClip != null)
                    {
                        crackSource.PlayOneShot(crackClip);
                    }
                }
            }

            if (vapour != null)
            {
                ParticleSystem.EmissionModule em = vapour.emission;
                // 녹는 중에만 김이 오른다. 끝나면 히터만 남고 김은 잦아든다.
                bool running = progress > 0f && progress < 1f;
                em.rateOverTime = running ? Mathf.Lerp(4f, vapourRateMax, progress) : 0f;
            }
        }
    }
}
