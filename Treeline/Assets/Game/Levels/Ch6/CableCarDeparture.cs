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
    /// IMPORTANT: 기획의 <b>「화면이 뒤집힌다」— 그것의 시점으로 넘어가는 컷은 아직
    /// 넣지 않았다.</b> 그것이 케이블을 붙잡고 따라 올라오는 연출과 한 덩어리인데,
    /// 그 연출은 3 · 4 · 5장이 붙은 뒤에 들어간다. 지금은 <b>플레이어 시점 그대로</b>
    /// 승강장에 남은 그것이 멀어지는 것만 본다. 미룬 이유와 그때 쓸 값은
    /// <c>docs/tech-design/decisions.md</c> 에 있다. 씬의 <c>WatcherCamera</c> 는
    /// 그때 쓰려고 꺼둔 채 남겨두었다.
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
        [SerializeField] private float rideSeconds = 5f;

        [Header("남는 것")]
        [Tooltip("출발하면 멈춘다. 계속 쫓아오면 멀어지는 그림이 안 나온다.")]
        [SerializeField] private Colossus watcher;

        [Tooltip("그것이 마지막으로 서는 자리. 승강장 가장자리이며, 여기가 플레이어에게 보여야 한다.")]
        [SerializeField] private Transform watchStand;

        [Tooltip("차체가 멈춘 뒤 더 보는 시간. 끝나면 다음 장으로 간다.")]
        [SerializeField] private float watchSeconds = 3.5f;

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
            // 엔딩까지 화면에 붙어 있다. 손에 든 총은 그대로 둔다 — 시점이 여전히
            // 플레이어이고, 들고 탄 것이 보이는 편이 맞다.
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            // 「그것이 케이블을 붙잡고 따라 올라온다」— 붙잡는 것 자체는 3 · 4 · 5장이
            // 붙은 뒤에 넣는다. 지금은 가장자리까지 와서 멈추는 데까지다. 어디서 헤매다
            // 왔든 같은 그림이 나오도록 자리를 정해준다. 플레이어가 이것이 멀어지는 것을 본다.
            if (watcher != null)
            {
                if (watchStand != null)
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
                }

                // 컴포넌트만 끄면 Speed 파라미터가 마지막 값에 굳어 제자리걸음이 남는다.
                watcher.StandDown();
            }

            Vector3 from = car.position;
            Vector3 to = from + travel;

            float elapsed = 0f;

            while (elapsed < rideSeconds)
            {
                elapsed += Time.deltaTime;

                // 정지 마찰에서 서서히 풀리듯 시작한다. 플레이어는 승강장을 등지고 선
                // 것이 아니라 마주 본 채로 실려 가므로, 이 가속이 그대로 「멀어진다」가 된다.
                float k = Mathf.Clamp01(elapsed / rideSeconds);
                car.position = Vector3.Lerp(from, to, k * k);

                yield return null;
            }

            car.position = to;

            float watched = 0f;
            while (watched < watchSeconds)
            {
                watched += Time.deltaTime;
                yield return null;
            }

            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.NextChapter();
            }
        }
    }
}
