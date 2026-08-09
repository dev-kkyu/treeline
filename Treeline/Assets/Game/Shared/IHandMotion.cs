namespace Treeline
{
    /// <summary>상호작용할 때 손이 하는 동작.</summary>
    /// <remarks>
    /// 팔 리그에 있는 동작만큼만 있다. 늘리려면 클립이 먼저 있어야 한다.
    /// </remarks>
    public enum HandMotion
    {
        /// <summary>손을 뻗어 건드린다. 획득물 · 읽을 것 · 버튼.</summary>
        Reach = 0,

        /// <summary>두 손으로 잡고 당긴다. 레버 · 밸브처럼 힘을 주는 것.</summary>
        Pull = 1,
    }

    /// <summary>
    /// 손이 자기에게 어떻게 닿을지 스스로 정하는 상호작용물.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: <see cref="IInteractable"/> 에 넣지 않고 따로 둔다. 그쪽은 셋이
    /// 합의한 계약이라 함부로 늘리지 않고, <b>이것은 없어도 되는 정보</b>이기
    /// 때문이다 — 구현하지 않으면 <see cref="HandMotion.Reach"/> 로 친다.
    /// <para>
    /// 팔 쪽에서 <c>is Lever</c> 로 타입을 보고 고를 수도 있었다. 그러면 플레이어
    /// 코드가 상호작용 코드를 알게 되고, 밸브나 크랭크가 생길 때마다 팔을 고쳐야
    /// 한다. 자기 동작은 자기가 안다.
    /// </para>
    /// </remarks>
    public interface IHandMotion
    {
        HandMotion Motion { get; }
    }
}
