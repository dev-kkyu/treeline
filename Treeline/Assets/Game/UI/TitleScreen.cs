using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 타이틀 화면. 「시작」을 누르면 첫 장으로 간다.
    /// </summary>
    /// <remarks>
    /// 버튼 참조를 인스펙터로 끼우지 않고 자식에서 찾는다. 끼우는 것을 빠뜨리면
    /// 아무 일도 안 일어나는데, 화면은 멀쩡해 보여서 원인을 찾기 어렵다.
    /// </remarks>
    public sealed class TitleScreen : MonoBehaviour
    {
        private void Start()
        {
            // 이전 플레이에서 잠가둔 커서를 풀어준다. 안 그러면 버튼을 못 누른다.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Button start = GetComponentInChildren<Button>(true);
            if (start == null)
            {
                Debug.LogError("[TitleScreen] 시작 버튼을 못 찾았다.");
                return;
            }

            start.onClick.AddListener(StartGame);
        }

        private void StartGame()
        {
            GameFlow.Instance.StartNewGame();
        }
    }
}
