using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>
    /// 1장에서 비전투 조작을 한 단계씩 익히고, 이후에는 다음 목적지만 짧게 알려준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ch1Tutorial : MonoBehaviour
    {
        private const string SceneName = "Ch1";
        private const string CompletedKey = "Treeline.Tutorial.Ch1.ControlsComplete";
        private const float ObjectiveRepeatSeconds = 30f;

        private enum Stage
        {
            Move,
            Look,
            Interact,
            FindFlashlight,
            Flashlight,
            Sprint,
            Crouch,
            Jump,
            Complete,
            Objective
        }

        private Transform playerRoot;
        private PlayerInputReader input;
        private Flashlight flashlight;
        private SustainedDevice searchlight;
        private Stage stage;
        private float completeUntil;
        private float nextObjectiveReminder;
        private string lastObjective;

        public static void AttachTo(Transform hudRoot, Transform player)
        {
            if (!string.Equals(SceneManager.GetActiveScene().name, SceneName, StringComparison.Ordinal))
            {
                return;
            }

            Ch1Tutorial tutorial = hudRoot.GetComponent<Ch1Tutorial>();
            if (tutorial == null)
            {
                tutorial = hudRoot.gameObject.AddComponent<Ch1Tutorial>();
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
            flashlight = playerRoot.GetComponentInChildren<Flashlight>(true);
            searchlight = FindSearchlight();

            if (input == null)
            {
                Debug.LogError($"{nameof(Ch1Tutorial)}: {nameof(PlayerInputReader)}를 찾지 못했다.", this);
                enabled = false;
                return;
            }

            if (PlayerPrefs.GetInt(CompletedKey, 0) != 0)
            {
                stage = Stage.Objective;
                ShowObjective(force: true);
                return;
            }

            stage = Stage.Move;
            ShowStage();
        }

        private void Update()
        {
            switch (stage)
            {
                case Stage.Move when input.Move.sqrMagnitude > 0.16f:
                    Advance(Stage.Look);
                    break;
                case Stage.Look when input.Look.sqrMagnitude > 4f:
                    Advance(Stage.Interact);
                    break;
                case Stage.Interact when input.InteractPressed:
                    Advance(Stage.FindFlashlight);
                    break;
                case Stage.FindFlashlight when flashlight != null && flashlight.IsOwned:
                    Advance(Stage.Flashlight);
                    break;
                case Stage.Flashlight when input.FlashlightPressed:
                    Advance(Stage.Sprint);
                    break;
                case Stage.Sprint when input.SprintHeld && input.Move.sqrMagnitude > 0.16f:
                    Advance(Stage.Crouch);
                    break;
                case Stage.Crouch when input.CrouchHeld:
                    Advance(Stage.Jump);
                    break;
                case Stage.Jump when input.JumpPressed:
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
                case Stage.Move:
                    ShowControl("keycap-wasd", "WASD로 관제실 안을 이동하세요");
                    break;
                case Stage.Look:
                    ShowControl("mouse-look", "마우스로 주변을 천천히 둘러보세요");
                    break;
                case Stage.Interact:
                    ShowControl("keycap-e", "E로 문·물건·기록과 상호작용하세요");
                    break;
                case Stage.FindFlashlight:
                    ShowControl("keycap-e", "관제실 바닥의 손전등을 찾아 주우세요");
                    break;
                case Stage.Flashlight:
                    ShowControl("keycap-f", "F로 손전등을 켜고 끌 수 있습니다");
                    break;
                case Stage.Sprint:
                    ShowControl("keycap-shift", "이동하며 Shift를 눌러 달리세요");
                    break;
                case Stage.Crouch:
                    ShowControl("keycap-c", "C를 눌러 몸을 낮추세요");
                    break;
                case Stage.Jump:
                    ShowControl("keycap-space", "Space를 눌러 장애물을 넘으세요");
                    break;
            }
        }

        private void ShowControl(string resourceName, string message)
        {
            Sprite icon = Resources.Load<Sprite>($"Tutorial/{resourceName}");
            HudGuideText.ShowControl(icon, message);
        }

        private void FinishControls()
        {
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
            stage = Stage.Complete;
            completeUntil = Time.unscaledTime + 2.5f;
            HudGuideText.Show("기본 조작 확인 완료", 2.5f);
        }

        private void ShowObjective(bool force)
        {
            string objective;
            if (flashlight != null && !flashlight.IsOwned)
            {
                objective = "관제실 바닥에서 손전등을 찾으세요";
            }
            else if (searchlight == null || !searchlight.IsActive)
            {
                objective = "계단을 따라 옥상으로 올라가 서치라이트를 켜세요";
            }
            else
            {
                objective = "서치라이트가 비춘 외부 통로로 이동하세요";
            }

            float now = Time.unscaledTime;
            if (!force && objective == lastObjective && now < nextObjectiveReminder)
            {
                return;
            }

            lastObjective = objective;
            nextObjectiveReminder = now + ObjectiveRepeatSeconds;
            HudGuideText.ShowObjective(objective);
        }

        private static SustainedDevice FindSearchlight()
        {
            SustainedDevice[] devices = FindObjectsByType<SustainedDevice>(FindObjectsSortMode.None);
            foreach (SustainedDevice device in devices)
            {
                if (device.name.IndexOf("Searchlight", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return device;
                }
            }

            return null;
        }
    }
}
