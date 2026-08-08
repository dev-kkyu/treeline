using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 들고 온 것을 받는 자리. 받으면 장치가 발동한다. 6장 권양기의 연료 투입구가 이것이다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Carriable"/> 만으로는 「들고 다닌다」에서 끝난다. <b>어디에 가져다
    /// 놓아야 의미가 생기는지</b>를 정하는 것이 이쪽이고, 그래서 받는 물건과 발동시킬
    /// 장치를 둘 다 여기서 지정한다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>빈손이면 아예 안 잡힌다.</b> <see cref="CanInteract"/> 가 false 라
    /// 조준해도 문구가 안 뜬다. 「E 를 눌렀는데 아무 일도 안 일어났다」가 가장 나쁜
    /// 피드백이고, 무엇이 없어서 안 되는지는 물건을 들어 봐야 알게 되는 편이 낫다.
    /// 대신 <see cref="promptWhenEmpty"/> 를 채우면 그 문구로 알려 준다.
    /// </para>
    /// <para>
    /// 받은 물건은 기본적으로 <b>사라진다.</b> 연료는 부으면 없어지는 것이고, 남겨두면
    /// 플레이어가 그것을 다시 집어 다른 데로 옮길 수 있다. 남겨야 하는 물건이라면
    /// <see cref="consume"/> 를 끄고 <see cref="placeAt"/> 을 준다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CarrySlot : MonoBehaviour, IInteractable
    {
        [Tooltip("들고 있을 때 뜨는 문구.")]
        [SerializeField] private string prompt = "연료를 붓는다";

        [Tooltip("빈손일 때 뜨는 문구. 비우면 아예 잡히지 않는다.")]
        [SerializeField] private string promptWhenEmpty = "";

        [Tooltip("이것만 받는다. 비우면 아무 운반물이나 받는다.")]
        [SerializeField] private Carriable accepts;

        [Tooltip("받으면 발동시킬 장치들.")]
        [SerializeField] private Device[] devices;

        [Tooltip("받은 물건을 없앨지. 끄면 placeAt 자리에 남는다.")]
        [SerializeField] private bool consume = true;

        [Tooltip("consume 을 껐을 때 물건이 놓일 자리.")]
        [SerializeField] private Transform placeAt;

        [Header("소리")]
        [SerializeField] private AudioClip acceptClip;

        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;

        private Carriable held;

        /// <summary>이미 받았는지. 한 번 받으면 끝이다.</summary>
        public bool IsFilled { get; private set; }

        public string Prompt => IsFilled ? string.Empty : (Carried() != null ? prompt : promptWhenEmpty);

        public bool CanInteract
        {
            get
            {
                if (IsFilled)
                {
                    return false;
                }

                // 들고 있으면 언제나 받는다. 빈손일 때는 문구가 있을 때만 잡힌다 —
                // 없는데 잡히면 눌러도 아무 일이 없는 자리가 된다.
                return Carried() != null || !string.IsNullOrEmpty(promptWhenEmpty);
            }
        }

        public void Interact(GameObject interactor)
        {
            Carriable carried = Carried();
            if (IsFilled || carried == null)
            {
                return;
            }

            IsFilled = true;
            held = carried;
            carried.Drop();

            if (consume)
            {
                carried.gameObject.SetActive(false);
            }
            else if (placeAt != null)
            {
                carried.transform.SetPositionAndRotation(placeAt.position, placeAt.rotation);
            }

            if (acceptClip != null)
            {
                AudioSource.PlayClipAtPoint(acceptClip, transform.position, sfxVolume);
            }

            if (devices == null)
            {
                return;
            }

            foreach (Device device in devices)
            {
                if (device != null)
                {
                    device.Activate(interactor);
                }
            }
        }

        /// <summary>지금 이 자리가 받을 수 있는 운반물을 누가 들고 있나.</summary>
        private Carriable Carried()
        {
            if (accepts != null)
            {
                return accepts.IsCarried ? accepts : null;
            }

            // 지정하지 않았으면 들려 있는 아무거나 받는다. 6장은 연료 하나뿐이라
            // 여기까지 올 일이 없지만, 물건이 늘면 이쪽이 기본이 된다.
            foreach (Carriable candidate in FindObjectsByType<Carriable>(FindObjectsSortMode.None))
            {
                if (candidate.IsCarried)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
