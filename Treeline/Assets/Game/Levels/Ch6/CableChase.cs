using UnityEngine;
using UnityEngine.AI;

namespace Treeline
{
    /// <summary>
    /// 삭도가 떠난 뒤, 그것이 <b>케이블을 타고 따라붙어 차체를 들이받는다.</b>
    /// </summary>
    /// <remarks>
    /// 기획의 「그것이 케이블을 붙잡고 따라 올라온다」이며, 클립 셋
    /// (<c>Grip_Cable</c> 42f → <c>Cable_Climb</c> loop → <c>Cable_Fall</c> 54f)은
    /// 이미 만들어져 있다. <b>여기가 그 클립들을 부르는 「엔딩 연출」이다.</b>
    /// <para>
    /// IMPORTANT: <see cref="Colossus"/> 에 상태를 더하지 않는다. 그쪽은 추격 ·
    /// 공격 · 밀림 · 묻힘만 알고, 케이블은 모른다. 연출용 분기를 적의 상태 기계에
    /// 넣으면 6장 전체가 그것을 알아야 한다 — 애니메이션 문서의 코드 계약이다.
    /// </para>
    /// <para>
    /// <b>스스로 떨어지지 않는다.</b> 처음에는 중간에 놓치게 만들었는데, 그러면
    /// 플레이어가 아무것도 안 해도 위험이 저절로 사라져서 마지막 몇 초의 긴장이
    /// 풀린다. 지금은 <b>차체를 따라잡아 들이받고</b>, 그 충격으로 떨어진다.
    /// </para>
    /// <para>
    /// <b>보폭은 재생 배속이 아니라 <see cref="metersPerPull"/> 이 정한다.</b> 클립
    /// 한 번의 당김은 0.85 m 인데, 그 값을 지키면서 차체를 따라잡으려면 배속을
    /// 10 배 가까이 올려야 하고 그러면 팔을 파닥거리는 작은 짐승이 된다. 대신
    /// <b>한 번의 당김이 보내는 거리를 늘리고</b> 배속은 그만큼 낮춘다. 손은
    /// 케이블 위를 미끄러지지만, 굵기가 일정한 줄이라 미끄러짐 자체는 안 보이고
    /// <b>한 번에 크게 당기는 것</b>만 보인다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CableChase : MonoBehaviour
    {
        private static readonly int GripHash = Animator.StringToHash("GripCable");
        private static readonly int FallHash = Animator.StringToHash("CableFall");

        /// <summary><c>Cable_Climb</c> 한 주기(48f · 30fps).</summary>
        private const float PullClipSeconds = 1.6f;

        [Header("무엇이 무엇을 타나")]
        [Tooltip("케이블에 매달릴 것.")]
        [SerializeField] private Colossus watcher;

        [Tooltip("케이블. 로컬 +Z 로 뻗은 상자이며 여기서 선과 길이를 읽는다.")]
        [SerializeField] private Transform cable;

        [Tooltip("따라잡을 차체.")]
        [SerializeField] private Transform car;

        [Header("붙잡기")]
        [Tooltip("케이블 위쪽 끝에서 몇 m 지점을 잡나. 철탑 대들보를 피해 잡는다.")]
        [SerializeField] private float gripOffset = 3.2f;

        [Tooltip("클립이 상정한 케이블 높이. 손이 케이블에 닿기 전까지 이 값으로 세운다.")]
        [SerializeField] private float hangDrop = 5.25f;

        [Tooltip("붙잡는 클립의 길이(42f).")]
        [SerializeField] private float gripSeconds = 1.4f;

        [Header("따라붙기")]
        [Tooltip("한 번 당길 때 나아가는 거리. 클립 그대로는 0.85 m 이고, 그 보폭은 이 덩치에 안 맞는다.")]
        [SerializeField] private float metersPerPull = 4f;

        [Tooltip("재생 배속 상한. 넘어가면 다시 파닥거린다.")]
        [SerializeField] private float maxPlaybackSpeed = 2.6f;

        [Tooltip("들이받는 순간 차체 중심과의 거리. 차체 절반 깊이보다 커야 몸이 안 파묻힌다.")]
        [SerializeField] private float hitGap = 1.6f;

        [Header("떨어지기")]
        [Tooltip("얻어맞고 뒤로 밀려나는 속도.")]
        [SerializeField] private float knockback = 3.5f;

        [Tooltip("놓치는 클립의 길이(54f). 이 뒤부터는 이쪽이 계속 떨어뜨린다.")]
        [SerializeField] private float fallClipSeconds = 1.8f;

        [Tooltip("클립이 끝난 뒤의 낙하 가속.")]
        [SerializeField] private float gravity = 18f;

        [Tooltip("이 높이 아래로 내려가면 치운다.")]
        [SerializeField] private float killY = -45f;

        [Header("소리")]
        [Tooltip("그것에게서 나는 소리. Colossus 아래에 둔다.")]
        [SerializeField] private AudioSource voice;

        [Tooltip("차체에서 나는 소리. 부딪히는 것은 차체 쪽에서 들려야 한다.")]
        [SerializeField] private AudioSource impact;

        [Tooltip("이미 있는 포효. 부딪히기 직전에 한 번 낸다.")]
        [SerializeField] private AudioSource roar;

        [SerializeField] private AudioClip gripClip;

        [Tooltip("한 번 당길 때마다 하나. 둘 이상 넣으면 번갈아 난다.")]
        [SerializeField] private AudioClip[] pullClips;

        [SerializeField] private AudioClip clangClip;
        [SerializeField] private AudioClip slamClip;
        [SerializeField] private AudioClip staggerClip;
        [SerializeField] private AudioClip fallClip;

        [Tooltip("부딪히기 몇 초 전에 포효하나.")]
        [SerializeField] private float roarBefore = 1.1f;

        [Tooltip("떨어지면서 내는 마지막 소리. 그것과 함께 멀어진다.")]
        [SerializeField] private AudioClip farewellClip;

        [Tooltip("부딪힌 뒤 몇 초에 그것을 내나.")]
        [SerializeField] private float farewellAfter = 2.2f;

        private Animator animator;
        private Transform body;
        private Transform digitsLeft;
        private Transform digitsRight;

        private Vector3 cableTop;
        private Vector3 cableDirection;
        private float cableLength;

        private Vector3 standAt;
        private float distance;
        private float startGap;
        private float chaseSeconds;
        private float gripAt = float.PositiveInfinity;
        private float chaseAt = float.PositiveInfinity;
        private float hitAt = float.PositiveInfinity;
        private float fallSpeed;
        private int lastPull;
        private int nextPullClip;
        private bool roared;
        private bool farewelled;
        private bool running;

        /// <summary>
        /// 붙잡는다. 여기서부터 <see cref="Colossus"/> 는 손을 뗀다.
        /// </summary>
        /// <param name="secondsToImpact">
        /// 지금부터 차체에 닿기까지. 연출이 시간표를 쥐고 있으므로 여기서 받는다 —
        /// 이쪽이 따로 정하면 시간표가 두 곳에 생긴다.
        /// </param>
        public void Grip(float secondsToImpact)
        {
            if (running || watcher == null || cable == null || car == null)
            {
                return;
            }

            MeasureCable();

            body = watcher.transform;
            animator = watcher.GetComponent<Animator>();

            digitsLeft = FindBone("DEF_Digits.L");
            digitsRight = FindBone("DEF_Digits.R");

            watcher.StandDown();

            NavMeshAgent agent = watcher.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            standAt = body.position;
            distance = Mathf.Clamp(gripOffset, 0f, cableLength);
            gripAt = Time.time;
            chaseAt = gripAt + gripSeconds;
            hitAt = gripAt + Mathf.Max(gripSeconds + 0.1f, secondsToImpact);
            chaseSeconds = hitAt - chaseAt;
            running = true;

            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetTrigger(GripHash);
            }

            Play(voice, gripClip);
        }

