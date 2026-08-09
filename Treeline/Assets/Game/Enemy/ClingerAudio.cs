using UnityEngine;
using UnityEngine.AI;

namespace Treeline
{
    /// <summary>
    /// 붙어 있는 것의 <b>이동 루프만</b> 낸다. 단발은 프리팹의 <c>UnityEvent</c> 가 낸다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IMPORTANT: <b>단발 소리를 여기서 내지 않는다.</b> <c>Clinger.prefab</c> 이 이미
    /// <c>tremored</c> · <c>dropped</c> · <c>landed</c> · <c>attackTell</c> ·
    /// <c>attackHit</c> · <c>hurt</c> · <c>died</c> 일곱 개를 <c>Snd_Voice</c> ·
    /// <c>Snd_Tremor</c> 에 걸어 두었다. 같은 것을 코드로 또 내면 소리가 두 번 난다 —
    /// <c>audio.md</c> 의 「붙이는 길은 둘이다, 한쪽만 쓴다」 그대로다.
    /// </para>
    /// <para>
    /// 그런데 <b>루프는 이벤트로 걸 수 없다.</b> 「천장에 붙어 있는 동안」과 「땅에서
    /// 달리는 동안」은 사건이 아니라 <b>상태</b>라 매 프레임 판단해야 하고, 실제로
    /// <c>Snd_Cling</c> 과 <c>Snd_Scuttle</c> 은 아무도 재생하지 않아 소리가 안 나고
    /// 있었다. 그 두 개만 이 컴포넌트가 맡는다.
    /// </para>
    /// <para>
    /// 소스를 하나로 합쳐 클립만 갈지 않는 이유는 <b>두 소리의 들리는 거리가 다르기</b>
    /// 때문이다. 프리팹이 천장 쪽을 0.45 · 13 m, 지상 쪽을 0.80 · 22 m 로 잡아 두었다 —
    /// 매달린 것은 가까이 와야 들리고 달려오는 것은 멀리서 들려야 한다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Clinger))]
    [DisallowMultipleComponent]
    public sealed class ClingerAudio : MonoBehaviour
    {
        /// <summary>지상 이동 루프가 어느 바닥에서 나는가.</summary>
        /// <remarks>
        /// <see cref="PlayerFootsteps"/> 가 「밟고 있는 바닥은 레벨을 만든 쪽만 안다」로
        /// 장마다 표면을 정하는 것과 같다. 4 장은 실내이고 5 장은 눈보라 속이다.
        /// </remarks>
        public enum GroundSurface
        {
            Concrete = 0,
            Snow = 1,
        }

        [SerializeField] private Clinger clinger;

        [Tooltip("이동 속도를 여기서 읽는다. Clinger 가 속도를 밖으로 내지 않는다.")]
        [SerializeField] private NavMeshAgent agent;

        [Header("소리를 내는 자리")]
        [Tooltip("천장에 붙어 있는 동안. 프리팹의 Snd_Cling.")]
        [SerializeField] private AudioSource clingSource;

        [Tooltip("땅에서 움직이는 동안. 프리팹의 Snd_Scuttle.")]
        [SerializeField] private AudioSource scuttleSource;

        [Header("루프")]
        [SerializeField] private AudioClip clingLoop;

        [Tooltip("실내. 4 장.")]
        [SerializeField] private AudioClip scuttleConcrete;

        [Tooltip("눈. 5 장.")]
        [SerializeField] private AudioClip scuttleSnow;

        [Tooltip("이 장의 바닥. 씬에 놓을 때 장에 맞게 덮어쓴다.")]
        [SerializeField] private GroundSurface surface = GroundSurface.Concrete;

        [Header("판단")]
        [Tooltip("이 속도 아래는 멈춘 것으로 본다. Clinger 의 같은 값과 맞춰 둔다.")]
        [SerializeField] private float movingSpeed = 0.15f;

        [Tooltip("루프가 들어오고 나가는 시간(초). 0 이면 딱 끊긴다.")]
        [SerializeField] private float crossfade = 0.25f;

        private float clingVolume;
        private float scuttleVolume;
        private GroundSurface appliedSurface;

        private void Awake()
        {
            if (clinger == null)
            {
                clinger = GetComponent<Clinger>();
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            // 프리팹이 잡아 둔 음량이 목표다. 여기서 숫자를 새로 정하면 두 소리의
            // 거리 설계가 무너진다.
            clingVolume = Prepare(clingSource, clingLoop);
            scuttleVolume = Prepare(scuttleSource, ScuttleClip());
            appliedSurface = surface;
        }

        private void Update()
        {
            // 표면은 씬에서 덮어쓰는 값이라 런타임에 바뀔 일이 거의 없지만, 바뀌면
            // 따라가야 한다. 프리팹 인스턴스를 에디터에서 만지며 확인할 때 그렇다.
            if (appliedSurface != surface)
            {
                appliedSurface = surface;
                SwapScuttleClip();
            }

            bool cling = false;
            bool scuttle = false;

            if (clinger.IsAlive)
            {
                switch (clinger.Mode)
                {
                    case Clinger.ClingerMode.Ceiling:
                        cling = true;
                        break;

                    // 떨어지는 동안은 둘 다 아니다. drop-release 단발이 그 구간을 채운다.
                    case Clinger.ClingerMode.Falling:
                        break;

                    case Clinger.ClingerMode.Ground:
                        scuttle = Speed() > movingSpeed;
                        break;
                }
            }

            Drive(clingSource, cling, clingVolume);
            Drive(scuttleSource, scuttle, scuttleVolume);
        }

        private float Speed()
        {
            if (agent == null)
            {
                return 0f;
            }

            // 수평 속도만 센다. 떨어지는 동안 쌓이면 착지하는 순간 달리는 소리가 난다.
            Vector3 flat = agent.velocity;
            flat.y = 0f;
            return flat.magnitude;
        }

        private AudioClip ScuttleClip()
        {
            return surface == GroundSurface.Snow && scuttleSnow != null ? scuttleSnow : scuttleConcrete;
        }

        private void SwapScuttleClip()
        {
            if (scuttleSource == null)
            {
                return;
            }

            AudioClip want = ScuttleClip();
            if (scuttleSource.clip == want)
            {
                return;
            }

            bool wasPlaying = scuttleSource.isPlaying;
            scuttleSource.clip = want;
            if (wasPlaying && want != null)
            {
                scuttleSource.Play();
            }
        }

        private float Prepare(AudioSource source, AudioClip clip)
        {
            if (source == null)
            {
                return 0f;
            }

            float target = source.volume;
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            // 페이드로 들어오므로 시작 음량은 0 이다.
            source.volume = 0f;
            return target;
        }

        private void Drive(AudioSource source, bool wanted, float target)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            if (wanted && !source.isPlaying)
            {
                source.Play();
            }

            float want = wanted ? target : 0f;
            source.volume = crossfade > 0f
                ? Mathf.MoveTowards(source.volume, want, target / crossfade * Time.deltaTime)
                : want;

            if (!wanted && source.isPlaying && source.volume <= 0f)
            {
                source.Stop();
            }
        }
    }
}
