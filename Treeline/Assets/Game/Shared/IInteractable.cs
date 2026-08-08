using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 플레이어가 바라보고 상호작용할 수 있는 것. 문 · 레버 · 밸브 · 차단기 · 획득물.
    /// 이것을 구현하면 플레이어 쪽을 고치지 않아도 붙는다.
    /// </summary>
    /// <remarks>
    /// 만드는 쪽은 상호작용 담당(C), 호출하는 쪽은 플레이어(A) 다.
    /// 셋이 합의하지 않고 바꾸지 않는다.
    /// </remarks>
    public interface IInteractable
    {
        /// <summary>화면에 띄울 문구. "문 열기" 처럼 동사로 쓴다.</summary>
        string Prompt { get; }

        /// <summary>지금 상호작용할 수 있는지. 잠긴 문이나 이미 내린 레버는 false 다.</summary>
        bool CanInteract { get; }

        /// <param name="interactor">상호작용을 건 쪽. 지금은 플레이어뿐이다.</param>
        void Interact(GameObject interactor);
    }
}
