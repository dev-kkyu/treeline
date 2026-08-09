using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 주인공의 몸에서 나는 소리. 숨 · 신음 · 장비가 흔들리는 소리다.
    /// </summary>
    /// <remarks>
    /// 대사는 없다. 주인공은 정비공이고 혼자이며, 말을 하면 「누구에게 하는 말인가」가
    /// 생긴다. 그래서 소리는 전부 <b>호흡과 힘쓰는 소리</b>다.
    ///
    /// <para>
    /// 발소리는 여기 없다. <see cref="PlayerFootsteps"/> 가 바닥 재질별로 따로 낸다 —
    /// 그쪽은 <b>이동한 거리</b>로 내고 이쪽은 <b>사건</b>으로 낸다.
    /// </para>
    /// <para>
    /// 팔 클립(<c>Arms_Hit</c> · <c>Arms_Death</c> · <c>Body_Grabbed</c>)이 Unity 에
    /// 들어오면 여기 붙은 소리와 같은 순간을 가리킨다. 지금은 클립이 없으므로 코드가
    /// 내는 이벤트에만 붙어 있다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerBodyAudio : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;

        [SerializeField] private PlayerMotor motor;

        [SerializeField] private WeaponSwitcher weapons;

        [Tooltip("착지 시점을 여기서 받는다. 접지 판정을 두 벌 두지 않는다.")]
        [SerializeField] private PlayerFootsteps footsteps;

        [Header("소리")]
        [Tooltip("피격. 둘 이상 넣으면 번갈아 낸다.")]
        [SerializeField] private AudioClip[] hurt = Array.Empty<AudioClip>();

        [SerializeField] private AudioClip death;

        [Tooltip("도약. 발이 바닥을 미는 소리는 PlayerFootsteps 가 따로 낸다.")]
        [SerializeField] private AudioClip jump;

        [Tooltip("착지. 발이 닿는 소리 위에 무릎과 장비가 내려앉는 것을 얹는다.")]
        [SerializeField] private AudioClip land;

        [Tooltip("무거운 것을 들 때. 통에서 나는 소리는 Carriable 이 따로 낸다.")]
        [SerializeField] private AudioClip exert;

        [Tooltip("붙잡혔을 때의 발버둥. Colossus 의 처형이 부른다.")]
        [SerializeField] private AudioClip struggle;

        [Tooltip("스태미너가 다 빠졌을 때 깔리는 호흡. 루프다.")]
        [SerializeField] private AudioClip exhaustedBreath;

        [Tooltip("추운 데 서 있을 때의 입김. 둘 이상 넣으면 번갈아 낸다.")]
        [SerializeField] private AudioClip[] coldBreath = Array.Empty<AudioClip>();

        [Tooltip("입김 소리 크기. 파티클과 함께 나므로 작게 둔다.")]
        [SerializeField, Range(0f, 1f)] private float coldBreathVolume = 0.55f;

        [Header("크기")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [SerializeField, Range(0f, 1f)] private float breathVolume = 0.75f;

        [Tooltip("호흡이 들어오고 나가는 시간(초). 갑자기 켜지면 소리가 튄다.")]
        [SerializeField] private float breathFadeSeconds = 0.7f;

        private static PlayerBodyAudio instance;

        private AudioSource source;
        private AudioSource breathSource;
        private int lastHealth;
        private int lastHurtIndex = -1;
        private int lastColdIndex = -1;
        private bool wasCarrying;

        /// <summary>
        /// 붙잡혔다. <see cref="Colossus"/> 의 처형이 부른다.
        /// </summary>
        /// <remarks>
        /// 붙잡는 쪽의 소리(<c>colossus-grab</c>)와 별개다. 저쪽은 손이 닫히는 소리고
        /// 이쪽은 그 안에서 사람이 내는 소리다. 둘이 같은 프레임에 겹쳐야 한다.
        /// </remarks>
        public void PlayStruggle()
        {
            Play(struggle);
        }

        /// <summary>
        /// 입김 한 번. <see cref="PlayerColdBreath"/> 가 파티클을 뿜을 때 부른다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <b>정적 진입점을 둔 것은 저쪽 파일을 한 줄만 건드리기 위해서다.</b>
        /// 입김 파티클은 <see cref="PlayerHud"/> 가 런타임에 붙이는 것이라 참조를
        /// 인스펙터로 물릴 수 없고, 저쪽에 필드와 찾는 코드를 넣으면 담당이 다른 파일에
        /// 남기는 자리가 늘어난다. 플레이어는 씬에 하나뿐이라 정적으로 잡아도 된다.
        /// </remarks>
        public static void PlayColdBreathOnce()
        {
            if (instance != null)
            {
                instance.PlayColdBreath();
            }
        }

        /// <summary>
        /// 입김 한 번. 위 정적 진입점이 부른다.
        /// </summary>
        /// <remarks>
        /// <para>
        /// 소리를 저쪽이 직접 내지 않는 이유는 둘이다. 하나는 <b>몸에서 나는 소리를
        /// 한 곳에서 내기 위해서</b>이고, 다른 하나가 더 중요하다 —
        /// <b>지쳐서 헐떡이는 동안에는 입김 소리를 내지 않아야 한다.</b> 이미 깔려
        /// 있는 호흡 위에 느긋한 숨을 얹으면 두 사람이 숨 쉬는 것으로 들린다.
        /// 그 상태를 아는 것은 이 컴포넌트뿐이다.
        /// </para>
        /// <para>
        /// 파티클은 그대로 나온다. 보이는 것을 멈추면 추운 구간을 벗어난 것처럼
        /// 읽히는데, 지친 것과 추운 것은 다른 일이다.
        /// </para>
        /// </remarks>
        public void PlayColdBreath()
        {
            if (coldBreath.Length == 0 || motor == null)
            {
                return;
            }

            if (motor.IsExhausted && breathSource.isPlaying)
            {
                return;
            }

            lastColdIndex = PickIndex(coldBreath.Length, lastColdIndex);
            AudioClip clip = coldBreath[lastColdIndex];
            if (clip != null)
            {
                source.PlayOneShot(clip, volume * coldBreathVolume);
            }
        }

        private void Awake()
        {
            instance = this;

            if (health == null)
            {
                health = GetComponent<PlayerHealth>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (weapons == null)
            {
                weapons = GetComponent<WeaponSwitcher>();
            }

            if (footsteps == null)
            {
                footsteps = GetComponent<PlayerFootsteps>();
            }

            // 발소리가 쓰는 AudioSource 를 같이 쓰지 않는다. 발소리는 거리로 계속
            // 나므로, 한 소스를 나눠 쓰면 신음이 발소리에 끊긴다.
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            breathSource = gameObject.AddComponent<AudioSource>();
            breathSource.playOnAwake = false;
            breathSource.loop = true;
            breathSource.spatialBlend = 0f;
            breathSource.volume = 0f;
            breathSource.clip = exhaustedBreath;

            if (health != null)
            {
                lastHealth = health.Health;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.HealthChanged += OnHealthChanged;
                health.Died += OnDied;
            }

            if (motor != null)
            {
                motor.Jumped += OnJumped;
            }

            if (footsteps != null)
            {
                footsteps.Landed += OnLanded;
            }
        }

        private void OnDestroy()
        {
            // 장이 넘어가면 새 플레이어가 생긴다. 죽은 것을 잡고 있으면 안 된다.
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
                health.Died -= OnDied;
            }

            if (motor != null)
            {
                motor.Jumped -= OnJumped;
            }

            if (footsteps != null)
            {
                footsteps.Landed -= OnLanded;
            }
        }

        private void Update()
        {
            UpdateCarrying();
            UpdateBreath();
        }

        private void UpdateCarrying()
        {
            if (weapons == null)
            {
                return;
            }

            bool carrying = weapons.IsCarrying;
            if (carrying && !wasCarrying)
            {
                Play(exert);
            }

            wasCarrying = carrying;
        }

        /// <summary>
        /// 스태미너가 다 빠진 동안 호흡을 깐다.
        /// </summary>
        /// <remarks>
        /// 달리기를 멈출 때마다 켜지 않는다. 짧게 달려도 나면 소리가 늘 깔려 있게 되고,
        /// 그러면 <b>정말 지쳤다는 것이 안 들린다.</b> 다 빠진 순간에만 붙인다.
        /// </remarks>
        private void UpdateBreath()
        {
            if (breathSource.clip == null || motor == null)
            {
                return;
            }

            bool wanted = motor.IsExhausted && health != null && health.IsAlive;
            float target = wanted ? breathVolume : 0f;

            if (wanted && !breathSource.isPlaying)
            {
                breathSource.Play();
            }

            float step = breathFadeSeconds > 0f
                ? Time.deltaTime / breathFadeSeconds * breathVolume
                : breathVolume;
            breathSource.volume = Mathf.MoveTowards(breathSource.volume, target, step);

            if (!wanted && breathSource.isPlaying && breathSource.volume <= 0f)
            {
                breathSource.Stop();
            }
        }

        private void OnHealthChanged(int value)
        {
            // 장이 넘어가며 회복될 때도 온다. 줄어든 경우만 소리를 낸다.
            if (value < lastHealth && value > 0)
            {
                Play(Pick());
            }

            lastHealth = value;
        }

        private void OnDied()
        {
            // 마지막 한 칸이 깎인 것과 죽은 것이 같은 순간이다. 신음과 사망 소리가
            // 겹치면 둘 다 안 들리므로, 죽을 때는 사망 소리만 남긴다.
            source.Stop();
            breathSource.Stop();
            Play(death);
        }

        private void OnJumped()
        {
            Play(jump);
        }

        private void OnLanded()
        {
            Play(land);
        }

        /// <summary>바로 앞에 낸 것을 빼고 고른다. 발소리와 같은 이유다.</summary>
        private AudioClip Pick()
        {
            if (hurt.Length == 0)
            {
                return null;
            }

            lastHurtIndex = PickIndex(hurt.Length, lastHurtIndex);
            return hurt[lastHurtIndex];
        }

        /// <summary>
        /// <paramref name="count"/> 중 <paramref name="last"/> 를 뺀 하나를 고른다.
        /// </summary>
        /// <remarks>
        /// 피격 둘과 입김 둘이 같은 규칙을 쓴다. 같은 소리가 두 번 이어지면 티가
        /// 나고, 특히 입김은 3 초마다 나므로 두 번째부터 바로 들킨다.
        /// </remarks>
        private static int PickIndex(int count, int last)
        {
            if (count <= 1)
            {
                return 0;
            }

            // 첫 소리에는 뺄 것이 없다. 이 분기가 없으면 0 번이 영영 안 나온다.
            if (last < 0)
            {
                return UnityEngine.Random.Range(0, count);
            }

            int index = UnityEngine.Random.Range(0, count - 1);
            if (index >= last)
            {
                index++;
            }

            return index;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
