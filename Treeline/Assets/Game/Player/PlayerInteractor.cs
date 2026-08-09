using System;
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

        /// <summary>
        /// 상호작용이 <b>실제로 일어났다.</b> 팔 동작과 소리가 여기에 붙는다.
        /// </summary>
        /// <remarks>
        /// 무기가 <c>Fired</c> 를 알리는 것과 같은 자리다 — 여기는 「무엇을 했다」만
        /// 알리고, 그것을 어떻게 보여줄지는 듣는 쪽이 정한다. 대상까지 넘기는 것은
        /// 레버와 획득물의 손동작이 다르기 때문이다(<see cref="IHandMotion"/>).
        /// </remarks>
        public event Action<IInteractable> Interacted;

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

            // 붙잡아 두고 부른다. 획득물은 Interact 안에서 스스로 꺼지므로
            // 그 뒤에 Current 를 다시 읽으면 이미 다른 것을 보고 있을 수 있다.
            IInteractable done = Current;
            done.Interact(gameObject);
            Interacted?.Invoke(done);
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
