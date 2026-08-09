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
                idleState = StateIdle;
                Play(idleState);
                return;
            }

            subscribed = weapon;
            weapon.Fired += OnFired;
            weapon.ReloadingChanged += OnReloadingChanged;

            string prefix = weapon.Kind == WeaponKind.Shotgun ? "Arms_Shotgun" : "Arms_Pistol";
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
            subscribed = null;
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

        private void Play(string state)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.CrossFadeInFixedTime(state, blendSeconds, 0);
            }
        }
    }
}
