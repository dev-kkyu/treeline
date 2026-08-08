using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

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

        [Header("청각 — 벽 뒤에 있어도 들린다")]
        [Tooltip("플레이어가 내는 소리 반경에 곱한다. 1 이면 들리는 대로, 0 이면 못 듣는다.")]
        [SerializeField] private float hearingScale = 1f;

        [Header("배회 — 절차 지점 사이를 오간다")]
        [Tooltip("돌 자리들. 비워두면 제자리에 선다. 기획의 「승강장을 배회하며」가 이것이다.")]
        [SerializeField] private Transform[] roamPoints;

        [Tooltip("한 지점에 닿으면 이만큼 서 있다가 다음으로 간다.")]
        [SerializeField] private float roamPauseSeconds = 3f;

        [Tooltip("배회할 때의 속도. 쫓을 때보다 느리다.")]
        [SerializeField] private float roamSpeed = 1.2f;

        [Tooltip("쫓을 때의 속도. 걷기(2.8)보다 빠르고 달리기(4.6)보다 느리다 — 뛰어야 벗어난다.")]
        [SerializeField] private float chaseSpeed = 3.4f;

        [Header("발각 — 돌아보고 포효한 뒤에 온다")]
        [Tooltip("알아채고 이만큼 멈춰 서서 플레이어를 본다. 0 이면 바로 쫓는다.")]
        [SerializeField] private float roarSeconds = 2f;

        [Tooltip("포효하는 동안 플레이어 쪽으로 도는 빠르기(도/초).")]
        [SerializeField] private float roarTurnSpeed = 220f;

        [Tooltip("못 보던 상태에서 알아챈 순간 한 번 불린다. 포효 소리 · 화면 흔들림 · 음악 큐.")]
        [SerializeField] private UnityEvent noticed;

        [Header("공격")]
        [Tooltip("팔이 무릎을 지나 바닥에 닿는다. 사거리가 길다.")]
        [SerializeField] private float attackRange = 3.2f;

        [SerializeField] private float attackCooldown = 2.4f;

        [Tooltip("붙잡히면 죽는다. 체력 3칸을 넘는 값이라 한 번에 끝난다 — 깎이는 것이 아니다.")]
        [SerializeField] private int attackDamage = 99;

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

        /// <summary>소리의 크기는 내는 쪽이 안다. 청각 판정은 이 값을 읽는다.</summary>
        private PlayerMotor playerMotor;

        private int roamIndex = -1;
        private float roamResumeTime;
        private bool roamArrived = true;
        private float roarUntil = float.NegativeInfinity;

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

        /// <summary>못 보던 상태에서 알아챈 순간 한 번. 코드에서 듣는 쪽은 이것을 쓴다.</summary>
        public event Action Noticed;

        /// <summary>밀려나 멈춰 있는 동안 참이다.</summary>
        public bool IsStaggered => Time.time < staggerUntil;

        /// <summary>알아채고 돌아보며 포효하는 동안 참이다. 이 동안은 움직이지 않는다.</summary>
        public bool IsRoaring => Time.time < roarUntil;

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

                // 보이거나 들리거나. 둘 중 하나면 알아챈다.
                if (CanSeePlayer() || CanHearPlayer())
                {
                    lastSeenTime = Time.time;
                    lastSeenPosition = player.position;

                    // 알아챈 「순간」에만 알린다. 쫓는 내내 부르면 연출이 아니라 소음이 된다.
                    if (!IsChasing)
                    {
                        IsChasing = true;
                        roarUntil = Time.time + roarSeconds;
                        Noticed?.Invoke();
                        noticed?.Invoke();
                    }
                }
                else if (IsChasing && Time.time - lastSeenTime > loseInterest)
                {
                    IsChasing = false;
                    // 놓친 자리에서부터 다시 돈다. 원래 순번으로 돌아가면 방금 있던
                    // 곳을 등지고 반대편으로 걸어가는 것이 보인다.
                    roamArrived = true;
                    roamResumeTime = Time.time;
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
                Roam();
                return;
            }

            // 알아채자마자 달려들지 않는다. 돌아보고 포효하는 동안 플레이어에게
            // 달아날 시간이 생기고, 「발각됐다」가 사건으로 읽힌다.
            if (IsRoaring)
            {
                Roar();
                return;
            }

            if (agent.isOnNavMesh)
            {
                agent.speed = chaseSpeed;
            }

            Chase();
        }

        /// <summary>멈춰 서서 플레이어 쪽으로 돈다. 다 돌면 쫓기 시작한다.</summary>
        private void Roar()
        {
            if (agent.isOnNavMesh && agent.hasPath)
            {
                agent.ResetPath();
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // 에이전트가 아니라 여기서 돌린다. 경로가 없으면 에이전트는 회전도 안 한다.
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(toPlayer, Vector3.up),
                roarTurnSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 절차 지점 사이를 오간다. 돌 자리를 안 주면 예전처럼 제자리에 선다.
        /// </summary>
        /// <remarks>
        /// 이것이 없으면 <b>시야만 끊으면 영원히 안 온다.</b> 기획이 「승강장을 배회하며
        /// 절차 지점 사이를 오간다」고 적어둔 것이 이 자리다. 6 장에서 히터가 도는 25 초
        /// 동안 숨어야 하는 이유가 여기서 나온다.
        /// </remarks>
        private void Roam()
        {
            if (!agent.isOnNavMesh)
            {
                return;
            }

            if (roamPoints == null || roamPoints.Length == 0)
            {
                if (agent.hasPath)
                {
                    agent.ResetPath();
                }

                return;
            }

            agent.speed = roamSpeed;

            if (agent.pathPending)
            {
                return;
            }

            bool arrived = !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.1f;
            if (!arrived)
            {
                return;
            }

            // 닿은 프레임에 한 번만 멈춰 선다. 쉬지 않고 돌면 순찰이 아니라 회전문이 된다.
            if (!roamArrived)
            {
                roamArrived = true;
                roamResumeTime = Time.time + roamPauseSeconds;
                agent.ResetPath();
                return;
            }

            if (Time.time < roamResumeTime)
            {
                return;
            }

            roamIndex = (roamIndex + 1) % roamPoints.Length;
            Transform next = roamPoints[roamIndex];
            if (next == null)
            {
                return;
            }

            agent.SetDestination(next.position);
            roamArrived = false;
        }

        /// <summary>
        /// 소리로 알아챈다. <b>시야와 달리 벽을 통과한다.</b>
        /// </summary>
        /// <remarks>
        /// 얼마나 큰 소리를 내는지는 움직이는 쪽이 정한다
        /// (<see cref="PlayerMotor.NoiseRadius"/>). 여기서는 그 거리 안에 있는지만 본다.
        /// 웅크리면 조용하고 달리면 멀리 들리는 것이 그 값에서 나온다.
        /// </remarks>
        private bool CanHearPlayer()
        {
            if (playerMotor == null || hearingScale <= 0f)
            {
                return false;
            }

            float radius = playerMotor.NoiseRadius * hearingScale;
            if (radius <= 0f)
            {
                return false;
            }

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.sqrMagnitude <= radius * radius;
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

        /// <summary>
        /// 붙잡는다. **체력을 깎는 것이 아니라 죽인다.**
        /// </summary>
        /// <remarks>
        /// 양손으로 붙들어 목을 따는 처형이라 한 번 닿으면 끝이다 (2026-08-08 팀 합의).
        /// 「죽지 않는 것이 있다」와 짝이 되는 규칙 — 이쪽은 죽일 수 없고 저쪽은
        /// 한 번에 죽인다. 연출은 모델이 들어와야 붙는다.
        /// </remarks>
        private void Attack()
        {
            nextAttackTime = Time.time + attackCooldown;

            if (playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            // 붙잡는 순간 마주 본다. 무엇에 잡혔는지 화면에 남아야 한다.
            Vector3 facing = player.position - transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
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

        /// <summary>
        /// 소리를 낸 것으로 친다. **레버가 내려가는 소리** 같은 것을 바깥에서 알린다.
        /// </summary>
        /// <remarks>
        /// 인자가 없는 것은 인스펙터의 <c>UnityEvent</c> 에 그대로 걸기 위해서다.
        /// 장치를 발동시킨 것은 플레이어이므로 플레이어의 자리를 들은 것으로 둔다.
        /// 이것이 있어야 <b>숨어서 레버만 당기고 버티는 것</b>이 안 된다.
        /// </remarks>
        public void Alert()
        {
            if (IsBuried || (player == null && !AcquirePlayer()))
            {
                return;
            }

            lastSeenTime = Time.time;
            lastSeenPosition = player.position;

            if (IsChasing)
            {
                return;
            }

            IsChasing = true;
            roarUntil = Time.time + roarSeconds;
            Noticed?.Invoke();
            noticed?.Invoke();
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
            playerMotor = found.GetComponentInParent<PlayerMotor>();
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

            // 배회 경로. 순서대로 이어 그린다.
            if (roamPoints == null || roamPoints.Length == 0)
            {
                return;
            }

            Gizmos.color = new Color(0.4f, 0.9f, 0.5f, 0.9f);
            for (int i = 0; i < roamPoints.Length; i++)
            {
                Transform a = roamPoints[i];
                Transform b = roamPoints[(i + 1) % roamPoints.Length];
                if (a == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(a.position, 0.6f);
                if (b != null)
                {
                    Gizmos.DrawLine(a.position, b.position);
                }
            }
        }
    }
}
