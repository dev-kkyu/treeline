using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 지나가면 다시 시작할 자리가 여기로 바뀐다.
    /// 트리거 콜라이더를 붙이고 레이어는 <see cref="GameLayers.Trigger"/> 로 둔다.
    /// </summary>
    /// <remarks>
    /// 규칙은 「붙잡히면 그 장의 시작 지점에서 다시 시작」이다. 체크포인트는 장이
    /// 길어질 때만 쓴다. 촘촘히 깔면 죽는 것이 아무렇지 않아지고, 그러면 무섭지 않다.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [Tooltip("비워두면 이 오브젝트의 위치와 방향을 쓴다.")]
        [SerializeField] private Transform respawnAt;

        private bool taken;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = GameLayers.Trigger;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (taken || !other.CompareTag("Player"))
            {
                return;
            }

            taken = true;

            Transform target = respawnAt != null ? respawnAt : transform;
            GameFlow.Instance.SetCheckpoint(target.position, target.rotation);
        }

        private void OnDrawGizmos()
        {
            Transform target = respawnAt != null ? respawnAt : transform;
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.9f);
            Gizmos.DrawWireSphere(target.position + Vector3.up, 0.35f);
        }
    }
}
