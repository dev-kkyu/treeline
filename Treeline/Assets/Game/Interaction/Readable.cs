using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 읽을 수 있는 것. 무전 일지 · 근무표 · 낙서가 이것이다.
    /// </summary>
    /// <remarks>
    /// 기획이 읽을거리를 최소로 두기로 했다 — <b>이야기를 설명하지 않고, 사람이 여기
    /// 있었다는 흔적과 쓸모 있는 정보만 담는다.</b> 1장 근무표의 숫자가 3장 경비
    /// 캐비닛 번호인 것이 「쓸모 있는 정보」 쪽이다.
    /// <para>
    /// 읽는 동안 플레이어를 묶지 않는다. 움직임을 막으면 그 순간이 컷신이 되는데,
    /// 이 게임에서 조작을 빼앗는 자리는 6장 하나로 충분하다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Readable : MonoBehaviour
        , IInteractable
    {
        [Tooltip("화면에 띄울 문구.")]
        [SerializeField] private string prompt = "읽기";

        [Tooltip("펼쳤을 때의 제목.")]
        [SerializeField] private string title = "무전 일지";

        [Tooltip("본문. 줄바꿈을 그대로 쓴다.")]
        [TextArea(4, 12)]
        [SerializeField] private string body = "";

        [Header("소리")]
        [Tooltip("집어서 펼치는 소리. 비우면 소리가 나지 않는다.")]
        [SerializeField] private AudioClip readClip;

        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;

        public string Prompt => prompt;

        /// <summary>이미 읽는 중이면 다시 집지 않는다. 같은 키로 닫기 때문이다.</summary>
        public bool CanInteract => !ReadingPanel.IsOpen;

        private void Reset()
        {
            gameObject.layer = GameLayers.Interactable;
        }

        public void Interact(GameObject who)
        {
            if (!CanInteract)
            {
                return;
            }

            // 닫을 때는 소리를 내지 않는다. 닫기는 같은 키라 읽는 동안 한 번 더
            // 누르면 여기로 오지 않고, 「집는 소리」 는 펼치는 순간의 것이다.
            if (readClip != null)
            {
                AudioSource.PlayClipAtPoint(readClip, transform.position, sfxVolume);
            }

            ReadingPanel.Show(title, body);
        }
    }
}
