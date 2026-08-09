using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 콜로서스 손아귀에 잡힌 주인공의 몸. <b>처형이 시작되는 순간부터 피가 난다.</b>
    /// </summary>
    /// <remarks>
    /// <c>Body_Grabbed</c> 는 <c>Colossus_Grab_Execute</c> 와 같은 길이(4.37 초 · 131 프레임)로
    /// 잡혀 있고 <c>GrabLock</c>(7f) · <c>GrabKill</c>(89f) 이 콜로서스 쪽 이벤트와 같은
    /// 프레임이다. 그래서 <b>둘을 같은 시각에서 시작시키면 그대로 맞는다</b> —
    /// <see cref="Colossus"/> 가 붙잡는 순간 이쪽을 켜면서 시각을 넘겨준다.
    /// <para>
    /// 피는 <b>켜지는 순간부터 끝까지</b> 계속 뿌린다. 무는 프레임에서만 터뜨리면
    /// 화면이 같은 프레임에 닫혀서 아무것도 안 보인다. 잡힌 순간부터 사방으로 튀어야
    /// 4 초짜리 처형이 그 4 초 내내 처형으로 보인다.
    /// </para>
    /// <para>
    /// IMPORTANT: 애니메이션 이벤트는 <b>애니메이터가 달린 오브젝트에서만</b> 풀린다.
    /// 그래서 이 컴포넌트는 몸 루트에 붙고, 클립이 부르는 다섯을 전부 정의한다. 비어
    /// 있는 것까지 두는 이유는 없으면 재생할 때마다 콘솔에 경고가 쌓이기 때문이다.
    /// </para>
    /// <para>
    /// 뿌리는 일은 <see cref="EnemyHitBloodVfx"/> 가 한다. 총에 맞은 적이 쓰는 것과 같은
    /// 풀이라 WebGL 에서 새로 만드는 것이 없다.
    /// </para>
    /// </remarks>
    public sealed class GrabbedBody : MonoBehaviour
    {
        [Header("계속 나는 피")]
        [Tooltip("한 번에 터지는 갈래 수.")]
        [SerializeField] private int burstsPerWave = 4;

        [Tooltip("터지는 간격(초). 풀이 한 바퀴 도는 시간이 알갱이 수명보다 길어야 한다.")]
        [SerializeField] private float waveInterval = 0.18f;

        [Header("한 번씩 크게")]
        [Tooltip("손을 오므리는 55f. 아직 화면이 살아 있는 마지막 큰 순간이다.")]
        [SerializeField] private int snatchBursts = 8;

        [Tooltip("무는 89f. 화면이 같은 프레임에 닫히므로 검게 넘어가는 결에 얹는다.")]
        [SerializeField] private int killBursts = 12;

        [Header("퍼지는 모양")]
        [Tooltip("피가 터지는 자리의 흩어짐. 손 크기에 맞춘다.")]
        [SerializeField] private float spread = 0.45f;

        [Tooltip("위로 솟구치는 정도. 0 이면 사방으로 고르게 퍼진다.")]
        [Range(0f, 3f)]
        [SerializeField] private float upward = 1f;

        [Tooltip("피가 나는 자리. 비우면 이 오브젝트다.")]
        [SerializeField] private Transform origin;

        private Coroutine bleeding;

        private void OnEnable()
        {
            // 클립의 GrabLock(7f) 을 기다리지 않는다. 이 오브젝트가 켜지는 시점이 곧
            // 붙잡힌 시점이고, 콜로서스가 7f 에서 켜기 때문에 그 이벤트는 이미 지나 있다.
            bleeding = StartCoroutine(Bleed());
        }

        private void OnDisable()
        {
            if (bleeding != null)
            {
                StopCoroutine(bleeding);
                bleeding = null;
            }
        }

        /// <summary>7f — 잡을지 놓칠지가 갈리는 지점. 판정은 콜로서스가 한다.</summary>
        public void GrabLock()
        {
        }

        /// <summary>39f — 발이 바닥에서 떨어진다.</summary>
        public void LiftClear()
        {
        }

        /// <summary>55f — 손을 오므린다.</summary>
        public void Snatch()
        {
            Spray(snatchBursts);
        }

        /// <summary>89f — 턱이 닫힌다.</summary>
        public void GrabKill()
        {
            Spray(killBursts);
        }

        /// <summary>119f — 손이 풀린다.</summary>
        public void Release()
        {
        }

        private IEnumerator Bleed()
        {
            var wait = new WaitForSeconds(Mathf.Max(0.02f, waveInterval));

            while (true)
            {
                Spray(burstsPerWave);
                yield return wait;
            }
        }

        private void Spray(int count)
        {
            Transform at = origin != null ? origin : transform;

            for (int i = 0; i < count; i++)
            {
                // 사방으로 튀되 위쪽이 우세하게. 아래로만 흘리면 「흘렀다」가 되고,
                // 위로 솟구쳐야 「짜였다」로 읽힌다. 월드 up 이 아니라 몸의 up 을 쓰는
                // 것은 손이 기울면 피도 같이 기울어야 하기 때문이다.
                Vector3 direction = (at.up * upward + Random.onUnitSphere).normalized;
                Vector3 point = at.position + Random.insideUnitSphere * spread;

                EnemyHitBloodVfx.Spawn(point, direction, -direction);
            }
        }
    }
}
