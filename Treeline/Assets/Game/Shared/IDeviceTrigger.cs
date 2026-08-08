using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 발동시킬 수 있는 장치. 셔터 · 히터 · 환기 팬 · 발전기 · 제설기 같은 것들이다.
    /// 레버나 스크립트 이벤트가 이것을 호출한다.
    /// </summary>
    /// <remarks>
    /// 장치 구현은 세 패턴으로 수렴한다 — 즉시 발동 · 시간이 걸리는 발동 · 지속 상태.
    /// <see cref="Deactivate"/> 가 있는 것은 세 번째 패턴 때문이다.
    /// 만드는 쪽은 장치 담당(C) 이다. 셋이 합의하지 않고 바꾸지 않는다.
    /// </remarks>
    public interface IDeviceTrigger
    {
        /// <summary>지금 켜져 있는지. 시간이 걸리는 발동은 다 끝난 뒤에 true 가 된다.</summary>
        bool IsActive { get; }

        /// <param name="source">발동시킨 쪽. 레버 · 트리거 볼륨 등.</param>
        void Activate(GameObject source);

        /// <summary>지속 상태 장치를 끈다. 즉시 발동 장치는 아무것도 하지 않아도 된다.</summary>
        void Deactivate(GameObject source);
    }
}
