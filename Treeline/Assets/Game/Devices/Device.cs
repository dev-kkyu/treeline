using System;
using UnityEngine;
using UnityEngine.Events;

namespace Treeline
{
    /// <summary>
    /// 장치의 공통 부분. <b>발동하면 무엇이 켜지고 무엇이 꺼지는가</b>를 담는다.
    /// </summary>
    /// <remarks>
    /// 기획의 장치는 아홉이지만 구현은 세 패턴으로 끝난다 —
    /// <see cref="InstantDevice"/> · <see cref="TimedDevice"/> · <see cref="SustainedDevice"/>.
    /// 셋의 차이는 <b>언제 <see cref="IsActive"/> 가 되고 되돌릴 수 있는가</b>뿐이고,
    /// 「무엇이 일어나는가」는 전부 이 클래스가 맡는다.
    /// <para>
    /// IMPORTANT: <b>새 장치를 만들 때 새 스크립트를 쓰지 않는다.</b> 셋 중 하나를 붙이고
    /// 인스펙터에서 값만 채운다. 세 패턴으로 안 되는 것이 나오면 그때 <b>왜 안 되는지를
    /// 먼저 확인한다.</b> 장치마다 클래스를 만들면 아홉 개가 되고, 그중 여덟은 서로
    /// 복사본이 된다.
    /// </para>
    /// </remarks>
    public abstract class Device : MonoBehaviour, IDeviceTrigger
    {
        [Header("발동하면")]
        [Tooltip("켜질 것들. 내려온 셔터, 열린 문, 불이 들어온 조명.")]
        [SerializeField] private GameObject[] enables;

        [Tooltip("꺼질 것들. 얼음 막, 닫힌 문, 막고 있던 벽.")]
        [SerializeField] private GameObject[] disables;

        [Header("소리 · 연출을 붙일 자리")]
        [Tooltip("발동했을 때. 시간 발동 장치는 다 끝난 뒤에 부른다.")]
        [SerializeField] private UnityEvent activated;

        [Tooltip("꺼졌을 때. 지속 상태 장치에서만 불린다.")]
        [SerializeField] private UnityEvent deactivated;

        /// <summary>
        /// 켜지고 꺼질 때마다 불린다. 코드에서 듣는 쪽은 이것을 쓴다.
        /// </summary>
        /// <remarks>
        /// 인스펙터의 <c>UnityEvent</c> 와 나란히 두는 이유는 <b>배선하는 비용이 다르기
        /// 때문</b>이다. 소리 하나 붙이는 것은 에디터에서 끌어다 놓는 편이 빠르고,
        /// 다른 시스템이 상태를 따라가야 하는 것은 코드로 거는 편이 안전하다.
        /// </remarks>
        public event Action<bool> ActiveChanged;

        /// <summary>지금 켜져 있는지. 시간이 걸리는 발동은 <b>다 끝난 뒤에</b> true 다.</summary>
        public abstract bool IsActive { get; }

        public abstract void Activate(GameObject source);

        public abstract void Deactivate(GameObject source);

        /// <summary>
        /// 씬에 저장된 상태와 관계없이 <b>발동 전 모습으로 맞춘다.</b>
        /// </summary>
        /// <remarks>
        /// 편집하다 켜둔 채로 저장하는 일이 반드시 생긴다. 그것을 씬 파일에 의존하면
        /// 「내 컴퓨터에서는 닫혀 있었다」가 된다. 여기서 한 번 바로잡으면 씬을 누가
        /// 어떻게 저장했든 시작 모습이 같다.
        /// </remarks>
        protected virtual void Awake()
        {
            ApplyState(IsActive, notify: false);
        }

        /// <summary>
        /// 켜지고 꺼지는 것을 실제로 반영한다. 세 패턴이 모두 이것만 부른다.
        /// </summary>
        /// <param name="notify">알릴지. <see cref="Awake"/> 의 초기 정렬은 알리지 않는다.</param>
        protected void ApplyState(bool active, bool notify)
        {
            SetAll(enables, active);
            SetAll(disables, !active);

            if (!notify)
            {
                return;
            }

            ActiveChanged?.Invoke(active);

            if (active)
            {
                activated?.Invoke();
            }
            else
            {
                deactivated?.Invoke();
            }
        }

        private static void SetAll(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
