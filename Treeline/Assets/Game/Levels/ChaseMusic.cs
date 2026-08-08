using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 적이 쫓아오는 동안 추격 루프를 깔고, 그동안 장 음악을 낮춘다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>chase-dread-loop</c> 은 <b>큐가 아니라 깔아 두는 루프</b>다 (22 초).
    /// 발각 순간에 치는 스팅어가 아니므로 켜고 끄는 것을 페이드로 한다. 딱
    /// 끊으면 루프에 구멍이 생기는데, 클립 앞뒤 무음을 잘라낸 파일이라 더 티가 난다.
    /// </para>
    /// <para>
    /// IMPORTANT: 추격 음악이 붙으면 <b>장 음악을 낮춰야 한다.</b> 둘을 같은
    /// 크기로 겹치면 발소리와 적 소리가 묻히고, 이 게임의 공포는 거기서 나온다
    /// (<c>docs/tech-design/audio.md</c> 「음악을 크게 깔면 오히려 덜 무서워진다」).
    /// </para>
    /// <para>
    /// 적의 이벤트를 구독하지 않고 <see cref="IHunter.IsHunting"/> 을 읽는다.
    /// 적은 죽으면 <c>Destroy</c> 되므로 구독은 해지 시점을 관리해야 하는데,
    /// 읽는 쪽이 매 프레임 보면 그 문제가 아예 없다. 목록만 이따금 새로 찾는다.
    /// </para>
    /// <para>
    /// 처음에는 <c>Crawler[]</c> 를 들고 있었다. 그래서 6 장 <see cref="Colossus"/> 는
    /// 이 연출을 못 받고 <c>noticed</c> · <c>lost</c> UnityEvent 로 생으로 켜고 꺼야
    /// 했다 — 페이드도 더킹도 없이. <see cref="IHunter"/> 로 열어 두면 6 장도 이
    /// 컴포넌트를 놓기만 하면 같아진다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ChaseMusic : MonoBehaviour
    {
        [Header("음악")]
        [Tooltip("추격 중에 깔 루프. chase-dread-loop.")]
        [SerializeField] private AudioSource chaseLoop;

        [Tooltip("장 음악. 추격 중에 이만큼 낮춘다. 비워도 된다.")]
        [SerializeField] private AudioSource chapterLoop;

        [Tooltip("추격 중 장 음악 배율. 0 이면 완전히 죽인다.")]
        [SerializeField, Range(0f, 1f)] private float chapterDuck = 0.35f;

        [Tooltip("켜고 끄는 데 걸리는 시간. 문서의 1~2 초 구간.")]
        [SerializeField] private float fadeSeconds = 1.4f;

        [Header("누가 쫓는지")]
        [Tooltip("적 목록을 다시 찾는 간격(초). 죽거나 새로 생기는 것을 따라간다.")]
        [SerializeField] private float rescanInterval = 2f;

        private readonly System.Collections.Generic.List<IHunter> hunters =
            new System.Collections.Generic.List<IHunter>();

        private float nextScan;
        private float chaseVolume;
        private float chapterVolume;

        private void Awake()
        {
            if (chaseLoop != null)
            {
                chaseVolume = chaseLoop.volume;
                chaseLoop.loop = true;
                chaseLoop.volume = 0f;
            }

            if (chapterLoop != null)
            {
                chapterVolume = chapterLoop.volume;
            }
        }

        private void Update()
        {
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + rescanInterval;
                Rescan();
            }

            bool chasing = false;
            for (int i = 0; i < hunters.Count; i++)
            {
                // 죽어서 Destroy 된 것은 null 로 남는다. 다음 재탐색에서 걸러진다.
                if (hunters[i] != null && hunters[i].IsHunting)
                {
                    chasing = true;
                    break;
                }
            }

            Drive(chasing);
        }

        /// <summary>
        /// 씬의 <see cref="IHunter"/> 를 모은다.
        /// </summary>
        /// <remarks>
        /// 인터페이스는 <c>FindObjectsByType</c> 로 바로 찾을 수 없어
        /// <see cref="MonoBehaviour"/> 를 훑는다. 매 프레임 하면 비싸지만
        /// 몇 초에 한 번이라 괜찮다. LINQ 를 안 쓴 것은 그때마다 가비지를
        /// 만들지 않으려는 것뿐이다.
        /// </remarks>
        private void Rescan()
        {
            hunters.Clear();
            MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is IHunter hunter)
                {
                    hunters.Add(hunter);
                }
            }
        }

        private void Drive(bool chasing)
        {
            float step = fadeSeconds > 0f ? Time.deltaTime / fadeSeconds : 1f;

            if (chaseLoop != null)
            {
                float target = chasing ? chaseVolume : 0f;
                chaseLoop.volume = Mathf.MoveTowards(chaseLoop.volume, target, chaseVolume * step);

                // 소리가 0 인 동안 재생을 돌려 둘 이유가 없다. 켜질 때 앞에서 시작한다.
                if (chaseLoop.volume > 0f && !chaseLoop.isPlaying)
                {
                    chaseLoop.Play();
                }
                else if (chaseLoop.volume <= 0f && chaseLoop.isPlaying)
                {
                    chaseLoop.Stop();
                }
            }

            if (chapterLoop != null)
            {
                float target = chasing ? chapterVolume * chapterDuck : chapterVolume;
                chapterLoop.volume = Mathf.MoveTowards(chapterLoop.volume, target, chapterVolume * step);
            }
        }
    }
}
