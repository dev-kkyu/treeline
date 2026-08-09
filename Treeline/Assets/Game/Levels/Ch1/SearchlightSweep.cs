using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 서치라이트가 능선을 훑다가 <b>무언가를 붙잡고 따라간다.</b> 1장에서 처음이자
    /// 마지막으로 「그것」을 보여주는 자리다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 처음에는 빛이 한자리에 켜지고 실루엣이 그 앞을 지나갔다. 두 가지가 어색했다 —
    /// 옥상이 밝아 <b>실루엣이 켜지고 꺼지는 순간이 그대로 보였고</b>, 지나가는 것이
    /// 빛과 아무 상관이 없어 그냥 배경에서 무언가 움직인 것이 됐다.
    /// </para>
    /// <para>
    /// 빛이 따라가면 셋이 한꺼번에 풀린다. 어두우니 <b>빛 밖은 안 보이고</b>, 빛이
    /// 붙잡았으니 <b>내가 켜서 봤다</b>가 되며, 놓치는 것으로 끝나니 <b>사라지는 데
    /// 이유가 생긴다.</b>
    /// </para>
    /// <para>
    /// IMPORTANT: <b>단번에 겨누지 않는다.</b> <see cref="trackDegreesPerSecond"/> 로
    /// 각속도를 묶어 늘 조금씩 뒤처지게 한다. 정확히 따라가면 기계가 자동으로 조준하는
    /// 것으로 보이고, 이 장에서 그것을 조작하는 것은 사람이다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SearchlightSweep : MonoBehaviour
    {
        [Tooltip("따라갈 것. 꺼져 있는 동안에는 훑기만 한다.")]
        [SerializeField] private Transform target;

        [Tooltip("겨눌 높이. 발밑이 아니라 몸통을 비춰야 실루엣이 선다.")]
        [SerializeField] private float targetHeight = 3.2f;

        [Header("훑기")]
        [Tooltip("아무것도 없을 때 좌우로 훑는 폭(도).")]
        [SerializeField] private float sweepDegrees = 26f;

        [Tooltip("훑는 속도(도/초).")]
        [SerializeField] private float sweepDegreesPerSecond = 7f;

        [Tooltip("훑을 때의 기준 방향. 비우면 켜질 때의 방향을 쓴다.")]
        [SerializeField] private Vector3 restEuler = new Vector3(6f, 0f, 0f);

        [Header("따라가기")]
        [Tooltip("각속도 상한. 낮을수록 사람이 손으로 돌리는 것처럼 보인다.")]
        [SerializeField] private float trackDegreesPerSecond = 11f;

        [Tooltip("이 각도 안으로 들어오면 붙잡은 것으로 본다.")]
        [SerializeField] private float catchDegrees = 20f;

        [Header("붙잡았을 때")]
        [Tooltip("붙잡는 순간 한 번. 비워 두면 조용하다.")]
        [SerializeField] private AudioSource catchSource;

        [SerializeField] private AudioClip catchClip;

        private float sweepPhase;
        private bool caught;

        /// <summary>지금 무언가를 비추고 있는지. 연출을 잇고 싶을 때 본다.</summary>
        public bool Caught => caught;

        private void OnEnable()
        {
            // 켤 때마다 훑기부터 다시 시작한다. 장을 다시 시작해도 같은 순서로 보인다.
            sweepPhase = 0f;
            caught = false;
            transform.localRotation = Quaternion.Euler(restEuler);
        }

        private void Update()
        {
            Quaternion want;

            if (target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 at = target.position + Vector3.up * targetHeight;
                Vector3 to = at - transform.position;
                if (to.sqrMagnitude > 0.001f)
                {
                    want = Quaternion.LookRotation(to.normalized, Vector3.up);

                    if (!caught && Quaternion.Angle(transform.rotation, want) <= catchDegrees)
                    {
                        caught = true;
                        if (catchSource != null && catchClip != null)
                        {
                            catchSource.PlayOneShot(catchClip);
                        }
                    }
                }
                else
                {
                    want = transform.rotation;
                }
            }
            else
            {
                // 아무것도 없으면 좌우로 훑는다. 사인파라 끝에서 저절로 느려진다.
                sweepPhase += sweepDegreesPerSecond * Time.deltaTime;
                float yaw = Mathf.Sin(sweepPhase * Mathf.Deg2Rad) * sweepDegrees;
                want = Quaternion.Euler(restEuler) * Quaternion.Euler(0f, yaw, 0f);
                caught = false;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, want, trackDegreesPerSecond * Time.deltaTime);
        }
    }
}
