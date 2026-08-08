using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 타이틀 화면의 카메라를 아주 느리게 흔든다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 정지 화면은 눈이 내려도 사진처럼 보인다. 카메라가 조금씩 움직이면 눈송이가
    /// 카메라를 스쳐 지나가면서 <b>깊이가 생긴다.</b> 파티클을 늘리는 것보다 이쪽이
    /// 싸고 효과가 크다.
    /// </para>
    /// <para>
    /// 진폭을 크게 두지 않는 이유는 메뉴가 이 위에 얹히기 때문이다. 배경이 크게
    /// 움직이면 글자를 읽는 눈이 따라간다. 서로 어긋나는 두 주기를 겹쳐 반복이
    /// 눈에 띄지 않게만 한다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TitleCameraDrift : MonoBehaviour
    {
        [Tooltip("좌우 · 상하로 움직이는 폭(m).")]
        [SerializeField] private Vector2 amplitude = new Vector2(0.9f, 0.35f);

        [Tooltip("한 바퀴에 걸리는 시간(초). 둘을 서로 나누어떨어지지 않게 둔다.")]
        [SerializeField] private Vector2 period = new Vector2(37f, 23f);

        [Tooltip("바라보는 방향이 흔들리는 각도.")]
        [SerializeField] private float yawSwing = 1.6f;

        private Vector3 origin;
        private Quaternion baseRotation;

        private void Awake()
        {
            origin = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        private void Update()
        {
            float t = Time.unscaledTime;   // 일시정지해도 배경은 산다
            float x = Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(1f, period.x)) * amplitude.x;
            float y = Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(1f, period.y)) * amplitude.y;

            transform.localPosition = origin + new Vector3(x, y, 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, x / Mathf.Max(0.01f, amplitude.x) * yawSwing, 0f);
        }
    }
}
