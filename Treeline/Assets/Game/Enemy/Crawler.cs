using System;
using UnityEngine;
using UnityEngine.AI;

namespace Treeline
{
    /// <summary>기는 것이 지금 무엇을 하고 있는지.</summary>
    public enum CrawlerState
    {
        /// <summary>배회. 집 주변을 느리게 돌아다닌다.</summary>
        Patrol,

        /// <summary>소리를 듣고 그쪽으로 가 본다. 아직 본 것은 아니다.</summary>
        Investigate,

        /// <summary>보고 쫓는다.</summary>
        Chase,

        /// <summary>놓친 자리 주변을 뒤진다.</summary>
        Search,

        /// <summary>죽었다.</summary>
        Dead,
    }

    /// <summary>
    /// 기는 것 — 1단계의 유일한 적. 네발로 낮게 깔려 빠르고, 권총 한두 발에 죽는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 행동은 다섯 상태다 — <b>배회 · 조사 · 추격 · 수색 · 사망.</b> 처음에는
    /// 「보면 쫓고 아니면 선다」 둘뿐이었는데, 그러면 플레이어가 시야만 피하면
    /// 아무 일도 일어나지 않고 <b>적이 배경이 된다.</b> 특히 서 있기만 하는 적은
    /// 소리로 위치를 가늠하는 2 장에서 아무 정보도 주지 않는다.
    /// </para>
    /// <para>
    /// 그래서 셋을 더했다. <b>배회</b>는 가만히 있지 않게 하고(Skitter 클립이
    /// 여기서 쓰인다), <b>조사</b>는 안 보여도 뛰면 들키게 하며, <b>수색</b>은
    /// 놓친 뒤에 바로 포기하지 않게 한다. 셋 다 「숨어서 지나간다」 를 판단이
    /// 필요한 일로 만든다.
    /// </para>
    /// <para>
    /// 외형은 <b>있으면 쓰고 없으면 없는 대로</b> 돌아간다. <see cref="animator"/> 가
    /// 비어 있으면 회색 캡슐이던 때와 똑같이 동작한다. 모델이 들어오기 전에 세운
    /// 씬과 1단계 검증이 그대로 유효해야 하므로 이 성질을 유지한다.
    /// </para>
    /// <para>
    /// 연결 규격은 <c>docs/enemy-sheets/mockups/codex/crawler/animation/unity-integration.md</c>
    /// 다. 파라미터 이름과 상태 우선순위, 그리고 아래의 「피해는 애니메이션 이벤트가
    /// 준다」 가 거기서 왔다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Crawler : MonoBehaviour, IDamageable
    {
        [Header("체력")]
        [Tooltip("권총 한두 발. 기획의 「약하다」를 이 값으로 표현한다.")]
        [SerializeField] private int maxHealth = 2;

        [Header("감지")]
        [Tooltip("이 거리 안에서만 플레이어를 본다.")]
        [SerializeField] private float sightRange = 18f;

        [Tooltip("시야각(전체). 뒤에서 접근하면 못 본다.")]
        [SerializeField] private float sightAngle = 120f;

        [Tooltip("눈 높이. 바닥에서 레이를 쏘면 문턱에 걸린다.")]
        [SerializeField] private float eyeHeight = 0.5f;

        [Tooltip("시야 판정 간격(초). 매 프레임 레이캐스트할 이유가 없다.")]
        [SerializeField] private float sightInterval = 0.15f;

        [Tooltip("놓친 뒤 이 시간이 지나면 추격을 그만두고 수색으로 넘어간다.")]
        [SerializeField] private float loseInterest = 4f;

        [Header("듣기 — 안 보여도 뛰면 들킨다")]
        [Tooltip("이 거리 안에서 나는 소리를 듣는다. 벽은 막지 않는다 — 듣는 것이므로.")]
        [SerializeField] private float hearRange = 13f;

        [Tooltip("이 속도보다 빠르게 움직이면 들린다. 걷기는 이 아래, 달리기는 위여야 한다.")]
        [SerializeField] private float hearSpeed = 2.6f;

        [Tooltip("한 번 듣고 나서 다시 들을 때까지. 이게 없으면 계속 재조준한다.")]
        [SerializeField] private float hearCooldown = 2.5f;

        [Header("배회")]
        [Tooltip("처음 놓인 자리에서 이만큼 안을 돈다. 장 전체를 헤매면 만날 수가 없다.")]
        [SerializeField] private float patrolRadius = 15f;

        [Tooltip("배회 속도. Skitter 클립의 속도(2.4)에 맞춘다.")]
        [SerializeField] private float patrolSpeed = 2.4f;

        [Tooltip("한 지점에 닿은 뒤 쉬는 시간. 계속 움직이면 발소리가 배경음이 된다.")]
        [SerializeField] private Vector2 patrolPause = new Vector2(1.4f, 4.5f);

        [Header("추격 · 수색")]
        [Tooltip("추격 속도. Sprint 클립의 속도(5.5)에 맞춘다.")]
        [SerializeField] private float chaseSpeed = 5.5f;

        [Tooltip("놓친 자리 주변을 몇 군데나 뒤지는지.")]
        [SerializeField] private int searchPoints = 3;

        [Tooltip("놓친 자리에서 이 반경 안을 뒤진다.")]
        [SerializeField] private float searchRadius = 5f;

        [Tooltip("한 지점에서 머무는 시간. 0 이면 스치고 지나가 뒤지는 것으로 안 보인다.")]
        [SerializeField] private float searchDwell = 1.6f;

        [Header("공격")]
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private int attackDamage = 1;

        [Tooltip("예고 동안 뒷걸음질할 여유. 사거리에 이만큼 더해 히트 시점에 다시 잰다.")]
        [SerializeField] private float attackHitTolerance = 0.4f;

        [Header("사망")]
        [Tooltip("죽고 나서 사라지기까지. 0 이면 즉시 사라진다.")]
        [SerializeField] private float corpseLifetime = 3f;

        [Header("외형 — 없어도 동작한다")]
        [Tooltip("자식 Visual 의 Animator. 비우면 Awake 에서 자식에서 찾는다.")]
        [SerializeField] private Animator animator;

        [Tooltip("피해가 나가는 자리. 머리 아래. 비우면 눈높이를 쓴다.")]
        [SerializeField] private Transform attackOrigin;

        [Tooltip("Speed 를 그대로 넣으면 블렌드가 떨린다. 명세의 0.08~0.12 구간.")]
        [SerializeField] private float speedDamping = 0.1f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int IsChasingId = Animator.StringToHash("IsChasing");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        private NavMeshAgent agent;
        private Transform player;
        private IDamageable playerDamageable;

        private int health;
        private float nextSightCheck;
        private float lastSeenTime = float.NegativeInfinity;
        private Vector3 lastSeenPosition;
        private float nextAttackTime;
        private bool attackHitConsumed = true;

        private Vector3 home;
        private Vector3 goal;
        private float waitUntil;
        private float nextHearTime;
        private int searchesLeft;
        private Vector3 previousPlayerPosition;
        private float playerSpeed;

        public bool IsAlive => health > 0;

        /// <summary>지금 무엇을 하고 있는지. 연출·디버그가 읽는다.</summary>
        public CrawlerState State { get; private set; } = CrawlerState.Patrol;

        /// <summary>추격 중인지. 연출·사운드가 붙을 자리라 밖에서 읽을 수 있게 둔다.</summary>
        public bool IsChasing { get; private set; }

        /// <summary>지금 실제로 나아가는 속도(수평). 소리가 어느 루프를 걸지 이걸로 고른다.</summary>
        public float PlanarSpeed
        {
            get
            {
                if (agent == null || !agent.enabled)
                {
                    return 0f;
                }

                Vector3 v = agent.velocity;
                v.y = 0f;
                return v.magnitude;
            }
        }

        /// <summary>배회에서 추격으로 넘어가는 순간. 한 번만 부른다.</summary>
        public event Action Noticed;

        /// <summary>소리를 듣고 조사하러 간다. 본 것은 아니다.</summary>
        public event Action Heard;

        /// <summary>공격 모션이 시작됐다. 아직 피해는 없다.</summary>
        public event Action AttackStarted;

        /// <summary>예고 정점(8f). 여기서 소리가 나야 플레이어가 피할 수 있다.</summary>
        public event Action AttackTell;

        /// <summary>실제로 피해가 나가는 순간(13f).</summary>
        public event Action AttackHit;

        /// <summary>죽지 않을 만큼 맞았다.</summary>
        public event Action Hurt;

        /// <summary>죽었다.</summary>
        public event Action Died;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = maxHealth;

            // 배회 반경의 중심은 씬에 놓인 자리다. 레벨을 만드는 쪽이 값을 따로
            // 넣지 않아도 「이 근처를 돈다」 가 되도록 이렇게 잡았다.
            home = transform.position;
            agent.speed = patrolSpeed;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (gameObject.layer != GameLayers.Enemy)
            {
                // 레이어가 틀리면 총알이 통과하고 적끼리 서로 밀어낸다. 조용히 깨지는 종류라 경고한다.
                Debug.LogWarning($"{name}: 레이어가 Enemy(12) 가 아니다. 사격 판정과 충돌 매트릭스가 어긋난다.", this);
            }
        }

