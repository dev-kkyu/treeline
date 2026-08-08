using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 손전등. 무기가 아니고 배터리가 닳는다.
    /// </summary>
    /// <remarks>
    /// 1장에서 줍는다는 기획은 획득 처리가 붙는 2단계에 맞춘다. 지금은 처음부터
    /// 들고 있고, 배터리 보충은 <see cref="AddBattery"/> 로 상호작용 담당(C)이 부른다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Flashlight : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("켜고 끌 조명. 보통 카메라 자식의 Spot Light 다.")]
        [SerializeField] private Light beam;

        [Tooltip("가득 찬 배터리로 켜 둘 수 있는 시간(초).")]
        [SerializeField] private float batteryCapacitySeconds = 240f;

        private float batterySeconds;

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

            batterySeconds = batteryCapacitySeconds;
            beam.enabled = false;
        }

        private void Update()
        {
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

        /// <summary>배터리를 보충한다. 용량을 넘지 않는다.</summary>
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
