using System;
using System.Collections;
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
    public sealed class Colossus : MonoBehaviour, IDamageable, IHunter
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

        [Header("애니메이션")]
        [Tooltip("AC_Colossus. 비우면 모델 없이 돌던 회색 박스 동작 그대로다.")]
        [SerializeField] private Animator animator;

        [Tooltip("붙잡은 플레이어를 매달 자리. DEF_Digits.R 의 자식인 빈 트랜스폼.")]
        [SerializeField] private Transform grabAnchor;

        [Tooltip("입. 붙잡힌 동안 플레이어 카메라가 이쪽을 본다. DEF_HeadTip.")]
        [SerializeField] private Transform mouthTarget;

        [Tooltip("카메라를 뺏는 데 쓰는 시간. 즉시 끊으면 화면이 고장난 줄 안다.")]
        [SerializeField] private float grabHandoverSeconds = 0.3f;

        [Tooltip("붙잡힌 동안 카메라가 도는 최대 속도(도/초). 넘기면 멀미가 난다.")]
        [SerializeField] private float grabTurnSpeed = 90f;

        [Tooltip("8f 재검사에서 봐주는 여유(m). 0 이면 딱 사거리로 자른다.")]
        [SerializeField] private float grabLockSlack = 0.6f;

        [Tooltip("붙잡히는 순간과 무는 순간 화면이 흔들리는 크기(도).")]
        [SerializeField] private float grabShakeDegrees = 4.5f;

        [Tooltip("흔들림이 잦아드는 시간.")]
        [SerializeField] private float grabShakeSeconds = 0.5f;

        [Header("발소리 — 거리로 낸다")]
        [Tooltip("접지 소리를 낼 곳. 비우면 발소리가 없다.")]
        [SerializeField] private AudioSource stepSource;

        [Tooltip("번갈아 쓴다. 하나만 두면 기관총처럼 들린다.")]
        [SerializeField] private AudioClip[] stepClips;

        [Tooltip("이만큼 갈 때마다 한 발. 시간이 아니라 거리라서 배회와 추격이 저절로 갈린다.")]
        [SerializeField] private float stepStride = 1.1f;

        [Tooltip("배회할 때 / 쫓을 때 발소리 크기.")]
        [SerializeField, Range(0f, 1f)] private float roamStepVolume = 0.55f;

        [SerializeField, Range(0f, 1f)] private float chaseStepVolume = 0.9f;

        [Tooltip("움직이는 동안 까는 루프. colossus-walk-loop.")]
        [SerializeField] private AudioSource moveLoop;

        [Tooltip("이 속도를 넘으면 움직이는 것으로 본다.")]
        [SerializeField] private float movingSpeed = 0.15f;

        [Header("발각 — 돌아보고 포효한 뒤에 온다")]
        [Tooltip("알아채고 이만큼 멈춰 서서 플레이어를 본다. Colossus_Roar 클립 길이(1.17초)에 맞춘 값이다.")]
        [SerializeField] private float roarSeconds = 1.2f;

        [Tooltip("포효하는 동안 플레이어 쪽으로 도는 빠르기(도/초).")]
        [SerializeField] private float roarTurnSpeed = 220f;

        [Tooltip("못 보던 상태에서 알아챈 순간 한 번 불린다. 포효 소리 · 화면 흔들림 · 음악 큐.")]
        [SerializeField] private UnityEvent noticed;

        [Tooltip("놓쳐서 배회로 돌아가는 순간 한 번. 추격 음악을 끄는 자리다.")]
        [SerializeField] private UnityEvent lost;

        [Tooltip("포효 클립 21f. 알아챈 순간이 아니라 턱이 열리는 순간에 소리를 낸다.")]
        [SerializeField] private UnityEvent roarSound;

        [Tooltip("위협 클립 25f. 아직 소리가 없다.")]
        [SerializeField] private UnityEvent threatSound;

        [Tooltip("붙잡는 순간(8f). 놓쳤을 때는 울리지 않는다.")]
        [SerializeField] private UnityEvent grabSound;

        [Tooltip("무는 순간(90f). 화면이 닫히는 것과 같은 프레임이다.")]
        [SerializeField] private UnityEvent killSound;

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

        // 애니메이터 파라미터는 이름을 매 프레임 해싱하지 않는다.
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int ChasingHash = Animator.StringToHash("IsChasing");
        private static readonly int BuriedHash = Animator.StringToHash("IsBuried");
        private static readonly int RoarHash = Animator.StringToHash("Roar");
        private static readonly int GrabHash = Animator.StringToHash("Grab");
        private static readonly int MissHash = Animator.StringToHash("Miss");
        private static readonly int StaggerHash = Animator.StringToHash("Stagger");
        private static readonly int HitReactHash = Animator.StringToHash("HitReact");

        /// <summary>상태 이름 해시다. 위 트리거 해시와 값이 같아도 쓰는 곳이 다르다.</summary>
        private static readonly int RoarStateHash = Animator.StringToHash("Roar");

        private static readonly int GrabStateHash = Animator.StringToHash("GrabExecute");

        private GameObject grabbed;
        private Transform grabbedEye;
        private Coroutine grabRoutine;
        private float grabShakeStart = float.NegativeInfinity;

        private Vector3 lastStepPosition;
        private float travelled;
        private int lastStep = -1;

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

        /// <summary>그것은 죽지 않으므로 추격 여부가 곧 사냥 여부다. <see cref="IHunter"/> 참고.</summary>
        public bool IsHunting => IsChasing;

        /// <summary>못 보던 상태에서 알아챈 순간 한 번. 코드에서 듣는 쪽은 이것을 쓴다.</summary>
        public event Action Noticed;

        /// <summary>
        /// 놓쳐서 배회로 돌아가는 순간 한 번. <see cref="Noticed"/> 와 짝이다.
        /// </summary>
        /// <remarks>
        /// 추격 음악이 <b>루프</b>라 켜는 자리만 있으면 영영 안 꺼진다. 켜는 쪽과 끄는
        /// 쪽을 같은 모양으로 둬야 붙이는 사람이 한쪽을 빠뜨리지 않는다.
        /// </remarks>
        public event Action Lost;

        /// <summary>밀려나 멈춰 있는 동안 참이다.</summary>
        public bool IsStaggered => Time.time < staggerUntil;

        /// <summary>
        /// 알아채고 돌아보며 포효하는 동안 참이다. 이 동안은 움직이지 않는다.
        /// </summary>
        /// <remarks>
        /// <see cref="roarSeconds"/> 로만 재면 안 된다. 트리거를 보내고 <c>Roar</c> 상태로
        /// <b>전이하는 데 걸리는 시간</b>이 앞에 붙어서, 시간이 다 차는 순간 클립이 아직
        /// 남아 있다. 실측 0.33 초 뒤에 시작해 1.50 초에 끝났다 — 1.2 초로 끊으면
        /// 포효 끝자락에서 걸어나간다. 그래서 <b>클립이 살아 있는 동안은 계속 붙잡는다.</b>
        /// 애니메이터가 없으면 예전처럼 시간만 본다.
        /// </remarks>
        public bool IsRoaring => Time.time < roarUntil || InRoarState();

        private bool InRoarState()
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.shortNameHash == RoarStateHash && state.normalizedTime < 1f;
        }

        /// <summary>플레이어를 붙들고 있는 동안 참이다. 클립이 끝을 정하므로 코드가 끼어들지 않는다.</summary>
        public bool IsGrabbing => grabbed != null;

        /// <summary>
        /// 눈에 묻혀 멈춘 상태. 6장에서 밖에서 켜준다. 죽는 것이 아니라 갇히는 것이다.
        /// </summary>
        public bool IsBuried { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

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
            lastStepPosition = transform.position;
            travelled = 0f;
        }

        private void Update()
        {
            DriveAnimator();
            DriveFootsteps();

            if (IsBuried)
            {
                return;
            }

            // 붙잡은 동안은 클립이 전부를 몬다. 여기서 경로나 회전을 건드리면
            // 입 앞으로 채가는 8프레임이 어긋난다.
            if (IsGrabbing)
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
                        if (animator != null)
                        {
                            animator.SetTrigger(RoarHash);
                        }

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

                    Lost?.Invoke();
                    lost?.Invoke();
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

            // 돌 자리가 없어도 속도는 되돌린다. 안 그러면 한 번 쫓은 뒤로 추격 속도가
            // 그대로 남아, 서 있기만 하는 개체가 빠른 채로 굳는다.
            agent.speed = roamSpeed;

            if (roamPoints == null || roamPoints.Length == 0)
            {
                if (agent.hasPath)
                {
                    agent.ResetPath();
                }

                return;
            }

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
        /// 양손으로 붙들어 입으로 가져가는 처형이라 한 번 닿으면 끝이다 (2026-08-08 팀 합의).
        /// 「죽지 않는 것이 있다」와 짝이 되는 규칙 — 이쪽은 죽일 수 없고 저쪽은 한 번에 죽인다.
        /// <para>
        /// **여기서는 피해를 주지 않는다.** 트리거만 보내고 실제 죽음은 클립 90f 의
        /// <see cref="AE_GrabKill"/> 이 준다. 8f 에서 한 번 더 재기 때문에
        /// <b>마지막 순간에 빠져나가는 것이 가능하고, 그것이 이 적을 상대하는 유일한 기술</b>이다.
        /// </para>
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

            if (animator != null)
            {
                animator.SetTrigger(GrabHash);
                return;
            }

            // 모델이 없던 시절의 경로. 회색 박스 씬과 1 단계 검증이 그대로 유효해야 한다.
            KillPlayer();
        }

        /// <summary>
        /// 클립 8f — **여기서 잡을지 놓칠지가 갈린다.**
        /// </summary>
        /// <remarks>
        /// 1~8f 를 <c>Grab_Miss</c> 와 같은 자세로 공유하므로, 플레이어는 그 0.27 초
        /// 동안 결과를 모른다. 사거리를 <b>다시</b> 재서 벗어났으면 헛손질로 넘긴다.
        /// </remarks>
        public void AE_GrabLock()
        {
            if (player == null || playerDamageable == null || !playerDamageable.IsAlive
                || Vector3.Distance(transform.position, player.position) > attackRange + grabLockSlack)
            {
                if (animator != null)
                {
                    animator.SetTrigger(MissHash);
                }

                return;
            }

            CapturePlayer();

            // 잡힌 뒤에만 울린다. 헛손질에는 아무 소리도 안 나야 「빠져나왔다」가 읽힌다.
            grabSound?.Invoke();
            grabShakeStart = Time.time;
        }

        /// <summary>클립 90f — 턱이 닫힌다. **여기서 죽는다.**</summary>
        public void AE_GrabKill()
        {
            killSound?.Invoke();
            grabShakeStart = Time.time;

            // 화면을 닫는 것이 무는 것과 같은 프레임이다. 페이드로 넘기면 「서서히
            // 죽는다」가 되고, 이 적은 그런 식으로 죽이지 않는다.
            if (grabbed != null)
            {
                PlayerHud hud = grabbed.GetComponentInChildren<PlayerHud>(true);
                if (hud != null)
                {
                    hud.Blackout(0.05f);
                }
            }

            KillPlayer();
        }

        /// <summary>클립 21f — 포효 소리를 내는 자리. 인스펙터에서 <c>AudioSource.Play</c> 를 건다.</summary>
        public void AE_ColossusRoar()
        {
            roarSound?.Invoke();
        }

        /// <summary>클립 25f — 위협 소리를 내는 자리. 아직 클립이 없다.</summary>
        public void AE_ColossusThreat()
        {
            threatSound?.Invoke();
        }

        private void KillPlayer()
        {
            if (playerDamageable == null || !playerDamageable.IsAlive)
            {
                return;
            }

            Vector3 point = transform.position + Vector3.up * eyeHeight;
            Vector3 direction = (player.position - transform.position).normalized;
            playerDamageable.TakeDamage(new DamageInfo(attackDamage, point, direction, gameObject));
        }

        /// <summary>플레이어에게서 조작을 뺏어 손에 매단다.</summary>
        private void CapturePlayer()
        {
            if (grabAnchor == null || grabbed != null)
            {
                return;
            }

            grabbed = player.gameObject;

            // 걷기 · 시선 · 중력을 전부 끈다. 하나라도 남으면 클립과 싸운다 —
            // 특히 PlayerLook 은 매 프레임 회전을 덮어써서 카메라가 안 넘어간다.
            SetPlayerControl(false);

            grabbedEye = grabbed.GetComponentInChildren<Camera>() != null
                ? grabbed.GetComponentInChildren<Camera>().transform
                : grabbed.transform;

            // 월드 위치를 유지한 채 붙인다. 순간이동으로 손에 박아 넣으면 한 프레임에
            // 화면이 튀어 「잡혔다」가 아니라 「끊겼다」로 읽힌다. 대신 아래에서 끌어당긴다.
            grabbed.transform.SetParent(grabAnchor, worldPositionStays: true);

            PlayerHud hud = grabbed.GetComponentInChildren<PlayerHud>(true);
            if (hud != null)
            {
                hud.HideForCutscene();
            }

            grabRoutine = StartCoroutine(HoldPlayer());
        }

        /// <summary>
        /// 붙잡힌 동안 카메라를 입 쪽으로 넘기고 계속 그쪽을 보게 둔다.
        /// </summary>
        /// <remarks>
        /// 즉시 고정하지 않는 것이 핵심이다. <see cref="grabHandoverSeconds"/> 동안
        /// 넘기고 그 뒤로는 각속도를 <see cref="grabTurnSpeed"/> 로 묶는다. 이 제한이
        /// 없으면 <b>화면이 고장난 것처럼 보이고 멀미가 난다.</b>
        /// </remarks>
        private IEnumerator HoldPlayer()
        {
            Transform look = mouthTarget != null ? mouthTarget : transform;
            Quaternion from = grabbedEye.rotation;
            Vector3 fromLocal = grabbed.transform.localPosition;
            float started = Time.time;

            while (grabbed != null)
            {
                // 클립이 끝나면 놓는다. 죽으면 장이 다시 시작하므로 평소에는 여기까지
                // 오지 않지만, 안 죽고 클립만 끝나는 길이 하나라도 있으면 플레이어가
                // 손에 매달린 채 조작 불능으로 남는다. 실제로 그 상태를 봤다.
                if (animator != null && Time.time - started > 0.3f
                    && animator.GetCurrentAnimatorStateInfo(0).shortNameHash != GrabStateHash)
                {
                    break;
                }

                float pull = grabHandoverSeconds > 0f
                    ? Mathf.Clamp01((Time.time - started) / grabHandoverSeconds)
                    : 1f;

                // 손 안으로 끌어당긴다. 이게 없으면 팔 길이만큼 떨어진 채 매달려서
                // 입이 화면을 채우는 40~64f 가 통째로 안 나온다.
                grabbed.transform.localPosition =
                    Vector3.Lerp(fromLocal, Vector3.zero, pull * pull * (3f - 2f * pull));

                Vector3 toMouth = look.position - grabbedEye.position;
                if (toMouth.sqrMagnitude > 0.0001f)
                {
                    Quaternion target = Quaternion.LookRotation(toMouth, Vector3.up);
                    float elapsed = Time.time - started;

                    if (elapsed < grabHandoverSeconds && grabHandoverSeconds > 0f)
                    {
                        // 뺏는 구간. 원래 보던 방향에서 부드럽게 옮겨온다.
                        float k = elapsed / grabHandoverSeconds;
                        grabbedEye.rotation = Quaternion.Slerp(from, target, k * k * (3f - 2f * k));
                    }
                    else
                    {
                        grabbedEye.rotation = Quaternion.RotateTowards(
                            grabbedEye.rotation, target, grabTurnSpeed * Time.deltaTime);
                    }
                }

                // 붙잡힌 순간과 무는 순간에만 흔든다. 계속 흔들면 멀미가 나고,
                // 무엇보다 그 두 지점이 사건이라는 것이 흐려진다. 롤(z)을 같이 섞는
                // 것이 핵심이다 — 목을 잡혀 흔들리는 느낌은 좌우가 아니라 기울기에서 온다.
                float since = Time.time - grabShakeStart;
                if (since >= 0f && since < grabShakeSeconds && grabShakeSeconds > 0f)
                {
                    float left = 1f - since / grabShakeSeconds;
                    float amount = grabShakeDegrees * left * left;
                    float t = Time.time * 34f;
                    grabbedEye.rotation *= Quaternion.Euler(
                        Mathf.Sin(t * 1.7f) * amount,
                        Mathf.Sin(t) * amount,
                        Mathf.Sin(t * 2.3f) * amount * 0.7f);
                }

                yield return null;
            }

            grabRoutine = null;
            ReleasePlayer();
        }

        /// <summary>손에서 내려놓는다. 죽어서 장이 다시 시작하든, 클립이 끊기든 여기로 온다.</summary>
        private void ReleasePlayer()
        {
            if (grabbed == null)
            {
                return;
            }

            if (grabRoutine != null)
            {
                StopCoroutine(grabRoutine);
                grabRoutine = null;
            }

            grabbed.transform.SetParent(null, worldPositionStays: true);
            SetPlayerControl(true);
            grabbed = null;
            grabbedEye = null;
        }

        /// <summary>
        /// 플레이어의 조작을 통째로 켜고 끈다.
        /// </summary>
        /// <remarks>
        /// <see cref="CharacterController"/> 는 켜져 있는 동안 위치를 되돌리므로 껐다
        /// 옮긴다. 입력만 끄면 <see cref="PlayerMotor"/> 가 계속 돌면서 꺼진 컨트롤러에
        /// <c>Move</c> 를 걸고 중력으로 손에서 빠져나간다 — 삭도에서 이미 밟은 함정이다.
        /// </remarks>
        private void SetPlayerControl(bool on)
        {
            if (grabbed == null)
            {
                return;
            }

            var input = grabbed.GetComponent<PlayerInputReader>();
            if (input != null)
            {
                input.enabled = on;
            }

            var look = grabbed.GetComponentInChildren<PlayerLook>();
            if (look != null)
            {
                look.enabled = on;
            }

            var motor = grabbed.GetComponent<PlayerMotor>();
            if (motor != null)
            {
                motor.enabled = on;
            }

            var controller = grabbed.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = on;
            }
        }

        /// <summary>
        /// 간 거리로 발소리를 낸다. <b>클립에 접지 이벤트가 없어서</b> 시간이 아니라
        /// 거리로 잡는다.
        /// </summary>
        /// <remarks>
        /// 플레이어 발소리와 같은 방식이다. 거리로 재면 배회(1.2)와 추격(3.4)의 간격이
        /// 속도에서 저절로 따라오고, 벽에 붙어 제자리걸음 할 때 소리가 나지 않는다.
        /// 시간으로 하면 속도를 고칠 때마다 간격도 같이 고쳐야 한다.
        /// </remarks>
        private void DriveFootsteps()
        {
            float speed = agent != null && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
            bool moving = speed > movingSpeed;

            if (moveLoop != null)
            {
                if (moving && !moveLoop.isPlaying)
                {
                    moveLoop.Play();
                }
                else if (!moving && moveLoop.isPlaying)
                {
                    moveLoop.Stop();
                }
            }

            if (stepSource == null || stepClips == null || stepClips.Length == 0)
            {
                return;
            }

            if (!moving)
            {
                // 멈추면 쌓아둔 거리를 버린다. 안 그러면 다시 걷는 첫 프레임에 터진다.
                lastStepPosition = transform.position;
                travelled = 0f;
                return;
            }

            Vector3 moved = transform.position - lastStepPosition;
            moved.y = 0f;
            lastStepPosition = transform.position;
            travelled += moved.magnitude;

            if (travelled < stepStride)
            {
                return;
            }

            travelled -= stepStride;
            stepSource.volume = IsChasing ? chaseStepVolume : roamStepVolume;
            stepSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            stepSource.PlayOneShot(PickStep());
        }

        /// <summary>직전 것을 빼고 고른다. 하나뿐이면 그것을 쓴다.</summary>
        private AudioClip PickStep()
        {
            if (stepClips.Length == 1 || lastStep < 0)
            {
                // 첫 발은 아무거나. 여기서 걸러내면 0 번이 영영 안 나온다.
                lastStep = UnityEngine.Random.Range(0, stepClips.Length);
                return stepClips[lastStep];
            }

            int i = UnityEngine.Random.Range(0, stepClips.Length - 1);
            if (i >= lastStep)
            {
                i++;
            }

            lastStep = i;
            return stepClips[i];
        }

        private void DriveAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedHash, agent.isOnNavMesh ? agent.velocity.magnitude : 0f);
            animator.SetBool(ChasingHash, IsChasing);
            animator.SetBool(BuriedHash, IsBuried);
        }

        private void OnDisable()
        {
            // 잡은 채로 꺼지면 플레이어가 손에 매달린 채 조작 불능으로 남는다.
            ReleasePlayer();

            // 루프는 스스로 안 멈춘다. Update 가 도는 동안만 관리되므로 여기서 끊는다.
            if (moveLoop != null && moveLoop.isPlaying)
            {
                moveLoop.Stop();
            }
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
                // 임계에 못 미쳐도 반응은 한다. 아무 반응이 없으면 맞았는지도 모른다.
                if (animator != null && !IsGrabbing)
                {
                    animator.SetTrigger(HitReactHash);
                }

                return;
            }

            damageSinceStagger = 0;
            if (animator != null)
            {
                animator.SetTrigger(StaggerHash);
            }

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
            if (animator != null)
            {
                animator.SetTrigger(RoarHash);
            }

            Noticed?.Invoke();
            noticed?.Invoke();
        }

        /// <summary>
        /// 쫓기를 그만두고 선다. 6장에서 삭도가 떠날 때 부른다.
        /// </summary>
        /// <remarks>
        /// <b>컴포넌트를 그냥 끄면 안 된다.</b> <c>Update</c> 가 멈추면서
        /// <c>Speed</c> 파라미터가 마지막 값에 굳어, 서 있는데 제자리걸음을 계속한다.
        /// 여기서 경로와 속도를 먼저 지우고 애니메이터를 아이들로 내린 다음에 끈다.
        /// <para>
        /// <see cref="Bury"/> 와 다르다. 저쪽은 눈에 묻혀 갇히는 것이라 전용 클립이
        /// 있고, 이쪽은 그냥 서서 지켜보는 것이다.
        /// </para>
        /// </remarks>
        public void StandDown()
        {
            IsChasing = false;
            roarUntil = float.NegativeInfinity;

            if (agent.isOnNavMesh)
            {
                if (agent.hasPath)
                {
                    agent.ResetPath();
                }

                agent.velocity = Vector3.zero;
            }

            if (animator != null)
            {
                animator.SetFloat(SpeedHash, 0f);
                animator.SetBool(ChasingHash, false);
            }

            enabled = false;
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
