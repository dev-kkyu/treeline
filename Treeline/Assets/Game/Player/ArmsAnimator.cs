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

        [Tooltip("상태를 바꿀 때 섞는 시간(초).")]
        [SerializeField, Range(0f, 0.4f)] private float blendSeconds = 0.08f;

        private PlayerWeapon subscribed;
        private string idleState = StateIdle;
        private float returnToIdleAt = float.PositiveInfinity;
        private Renderer[] armsRenderers;
        private bool wasCarrying;

        /// <summary>
        /// 무기별 부품. <b>재부모화하지 않고 이름으로 켜고 끈다.</b>
        /// </summary>
        /// <remarks>
        /// 처음에는 빈 오브젝트로 묶어 <c>viewModel</c> 에 물렸는데, 이 부품들은
        /// FBX 프리팹 인스턴스의 자식이라 <b>밖으로 옮기면 프리팹 저장에서
        /// 되돌려진다.</b> 묶음만 빈 채로 남아 총이 통째로 사라졌다.
        /// </remarks>
        /// <remarks>
        /// IMPORTANT: 리그에 딸려 온 스킨 무기(<c>W_Pistol</c> 등)는 켜도 화면에
        /// 안 나온다 — 바인딩이 제대로 안 걸려 있다. 그래서 <b>낱개 FBX 를 손 본
        /// (<c>WeaponRoot</c>)에 그냥 물렸다.</b> 본은 애니메이션을 따라가므로
        /// 결과는 같고, 스킨 문제를 손댈 필요가 없다.
        /// </remarks>
        private static readonly string[] PistolParts = { "Held_Pistol" };

        private static readonly string[] ShotgunParts = { "Held_Shotgun" };

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (switcher == null)
            {
                switcher = GetComponentInParent<WeaponSwitcher>();
            }

            if (animator == null)
            {
                Debug.LogError($"{nameof(ArmsAnimator)}: Animator 를 못 찾았다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
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
            if (switcher != null)
            {
                switcher.Equipped -= OnEquipped;
            }

            Unsubscribe();
        }

        private void Update()
        {
            UpdateCarry();

            // 한 번짜리 동작이 끝나면 쉬기로 돌려보낸다. 안 그러면 마지막 프레임에서
            // 팔이 굳는다. 상태 시간을 보지 않고 우리가 잰 시각을 쓰는 이유는,
            // 연사하면 같은 상태를 다시 걸어 normalizedTime 이 0 으로 돌아가서다.
            if (Time.time >= returnToIdleAt)
            {
                returnToIdleAt = float.PositiveInfinity;
                Play(idleState);
            }
        }

        /// <summary>무기가 바뀌었다. 구독을 갈아타고 드는 동작을 재생한다.</summary>
        private void OnEquipped(PlayerWeapon weapon)
        {
            Unsubscribe();

            if (weapon == null)
            {
                // 아무것도 안 들었어도 팔은 남는다. 손이 사라지면 몸이 없는 시점이
                // 되어 1인칭이 깨진다 — 어느 장에서든 팔은 늘 있어야 한다.
                idleState = StateIdle;
                Play(idleState);
                SetArmsVisible(true);
                return;
            }

            subscribed = weapon;
            weapon.Fired += OnFired;
            weapon.ReloadingChanged += OnReloadingChanged;
            weapon.AmmoChanged += OnAmmoChanged;
            OnAmmoChanged();

            bool shotgun = weapon.Kind == WeaponKind.Shotgun;
            ShowParts(shotgun ? ShotgunParts : PistolParts, true);
            ShowParts(shotgun ? PistolParts : ShotgunParts, false);

            string prefix = shotgun ? "Arms_Shotgun" : "Arms_Pistol";
            idleState = prefix + "Idle";
            PlayOnce(prefix + "Equip", 0.5f);
        }

        private void Unsubscribe()
        {
            if (subscribed == null)
            {
                return;
            }

            subscribed.Fired -= OnFired;
            subscribed.ReloadingChanged -= OnReloadingChanged;
            subscribed.AmmoChanged -= OnAmmoChanged;
            subscribed = null;
        }

        /// <summary>
        /// 탄약이 바닥나면 팔도 내린다. 쏠 수 없는 총을 계속 들고 있으면 화면의
        /// 3분의 1을 쓸모없는 것이 차지하고, <b>쏠 수 있는지를 HUD 로만 알게 된다.</b>
        /// 손이 비면 그 자체가 신호다.
        /// </summary>
        private void OnAmmoChanged()
        {
            if (subscribed == null)
            {
                return;
            }

            // 탄약이 없으면 **총만** 내린다. 팔은 남는다 — 손이 사라지면 몸이
            // 없는 시점이 되어 1인칭이 깨진다. 빈손 자세로 돌려보내면
            // PlayerWeapon 이 뷰모델을 끄므로 총은 알아서 사라진다.
            bool hasAmmo = subscribed.Loaded > 0 || subscribed.Reserve > 0;
            SetArmsVisible(true);
            if (!hasAmmo && idleState != StateIdle)
            {
                idleState = StateIdle;
                Play(idleState);
            }
        }

        /// <summary>
        /// 무언가를 두 손으로 들고 있는 동안의 자세. <see cref="WeaponSwitcher"/> 가
        /// 총을 봉인하는 것과 같은 신호를 팔도 받는다 — 6장 연료통이 이것이다.
        /// </summary>
        private void UpdateCarry()
        {
            if (switcher == null)
            {
                return;
            }

            bool carrying = switcher.IsCarrying;
            if (carrying == wasCarrying)
            {
                return;
            }

            wasCarrying = carrying;
            SetArmsVisible(true);
            if (carrying)
            {
                idleState = "Arms_CarryIdle";
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
                if (armsRenderers[i] != null)
                {
                    armsRenderers[i].enabled = visible;
                }
            }
        }

        private void OnFired()
        {
            if (subscribed == null)
            {
                return;
            }

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
