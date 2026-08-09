using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Treeline
{
    /// <summary>
    /// 제설포가 한 발 쏘고, 그 자리의 눈이 무너져 흘러내린다. 5장에서 막힌 길을 여는
    /// 것이 이것이고, 6장 피날레가 같은 물건을 다시 쓴다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 처음에는 레버가 <see cref="Device"/> 의 <c>disables</c> 로 눈덩이를 그냥 껐다.
    /// 기능은 맞지만 <b>한 프레임에 눈이 사라져서</b> 무슨 일이 일어났는지 읽히지
    /// 않는다. 이 장의 마지막 장치인데 가장 밋밋했다.
    /// </para>
    /// <para>
    /// 그래서 <b>원인과 결과를 눈으로 잇는다</b> — 포구가 번쩍이고, 포탄이 날아가
    /// 눈벽에 맞고, 맞은 자리부터 차례로 무너져 아래로 흘러내린다. 세 단계가 시간을
    /// 두고 이어져야 「내가 쏴서 열렸다」가 된다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>덩어리는 맞은 자리에서 가까운 것부터 무너진다.</b> 한꺼번에
    /// 내리면 눈이 아니라 판이 통째로 사라지는 것으로 보인다. <see cref="stagger"/> 가
    /// 그 시간차이고, 거리순으로 정렬하는 것은 이 컴포넌트가 한다 — 씬에서 순서를
    /// 맞춰 넣게 하면 덩어리를 하나 옮길 때마다 어긋난다.
    /// </para>
    /// <para>
    /// 무너진 덩어리는 <b>줄어들면서</b> 사라진다. 그냥 꺼면 깜빡 사라지고, 끝까지
    /// 미끄러뜨리면 아래에 눈덩이가 쌓여 길을 다시 막는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AvalancheShot : MonoBehaviour
    {
        [Tooltip("이것이 켜지면 쏜다. 레버가 켜는 장치를 물린다.")]
        [SerializeField] private Device device;

        [Header("쏘기")]
        [Tooltip("포구. 여기서 포탄이 나간다.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("맞는 자리. 눈벽 위쪽을 겨눈다.")]
        [SerializeField] private Transform impactPoint;

        [Tooltip("날아가는 포탄. 평소에는 꺼 둔다.")]
        [SerializeField] private GameObject shell;

        [Tooltip("포구에서 맞는 자리까지 걸리는 시간.")]
        [SerializeField] private float flightSeconds = 0.42f;

        [Tooltip("포탄이 그리는 호의 높이. 0 이면 직선이다.")]
        [SerializeField] private float arcHeight = 1.6f;

        [Header("무너지는 눈")]
        [Tooltip("무너질 덩어리들. 맞은 자리에서 가까운 것부터 저절로 정렬한다.")]
        [SerializeField] private Transform[] slabs;

        [Tooltip("흘러내리는 방향과 거리. 산 아래쪽으로 준다.")]
        [SerializeField] private Vector3 slideBy = new Vector3(-3f, -5f, -6f);

        [Tooltip("덩어리 하나가 흘러내려 사라지기까지.")]
        [SerializeField] private float slideSeconds = 1.9f;

        [Tooltip("옆 덩어리가 뒤따라 무너지기까지의 간격.")]
        [SerializeField] private float stagger = 0.14f;

        [Tooltip("흘러내리며 도는 정도. 눈덩이가 구르는 느낌을 낸다.")]
        [SerializeField] private float tumbleDegrees = 55f;

        [Header("보이는 것")]
        [SerializeField] private ParticleSystem muzzleBlast;
        [SerializeField] private ParticleSystem impactBurst;

        /// <remarks>
        /// IMPORTANT: <b>어두운 장면에서는 파티클보다 빛이 훨씬 잘 읽힌다.</b> 눈보라
        /// 속 흰 배경에 흰 입자를 뿌리면 안개와 구분이 안 되는데, 한 프레임 번쩍이는
        /// 빛은 주변 눈을 통째로 물들여서 「무슨 일이 일어났다」가 바로 선다.
        /// </remarks>
        [Tooltip("쏠 때 포구에서 번쩍인다.")]
        [SerializeField] private Light muzzleLight;

        [Tooltip("맞는 순간 터진다. 여운을 두고 꺼진다.")]
        [SerializeField] private Light impactLight;

        [Tooltip("번쩍임이 사그라드는 시간.")]
        [SerializeField] private float flashSeconds = 0.28f;

        /// <remarks>
        /// IMPORTANT: 이 프로젝트의 <b>유도등이 세기 6</b> 이다. 폭발이라고 수백을
        /// 주면 화면이 통째로 하얗게 날아가 무엇이 터졌는지도 안 보인다. 주변 조명의
        /// 열 배 안쪽에서 고른다.
        /// </remarks>
        [Tooltip("포구 섬광의 최대 세기.")]
        [SerializeField] private float muzzlePeak = 40f;

        [Tooltip("착탄 섬광의 최대 세기.")]
        [SerializeField] private float impactPeak = 70f;

        /// <remarks>
        /// IMPORTANT: <b>광원은 안개를 못 뚫는다.</b> 시야가 17m 인 이 장에서 점광원은
        /// 제 자리만 밝히고 멀리서는 사라진다. 유도등에서 쓴 것과 같은 방법으로,
        /// 가산 합성 카드를 빛과 <b>같이</b> 세워 안개 위에 얹는다. 카드는 커졌다
        /// 작아지며 사라진다 — 알파를 만지는 대신 크기를 0 으로 되돌리면 재질을
        /// 건드리지 않고도 깔끔하게 꺼진다.
        /// </remarks>
        [Tooltip("포구에 겹치는 발광 카드. 빛만으로는 안개를 못 뚫는다.")]
        [SerializeField] private Transform muzzleGlow;

        [Tooltip("착탄 자리에 겹치는 발광 카드.")]
        [SerializeField] private Transform impactGlow;

        [Tooltip("발광 카드가 부풀어 오르는 크기(m).")]
        [SerializeField] private float muzzleGlowScale = 3.2f;

        [SerializeField] private float impactGlowScale = 9f;

        [Tooltip("무너지는 동안 깔리는 눈보라. 알아서 켜고 끈다.")]
        [SerializeField] private ParticleSystem slideDust;

        [Header("소리")]
        [SerializeField] private AudioSource fireSource;
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private AudioSource impactSource;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private AudioClip slideClip;

        [Tooltip("각각의 크기. 1 을 넘겨도 된다 — 제설포는 이 장에서 제일 큰 소리다.")]
        [SerializeField] private float fireVolume = 1f;

        [SerializeField] private float impactVolume = 1f;

        [SerializeField] private float slideVolume = 0.9f;

        [Header("끝나고")]
        [Tooltip("눈이 다 무너진 뒤. 밀려난 잔해를 켜는 자리다.")]
        [SerializeField] private UnityEvent settled;

        private bool fired;

        private void Awake()
        {
            if (shell != null)
            {
                shell.SetActive(false);
            }

            if (slideDust != null)
            {
                slideDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            Darken(muzzleLight);
            Darken(impactLight);
            Shrink(muzzleGlow);
            Shrink(impactGlow);
        }

        private static void Shrink(Transform glow)
        {
            if (glow != null)
            {
                glow.localScale = Vector3.zero;
                glow.gameObject.SetActive(false);
            }
        }

        /// <summary>발광 카드가 부풀었다 사그라든다.</summary>
        private IEnumerator Bloom(Transform glow, float peak, float seconds)
        {
            if (glow == null)
            {
                yield break;
            }

            glow.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                // 앞의 15% 에 확 부풀고 나머지 시간에 걸쳐 줄어든다.
                float s = t < 0.15f
                    ? Mathf.Lerp(0.2f, 1f, t / 0.15f)
                    : Mathf.Lerp(1f, 0f, (t - 0.15f) / 0.85f);
                glow.localScale = Vector3.one * (peak * s);
                yield return null;
            }

            glow.localScale = Vector3.zero;
            glow.gameObject.SetActive(false);
        }

        private static void Darken(Light light)
        {
            if (light != null)
            {
                light.enabled = false;
            }
        }

        /// <summary>한 번 번쩍이고 사그라든다.</summary>
        private IEnumerator Flash(Light light, float peak, float seconds)
        {
            if (light == null)
            {
                yield break;
            }

            light.enabled = true;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / seconds);
                // 처음이 제일 밝고 빠르게 죽는다. 선형으로 줄이면 조명이 켜졌다 꺼진 것처럼 보인다.
                light.intensity = peak * (1f - t) * (1f - t);
                yield return null;
            }

            light.enabled = false;
        }

        private void Update()
        {
            if (fired || device == null || !device.IsActive)
            {
                return;
            }

            fired = true;
            StartCoroutine(Fire());
        }

        private IEnumerator Fire()
        {
            Vector3 from = muzzle != null ? muzzle.position : transform.position;
            Vector3 to = impactPoint != null ? impactPoint.position : from + transform.forward * 20f;

            Play(fireSource, fireClip, fireVolume);
            if (muzzleBlast != null)
            {
                muzzleBlast.Play(true);
            }

            StartCoroutine(Flash(muzzleLight, muzzlePeak, 0.12f));
            StartCoroutine(Bloom(muzzleGlow, muzzleGlowScale, 0.22f));

            yield return Travel(from, to);

            if (shell != null)
            {
                shell.SetActive(false);
            }

            Play(impactSource, impactClip, impactVolume);
            if (impactBurst != null)
            {
                impactBurst.transform.position = to;
                impactBurst.Play(true);
            }

            if (impactLight != null)
            {
                impactLight.transform.position = to;
            }

            if (impactGlow != null)
            {
                impactGlow.position = to;
            }

            StartCoroutine(Flash(impactLight, impactPeak, flashSeconds));
            StartCoroutine(Bloom(impactGlow, impactGlowScale, flashSeconds * 2.2f));

            // 무너지는 소리는 길게 깔린다. 맞는 소리와 겹쳐야 한 사건으로 들린다.
            if (impactSource != null && slideClip != null)
            {
                impactSource.PlayOneShot(slideClip, slideVolume);
            }

            if (slideDust != null)
            {
                slideDust.transform.position = to;
                slideDust.Play(true);
            }

            yield return Collapse(to);

            if (slideDust != null)
            {
                slideDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            settled?.Invoke();
        }

        /// <summary>포탄이 호를 그리며 날아간다.</summary>
        private IEnumerator Travel(Vector3 from, Vector3 to)
        {
            if (shell == null || flightSeconds <= 0f)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, flightSeconds));
                yield break;
            }

            shell.SetActive(true);
            float elapsed = 0f;
            Vector3 previous = from;

            while (elapsed < flightSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightSeconds);
                Vector3 at = Vector3.Lerp(from, to, t);
                at.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

                shell.transform.position = at;
                Vector3 heading = at - previous;
                if (heading.sqrMagnitude > 0.0001f)
                {
                    shell.transform.rotation = Quaternion.LookRotation(heading.normalized, Vector3.up);
                }

                previous = at;
                yield return null;
            }
        }

        /// <summary>맞은 자리에서 가까운 덩어리부터 차례로 흘러내린다.</summary>
        private IEnumerator Collapse(Vector3 hit)
        {
            if (slabs == null || slabs.Length == 0)
            {
                yield break;
            }

            Transform[] order = SortedByDistance(hit);

            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] != null)
                {
                    StartCoroutine(SlideAway(order[i]));
                }

                if (stagger > 0f)
                {
                    yield return new WaitForSeconds(stagger);
                }
            }

            // 마지막으로 출발한 덩어리가 다 내려갈 때까지 기다린다.
            yield return new WaitForSeconds(slideSeconds);
        }

        private Transform[] SortedByDistance(Vector3 hit)
        {
            var order = new Transform[slabs.Length];
            System.Array.Copy(slabs, order, slabs.Length);

            System.Array.Sort(order, delegate (Transform a, Transform b)
            {
                if (a == null)
                {
                    return b == null ? 0 : 1;
                }

                if (b == null)
                {
                    return -1;
                }

                float da = (a.position - hit).sqrMagnitude;
                float db = (b.position - hit).sqrMagnitude;
                return da.CompareTo(db);
            });

            return order;
        }

        /// <summary>한 덩어리가 흘러내리며 줄어들다 사라진다.</summary>
        private IEnumerator SlideAway(Transform slab)
        {
            Vector3 from = slab.position;
            Quaternion spin = slab.rotation;
            Vector3 scale = slab.localScale;

            // 덩어리마다 조금씩 다르게 굴러야 눈으로 보인다. 자리에서 값을 뽑으므로
            // 다시 시작해도 같은 모양으로 무너진다.
            float seed = Mathf.Abs(from.x * 7.13f + from.z * 3.71f);
            Vector3 skew = new Vector3(
                Mathf.Sin(seed) * 1.8f,
                0f,
                Mathf.Cos(seed * 1.7f) * 1.4f);
            Vector3 axis = new Vector3(1f, Mathf.Sin(seed * 2.3f), Mathf.Cos(seed)).normalized;
            float degrees = tumbleDegrees * (0.6f + Mathf.Abs(Mathf.Sin(seed * 0.9f)) * 0.8f);

            // 콜라이더는 무너지기 시작할 때 바로 뺀다. 눈이 흘러가는 동안 플레이어가
            // 그 위에 올라타거나 밀려나면 안 된다.
            foreach (Collider col in slab.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            float elapsed = 0f;
            while (elapsed < slideSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideSeconds);

                // 처음에는 천천히 떨어지다 가속한다. 눈사태는 그렇게 움직인다.
                float fall = t * t;
                slab.position = from + (slideBy + skew) * fall;
                slab.rotation = spin * Quaternion.AngleAxis(degrees * fall, axis);

                // 끝의 40% 구간에서 줄어들며 사라진다. 그냥 끄면 깜빡 사라진다.
                float shrink = t < 0.6f ? 1f : Mathf.InverseLerp(1f, 0.6f, t);
                slab.localScale = scale * Mathf.Max(0.01f, shrink);

                yield return null;
            }

            slab.gameObject.SetActive(false);
        }

        private static void Play(AudioSource source, AudioClip clip, float volume)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
