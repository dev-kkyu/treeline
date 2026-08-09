using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>3장의 두 목표를 권총 소유 상태에 맞춰 갱신한다.</summary>
    [DisallowMultipleComponent]
    public sealed class Ch3Objectives : MonoBehaviour
    {
        private const string SceneName = "Ch3";

        private Transform playerRoot;
        private PlayerWeapon pistol;
        private int lastObjective = -1;

        public static void AttachTo(Transform hudRoot, Transform player)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            Ch3Objectives tracker = hudRoot.GetComponent<Ch3Objectives>();
            if (tracker == null)
            {
                tracker = hudRoot.gameObject.AddComponent<Ch3Objectives>();
            }

            tracker.playerRoot = player;
        }

        private void Start()
        {
            if (playerRoot == null)
            {
                enabled = false;
                return;
            }

            PlayerWeapon[] weapons = playerRoot.GetComponentsInChildren<PlayerWeapon>(true);
            foreach (PlayerWeapon weapon in weapons)
            {
                if (weapon.Kind == WeaponKind.Pistol)
                {
                    pistol = weapon;
                    break;
                }
            }

            if (pistol == null)
            {
                Debug.LogWarning($"[{nameof(Ch3Objectives)}] 플레이어의 권총을 찾지 못했다.", this);
            }

            RefreshObjective(force: true);
        }

        private void Update()
        {
            RefreshObjective(force: false);
        }

        private void RefreshObjective(bool force)
        {
            int objective = pistol != null && pistol.IsOwned ? 1 : 0;
            if (!force && objective == lastObjective)
            {
                return;
            }

            lastObjective = objective;
            HudGuideText.ShowObjectiveStep(objective);
        }
    }
}
