using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 6장의 마지막 — 삭도가 출발하고 <b>그것이 멀어진다.</b>
    /// </summary>
    /// <remarks>
    /// 장을 끝내는 것은 <see cref="ChapterExit"/> 가 아니라 이 연출이다. 걸어 들어가면
    /// 끝나는 트리거를 남겨두면 삭도를 기동하지 않고도 장이 끝난다.
    /// <para>
    /// IMPORTANT: 이 단계의 6장 절차는 <b>히터 → 출발</b> 둘뿐이다. 연료(3장) ·
    /// 배전반(4장) · 제설포(5장)는 그 장이 붙을 때 앞에 한 단씩 더한다. 지금 넣으면
    /// 가운데를 잘랐을 때 배운 적 없는 장치로 게임이 끝난다.
    /// </para>
    /// <para>
    /// 순서는 <b>시점 전환 → 붙잡기 → 놓치기 → 닫히기</b>다. 출발과 동시에 차체 위의
    /// 카메라로 넘어가 승강장을 돌아보고, <see cref="CableChase"/> 가 그것을 케이블에
    /// 매달아 따라오게 하고, 놓쳐서 떨어뜨리고, <see cref="Letterbox"/> 가 닫히면서
    /// 다음 장으로 간다. 시각은 전부 여기 한 줄기에 모아 두었다 — 코루틴을 여럿
    /// 띄우면 어느 것이 언제 도는지 이 파일만 봐서는 알 수 없게 된다.
    /// </para>
    /// <para>
    /// 기획의 「화면이 뒤집힌다」는 <b>상하가 아니라 시점의 주인</b>을 말한 것으로
    /// 읽었다. 근거는 <c>docs/tech-design/decisions.md</c>. 그것의 머리에 박혀 있던
    /// <c>WatcherCamera</c> 대신 차체에 카메라를 두었으므로 그쪽은 그대로 남는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CableCarDeparture : MonoBehaviour
    {
        [Tooltip("이것이 켜지면 출발한다. 보통 승강 레버가 미는 장치다.")]
        [SerializeField] private Device trigger;

        [Tooltip("실제로 움직일 차체. 플레이어를 여기에 붙인다.")]
        [SerializeField] private Transform car;

        [Tooltip("플레이어가 올라설 자리. 비우면 차체 원점이다.")]
        [SerializeField] private Transform boardPoint;

        [Tooltip("차체가 갈 거리와 방향(월드). 산 아래로 내려간다.")]
        [SerializeField] private Vector3 travel = new Vector3(0f, -8f, 46f);

        [Tooltip("삭도가 움직이는 시간.")]
        [SerializeField] private float rideSeconds = 15f;

        [Tooltip("가속의 세기. 1 이면 등속이고 클수록 처음이 느리다. 2(제곱)는 앞이 너무 느려 붙잡는 순간에도 승강장을 못 벗어난다.")]
        [SerializeField] private float rideEase = 1.4f;

        [Header("남는 것")]
        [Tooltip("출발하면 멈춘다. 계속 쫓아오면 멀어지는 그림이 안 나온다.")]
        [SerializeField] private Colossus watcher;

        [Tooltip("그것이 뛰어오르기 전에 서는 자리. 케이블 아래 갑판 끝이다.")]
        [SerializeField] private Transform watchStand;

        [Tooltip("차체가 멈춘 뒤 더 보는 시간. 끝나면 다음 장으로 간다.")]
        [SerializeField] private float watchSeconds = 3.5f;

        [Header("보는 자리")]
        [Tooltip("차체에 달린 카메라. 승강장을 등지고 실려 가는 대신 돌아본다.")]
        [SerializeField] private Camera chaseCamera;

        [Tooltip("출발 후 몇 초에 그 카메라로 넘어가나. 0 이면 출발과 동시다.")]
        [SerializeField] private float cameraAt;

        [Header("쫓아오는 것")]
        [Tooltip("케이블 추격 연출. 비우면 그것은 승강장에 선 채로 남는다.")]
        [SerializeField] private CableChase chase;

        [Tooltip("출발 후 몇 초에 케이블을 붙잡나. 차체가 충분히 멀어진 뒤라야 뒤에서 잡는다.")]
        [SerializeField] private float gripAt = 5f;

        [Tooltip("출발 후 몇 초에 놓치나.")]
        [SerializeField] private float letGoAt = 9f;

        [Header("레터박스")]
        [Tooltip("시점이 넘어가는 순간 잡는 띠. 여기서부터 연출이라는 것을 알린다.")]
        [SerializeField] private float cinematicBar = 0.08f;

        [Tooltip("마지막에 완전히 닫히는 데 걸리는 시간. 끝나면 다음 장으로 간다.")]
        [SerializeField] private float letterboxSeconds = 3f;

        private bool departed;

        private void Awake()
        {
            if (car == null)
            {
                car = transform;
            }

            if (trigger == null)
            {
                Debug.LogWarning($"[{nameof(CableCarDeparture)}] '{name}' 에 발동시킬 장치가 없다. 삭도가 영영 출발하지 않는다.", this);
            }
        }

        private void OnEnable()
        {
            if (trigger != null)
            {
                trigger.ActiveChanged += OnTriggerChanged;
            }
        }

        private void OnDisable()
        {
            if (trigger != null)
            {
                trigger.ActiveChanged -= OnTriggerChanged;
            }
        }

        private void OnTriggerChanged(bool active)
        {
            if (!active || departed)
            {
                return;
            }

            departed = true;
            StartCoroutine(Depart());
        }

        private IEnumerator Depart()
        {
            GameObject player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                // 타고 가는 동안은 걷지 않는다. 입력을 한 곳에서 읽으므로 하나만 끄면 된다.
                var input = player.GetComponent<PlayerInputReader>();
                if (input != null)
                {
                    input.enabled = false;
                }

                Transform stand = boardPoint != null ? boardPoint : car;
                var controller = player.GetComponent<CharacterController>();

                // CharacterController 는 켜져 있는 동안 위치를 되돌린다. 껐다 옮기고,
                // 끈 채로 타고 간다 — 차체에 붙어 움직이므로 스스로 걸을 필요가 없다.
                if (controller != null)
                {
                    controller.enabled = false;
                }

                // 입력만 끄면 PlayerMotor 는 계속 돌면서 꺼진 컨트롤러에 Move 를 건다.
                // 중력도 그대로라 차체에 붙은 채 아래로 밀린다.
                var motor = player.GetComponent<PlayerMotor>();
                if (motor != null)
                {
                    motor.enabled = false;
                }

                player.transform.SetPositionAndRotation(stand.position, stand.rotation);
                player.transform.SetParent(car, worldPositionStays: true);
            }

            // 여기서부터는 조작할 것이 없으므로 HUD 를 치운다. 남겨두면 체력 · 탄창이
            // 엔딩까지 화면에 붙어 있다. 손에 든 총은 따로 치우지 않는다 — 차체
            // 카메라가 `ViewModel` 을 컬링 마스크에서 빼므로 저절로 안 보인다.
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            // 「그것이 케이블을 붙잡고 따라 올라온다」— 여기가 그 자리다. 어디서 헤매다
            // 왔든 같은 그림이 나오도록 뛰어오를 자리를 정해준다.
            if (watcher != null && watchStand != null)
            {
                var agent = watcher.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.Warp(watchStand.position);
                }
                else
                {
                    watcher.transform.position = watchStand.position;
                }

                watcher.transform.rotation = watchStand.rotation;

                // 붙잡을 때까지는 서서 지켜본다. 컴포넌트만 끄면 Speed 파라미터가
                // 마지막 값에 굳어 제자리걸음이 남으므로 이쪽으로 내린다. 여기서
                // 안 세우면 그 몇 초 동안 제 갈 길을 가다가 붙잡는 순간 튄다.
                watcher.StandDown();
            }

            Vector3 from = car.position;
            Vector3 to = from + travel;

            Letterbox letterbox = null;
            float total = rideSeconds + watchSeconds;
            float closeFrom = Mathf.Max(0f, total - letterboxSeconds);
            bool cut = false;
            bool gripped = false;
            bool letGo = false;
            bool closing = false;
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                // 정지 마찰에서 서서히 풀리듯 시작한다. 이 가속이 그대로 「멀어진다」가 된다.
                float k = Mathf.Clamp01(elapsed / rideSeconds);
                car.position = Vector3.Lerp(from, to, Mathf.Pow(k, rideEase));

                if (!cut && elapsed >= cameraAt)
                {
                    // 기본값은 0 — 출발과 동시에 넘긴다. 얼마간 1인칭을 보여주다
                    // 넘기는 안을 만들어 봤는데, 플레이어가 차체 안에 서 있어서
                    // 그 몇 초가 통째로 객실 내벽이었다. 값은 남겨 둔다.
                    cut = true;
                    LookFromCar();
                    letterbox = Letterbox.Create();
                    StartCoroutine(letterbox.To(cinematicBar, 1.2f));
                }

                if (!gripped && elapsed >= gripAt)
                {
                    gripped = true;
                    if (chase != null)
                    {
                        chase.Grip();
                    }
                }

                if (!letGo && elapsed >= letGoAt)
                {
                    letGo = true;
                    if (chase != null)
                    {
                        chase.LetGo();
                    }
                }

                if (!closing && letterbox != null && elapsed >= closeFrom)
                {
                    closing = true;
                    StartCoroutine(letterbox.To(Letterbox.Closed, letterboxSeconds));
                }

                yield return null;
            }

            car.position = to;

            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.NextChapter();
            }
        }

        /// <summary>
        /// 플레이어 눈에서 차체 위의 카메라로 넘어간다.
        /// </summary>
        /// <remarks>
        /// 카메라 <b>컴포넌트만</b> 끈다. 오브젝트를 끄면 <see cref="AudioListener"/>
        /// 가 같이 죽어서 엔딩이 통째로 무음이 된다.
        /// </remarks>
        private void LookFromCar()
        {
            if (chaseCamera == null)
            {
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                foreach (Camera camera in player.GetComponentsInChildren<Camera>(true))
                {
                    camera.enabled = false;
                }
            }

            chaseCamera.gameObject.SetActive(true);
            chaseCamera.enabled = true;
        }
    }
}
