using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 죽어가는 형광등. 불규칙하게 깜빡이고, 대부분의 시간은 꺼져 있다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 복도에 이런 등을 **하나만** 둔다. 여럿이면 밝아져서 손전등이 의미를 잃고,
    /// 깜빡임이 배경이 되어 무섭지도 않다. 하나일 때만 「저기 뭔가 있나」가 된다.
    /// </para>
    /// <para>
    /// IMPORTANT: WebGL 이라 실시간 광원을 아껴야 한다. 천장의 나머지 등은 광원 없이
    /// 발광 머티리얼로만 두고, 실제 <see cref="Light"/> 는 손전등 · 비상등 ·
    /// 이 깜빡이 하나로 제한한다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Light))]
    public sealed class FlickerLight : MonoBehaviour
    {
        [SerializeField] private float onIntensity = 2.4f;

        [Tooltip("꺼져 있는 시간의 범위(초). 길수록 불안하다.")]
        [SerializeField] private Vector2 offSeconds = new Vector2(1.2f, 5.0f);

        [Tooltip("한 번 살아 있는 시간의 범위(초). 짧아야 놀란다.")]
        [SerializeField] private Vector2 onSeconds = new Vector2(0.04f, 0.35f);

        [Tooltip("한 번 켜질 때 몇 번 떨리는가.")]
        [SerializeField] private Vector2Int burst = new Vector2Int(1, 4);

        [SerializeField] private AudioSource flickerSound;

        private Light target;

        private void Awake()
        {
            target = GetComponent<Light>();
            target.intensity = 0f;
        }

        private void OnEnable()
        {
            StartCoroutine(Loop());
        }

        private System.Collections.IEnumerator Loop()
        {
            // WebGL 에서 코루틴은 안전하다. 스레드를 쓰지 않는다.
            while (true)
            {
                target.intensity = 0f;
                yield return new WaitForSeconds(Random.Range(offSeconds.x, offSeconds.y));

                int n = Random.Range(burst.x, burst.y + 1);
                if (flickerSound != null)
                {
                    flickerSound.Play();
                }

                for (int i = 0; i < n; i++)
                {
                    target.intensity = onIntensity * Random.Range(0.55f, 1f);
                    yield return new WaitForSeconds(Random.Range(onSeconds.x, onSeconds.y));
                    target.intensity = 0f;
                    yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
                }
            }
        }
    }
}
