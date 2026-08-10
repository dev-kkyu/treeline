using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 탄이 벽 · 바닥에 맞은 자리에 불똥과 먼지를 짧게 뿌린다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 적에게 맞으면 <see cref="EnemyHitBloodVfx"/> 가 피를 뿌리는데, <b>빗맞으면
    /// 아무것도 없었다.</b> 어두운 게임이라 예광선만으로는 어디에 닿았는지 모르고,
    /// 그러면 조준이 늘 「맞은 것 같기도」로 끝난다. 여기서 나는 불똥이 <b>빗맞았다는
    /// 사실 자체</b>를 알려 준다.
    /// </para>
    /// <para>
    /// <see cref="PlayerWeapon"/> 이 <b>탄마다</b> 부른다. 산탄 여덟 발이 한 번만
    /// 튀면 산탄으로 안 보인다 — 혈흔을 탄마다 뿌리는 것과 같은 이유다. 소리는
    /// 반대로 한 발사에 한 번만 나야 해서 그쪽은 <see cref="PlayerWeapon.Impacted"/>
    /// 가 따로 맡는다.
    /// </para>
    /// <para>
    /// 그림은 파일로 두지 않고 <see cref="VfxDots"/> 로 굽는다. 불똥 하나와 흐릿한 점
    /// 하나뿐이라 텍스처를 따로 관리할 값어치가 없다.
    /// </para>
    /// </remarks>
    public sealed class ShotImpactVfx : MonoBehaviour
    {
        /// <summary>산탄 한 발이 여덟 갈래라 넉넉히 둔다. 슬롯은 안 도는 파티클 둘뿐이라 싸다.</summary>
        private const int PoolSize = 24;

        private sealed class BurstSlot
        {
            public Transform Root;
            public ParticleSystem Sparks;
            public ParticleSystem Dust;
        }

        private static ShotImpactVfx instance;

        private BurstSlot[] slots;
        private int nextSlot;
        private Material sparkMaterial;
        private Material dustMaterial;
        private Texture2D sparkTexture;
        private Texture2D dustTexture;

        /// <summary>탄이 적이 아닌 것에 맞은 순간 호출한다.</summary>
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

            GameObject root = new GameObject(nameof(ShotImpactVfx));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<ShotImpactVfx>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            sparkTexture = VfxDots.BakeDot(16, 0.15f, "Runtime_SparkDot");
            dustTexture = VfxDots.BakeDot(32, 1f, "Runtime_DustDot");
            sparkMaterial = VfxDots.CreateParticleMaterial(sparkTexture, "Runtime_ImpactSpark", true);
            dustMaterial = VfxDots.CreateParticleMaterial(dustTexture, "Runtime_ImpactDust", false);

            slots = new BurstSlot[PoolSize];
            for (int i = 0; i < slots.Length; i++)
            {
                Transform slotRoot = new GameObject($"ShotImpact_{i:00}").transform;
                slotRoot.SetParent(transform, false);
                slots[i] = new BurstSlot
                {
                    Root = slotRoot,
                    Sparks = BuildSparks(slotRoot),
                    Dust = BuildDust(slotRoot)
                };
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            Destroy(sparkMaterial);
            Destroy(dustMaterial);
            Destroy(sparkTexture);
            Destroy(dustTexture);
        }

        private void Emit(Vector3 point, Vector3 surfaceNormal, Vector3 shotDirection)
        {
            BurstSlot slot = slots[nextSlot];
            nextSlot = (nextSlot + 1) % slots.Length;

            // 불똥은 면에서 튀어나온다. 법선이 없으면 쏜 방향을 되돌려 쓴다.
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
            slot.Root.SetPositionAndRotation(point + forward * 0.02f, Quaternion.LookRotation(forward, up));

            slot.Sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.Dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.Sparks.Emit(Random.Range(7, 13));
            slot.Dust.Emit(Random.Range(1, 3));
        }

        private ParticleSystem BuildSparks(Transform parent)
        {
            ParticleSystem ps = VfxDots.NewSystem(parent, "Sparks");
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 8.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.86f, 0.5f, 1f),
                new Color(1f, 0.55f, 0.18f, 1f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.maxParticles = 32;

            ConfigureCone(ps, 38f, 0.01f);
            ConfigureFade(ps, 0.9f);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            // 늘여 그려야 점이 아니라 튀는 것으로 보인다.
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.6f;
            renderer.velocityScale = 0.05f;
            renderer.sharedMaterial = sparkMaterial;
            return ps;
        }

        private ParticleSystem BuildDust(Transform parent)
        {
            ParticleSystem ps = VfxDots.NewSystem(parent, "Dust");
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.66f, 0.64f, 0.62f, 0.42f),
                new Color(0.82f, 0.8f, 0.78f, 0.6f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f, 0.06f);
            main.maxParticles = 16;

            ConfigureCone(ps, 46f, 0.02f);
            ConfigureFade(ps, 0.7f);

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 1.5f)));

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = dustMaterial;
            renderer.sortingFudge = 0.2f;
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
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(holdAlpha, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

    }
}
