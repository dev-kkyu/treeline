using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 총을 쐈다는 것을 눈으로 보여준다 — 총구 섬광 · 탄도 · 화면 흔들림.
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

        [Header("탄도")]
        [Tooltip("총구에서 착탄점까지 긋는 선.")]
        [SerializeField] private LineRenderer tracer;

        [Tooltip("선이 남아 있는 시간(초).")]
        [SerializeField, Range(0.02f, 0.5f)] private float tracerSeconds = 0.085f;

        [Tooltip("선의 색. 빨간 예광탄이 기본이다.")]
        [SerializeField] private Color tracerColor = new Color(1f, 0.12f, 0.06f, 1f);

        [Tooltip("선의 굵기(m). 총구 쪽이 굵고 끝이 가늘다.")]
        [SerializeField] private float tracerWidth = 0.035f;

        [Header("화면 흔들림")]
        [Tooltip("발사 한 번의 흔들림 각도. 반동과 별개로 화면 전체가 떤다.")]
        [SerializeField] private float shakeDegrees = 0.55f;

        [SerializeField, Range(0.02f, 0.5f)] private float shakeSeconds = 0.13f;

        private float flashOffAt = float.NegativeInfinity;
        private float tracerOffAt = float.NegativeInfinity;

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

            if (tracer != null)
            {
                tracer.enabled = false;
                tracer.positionCount = 2;
                tracer.useWorldSpace = true;
                tracer.widthMultiplier = 1f;
                tracer.startWidth = tracerWidth;
                tracer.endWidth = tracerWidth * 0.35f;
                tracer.startColor = tracerColor;
                tracer.endColor = new Color(tracerColor.r, tracerColor.g, tracerColor.b, 0f);
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

            // 총을 바꾸는 순간 꺼져야 한다. 남겨 두면 다음에 꺼낼 때까지 선이 떠 있다.
            if (tracer != null)
            {
                tracer.enabled = false;
            }

            if (flashLight != null)
            {
                flashLight.enabled = false;
            }
        }

        private void Update()
        {
            float now = Time.time;

            if (flashLight != null && flashLight.enabled && now >= flashOffAt)
            {
                flashLight.enabled = false;
            }

            if (tracer != null && tracer.enabled)
            {
                if (now >= tracerOffAt)
                {
                    tracer.enabled = false;
                }
                else
                {
                    // 남은 시간만큼 옅어진다. 툭 사라지면 선이 있었는지도 모른다.
                    float left = Mathf.Clamp01((tracerOffAt - now) / Mathf.Max(0.001f, tracerSeconds));
                    Color head = tracerColor;
                    head.a = left;
                    tracer.startColor = head;
                    Color tail = head;
                    tail.a = left * 0.15f;
                    tracer.endColor = tail;
                }
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

        /// <summary>탄 하나가 지나간 선. 산탄은 여러 번 오므로 마지막 것이 남는다.</summary>
        private void OnShotFired(Vector3 origin, Vector3 end)
        {
            if (tracer == null)
            {
                return;
            }

            // 총구가 카메라에 붙어 있으면 선이 화면 한가운데 점으로만 보인다.
            // muzzle 이 지정돼 있으면 그쪽에서 긋는다.
            Vector3 from = muzzle != null ? muzzle.position : origin;
            tracer.SetPosition(0, from);
            tracer.SetPosition(1, end);
            tracer.startColor = tracerColor;
            tracer.enabled = true;
            tracerOffAt = Time.time + tracerSeconds;
        }
    }
}
