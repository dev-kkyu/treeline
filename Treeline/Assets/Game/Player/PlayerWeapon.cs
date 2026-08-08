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

        [Tooltip("화면에 보이는 총 모델. 이 컴포넌트가 켜질 때만 보인다.")]
        [SerializeField] private GameObject viewModel;

        [Header("탄도")]
        [Tooltip("한 발에 나가는 탄자 수. 권총 1, 산탄총 여럿.")]
        [SerializeField] private int pelletsPerShot = 1;

        [Tooltip("탄자가 퍼지는 반각(도). 0 이면 정확히 조준점으로 간다.")]
        [SerializeField] private float spreadDegrees;

        [SerializeField] private int damagePerPellet = 1;

        [SerializeField] private float range = 40f;

        [Tooltip("초당 발사 수.")]
        [SerializeField] private float shotsPerSecond = 2f;

        [Header("획득")]
        [Tooltip("시작할 때 이미 가지고 있는가. 권총은 3장, 산탄총은 5장에서 얻는다.")]
        [SerializeField] private bool startsOwned;

        [Header("탄약")]
        [Tooltip("탄창 하나에 들어가는 발수.")]
        [SerializeField] private int magazineSize = 8;

        [Tooltip("여분 탄약 최대치. 탄창 안은 여기 포함되지 않는다.")]
        [SerializeField] private int maxReserve = 24;

        [Tooltip("시작할 때 가진 총 탄약. 여기서 탄창을 먼저 채운다.")]
        [SerializeField] private int startingAmmo;

        [Tooltip("재장전에 걸리는 시간(초). 정비공이라 빠르지 않다.")]
        [SerializeField] private float reloadSeconds = 1.8f;

        [Header("반동")]
        [Tooltip("한 발에 위로 튀는 각도.")]
        [SerializeField] private float recoilUpDegrees = 1.6f;

        [Tooltip("한 발에 좌우로 틀어지는 각도. 방향은 매번 무작위다.")]
        [SerializeField] private float recoilSideDegrees = 0.6f;

        [Header("재장전 동작")]
        [Tooltip("재장전 중에 총이 내려가는 깊이(m).")]
        [SerializeField] private float reloadDipMeters = 0.1f;

        [Tooltip("재장전 중에 총구가 내려가는 각도.")]
        [SerializeField] private float reloadTiltDegrees = 30f;

        [Tooltip("재장전 중에 총이 안쪽으로 눕는 각도.")]
        [SerializeField] private float reloadRollDegrees = 16f;

        private float nextShotTime;
        private bool reloading;
        private float reloadStartTime;
        private float reloadEndTime;
        private Vector3 viewModelBasePosition;
        private Quaternion viewModelBaseRotation;

        public WeaponKind Kind => kind;

        /// <summary>주웠는가. 안 주운 총은 들 수 없다.</summary>
        public bool IsOwned { get; private set; }

        /// <summary>탄창에 든 발수. 0 이면 재장전해야 쏜다.</summary>
        public int Loaded { get; private set; }

        /// <summary>여분 탄약. 재장전할 때 여기서 탄창으로 옮긴다.</summary>
        public int Reserve { get; private set; }

        public int MagazineSize => magazineSize;

        public int MaxReserve => maxReserve;

        public bool IsReloading => reloading;

        /// <summary>
        /// 장을 넘어온 상태를 그대로 되돌린다. <see cref="GameFlow"/> 가 부른다.
        /// </summary>
        /// <remarks>
        /// <see cref="PickUp"/> · <see cref="AddAmmo"/> 로는 못 한다. 둘 다 더하는
        /// 함수라 이전 장에서 쓴 만큼을 표현할 수 없고, 특히 탄창(<see cref="Loaded"/>)
        /// 은 넣을 방법이 아예 없다.
        /// </remarks>
        public void RestoreState(bool owned, int loaded, int reserve)
        {
            IsOwned = owned;
            Loaded = Mathf.Clamp(loaded, 0, magazineSize);
            Reserve = Mathf.Clamp(reserve, 0, maxReserve);
            AmmoChanged?.Invoke();
        }

        /// <summary>탄창이나 여분이 바뀌었다. 어느 쪽인지는 읽는 쪽이 본다.</summary>
        public event Action AmmoChanged;

        /// <summary>재장전을 시작하거나 끝냈다.</summary>
        public event Action<bool> ReloadingChanged;

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

            if (viewModel != null)
            {
                viewModelBasePosition = viewModel.transform.localPosition;
                viewModelBaseRotation = viewModel.transform.localRotation;
            }

            IsOwned = startsOwned;

            // 가진 탄약에서 탄창을 먼저 채우고 나머지를 여분으로 둔다.
            int total = Mathf.Max(0, startingAmmo);
            Loaded = Mathf.Min(magazineSize, total);
            Reserve = Mathf.Clamp(total - Loaded, 0, maxReserve);

            SetViewModelVisible(false);
        }

        // 장착 여부와 모델 표시를 한 곳에 묶는다. WeaponSwitcher 가 이 컴포넌트를
        // 켜고 끄는 것만으로 화면의 총도 같이 바뀐다.
        private void OnEnable()
        {
            SetViewModelVisible(true);
        }

        private void OnDisable()
        {
            // 총을 바꾸거나 무거운 것을 들면 재장전이 끊긴다. 다시 들면 처음부터다.
            CancelReload();
            SetViewModelVisible(false);
        }

        private void Update()
        {
            // 포인터가 잠기기 전의 첫 클릭은 잠금용이다. 그것으로 한 발 나가면
            // 탄약이 귀한 게임에서 손해가 크다. PlayerLook 과 짝이다.
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (reloading)
            {
                UpdateReloadPose();

                if (Time.time >= reloadEndTime)
                {
                    FinishReload();
                }

                return;
            }

            if (input.ReloadPressed)
            {
                TryStartReload();
                return;
            }

            if (!input.AttackPressed || Loaded <= 0 || Time.time < nextShotTime)
            {
                return;
            }

            nextShotTime = Time.time + (shotsPerSecond > 0f ? 1f / shotsPerSecond : 0f);
            Loaded--;
            AmmoChanged?.Invoke();
            Fired?.Invoke();

            Fire();

            if (look != null)
            {
                look.AddRecoil(recoilUpDegrees, UnityEngine.Random.Range(-recoilSideDegrees, recoilSideDegrees));
            }
        }

        /// <summary>
        /// 재장전을 시작한다. 탄창이 이미 가득하거나 여분이 없으면 아무 일도 없다.
        /// </summary>
        /// <returns>실제로 시작했으면 참.</returns>
        public bool TryStartReload()
        {
            if (reloading || Loaded >= magazineSize || Reserve <= 0)
            {
                return false;
            }

            reloading = true;
            reloadStartTime = Time.time;
            reloadEndTime = Time.time + Mathf.Max(0.01f, reloadSeconds);
            ReloadingChanged?.Invoke(true);
            return true;
        }

        /// <summary>
        /// 이 총을 주웠다. 이제 들 수 있다. 부르는 쪽은 <see cref="WeaponSwitcher.PickUp"/> 이다.
        /// </summary>
        /// <remarks>탄약은 별개다. 총만 줍고 탄이 없을 수 있다.</remarks>
        public void PickUp()
        {
            IsOwned = true;
        }

        /// <summary>여분 탄약을 줍는다. 넘치는 분은 버린다.</summary>
        /// <remarks>총을 아직 안 주웠어도 탄약은 쌓인다. 3장에서 권총과 탄약을 함께 얻는다.</remarks>
        /// <returns>실제로 채운 발수.</returns>
        public int AddAmmo(int rounds)
        {
            int before = Reserve;
            Reserve = Mathf.Clamp(Reserve + Mathf.Max(0, rounds), 0, maxReserve);

            if (Reserve != before)
            {
                AmmoChanged?.Invoke();
            }

            return Reserve - before;
        }

        private void FinishReload()
        {
            int take = Mathf.Min(magazineSize - Loaded, Reserve);
            Loaded += take;
            Reserve -= take;

            reloading = false;
            ResetViewModelPose();
            ReloadingChanged?.Invoke(false);
            AmmoChanged?.Invoke();
        }

        private void CancelReload()
        {
            if (!reloading)
            {
                return;
            }

            reloading = false;
            ResetViewModelPose();
            ReloadingChanged?.Invoke(false);
        }

        /// <summary>총을 내렸다 올리는 동작. 뼈대가 없으니 총 전체를 움직인다.</summary>
        private void UpdateReloadPose()
        {
            if (viewModel == null)
            {
                return;
            }

            float t = Mathf.InverseLerp(reloadStartTime, reloadEndTime, Time.time);

            // 0 에서 시작해 가운데서 가장 크고 다시 0 으로 돌아온다. 끝났을 때
            // 원래 자세와 정확히 맞아 튀지 않는다.
            float amount = Mathf.Sin(t * Mathf.PI);

            Transform tr = viewModel.transform;
            tr.localPosition = viewModelBasePosition + Vector3.down * (reloadDipMeters * amount);
            tr.localRotation = viewModelBaseRotation * Quaternion.Euler(
                -reloadTiltDegrees * amount, 0f, reloadRollDegrees * amount);
        }

        private void ResetViewModelPose()
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.transform.localPosition = viewModelBasePosition;
            viewModel.transform.localRotation = viewModelBaseRotation;
        }

        private void SetViewModelVisible(bool visible)
        {
            if (viewModel != null && viewModel.activeSelf != visible)
            {
                viewModel.SetActive(visible);
            }
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
