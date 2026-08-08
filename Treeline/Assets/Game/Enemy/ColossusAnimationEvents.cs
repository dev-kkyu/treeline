using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// <see cref="Colossus"/> 애니메이션의 박자를 밖으로 내보내는 중계기.
    /// <b>Animator 와 같은 오브젝트</b>에 붙인다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IMPORTANT: 애니메이션 이벤트는 Animator 가 붙은 GameObject 의 컴포넌트에서만
    /// 함수를 찾는다. 부모도 자식도 보지 않는다. 받는 쪽이 없으면 콘솔에
    /// 「<c>AnimationEvent ... has no receiver!</c>」 가 프레임마다 쌓인다.
    /// <see cref="CrawlerAnimationEvents"/> 와 같은 이유로 있는 파일이다.
    /// </para>
    /// <para>
    /// <c>Colossus.fbx</c> 에는 이벤트가 <b>넷</b> 들어 있는데 2026-08-09 까지
    /// 프로젝트에 받는 쪽이 하나도 없었다. 타이틀 배경에 실루엣을 올리면서 처음
    /// 드러났지만, 6 장에서도 같은 오류가 났을 자리다.
    /// </para>
    /// <list type="table">
    /// <item><term><c>AE_ColossusRoar</c></term><description>`Roar` 22f — 포효 정점</description></item>
    /// <item><term><c>AE_ColossusThreat</c></term><description>`Threat` 26f — 위협 정점</description></item>
    /// <item><term><c>AE_GrabLock</c></term><description>`Grab_Execute` 8f — 사거리를 다시 재는 지점</description></item>
    /// <item><term><c>AE_GrabKill</c></term><description>`Grab_Execute` 90f — 여기서 죽는다</description></item>
    /// </list>
    /// <para>
    /// 포획 둘은 <b>아직 게임 로직이 없다.</b> 그래서 이 파일은 신호만 내보내고
    /// 판정하지 않는다. 붙이는 쪽이 <see cref="GrabLock"/> · <see cref="GrabKill"/> 을
    /// 구독하면 되고, 그때 <c>docs/enemy-sheets/.../12-grab-execute.md</c> 의
    /// 「8f 에서 사거리를 다시 검사해 벗어났으면 Grab_Miss 로」 를 따른다.
    /// </para>
    /// <para>
    /// 소리 칸은 <b>비워 두어도 된다.</b> 타이틀 배경처럼 소리를 다른 곳에서 내는
    /// 경우가 있어서다. 채우면 프레임에 정확히 맞춰 나가고, 비우면 아무 일도
    /// 하지 않는다 — 두 군데서 내면 두 번 울린다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ColossusAnimationEvents : MonoBehaviour
    {
        [Header("소리 — 비워도 된다")]
        [Tooltip("포효 정점에 낼 소리. 다른 곳에서 내고 있으면 비운다.")]
        [SerializeField] private AudioSource roarSource;

        [SerializeField] private AudioClip roarClip;

        /// <summary>포효 정점(`Roar` 22f).</summary>
        public event Action RoarPeak;

        /// <summary>위협 정점(`Threat` 26f).</summary>
        public event Action ThreatPeak;

        /// <summary>포획 고정(`Grab_Execute` 8f). 여기서 사거리를 다시 잰다.</summary>
        public event Action GrabLock;

        /// <summary>치명(`Grab_Execute` 90f). 여기서 죽는다.</summary>
        public event Action GrabKill;

        public void AE_ColossusRoar()
        {
            if (roarSource != null && roarClip != null)
            {
                roarSource.PlayOneShot(roarClip);
            }

            RoarPeak?.Invoke();
        }

        public void AE_ColossusThreat()
        {
            ThreatPeak?.Invoke();
        }

        public void AE_GrabLock()
        {
            GrabLock?.Invoke();
        }

        public void AE_GrabKill()
        {
            GrabKill?.Invoke();
        }
    }
}
