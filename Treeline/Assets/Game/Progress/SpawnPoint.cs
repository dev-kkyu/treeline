using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 장이 시작될 때 플레이어가 서는 자리. 빈 오브젝트에 붙이고 방향까지 맞춘다.
    /// 씬에 하나만 둔다.
    /// </summary>
    /// <remarks>
    /// 없어도 게임은 돈다. 그때는 씬에 배치된 위치를 그대로 쓴다.
    /// </remarks>
    public sealed class SpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            // 회색 박스 단계에서는 이 기즈모가 유일한 표시다.
            Gizmos.color = new Color(0.4f, 0.9f, 0.5f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.8f);
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 1.2f);
        }
    }
}
