using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 두 손으로 드는 물건. 들면 총을 못 쓴다. 연료통과 축전지가 이것이다.
    /// </summary>
    /// <remarks>
    /// 기획이 늘린 규칙은 이것 하나다 — 「무거운 것을 들면 총을 못 쓴다」. 3장에서
    /// 연료통을 들고 가다 <b>내려놓고 쏘고 다시 드는</b> 것이 그 규칙이 만드는 리듬이고,
    /// 6장 권양기에 연료를 넣는 절차가 같은 물건의 두 번째 용도다.
    /// <para>
    /// 총을 잠그는 쪽은 <see cref="WeaponSwitcher.SetCarrying"/> 이다. 들었다는 것을
    /// 여기서 노출하고 A 가 그것을 보게 하는 반대 방향도 검토했는데, <b>C 가 아는 것이
    /// <c>bool</c> 하나뿐이라</b> 그럴 이유가 없었다. 뒤집으면 오히려 A 가 아직 없는
    /// C 의 타입을 참조하게 된다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Carriable : MonoBehaviour
        , IInteractable
    {
        [Tooltip("화면에 띄울 문구.")]
        [SerializeField] private string prompt = "집어 든다";

        [Header("드는 자세")]
        [Tooltip("눈을 기준으로 어디에 들리나. 조준선을 가리지 않게 아래로 내려 둔다.")]
        [SerializeField] private Vector3 holdOffset = new Vector3(0.3f, -0.35f, 0.8f);

        [Tooltip("들었을 때의 기울기.")]
        [SerializeField] private Vector3 holdRotation = Vector3.zero;

        [Header("내려놓기")]
        [Tooltip("발 앞 몇 미터에 내려놓나.")]
        [SerializeField] private float dropDistance = 1.2f;

        [Header("소리")]
        [SerializeField] private AudioClip liftClip;
        [SerializeField] private AudioClip dropClip;
        [SerializeField] private float sfxVolume = 0.85f;

        private Transform hands;
        private WeaponSwitcher weapons;
        private PlayerInputReader input;
        private PlayerInteractor interactor;
        private BoxCollider body;
        private Transform originalParent;

        /// <summary>지금 들려 있는지.</summary>
        public bool IsCarried => hands != null;

        public string Prompt => prompt;

        public bool CanInteract => !IsCarried;

        private void Reset()
        {
            gameObject.layer = GameLayers.Carriable;
        }

        private void Awake()
        {
            body = GetComponent<BoxCollider>();
            originalParent = transform.parent;
        }

        public void Interact(GameObject who)
        {
            if (IsCarried)
            {
                return;
            }

            PickUp(who);

            if (liftClip != null)
            {
                AudioSource.PlayClipAtPoint(liftClip, transform.position, sfxVolume);
            }
        }

        /// <summary>
        /// 내려놓기는 조준한 대상이 없을 때만 걸린다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: <b>들고 있는 동안에도 다른 것과 상호작용할 수 있어야 한다.</b>
        /// 6장에서 연료통을 들고 권양기 앞에 서서 누르는 것이 그 자리다. 그래서 같은
        /// <c>E</c> 를 나눠 쓰되 <b>조준한 것이 있으면 그쪽이 먼저</b>고, 허공을 보고
        /// 눌렀을 때만 내려놓는다.
        /// <para>
        /// 들려 있는 동안에는 콜라이더를 꺼 두므로 자기 자신이 조준에 걸리는 일은 없다.
        /// </para>
        /// </remarks>
        private void Update()
        {
            if (!IsCarried || input == null)
            {
                return;
            }

            if (!input.InteractPressed)
            {
                return;
            }

            if (interactor != null && interactor.Current != null)
            {
                return;
            }

            Drop();
        }

        private void PickUp(GameObject who)
        {
            Transform eyes = FindEyes(who);
            if (eyes == null)
            {
                Debug.LogWarning($"[Carriable] '{name}' 을 들 카메라를 못 찾았다.", this);
                return;
            }

            hands = eyes;
            weapons = who.GetComponentInParent<WeaponSwitcher>();
            input = who.GetComponentInParent<PlayerInputReader>();
            interactor = who.GetComponentInParent<PlayerInteractor>();

            transform.SetParent(hands, worldPositionStays: false);
            transform.localPosition = holdOffset;
            transform.localRotation = Quaternion.Euler(holdRotation);

            // 들고 있는 동안은 부딪히지도, 조준에 걸리지도 않는다.
            body.enabled = false;

            if (weapons != null)
            {
                weapons.SetCarrying(true);
            }
        }

        /// <summary>
        /// 지금 있는 자리에 내려놓는다. 들고 있지 않으면 아무것도 하지 않는다.
        /// </summary>
        /// <remarks>
        /// 플레이어가 <c>E</c> 로 부르는 것 말고도, <b>장치가 운반물을 받을 때</b> 바깥에서
        /// 부른다. 6장 권양기에 연료를 넣는 절차가 그 자리다.
        /// </remarks>
        public void Drop()
        {
            if (!IsCarried)
            {
                return;
            }

            Vector3 where = DropPosition();

            if (dropClip != null)
            {
                AudioSource.PlayClipAtPoint(dropClip, where, sfxVolume);
            }

            transform.SetParent(originalParent, worldPositionStays: false);
            transform.SetPositionAndRotation(where, Quaternion.Euler(0f, hands.eulerAngles.y, 0f));
            body.enabled = true;

            if (weapons != null)
            {
                weapons.SetCarrying(false);
            }

            hands = null;
            weapons = null;
            input = null;
            interactor = null;
        }

        /// <summary>발 앞의 바닥을 찾아 그 위에 놓는다. 못 찾으면 눈높이 앞에 그냥 둔다.</summary>
        private Vector3 DropPosition()
        {
            Vector3 forward = hands.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;

            Vector3 from = hands.position + forward * dropDistance;
            float half = body.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;

            return Physics.Raycast(from, Vector3.down, out RaycastHit hit, 5f,
                       ~0, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * half
                : from;
        }

        /// <summary>
        /// 눈이 되는 카메라를 찾는다. <b>이름이 아니라 태그로 찾는다.</b>
        /// </summary>
        /// <remarks>
        /// 플레이어에는 카메라가 둘이다 — 세상을 그리는 것과 손·무기를 그리는 것.
        /// <c>GetComponentInChildren&lt;Camera&gt;</c> 로 먼저 걸리는 것을 쓰면 뷰모델
        /// 카메라를 집을 수 있다. 계층 구조는 A 의 것이라 이름에 기대지 않는다.
        /// </remarks>
        private static Transform FindEyes(GameObject who)
        {
            var cameras = who.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                if (camera.CompareTag("MainCamera"))
                {
                    return camera.transform;
                }
            }

            return cameras.Length > 0 ? cameras[0].transform : null;
        }
    }
}
