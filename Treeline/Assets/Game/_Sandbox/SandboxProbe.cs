using System.Text;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 샌드박스에서 상태를 화면에 띄운다. <b>레벨 씬에는 넣지 않는다.</b>
    /// </summary>
    /// <remarks>
    /// HUD 가 아직 없어서 조준한 대상의 문구가 어디에도 안 그려진다. 그 상태로는 손으로
    /// 시험할 때 <b>"눌러도 아무 일이 없다" 와 "아무것도 조준하지 않았다" 를 구별할 수
    /// 없다.</b> 실제로 그것 때문에 한 번 헤맸다.
    /// <para>
    /// 정식 HUD 는 A 의 <c>Player.prefab</c> 안에 들어간다. 이것은 그때까지 쓰는
    /// 시험용이고 <c>IMGUI</c> 라 성능도 모양도 신경 쓰지 않는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SandboxProbe : MonoBehaviour
    {
        private static readonly StringBuilder Text = new StringBuilder();

        private PlayerInteractor interactor;
        private WeaponSwitcher weapons;
        private Flashlight flashlight;
        private GUIStyle style;

        private void Awake()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[SandboxProbe] 씬에 Player 가 없다.", this);
                enabled = false;
                return;
            }

            interactor = player.GetComponent<PlayerInteractor>();
            weapons = player.GetComponent<WeaponSwitcher>();
            flashlight = player.GetComponent<Flashlight>();
        }

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, richText = false };

            DrawPrompt();
            DrawState();
        }

        /// <summary>조준한 대상의 문구를 화면 가운데 아래에 띄운다.</summary>
        private void DrawPrompt()
        {
            string prompt = interactor != null ? interactor.Prompt : string.Empty;
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            var area = new Rect(0f, Screen.height * 0.62f, Screen.width, 30f);
            var centered = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(area, $"[E]  {prompt}", centered);
        }

        private void DrawState()
        {
            Text.Length = 0;

            if (weapons != null)
            {
                Text.AppendLine(weapons.IsCarrying
                    ? "양손: 무언가 들고 있다 (총 잠김)"
                    : "양손: 비었다");

                PlayerWeapon gun = weapons.Current;
                Text.AppendLine(gun != null
                    ? $"총: {gun.Kind}  탄창 {gun.Loaded} / 여분 {gun.Reserve}"
                    : "총: 맨손");
            }

            if (flashlight != null)
            {
                Text.AppendLine(flashlight.IsOwned
                    ? $"손전등: {(flashlight.IsOn ? "켜짐" : "꺼짐")}  배터리 {flashlight.BatteryRatio * 100f:0}%"
                    : "손전등: 아직 안 주웠다");
            }

            foreach (TimedDevice timed in FindObjectsByType<TimedDevice>(FindObjectsSortMode.None))
            {
                if (timed.IsRunning)
                {
                    Text.AppendLine($"{timed.name}: 도는 중 {timed.Progress * 100f:0}%  ({timed.SecondsLeft:0.0}초 남음)");
                }
                else if (timed.IsActive)
                {
                    Text.AppendLine($"{timed.name}: 끝났다");
                }
            }

            GUI.Label(new Rect(12f, 12f, 520f, 200f), Text.ToString(), style);
        }
    }
}
