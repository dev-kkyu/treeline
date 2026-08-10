using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>
    /// 2장에서 도주에 필요한 조작을 다시 확인하고 히터 순서만 안내한다.
    /// 무기가 없는 장이므로 전투 입력은 노출하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ch2Tutorial : MonoBehaviour
    {
        private const string SceneName = "Ch2";
        private const float ObjectiveRepeatSeconds = 30f;

        private enum Stage
        {
            Flashlight,
            Sprint,
            Crouch,
            Interact,
            Complete,
            Objective
        }

        private Transform playerRoot;
        private PlayerInputReader input;
        private TimedDevice heater;
        private Stage stage;
        private float completeUntil;
        private float nextObjectiveReminder;
        private int lastObjective = -1;
        private int trackedObjective = -1;

        public static void AttachTo(Transform hudRoot, Transform player)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            Ch2Tutorial tutorial = hudRoot.GetComponent<Ch2Tutorial>();
            if (tutorial == null)
            {
                tutorial = hudRoot.gameObject.AddComponent<Ch2Tutorial>();
            }

            tutorial.playerRoot = player;
        }

        private void Start()
        {
            if (playerRoot == null)
            {
                enabled = false;
                return;
            }

            input = playerRoot.GetComponentInChildren<PlayerInputReader>(true);
            heater = FindDevice<TimedDevice>("Heater");

            if (input == null)
            {
                Debug.LogError($"{nameof(Ch2Tutorial)}: {nameof(PlayerInputReader)}를 찾지 못했다.", this);
                enabled = false;
                return;
            }

            stage = Stage.Flashlight;
            ShowStage();
        }

        private void Update()
        {
            TrackObjectiveState();

            switch (stage)
            {
                case Stage.Flashlight when input.FlashlightPressed:
                    Advance(Stage.Sprint);
                    break;
                case Stage.Sprint when input.SprintHeld && input.Move.sqrMagnitude > 0.16f:
                    Advance(Stage.Crouch);
                    break;
                case Stage.Crouch when input.CrouchHeld:
                    Advance(Stage.Interact);
                    break;
                case Stage.Interact when input.InteractPressed:
                    FinishControls();
                    break;
                case Stage.Complete when Time.unscaledTime >= completeUntil:
                    stage = Stage.Objective;
                    ShowObjective(force: true);
                    break;
                case Stage.Objective:
                    ShowObjective(force: false);
                    break;
            }
        }

        private void Advance(Stage next)
        {
            stage = next;
            ShowStage();
        }

        private void ShowStage()
        {
            switch (stage)
            {
                case Stage.Flashlight:
                    ShowControl("keycap-f", "F로 손전등을 켜 복도 앞을 확인하세요");
                    break;
                case Stage.Sprint:
                    ShowControl("keycap-shift", "이동하며 Shift를 눌러 도망칠 준비를 하세요");
                    break;
                case Stage.Crouch:
                    ShowControl("keycap-c", "C를 눌러 사물함 뒤에 몸을 낮추세요");
                    break;
                case Stage.Interact:
                    ShowControl("keycap-e", "E로 배전함·레버 같은 장치를 조작하세요");
                    break;
            }
        }

        private static void ShowControl(string resourceName, string message)
        {
            Sprite icon = Resources.Load<Sprite>($"Tutorial/{resourceName}");
            HudGuideText.ShowControl(icon, message);
        }

        private void FinishControls()
        {
            stage = Stage.Complete;
            completeUntil = Time.unscaledTime + 2.5f;
            HudGuideText.Show("도주 조작 확인 완료", 2.5f);
        }

        private void ShowObjective(bool force)
        {
            int objective = ResolveObjective();

            float now = Time.unscaledTime;
            if (!force && objective == lastObjective && now < nextObjectiveReminder)
            {
                return;
            }

            lastObjective = objective;
            nextObjectiveReminder = now + ObjectiveRepeatSeconds;
            HudGuideText.ShowObjectiveStep(objective);
        }

        private void TrackObjectiveState()
        {
            int objective = ResolveObjective();
            if (objective == trackedObjective)
            {
                return;
            }

            trackedObjective = objective;
            HudGuideText.TrackObjectiveStep(objective);
        }

        private int ResolveObjective()
        {
            if (heater == null)
            {
                return 0;
            }

            if (!heater.IsRunning && !heater.IsActive)
            {
                return 1;
            }

            if (heater.IsRunning)
            {
                return 2;
            }

            // 셔터 레버는 단계가 아니다. 셔터가 처음부터 내려와 있지 않아 길을 막지
            // 않으므로, 예열이 끝나면 레버를 건드리지 않아도 그대로 나가진다. 목표로
            // 세워 두면 하지 않아도 되는 일이 해야 하는 일처럼 읽힌다.
            return 3;
        }

        private static T FindDevice<T>(string namePart) where T : Component
        {
            T[] devices = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (T device in devices)
            {
                if (device.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return device;
                }
            }

            return null;
        }
    }
}
