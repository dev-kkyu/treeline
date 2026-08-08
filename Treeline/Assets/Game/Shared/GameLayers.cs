using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 레이어 번호와 레이캐스트 마스크.
    /// <c>ProjectSettings/TagManager.asset</c> 과 짝이라 한쪽만 고치면 조용히 어긋난다.
    /// 둘은 같은 커밋에 담는다.
    /// </summary>
    /// <remarks>
    /// 레이어를 추가할 땐 자기 구간 안에서만 쓴다. 그래야 셋이 동시에 추가해도
    /// 번호가 겹치지 않는다. A 8~11 / B 12~15 / C 16~23 / 공용 24~31.
    /// 구간이 모자라면 24 번대에서 가져오되 셋이 합의한다.
    /// </remarks>
    public static class GameLayers
    {
        public const int Default = 0;

        // A — 플레이어 · 전투
        public const int Player = 8;

        /// <summary>1인칭 손 · 무기 모델. 별도 카메라로 그려 벽에 안 박히게 한다. 물리 없음.</summary>
        public const int ViewModel = 9;

        // B — 적
        public const int Enemy = 12;

        // C — 상호작용 · 장치 · 진행
        public const int Interactable = 16;

        /// <summary>양손 운반물. 들고 있는 동안 플레이어와 부딪히지 않는다.</summary>
        public const int Carriable = 17;

        public const int Device = 18;

        /// <summary>스크립트 이벤트 · 체크포인트 볼륨. 플레이어만 감지하고 아무것도 막지 않는다.</summary>
        public const int Trigger = 19;

        /// <summary>불 · 수소 폭발 · 제설기처럼 닿으면 피해를 주는 영역.</summary>
        public const int Hazard = 20;

        /// <summary>플레이어가 바라보는 대상을 찾을 때.</summary>
        public static readonly LayerMask Interaction = MaskOf(Interactable, Carriable, Device);

        /// <summary>총알 판정. <see cref="Trigger"/> 와 <see cref="ViewModel"/> 은 들어가지 않는다.</summary>
        public static readonly LayerMask Shot = MaskOf(Default, Enemy);

        /// <summary>적이 플레이어를 볼 수 있는지 확인할 때.</summary>
        public static readonly LayerMask EnemySight = MaskOf(Default, Player);

        private static int MaskOf(params int[] layers)
        {
            int mask = 0;
            foreach (int layer in layers)
            {
                mask |= 1 << layer;
            }

            return mask;
        }
    }
}
