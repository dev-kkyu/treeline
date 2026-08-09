using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>5장의 네 목표를 장치 상태에 맞춰 갱신한다.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="Ch4Objectives"/> 와 같은 자리이고 판정만 다르다. 4장은 갈림길이
    /// 있어 「도달 가능한 가장 앞선 단계」를 매번 다시 계산했는데, 5장은 <b>눈과
    /// 사면이 물리적으로 막는 한 줄</b>이라 순서가 뒤집힐 수 없다. 그래서 뒤에서부터
    /// 확인해 내려온다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>세 번째 단계는 장치가 아니라 <see cref="SnowPlow.Arrived"/> 로
    /// 판정한다.</b> 제설기는 시동을 걸면 계속 켜져 있으므로 장치 상태만 보면
    /// 「시동을 거세요」에서 「따라가세요」로 넘어간 뒤 영영 안 바뀐다. 길이 뚫린
    /// 것은 제설기가 끝까지 갔다는 뜻이고, 그 사실을 아는 것은 제설기 자신뿐이다.
    /// </para>
    /// <para>
    /// 유도등은 <b>단계로 세지 않는다.</b> 세 개가 장 전체에 흩어져 있고 마지막
    /// 하나는 제설포보다 뒤에 있어서, 단계로 만들면 목록이 앞뒤로 튄다. 처음 두 개를
    /// 켜는 것까지만 첫 줄로 두고 나머지는 길잡이로 남긴다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Ch5Objectives : MonoBehaviour
    {
        private const string SceneName = "Ch5";

        private const string Beacon2Name = "BeaconDevice2";
        private const string PlowName = "PlowDevice";
        private const string CannonName = "CannonDevice";

        private Device beacon2;
        private Device plow;
        private Device cannon;
        private SnowPlow plowMachine;

        private int lastObjective = -1;

        public static void AttachTo(Transform hudRoot)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (hudRoot.GetComponent<Ch5Objectives>() == null)
            {
                hudRoot.gameObject.AddComponent<Ch5Objectives>();
            }
        }

        private void Start()
        {
            foreach (Device device in FindObjectsByType<Device>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (device.name)
                {
                    case Beacon2Name: beacon2 = device; break;
                    case PlowName: plow = device; break;
                    case CannonName: cannon = device; break;
                }
            }

            plowMachine = FindFirstObjectByType<SnowPlow>(FindObjectsInactive.Include);

            Warn(beacon2, Beacon2Name);
            Warn(plow, PlowName);
            Warn(cannon, CannonName);
            Warn(plowMachine, nameof(SnowPlow));

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

        /// <summary>지금 해야 할 줄. 목록 길이(4)면 전부 끝난 것이다.</summary>
        private int Evaluate()
        {
            if (cannon != null && cannon.IsActive)
            {
                return 4;
            }

            // 제설기가 끝까지 갔으면 길은 뚫렸다. 남은 것은 제설포다.
            if (plowMachine != null && plowMachine.Arrived)
            {
                return 3;
            }

            if (plow != null && plow.IsActive)
            {
                return 2;
            }

            if (beacon2 != null && beacon2.IsActive)
            {
                return 1;
            }

            return 0;
        }

        private void Warn(object found, string wanted)
        {
            if (found == null)
            {
                Debug.LogWarning($"[{nameof(Ch5Objectives)}] '{wanted}' 를 못 찾았다. 그 단계는 넘어가지 않는다.", this);
            }
        }
    }
}
