using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 당기면 무언가가 열리는 레버. <see cref="IInteractable"/> 의 첫 구현체다.
    /// </summary>
    /// <remarks>
    /// 6장의 삭도 기동이 이것이다. 1단계에서는 레버 하나뿐이고, 가운데 장이 붙을
    /// 때마다 절차가 한 단씩 늘어난다.
    /// <para>
    /// 지금은 대상을 켜는 것으로 끝난다. 장치 시스템(<see cref="IDeviceTrigger"/>)이
    /// 들어오면 그쪽을 발동시키는 것으로 바꾼다. 그때까지 여는 대상을 직접 참조하는
    /// 이유는, 없는 시스템에 맞춰 미리 추상화하면 상상으로 만들게 되기 때문이다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public sealed class Lever : MonoBehaviour
        , IInteractable
    {
        [Tooltip("화면에 띄울 문구. 기본 폰트에 한글 글리프가 없어 영문으로 쓴다.")]
        [SerializeField] private string prompt = "Pull lever";

        [Tooltip("당기면 켜지는 것. 6장에서는 삭도 출구다.")]
        [SerializeField] private GameObject opens;

        [Tooltip("한 번만 당길 수 있는지.")]
        [SerializeField] private bool oneShot = true;

        /// <summary>이미 당겼는지.</summary>
        public bool IsPulled { get; private set; }

        public string Prompt => prompt;

        public bool CanInteract => !(oneShot && IsPulled);

        private void Reset()
        {
            gameObject.layer = GameLayers.Interactable;
        }

        private void Awake()
        {
            // 당기기 전에는 닫혀 있어야 한다. 씬에서 켜둔 채 저장해도 여기서 바로잡는다.
            if (opens != null && !IsPulled)
            {
                opens.SetActive(false);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            IsPulled = true;

            if (opens != null)
            {
                opens.SetActive(true);
            }
        }
    }
}
