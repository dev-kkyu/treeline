using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>밟고 있는 바닥의 재질. 어떤 발소리를 낼지 고르는 유일한 기준이다.</summary>
    public enum FootstepSurface
    {
        Concrete = 0,
        MetalGrate = 1,
        Snow = 2,
        Wood = 3,
    }

    /// <summary>
    /// 발소리. 시간이 아니라 **이동한 거리**로 낸다.
    /// </summary>
    /// <remarks>
    /// 거리로 재면 걷기 · 달리기 · 웅크리기의 발소리 간격이 속도에서 저절로 따라온다.
    /// 타이머로 하면 속도를 바꿀 때마다 간격도 같이 고쳐야 하고, 벽에 붙어 제자리걸음
    /// 할 때도 소리가 난다.
    ///
    /// 밟고 있는 바닥이 무엇인지는 **레벨을 만든 쪽만 안다.** 회색 박스에는 재질이
    /// 없으므로 장마다 기본 표면 하나를 정해 두고, 씬에 놓인 <c>Player</c> 인스턴스에서
    /// <see cref="surface"/> 를 덮어쓴다. 한 장 안에서 갈라야 할 때가 오면 그때 표면
    /// 판정을 만든다.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        /// <summary>표면 하나와 그 소리들. 셋을 번갈아 내야 반복이 덜 들린다.</summary>
        [Serializable]
        private sealed class SurfaceClips
        {
            public FootstepSurface surface;
            public AudioClip[] clips = Array.Empty<AudioClip>();
        }

        [SerializeField] private PlayerMotor motor;

        [SerializeField] private AudioSource source;

        [Tooltip("이 장의 바닥 재질. 씬에 놓을 때 장에 맞게 덮어쓴다.")]
        [SerializeField] private FootstepSurface surface = FootstepSurface.Concrete;

        [SerializeField] private SurfaceClips[] library = Array.Empty<SurfaceClips>();

        [Header("보폭 (m)")]
        [Tooltip("걸을 때 한 걸음의 거리.")]
        [SerializeField] private float walkStride = 0.85f;

        [Tooltip("달릴 때. 보폭이 넓어도 속도가 더 빨라 간격은 짧아진다.")]
        [SerializeField] private float sprintStride = 1.15f;

        [Tooltip("웅크렸을 때. 발을 끌듯 자주 딛는다.")]
        [SerializeField] private float crouchStride = 0.7f;

        [Header("크기")]
        [SerializeField, Range(0f, 1f)] private float walkVolume = 0.6f;

        [SerializeField, Range(0f, 1f)] private float sprintVolume = 0.85f;

        [Tooltip("숨을 때 쓰는 동작이라 작아야 한다.")]
        [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.25f;

        [SerializeField, Range(0f, 1f)] private float landVolume = 0.9f;

        [Tooltip("같은 소리가 이어져도 덜 티나게 음높이를 흔든다.")]
        [SerializeField, Range(0f, 0.3f)] private float pitchJitter = 0.06f;

        [Tooltip("이만큼 떠 있었어야 착지음을 낸다. 계단을 내려갈 때 매 칸 울리는 것을 막는다.")]
        [SerializeField] private float minAirTimeForLanding = 0.2f;

        [Tooltip("도약 순간의 음량. 착지보다 작다 — 미는 소리가 떨어지는 소리보다 작다.")]
        [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.45f;

        [Tooltip("도약음의 피치 배수. 1 보다 낮춰 발을 끌며 미는 소리로 만든다.")]
        [SerializeField, Range(0.5f, 1f)] private float jumpPitchScale = 0.78f;

        private CharacterController controller;
        private Vector3 lastPosition;
        private float travelled;
        private float airTime;
        private bool wasGrounded = true;
        private int lastClipIndex = -1;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            if (source == null)
            {
                Debug.LogError($"{nameof(PlayerFootsteps)}: {nameof(AudioSource)} 가 없다.", this);
                enabled = false;
                return;
            }

            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            if (motor != null)
            {
                motor.Jumped += OnJumped;
            }
        }

        private void OnDisable()
        {
            if (motor != null)
            {
                motor.Jumped -= OnJumped;
            }
        }

        /// <summary>
        /// 도약음. 새 클립을 만들지 않고 <b>같은 표면의 발소리를 낮은 피치로</b> 낸다.
        /// </summary>
        /// <remarks>
        /// 발이 바닥을 미는 소리는 밟는 소리와 같은 재질에서 난다. 피치를 내리고
        /// 음량을 줄이면 「밟았다」가 「밀었다」로 읽힌다. 클립을 따로 뽑는 것보다
        /// 싸고, 표면이 바뀌면 도약음도 저절로 따라온다.
        /// </remarks>
        private void OnJumped()
        {
            Play(jumpVolume, jumpPitchScale);
        }

        private void Update()
        {
            // 실려 가는 동안은 걸음이 아니다. 삭도에 올라타면 콜라이더를 끄고 차체에
            // 붙여 옮기는데, 그 이동이 그대로 쌓이면 가만히 서서 걷는 소리가 난다.
            // 태우는 쪽마다 발소리를 따로 꺼주게 하지 않고 여기서 한 번 막는다.
            if (!controller.enabled)
            {
                lastPosition = transform.position;
                travelled = 0f;
                airTime = 0f;
                wasGrounded = true;
                return;
            }

            bool grounded = controller.isGrounded;

            // 수평 이동만 센다. 떨어지는 동안 쌓이면 착지하자마자 한 걸음이 터진다.
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;

            if (!grounded)
            {
                airTime += Time.deltaTime;
                wasGrounded = false;
                return;
            }

            if (!wasGrounded)
            {
                wasGrounded = true;

                if (airTime >= minAirTimeForLanding)
                {
                    Play(landVolume);
                    // 착지 직후에 바로 한 걸음이 또 나오지 않게 되감는다.
                    travelled = 0f;
                }

                airTime = 0f;
                return;
            }

            travelled += delta.magnitude;

            float stride = motor == null ? walkStride
                : motor.IsCrouching ? crouchStride
                : motor.IsSprinting ? sprintStride
                : walkStride;

            if (stride <= 0f || travelled < stride)
            {
                return;
            }

            travelled -= stride;

            float volume = motor == null ? walkVolume
                : motor.IsCrouching ? crouchVolume
                : motor.IsSprinting ? sprintVolume
                : walkVolume;

            Play(volume);
        }

        private void Play(float volume)
        {
            Play(volume, 1f);
        }

        private void Play(float volume, float pitchScale)
        {
            AudioClip[] clips = Find(surface);
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            int index = Pick(clips.Length);
            AudioClip clip = clips[index];
            if (clip == null)
            {
                return;
            }

            source.pitch = (1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter)) * pitchScale;
            source.PlayOneShot(clip, volume);
        }

        /// <summary>바로 앞에 낸 것을 빼고 고른다. 같은 소리가 두 번 이어지면 티가 난다.</summary>
        private int Pick(int count)
        {
            if (count <= 1)
            {
                lastClipIndex = 0;
                return 0;
            }

            // 첫 소리에는 뺄 것이 없다. 이 분기가 없으면 0 번이 영영 안 나온다.
            if (lastClipIndex < 0)
            {
                lastClipIndex = UnityEngine.Random.Range(0, count);
                return lastClipIndex;
            }

            int index = UnityEngine.Random.Range(0, count - 1);
            if (index >= lastClipIndex)
            {
                index++;
            }

            lastClipIndex = index;
            return index;
        }

        private AudioClip[] Find(FootstepSurface wanted)
        {
            for (int i = 0; i < library.Length; i++)
            {
                if (library[i] != null && library[i].surface == wanted)
                {
                    return library[i].clips;
                }
            }

            return null;
        }
    }
}
