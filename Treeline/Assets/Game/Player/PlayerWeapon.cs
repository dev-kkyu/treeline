using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 권총. 레이캐스트 한 발로 <see cref="IDamageable"/> 을 때린다.
    /// </summary>
    /// <remarks>
    /// 탄약이 부족한 것이 이 게임의 긴장이므로 기본 소지량을 0 으로 두고
    /// <see cref="AddAmmo"/> 로만 채운다. 산탄총은 2단계에 같은 모양으로 하나 더 만든다.
    /// 반동과 조준 흔들림은 움직이는 것을 본 뒤에 넣는다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("총알이 나가는 기준. 보통 카메라다.")]
        [SerializeField] private Transform muzzle;

        [SerializeField] private int damagePerShot = 1;

        [SerializeField] private float range = 40f;

        [Tooltip("초당 발사 수.")]
        [SerializeField] private float shotsPerSecond = 2f;

        [SerializeField] private int startingAmmo;

        [SerializeField] private int maxAmmo = 24;

        private float nextShotTime;

        public int Ammo { get; private set; }

        public int MaxAmmo => maxAmmo;

        public event Action<int> AmmoChanged;

        /// <summary>실제로 한 발 나갔을 때. 소리 · 총구 섬광이 여기에 붙는다.</summary>
        public event Action Fired;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (input == null || muzzle == null)
            {
                Debug.LogError($"{nameof(PlayerWeapon)}: {nameof(input)} 또는 {nameof(muzzle)} 이 비어 있다.", this);
                enabled = false;
                return;
            }

            Ammo = Mathf.Clamp(startingAmmo, 0, maxAmmo);
        }

        private void Update()
        {
            // 포인터가 잠기기 전의 첫 클릭은 잠금용이다. 그것으로 한 발 나가면
            // 탄약이 귀한 게임에서 손해가 크다. PlayerLook 과 짝이다.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (!input.AttackPressed || Ammo <= 0 || Time.time < nextShotTime)
            {
                return;
            }

            nextShotTime = Time.time + (shotsPerSecond > 0f ? 1f / shotsPerSecond : 0f);
            Ammo--;
            AmmoChanged?.Invoke(Ammo);
            Fired?.Invoke();

            Fire();
        }

        /// <summary>탄약을 줍는다. 넘치는 분은 버린다.</summary>
        /// <returns>실제로 채운 발수.</returns>
        public int AddAmmo(int rounds)
        {
            int before = Ammo;
            Ammo = Mathf.Clamp(Ammo + Mathf.Max(0, rounds), 0, maxAmmo);

            if (Ammo != before)
            {
                AmmoChanged?.Invoke(Ammo);
            }

            return Ammo - before;
        }

        private void Fire()
        {
            Vector3 origin = muzzle.position;
            Vector3 direction = muzzle.forward;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, range,
                    GameLayers.Shot, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            // 콜라이더가 자식에 달려 있을 수 있어 부모까지 올라가며 찾는다.
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive)
            {
                return;
            }

            target.TakeDamage(new DamageInfo(damagePerShot, hit.point, direction, gameObject));
        }
    }
}
