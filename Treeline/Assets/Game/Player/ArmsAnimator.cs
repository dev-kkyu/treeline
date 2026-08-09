using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 1인칭 팔을 무기 상태에 맞춰 움직인다. 들기 · 쉬기 · 쏘기 · 재장전.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="WeaponAudio"/> · <see cref="WeaponVfx"/> 와 같은 자리다 — 무기의
    /// 이벤트만 구독하고 <see cref="PlayerWeapon"/> 은 건드리지 않는다. 팔을 빼도
    /// 사격은 그대로 돌아간다.
    /// </para>
    /// <para>
    /// IMPORTANT: 전이(Transition)를 컨트롤러에 그리지 않고 <b>코드에서
    /// <see cref="Animator.CrossFadeInFixedTime(string,float)"/> 로 건다.</b> 상태가
    /// 17 개인데 전이를 다 그리면 조건이 수십 개가 되고, 그 그래프는 스크립트로
    /// 만들면 읽을 수도 고칠 수도 없는 물건이 된다. 여기서는 「지금 무슨 상태인가」를
    /// 무기가 이미 알고 있으므로 이름으로 부르는 편이 짧고 분명하다.
    /// </para>
    /// <para>
    /// 쏘기와 재장전은 <b>끝나면 스스로 쉬기로 돌아온다.</b> 클립이 루프가 아니라
    /// 마지막 프레임에서 멈추므로, 돌려보내지 않으면 팔이 굳은 채로 남는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ArmsAnimator : MonoBehaviour
    {
        /// <summary>맨손. 아무 무기도 안 들었을 때.</summary>
        private const string StateIdle = "Arms_Idle";

        [Tooltip("팔 리그의 Animator. 비우면 자식에서 찾는다.")]
        [SerializeField] private Animator animator;

        [Tooltip("무기 교체를 알려 주는 쪽. 비우면 부모에서 찾는다.")]
        [SerializeField] private WeaponSwitcher switcher;

        [Tooltip("상호작용을 알려 주는 쪽. 비우면 부모에서 찾는다.")]
        [SerializeField] private PlayerInteractor interactor;

        [Tooltip("상태를 바꿀 때 섞는 시간(초).")]
        [SerializeField, Range(0f, 0.4f)] private float blendSeconds = 0.08f;

        [Header("총열 정렬")]
        [Tooltip("권총을 들 때 팔 전체를 이만큼 돌린다(도).")]
        [SerializeField] private float pistolYawDegrees = 11.3f;

        [Tooltip("산탄총을 들 때 팔 전체를 이만큼 돌린다(도).")]
        [SerializeField] private float shotgunYawDegrees = 32f;

        [Tooltip("돌리는 중심. 오른손이 총을 쥔 자리이며 부모(카메라) 기준이다.")]
        [SerializeField] private Vector3 gripPivot = new Vector3(0.227f, -0.307f, 0.469f);

        private PlayerWeapon subscribed;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private string idleState = StateIdle;
        private float returnToIdleAt = float.PositiveInfinity;
        private Renderer[] armsRenderers;
        private bool wasCarrying;
        private bool weaponHidden;

        /// <summary>이번 프레임에 상호작용이 일어났다. <c>Update</c> 가 처리한다.</summary>
        private IInteractable pendingInteract;

        /// <summary>
        /// 무기별 부품. <b>재부모화하지 않고 이름으로 켜고 끈다.</b>
        /// </summary>
        /// <remarks>
        /// 처음에는 빈 오브젝트로 묶어 <c>viewModel</c> 에 물렸는데, 이 부품들은
        /// FBX 프리팹 인스턴스의 자식이라 <b>밖으로 옮기면 프리팹 저장에서
        /// 되돌려진다.</b> 묶음만 빈 채로 남아 총이 통째로 사라졌다.
        /// </remarks>
        /// <remarks>
        /// IMPORTANT: <b>리그에 딸려 온 스킨 무기를 그대로 쓴다.</b> 한동안 낱개
        /// FBX 를 손 본에 물려 놓고 각도를 손으로 맞췄는데, 그러면 클립마다 자세가
        /// 달라 어느 한 자세에 맞추면 나머지가 어긋난다. 스킨 무기는 애니메이션이
        /// 직접 잡아 주므로 맞출 것이 없고, 노리쇠 · 펌프 · 탄피까지 같이 움직인다.
        /// <para>
        /// 이것들이 「안 나온다」고 판단해 낱개 FBX 로 우회했던 것이 실수였다. 원인은
        /// 두 가지였다 — <b>오브젝트 자체가 꺼져 있었고</b>(렌더러만 켜서는 안 나온다),
        /// <c>updateWhenOffscreen</c> 이 꺼져 있어 <b>바인드 포즈 기준 바운즈</b>가
        /// 팔에서 40 m 떨어진 곳으로 잡혀 프러스텀에서 잘려 나갔다.
        /// </para>
        /// </remarks>
        private static readonly string[] PistolParts = { "W_Pistol", "W_PistolMag", "W_Slide" };

        private static readonly string[] ShotgunParts = { "W_Shotgun", "W_Pump", "W_TubeFill", "W_Shell" };

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (switcher == null)
            {
                switcher = GetComponentInParent<WeaponSwitcher>();
            }

            if (interactor == null)
            {
                interactor = GetComponentInParent<PlayerInteractor>();
            }

            if (animator == null)
            {
                Debug.LogError($"{nameof(ArmsAnimator)}: Animator 를 못 찾았다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (interactor != null)
            {
                interactor.Interacted += OnInteracted;
            }

            if (switcher != null)
            {
                switcher.Equipped += OnEquipped;
                OnEquipped(switcher.Current);
            }
            else
            {
                Play(StateIdle);
            }
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.Interacted -= OnInteracted;
            }

            if (switcher != null)
            {
                switcher.Equipped -= OnEquipped;
            }

            Unsubscribe();
        }

        private void Update()
        {
            bool carryChanged = UpdateCarry();

            if (pendingInteract != null)
            {
                // 든 것이 바뀌었으면 그쪽이 이긴다. 연료통을 집는 것도 상호작용이라
                // 둘 다 재생하면 손 뻗기가 드는 동작을 덮어쓴다. 어느 쪽이 먼저
                // 도는지에 기대지 않으려고 판단을 여기 한 곳에 모았다.
                if (!carryChanged)
                {
                    PlayInteract(pendingInteract);
                }

                pendingInteract = null;
            }

            // 한 번짜리 동작이 끝나면 쉬기로 돌려보낸다. 안 그러면 마지막 프레임에서
            // 팔이 굳는다. 상태 시간을 보지 않고 우리가 잰 시각을 쓰는 이유는,
            // 연사하면 같은 상태를 다시 걸어 normalizedTime 이 0 으로 돌아가서다.
            if (Time.time >= returnToIdleAt)
            {
                returnToIdleAt = float.PositiveInfinity;
                TakeWeaponBack();
                Play(idleState);
            }
        }

        /// <summary>무기가 바뀌었다. 구독을 갈아타고 드는 동작을 재생한다.</summary>
        private void OnEquipped(PlayerWeapon weapon)
        {
            Unsubscribe();
            weaponHidden = false;

            if (weapon == null)
            {
                // 아무것도 안 들었어도 팔은 남는다. 손이 사라지면 몸이 없는 시점이
                // 되어 1인칭이 깨진다 — 어느 장에서든 팔은 늘 있어야 한다.
                idleState = StateIdle;
                Play(idleState);
                SetArmsVisible(true);
                ShowParts(PistolParts, false);
                ShowParts(ShotgunParts, false);
                AimBarrelForward(0f);
                return;
            }

            subscribed = weapon;
            weapon.Fired += OnFired;
            weapon.ReloadingChanged += OnReloadingChanged;
            SetArmsVisible(true);

            bool shotgun = weapon.Kind == WeaponKind.Shotgun;
            ShowParts(shotgun ? ShotgunParts : PistolParts, true);
            ShowParts(shotgun ? PistolParts : ShotgunParts, false);
            AimBarrelForward(shotgun ? shotgunYawDegrees : pistolYawDegrees);

            string prefix = shotgun ? "Arms_Shotgun" : "Arms_Pistol";
            idleState = prefix + "Idle";
            PlayOnce(prefix + "Equip", 0.5f);
        }

        /// <summary>상호작용이 일어났다. 재생은 <c>Update</c> 로 미룬다.</summary>
        private void OnInteracted(IInteractable target)
        {
            pendingInteract = target;
        }

        /// <summary>
        /// 무언가를 조작하는 손동작. 대상이 <see cref="IHandMotion"/> 로 말해 주면
        /// 그대로 따르고, 아무 말이 없으면 손을 뻗는다.
        /// </summary>
        /// <remarks>
        /// 조작하는 동안 <b>들고 있던 총을 감춘다.</b> 이 클립들은 빈손을 전제로
        /// 만들어져 있어 총이 붙어 있으면 레버를 총으로 당기는 그림이 된다. 총열을
        /// 앞으로 돌려 두는 보정도 같이 푼다 — 그대로 두면 레버를 옆에서 더듬는다.
        /// </remarks>
        private void PlayInteract(IInteractable target)
        {
            HandMotion motion = target is IHandMotion m ? m.Motion : HandMotion.Reach;
            PutWeaponAway();

            if (motion == HandMotion.Pull)
            {
                PlayOnce("Arms_LeverPull", 1.17f);
                return;
            }

            PlayOnce("Arms_Interact", 0.57f);
        }

        private void PutWeaponAway()
        {
            if (weaponHidden)
            {
                return;
            }

            weaponHidden = true;
            ShowParts(PistolParts, false);
            ShowParts(ShotgunParts, false);
            AimBarrelForward(0f);
        }

        /// <summary>조작이 끝났다. 감춰 둔 총을 도로 손에 붙인다.</summary>
        private void TakeWeaponBack()
        {
            if (!weaponHidden)
            {
                return;
            }

            weaponHidden = false;

            if (subscribed == null)
            {
                return;
            }

            bool shotgun = subscribed.Kind == WeaponKind.Shotgun;
            ShowParts(shotgun ? ShotgunParts : PistolParts, true);
            AimBarrelForward(shotgun ? shotgunYawDegrees : pistolYawDegrees);
        }

        /// <summary>
        /// 팔 전체를 쥔 자리를 중심으로 돌려 <b>총열이 조준선을 향하게</b> 한다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: 리그는 총을 몸 앞으로 비스듬히 뉘여 쥔다 — 권총 11°, 산탄총 31°
        /// 왼쪽이다. 자세로는 자연스럽지만 1인칭에서는 <b>총구가 조준점을 안 보는</b>
        /// 그림이 되고, 탄이 화면 가운데서 나가므로 궤적이 총구에서 시작하지 않는다.
        /// <para>
        /// 총을 손에서 떼어 돌리면 그 각도만큼 손과 총이 어긋난다. 그래서 총이 아니라
        /// <b>팔 전체를</b> 돌리고, 중심을 손이 쥔 자리에 두어 손은 제자리에 남긴다.
        /// 이 오브젝트의 트랜스폼은 애니메이션이 건드리지 않으므로(클립은
        /// <c>Arms_Armature</c> 아래만 다룬다) 여기서 한 번 정해 두면 유지된다.
        /// </para>
        /// </remarks>
        private void AimBarrelForward(float yawDegrees)
        {
            Quaternion turn = Quaternion.AngleAxis(yawDegrees, Vector3.up);
            transform.localRotation = turn * baseRotation;
            transform.localPosition = gripPivot + turn * (basePosition - gripPivot);
        }

        private void Unsubscribe()
        {
            if (subscribed == null)
            {
                return;
            }

            subscribed.Fired -= OnFired;
            subscribed.ReloadingChanged -= OnReloadingChanged;
            subscribed = null;
        }

        // 한때 탄약이 떨어지면 빈손 자세로 돌려보냈다. 「쏠 수 없다」를 손으로
        // 알리려는 것이었는데, 실제로는 **총이 사라진 것처럼 보였다.** 총을 잃은
        // 것과 탄이 떨어진 것은 다른 일이고, 플레이어는 앞쪽으로 읽는다.
        // 지금은 빈 총도 계속 들고 있다 — 방아쇠를 당길 때 나는 딸깍 소리가
        // 이미 그 신호이므로(PlayerWeapon.DryFired) 자세까지 바꿀 이유가 없다.

        /// <summary>
        /// 무언가를 두 손으로 들고 있는 동안의 자세. <see cref="WeaponSwitcher"/> 가
        /// 총을 봉인하는 것과 같은 신호를 팔도 받는다 — 6장 연료통이 이것이다.
        /// </summary>
        /// <returns>이번 프레임에 드는/놓는 것이 바뀌었으면 참.</returns>
        private bool UpdateCarry()
        {
            if (switcher == null)
            {
                return false;
            }

            bool carrying = switcher.IsCarrying;
            if (carrying == wasCarrying)
            {
                return false;
            }

            wasCarrying = carrying;
            SetArmsVisible(true);
            if (carrying)
            {
                idleState = "Arms_CarryIdle";
                PutWeaponAway();
                PlayOnce("Arms_CarryLift", 0.77f);
            }
            else
            {
                // 내려놓고 나면 들고 있던 총으로 돌아간다.
                PlayerWeapon w = switcher.Current;
                idleState = w == null ? StateIdle
                    : (w.Kind == WeaponKind.Shotgun ? "Arms_ShotgunIdle" : "Arms_PistolIdle");
                PlayOnce("Arms_CarryDrop", 0.57f);
            }

            return true;
        }

        /// <summary>
        /// IMPORTANT: 오브젝트를 끄지 않고 <b>렌더러만</b> 끈다. 이 컴포넌트가 팔
        /// 오브젝트에 붙어 있어서, 오브젝트를 끄면 자기도 같이 꺼져 다시 켜 줄
        /// 사람이 없어진다. Animator 도 계속 돌아야 다시 나타날 때 자세가 맞다.
        /// </summary>
        private void SetArmsVisible(bool visible)
        {
            if (armsRenderers == null || armsRenderers.Length == 0)
            {
                armsRenderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < armsRenderers.Length; i++)
            {
                if (armsRenderers[i] == null)
                {
                    continue;
                }

                // IMPORTANT: **팔 살 말고는 건드리지 않는다.** 무기까지 켜 버렸더니
                // 권총을 재장전할 때 `AmmoChanged` 를 타고 여기로 들어와 산탄총도
                // 같이 켜졌다 — 장전하면 총이 바뀌는 것처럼 보이던 버그가 이것이다.
                // 총구 섬광과 탄도도 마찬가지다. 켜고 끌 주인이 따로 있는 것들이라
                // (ShowParts · WeaponVfx) 여기서 손대면 그쪽 판단을 덮어쓴다.
                if (!(armsRenderers[i] is SkinnedMeshRenderer) || armsRenderers[i].name.StartsWith("W_"))
                {
                    continue;
                }

                armsRenderers[i].enabled = visible;
            }
        }

        private void OnFired()
        {
            if (subscribed == null)
            {
                return;
            }

            // 조작 중에 방아쇠를 당겼다. 총이 나가는데 손에 없으면 안 된다.
            TakeWeaponBack();

            // 산탄총은 쏘고 펌프질까지 한 클립이라 길다.
            bool shotgun = subscribed.Kind == WeaponKind.Shotgun;
            PlayOnce(shotgun ? "Arms_ShotgunFirePump" : "Arms_PistolFire", shotgun ? 0.97f : 0.23f);
        }

        private void OnReloadingChanged(bool reloading)
        {
            if (subscribed == null)
            {
                return;
            }

            TakeWeaponBack();

            if (!reloading)
            {
                Play(idleState);
                returnToIdleAt = float.PositiveInfinity;
                return;
            }

            // 클립 길이가 아니라 **무기가 정한 재장전 시간**에 맞춰 돌려보낸다.
            // 둘이 다르면 팔이 먼저 끝나고 총은 아직 못 쏘는 상태가 된다.
            string clip = subscribed.Kind == WeaponKind.Shotgun ? "Arms_ShotgunReload" : "Arms_PistolReload";
            PlayOnce(clip, subscribed.ReloadSeconds);
        }

        private void PlayOnce(string state, float seconds)
        {
            Play(state);
            returnToIdleAt = Time.time + Mathf.Max(0.05f, seconds);
        }

        /// <summary>이름으로 부품 렌더러를 켜고 끈다.</summary>
        private void ShowParts(string[] names, bool visible)
        {
            if (armsRenderers == null || armsRenderers.Length == 0)
            {
                armsRenderers = GetComponentsInChildren<Renderer>(true);
            }

            for (int i = 0; i < armsRenderers.Length; i++)
            {
                if (armsRenderers[i] == null)
                {
                    continue;
                }

                for (int k = 0; k < names.Length; k++)
                {
                    if (armsRenderers[i].name == names[k])
                    {
                        armsRenderers[i].enabled = visible;
                    }
                }
            }
        }

        // ── 애니메이션 이벤트 수신부 ─────────────────────────────────────────
        // IMPORTANT: 이벤트는 **Animator 와 같은 오브젝트의 컴포넌트에만** 간다.
        // 이 컴포넌트가 팔 루트에 붙어 있어 여기가 받는 자리다. 클립에 심어 둔
        // 이름은 하나라도 빠지면 「has no receiver」 경고가 매 재생마다 쌓인다.
        //
        // 지금은 대부분 비어 있다. **소리와 연출을 붙일 자리를 먼저 열어 두는 것**이
        // 목적이고, WeaponAudio 가 이미 자기 타이밍표로 소리를 내고 있어 여기서
        // 또 내면 두 번 울린다. 붙일 때는 한쪽을 지운다.

        /// <summary>총이 손에 자리잡았다. 드는 동작의 끝.</summary>
        public void WeaponReady() { }

        /// <summary>든 물건이 손에 붙는 순간. 6장 연료통이 이 시점에 손에 온다.</summary>
        public void AttachProp() { }

        /// <summary>든 물건을 놓는 순간.</summary>
        public void DetachProp() { }

        public void Fire() { }

        public void Eject() { }

        public void PumpBack() { }

        public void PumpClosed() { }

        public void ShellIn() { }

        public void TubesClosed() { }

        public void MagRelease() { }

        public void MagOut() { }

        public void MagazineIn() { }

        public void SlideRelease() { }

        public void ReloadComplete() { }

        /// <summary>레버에 손이 닿았다.</summary>
        public void HandsOn() { }

        /// <summary>상호작용이 실제로 발동하는 프레임.</summary>
        public void InteractCommit() { }

        public void Died() { }

        private void Play(string state)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.CrossFadeInFixedTime(state, blendSeconds, 0);
            }
        }
    }
}
