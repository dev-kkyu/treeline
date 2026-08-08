using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 장의 끝. 플레이어가 들어오면 다음 장으로 넘어간다.
    /// 트리거 콜라이더를 붙이고 레이어는 <see cref="GameLayers.Trigger"/> 로 둔다.
    /// </summary>
    /// <remarks>
    /// 마지막 장의 출구는 엔딩으로 간다. 어디로 갈지는 <see cref="Chapters.Order"/> 가
    /// 정하므로 여기에 다음 장 이름을 적지 않는다. 적어두면 장을 끼워 넣을 때마다
    /// 씬을 열어 고쳐야 한다.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public sealed class ChapterExit : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.layer = GameLayers.Trigger;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (GameFlow.Instance.IsTransitioning)
            {
                return;
            }

            GameFlow.Instance.NextChapter();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.35f);
            var box = GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
