using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 환기 팬이 도는 동안 <b>가스가 옅어지는 것을 보이게</b> 한다. 4장 축전지실이다.
    /// </summary>
    /// <remarks>
    /// 팬이 다 돌고 나서야 가스가 툭 사라지면, 기다린 25 초가 아무것도 아닌 시간이 된다.
    /// 옅어지는 것이 보여야 「빠지고 있다」가 읽히고, 그래야 기다림이 선택으로 느껴진다.
    /// <para>
    /// <b>가스 오브젝트에 붙이고 팬을 물린다.</b> 이벤트로 받지 않고
    /// <see cref="TimedDevice.Progress"/> 를 매 프레임 읽는 이유는, 체크포인트에서
    /// 되살아났을 때 값이 저절로 맞기 때문이다. 이벤트로 받으면 되돌린 상태를 여기서
    /// 또 기억해야 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: 색은 <see cref="MaterialPropertyBlock"/> 으로 준다.
    /// <c>MAT_Smoke</c> 는 폭발 연기 · 문틈 연기가 같이 쓰는 공용 머티리얼이라,
    /// 직접 건드리면 이 방과 상관없는 연기까지 같이 옅어진다.
    /// </para>
    /// </remarks>
    public sealed class GasVent : MonoBehaviour
    {
        [Tooltip("이 장치가 도는 동안 옅어진다. 4장은 환기 팬이다.")]
        [SerializeField] private TimedDevice fan;

        private ParticleSystem[] clouds;
        private ParticleSystemRenderer[] skins;
        private float[] baseRate;
        private Light[] lamps;
        private float[] baseIntensity;
        private AudioSource[] hisses;
        private float[] baseVolume;
        private MaterialPropertyBlock block;
        private float applied = -1f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            clouds = GetComponentsInChildren<ParticleSystem>(true);
            skins = new ParticleSystemRenderer[clouds.Length];
            baseRate = new float[clouds.Length];
            for (int i = 0; i < clouds.Length; i++)
            {
                skins[i] = clouds[i].GetComponent<ParticleSystemRenderer>();
                baseRate[i] = clouds[i].emission.rateOverTimeMultiplier;
            }

            lamps = GetComponentsInChildren<Light>(true);
            baseIntensity = new float[lamps.Length];
            for (int i = 0; i < lamps.Length; i++)
            {
                baseIntensity[i] = lamps[i].intensity;
            }

            // 새는 소리도 같이 옅어져야 한다. 보이는 것만 옅어지고 소리가 그대로면
            // 팬을 다 돌린 뒤에도 방이 여전히 위험하게 들린다.
            hisses = GetComponentsInChildren<AudioSource>(true);
            baseVolume = new float[hisses.Length];
            for (int i = 0; i < hisses.Length; i++)
            {
                baseVolume[i] = hisses[i].volume;
            }

            block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (fan == null)
            {
                return;
            }

            // 앞쪽은 천천히, 끝에서 훅 빠진다. 비례로 주면 팬이 다 돌아 가스가 꺼지는
            // 순간에도 아직 눈에 보이는 만큼 남아 있어서, 마지막에 툭 사라진 것으로 보인다.
            float left = Mathf.SmoothStep(1f, 0f, fan.Progress);
            if (Mathf.Abs(left - applied) < 0.002f)
            {
                return;
            }

            applied = left;

            for (int i = 0; i < clouds.Length; i++)
            {
                ParticleSystem.EmissionModule emission = clouds[i].emission;
                emission.rateOverTimeMultiplier = baseRate[i] * left;

                if (skins[i] == null)
                {
                    continue;
                }

                // 이미 떠 있는 알갱이까지 같이 옅어져야 한다. 방출만 줄이면 18 초짜리
                // 수명이 다 지날 때까지 짙은 채로 남는다.
                skins[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, new Color(1f, 1f, 1f, left));
                skins[i].SetPropertyBlock(block);
            }

            for (int i = 0; i < lamps.Length; i++)
            {
                lamps[i].intensity = baseIntensity[i] * left;
            }

            for (int i = 0; i < hisses.Length; i++)
            {
                hisses[i].volume = baseVolume[i] * left;
            }
        }
    }
}
