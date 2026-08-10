using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>4장의 다섯 목표를 산탄총 소유와 장치 상태에 맞춰 갱신한다.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="Ch3Objectives"/> 와 같은 자리이고 판정만 다르다. 3장은 권총 하나를
    /// 보면 되지만 4장은 절차가 다섯이라 <b>산탄총과 장치 셋의 상태를 조합</b>해야
    /// 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>환기 팬은 목표가 아니다.</b> 기획이 「팬을 먼저 돌린다 / 그냥
    /// 들어간다」를 고르게 해 두었으므로, 팬은 축전지실 차단기를 올리는 <b>두 가지
    /// 방법 중 하나</b>이지 거쳐야 하는 단계가 아니다. 목록에 한 줄로 얹으면 그 자체로
    /// 필수처럼 읽히므로 넣지 않는다. 팬을 돌릴 수 있다는 사실은 문 옆 레버의 조준
    /// 문구가 알린다.
    /// </para>
    /// <para>
    /// 그래도 <b>순서대로 세지는 않는다.</b> 산탄총은 지나칠 수 있어서, 단계를 하나씩
    /// 올리지 않고 <b>지금 상태로 도달 가능한 가장 앞선 단계</b>를 매번 다시 계산한다.
    /// </para>
    /// <para>
    /// 첫 줄만 장치가 아니라 <b>플레이어가 산탄총을 들었는지</b>로 판정한다. 씬의
    /// 획득물을 보면 안 된다 — 주우면 그 오브젝트는 꺼지므로 「없어진 것」과 「원래
    /// 없는 것」이 구분되지 않고, 산탄총은 3장에서 이리로 옮겨 온 물건이라
    /// (<c>docs/game-design/decisions.md</c>) 다음 장으로도 따라간다. 들고 있는 쪽이
    /// 사실이다.
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

        private const string GeneratorName = "GeneratorDevice";

        private Transform playerRoot;

        private PlayerWeapon shotgun;
        private Device breakerOuter;
        private Device breakerInner;
        private TimedDevice generator;

        private int lastObjective = -1;

        public static void AttachTo(Transform hudRoot, Transform player)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            Ch4Objectives tracker = hudRoot.GetComponent<Ch4Objectives>();
            if (tracker == null)
            {
                tracker = hudRoot.gameObject.AddComponent<Ch4Objectives>();
            }

            tracker.playerRoot = player;
        }

        private void Start()
        {
            if (playerRoot != null)
            {
                foreach (PlayerWeapon weapon in playerRoot.GetComponentsInChildren<PlayerWeapon>(true))
                {
                    if (weapon.Kind == WeaponKind.Shotgun)
                    {
                        shotgun = weapon;
                        break;
                    }
                }
            }

            foreach (Device device in FindObjectsByType<Device>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                switch (device.name)
                {
                    case BreakerOuterName: breakerOuter = device; break;
                    case BreakerInnerName: breakerInner = device; break;
                    case GeneratorName: generator = device as TimedDevice; break;
                }
            }

            Warn(shotgun, "플레이어의 " + WeaponKind.Shotgun);
            Warn(breakerOuter, BreakerOuterName);
            Warn(breakerInner, BreakerInnerName);
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
                // 산탄총은 걸어가는 선 위에 놓여 있지만 지나칠 수는 있다. 차단기를
                // 올린 사람에게 「산탄총을 주우세요」가 현재 목표로 남으면 안 되므로,
                // 이 줄은 아직 아무것도 안 한 동안에만 산다.
                return shotgun != null && shotgun.IsOwned ? 1 : 0;
            }

            if (breakerInner != null && breakerInner.IsActive)
            {
                return 3;
            }

            return 2;
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
