using System;
using UnityEngine;
using UnityEngine.Events;

namespace Treeline
{
    /// <summary>
    /// 발동시켜 놓고 <b>기다려야</b> 켜지는 장치.
    /// 히터 케이블 · 환기 팬 · 발전기 예열이 이것이다.
    /// </summary>
    /// <remarks>
    /// 이 장치가 만드는 것은 효과가 아니라 <b>시간</b>이다. 2장은 히터가 도는 동안 숨어서
    /// 버티는 구간이고, 6장은 같은 물건으로 삭도 케이블을 녹이는 동안 쫓긴다. 그래서
    /// 「돌고 있다」를 밖에서 볼 수 있어야 한다 — <see cref="IsRunning"/> 과
    /// <see cref="Progress"/> 가 그 자리다.
    /// <para>
    /// 한 번 걸면 멈출 수 없다. 취소를 만들면 <b>기다린다는 것이 선택이 되어</b> 버티는
    /// 구간이 사라진다. 4장의 「환기 팬을 돌릴까 그냥 들어갈까」도 <b>걸기 전에</b>
    /// 하는 선택이다.
    /// </para>
    /// </remarks>
    public sealed class TimedDevice : Device
    {
        [Header("얼마나 걸리나")]
        [Tooltip("발동시키고 켜질 때까지의 초. 대기 시간은 움직이는 것을 보고 정한다.")]
        [SerializeField] private float seconds = 20f;

        [Tooltip("돌기 시작했을 때. 웅웅거리는 소리를 여기 붙인다.")]
        [SerializeField] private UnityEvent started;

        [Header("소리")]
        [Tooltip("걸었을 때 한 번. 레버를 당겼는데 아무 소리도 없으면 걸렸는지 알 수 없다.")]
        [SerializeField] private AudioClip startedClip;

        [Tooltip("도는 동안 계속. 이 소리가 없으면 기다리는 시간이 그냥 멈춘 시간이 된다.")]
        [SerializeField] private AudioSource runningLoop;

        private float elapsed;
        private bool isRunning;
        private bool isActive;

        /// <summary>다 돌아서 켜졌는지. 도는 <b>중</b>에는 아직 false 다.</summary>
        public override bool IsActive => isActive;

        /// <summary>지금 돌고 있는지. 소리와 표시를 이 값에 맞춘다.</summary>
        public bool IsRunning => isRunning;

        /// <summary>0 에서 1. 게이지를 붙일 때 쓴다.</summary>
        public float Progress => seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);

        /// <summary>남은 초. 숫자로 보여줄 때 쓴다.</summary>
        public float SecondsLeft => Mathf.Max(0f, seconds - elapsed);

        /// <summary>돌기 시작했을 때 불린다. 코드에서 듣는 쪽은 이것을 쓴다.</summary>
        public event Action Started;

        private void Reset()
        {
            gameObject.layer = GameLayers.Device;
        }

        public override void Activate(GameObject source)
        {
            if (isActive || isRunning)
            {
                return;
            }

            if (seconds <= 0f)
            {
                // 대기 시간을 0 으로 둔 것은 즉시 발동과 같다. 한 프레임도 끌지 않는다.
                Complete();
                return;
            }

            elapsed = 0f;
            isRunning = true;

            PlaySfx(startedClip);
            if (runningLoop != null)
            {
                runningLoop.loop = true;
                runningLoop.Play();
            }

            Started?.Invoke();
            started?.Invoke();
        }

        /// <summary>한 번 건 것은 멈출 수 없다. 아무것도 하지 않는다.</summary>
        public override void Deactivate(GameObject source)
        {
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            elapsed += Time.deltaTime;

            if (elapsed < seconds)
            {
                return;
            }

            isRunning = false;
            Complete();
        }

        private void Complete()
        {
            elapsed = seconds;
            isActive = true;

            // 다 녹은 뒤에도 히터는 계속 돈다. 끄지 않는 것이 맞다.
            // 꺼야 하는 장치라면 그 장에서 이벤트로 끈다.
            ApplyState(true, notify: true);
        }
    }
}
