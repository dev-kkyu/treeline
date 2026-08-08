using System.Collections.Generic;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 플레이어가 <b>들고 있는 것</b>을 씬 밖으로 떼어낸 사본.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 장마다 플레이어가 새로 생기므로, 1 장에서 주운 손전등은 2 장이 열리는 순간
    /// 사라진다. 씬에 놓인 프리팹은 <c>Awake</c> 에서 인스펙터 기본값으로 자기를
    /// 초기화하기 때문이다. 그래서 나갈 때 여기 베껴 두고 들어와서 되돌린다.
    /// </para>
    /// <para>
    /// 플레이어를 <c>DontDestroyOnLoad</c> 로 살려 두는 방법도 있지만 택하지 않았다.
    /// 그러면 장마다 놓인 <c>Player</c> 를 지우거나 스폰 지점으로 옮기는 처리가
    /// 필요하고, <b>레벨을 만드는 쪽이 씬에서 플레이어를 보고 배치를 맞추는 방식</b>을
    /// 못 쓰게 된다. 데이터만 옮기면 씬 구성은 지금 그대로다.
    /// </para>
    /// <para>
    /// IMPORTANT: 새 항목이 생기면 <see cref="Capture"/> 와 <see cref="Apply"/>
    /// <b>양쪽</b>에 넣는다. 한쪽만 고치면 조용히 안 넘어가고, 증상이 「가끔 사라진다」
    /// 라서 원인을 찾기 어렵다.
    /// </para>
    /// </remarks>
    public sealed class PlayerLoadout
    {
        private struct WeaponState
        {
            public WeaponKind Kind;
            public bool Owned;
            public int Loaded;
            public int Reserve;
        }

        private bool flashlightOwned;
        private float batterySeconds;
        private readonly List<WeaponState> weapons = new List<WeaponState>();
        private bool hasEquipped;
        private WeaponKind equipped;

        /// <summary>
        /// 지금 씬의 플레이어에서 베껴 온다. 플레이어가 없으면 <c>null</c> 이다.
        /// </summary>
        /// <remarks>
        /// 타이틀과 엔딩에는 플레이어가 없다. 거기서 부르면 빈 것을 만들지 않고
        /// <c>null</c> 을 돌려주어, 「베낄 것이 없었다」와 「빈손이었다」를 구분한다.
        /// </remarks>
        public static PlayerLoadout Capture(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            var loadout = new PlayerLoadout();

            Flashlight flashlight = player.GetComponentInChildren<Flashlight>(true);
            if (flashlight != null)
            {
                loadout.flashlightOwned = flashlight.IsOwned;
                loadout.batterySeconds = flashlight.BatterySeconds;
            }

            foreach (PlayerWeapon weapon in player.GetComponentsInChildren<PlayerWeapon>(true))
            {
                loadout.weapons.Add(new WeaponState
                {
                    Kind = weapon.Kind,
                    Owned = weapon.IsOwned,
                    Loaded = weapon.Loaded,
                    Reserve = weapon.Reserve,
                });
            }

            WeaponSwitcher switcher = player.GetComponentInChildren<WeaponSwitcher>(true);
            if (switcher != null && switcher.Current != null)
            {
                loadout.hasEquipped = true;
                loadout.equipped = switcher.Current.Kind;
            }

            return loadout;
        }

        /// <summary>새 장의 플레이어에게 그대로 되돌린다.</summary>
        /// <remarks>
        /// 무기는 <b>종류로 짝을 맞춘다.</b> 배열 순서로 맞추면 어느 장에서 무기
        /// 칸 순서를 바꾸는 순간 권총 탄약이 산탄총으로 들어간다.
        /// </remarks>
        public void Apply(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            Flashlight flashlight = player.GetComponentInChildren<Flashlight>(true);
            if (flashlight != null)
            {
                flashlight.RestoreState(flashlightOwned, batterySeconds);
            }

            foreach (PlayerWeapon weapon in player.GetComponentsInChildren<PlayerWeapon>(true))
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    if (weapons[i].Kind != weapon.Kind)
                    {
                        continue;
                    }

                    weapon.RestoreState(weapons[i].Owned, weapons[i].Loaded, weapons[i].Reserve);
                    break;
                }
            }

            if (!hasEquipped)
            {
                return;
            }

            WeaponSwitcher switcher = player.GetComponentInChildren<WeaponSwitcher>(true);
            if (switcher != null)
            {
                // 안 주운 총이면 Equip 이 거부하므로 따로 검사하지 않는다.
                switcher.Equip(equipped);
            }
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("손전등=").Append(flashlightOwned ? "O" : "X")
              .Append(" 배터리=").Append(batterySeconds.ToString("F0")).Append("s");
            for (int i = 0; i < weapons.Count; i++)
            {
                sb.Append(" / ").Append(weapons[i].Kind).Append("=").Append(weapons[i].Owned ? "O" : "X")
                  .Append(" ").Append(weapons[i].Loaded).Append("+").Append(weapons[i].Reserve);
            }

            return sb.ToString();
        }
    }
}
