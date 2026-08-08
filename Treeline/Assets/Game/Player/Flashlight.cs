using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 손전등. 무기가 아니고 배터리가 닳는다.
    /// </summary>
    /// <remarks>
    /// **1장에서 줍기 전에는 켜지지 않는다.** 상호작용 담당(C)이 <see cref="PickUp"/> 을
    /// 부르고, 배터리 보충은 <see cref="AddBattery"/> 로 부른다. 무기의 소지 판정과
    /// 같은 모양이라 셋을 한 규칙으로 다룬다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Flashlight : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("켜고 끌 조명. 보통 카메라 자식의 Spot Light 다.")]
        [SerializeField] private Light beam;

        [Tooltip("가득 찬 배터리로 켜 둘 수 있는 시간(초).")]
        [SerializeField] private float batteryCapacitySeconds = 240f;

        [Header("획득")]
        [Tooltip("시작할 때 이미 가지고 있는가. 기획상 1장에서 줍는다.")]
        [SerializeField] private bool startsOwned;

        private float batterySeconds;

        /// <summary>주웠는가. 안 주웠으면 켜지지 않는다.</summary>
        public bool IsOwned { get; private set; }

        public bool IsOn => beam != null && beam.enabled;

        /// <summary>0~1. HUD 와 배터리 경고에 쓴다.</summary>
        public float BatteryRatio =>
            batteryCapacitySeconds > 0f ? Mathf.Clamp01(batterySeconds / batteryCapacitySeconds) : 0f;

        /// <summary>켜짐 상태가 바뀔 때. 배터리가 다 닳아 꺼질 때도 부른다.</summary>
        public event Action<bool> Toggled;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (beam == null)
            {
                Debug.LogError($"{nameof(Flashlight)}: {nameof(beam)} 이 비어 있다.", this);
                enabled = false;
                return;
            }

            IsOwned = startsOwned;
            batterySeconds = batteryCapacitySeconds;
            beam.enabled = false;
        }

        private void Update()
        {
            // 안 주웠으면 아무 반응이 없다. 이미 켜져 있었다면 끈다.
            if (!IsOwned)
            {
                if (IsOn)
                {
                    SetOn(false);
                }

                return;
            }

            if (input != null && input.FlashlightPressed && (batterySeconds > 0f || IsOn))
            {
                SetOn(!IsOn);
            }

            if (!IsOn)
            {
                return;
            }

            batterySeconds -= Time.deltaTime;
            if (batterySeconds <= 0f)
            {
                batterySeconds = 0f;
                SetOn(false);
            }
        }

        /// <summary>
        /// 손전등을 주웠다. 이제 켤 수 있다. 상호작용 담당(C)이 1장에서 부른다.
        /// </summary>
        public void PickUp()
        {
            IsOwned = true;
        }

        /// <summary>배터리를 보충한다. 용량을 넘지 않는다.</summary>
        /// <remarks>손전등을 아직 안 주웠어도 배터리는 쌓인다.</remarks>
        public void AddBattery(float seconds)
        {
            batterySeconds = Mathf.Clamp(batterySeconds + seconds, 0f, batteryCapacitySeconds);
        }

        private void SetOn(bool on)
        {
            if (beam.enabled == on)
            {
                return;
            }

            beam.enabled = on;
            Toggled?.Invoke(on);
        }
    }
}
