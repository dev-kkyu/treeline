using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 자기 축을 계속 돈다. 4장 환기 팬이 도는 것을 보여주는 데 쓴다.
    /// </summary>
    /// <remarks>
    /// 팬이 도는지 안 도는지는 <b>이 장의 선택지 그 자체</b>다 — 수소를 빼고 들어갈지
    /// 그냥 들어갈지. 소리만으로는 문 밖에서 판단이 안 서서 눈에 보이게 했다.
    /// <para>
    /// <see cref="Device"/> 가 켜고 끈다. 도는 팬과 멈춘 팬을 따로 두지 않고 이
    /// 컴포넌트만 켜면 되도록 <see cref="MonoBehaviour"/> 하나로 뒀다.
    /// </para>
    /// </remarks>
    public sealed class Spin : MonoBehaviour
    {
        [Tooltip("초당 도는 각도. 팬 날개는 실제보다 느려야 프레임에서 형태가 읽힌다.")]
        [SerializeField] private float degreesPerSecond = 220f;

        [Tooltip("도는 축. 로컬 기준이다.")]
        [SerializeField] private Vector3 axis = Vector3.forward;

        private void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
