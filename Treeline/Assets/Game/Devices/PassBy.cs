using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 켜지면 한 번 지나가고 사라진다. 창밖을 스치는 그림자가 이것이다.
    /// </summary>
    /// <remarks>
    /// 1장 끝의 「눈보라 사이로 무언가가 지나간다」가 첫 용도다. 처음에는 <b>대역
    /// 실루엣</b>으로 만들었다 — 잘못 본 것 같은 정도면 충분하고, <c>Colossus.prefab</c>
    /// 을 기다리면 1장이 적 담당에 묶이기 때문이다. 적 모델이 들어온 뒤에는 실물을
    /// 쓰지만 <b>싸우지 않는다는 것은 그대로다</b> — 눈보라와 거리가 그것을 실루엣으로
    /// 만들고, 맞닥뜨리는 것은 6장이다.
    /// <para>
    /// <see cref="Device"/> 가 아니라 그냥 컴포넌트인 이유는 <b>발동시킬 대상이 아니라
    /// 발동의 결과</b>이기 때문이다. 장치의 <c>enables</c> 에 넣으면 켜지는 순간
    /// 저절로 지나간다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PassBy : MonoBehaviour
    {
        [Tooltip("어디서 어디로. 켜졌을 때의 자리를 기준으로 한 상대 위치다.")]
        [SerializeField] private Vector3 from = new Vector3(-18f, 0f, 0f);

        [SerializeField] private Vector3 to = new Vector3(18f, 0f, 0f);

        [Tooltip("지나가는 데 걸리는 시간.")]
        [SerializeField] private float seconds = 4f;

        [Tooltip("켜지고 나서 움직이기까지 기다리는 시간. 켜자마자 지나가면 우연으로 안 보인다.")]
        [SerializeField] private float delaySeconds = 1.2f;

        [Tooltip("지나간 뒤 스스로 꺼지는지. 끄면 제자리에 남는다.")]
        [SerializeField] private bool hideWhenDone = true;

        [Header("걷는 모습")]
        [Tooltip("지나가는 동안 이 Animator 의 속도 파라미터를 올린다. 비워 두면 아무것도 안 한다.")]
        [SerializeField] private Animator animator;

        [Tooltip("올릴 파라미터 이름.")]
        [SerializeField] private string speedParameter = "Speed";

        [Tooltip("걷기에 해당하는 값. Colossus 는 1.15 가 너클보행이다.")]
        [SerializeField] private float speedValue = 1.15f;

        private Vector3 origin;
        private float elapsed;
        private bool captured;

        private void Awake()
        {
            origin = transform.localPosition;
            captured = true;
        }

        private void OnEnable()
        {
            if (!captured)
            {
                // Awake 보다 먼저 켜지는 경우가 있다. 그때는 지금 자리를 기준으로 삼는다.
                origin = transform.localPosition;
                captured = true;
            }

            elapsed = 0f;
            transform.localPosition = origin + from;

            // 위치만 옮기면 미끄러져 지나간다. 다리가 움직여야 「지나갔다」로 읽힌다.
            if (animator != null && !string.IsNullOrEmpty(speedParameter))
            {
                animator.SetFloat(speedParameter, speedValue);
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            float t = (elapsed - delaySeconds) / Mathf.Max(0.0001f, seconds);
            if (t < 0f)
            {
                return;
            }

            if (t >= 1f)
            {
                transform.localPosition = origin + to;
                if (hideWhenDone)
                {
                    gameObject.SetActive(false);
                }

                return;
            }

            transform.localPosition = Vector3.Lerp(origin + from, origin + to, t);
        }
    }
}