        private void OnEnable()
        {
            AcquirePlayer();
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            // Player.prefab 이 아직 없거나 씬 로드 후 새로 생겼을 수 있어 계속 다시 찾는다.
            if (player != null || AcquirePlayer())
            {
                Think();
            }

            // 생각한 결과가 아니라 실제 이동을 넣어야 발이 미끄러지지 않는다.
            DriveAnimator();
        }

        private void Think()
        {
            TrackPlayerSpeed();

            if (Time.time >= nextSightCheck)
            {
                nextSightCheck = Time.time + sightInterval;
                Sense();
            }

            switch (State)
            {
                case CrawlerState.Chase: Chase(); break;
                case CrawlerState.Search: Search(); break;
                case CrawlerState.Investigate: Investigate(); break;
                default: Patrol(); break;
            }
        }

        /// <summary>
        /// 플레이어의 실제 이동 속도. 소리를 낼 만큼 빠른지 판단하는 데 쓴다.
        /// </summary>
        /// <remarks>
        /// A 의 컴포넌트에서 읽지 않고 위치 차이로 잰다. 플레이어의 속도를 밖에
        /// 내주는 계약이 아직 없고, 그것 하나 때문에 남의 스크립트를 고치면
        /// 담당 경계가 흐려진다. 위치는 누구나 볼 수 있다.
        /// </remarks>
        private void TrackPlayerSpeed()
        {
            if (player == null || Time.deltaTime <= 0f)
            {
                return;
            }

            Vector3 delta = player.position - previousPlayerPosition;
            delta.y = 0f;
            previousPlayerPosition = player.position;

            // 텔레포트나 씬 시작 첫 프레임에 튀지 않게 완만하게 따라간다.
            float instant = Mathf.Min(delta.magnitude / Time.deltaTime, 20f);
            playerSpeed = Mathf.Lerp(playerSpeed, instant, 8f * Time.deltaTime);
        }

