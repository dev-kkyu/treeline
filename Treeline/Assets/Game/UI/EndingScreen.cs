using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 엔딩 화면. 빠져나왔다는 것을 알리고 타이틀로 돌아간다.
    /// </summary>
    /// <remarks>
    /// 1단계에서는 글자와 버튼뿐이다. 기획의 「화면이 뒤집혀 그것의 시점이 된다」는
    /// 연출은 2단계에서 붙인다. 지금 필요한 것은 <b>끝났다는 것을 알리는 것</b>
    /// 하나다. 검은 화면만 두면 게임이 끝난 것인지 멈춘 것인지 알 수 없다.
    /// </remarks>
    public sealed class EndingScreen : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Button back = GetComponentInChildren<Button>(true);
            if (back == null)
            {
                Debug.LogError("[EndingScreen] 돌아가기 버튼을 못 찾았다.");
                return;
            }

            back.onClick.AddListener(BackToTitle);
        }

        private void BackToTitle()
        {
            GameFlow.Instance.ReturnToTitle();
        }
    }
}
