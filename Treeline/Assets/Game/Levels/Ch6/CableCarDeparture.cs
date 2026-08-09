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
    /// 순서는 <b>시점 전환 → 붙잡기 → 들이받기 → 닫히기</b>다. 출발과 동시에 차체
    /// 위의 카메라로 넘어가 승강장을 돌아보고, <see cref="CableChase"/> 가 그것을
    /// 케이블에 매달아 따라붙게 하고, 차체를 들이받아 흔들어 놓고 떨어지고,
    /// <see cref="Letterbox"/> 가 닫히면서 다음 장으로 간다. 시각은 전부 여기 한
    /// 줄기에 모아 두었다 — 코루틴을 여럿 띄우면 어느 것이 언제 도는지 이 파일만
    /// 봐서는 알 수 없게 된다.
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
        [Tooltip("이것이 켜지면 출발한다. 비우면 Depart() 를 밖에서 불러야 한다 — 탑승 연출(Ch6Boarding)이 그쪽이다.")]
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

        [Tooltip("출발 후 몇 초에 차체를 들이받나. 여기서 떨어진다.")]
        [SerializeField] private float hitAt = 11.5f;

        [Header("다가올 때 — 줄을 타고 오는 충격")]
        [Tooltip("한 번 당길 때마다 오는 충격이 잦아드는 시간.")]
        [SerializeField] private float tugSeconds = 0.5f;

        [Tooltip("가장 가까울 때의 충격 세기. 부딪히는 것에 대한 비율이다.")]
        [Range(0f, 1f)]
        [SerializeField] private float tugPower = 0.3f;

        [Tooltip("당김과 별개로 계속 떠는 정도. 가까울수록 세다.")]
        [SerializeField] private float tremble = 0.05f;

        [Header("들이받힌 차체")]
        [Tooltip("흔들리는 시간.")]
        [SerializeField] private float shakeSeconds = 2.6f;

        [Tooltip("진자가 도는 빠르기(rad/s). 삭도는 길게 매달린 것이라 느리게 돈다.")]
        [SerializeField] private float shakeRate = 7f;

        [Tooltip("좌우로 기우는 각도.")]
        [SerializeField] private float shakeRoll = 9f;

        [Tooltip("앞뒤로 끄덕이는 각도.")]
        [SerializeField] private float shakePitch = 4f;

        [Tooltip("맞는 순간의 잔떨림. 금방 잦아든다.")]
        [SerializeField] private float shakeJolt = 0.22f;

        [Header("레터박스")]
        [Tooltip("시점이 넘어가는 순간 잡는 띠. 여기서부터 연출이라는 것을 알린다.")]
        [SerializeField] private float cinematicBar = 0.08f;

        [Tooltip("마지막에 완전히 닫히는 데 걸리는 시간. 끝나면 다음 장으로 간다.")]
        [SerializeField] private float letterboxSeconds = 3f;

        [Header("눈보라를 벗어난다")]
        [Tooltip("내려가면서 시야가 여기까지 트인다(m). 0 이면 안개를 안 만진다. 뒤에 남는 승강장이 보여야 「멀어진다」가 된다.")]
        [SerializeField] private float clearVisibility = 90f;

        private bool departed;
        private float shakeLeft;
        private float shakeFor = 1f;
        private float shakePower;
        private float shakeSide = 1f;

        private void Awake()
        {
            if (car == null)
            {
                car = transform;
            }
        }

        /// <summary>출발시킨다. 탑승 연출이 문을 닫은 뒤에 부른다.</summary>
        public void Depart()
        {
            OnTriggerChanged(true);
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
            StartCoroutine(Ride());
        }

        private IEnumerator Ride()
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
            Quaternion hang = car.rotation;

            // 눈보라 밀도는 장이 정하므로 여기서 읽어 둔다. 목표 밀도로는 5장식
            // 환산(√ln10 / 거리)을 쓴다 — Whiteout 과 같은 자로 재야 이어진다.
            float fogFrom = RenderSettings.fogDensity;
            float fogTo = clearVisibility > 0f ? 1.517f / clearVisibility : fogFrom;

            Letterbox letterbox = null;
            float total = rideSeconds + watchSeconds;
            float closeFrom = Mathf.Max(0f, total - letterboxSeconds);
            bool cut = false;
            bool gripped = false;
            bool struck = false;
            bool closing = false;
            float elapsed = 0f;

            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                // 정지 마찰에서 서서히 풀리듯 시작한다. 이 가속이 그대로 「멀어진다」가 된다.
                float k = Mathf.Clamp01(elapsed / rideSeconds);
                Vector3 at = Vector3.Lerp(from, to, Mathf.Pow(k, rideEase));

                // 내려가면서 눈보라를 벗어난다. 시야가 트이며 승강장과 중계소가
                // 드러나는 것이 「멀어진다」를 만드는 절반이다.
                if (clearVisibility > 0f && RenderSettings.fog)
                {
                    RenderSettings.fogDensity = Mathf.Lerp(fogFrom, fogTo, Mathf.SmoothStep(0f, 1f, k));
                }

                // 다가오는 동안은 당길 때마다 한 번씩, 가까울수록 세게. 그 사이는
                // 잔떨림으로 메운다 — 당김 사이가 조용하면 매번 처음부터 놀란다.
                if (!struck && chase != null)
                {
                    if (chase.TakePull())
                    {
                        // 바닥을 깔아 둔다. 비례만 주면 붙잡은 직후 몇 초가 아무
                        // 일도 없는 화면이 되어, 저것이 줄에 올라탔다는 것이 안 온다.
                        Jolt(tugSeconds, tugPower * (0.25f + 0.75f * chase.Closeness));
                    }

                    float near = chase.Closeness * chase.Closeness;
                    at += Random.insideUnitSphere * (tremble * near);
                }

                at = Sway(at, hang);
                car.position = at;

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
                        chase.Grip(hitAt - gripAt);
                    }
                }

                if (!struck && elapsed >= hitAt)
                {
                    struck = true;
                    Jolt(shakeSeconds, 1f);
                    if (chase != null)
                    {
                        chase.Hit();
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

        /// <summary>충격을 하나 시작한다. 세기는 들이받히는 것을 1 로 본 비율이다.</summary>
        private void Jolt(float seconds, float power)
        {
            // 약한 것이 센 것을 덮어쓰지 않는다. 당기는 충격이 계속 오는 중에
            // 들이받히므로, 이 판단이 없으면 다음 당김이 충돌을 지운다.
            if (shakeLeft > 0f && power < shakePower * (shakeLeft / shakeFor))
            {
                return;
            }

            shakeFor = Mathf.Max(0.01f, seconds);
            shakeLeft = shakeFor;
            shakePower = power;
            shakeSide = -shakeSide;
        }

        /// <summary>
        /// 매달린 차체가 흔들린다. <b>카메라도 같이 흔들린다</b> — 차체의 자식이라
        /// 따로 흔들 것이 없다.
        /// </summary>
        /// <remarks>
        /// 난수만 뿌리면 「고장난 화면」이지 「얻어맞은 것」이 아니다. 줄 하나에
        /// 매달린 것이므로 몸통은 <b>감쇠하는 진자</b>로 돌리고, 금속이 부딪힌
        /// 잔떨림만 앞쪽 짧은 구간에 난수로 얹는다(<c>f³</c> 이라 훨씬 빨리 잦아든다).
        /// <para>
        /// 들이받히는 것과 <b>당길 때마다 줄을 타고 오는 충격이 같은 모양</b>이다.
        /// 세기와 길이만 다르다 — 둘 다 한 줄에 매달린 것이 한 번 얻어맞는 일이라,
        /// 따로 만들면 같은 것을 두 벌 갖게 된다.
        /// </para>
        /// </remarks>
        private Vector3 Sway(Vector3 at, Quaternion hang)
        {
            if (shakeLeft <= 0f)
            {
                car.rotation = hang;
                return at;
            }

            shakeLeft -= Time.deltaTime;

            float left = Mathf.Clamp01(shakeLeft / shakeFor);
            float swing = Mathf.Sin(left * shakeRate) * left * left * shakePower * shakeSide;

            car.rotation = hang * Quaternion.Euler(swing * shakePitch, 0f, swing * shakeRoll);

            float jolt = left * left * left * shakePower;
            return at
                + Vector3.up * (swing * 0.3f)
                + Random.insideUnitSphere * (shakeJolt * jolt);
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