        /// <summary>보고 듣는다. 상태 전환은 전부 여기서만 일어난다.</summary>
        private void Sense()
        {
            if (CanSeePlayer())
            {
                lastSeenTime = Time.time;
                lastSeenPosition = player.position;
                Enter(CrawlerState.Chase);
                return;
            }

            if (State == CrawlerState.Chase && Time.time - lastSeenTime > loseInterest)
            {
                // 바로 포기하지 않는다. 놓친 자리부터 뒤진다.
                searchesLeft = searchPoints;
                Enter(CrawlerState.Search);
                return;
            }

            if (State == CrawlerState.Chase || Time.time < nextHearTime)
            {
                return;
            }

            // 듣기는 벽을 통과한다. 발소리는 시야와 상관없이 들리는 것이 자연스럽고,
            // 무엇보다 「뛰면 위험하다」 를 가르치는 유일한 규칙이다.
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= hearRange && playerSpeed >= hearSpeed)
            {
                nextHearTime = Time.time + hearCooldown;
                goal = player.position;
                Enter(CrawlerState.Investigate);
                Heard?.Invoke();
            }
        }

        private void Enter(CrawlerState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            waitUntil = 0f;

            switch (next)
            {
                case CrawlerState.Chase:
                    agent.speed = chaseSpeed;
                    SetChasing(true);
                    break;

                case CrawlerState.Investigate:
                    // 조사는 뛰어간다. 걸어가면 소리를 들은 것이 화면에 안 보인다.
                    agent.speed = chaseSpeed * 0.8f;
                    SetChasing(false);
                    MoveTo(goal);
                    break;

                case CrawlerState.Search:
                    agent.speed = patrolSpeed * 1.4f;
                    SetChasing(false);
                    MoveTo(lastSeenPosition);
                    break;

                default:
                    agent.speed = patrolSpeed;
                    SetChasing(false);
                    waitUntil = Time.time + UnityEngine.Random.Range(patrolPause.x, patrolPause.y);
                    break;
            }
        }

        /// <summary>추격 여부를 바꾼다. 켜지는 순간에만 <see cref="Noticed"/> 를 부른다.</summary>
        private void SetChasing(bool chasing)
        {
            if (IsChasing == chasing)
            {
                return;
            }

            IsChasing = chasing;

            if (chasing)
            {
                Noticed?.Invoke();
            }
        }

        /// <summary>집 주변을 느리게 돈다. 닿으면 잠시 쉬고 다음 자리를 고른다.</summary>
        private void Patrol()
        {
            if (!Arrived())
            {
                return;
            }

            if (Time.time < waitUntil)
            {
                return;
            }

            if (TryPickPoint(home, patrolRadius, out Vector3 point))
            {
                MoveTo(point);
                waitUntil = 0f;
            }
            else
            {
                // 좁은 곳에 갇혀 고를 자리가 없을 수 있다. 다음 프레임에 다시 시도한다.
                waitUntil = Time.time + 0.5f;
            }
        }

        /// <summary>소리가 난 자리까지 가 본다. 도착해서 아무것도 없으면 배회로 돌아간다.</summary>
        private void Investigate()
        {
            if (!Arrived())
            {
                return;
            }

            if (waitUntil <= 0f)
            {
                waitUntil = Time.time + searchDwell;
                return;
            }

            if (Time.time >= waitUntil)
            {
                Enter(CrawlerState.Patrol);
            }
        }

        /// <summary>놓친 자리 주변을 몇 군데 뒤지고 나서 포기한다.</summary>
        private void Search()
        {
            if (!Arrived())
            {
                return;
            }

            if (waitUntil <= 0f)
            {
                waitUntil = Time.time + searchDwell;
                return;
            }

            if (Time.time < waitUntil)
            {
                return;
            }

            waitUntil = 0f;
            searchesLeft--;

            if (searchesLeft <= 0 || !TryPickPoint(lastSeenPosition, searchRadius, out Vector3 point))
            {
                Enter(CrawlerState.Patrol);
                return;
            }

            MoveTo(point);
        }

        private void MoveTo(Vector3 destination)
        {
            if (!agent.isOnNavMesh)
            {
                return;
            }

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                goal = hit.position;
                agent.SetDestination(goal);
            }
        }

        /// <summary>목적지에 닿았는지. 경로가 없으면 닿은 것으로 본다.</summary>
        private bool Arrived()
        {
            if (!agent.isOnNavMesh)
            {
                return true;
            }

            if (agent.pathPending)
            {
                return false;
            }

            return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.6f);
        }

        /// <summary>NavMesh 위의 갈 수 있는 자리를 무작위로 고른다.</summary>
        private bool TryPickPoint(Vector3 around, float radius, out Vector3 point)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 circle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 candidate = around + new Vector3(circle.x, 0f, circle.y);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    continue;
                }

                // 코앞을 고르면 제자리걸음처럼 보인다.
                if (Vector3.Distance(hit.position, transform.position) < 2.5f)
                {
                    continue;
                }

                point = hit.position;
                return true;
            }

            point = transform.position;
            return false;
        }

        private void DriveAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedId, PlanarSpeed, speedDamping, Time.deltaTime);
            animator.SetBool(IsChasingId, IsChasing);
        }

        private void Chase()
        {
            // 본 직후에는 실제 위치를, 놓친 뒤에는 마지막으로 본 자리를 향한다.
            Vector3 destination = Time.time - lastSeenTime <= sightInterval * 2f
                ? player.position
                : lastSeenPosition;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
            }

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                Attack();
            }
        }

        private void Attack()
        {
            nextAttackTime = Time.time + attackCooldown;
            attackHitConsumed = false;
            AttackStarted?.Invoke();

            if (animator != null)
            {
                // 피해는 여기서 주지 않는다. 13f 의 AE_AttackHit 이 준다.
                animator.SetTrigger(AttackId);
                return;
            }

            // 애니메이터가 없으면 예고 구간 자체가 없다. 그때는 즉시 준다.
            OnAttackHit();
        }

        /// <summary>
        /// 예고 정점. 애니메이션 이벤트 <c>AE_AttackTell</c> (8f) 이 부른다.
        /// </summary>
        /// <remarks>
        /// 애니메이션 이벤트는 <b>Animator 가 붙은 오브젝트</b>로만 간다. Animator 는
        /// 자식 <c>Visual</c> 에 있으므로 <see cref="CrawlerAnimationEvents"/> 가
        /// 받아서 여기로 넘긴다.
        /// </remarks>
        public void OnAttackTell()
        {
            AttackTell?.Invoke();
        }

        /// <summary>
        /// 실제 피해. 애니메이션 이벤트 <c>AE_AttackHit</c> (13f) 이 부른다.
        /// </summary>
        /// <remarks>
        /// 한 공격에 한 번만 들어가게 <c>attackHitConsumed</c> 로 막는다. 전환이
        /// 끊기거나 프레임이 밀려 클립에 다시 들어와도 두 번 맞지 않아야 한다.
        /// </remarks>
        public void OnAttackHit()
        {
            if (attackHitConsumed || !IsAlive)
            {
                return;
            }

            attackHitConsumed = true;
            AttackHit?.Invoke();

            if (player == null || playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            // 예고 동안 물러났으면 안 맞는다. 이 재검사가 예고를 의미 있게 만든다.
            if (Vector3.Distance(transform.position, player.position) > attackRange + attackHitTolerance)
            {
                return;
            }

            Vector3 point = attackOrigin != null
                ? attackOrigin.position
                : transform.position + Vector3.up * eyeHeight;
            Vector3 direction = (player.position - point).normalized;
            playerDamageable.TakeDamage(new DamageInfo(attackDamage, point, direction, gameObject));
        }

        public void TakeDamage(in DamageInfo damage)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= damage.Amount;

            // 뒤에서 맞아도 쫓아온다. 안 그러면 등 뒤 사격이 무위험 행동이 된다.
            lastSeenTime = Time.time;
            lastSeenPosition = damage.Source != null ? damage.Source.transform.position : transform.position;
            Enter(CrawlerState.Chase);

            if (health <= 0)
            {
                Die();
                return;
            }

            if (animator != null)
            {
                animator.SetTrigger(HitId);
            }

            Hurt?.Invoke();
        }

        private void Die()
        {
            // Enter 를 쓰지 않는다 — Agent 를 곧 끄므로 speed 를 건드릴 이유가 없다.
            State = CrawlerState.Dead;
            SetChasing(false);

            // Dead 를 먼저 보낸다. Agent 를 끄면 이 프레임의 Speed 가 0 이 되는데,
            // 그 전에 Death 로 넘어가 있어야 Locomotion 이 한 프레임 끼어들지 않는다.
            if (animator != null)
            {
                animator.SetBool(DeadId, true);
                animator.SetFloat(SpeedId, 0f);
            }

            Died?.Invoke();

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            agent.enabled = false;

            // 시체가 총알과 플레이어를 막지 않게 한다.
            foreach (Collider c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            enabled = false;

            if (corpseLifetime <= 0f)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject, corpseLifetime);
            }
        }

        /// <summary>
        /// 플레이어를 태그로 찾는다. 태그를 늘리지 않기로 한 합의에 따른 것이다.
        /// </summary>
        private bool AcquirePlayer()
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found == null)
            {
                return false;
            }

            player = found.transform;
            playerDamageable = found.GetComponentInParent<IDamageable>();

            // 첫 프레임에 위치 차이가 통째로 속도로 잡히면 스폰하자마자 들킨다.
            previousPlayerPosition = player.position;
            playerSpeed = 0f;
            return true;
        }

        private bool CanSeePlayer()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = (player.position + Vector3.up * eyeHeight) - eye;

            if (toPlayer.sqrMagnitude > sightRange * sightRange)
            {
                return false;
            }

            if (Vector3.Angle(transform.forward, toPlayer) > sightAngle * 0.5f)
            {
                return false;
            }

            // EnemySight 는 Default 와 Player 만 포함한다. 맨 앞에 맞은 것이 플레이어여야 보인다.
            if (!Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, sightRange, GameLayers.EnemySight, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eye, sightRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Quaternion left = Quaternion.AngleAxis(-sightAngle * 0.5f, Vector3.up);
            Quaternion right = Quaternion.AngleAxis(sightAngle * 0.5f, Vector3.up);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(eye, left * transform.forward * sightRange);
            Gizmos.DrawRay(eye, right * transform.forward * sightRange);
        }
    }
}