        /// <summary>
        /// 차체에 부딪혔다. 여기서 놓친다.
        /// </summary>
        /// <remarks>
        /// 소리는 <b>둘을 겹친다.</b> 금속을 때리는 소리만으로는 무엇이 부딪혔는지
        /// 모르고, 몸이 부딪히는 소리만으로는 무엇에 부딪혔는지 모른다.
        /// </remarks>
        public void Hit()
        {
            if (!running || Time.time >= hitAt + 0.5f)
            {
                return;
            }

            hitAt = Time.time;

            Play(impact, clangClip);
            Play(impact, slamClip);
            Play(voice, staggerClip);
            Play(voice, fallClip);

            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetTrigger(FallHash);
            }
        }

        /// <summary>
        /// 애니메이터가 자세를 만든 뒤에 몸을 케이블에 얹는다.
        /// </summary>
        /// <remarks>
        /// 순서가 중요하다. <c>Update</c> 에서 옮기면 그 프레임의 애니메이션이
        /// 덮어써서 손이 케이블에서 한 프레임씩 벗어난다.
        /// </remarks>
        private void LateUpdate()
        {
            if (!running)
            {
                return;
            }

            float now = Time.time;
            bool fallen = now >= hitAt;
            float sinceHit = fallen ? now - hitAt : 0f;

            // 떨어지면서 한 번 더 운다. 소리를 내는 자리가 그것과 함께 내려가므로
            // 저절로 멀어진다. **끝내 죽지 않는다**는 것이 이 적의 전부라, 화면이
            // 닫히는 동안 아래에서 이 소리가 올라와야 마지막까지 긴장이 안 풀린다.
            if (fallen && !farewelled && sinceHit >= farewellAfter)
            {
                farewelled = true;
                Play(voice, farewellClip);
            }

            if (fallen && sinceHit > fallClipSeconds)
            {
                // 클립은 아직 떨어지는 중에 끝난다. 그 뒤를 이어서 떨어뜨린다.
                fallSpeed += gravity * Time.deltaTime;
                body.position += Vector3.down * (fallSpeed * Time.deltaTime);

                if (body.position.y < killY)
                {
                    body.gameObject.SetActive(false);
                    running = false;
                }

                return;
            }

            if (fallen)
            {
                // 얻어맞았으니 차체에서 떨어져 나간다. 그대로 붙어 있으면 차체가
                // 그것을 끌고 가는 그림이 된다.
                distance -= knockback * Time.deltaTime;
            }
            else
            {
                Advance(now);
            }

            Vector3 hold = PointOnCable(distance);
            body.position = HangPosition(hold, now);
            body.rotation = Quaternion.LookRotation(Flat(cableDirection), Vector3.up);

            LockToCable(hold, LockWeight(now, fallen, sinceHit));
        }

        /// <summary>
        /// 붙잡은 뒤로는 차체와의 간격을 좁힌다. 뒤로 물러나는 프레임은 없다.
        /// </summary>
        /// <remarks>
        /// 속도를 정해 두고 미는 대신 <b>간격을 몰아간다.</b> 차체의 가속 곡선이
        /// 바뀌어도 부딪히는 시각이 안 밀리고, 「따라잡는다」가 값으로 보장된다.
        /// </remarks>
        private void Advance(float now)
        {
            if (now < chaseAt)
            {
                return;
            }

            float carAt = Along(car.position);

            if (startGap <= 0f)
            {
                startGap = Mathf.Max(hitGap, carAt - distance);
            }

            float t = Mathf.Clamp01((now - chaseAt) / Mathf.Max(0.01f, chaseSeconds));

            // 뒤로 갈수록 빨리 좁힌다. 등속으로 좁히면 「따라온다」로 끝나고,
            // 마지막에 붙는 맛이 없다.
            float gap = Mathf.Lerp(startGap, hitGap, Mathf.Pow(t, 1.35f));
            float moved = Mathf.Max(0f, carAt - gap - distance);
            distance += moved;

            Pace(moved);

            if (!roared && now >= hitAt - roarBefore)
            {
                roared = true;
                Play(roar, null);
            }
        }

        /// <summary>
        /// 나아간 만큼에 맞춰 당기는 박자를 정하고, 당길 때마다 소리를 낸다.
        /// </summary>
        /// <remarks>
        /// 배속을 거리에서 거꾸로 구하므로 <see cref="metersPerPull"/> 을 키우면
        /// <b>같은 속도에서 박자만 느려진다.</b> 그것이 곧 보폭이다.
        /// </remarks>
        private void Pace(float moved)
        {
            if (animator == null)
            {
                return;
            }

            float speed = Time.deltaTime > 0f ? moved / Time.deltaTime : 0f;
            float wanted = Mathf.Clamp(PullClipSeconds * speed / Mathf.Max(0.1f, metersPerPull),
                0.25f, maxPlaybackSpeed);

            // 프레임마다 튀면 팔이 덜덜 떨린다.
            animator.speed = Mathf.MoveTowards(animator.speed, wanted, 3f * Time.deltaTime);

            int pull = Mathf.FloorToInt(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            if (pull != lastPull)
            {
                lastPull = pull;
                PlayPull();
            }
        }

        /// <summary>
        /// 서 있던 자리에서 매달린 자리로 뛰어오른다.
        /// </summary>
        /// <remarks>
        /// 클립은 8f 에 팔을 올리기 시작해 34f 에 체중이 전부 팔에 실린다. 그 사이에
        /// 몸을 올려 주면 <b>갑판에서 케이블로 뛰어 붙는 것</b>으로 읽힌다. 처음부터
        /// 매달린 자리에 두면 1 m 남짓 공중에 뜬 채로 팔을 드는 그림이 된다.
        /// </remarks>
        private Vector3 HangPosition(Vector3 hold, float now)
        {
            Vector3 hang = hold - Vector3.up * hangDrop;

            float leap = Mathf.InverseLerp(gripSeconds * 0.19f, gripSeconds * 0.81f, now - gripAt);
            return Vector3.Lerp(standAt, hang, Mathf.SmoothStep(0f, 1f, leap));
        }

        /// <summary>
        /// 몸을 케이블 <b>선</b>에 붙인다. 이 프로젝트에 IK 는 이것뿐이다.
        /// </summary>
        /// <remarks>
        /// <c>Colossus</c> 는 Generic 리그라 <c>Animator</c> 의 IK 목표를 못 쓴다
        /// (그쪽은 Humanoid 전용이다). 손을 끌어오는 대신 <b>몸을 민다</b> — 두 손의
        /// 가운데가 케이블 선 위에 오도록 루트를 옮기므로, 팔을 비틀지 않고도 손이
        /// 줄에서 떨어지지 않는다.
        /// <para>
        /// IMPORTANT: <b>줄을 따라가는 방향은 안 고친다.</b> 그 성분까지 붙이면 한
        /// 번의 당김이 클립이 담은 0.85 m 로 도로 묶여서, 보폭을 늘린 것이 통째로
        /// 취소된다. 고치는 것은 줄에서 벗어난 만큼뿐이다.
        /// </para>
        /// </remarks>
        private void LockToCable(Vector3 hold, float weight)
        {
            if (weight <= 0f || digitsLeft == null || digitsRight == null)
            {
                return;
            }

            Vector3 hands = (digitsLeft.position + digitsRight.position) * 0.5f;
            Vector3 offset = hold - hands;
            offset -= Vector3.Dot(offset, cableDirection) * cableDirection;

            body.position += offset * weight;
        }

        /// <summary>
        /// 손이 케이블에 걸려 있는 정도. 26f 에 걸리고 놓치는 20f 에 풀린다.
        /// </summary>
        private float LockWeight(float now, bool fallen, float sinceHit)
        {
            if (fallen)
            {
                return 1f - Mathf.Clamp01(sinceHit / 0.67f);
            }

            return Mathf.Clamp01(
                Mathf.InverseLerp(gripSeconds * 0.62f, gripSeconds, now - gripAt));
        }

        /// <summary>케이블 상자에서 선을 읽는다. 위쪽 끝이 철탑이다.</summary>
        private void MeasureCable()
        {
            cableLength = cable.lossyScale.z;
            cableDirection = cable.forward;

            cableTop = cable.position - cableDirection * (cableLength * 0.5f);

            // 어느 쪽이 위인지는 씬이 정한다. 상자를 반대로 놓아도 같게 돌아간다.
            if (cableTop.y < cable.position.y + cableDirection.y * (cableLength * 0.5f))
            {
                cableDirection = -cableDirection;
                cableTop = cable.position - cableDirection * (cableLength * 0.5f);
            }
        }

        private float Along(Vector3 point)
        {
            return Vector3.Dot(point - cableTop, cableDirection);
        }

        private Vector3 PointOnCable(float along)
        {
            return cableTop + cableDirection * Mathf.Clamp(along, 0f, cableLength);
        }

        private void PlayPull()
        {
            if (voice == null || pullClips == null || pullClips.Length == 0)
            {
                return;
            }

            AudioClip clip = pullClips[nextPullClip % pullClips.Length];
            nextPullClip++;

            // 같은 소리가 같은 높이로 되풀이되면 기계가 된다.
            voice.pitch = Random.Range(0.88f, 1.02f);
            voice.PlayOneShot(clip);
            voice.pitch = 1f;
        }

        private static void Play(AudioSource source, AudioClip clip)
        {
            if (source == null)
            {
                return;
            }

            if (clip == null)
            {
                source.Play();
                return;
            }

            source.PlayOneShot(clip);
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private Transform FindBone(string boneName)
        {
            foreach (Transform candidate in watcher.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == boneName)
                {
                    return candidate;
                }
            }

            Debug.LogWarning($"[{nameof(CableChase)}] 뼈 '{boneName}' 을 못 찾았다. 손이 케이블에 안 붙는다.", this);
            return null;
        }
    }
}
