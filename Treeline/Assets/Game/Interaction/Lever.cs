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
        , IHandMotion
    {
        /// <summary>레버는 손을 뻗는 게 아니라 잡고 당긴다.</summary>
        public HandMotion Motion => HandMotion.Pull;

        [Tooltip("화면에 띄울 문구.")]
        [SerializeField] private string prompt = "레버를 당긴다";

        [Tooltip("켜져 있을 때의 문구. 비워두면 위와 같다. 스위치일 때만 쓴다.")]
        [SerializeField] private string promptWhenOn = "";

        [Tooltip("이 손잡이가 움직이는 장치들. 여럿이면 한꺼번에 발동한다.")]
        [SerializeField] private Device[] devices;

        [Tooltip("한 번만 당길 수 있는지. 끄면 켰다 껐다 하는 스위치가 된다.")]
        [SerializeField] private bool oneShot = true;

        [Header("선행 조건")]
        [Tooltip("이것들이 전부 켜져 있어야 당겨진다. 비우면 조건이 없다.")]
        [SerializeField] private Device[] requires;

        [Tooltip("조건이 안 찼을 때의 문구. 무엇이 막고 있는지를 적는다. 비우면 평소 문구가 뜬다.")]
        [SerializeField] private string promptWhenLocked = "";

        [Header("당겼다는 표시")]
        [Tooltip("실제로 움직일 부분. 손잡이만 넣는다. 비우면 이 오브젝트 전체가 움직인다.")]
        [SerializeField] private Transform handle;

        [Tooltip("당기면 이 각도만큼 눕는다. 회색 박스에서 유일한 시각 신호다.")]
        [SerializeField] private Vector3 pulledRotation = new Vector3(-55f, 0f, 0f);

        [Tooltip("움직이는 데 걸리는 시간.")]
        [SerializeField] private float pullSeconds = 0.25f;

        [Tooltip("당긴 뒤로 바꿀 머티리얼. 비워두면 색은 그대로다.")]
        [SerializeField] private Material pulledMaterial;

        [Header("소리")]
        [Tooltip("당길 때. 비워 두면 조용하다.")]
        [SerializeField] private AudioClip pullClip;

        [Tooltip("이미 당겨 더 못 쓸 때. 눌렀는데 아무 반응이 없으면 고장으로 오해한다.")]
        [SerializeField] private AudioClip lockedClip;

        [SerializeField] private float sfxVolume = 0.8f;

        private Quaternion restRotation;
        private Material restMaterial;
        private Renderer body;

        /// <summary>지금 당겨져 있는지. 스위치면 켜져 있는지와 같다.</summary>
        public bool IsPulled { get; private set; }

        public string Prompt
        {
            get
            {
                if (!Ready && !string.IsNullOrEmpty(promptWhenLocked))
                {
                    return promptWhenLocked;
                }

                return IsPulled && !string.IsNullOrEmpty(promptWhenOn) ? promptWhenOn : prompt;
            }
        }

        public bool CanInteract => !(oneShot && IsPulled);

        /// <summary>
        /// 선행 장치가 전부 켜져 있는지. 안 찼으면 당겨도 잠김 소리만 난다.
        /// </summary>
        /// <remarks>
        /// <see cref="CanInteract"/> 를 끄지 않는 이유가 있다. 끄면 조준해도 문구가 안 떠서
        /// 레버가 있는지도 모르게 된다. <b>잠긴 문구를 보여주고 눌러도 안 되는 것</b>이
        /// 6장의 절차를 가르치는 피드백이다.
        /// </remarks>
        private bool Ready
        {
            get
            {
                if (requires == null)
                {
                    return true;
                }

                foreach (Device device in requires)
                {
                    if (device != null && !device.IsActive)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Reset()
        {
            gameObject.layer = GameLayers.Interactable;
        }

        private void Awake()
        {
            // 손잡이를 지정하지 않았으면 예전처럼 통째로 움직인다. 회색 박스
            // 시절의 레버가 그대로 돌아가야 하기 때문이다.
            if (handle == null)
            {
                handle = transform;
            }

            restRotation = handle.localRotation;

            body = handle.GetComponent<Renderer>();
            if (body == null)
            {
                body = GetComponent<Renderer>();
            }
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
            if (!CanInteract || !Ready)
            {
                // 눌렀다는 것 자체는 알려준다. 무반응이면 조작이 안 먹은 줄 안다.
                Play(lockedClip);
                return;
            }

            Play(pullClip);

            bool turningOn = !IsPulled;
            IsPulled = turningOn;

            Drive(turningOn, interactor);
            ShowState(turningOn);
        }

        /// <summary>
        /// 재시작 뒤 모습을 <b>자기 장치 상태에 맞춘다.</b> 장치는 건드리지 않는다.
        /// </summary>
        /// <remarks>
        /// <see cref="ChapterState"/> 가 장치를 되돌린 뒤에 부른다.
        /// <para>
        /// IMPORTANT: <b>당겼는지를 따로 기억하지 않는 이유가 있다.</b> 시간 발동
        /// 장치는 도는 중에 죽으면 그 대기를 되살리지 않는데, 레버만 「당겨짐」으로
        /// 되돌리면 <see cref="CanInteract"/> 가 거짓이 되어 <b>다시 당길 수 없고 장이
        /// 막힌다.</b> 4장 발전기 예열에서 실제로 그렇게 갇혔다. 장치에서 끌어내면
        /// 둘이 어긋날 수가 없다.
        /// </para>
        /// </remarks>
        public void Restore()
        {
            bool on = devices != null && devices.Length > 0;

            if (on)
            {
                foreach (Device device in devices)
                {
                    if (device == null || !device.IsActive)
                    {
                        on = false;
                        break;
                    }
                }
            }

            IsPulled = on;

            if (body != null)
            {
                Material next = on ? pulledMaterial : restMaterial;
                if (next != null)
                {
                    body.sharedMaterial = next;
                }
            }

            // 되돌리는 것이라 흔들지 않고 그 자세로 놓는다. 코루틴을 쓰면 아직 꺼져
            // 있는 레버에서 「Coroutine couldn't be started」 가 난다 — 장치가 켜 주는
            // 레버가 그렇다.
            handle.localRotation = on ? restRotation * Quaternion.Euler(pulledRotation) : restRotation;
        }

        /// <summary>
        /// 손잡이 자리에서 한 번 낸다. 3D 라 방향과 거리가 들린다.
        /// </summary>
        /// <remarks>
        /// <see cref="AudioSource"/> 를 컴포넌트에 두지 않고 그 자리에서 재생한다.
        /// 레버는 눌리고 끝나는 물건이라 붙여 둘 이유가 없고, 프리팹마다
        /// <c>AudioSource</c> 를 챙기지 않아도 된다.
        /// </remarks>
        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, sfxVolume);
            }
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
            StartCoroutine(Swing(handle.localRotation, target, pullSeconds));
        }

        private IEnumerator Swing(Quaternion from, Quaternion to, float seconds)
        {
            if (seconds <= 0f)
            {
                handle.localRotation = to;
                yield break;
            }

            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                // 끝에서 부드럽게 멎도록 한 번 감쇠한다.
                float k = t / seconds;
                handle.localRotation = Quaternion.Slerp(from, to, k * k * (3f - 2f * k));
                yield return null;
            }

            handle.localRotation = to;
        }
    }
}
