using System.Collections;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 6장 탑승 연출 — <b>레버가 문을 열고, 그것이 오고, 제설포가 눕히고, 올라타면
    /// 문이 닫히며 출발한다.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 순서는 이렇다. 탑승 레버(<see cref="boardDevice"/>)가 켜지면 문이 열리고
    /// 그것에게 들킨다 — 레버 소리가 위치를 알리는 것이므로 조용히 열 수 없다.
    /// 다가오는 동안 옆의 제설포(<see cref="cannonDevice"/>)를 쏘면 포탄이 그것을
    /// 맞혀 잠시 눕는다(<see cref="Colossus.Daze"/>). 그 틈에 열린 문으로 들어서면
    /// (<see cref="OnTriggerEnter"/>) 문이 닫히고 <see cref="CableCarDeparture.Depart"/> 다.
    /// </para>
    /// <para>
    /// 이 컴포넌트는 <b>문 앞의 트리거 볼륨에 붙는다.</b> 차체 메시가 한 덩어리라
    /// 안으로 걸어 들어갈 수 없고, 들어서는 순간 시점이 차체 밖으로 넘어가므로
    /// 문턱에서 끊어도 이어져 보인다.
    /// </para>
    /// <para>
    /// 제설포를 먼저 쏴 버렸어도 막히지는 않는다. 눕는 틈 없이 타야 할 뿐이다.
    /// 한 발뿐인 것은 5장과 같다 — 아껴 쓰는 것이 이 장치의 규칙이다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Ch6Boarding : MonoBehaviour
    {
        [Header("신호")]
        [Tooltip("탑승 레버가 미는 장치. 켜지면 문이 열린다.")]
        [SerializeField] private Device boardDevice;

        [Tooltip("제설포 레버가 미는 장치. 켜지면 한 발 쏜다.")]
        [SerializeField] private Device cannonDevice;

        [Header("무엇이 움직이나")]
        [SerializeField] private CableCarDoor door;

        [SerializeField] private Colossus watcher;

        [SerializeField] private CableCarDeparture departure;

        [Header("출발 소리")]
        [Tooltip("출발 순간 한 번. cablecar-start.")]
        [SerializeField] private AudioSource startSound;

        [Tooltip("타고 가는 동안. cablecar-loop.")]
        [SerializeField] private AudioSource runSound;

        [Header("제설포")]
        [Tooltip("돌아갈 몸통. 쏘기 전에 그것 쪽으로 튼다.")]
        [SerializeField] private Transform cannonBody;

        [Tooltip("포구. 여기서 나간다.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("날아가는 포탄. 평소에는 꺼 둔다.")]
        [SerializeField] private GameObject shell;

        [Tooltip("포구에서 그것까지 걸리는 시간.")]
        [SerializeField] private float flightSeconds = 0.38f;

        [Tooltip("맞은 그것이 눕는 시간. 문까지 걸어 들어갈 만큼 준다.")]
        [SerializeField] private float dazeSeconds = 7f;

        [Tooltip("맞는 자리의 높이. 가슴께다.")]
        [SerializeField] private float hitHeight = 2.4f;

        [Header("제설포 — 보이는 것")]
        [SerializeField] private ParticleSystem impactBurst;

        [SerializeField] private Light impactLight;

        [Tooltip("착탄 섬광의 최대 세기. 유도등(6)의 열 배 안쪽.")]
        [SerializeField] private float impactPeak = 60f;

        [SerializeField] private Light muzzleLight;

        [SerializeField] private float muzzlePeak = 40f;

        [Header("제설포 — 소리")]
        [SerializeField] private AudioSource cannonSource;

        [SerializeField] private AudioClip fireClip;

        [SerializeField] private AudioClip hitClip;

        private bool armed;
        private bool boarded;

        private void Start()
        {
            // 재시작 복원. 레버까지 당긴 뒤 죽었으면 문이 열린 채로 시작해야 한다 —
            // 소리도 연출도 없이 상태만 맞춘다. 그것을 다시 부르지는 않는다.
            if (boardDevice != null && boardDevice.IsActive && door != null)
            {
                door.SnapOpen();
                armed = true;
            }
        }

        private void OnEnable()
        {
            if (boardDevice != null)
            {
                boardDevice.ActiveChanged += OnBoardRequested;
            }

            if (cannonDevice != null)
            {
                cannonDevice.ActiveChanged += OnCannonFired;
            }
        }

        private void OnDisable()
        {
            if (boardDevice != null)
            {
                boardDevice.ActiveChanged -= OnBoardRequested;
            }

            if (cannonDevice != null)
            {
                cannonDevice.ActiveChanged -= OnCannonFired;
            }
        }

        private void OnBoardRequested(bool active)
        {
            if (!active || armed)
            {
                return;
            }

            armed = true;

            if (door != null)
            {
                door.Open();
            }

            // 레버 소리가 위치를 알린다. 문이 열리는 동안 그것이 오는 것이
            // 이 장의 마지막 압박이다.
            if (watcher != null)
            {
                watcher.Alert();
            }
        }

        private void OnCannonFired(bool active)
        {
            if (!active || watcher == null)
            {
                return;
            }

            StartCoroutine(Fire());
        }

        private IEnumerator Fire()
        {
            Vector3 target = watcher.transform.position + Vector3.up * hitHeight;

            // 몸통을 그것 쪽으로 튼다. 포가 돌아가는 것이 「겨눴다」를 만든다.
            if (cannonBody != null)
            {
                Vector3 aim = target - cannonBody.position;
                aim.y = 0f;
                if (aim.sqrMagnitude > 0.001f)
                {
                    cannonBody.rotation = Quaternion.LookRotation(aim.normalized, Vector3.up);
                }
            }

            Play(fireClip);
            StartCoroutine(Flash(muzzleLight, muzzlePeak, 0.12f));

            Vector3 from = muzzle != null ? muzzle.position : transform.position;

            if (shell != null && flightSeconds > 0f)
            {
                shell.SetActive(true);
                for (float t = 0f; t < flightSeconds; t += Time.deltaTime)
                {
                    float k = t / flightSeconds;
                    Vector3 at = Vector3.Lerp(from, target, k);
                    at.y += Mathf.Sin(k * Mathf.PI) * 1.2f;
                    shell.transform.position = at;
                    yield return null;
                }

                shell.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(Mathf.Max(0f, flightSeconds));
            }

            // 맞은 자리는 발사 시점이 아니라 지금의 그것이다. 느린 상대라 크게
            // 안 벗어나지만, 헛맞는 그림은 한 프레임이라도 보이면 안 된다.
            Vector3 hit = watcher.transform.position + Vector3.up * hitHeight;

            if (impactBurst != null)
            {
                impactBurst.transform.position = hit;
                impactBurst.Play(true);
            }

            if (impactLight != null)
            {
                impactLight.transform.position = hit + Vector3.up * 0.5f;
            }

            StartCoroutine(Flash(impactLight, impactPeak, 0.3f));
            Play(hitClip);

            watcher.Daze(dazeSeconds, hit - from);
        }

        private IEnumerator Flash(Light light, float peak, float seconds)
        {
            if (light == null)
            {
                yield break;
            }

            light.enabled = true;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Clamp01(t / seconds);
                light.intensity = peak * k * k;
                yield return null;
            }

            light.enabled = false;
        }

        private void Play(AudioClip clip)
        {
            if (cannonSource != null && clip != null)
            {
                cannonSource.PlayOneShot(clip);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!armed || boarded || !other.CompareTag("Player"))
            {
                return;
            }

            boarded = true;

            // 문이 닫히는 것과 출발이 같은 순간이다. 시점이 바로 차체 밖으로
            // 넘어가므로, 첫 1초의 화면이 「문이 닫히며 미끄러져 나가는 차체」가 된다.
            if (door != null)
            {
                door.Close();
            }

            if (startSound != null)
            {
                startSound.Play();
            }

            if (runSound != null)
            {
                runSound.Play();
            }

            if (departure != null)
            {
                departure.Depart();
            }
        }
    }
}
