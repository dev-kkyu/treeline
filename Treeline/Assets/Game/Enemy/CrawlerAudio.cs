using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 기는 것의 소리. 상태에 따라 루프를 갈아 끼우고, 사건은 단발로 얹는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Crawler"/> 를 고치지 않고 이벤트만 구독한다. 발소리를
    /// <c>PlayerFootsteps</c> 로 떼어낸 것과 같은 이유다 — 행동과 소리가 한 파일에
    /// 있으면 둘 중 하나를 고칠 때마다 다른 쪽 diff 가 섞인다.
    /// </para>
    /// <para>
    /// 클립 배치는 <c>docs/tech-design/audio.md</c> 의 「<c>Crawler</c> — 바닥 쐐기
    /// 10 종」 표를 그대로 따른다. 비워 둔 칸은 아무 일도 하지 않으므로 일부만 채워
    /// 넣어도 된다.
    /// </para>
    /// <para>
    /// IMPORTANT: 2 장은 소리로 적의 위치를 가늠하는 장이다. <see cref="AudioSource"/>
    /// 의 <c>Max Distance</c> 를 기본값 500 으로 두면 복도 저 끝의 것이 코앞처럼
    /// 들려 그 난이도가 사라진다. 프리팹은 20~26 으로 좁혀 두었다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Crawler))]
    [DisallowMultipleComponent]
    public sealed class CrawlerAudio : MonoBehaviour
    {
        [Header("소리를 내는 자리")]
        [Tooltip("상태 루프. 클립을 갈아 끼우며 계속 돈다.")]
        [SerializeField] private AudioSource loopSource;

        [Tooltip("단발. 예고 · 히트 · 피격 · 사망 · 발각.")]
        [SerializeField] private AudioSource oneShotSource;

        [Header("상태 루프")]
        [SerializeField] private AudioClip idleLoop;
        [SerializeField] private AudioClip skitterLoop;
        [SerializeField] private AudioClip sprintLoop;

        [Tooltip("이 속도 아래는 멈춘 것으로 본다.")]
        [SerializeField] private float stillSpeed = 0.15f;

        [Tooltip("루프를 바꿀 때 넘어가는 시간. 0 이면 딱 끊긴다.")]
        [SerializeField] private float crossfade = 0.25f;

        [Header("사건")]
        [SerializeField] private AudioClip notice;
        [SerializeField] private AudioClip attackTell;
        [SerializeField] private AudioClip attack;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip death;

        [Header("배회 중 간헐 발성")]
        [SerializeField] private AudioClip vocalA;
        [SerializeField] private AudioClip vocalB;

        [Tooltip("두 값 사이에서 무작위로 고른 간격마다 한 번.")]
        [SerializeField] private Vector2 vocalInterval = new Vector2(5f, 11f);

        private Crawler crawler;
        private AudioClip currentLoop;
        private float loopTargetVolume;
        private float nextVocalTime;
        private bool vocalToggle;

        private void Awake()
        {
            crawler = GetComponent<Crawler>();

            if (loopSource != null)
            {
                loopSource.loop = true;
                loopTargetVolume = loopSource.volume;
                // 페이드 도중에 켜지므로 시작 음량은 0 이다.
                loopSource.volume = 0f;
            }

            ScheduleVocal();
        }

        private void OnEnable()
        {
            crawler.Heard += OnHeard;
            crawler.Noticed += OnNoticed;
            crawler.AttackTell += OnAttackTell;
            crawler.AttackHit += OnAttackHit;
            crawler.Hurt += OnHurt;
            crawler.Died += OnDied;
        }

        private void OnDisable()
        {
            crawler.Heard -= OnHeard;
            crawler.Noticed -= OnNoticed;
            crawler.AttackTell -= OnAttackTell;
            crawler.AttackHit -= OnAttackHit;
            crawler.Hurt -= OnHurt;
            crawler.Died -= OnDied;
        }

        private void Update()
        {
            if (!crawler.IsAlive)
            {
                FadeLoopOut();
                return;
            }

            ChooseLoop();
            Vocalize();
        }

        /// <summary>속도와 추격 여부로 루프를 고른다. 표의 세 줄이 이 함수다.</summary>
        private void ChooseLoop()
        {
            AudioClip want;
            if (crawler.IsChasing && crawler.PlanarSpeed > stillSpeed)
            {
                want = sprintLoop;
            }
            else if (crawler.PlanarSpeed > stillSpeed)
            {
                want = skitterLoop;
            }
            else
            {
                want = idleLoop;
            }

            SetLoop(want);
        }

        private void SetLoop(AudioClip clip)
        {
            if (loopSource == null)
            {
                return;
            }

            if (clip != currentLoop)
            {
                currentLoop = clip;

                if (clip == null)
                {
                    loopSource.Stop();
                }
                else
                {
                    // 클립을 바꾸면 재생 위치가 0 으로 돌아간다. 루프끼리는 이어 붙일
                    // 이음매가 없으므로 음량으로만 넘긴다.
                    loopSource.clip = clip;
                    loopSource.Play();
                }
            }

            float target = clip != null ? loopTargetVolume : 0f;
            loopSource.volume = crossfade > 0f
                ? Mathf.MoveTowards(loopSource.volume, target, loopTargetVolume / crossfade * Time.deltaTime)
                : target;
        }

        private void FadeLoopOut()
        {
            if (loopSource == null || loopSource.volume <= 0f)
            {
                return;
            }

            loopSource.volume = crossfade > 0f
                ? Mathf.MoveTowards(loopSource.volume, 0f, loopTargetVolume / crossfade * Time.deltaTime)
                : 0f;

            if (loopSource.volume <= 0f)
            {
                loopSource.Stop();
            }
        }

        /// <summary>배회하는 동안에만 간헐적으로 낸다. 추격 중에는 루프가 그 자리를 채운다.</summary>
        private void Vocalize()
        {
            if (crawler.IsChasing || Time.time < nextVocalTime)
            {
                return;
            }

            ScheduleVocal();

            AudioClip clip = vocalToggle ? vocalB : vocalA;
            vocalToggle = !vocalToggle;
            PlayOneShot(clip);
        }

        private void ScheduleVocal()
        {
            nextVocalTime = Time.time + Random.Range(vocalInterval.x, vocalInterval.y);
        }

        /// <summary>
        /// 들었을 때는 발성만 낸다. <c>crawler-notice</c> 는 「봤다」의 소리라
        /// 여기 쓰면 플레이어가 이미 들킨 줄 알고 숨기를 포기한다.
        /// </summary>
        private void OnHeard()
        {
            ScheduleVocal();
            AudioClip clip = vocalToggle ? vocalB : vocalA;
            vocalToggle = !vocalToggle;
            PlayOneShot(clip);
        }

        private void OnNoticed() => PlayOneShot(notice);

        private void OnAttackTell() => PlayOneShot(attackTell);

        private void OnAttackHit() => PlayOneShot(attack);

        private void OnHurt() => PlayOneShot(hit);

        private void OnDied() => PlayOneShot(death);

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || oneShotSource == null)
            {
                return;
            }

            oneShotSource.PlayOneShot(clip);
        }
    }
}
