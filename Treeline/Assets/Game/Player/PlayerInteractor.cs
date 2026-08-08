using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 바라보는 대상을 찾아 <see cref="IInteractable"/> 을 부른다.
    /// 대상 쪽은 인터페이스만 구현하면 되고 여기를 고칠 일이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("조준 기준. 보통 카메라다.")]
        [SerializeField] private Transform aim;

        [Tooltip("손이 닿는 거리(m).")]
        [SerializeField] private float range = 2.5f;

        /// <summary>지금 조준하고 있는 대상. 없으면 null 이다.</summary>
        public IInteractable Current { get; private set; }

        /// <summary>화면에 띄울 문구. 대상이 없거나 못 쓰는 상태면 빈 문자열이다.</summary>
        public string Prompt =>
            Current != null && Current.CanInteract ? Current.Prompt : string.Empty;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (input == null || aim == null)
            {
                Debug.LogError($"{nameof(PlayerInteractor)}: {nameof(input)} 또는 {nameof(aim)} 이 비어 있다.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Current = FindTarget();

            if (Current == null || !Current.CanInteract || !input.InteractPressed)
            {
                return;
            }

            Current.Interact(gameObject);
        }

        private void OnDisable()
        {
            Current = null;
        }

        private IInteractable FindTarget()
        {
            if (!Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, range,
                    GameLayers.Interaction, QueryTriggerInteraction.Collide))
            {
                return null;
            }

            // 문고리 콜라이더가 자식일 수 있어 부모까지 올라가며 찾는다.
            return hit.collider.GetComponentInParent<IInteractable>();
        }
    }
}
