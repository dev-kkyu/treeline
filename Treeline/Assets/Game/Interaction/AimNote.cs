using UnityEngine;

namespace Treeline
{
    /// <summary>이 상호작용물을 조준하고 있는 동안 화면에 설명을 띄운다.</summary>
    /// <remarks>
    /// <para>
    /// 조준 문구(<see cref="Lever.Prompt"/>)는 <b>무엇인지</b>만 짧게 말한다. 거기에
    /// 설명까지 붙이면 <c>E</c> 키캡 옆 한 줄이 길어져 읽히지 않는다. <b>무엇을 할 수
    /// 있는지</b>는 이쪽이 맡는다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>시간으로 지우지 않는다.</b> 읽는 속도는 사람마다 다르고 문구 길이도
    /// 바뀌므로, 초를 박아두면 누군가는 마지막 줄을 놓친다. 조준하는 동안 떠 있고
    /// 조준을 거두면 사라진다 — 얼마나 볼지는 보는 쪽이 정한다.
    /// </para>
    /// <para>
    /// 4장 환기 팬 레버가 이걸 쓴다. 팬을 돌리는 것은 목표가 아니라 갈림길이라
    /// (<see cref="Ch4Objectives"/>) 목표 목록에 없고, 그래서 <b>팬을 돌릴 수 있다는
    /// 것과 수소에 쏘면 안 된다는 것을 알리는 자리가 여기뿐</b>이다.
    /// </para>
    /// <para>
    /// <see cref="PlayerInteractor"/> 에는 조준이 바뀌었다는 이벤트가 없어서 매 프레임
    /// <see cref="PlayerInteractor.Current"/> 를 본다. 다만 <see cref="HudGuideText.Show"/> 는
    /// 들어오고 나가는 순간에만 부른다. 매 프레임 부르면 패널이 계속 다시 열린다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AimNote : MonoBehaviour
    {
        [Tooltip("조준하는 동안 띄울 설명. 비어 있으면 아무것도 하지 않는다.")]
        [SerializeField, TextArea] private string note = string.Empty;

        private PlayerInteractor interactor;
        private bool wasFocused;

        private void Update()
        {
            if (string.IsNullOrEmpty(note))
            {
                return;
            }

            if (interactor == null)
            {
                interactor = FindFirstObjectByType<PlayerInteractor>();
                if (interactor == null)
                {
                    return;
                }
            }

            bool focused = IsFocused();
            if (focused == wasFocused)
            {
                return;
            }

            wasFocused = focused;

            if (focused)
            {
                // 0 초는 「지우지 않는다」는 뜻이다. HudGuideText 가 무한대로 잡는다.
                HudGuideText.Show(note, 0f);
            }
            else
            {
                HudGuideText.Hide();
            }
        }

        /// <summary>조준한 채로 꺼지면(장치 교체·챕터 전환) 설명이 남지 않게 한다.</summary>
        private void OnDisable()
        {
            if (!wasFocused)
            {
                return;
            }

            wasFocused = false;
            HudGuideText.Hide();
        }

        private bool IsFocused()
        {
            Component current = interactor.Current as Component;
            return current != null && current.transform.IsChildOf(transform);
        }
    }
}
