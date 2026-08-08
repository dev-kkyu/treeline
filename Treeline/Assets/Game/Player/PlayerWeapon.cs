using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>무기 종류. 획득물이 어느 총에 탄약을 넣을지 고르는 데 쓴다.</summary>
    public enum WeaponKind
    {
        Pistol = 0,
        Shotgun = 1,
    }

    /// <summary>
    /// 총 한 자루. 권총과 산탄총이 **같은 컴포넌트를 파라미터만 바꿔** 쓴다.
    /// </summary>
    /// <remarks>
    /// 산탄총은 <see cref="pelletsPerShot"/> 을 늘리고 <see cref="spreadDegrees"/> 를 벌린 것이다.
    /// 총마다 클래스를 만들지 않는 이유는 장치를 세 패턴으로 수렴시킨 것과 같다 —
    /// 차이가 값뿐이면 값만 다르게 둔다.
    ///
    /// 장착 전환은 <see cref="WeaponSwitcher"/> 가 이 컴포넌트를 켜고 끄는 것으로 한다.
    /// 꺼진 무기는 <c>Update</c> 가 돌지 않으므로 입력을 볼 일이 없다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponKind kind = WeaponKind.Pistol;

        [SerializeField] private PlayerInputReader input;

        [Tooltip("총알이 나가는 기준. 보통 카메라다.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("반동을 얹을 곳. 비어 있으면 반동이 없다.")]
        [SerializeField] private PlayerLook look;

        [Header("탄도")]
        [Tooltip("한 발에 나가는 탄자 수. 권총 1, 산탄총 여럿.")]
        [SerializeField] private int pelletsPerShot = 1;

        [Tooltip("탄자가 퍼지는 반각(도). 0 이면 정확히 조준점으로 간다.")]
        [SerializeField] private float spreadDegrees;

        [SerializeField] private int damagePerPellet = 1;

        [SerializeField] private float range = 40f;

        [Tooltip("초당 발사 수.")]
        [SerializeField] private float shotsPerSecond = 2f;

        [Header("탄약")]
        [SerializeField] private int startingAmmo;

        [SerializeField] private int maxAmmo = 24;

        [Header("반동")]
        [Tooltip("한 발에 위로 튀는 각도.")]
        [SerializeField] private float recoilUpDegrees = 1.6f;

        [Tooltip("한 발에 좌우로 틀어지는 각도. 방향은 매번 무작위다.")]
        [SerializeField] private float recoilSideDegrees = 0.6f;

        private float nextShotTime;

        public WeaponKind Kind => kind;

        public int Ammo { get; private set; }

        public int MaxAmmo => maxAmmo;

        public event Action<int> AmmoChanged;

        /// <summary>실제로 한 발 나갔을 때. 소리 · 총구 섬광이 여기에 붙는다.</summary>
        public event Action Fired;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponentInParent<PlayerInputReader>();
            }

            if (look == null)
            {
                look = GetComponentInParent<PlayerLook>();
            }

            if (input == null || muzzle == null)
            {
                Debug.LogError($"{nameof(PlayerWeapon)}({kind}): {nameof(input)} 또는 {nameof(muzzle)} 이 비어 있다.", this);
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

            if (look != null)
            {
                look.AddRecoil(recoilUpDegrees, UnityEngine.Random.Range(-recoilSideDegrees, recoilSideDegrees));
            }
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
            int pellets = Mathf.Max(1, pelletsPerShot);

            for (int i = 0; i < pellets; i++)
            {
                Vector3 direction = spreadDegrees > 0f ? Spread(muzzle.forward) : muzzle.forward;

                if (!Physics.Raycast(origin, direction, out RaycastHit hit, range,
                        GameLayers.Shot, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                // 콜라이더가 자식에 달려 있을 수 있어 부모까지 올라가며 찾는다.
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                target.TakeDamage(new DamageInfo(damagePerPellet, hit.point, direction, gameObject));
            }
        }

        /// <summary>조준 방향을 원뿔 안에서 흩뜨린다.</summary>
        private Vector3 Spread(Vector3 forward)
        {
            // insideUnitCircle 을 쓰면 원뿔 단면에 고르게 퍼진다. 각도 두 개를 따로
            // 무작위로 뽑으면 가운데가 촘촘해진다.
            Vector2 offset = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            return (forward + muzzle.right * offset.x + muzzle.up * offset.y).normalized;
        }
    }
}
