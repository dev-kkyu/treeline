using UnityEngine;
using UnityEngine.Rendering;

namespace Treeline
{
    /// <summary>
    /// 적이 탄환에 맞은 위치에 검붉은 미스트와 무거운 방울을 짧게 뿌린다.
    /// WebGL에서 매 피격마다 오브젝트를 만들지 않도록 작은 순환 풀을 사용한다.
    /// </summary>
    public sealed class EnemyHitBloodVfx : MonoBehaviour
    {
        // 6장 처형이 한 프레임에 16 갈래를 뿌린다. 12 였을 때는 뒤쪽 갈래가 앞쪽 슬롯을
        // 되잡아 방금 뿌린 피를 지웠다. 슬롯 하나는 안 도는 파티클 시스템 둘뿐이라 싸다.
        private const int PoolSize = 24;
        private const string MistTexturePath = "EnemyHit/blood-mist";
        private const string DropletTexturePath = "EnemyHit/blood-droplet";
        private const string MistMaterialPath = "EnemyHit/MAT_BloodMist";
        private const string DropletMaterialPath = "EnemyHit/MAT_BloodDroplet";

        private sealed class BurstSlot
        {
            public Transform Root;
            public ParticleSystem Mist;
            public ParticleSystem Droplets;
        }

        private static EnemyHitBloodVfx instance;

        private BurstSlot[] slots;
        private int nextSlot;
        private Material mistMaterial;
        private Material dropletMaterial;

        /// <summary>총알이 적에게 맞은 순간 호출한다.</summary>
        public static void Spawn(Vector3 point, Vector3 surfaceNormal, Vector3 shotDirection)
        {
            EnsureInstance();
            if (instance != null)
            {
                instance.Emit(point, surfaceNormal, shotDirection);
            }
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject root = new GameObject(nameof(EnemyHitBloodVfx));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<EnemyHitBloodVfx>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            mistMaterial = LoadMaterial(MistMaterialPath, MistTexturePath, "Runtime_BloodMist");
            dropletMaterial = LoadMaterial(DropletMaterialPath, DropletTexturePath, "Runtime_BloodDroplet");

            slots = new BurstSlot[PoolSize];
            for (int i = 0; i < slots.Length; i++)
            {
                Transform slotRoot = new GameObject($"BloodHit_{i:00}").transform;
                slotRoot.SetParent(transform, false);
                slots[i] = new BurstSlot
                {
                    Root = slotRoot,
                    Mist = BuildMist(slotRoot),
                    Droplets = BuildDroplets(slotRoot)
                };
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (mistMaterial != null && (mistMaterial.hideFlags & HideFlags.DontSave) != 0)
            {
                Destroy(mistMaterial);
            }

            if (dropletMaterial != null && (dropletMaterial.hideFlags & HideFlags.DontSave) != 0)
            {
                Destroy(dropletMaterial);
            }
        }

        private void Emit(Vector3 point, Vector3 surfaceNormal, Vector3 shotDirection)
        {
            BurstSlot slot = slots[nextSlot];
            nextSlot = (nextSlot + 1) % slots.Length;

            Vector3 forward = surfaceNormal.sqrMagnitude > 0.001f
                ? surfaceNormal.normalized
                : -shotDirection.normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f
                ? Vector3.right
                : Vector3.up;
            slot.Root.SetPositionAndRotation(point + forward * 0.018f, Quaternion.LookRotation(forward, up));

            slot.Mist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.Droplets.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.Mist.Emit(Random.Range(2, 4));
            slot.Droplets.Emit(30);
        }

        private ParticleSystem BuildMist(Transform parent)
        {
            ParticleSystem ps = NewSystem(parent, "BloodMist");
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.58f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 1.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.42f, 0.82f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.88f, 0.22f, 0.2f, 0.82f),
                new Color(1f, 0.42f, 0.36f, 1f));
            main.maxParticles = 32;

            ConfigureCone(ps, 19f, 0.018f);
            ConfigureFade(ps, 0.78f);

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.42f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0.82f)));

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mistMaterial;
            renderer.sortingFudge = 0.2f;
            return ps;
        }

        private ParticleSystem BuildDroplets(Transform parent)
        {
            ParticleSystem ps = NewSystem(parent, "BloodDroplets");
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 5.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.075f, 0.16f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.86f, 0.3f, 0.26f, 0.9f),
                new Color(1f, 0.58f, 0.48f, 1f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
            main.maxParticles = 64;

            ConfigureCone(ps, 27f, 0.012f);
            ConfigureFade(ps, 0.86f);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.4f;
            renderer.velocityScale = 0.12f;
            renderer.sharedMaterial = dropletMaterial;
            return ps;
        }

        private static ParticleSystem NewSystem(Transform parent, string objectName)
        {
            GameObject go = new GameObject(objectName);
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

        private static void ConfigureCone(ParticleSystem ps, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
            shape.length = 0.03f;
        }

        private static void ConfigureFade(ParticleSystem ps, float holdAlpha)
        {
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.05f),
                    new GradientAlphaKey(holdAlpha, 0.62f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private static Material CreateMaterial(Texture2D texture, string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
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
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            return material;
        }

        private static Material LoadMaterial(string materialPath, string texturePath, string fallbackName)
        {
            Material material = Resources.Load<Material>(materialPath);
            return material != null
                ? material
                : CreateMaterial(Resources.Load<Texture2D>(texturePath), fallbackName);
        }
    }
}
