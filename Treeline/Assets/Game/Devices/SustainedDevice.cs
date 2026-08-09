using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 켜고 <b>끌 수 있는</b> 장치. 서치라이트와 로터리 제설기가 이것이다.
    /// </summary>
    /// <remarks>
    /// 세 패턴 중 <see cref="Deactivate"/> 가 실제로 무언가 하는 유일한 것이고,
    /// <c>IDeviceTrigger</c> 에 그 함수가 있는 이유도 이 패턴 때문이다.
    /// <para>
    /// 1장 서치라이트는 <b>위험하지 않을 때 먼저 만져보는</b> 물건이라 마음대로 켜고 끌
    /// 수 있어야 한다. 5장에서 같은 물건이 생존 수단이 되는데, 그때 조작이 달라지면
    /// 배운 것이 소용없어진다.
    /// </para>
    /// </remarks>
    public sealed class SustainedDevice : Device
    {
        [Header("처음 상태")]
        [Tooltip("씬이 시작할 때 켜져 있는지. 정전으로 꺼진 조명은 여기를 끈다.")]
        [SerializeField] private bool startsActive;

        [Tooltip("켜져 있는 동안 계속 도는 소리. 제설기 · 서치라이트가 그렇다.")]
        [SerializeField] private AudioSource runningLoop;

        private bool isActive;

        public override bool IsActive => isActive;

        private void Reset()
        {
            gameObject.layer = GameLayers.Device;
        }

        protected override void Awake()
        {
            // base.Awake 가 IsActive 를 읽어 씬을 정렬하므로 그 전에 정해 둔다.
            isActive = startsActive;
            base.Awake();
        }

        public override void Activate(GameObject source)
        {
            Set(true);
        }

        public override void Deactivate(GameObject source)
        {
            Set(false);
        }

        public override void Restore(bool active)
        {
            isActive = active;

            // 돌던 소리는 켜진 채로 되돌아와야 한다. 조용한 서치라이트는 고장으로 읽힌다.
            if (runningLoop != null)
            {
                if (active)
                {
                    runningLoop.loop = true;
                    runningLoop.Play();
                }
                else
                {
                    runningLoop.Stop();
                }
            }

            ApplyState(active, notify: false);
        }

        private void Set(bool active)
        {
            if (isActive == active)
            {
                return;
            }

            isActive = active;

            if (runningLoop != null)
            {
                if (active)
                {
                    runningLoop.loop = true;
                    runningLoop.Play();
                }
                else
                {
                    runningLoop.Stop();
                }
            }

            ApplyState(active, notify: true);
        }
    }
}
