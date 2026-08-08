namespace Treeline
{
    /// <summary>
    /// 지금 플레이어를 쫓고 있는 적. <b>연출이 적의 종류를 몰라도 되게</b> 하는 계약이다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 추격 음악처럼 「누구든 쫓고 있으면」 으로 켜지는 것들이 이것을 읽는다.
    /// <see cref="ChaseMusic"/> 이 처음에 <c>Crawler[]</c> 를 들고 있었는데, 그래서
    /// 6 장 <see cref="Colossus"/> 는 같은 연출을 못 받고 생으로 켜고 꺼야 했다.
    /// 적을 하나 더 만들 때마다 연출 쪽을 고치게 되는 모양이라 계약으로 뺐다.
    /// </para>
    /// <para>
    /// 이름을 <c>IsChasing</c> 이 아니라 <see cref="IsHunting"/> 으로 둔 이유가 있다.
    /// 두 적 모두 이미 <c>IsChasing</c> 을 갖고 있지만 그것은 <b>내부 상태</b>이고,
    /// 여기서 필요한 것은 「연출을 켜야 하는가」다. 죽어 가는 적은 <c>IsChasing</c>
    /// 이 남아 있어도 사냥 중이 아니다. 둘을 같은 이름으로 묶으면 그 차이를 표현할
    /// 곳이 없어진다.
    /// </para>
    /// </remarks>
    public interface IHunter
    {
        /// <summary>지금 플레이어를 쫓는 중인지. 살아 있고 추격 상태일 때만 true 다.</summary>
        bool IsHunting { get; }
    }
}
