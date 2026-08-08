using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 6장의 마지막 — 삭도가 출발하고 <b>화면이 뒤집힌다.</b>
    /// </summary>
    /// <remarks>
    /// 기획의 마지막 세 줄이 이것이다. 「삭도가 움직인다. 올라탄다」 다음에 시점이
    /// <b>쫓기던 쪽에서 쫓던 쪽으로</b> 넘어간다. 그것의 눈으로, 멀어지는 삭도를 본다.
    /// <para>
    /// 장을 끝내는 것은 <see cref="ChapterExit"/> 가 아니라 이 연출이다. 걸어 들어가면
    /// 끝나는 트리거를 남겨두면 삭도를 기동하지 않고도 장이 끝난다.
    /// </para>
    /// <para>
    /// IMPORTANT: 이 단계의 6장 절차는 <b>히터 → 출발</b> 둘뿐이다. 연료(3장) ·
    /// 배전반(4장) · 제설포(5장)는 그 장이 붙을 때 앞에 한 단씩 더한다. 지금 넣으면
    /// 가운데를 잘랐을 때 배운 적 없는 장치로 게임이 끝난다.
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

        [Header("그것의 시점")]
        [Tooltip("승강장에 남는 쪽의 카메라. 보통 Colossus 의 자식이다.")]
        [SerializeField] private Camera watcherCamera;

        [Tooltip("출발하고 이만큼 지나서 시점이 넘어간다.")]
        [SerializeField] private float cutAfterSeconds = 1.2f;

        [Tooltip("넘어간 화면이 제자리로 돌아오는 시간. 이 동안 뒤집혀 있다.")]
        [SerializeField] private float flipSeconds = 1f;

        [Tooltip("멀어지는 삭도를 보는 시간. 끝나면 다음 장으로 간다.")]
        [SerializeField] private float watchSeconds = 3.5f;

        [Tooltip("출발하면 멈춘다. 계속 쫓아오면 화면에 담을 수가 없다.")]
        [SerializeField] private Colossus watcher;

        [Tooltip("그것이 마지막으로 서는 자리. 승강장 가장자리이며, 여기서 삭도가 보여야 한다.")]
        [SerializeField] private Transform watchStand;

        private bool departed;

        private void Awake()
        {
            if (car == null)
            {
                car = transform;
            }

            if (watcherCamera != null)
            {
                watcherCamera.enabled = false;
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
            Camera playerCamera = null;
            AudioListener playerEar = null;

            if (player != null)
            {
                // 타고 가는 동안은 걷지 않는다. 입력을 한 곳에서 읽으므로 하나만 끄면 된다.
                var input = player.GetComponent<PlayerInputReader>();
                if (input != null)
                {
                    input.enabled = false;
                }

                playerCamera = player.GetComponentInChildren<Camera>();
                playerEar = player.GetComponentInChildren<AudioListener>();

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

            // 「그것이 케이블을 붙잡고 따라 올라온다」— 붙잡는 것 자체는 3단계에 제설포와
            // 같이 붙인다. 지금은 가장자리까지 와서 멈추는 데까지다. 어디서 헤매다 왔든
            // 같은 그림이 나오도록 자리를 정해준다. 카메라가 이 자식이라 같이 움직인다.
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

                watcher.enabled = false;
            }

            Vector3 from = car.position;
            Vector3 to = from + travel;

            float cutAt = Mathf.Min(cutAfterSeconds, rideSeconds);
            bool cut = false;
            float elapsed = 0f;

            while (elapsed < rideSeconds)
            {
                elapsed += Time.deltaTime;

                // 정지 마찰에서 서서히 풀리듯 시작한다.
                float k = Mathf.Clamp01(elapsed / rideSeconds);
                car.position = Vector3.Lerp(from, to, k * k);

                if (!cut && elapsed >= cutAt)
                {
                    cut = true;
                    SwitchToWatcher(playerCamera, playerEar);
                }

                AimWatcher(elapsed - cutAt);
                yield return null;
            }

            car.position = to;

            if (!cut)
            {
                SwitchToWatcher(playerCamera, playerEar);
                cut = true;
            }

            float watched = 0f;
            while (watched < watchSeconds)
            {
                watched += Time.deltaTime;
                AimWatcher(rideSeconds - cutAt + watched);
                yield return null;
            }

            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.NextChapter();
            }
        }

        /// <summary>쫓기던 쪽의 화면을 끄고 쫓던 쪽으로 넘긴다.</summary>
        private void SwitchToWatcher(Camera playerCamera, AudioListener playerEar)
        {
            if (watcherCamera == null)
            {
                return;
            }

            // 귀가 둘이면 Unity 가 경고를 내고 어느 쪽이 들리는지도 정해지지 않는다.
            var watcherEar = watcherCamera.GetComponent<AudioListener>();
            if (watcherEar != null && playerEar != null)
            {
                playerEar.enabled = false;
                watcherEar.enabled = true;
            }

            if (playerCamera != null)
            {
                // 뷰모델 오버레이까지 같이 꺼야 총이 화면에 남지 않는다.
                foreach (Camera cam in playerCamera.GetComponentsInChildren<Camera>(true))
                {
                    cam.enabled = false;
                }

                playerCamera.enabled = false;
            }

            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            watcherCamera.enabled = true;
        }

        /// <summary>
        /// 멀어지는 삭도를 계속 본다. 넘어온 직후에는 화면이 뒤집혀 있다가 제자리로 돈다.
        /// </summary>
        private void AimWatcher(float sinceCut)
        {
            if (watcherCamera == null || !watcherCamera.enabled)
            {
                return;
            }

            Transform eye = watcherCamera.transform;
            eye.rotation = Quaternion.LookRotation(car.position - eye.position, Vector3.up);

            // 「화면이 뒤집힌다」 — 180 도로 들어와서 바로 선다. 시점이 반대편으로
            // 넘어갔다는 것을 한 컷 안에 보여주려는 것이다.
            if (flipSeconds > 0f && sinceCut < flipSeconds)
            {
                float k = Mathf.Clamp01(sinceCut / flipSeconds);
                eye.rotation *= Quaternion.Euler(0f, 0f, Mathf.Lerp(180f, 0f, k * k * (3f - 2f * k)));
            }
        }
    }
}
