using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 총을 쐈다는 것을 눈으로 보여준다 — 총구 섬광 · 예광탄 · 화면 흔들림.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="PlayerWeapon"/> 을 고치지 않고 이벤트만 구독한다.
    /// <see cref="WeaponAudio"/> 와 같은 모양이고, 이유도 같다 — 총이 무엇을
    /// 하는지와 그것이 어떻게 보이는지는 서로 몰라도 된다. 연출을 통째로 빼도
    /// 사격은 그대로 돌아간다.
    /// </para>
    /// <para>
    /// IMPORTANT: 이 게임은 어둡다. 총구 섬광은 장식이 아니라 <b>한 프레임 동안
    /// 통로 전체를 비추는 조명</b>이고, 기획이 3장에 적어 둔 「어둠 속에서 쏘면
    /// 저 앞에 몇 마리가 더 있는지 보인다」가 이것이다. 그래서 파티클과 함께
    /// 실제 <see cref="Light"/> 를 한 프레임 켠다.
    /// </para>
    /// <para>
    /// 날아가는 탄 자체는 <see cref="ShotTracerVfx"/> 가 그린다. 총마다 하나씩 둘 것이
    /// 아니라 화면에 한 벌이면 되고, 산탄 여덟 발이 같은 풀에서 나가야 다발로 보인다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WeaponVfx : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("비우면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private PlayerWeapon weapon;

        [Tooltip("섬광과 탄도가 시작되는 자리. 비우면 무기의 muzzle 을 쓴다.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("화면을 흔들 대상. 비우면 부모에서 찾는다.")]
        [SerializeField] private PlayerLook look;

        [Header("총구 섬광")]
        [Tooltip("한 발마다 재생할 파티클. 비워도 된다.")]
        [SerializeField] private ParticleSystem muzzleFlash;

        [Tooltip("섬광이 주변을 비추는 빛. 비우면 만들지 않는다.")]
        [SerializeField] private Light flashLight;

        [Tooltip("빛의 세기와 사거리.")]
        [SerializeField] private float flashIntensity = 28f;

        [SerializeField] private float flashRange = 22f;

        [Tooltip("빛이 켜져 있는 시간(초). 길면 조명등이 되고 짧으면 안 보인다.")]
        [SerializeField, Range(0.01f, 0.2f)] private float flashSeconds = 0.055f;

        [Header("화면 흔들림")]
        [Tooltip("발사 한 번의 흔들림 각도. 반동과 별개로 화면 전체가 떤다.")]
        [SerializeField] private float shakeDegrees = 0.55f;

        [SerializeField, Range(0.02f, 0.5f)] private float shakeSeconds = 0.13f;

        private float flashOffAt = float.NegativeInfinity;

        private void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponent<PlayerWeapon>();
            }

            if (weapon == null)
            {
                Debug.LogError($"{nameof(WeaponVfx)}: {nameof(weapon)} 을 찾지 못했다.", this);
                enabled = false;
                return;
            }

            if (look == null)
            {
                look = GetComponentInParent<PlayerLook>();
            }

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }

        }

        private void OnEnable()
        {
            if (weapon == null)
            {
                return;
            }

            weapon.Fired += OnFired;
            weapon.ShotFired += OnShotFired;
        }

        private void OnDisable()
        {
            if (weapon == null)
            {
                return;
            }

            weapon.Fired -= OnFired;
            weapon.ShotFired -= OnShotFired;

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }
        }

        private void Update()
        {
            if (flashLight != null && flashLight.enabled && Time.time >= flashOffAt)
            {
                flashLight.enabled = false;
            }
        }

        /// <summary>한 발 나갔다. 섬광과 흔들림은 산탄이어도 한 번이다.</summary>
        private void OnFired()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play(true);
            }

            if (flashLight != null)
            {
                flashLight.intensity = flashIntensity;
                flashLight.range = flashRange;
                flashLight.enabled = true;
                flashOffAt = Time.time + flashSeconds;
            }

            if (look != null)
            {
                look.Shake(shakeDegrees, shakeSeconds);
            }
        }

        /// <summary>탄 하나가 지나갔다. 산탄은 탄마다 오므로 그만큼 날아간다.</summary>
        private void OnShotFired(Vector3 origin, Vector3 end)
        {
            // 총구가 카메라에 붙어 있으면 화면 한가운데 점으로만 보인다.
            // muzzle 이 지정돼 있으면 그쪽에서 내보낸다.
            Vector3 from = muzzle != null ? muzzle.position : origin;
            ShotTracerVfx.Spawn(from, end);
        }
    }
}
