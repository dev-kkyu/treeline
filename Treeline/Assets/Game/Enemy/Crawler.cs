using UnityEngine;
using UnityEngine.AI;

namespace Treeline
{
    /// <summary>
    /// 기는 것 — 1단계의 유일한 적. 네발로 낮게 깔려 빠르고, 권총 한두 발에 죽는다.
    /// 플레이어를 보면 쫓아가 근접 공격하고, 놓치면 마지막으로 본 자리까지 갔다가 포기한다.
    /// </summary>
    /// <remarks>
    /// 모델이 없어도 1단계가 진행되어야 하므로 이 스크립트는 **외형을 전혀 모른다.**
    /// 캡슐에 붙여도 그대로 돌아가고, 나중에 리깅된 모델을 자식으로 끼우면 된다.
    /// 애니메이션 연결은 모델이 들어온 뒤에 붙인다.
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

        [Tooltip("놓친 뒤 이 시간이 지나면 추격을 포기한다.")]
        [SerializeField] private float loseInterest = 4f;

        [Header("공격")]
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private int attackDamage = 1;

        [Header("사망")]
        [Tooltip("죽고 나서 사라지기까지. 0 이면 즉시 사라진다.")]
        [SerializeField] private float corpseLifetime = 3f;

        private NavMeshAgent agent;
        private Transform player;
        private IDamageable playerDamageable;

        private int health;
        private float nextSightCheck;
        private float lastSeenTime = float.NegativeInfinity;
        private Vector3 lastSeenPosition;
        private float nextAttackTime;

        public bool IsAlive => health > 0;

        /// <summary>추격 중인지. 연출·사운드가 붙을 자리라 밖에서 읽을 수 있게 둔다.</summary>
        public bool IsChasing { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = maxHealth;

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

            if (playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            Vector3 point = transform.position + Vector3.up * eyeHeight;
            Vector3 direction = (player.position - transform.position).normalized;
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
            IsChasing = true;

            if (health <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            IsChasing = false;

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
