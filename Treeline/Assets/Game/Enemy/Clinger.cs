using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Treeline
{
    /// <summary>
    /// 천장에 붙어 기다리다 아래를 지나면 떨어지는 적. 4 · 5장에 여럿이 나온다.
    /// </summary>
    /// <remarks>
    /// <b>이 적은 두 공간에서 산다.</b> 천장에서는 <see cref="NavMeshAgent"/> 가 꺼져
    /// 있고 구(球) 하나로만 존재하며, 착지한 뒤에야 캡슐과 Agent 가 켜진다. 둘을 동시에
    /// 켜지 않는 것이 이 클래스의 전부라고 해도 된다.
    /// <para>
    /// 값과 상태 흐름은 리깅 세션이 남긴 계약을 그대로 옮긴 것이다 —
    /// <c>docs/enemy-sheets/mockups/codex/clinger/final/animation/unity-integration.md</c>.
    /// 애니메이션 이벤트 넷은 <see cref="ClingerAnimationEvents"/> 가 중계한다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Clinger : MonoBehaviour, IDamageable, IHunter
    {
        /// <summary>Animator 의 <c>Mode</c> 파라미터와 같은 값이다. 숫자를 바꾸지 않는다.</summary>
        public enum ClingerMode
        {
            Ceiling = 0,
            Falling = 1,
            Landing = 2,
            Ground = 3,
            Dead = 4,
        }

        [Header("체력")]
        [Tooltip("약하지만 여럿이다. 산탄총 한 발이나 권총 두 발.")]
        [SerializeField] private int maxHealth = 2;

        [Header("천장 — 언제 떨어지나")]
        [Tooltip("앵커 바로 아래 이 반경 안에 플레이어가 들어오면 떨어진다.")]
        [SerializeField] private float dropRadius = 3.2f;

        [Tooltip("들어온 뒤 실제로 떨어지기까지. 이 사이에 경련이 예고로 나간다.")]
        [SerializeField] private float dropDelay = 0.45f;

        [Tooltip("이보다 가까이 붙어 있으면 안 떨어진다. 머리 위에서 터지면 사고로 읽힌다.")]
        [SerializeField] private float minDropHeight = 1.5f;

        [Tooltip("대기 중 경련 간격. 가만히 있으면 배경이 된다.")]
        [SerializeField] private Vector2 tremorInterval = new Vector2(3.5f, 9f);

        [Header("낙하")]
        [Tooltip("낙하 가속. 중력보다 약간 세게 잡아야 무겁게 떨어진다.")]
        [SerializeField] private float fallGravity = 24f;

        [Tooltip("천장 구의 반경. 이 크기로 바닥을 찾는다.")]
        [SerializeField] private float probeRadius = 0.52f;

        [Tooltip("천장 구의 중심이 원점에서 얼마나 아래인가. 접힌 몸의 실제 중심이다.")]
        [SerializeField] private float ceilingCenterY = -0.65f;

        [Header("지상")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.4f;
        [SerializeField] private int attackDamage = 1;

        [Tooltip("이벤트 프레임에서 다시 잰다. 빗나간 공격이 맞는 것을 막는다.")]
        [SerializeField] private float attackHitTolerance = 0.45f;

        [Tooltip("착지한 뒤 이만큼 떨어지면 추격을 포기하고 배회한다.")]
        [SerializeField] private float loseInterest = 22f;

        [Header("시체")]
        [SerializeField] private float corpseLifetime = 4f;

        [Header("붙이는 것")]
        [SerializeField] private Animator animator;

        [Tooltip("천장에 매달려 있는 동안만 켜진다.")]
        [SerializeField] private SphereCollider ceilingCollider;

        [Tooltip("착지한 뒤에만 켜진다.")]
        [SerializeField] private CapsuleCollider groundCollider;

        [Tooltip("아래턱 앞. 공격 판정을 여기서 낸다.")]
        [SerializeField] private Transform attackOrigin;

        [Tooltip("바닥을 찾을 때 무시할 층. 플레이어와 적은 뺀다.")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("소리 · 연출을 붙일 자리")]
        [SerializeField] private UnityEvent tremored;
        [SerializeField] private UnityEvent dropped;
        [SerializeField] private UnityEvent landed;
        [SerializeField] private UnityEvent attackHit;
        [SerializeField] private UnityEvent hurt;
        [SerializeField] private UnityEvent died;

        private static readonly int ModeId = Animator.StringToHash("Mode");
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int TremorId = Animator.StringToHash("Tremor");
        private static readonly int DropId = Animator.StringToHash("Drop");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");

        private NavMeshAgent agent;
        private Transform player;
        private IDamageable playerDamageable;

        private int health;
        private Vector3 anchor;
        private float fallSpeed;
        private float nextTremorTime;
        private float dropAt = float.PositiveInfinity;
        private float nextAttackTime;
        private int attackSequence;
        private int servedAttack = -1;
        private bool corpseFalling;

        /// <summary>지금 어느 공간에 있는지.</summary>
        public ClingerMode Mode { get; private set; } = ClingerMode.Ceiling;

        public bool IsAlive => health > 0;

        /// <summary>추격 음악이 읽는다. 천장에서 기다리는 것은 추격이 아니다.</summary>
        public bool IsHunting => IsAlive && Mode == ClingerMode.Ground;

        public event Action Dropped;
        public event Action Landed;
        public event Action Died;

        private void Awake()
        {
            health = maxHealth;
            agent = GetComponent<NavMeshAgent>();

            // 천장에 매달린 동안 Agent 가 켜져 있으면 NavMesh 로 끌려 내려간다.
            agent.enabled = false;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // 갈고리가 무는 자리. 몸통 중심도 발밑도 아니다.
            anchor = transform.position;

            SetColliders(ceiling: true, ground: false);
            ApplyMode(ClingerMode.Ceiling);
            nextTremorTime = Time.time + UnityEngine.Random.Range(tremorInterval.x, tremorInterval.y);
        }

        private void Update()
        {
            if (player == null && !AcquirePlayer())
            {
                return;
            }

            switch (Mode)
            {
                case ClingerMode.Ceiling: Wait(); break;
                case ClingerMode.Falling: Fall(); break;
                case ClingerMode.Ground: Hunt(); break;
                case ClingerMode.Dead: if (corpseFalling) FallCorpse(); break;
            }
        }

        // ── 천장 ──────────────────────────────────────────────────────────────

        private void Wait()
        {
            Vector3 flat = player.position - anchor;
            flat.y = 0f;
            bool below = anchor.y - player.position.y >= minDropHeight;
            bool near = flat.sqrMagnitude <= dropRadius * dropRadius;

            if (near && below)
            {
                if (float.IsPositiveInfinity(dropAt))
                {
                    // 떨어지기 전에 한 번 떤다. 예고 없이 떨어지면 사고로 읽힌다.
                    dropAt = Time.time + dropDelay;
                    Tremor();
                }
            }
            else
            {
                dropAt = float.PositiveInfinity;
            }

            if (Time.time >= dropAt)
            {
                Drop();
                return;
            }

            if (Time.time >= nextTremorTime)
            {
                nextTremorTime = Time.time + UnityEngine.Random.Range(tremorInterval.x, tremorInterval.y);
                Tremor();
            }
        }

        private void Tremor()
        {
            if (animator != null)
            {
                animator.SetTrigger(TremorId);
            }

            tremored?.Invoke();
        }

        /// <summary>밖에서 떨어뜨린다. 예열 구간처럼 한꺼번에 몰 때 쓴다.</summary>
        public void Drop()
        {
            if (Mode != ClingerMode.Ceiling || !IsAlive)
            {
                return;
            }

            dropAt = float.PositiveInfinity;
            ApplyMode(ClingerMode.Falling);

            if (animator != null)
            {
                animator.SetTrigger(DropId);
            }
        }

        // ── 낙하 ──────────────────────────────────────────────────────────────

        /// <summary>`Unfold_Drop` 9f. 천장 고정만 푼다. 구는 그대로 두고 낙하를 시작한다.</summary>
        public void AE_DropRelease()
        {
            if (Mode != ClingerMode.Falling)
            {
                return;
            }

            fallSpeed = 0f;
            Dropped?.Invoke();
            dropped?.Invoke();
        }

        private void Fall()
        {
            fallSpeed += fallGravity * Time.deltaTime;
            float step = fallSpeed * Time.deltaTime;

            Vector3 center = transform.position + Vector3.up * ceilingCenterY;
            if (Physics.SphereCast(center, probeRadius, Vector3.down, out RaycastHit hit,
                    step + probeRadius, groundMask, QueryTriggerInteraction.Ignore))
            {
                // 접점으로 옮기고 나서 Land 를 시작한다. Land 1f 가 곧 이 자세라 안 튄다.
                transform.position = hit.point;
                ApplyMode(ClingerMode.Landing);
                return;
            }

            transform.position += Vector3.down * step;
        }

        /// <summary>`Land` 14f. 여기서 처음 충돌체와 Agent 를 바꾼다. 자리는 이미 맞아 있다.</summary>
        public void AE_EnableGroundMode()
        {
            if (Mode != ClingerMode.Landing)
            {
                return;
            }

            SetColliders(ceiling: false, ground: true);

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }

            agent.enabled = true;
            if (agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }

            ApplyMode(ClingerMode.Ground);
            Landed?.Invoke();
            landed?.Invoke();
        }

        // ── 지상 ──────────────────────────────────────────────────────────────

        private void Hunt()
        {
            if (!agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            agent.isStopped = false;

            if (distance <= loseInterest)
            {
                agent.SetDestination(player.position);
            }

            if (distance <= attackRange && Time.time >= nextAttackTime
                && playerDamageable != null && playerDamageable.IsAlive)
            {
                Attack();
            }

            if (animator != null)
            {
                Vector3 v = agent.velocity;
                v.y = 0f;
                animator.SetFloat(SpeedId, v.magnitude, 0.1f, Time.deltaTime);
            }
        }

        private void Attack()
        {
            nextAttackTime = Time.time + attackCooldown;
            attackSequence++;

            // 몸을 돌려놓고 친다. 옆으로 물면 무엇에 맞았는지 안 보인다.
            Vector3 look = player.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(look, Vector3.up);
            }

            if (animator != null)
            {
                animator.SetTrigger(AttackId);
            }
        }

        /// <summary>`Attack_Underjaw` 12f. 이 프레임에 다시 재서 한 번만 준다.</summary>
        public void AE_AttackHit()
        {
            if (Mode != ClingerMode.Ground || servedAttack == attackSequence)
            {
                return;
            }

            servedAttack = attackSequence;

            if (playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            Vector3 from = attackOrigin != null ? attackOrigin.position : transform.position;
            if (Vector3.Distance(from, player.position) > attackRange + attackHitTolerance)
            {
                return;
            }

            playerDamageable.TakeDamage(new DamageInfo(
                attackDamage, from, (player.position - from).normalized, gameObject));
            attackHit?.Invoke();
        }

        // ── 피격 · 사망 ───────────────────────────────────────────────────────

        public void TakeDamage(in DamageInfo damage)
        {
            if (!IsAlive)
            {
                return;
            }

            health -= damage.Amount;

            if (health <= 0)
            {
                Die();
                return;
            }

            if (animator != null)
            {
                // 천장에서는 피격 자세가 없다. 리그가 다른 공간이라 경련으로 대신한다.
                animator.SetTrigger(Mode == ClingerMode.Ground ? HitId : TremorId);
            }

            // 맞았으면 떨어진다. 천장의 놈을 안전하게 쏠 수 있으면 위협이 사라진다.
            if (Mode == ClingerMode.Ceiling)
            {
                Drop();
            }

            hurt?.Invoke();
        }

        private void Die()
        {
            bool onCeiling = Mode == ClingerMode.Ceiling || Mode == ClingerMode.Falling;
            ApplyMode(ClingerMode.Dead);

            if (animator != null)
            {
                animator.SetBool(DeadId, true);
                animator.SetFloat(SpeedId, 0f);
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            agent.enabled = false;
            corpseFalling = onCeiling;
            fallSpeed = 0f;

            Died?.Invoke();
            died?.Invoke();

            if (corpseLifetime > 0f)
            {
                Destroy(gameObject, corpseLifetime);
            }
        }

        /// <summary>`Death_Fall` 4f · `Death` 3f. 매달린 것을 놓고 충돌체를 전부 끈다.</summary>
        public void AE_DeathRelease()
        {
            SetColliders(ceiling: false, ground: false);
        }

        /// <summary>천장에서 죽었으면 시체가 떨어져야 한다. Agent 는 이미 꺼져 있다.</summary>
        private void FallCorpse()
        {
            fallSpeed += fallGravity * Time.deltaTime;
            float step = fallSpeed * Time.deltaTime;

            if (Physics.SphereCast(transform.position + Vector3.up * ceilingCenterY, probeRadius,
                    Vector3.down, out RaycastHit hit, step + probeRadius, groundMask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                corpseFalling = false;

                // 바닥에 닿아야 지상 사망 자세로 넘어간다. 시간으로 재면 높은 데서
                // 떨어질 때 공중에서 자세가 바뀐다.
                if (animator != null)
                {
                    animator.SetTrigger(GroundedId);
                }

                return;
            }

            transform.position += Vector3.down * step;
        }

        // ── 잡일 ──────────────────────────────────────────────────────────────

        private void ApplyMode(ClingerMode next)
        {
            Mode = next;

            if (animator != null)
            {
                animator.SetInteger(ModeId, (int)next);
            }
        }

        private void SetColliders(bool ceiling, bool ground)
        {
            if (ceilingCollider != null)
            {
                ceilingCollider.enabled = ceiling;
            }

            if (groundCollider != null)
            {
                groundCollider.enabled = ground;
            }
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

        private void OnDrawGizmosSelected()
        {
            Vector3 at = Application.isPlaying ? anchor : transform.position;

            // 갈고리가 무는 자리와 떨어뜨리는 범위
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(at + Vector3.up * ceilingCenterY, probeRadius);
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(at, dropRadius);

            // 실제로 떨어질 자리
            if (Physics.Raycast(at, Vector3.down, out RaycastHit hit, 30f, groundMask, QueryTriggerInteraction.Ignore))
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(at, hit.point);
                Gizmos.DrawWireCube(hit.point, new Vector3(1.2f, 0.02f, 1.2f));
            }
        }
    }
}
