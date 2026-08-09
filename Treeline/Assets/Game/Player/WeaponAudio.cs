using System;
using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 총 한 자루의 소리. 권총과 산탄총이 <b>같은 컴포넌트를 클립만 바꿔</b> 쓴다.
    /// </summary>
    /// <remarks>
    /// <see cref="PlayerWeapon"/> 과 같은 원칙이다 — 차이가 값뿐이면 값만 다르게 둔다.
    /// 권총은 발사 뒤 탄피가 떨어지고, 산탄총은 발사 뒤 펌프가 왕복한다. 그 차이는
    /// <see cref="fireBeats"/> 에 든 클립과 시각의 차이일 뿐이다.
    ///
    /// <para>
    /// IMPORTANT: 재장전과 발사는 소리 <b>하나</b>가 아니라 <b>여러 박자</b>다. 탄창이
    /// 빠지고, 새 탄창이 들어가고, 슬라이드가 당겨진다. 그 시각은 팔 애니메이션 명세
    /// (<c>docs/character-sheets/mockups/codex/final/animation/06-pistol.md</c> ·
    /// <c>07-shotgun.md</c>)의 이벤트 프레임에서 그대로 가져왔다.
    /// </para>
    /// <para>
    /// 팔 클립이 아직 Unity 에 없으므로 애니메이션 이벤트가 아니라 <b>시간</b>으로 낸다.
    /// FBX 가 들어오면 같은 프레임에 이벤트가 붙고, 그때 박자표를 이벤트 호출로 바꾸면
    /// 된다 — 소리와 클립이 이미 같은 프레임을 가리키고 있어 옮기는 값이 없다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerWeapon))]
    public sealed class WeaponAudio : MonoBehaviour
    {
        /// <summary>재장전 도중의 한 박자. 시각은 재장전 전체 길이에 대한 비율이다.</summary>
        /// <remarks>
        /// 초가 아니라 비율인 이유는 <see cref="PlayerWeapon.ReloadSeconds"/> 가
        /// 인스펙터 값이기 때문이다. 명세의 프레임도 그 길이에서 나온 것이라,
        /// 비율로 두면 재장전 시간을 조절해도 박자가 같은 자리에 남는다.
        /// </remarks>
        [Serializable]
        private sealed class ReloadBeat
        {
            public AudioClip clip;

            [Range(0f, 1f)]
            [Tooltip("재장전 전체 길이에 대한 비율.")]
            public float at;
        }

        /// <summary>발사 뒤에 이어지는 한 박자. 시각은 발사 순간부터의 초다.</summary>
        /// <remarks>
        /// 이쪽은 비율이 아니다. 팔의 발사 클립 길이는 연사 속도와 무관하게 고정이다.
        /// </remarks>
        [Serializable]
        private sealed class FireBeat
        {
            public AudioClip clip;

            [Tooltip("발사 순간부터 몇 초 뒤인가.")]
            public float at;
        }

        [SerializeField] private PlayerWeapon weapon;

        [SerializeField] private WeaponSwitcher switcher;

        [Header("소리")]
        [SerializeField] private AudioClip fire;

        [Tooltip("탄창이 빈 채로 당겼을 때.")]
        [SerializeField] private AudioClip dryFire;

        [Tooltip("이 총을 손에 들 때.")]
        [SerializeField] private AudioClip equip;

        [Tooltip("벽이나 바닥에 맞았을 때. 맞은 자리에서 난다.")]
        [SerializeField] private AudioClip impact;

        [Tooltip("금속에 맞았을 때. 비우면 위 소리를 쓴다.")]
        [SerializeField] private AudioClip impactMetal;

        [Tooltip("이 레이어에 맞으면 금속으로 친다.")]
        [SerializeField] private LayerMask metalLayers;

        [Tooltip("맞은 것의 이름이나 재질 이름에 이 조각이 들어 있으면 금속으로 친다.")]
        [SerializeField] private string[] metalNameHints = { "metal", "grate", "steel", "pipe", "drum", "shutter", "catwalk", "handrail" };

        [Tooltip("발사 뒤에 이어지는 것. 권총은 탄피, 산탄총은 펌프 왕복이다.")]
        [SerializeField] private FireBeat[] fireBeats = Array.Empty<FireBeat>();

        [Tooltip("재장전 도중의 것. 탄창 · 슬라이드 · 탄 · 펌프.")]
        [SerializeField] private ReloadBeat[] reloadBeats = Array.Empty<ReloadBeat>();

        [Header("크기")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Tooltip("탄착음 크기. 맞은 자리에서 나므로 거리에 따라 더 줄어든다.")]
        [SerializeField, Range(0f, 1f)] private float impactVolume = 0.8f;

        private AudioSource source;
        private Coroutine fireRoutine;
        private Coroutine reloadRoutine;

        private void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponent<PlayerWeapon>();
            }

            if (switcher == null)
            {
                switcher = GetComponentInParent<WeaponSwitcher>();
            }

            // 1 인칭에서 내 손의 총은 리스너와 같은 자리에 있다. spatialBlend 를 두면
            // 총이 화면 오른쪽에 있다는 이유로 총성이 오른쪽에서만 난다.
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            weapon.Fired += OnFired;
            weapon.DryFired += OnDryFired;
            weapon.Impacted += OnImpacted;
            weapon.ReloadingChanged += OnReloadingChanged;

            if (switcher != null)
            {
                switcher.Equipped += OnEquipped;
            }
        }

        private void OnDisable()
        {
            weapon.Fired -= OnFired;
            weapon.DryFired -= OnDryFired;
            weapon.Impacted -= OnImpacted;
            weapon.ReloadingChanged -= OnReloadingChanged;

            if (switcher != null)
            {
                switcher.Equipped -= OnEquipped;
            }

            StopSequences();
        }

        private void OnFired()
        {
            Play(fire);

            if (fireBeats.Length > 0)
            {
                if (fireRoutine != null)
                {
                    // 연사 중이면 앞선 발의 뒷소리를 버린다. 산탄총은 발사 간격이
                    // 1.1 초라 겹칠 일이 없지만, 권총은 0.5 초라 탄피가 밀린다.
                    StopCoroutine(fireRoutine);
                }

                fireRoutine = StartCoroutine(PlayFireBeats());
            }
        }

        private void OnDryFired()
        {
            Play(dryFire);
        }

        private void OnImpacted(RaycastHit hit)
        {
            AudioClip clip = SoundsMetal(hit) && impactMetal != null ? impactMetal : impact;
            if (clip == null)
            {
                return;
            }

            // 맞은 자리에서 나야 한다. 총구에서 내면 40 m 앞 벽에 맞은 것과 코앞에
            // 맞은 것이 같은 소리가 된다.
            AudioSource.PlayClipAtPoint(clip, hit.point, impactVolume);
        }

        /// <summary>
        /// 맞은 것이 금속인가. <b>레이어를 먼저 보고, 안 되면 이름으로 짐작한다.</b>
        /// </summary>
        /// <remarks>
        /// IMPORTANT: 이름으로 짐작하는 것은 임시방편이다. 표면 종류를 제대로 두려면
        /// 발소리처럼 표면 컴포넌트가 있어야 하는데(<see cref="PlayerFootsteps"/>),
        /// 그건 레벨 담당이 바닥마다 붙이는 물건이라 벽 · 난간 · 드럼통까지 넓히는
        /// 것은 상의가 먼저다. 그때까지는 <b>레이어를 지정해 두면 그쪽이 이긴다.</b>
        /// <para>
        /// 짐작이 틀려도 콘크리트 소리가 날 뿐 조용해지지는 않는다. 갱도는 벽이
        /// 콘크리트이고 통로 · 난간 · 드럼통이 금속이라, 이름 조각 몇 개로 대부분
        /// 갈린다.
        /// </para>
        /// </remarks>
        private bool SoundsMetal(RaycastHit hit)
        {
            if (impactMetal == null)
            {
                return false;
            }

            if ((metalLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                return true;
            }

            if (metalNameHints == null || metalNameHints.Length == 0)
            {
                return false;
            }

            string name = hit.collider.name;
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                name += " " + renderer.sharedMaterial.name;
            }

            name = name.ToLowerInvariant();

            for (int i = 0; i < metalNameHints.Length; i++)
            {
                string hint = metalNameHints[i];
                if (!string.IsNullOrEmpty(hint) && name.Contains(hint))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnReloadingChanged(bool reloading)
        {
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }

            // 끝났을 때도 여기로 온다. 마지막 박자가 재장전 길이보다 앞에 있으므로
            // 여기서 멈춰도 잃는 소리가 없고, 중간에 끊긴 경우는 남은 박자를 버려야 한다.
            if (reloading && reloadBeats.Length > 0)
            {
                reloadRoutine = StartCoroutine(PlayReloadBeats());
            }
        }

        private void OnEquipped(PlayerWeapon equippedWeapon)
        {
            if (equippedWeapon == weapon)
            {
                Play(equip);
                return;
            }

            // 다른 총으로 바뀌었다. 이 총의 남은 뒷소리는 손에 없는 총의 소리다.
            StopSequences();
        }

        private IEnumerator PlayFireBeats()
        {
            float start = Time.time;

            for (int i = 0; i < fireBeats.Length; i++)
            {
                FireBeat beat = fireBeats[i];
                if (beat == null)
                {
                    continue;
                }

                float wait = beat.at - (Time.time - start);
                if (wait > 0f)
                {
                    yield return new WaitForSeconds(wait);
                }

                Play(beat.clip);
            }

            fireRoutine = null;
        }

        private IEnumerator PlayReloadBeats()
        {
            float total = Mathf.Max(0.01f, weapon.ReloadSeconds);
            float start = Time.time;

            for (int i = 0; i < reloadBeats.Length; i++)
            {
                ReloadBeat beat = reloadBeats[i];
                if (beat == null)
                {
                    continue;
                }

                float wait = beat.at * total - (Time.time - start);
                if (wait > 0f)
                {
                    yield return new WaitForSeconds(wait);
                }

                Play(beat.clip);
            }

            reloadRoutine = null;
        }

        private void StopSequences()
        {
            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }

            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
