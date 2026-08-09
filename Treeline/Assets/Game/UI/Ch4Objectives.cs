using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>4장의 다섯 목표를 장치 상태에 맞춰 갱신한다.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="Ch3Objectives"/> 와 같은 자리이고 판정만 다르다. 3장은 권총 하나를
    /// 보면 되지만 4장은 절차가 다섯이라 <b>장치 넷의 상태를 조합</b>해야 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>순서대로 세지 않는다.</b> 환기 팬은 건너뛸 수 있는 갈림길이다 —
    /// 기획이 「팬을 먼저 돌린다 / 그냥 들어간다」를 고르게 해 두었으므로, 팬을
    /// 안 돌리고 축전지실 차단기를 올린 사람에게 「팬을 돌리세요」가 현재 목표로
    /// 남아 있으면 안 된다. 그래서 단계를 하나씩 올리지 않고 <b>지금 상태로 도달
    /// 가능한 가장 앞선 단계</b>를 매번 다시 계산한다.
    /// </para>
    /// <para>
    /// 장치는 이름으로 찾는다. 인스펙터로 물릴 수가 없어서다 — 이 부품은 씬에 놓인
    /// 것이 아니라 <see cref="PlayerHud"/> 가 실행 중에 붙인다. 이름이 바뀌면 경고를
    /// 남기고 그 단계만 판정에서 빠진다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Ch4Objectives : MonoBehaviour
    {
        private const string SceneName = "Ch4";

        /// <summary>발전동 바깥쪽 차단기.</summary>
        private const string BreakerOuterName = "BreakerDevice_A";

        /// <summary>축전지실 안쪽 차단기.</summary>
        private const string BreakerInnerName = "BreakerDevice_B";

        private const string FanName = "FanDevice";

        private const string GeneratorName = "GeneratorDevice";

        private Device breakerOuter;
        private Device breakerInner;
        private Device fan;
        private TimedDevice generator;

        private int lastObjective = -1;

        public static void AttachTo(Transform hudRoot)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (hudRoot.GetComponent<Ch4Objectives>() == null)
            {
                hudRoot.gameObject.AddComponent<Ch4Objectives>();
            }
        }

        private void Start()
        {
            foreach (Device device in FindObjectsByType<Device>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (device.name)
                {
                    case BreakerOuterName: breakerOuter = device; break;
                    case BreakerInnerName: breakerInner = device; break;
                    case FanName: fan = device; break;
                    case GeneratorName: generator = device as TimedDevice; break;
                }
            }

            Warn(breakerOuter, BreakerOuterName);
            Warn(breakerInner, BreakerInnerName);
            Warn(fan, FanName);
            Warn(generator, GeneratorName);

            RefreshObjective(force: true);
        }

        private void Update()
        {
            RefreshObjective(force: false);
        }

        private void RefreshObjective(bool force)
        {
            int objective = Evaluate();
            if (!force && objective == lastObjective)
            {
                return;
            }

            lastObjective = objective;
            HudGuideText.ShowObjectiveStep(objective);
        }

        /// <summary>지금 상태에서 「현재 해야 할 줄」. 목록 길이면 전부 끝난 것이다.</summary>
        private int Evaluate()
        {
            // 예열이 끝났다. 남은 것은 나가는 일뿐이라 목록을 전부 그어 둔다.
            if (generator != null && generator.IsActive)
            {
                return 5;
            }

            // 기동 레버를 당겼다. 이제 지키는 단계다.
            if (generator != null && generator.IsRunning)
            {
                return 4;
            }

            bool outer = breakerOuter != null && breakerOuter.IsActive;
            if (!outer)
            {
                return 0;
            }

            if (breakerInner != null && breakerInner.IsActive)
            {
                return 3;
            }

            // 팬을 돌렸지만 안쪽 차단기는 아직이다.
            if (fan != null && fan.IsActive)
            {
                return 2;
            }

            return 1;
        }

        private void Warn(object device, string wanted)
        {
            if (device == null)
            {
                Debug.LogWarning($"[{nameof(Ch4Objectives)}] '{wanted}' 를 못 찾았다. 그 단계는 넘어가지 않는다.", this);
            }
        }
    }
}
