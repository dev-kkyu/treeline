using UnityEngine;
using UnityEngine.Rendering;

namespace Treeline
{
    /// <summary>
    /// 파티클 한 알짜리 그림과 그것을 그릴 재질을 코드로 굽는다.
    /// </summary>
    /// <remarks>
    /// 불똥 · 먼지 · 예광탄은 전부 <b>가운데가 밝고 가장자리로 사라지는 점</b> 하나면
    /// 된다. 파일로 두면 셋이 각자 받아야 할 에셋만 늘고, 크기를 고칠 때마다 다시
    /// 굽는다. 피처럼 모양이 있는 것은 그림 파일을 쓴다 —
    /// <see cref="EnemyHitBloodVfx"/> 가 그쪽이다.
    /// </remarks>
    public static class VfxDots
    {
        /// <summary>가운데가 밝고 가장자리로 사라지는 점 하나.</summary>
        /// <param name="core">
        /// 심을 얼마나 두껍게 둘지(0~1). 작게 주면 또렷한 알갱이가 되고, 1 에 가까우면
        /// 처음부터 흐려져 연기가 된다.
        /// </param>
        public static Texture2D BakeDot(int size, float core, string textureName)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            float half = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float a = Mathf.Clamp01(1f - Mathf.Max(0f, d - core) / Mathf.Max(0.001f, 1f - core));
                    a *= a;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        /// <summary>파티클용 반투명 재질. <paramref name="additive"/> 면 더해서 그린다.</summary>
        /// <remarks>
        /// IMPORTANT: 뜨거운 것(불똥 · 예광탄)은 반드시 더해서 그린다. 어두운 벽에
        /// 알파로 섞으면 밝은 점이 아니라 <b>회색 점</b>이 된다.
        /// </remarks>
        public static Material CreateParticleMaterial(Texture2D texture, string materialName, bool additive)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default");

            var material = new Material(shader)
            {
                name = materialName,
                mainTexture = texture,
                hideFlags = HideFlags.DontSave
            };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", additive ? 1f : 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            return material;
        }

        /// <summary>연출용 파티클 시스템 하나. 스스로 돌지 않고 불러 줄 때만 뿜는다.</summary>
        public static ParticleSystem NewSystem(Transform parent, string objectName)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return ps;
        }
    }
}
