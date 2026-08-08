using System.Text;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 샌드박스에서 <b>HUD 가 안 보여주는 것</b>만 화면에 띄운다.
    /// 레벨 씬에는 넣지 않는다.
    /// </summary>
    /// <remarks>
    /// 체력 · 탄약 · 배터리 · 조준 문구는 A 의 <see cref="PlayerHud"/> 가 그린다.
    /// 여기서 같이 그리면 두 겹으로 겹쳐 보이므로 <b>겹치는 것은 두지 않는다.</b>
    /// <para>
    /// 남긴 둘은 정식 HUD 에 들어갈 것이 아니라 <b>시험할 때만 보고 싶은 값</b>이다.
    /// 양손 운반은 총이 잠겼는지를 눈으로 확인하려는 것이고, 시간 발동 장치의 진행률은
    /// 「기다리는 중」과 「고장나서 아무 일도 안 일어남」을 구별하려는 것이다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SandboxProbe : MonoBehaviour
    {
        private static readonly StringBuilder Text = new StringBuilder();

        private WeaponSwitcher weapons;
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

            weapons = player.GetComponent<WeaponSwitcher>();
        }

        private void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 18, richText = false };

            Text.Length = 0;

            if (weapons != null && weapons.IsCarrying)
            {
                Text.AppendLine("양손: 무언가 들고 있다 — 총이 잠긴다");
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

            if (Text.Length == 0)
            {
                return;
            }

            GUI.Label(new Rect(12f, 12f, 560f, 200f), Text.ToString(), style);
        }
    }
}
