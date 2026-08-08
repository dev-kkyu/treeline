using System.Collections;
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

        [Header("당겼다는 표시")]
        [Tooltip("당기면 이 각도만큼 눕는다. 회색 박스에서 유일한 시각 신호다.")]
        [SerializeField] private Vector3 pulledRotation = new Vector3(-55f, 0f, 0f);

        [Tooltip("내려가는 데 걸리는 시간.")]
        [SerializeField] private float pullSeconds = 0.25f;

        [Tooltip("당긴 뒤로 바꿀 머티리얼. 비워두면 색은 그대로다.")]
        [SerializeField] private Material pulledMaterial;

        private Quaternion restRotation;

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
            restRotation = transform.localRotation;

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

            ShowPulled();
        }

        /// <summary>
        /// 당겼다는 것을 화면에 보여준다. 내려간 채로 남는다.
        /// </summary>
        /// <remarks>
        /// 소리도 애니메이션 클립도 없는 단계라 <b>이 움직임이 유일한 피드백</b>이다.
        /// 없으면 눌러도 아무 일이 없는 것처럼 보인다. 실제로 그렇게 보고를 받았다.
        /// <para>
        /// 실제 레버처럼 제자리로 되돌리는 것도 만들어 봤다가 걷어냈다. **올라와 있으면
        /// 또 당길 수 있어 보이는데 <see cref="oneShot"/> 이라 안 된다.** 보이는 것과
        /// 되는 것이 어긋나면 색이 아무리 달라도 플레이어는 눌러보게 된다. 내려간
        /// 채로 두면 각도와 색이 같은 말을 한다.
        /// </para>
        /// </remarks>
        private void ShowPulled()
        {
            if (pulledMaterial != null)
            {
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = pulledMaterial;
                }
            }

            StopAllCoroutines();
            StartCoroutine(Swing(restRotation, restRotation * Quaternion.Euler(pulledRotation), pullSeconds));
        }

        private IEnumerator Swing(Quaternion from, Quaternion to, float seconds)
        {
            if (seconds <= 0f)
            {
                transform.localRotation = to;
                yield break;
            }

            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                // 끝에서 부드럽게 멎도록 한 번 감쇠한다.
                float k = t / seconds;
                transform.localRotation = Quaternion.Slerp(from, to, k * k * (3f - 2f * k));
                yield return null;
            }

            transform.localRotation = to;
        }
    }
}
