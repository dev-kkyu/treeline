using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 피해를 받을 수 있는 것. 플레이어와 적이 **같은 인터페이스를 쓴다.**
    /// 총이 적을 죽이는 것과 적이 플레이어를 때리는 것이 같은 모양이라,
    /// 하나로 두면 A 와 B 가 서로를 기다리지 않는다.
    /// </summary>
    /// <remarks>공용이다. 셋이 합의하지 않고 바꾸지 않는다.</remarks>
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(in DamageInfo damage);
    }

    /// <summary>
    /// 피해 한 건의 정보.
    /// </summary>
    /// <remarks>
    /// 인자를 따로 늘어놓지 않고 구조체로 묶은 이유는 **나중에 필드를 더해도
    /// 구현한 쪽을 고치지 않아도 되기 때문**이다. 시그니처를 바꾸면 A 와 B 의 코드가
    /// 동시에 깨진다. 넉백 세기나 피해 종류가 필요해지면 여기에 필드를 더한다.
    /// </remarks>
    public readonly struct DamageInfo
    {
        /// <summary>깎을 체력. 플레이어는 3칸이라 보통 1 이다.</summary>
        public readonly int Amount;

        /// <summary>맞은 지점. 피격 효과를 여기에 띄운다.</summary>
        public readonly Vector3 Point;

        /// <summary>날아온 방향. 넉백과 피격 방향 표시에 쓴다.</summary>
        public readonly Vector3 Direction;

        /// <summary>때린 쪽. 없을 수도 있다(추락 · 폭발 등).</summary>
        public readonly GameObject Source;

        public DamageInfo(int amount, Vector3 point, Vector3 direction, GameObject source = null)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Source = source;
        }
    }
}
