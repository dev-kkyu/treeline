using UnityEngine;
using UnityEngine.AI;

namespace Treeline
{
    /// <summary>
    /// 큰 것 — 화면을 채우는 4.5m 짜리. 느리고 **죽지 않는다.**
    /// 총을 맞으면 잠시 밀려났다가(<see cref="Stagger"/>) 다시 온다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Crawler"/> 와 코드를 공유하지 않는다. 규칙이 반대이기 때문이다 —
    /// 저쪽은 두 발에 죽고 이쪽은 죽지 않는다. 억지로 묶으면 「죽지 않는다」가
    /// 체력이 아주 큰 것으로 흐려진다. 세 번째 적(`Clinger`)에서 같은 코드가 또
    /// 반복되면 그때 공통부를 뗀다.
    /// </para>
    /// <para>
    /// IMPORTANT: 총폭 3.44m 다. **이 적이 지나갈 통로는 4m 이상**이어야 한다.
    /// 6장을 만드는 A 가 먼저 알아야 하는 수치다.
    /// </para>
    /// <para>
    /// IMPORTANT: 크기를 Transform 스케일로 만들지 않는다. 스케일을 쓰면
    /// <see cref="NavMeshAgent"/> 반경과 물리가 어긋난다. 모델 실측 크기로 만든다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Colossus : MonoBehaviour, IDamageable
    {
        [Header("감지")]
        [Tooltip("느린 대신 멀리서부터 알아본다.")]
        [SerializeField] private float sightRange = 30f;

        [Tooltip("시야각(전체). 기는 것보다 넓다 — 피하는 것이지 숨는 것이 아니다.")]
        [SerializeField] private float sightAngle = 160f;

        [Tooltip("눈 높이. 어깨 쌍봉 사이 골에 머리가 묻혀 있다.")]
        [SerializeField] private float eyeHeight = 3.6f;

        [SerializeField] private float sightInterval = 0.2f;

        [Tooltip("놓쳐도 오래 따라온다. 도망쳐서 떼어내는 상대다.")]
        [SerializeField] private float loseInterest = 12f;

        [Header("공격")]
        [Tooltip("팔이 무릎을 지나 바닥에 닿는다. 사거리가 길다.")]
        [SerializeField] private float attackRange = 3.2f;

        [SerializeField] private float attackCooldown = 2.4f;

        [Tooltip("한 대에 체력 3칸 중 2칸. 두 번 맞으면 죽는다.")]
        [SerializeField] private int attackDamage = 2;

        [Header("피격 — 죽지 않고 밀려난다")]
        [Tooltip("누적 피해가 이만큼 쌓이면 한 번 밀려난다.")]
        [SerializeField] private int staggerThreshold = 4;

        [Tooltip("밀려나는 거리.")]
        [SerializeField] private float staggerDistance = 2.5f;

        [Tooltip("밀려난 뒤 멈춰 있는 시간. 플레이어가 벌 수 있는 시간이 이것뿐이다.")]
        [SerializeField] private float staggerSeconds = 1.6f;

        private NavMeshAgent agent;
        private Transform player;
        private IDamageable playerDamageable;

        private float nextSightCheck;
        private float lastSeenTime = float.NegativeInfinity;
        private Vector3 lastSeenPosition;
        private float nextAttackTime;

        private int damageSinceStagger;
        private float staggerUntil = float.NegativeInfinity;

        /// <summary>
        /// **항상 참이다.** 총으로 죽일 수 없다는 것이 이 적의 정체다.
        /// </summary>
        /// <remarks>
        /// 체력을 아주 크게 주는 대신 이렇게 둔 이유는, 큰 값은 언젠가 깎이기
        /// 때문이다. 탄약을 다 쓰면 죽는 적이 되면 「죽지 않는 것이 있다」가
        /// 1장부터 6장까지 유지되지 않는다.
        /// </remarks>
        public bool IsAlive => true;

        /// <summary>추격 중인지. 연출과 사운드가 붙을 자리다.</summary>
        public bool IsChasing { get; private set; }

        /// <summary>밀려나 멈춰 있는 동안 참이다.</summary>
        public bool IsStaggered => Time.time < staggerUntil;

        /// <summary>
        /// 눈에 묻혀 멈춘 상태. 6장에서 밖에서 켜준다. 죽는 것이 아니라 갇히는 것이다.
        /// </summary>
        public bool IsBuried { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (gameObject.layer != GameLayers.Enemy)
            {
                Debug.LogWarning($"{name}: 레이어가 Enemy(12) 가 아니다. 사격 판정과 충돌 매트릭스가 어긋난다.", this);
            }

            if (!Mathf.Approximately(transform.localScale.x, 1f))
            {
                Debug.LogWarning($"{name}: Transform 스케일이 1 이 아니다. 에이전트 반경과 물리가 어긋난다.", this);
            }
        }

        private void OnEnable()
        {
            AcquirePlayer();
        }

        private void Update()
        {
            if (IsBuried)
            {
                return;
            }

            if (player == null && !AcquirePlayer())
            {
                return;
            }

            if (Time.time >= nextSightCheck)
            {
                nextSightCheck = Time.time + sightInterval;

                if (CanSeePlayer())
                {
                    lastSeenTime = Time.time;
                    lastSeenPosition = player.position;
                    IsChasing = true;
                }
                else if (IsChasing && Time.time - lastSeenTime > loseInterest)
                {
                    IsChasing = false;
                }
            }

            if (IsStaggered)
            {
                if (agent.isOnNavMesh && agent.hasPath)
                {
                    agent.ResetPath();
                }

                return;
            }

            if (!IsChasing)
            {
                if (agent.isOnNavMesh && agent.hasPath)
                {
                    agent.ResetPath();
                }

                return;
            }

            Chase();
        }

        private void Chase()
        {
            Vector3 destination = Time.time - lastSeenTime <= sightInterval * 2f
                ? player.position
                : lastSeenPosition;

            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
            }

            if (Vector3.Distance(transform.position, player.position) <= attackRange
                && Time.time >= nextAttackTime)
            {
                Attack();
            }
        }

