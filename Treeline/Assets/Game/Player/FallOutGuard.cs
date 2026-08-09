using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 세상 밖으로 떨어진 플레이어를 죽여 장을 다시 시작하게 한다.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: 이것은 <b>레벨의 구멍을 메우는 것이 아니라 마지막 안전망</b>이다.
    /// 막아야 할 곳은 벽으로 막는다. 다만 여섯 장을 셋이 나눠 짓는 동안 어느 한
    /// 군데가 뚫려 있을 수 있고, 그때 <b>무한히 떨어지는 것은 죽는 것보다 나쁘다</b> —
    /// 죽으면 체크포인트로 돌아오지만 떨어지는 중에는 아무것도 할 수 없어 브라우저
    /// 탭을 닫는 수밖에 없다. 심사자가 그걸 만나면 거기서 끝이다.
    /// <para>
    /// 2 장 복도가 앞뒤로 뚫려 있어 시작 지점에서 뒤로 걸으면 그대로 떨어졌다.
    /// 그쪽은 마감벽을 세워 막았고, 이것은 남은 곳을 위한 것이다.
    /// </para>
    /// <para>
    /// 기준 높이는 <b>씬마다 다르지 않게</b> 넉넉히 아래로 잡는다. 레벨 바닥이
    /// 어디든 y = -30 아래로 내려갔다면 지형 위가 아니다. 6 장 승강장이 가장 낮은데
    /// 그것도 0 부근이다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class FallOutGuard : MonoBehaviour
    {
        [Tooltip("이 높이 아래로 내려가면 세상 밖으로 본다.")]
        [SerializeField] private float killBelowY = -30f;

        [Tooltip("얼마나 자주 보는가(초). 매 프레임 볼 일이 아니다.")]
        [SerializeField] private float checkInterval = 0.25f;

        private PlayerHealth health;
        private float nextCheck;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (Time.time < nextCheck)
            {
                return;
            }

            nextCheck = Time.time + Mathf.Max(0.05f, checkInterval);

            if (transform.position.y >= killBelowY || !health.IsAlive)
            {
                return;
            }

            Debug.LogWarning(
                $"[{nameof(FallOutGuard)}] 플레이어가 세상 밖으로 떨어졌다 " +
                $"({transform.position.ToString("F1")}). 막혀 있어야 할 곳이 뚫려 있다.",
                this);

            // 데미지가 아니라 확실히 죽인다. 체력이 몇 칸 남았든 돌아올 방법이 없다.
            health.TakeDamage(new DamageInfo(health.MaxHealth, transform.position, Vector3.down, gameObject));
        }
    }
}
