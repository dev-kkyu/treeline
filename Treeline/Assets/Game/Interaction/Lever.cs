using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 장치를 발동시키는 손잡이. <see cref="IInteractable"/> 의 첫 구현체다.
    /// </summary>
    /// <remarks>
    /// 6장의 삭도 기동이 이것이고, 2장 비상 셔터와 1장 서치라이트도 같은 컴포넌트다.
    /// <b>레버는 조작부일 뿐이고 무슨 일이 일어나는지는 장치가 정한다.</b> 그래서 여는
    /// 대상을 직접 참조하지 않고 <see cref="Device"/> 를 발동시킨다.
    /// <para>
    /// 1단계에는 여는 대상(<c>opens</c>)을 직접 들고 있었다. 장치 시스템이 없을 때 미리
    /// 추상화하면 상상으로 만들게 되기 때문인데, 이제 실물이 생겼으므로 갈아끼웠다.
    /// </para>
    /// </remarks>
    // Collider 가 아니라 BoxCollider 다. 추상 타입을 적으면 Unity 가 붙이지 못해
    // 빈 오브젝트에 AddComponent 하면 조용히 null 이 돌아온다.
    [RequireComponent(typeof(BoxCollider))]
    public sealed class Lever : MonoBehaviour
        , IInteractable
    {
        [Tooltip("화면에 띄울 문구. 기본 폰트에 한글 글리프가 없어 영문으로 쓴다.")]
        [SerializeField] private string prompt = "Pull lever";

        [Tooltip("켜져 있을 때의 문구. 비워두면 위와 같다. 스위치일 때만 쓴다.")]
        [SerializeField] private string promptWhenOn = "";

        [Tooltip("이 손잡이가 움직이는 장치들. 여럿이면 한꺼번에 발동한다.")]
        [SerializeField] private Device[] devices;

        [Tooltip("한 번만 당길 수 있는지. 끄면 켰다 껐다 하는 스위치가 된다.")]
        [SerializeField] private bool oneShot = true;

        [Header("당겼다는 표시")]
        [Tooltip("당기면 이 각도만큼 눕는다. 회색 박스에서 유일한 시각 신호다.")]
        [SerializeField] private Vector3 pulledRotation = new Vector3(-55f, 0f, 0f);

        [Tooltip("움직이는 데 걸리는 시간.")]
        [SerializeField] private float pullSeconds = 0.25f;

        [Tooltip("당긴 뒤로 바꿀 머티리얼. 비워두면 색은 그대로다.")]
        [SerializeField] private Material pulledMaterial;

        private Quaternion restRotation;
        private Material restMaterial;
        private Renderer body;

        /// <summary>지금 당겨져 있는지. 스위치면 켜져 있는지와 같다.</summary>
        public bool IsPulled { get; private set; }

        public string Prompt => IsPulled && !string.IsNullOrEmpty(promptWhenOn) ? promptWhenOn : prompt;

        public bool CanInteract => !(oneShot && IsPulled);

        private void Reset()
        {
            gameObject.layer = GameLayers.Interactable;
        }

        private void Awake()
        {
            restRotation = transform.localRotation;

            body = GetComponent<Renderer>();
            if (body != null)
            {
                restMaterial = body.sharedMaterial;
            }

            if (devices == null || devices.Length == 0)
            {
                // 배선이 빠진 레버는 눌러도 아무 일이 없고, 그 증상은 상호작용 쪽 문제로
                // 보인다. 눌러보기 전에 씬을 열 때 알아채는 편이 싸다.
                Debug.LogWarning($"[Lever] '{name}' 에 발동시킬 장치가 없다.", this);
            }
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract)
            {
                return;
            }

            bool turningOn = !IsPulled;
            IsPulled = turningOn;

            Drive(turningOn, interactor);
            ShowState(turningOn);
        }

        private void Drive(bool turningOn, GameObject interactor)
        {
            if (devices == null)
            {
                return;
            }

            foreach (Device device in devices)
            {
                if (device == null)
                {
                    continue;
                }

                if (turningOn)
                {
                    device.Activate(interactor);
                }
                else
                {
                    device.Deactivate(interactor);
                }
            }
        }

        /// <summary>
        /// 지금 상태를 화면에 보여준다.
        /// </summary>
        /// <remarks>
        /// 소리도 애니메이션 클립도 없는 단계라 <b>이 움직임이 유일한 피드백</b>이다.
        /// 없으면 눌러도 아무 일이 없는 것처럼 보인다. 실제로 그렇게 보고를 받았다.
        /// <para>
        /// IMPORTANT: <b>각도가 되돌아오는 것은 「다시 당길 수 있다」는 뜻이다.</b>
        /// <see cref="oneShot"/> 레버를 되돌리는 연출을 만들었다가 걷어낸 적이 있다.
        /// 올라와 있는데 안 눌리면 색이 아무리 달라도 플레이어는 계속 눌러본다. 그래서
        /// 되돌아오는 것은 <b>실제로 다시 누를 수 있는 스위치일 때뿐</b>이다.
        /// </para>
        /// </remarks>
        private void ShowState(bool on)
        {
            if (body != null)
            {
                Material next = on ? pulledMaterial : restMaterial;
                if (next != null)
                {
                    body.sharedMaterial = next;
                }
            }

            Quaternion target = on
                ? restRotation * Quaternion.Euler(pulledRotation)
                : restRotation;

            StopAllCoroutines();
            StartCoroutine(Swing(transform.localRotation, target, pullSeconds));
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