        private void Attack()
        {
            nextAttackTime = Time.time + attackCooldown;

            if (playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            Vector3 point = transform.position + Vector3.up * eyeHeight;
            Vector3 direction = (player.position - transform.position).normalized;
            playerDamageable.TakeDamage(new DamageInfo(attackDamage, point, direction, gameObject));
        }

        /// <summary>
        /// 맞아도 죽지 않는다. 누적 피해가 임계에 닿을 때만 뒤로 밀려난다.
        /// </summary>
        public void TakeDamage(in DamageInfo damage)
        {
            if (IsBuried)
            {
                return;
            }

            // 뒤에서 쏴도 알아챈다.
            lastSeenTime = Time.time;
            lastSeenPosition = damage.Source != null ? damage.Source.transform.position : transform.position;
            IsChasing = true;

            if (IsStaggered)
            {
                // 밀려나 있는 동안 맞은 것은 다음 밀림에 쌓지 않는다.
                // 안 그러면 탄약만 있으면 영구히 묶어둘 수 있어 「도망친다」가 성립하지 않는다.
                return;
            }

            damageSinceStagger += Mathf.Max(1, damage.Amount);
            if (damageSinceStagger < staggerThreshold)
            {
                return;
            }

            damageSinceStagger = 0;
            Stagger(-damage.Direction);
        }

        /// <summary>뒤로 밀려나고 잠시 멈춘다. 총이 만드는 유일한 효과다.</summary>
        private void Stagger(Vector3 away)
        {
            staggerUntil = Time.time + staggerSeconds;

            away.y = 0f;
            if (away.sqrMagnitude < 0.001f)
            {
                away = -transform.forward;
            }

            Vector3 target = transform.position + away.normalized * staggerDistance;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, staggerDistance, NavMesh.AllAreas)
                && agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
        }

        /// <summary>눈에 묻어 멈춘다. 6장에서 부른다.</summary>
        public void Bury()
        {
            IsBuried = true;
            IsChasing = false;

            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        private bool AcquirePlayer()
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found == null)
            {
                return false;
            }

            player = found.transform;
            playerDamageable = found.GetComponentInParent<IDamageable>();
            return true;
        }

        private bool CanSeePlayer()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = (player.position + Vector3.up * 0.9f) - eye;

            if (toPlayer.sqrMagnitude > sightRange * sightRange)
            {
                return false;
            }

            if (Vector3.Angle(transform.forward, toPlayer) > sightAngle * 0.5f)
            {
                return false;
            }

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

            // 통로 제약. 이 폭이 안 들어가는 곳에는 놓을 수 없다.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2.25f, new Vector3(3.44f, 4.5f, 2.1f));
        }
    }
}
