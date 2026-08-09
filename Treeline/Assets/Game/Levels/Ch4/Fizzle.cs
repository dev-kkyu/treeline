using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 켜지면 정해진 시간 동안 <b>사그라들다 스스로 꺼지는</b> 파티클. 4장 폭발 뒤
    /// 남는 불씨가 이것이다.
    /// </summary>
    /// <remarks>
    /// 불씨는 <see cref="ParticleSystem.MainModule.loop"/> 이 켜져 있어 그냥 두면
    /// 영영 탄다. 장을 나갈 때까지 같은 세기로 타는 불은 「방금 터졌다」가 아니라
    /// 배경이 되고, 폭발이 지나간 일이라는 것이 안 읽힌다.
    /// <para>
    /// 알파는 <see cref="MaterialPropertyBlock"/> 으로 준다. <c>MAT_FuelFlame</c> ·
    /// <c>MAT_Ember</c> · <c>MAT_Smoke</c> 는 다른 연출이 같이 쓰는 공용
    /// 머티리얼이라, 직접 건드리면 상관없는 불까지 같이 꺼진다.
    /// </para>
    /// </remarks>
    public sealed class Fizzle : MonoBehaviour
    {
        [Tooltip("다 꺼질 때까지의 초. 이 시간이 지나면 오브젝트를 끈다.")]
        [SerializeField] private float seconds = 5f;

        private ParticleSystem[] flames;
        private ParticleSystemRenderer[] skins;
        private float[] baseRate;
        private MaterialPropertyBlock block;
        private float started;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            // 꺼진 채로 씬에 있다가 폭발이 켜므로, Awake 는 첫 점화 때 돈다.
            // 이때의 방출량이 원본이다.
            flames = GetComponentsInChildren<ParticleSystem>(true);
            skins = new ParticleSystemRenderer[flames.Length];
            baseRate = new float[flames.Length];
            for (int i = 0; i < flames.Length; i++)
            {
                skins[i] = flames[i].GetComponent<ParticleSystemRenderer>();
                baseRate[i] = flames[i].emission.rateOverTimeMultiplier;
            }

            block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            started = Time.time;
            Apply(1f);
        }

        private void Update()
        {
            float t = seconds <= 0f ? 1f : Mathf.Clamp01((Time.time - started) / seconds);

            // 앞은 천천히, 끝에서 훅 빠진다. 비례로 주면 끄는 순간에도 눈에 보이는
            // 만큼 남아 있어서 사그라든 것이 아니라 툭 사라진 것으로 보인다.
            Apply(Mathf.SmoothStep(1f, 0f, t));

            if (t >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        private void Apply(float left)
        {
            for (int i = 0; i < flames.Length; i++)
            {
                ParticleSystem.EmissionModule emission = flames[i].emission;
                emission.rateOverTimeMultiplier = baseRate[i] * left;

                if (skins[i] == null)
                {
                    continue;
                }

                skins[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, new Color(1f, 1f, 1f, left));
                skins[i].SetPropertyBlock(block);
            }
        }
    }
}
