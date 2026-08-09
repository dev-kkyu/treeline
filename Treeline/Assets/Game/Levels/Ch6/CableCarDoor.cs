using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 케이블카의 미닫이 문. 두 짝이 양옆으로 갈라진다.
    /// </summary>
    /// <remarks>
    /// 문짝은 원본 <c>CableCar_Mesh</c> 에서 <b>영역 컷으로 떼어낸 진짜 문</b>이다
    /// (<c>CableCar_DoorL/R.asset</c>). 임의의 판을 덧대면 곡면 차체에서 떠 보인다.
    /// 탑승 레버가 열고, 플레이어가 들어서면 <see cref="Ch6Boarding"/> 이 닫는다.
    /// 이동량은 차체 로컬 벡터다 — 옆으로 갈라지면서 살짝 바깥으로 빠지는,
    /// 곤돌라 미닫이의 그 움직임을 벡터 하나로 담는다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CableCarDoor : MonoBehaviour
    {
        [Tooltip("왼쪽 문짝.")]
        [SerializeField] private Transform left;

        [Tooltip("오른쪽 문짝.")]
        [SerializeField] private Transform right;

        [Tooltip("왼쪽 짝이 열릴 때 가는 로컬 오프셋. 폭만큼 옆으로, 살짝 바깥으로.")]
        [SerializeField] private Vector3 leftSlide = new Vector3(-0.42f, 0f, -0.04f);

        [Tooltip("오른쪽 짝의 로컬 오프셋.")]
        [SerializeField] private Vector3 rightSlide = new Vector3(0.42f, 0f, -0.04f);

        [Tooltip("열리고 닫히는 데 걸리는 시간.")]
        [SerializeField] private float seconds = 1.4f;

        [Header("소리")]
        [Tooltip("문 자리에서 낸다. 차체에 붙은 소스를 쓴다.")]
        [SerializeField] private AudioSource speaker;

        [SerializeField] private AudioClip openClip;

        [SerializeField] private AudioClip closeClip;

        private Vector3 leftClosed;
        private Vector3 rightClosed;
        private Coroutine moving;

        /// <summary>열려 있는지. 여닫는 중에는 목표 상태를 말한다.</summary>
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (left != null)
            {
                leftClosed = left.localPosition;
            }

            if (right != null)
            {
                rightClosed = right.localPosition;
            }
        }

        public void Open()
        {
            Move(open: true, snap: false);
        }

        public void Close()
        {
            Move(open: false, snap: false);
        }

        /// <summary>소리 없이 즉시 연다. 재시작 복원이 쓴다 — 이미 지나온 일은 다시 안 들린다.</summary>
        public void SnapOpen()
        {
            Move(open: true, snap: true);
        }

        private void Move(bool open, bool snap)
        {
            if (IsOpen == open && !snap)
            {
                return;
            }

            IsOpen = open;

            if (moving != null)
            {
                StopCoroutine(moving);
            }

            if (snap)
            {
                Apply(open ? 1f : 0f);
                return;
            }

            if (speaker != null)
            {
                AudioClip clip = open ? openClip : closeClip;
                if (clip != null)
                {
                    speaker.PlayOneShot(clip);
                }
            }

            moving = StartCoroutine(Slide(open));
        }

        private IEnumerator Slide(bool open)
        {
            float from = open ? 0f : 1f;
            float to = open ? 1f : 0f;

            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                Apply(Mathf.Lerp(from, to, k * k * (3f - 2f * k)));
                yield return null;
            }

            Apply(to);
            moving = null;
        }

        private void Apply(float opened)
        {
            if (left != null)
            {
                left.localPosition = leftClosed + leftSlide * opened;
            }

            if (right != null)
            {
                right.localPosition = rightClosed + rightSlide * opened;
            }
        }
    }
}
