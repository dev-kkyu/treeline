using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 타이틀 배경의 그것. 안개 속에서 <b>잠깐 실루엣만</b> 드러났다 사라진다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 계속 서 있으면 배경 장식이 되고, 한 번만 나오면 못 보고 지나간다. 그래서
    /// 주기로 돌린다 — 오래 숨어 있다가 짧게 드러난다. <b>드러나 있는 시간이
    /// 숨어 있는 시간보다 훨씬 짧아야</b> 본 사람이 「방금 뭐였지」 하게 된다.
    /// </para>
    /// <para>
    /// 사라지는 방법이 반투명이 아니라 <b>색을 안개색으로 미는 것</b>이다.
    /// 반투명으로 하면 뒤의 나무가 몸을 통과해 비쳐서 유령이 되고, 정렬 문제도
    /// 생긴다. 안개색으로 밀면 <b>거리에 묻힌 것</b>으로 읽힌다 — 이 장면에서
    /// 원하는 것이 그쪽이다.
    /// </para>
    /// <para>
    /// 소리는 <c>colossus-distant-loop</c> 을 계속 깔고, 드러나는 순간에만
    /// <c>colossus-roar</c> 를 얹는다. 앞의 것은 기획이 「1 장 창밖 — 실물 없이
    /// 실루엣만」 용으로 만들어 둔 클립이라 여기 그대로 맞는다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TitleApparition : MonoBehaviour
    {
        [Header("무엇을 보였다 감추나")]
        [SerializeField] private Renderer silhouette;

        [Tooltip("드러났을 때의 색. 하늘보다 어두워야 실루엣으로 읽힌다.")]
        [SerializeField] private Color visibleColor = new Color(0.012f, 0.016f, 0.022f);

        [Tooltip("숨었을 때의 색. 씬 안개색과 같아야 완전히 묻힌다.")]
        [SerializeField] private Color hiddenColor = new Color(0.055f, 0.075f, 0.100f);

        [Header("주기(초)")]
        [SerializeField] private float hiddenSeconds = 17f;
        [SerializeField] private float fadeInSeconds = 3.5f;
        [SerializeField] private float heldSeconds = 4.5f;
        [SerializeField] private float fadeOutSeconds = 5f;

        [Header("소리")]
        [Tooltip("계속 깔리는 먼 소리.")]
        [SerializeField] private AudioSource distantLoop;

        [Tooltip("드러나는 순간 한 번.")]
        [SerializeField] private AudioSource roarSource;

        [SerializeField] private AudioClip roarClip;

        [Tooltip("드러나기 시작하고 이만큼 뒤에 운다. 0 이면 소리가 형체보다 앞선다.")]
        [SerializeField] private float roarDelay = 1.2f;

        [Header("애니메이션")]
        [Tooltip("드러날 때 Roar 트리거를 보낸다. 비워도 된다.")]
        [SerializeField] private Animator animator;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int RoarId = Animator.StringToHash("Roar");

        private MaterialPropertyBlock block;
        private float cycleStart;
        private bool roared;

        private float CycleLength => hiddenSeconds + fadeInSeconds + heldSeconds + fadeOutSeconds;

        private void Awake()
        {
            block = new MaterialPropertyBlock();

            if (silhouette == null)
            {
                silhouette = GetComponentInChildren<Renderer>();
            }

            if (distantLoop != null)
            {
                distantLoop.loop = true;
                if (!distantLoop.isPlaying)
                {
                    distantLoop.Play();
                }
            }

            // 첫 등장이 화면을 켜자마자 오면 연출이 아니라 알림이 된다.
            cycleStart = Time.time - hiddenSeconds * 0.35f;
            Apply(0f);
        }

        private void Update()
        {
            if (silhouette == null)
            {
                return;
            }

            float t = Time.time - cycleStart;
            if (t >= CycleLength)
            {
                cycleStart += CycleLength;
                t -= CycleLength;
                roared = false;
            }

            float visibility;
            if (t < hiddenSeconds)
            {
                visibility = 0f;
            }
            else if (t < hiddenSeconds + fadeInSeconds)
            {
                visibility = Mathf.SmoothStep(0f, 1f, (t - hiddenSeconds) / Mathf.Max(0.01f, fadeInSeconds));
            }
            else if (t < hiddenSeconds + fadeInSeconds + heldSeconds)
            {
                visibility = 1f;
            }
            else
            {
                float k = (t - hiddenSeconds - fadeInSeconds - heldSeconds) / Mathf.Max(0.01f, fadeOutSeconds);
                visibility = Mathf.SmoothStep(1f, 0f, k);
            }

            Apply(visibility);

            if (!roared && t >= hiddenSeconds + roarDelay && t < hiddenSeconds + fadeInSeconds + heldSeconds)
            {
                roared = true;
                Roar();
            }
        }

        private void Apply(float visibility)
        {
            silhouette.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(hiddenColor, visibleColor, visibility));
            silhouette.SetPropertyBlock(block);
        }

        private void Roar()
        {
            if (roarSource != null && roarClip != null)
            {
                roarSource.PlayOneShot(roarClip);
            }

            if (animator != null)
            {
                animator.SetTrigger(RoarId);
            }
        }
    }
}
