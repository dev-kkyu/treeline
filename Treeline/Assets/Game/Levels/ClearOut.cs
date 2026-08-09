using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 정해 둔 적을 <b>전부 죽여야</b> 장치가 열린다. 3장 격리 셔터가 이것이다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 「지나가려면 싸워야 한다」를 만드는 가장 싼 방법이다. 적을 피해 달려 지나가는
    /// 것을 막으려고 통로를 좁히거나 적을 늘리면 다른 것이 망가지는데, 문 하나를
    /// 잠가 두면 규칙이 하나로 끝난다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>죽었는지는 물어봐서 안다.</b> <see cref="Crawler.Died"/> 같은
    /// 이벤트를 구독하지 않고 <see cref="IDamageable.IsAlive"/> 를 주기적으로 읽는다.
    /// 이유는 두 가지다 — 적 종류마다 이벤트 이름이 다르면 여기가 종류를 알아야 하고,
    /// 이벤트는 <b>구독하기 전에 죽으면 놓친다</b>(장을 다시 시작하며 상태를 되돌릴 때
    /// 실제로 그렇게 된다). 0.25 초에 한 번 세 마리를 읽는 비용은 없는 것과 같다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ClearOut : MonoBehaviour
    {
        [Tooltip("이것들이 전부 죽어야 한다. 적 오브젝트를 그대로 넣는다.")]
        [SerializeField] private GameObject[] enemies = new GameObject[0];

        [Tooltip("다 죽으면 발동할 장치. 막고 있던 것을 치우는 쪽에 물린다.")]
        [SerializeField] private Device[] devices = new Device[0];

        [Header("알림")]
        [Tooltip("남은 수를 화면에 띄운다. {0} 자리에 숫자가 들어간다.")]
        [SerializeField] private string remainingMessage = "구역이 격리됐다 — 남은 것 {0}";

        [Tooltip("다 잡았을 때 띄울 문구.")]
        [SerializeField] private string clearedMessage = "격리가 풀렸다";

        [Tooltip("다 잡았다는 문구를 띄워 두는 시간(초).")]
        [SerializeField] private float clearedSeconds = 3f;

        [Header("언제 세는가")]
        [Tooltip("살았는지 확인하는 간격(초).")]
        [SerializeField] private float checkInterval = 0.25f;

        [Tooltip("플레이어가 이 거리 안에 들어와야 세기 시작한다. 0 이면 처음부터 센다.")]
        [SerializeField] private float startWithinMeters = 18f;

        private IDamageable[] targets;
        private Transform player;
        private float nextCheck;
        private int lastRemaining = -1;
        private bool armed;
        private bool done;

        /// <summary>아직 살아 있는 수.</summary>
        public int Remaining { get; private set; }

        /// <summary>다 잡았는가.</summary>
        public bool IsCleared => done;

        private void Awake()
        {
            targets = new IDamageable[enemies.Length];
            int found = 0;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null)
                {
                    continue;
                }

                targets[i] = enemies[i].GetComponentInChildren<IDamageable>(true);
                if (targets[i] != null)
                {
                    found++;
                }
                else
                {
                    Debug.LogWarning($"[{nameof(ClearOut)}] '{enemies[i].name}' 에 {nameof(IDamageable)} 이 없다.", this);
                }
            }

            if (found == 0)
            {
                Debug.LogError($"[{nameof(ClearOut)}] 셀 적이 하나도 없다. 장치가 영영 안 열린다.", this);
                enabled = false;
                return;
            }

            Remaining = found;

            // 죽었다가 되살아난 경우다. 체크포인트는 장치 상태를 되돌리므로 문은 이미
            // 열려 있는데 적은 새로 살아난다. 그때 「남은 것 3」을 다시 띄우면 이미
            // 지나갈 수 있는 문을 두고 싸우라고 말하는 셈이다.
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] != null && devices[i].IsActive)
                {
                    done = true;
                    return;
                }
            }

            PlayerMotor motor = FindFirstObjectByType<PlayerMotor>();
            if (motor != null)
            {
                player = motor.transform;
            }
        }

        private void Update()
        {
            if (done || Time.time < nextCheck)
            {
                return;
            }

            nextCheck = Time.time + Mathf.Max(0.05f, checkInterval);

            // 장을 열자마자 「남은 것 3」이 뜨면 통로 저편 이야기를 미리 하는 셈이다.
            // 가까이 와서 셔터를 보고 나서 알려 준다.
            if (!armed)
            {
                if (startWithinMeters > 0f
                    && player != null
                    && Vector3.Distance(player.position, transform.position) > startWithinMeters)
                {
                    return;
                }

                armed = true;
            }

            int alive = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && targets[i].IsAlive)
                {
                    alive++;
                }
            }

            Remaining = alive;

            if (alive > 0)
            {
                if (alive != lastRemaining)
                {
                    lastRemaining = alive;
                    HudGuideText.ShowObjective(string.Format(remainingMessage, alive));
                }

                return;
            }

            done = true;
            HudGuideText.ShowObjective(clearedMessage, clearedSeconds);

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i] != null)
                {
                    devices[i].Activate(gameObject);
                }
            }
        }
    }
}
