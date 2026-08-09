using UnityEngine;
using UnityEngine.InputSystem;

namespace Treeline
{
    /// <summary>
    /// 타이틀 · 엔딩 화면의 카메라를 아주 느리게 흔들고, 마우스를 따라 조금 움직인다.
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
    /// <para>
    /// 저절로 흔들리기만 하면 <b>영상</b>이고, 마우스에 반응하면 <b>화면 안에 있는
    /// 것</b>이 된다. 그래서 자동 흔들림 위에 포인터 오프셋을 더한다. 배경 오브젝트를
    /// 층별로 따로 밀지 않는 이유는, 카메라를 움직이면 <b>원근이 알아서</b> 가까운
    /// 눈은 많이 먼 산은 조금 밀어 주기 때문이다 — 층을 손으로 나누면 그 비율이
    /// 씬을 고칠 때마다 어긋난다.
    /// </para>
    /// <para>
    /// IMPORTANT: 신규 Input System 전용 프로젝트라 <c>Input.mousePosition</c> 은
    /// 런타임에 예외가 난다. 여기는 액션이 아니라 장치를 직접 읽는데, 시선 액션은
    /// 마우스 <b>델타</b>라 화면 어디를 가리키는지 알 수 없기 때문이다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TitleCameraDrift : MonoBehaviour
    {
        [Header("저절로 흔들리는 폭")]
        [Tooltip("좌우 · 상하로 움직이는 폭(m).")]
        [SerializeField] private Vector2 amplitude = new Vector2(0.9f, 0.35f);

        [Tooltip("한 바퀴에 걸리는 시간(초). 둘을 서로 나누어떨어지지 않게 둔다.")]
        [SerializeField] private Vector2 period = new Vector2(37f, 23f);

        [Tooltip("바라보는 방향이 흔들리는 각도.")]
        [SerializeField] private float yawSwing = 1.6f;

        [Header("마우스 반응")]
        [Tooltip("포인터를 화면 끝까지 옮겼을 때 카메라가 따라가는 거리(m).")]
        [SerializeField] private Vector2 pointerShift = new Vector2(0.55f, 0.24f);

        [Tooltip("포인터를 화면 끝까지 옮겼을 때 더 도는 각도. x 가 좌우, y 가 위아래다.")]
        [SerializeField] private Vector2 pointerSwing = new Vector2(2.4f, 1.4f);

        [Tooltip("포인터를 따라붙는 데 걸리는 시간(초). 0 이면 그대로 붙는다.")]
        [SerializeField, Range(0f, 1.5f)] private float pointerEase = 0.35f;

        private Vector3 origin;
        private Quaternion baseRotation;
        private Vector2 aim;        // 지금 반영된 값. -1~1
        private Vector2 aimVel;     // SmoothDamp 가 들고 있는 속도

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

            // 포인터를 바로 따라가면 손떨림까지 화면에 그대로 나와 싸구려로 보인다.
            // 무거운 카메라가 뒤늦게 따라오는 느낌이라야 눈보라 속 무게가 산다.
            aim = Vector2.SmoothDamp(aim, ReadAim(), ref aimVel,
                                     pointerEase, Mathf.Infinity, Time.unscaledDeltaTime);

            transform.localPosition = origin + new Vector3(
                x + aim.x * pointerShift.x,
                y + aim.y * pointerShift.y,
                0f);

            // 위로 올리면 위를 봐야 하므로 pitch 는 부호가 반대다.
            transform.localRotation = baseRotation * Quaternion.Euler(
                -aim.y * pointerSwing.y,
                x / Mathf.Max(0.01f, amplitude.x) * yawSwing + aim.x * pointerSwing.x,
                0f);
        }

        /// <summary>포인터 위치를 화면 중앙 기준 -1~1 로 바꾼다. 마우스가 없으면 0.</summary>
        private static Vector2 ReadAim()
        {
            // 터치 기기와 WebGL 에서 마우스가 아예 없을 수 있다. 그때는 자동
            // 흔들림만 남고 화면이 멈추지는 않는다.
            Mouse mouse = Mouse.current;
            if (mouse == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return Vector2.zero;
            }

            Vector2 p = mouse.position.ReadValue();
            return new Vector2(
                Mathf.Clamp(p.x / Screen.width * 2f - 1f, -1f, 1f),
                Mathf.Clamp(p.y / Screen.height * 2f - 1f, -1f, 1f));
        }
    }
}
