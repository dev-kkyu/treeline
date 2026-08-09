using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 발동하면 바로 켜지고 <b>되돌릴 수 없는</b> 장치.
    /// 비상 셔터 · 연료 인화 · 수소 폭발 · 눈사태 제설포가 이것이다.
    /// </summary>
    /// <remarks>
    /// <see cref="Deactivate"/> 가 비어 있는 것은 빠뜨린 것이 아니다. 기획의 장치 아홉
    /// 중 껐다 켜는 것은 서치라이트와 제설기뿐이고 그 둘은 <see cref="SustainedDevice"/>
    /// 다. <b>셔터는 한 번 내려오면 끝이고, 그것이 이 장치를 쓰는 이유다.</b> 되돌릴 수
    /// 있게 만들면 「뒤를 끊었다」가 성립하지 않는다.
    /// </remarks>
    public sealed class InstantDevice : Device
    {
        private bool isActive;

        public override bool IsActive => isActive;

        private void Reset()
        {
            gameObject.layer = GameLayers.Device;
        }

        public override void Activate(GameObject source)
        {
            if (isActive)
            {
                return;
            }

            isActive = true;
            ApplyState(true, notify: true);
        }

        /// <summary>되돌릴 수 없는 장치라 아무것도 하지 않는다.</summary>
        public override void Deactivate(GameObject source)
        {
        }

        public override void Restore(bool active)
        {
            isActive = active;
            ApplyState(active, notify: false);
        }
    }
}
