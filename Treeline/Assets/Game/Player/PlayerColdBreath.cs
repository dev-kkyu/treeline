using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 추운 게임 장면에서 카메라 아래쪽에 입김을 뿜는다.
    /// 작은 수증기 입자 여러 개가 카메라를 따라가며 짧게 퍼져 달리는 중에도 보인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerColdBreath : MonoBehaviour
    {
        private const string MaterialPath = "PlayerBreath/MAT_ColdBreath";

        private static PlayerColdBreath instance;

        [SerializeField] private ParticleSystem breath;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Vector2 calmInterval = new Vector2(2f, 3.2f);
        [SerializeField] private Vector2 sprintInterval = new Vector2(0.7f, 1.05f);

        private float nextBreath;
        private float nextCluster;
        private int queuedParticles;
        private bool suspended;

        public static PlayerColdBreath AttachTo(Camera camera, PlayerMotor motor)
        {
            if (camera == null)
            {
                return null;
            }

            camera = FindWorldCamera(camera);
            PlayerColdBreath existing = camera.GetComponentInChildren<PlayerColdBreath>(true);
            if (existing != null)
            {
                existing.motor = motor;
                return existing;
            }

            GameObject root = new GameObject("PlayerColdBreath");
            root.transform.SetParent(camera.transform, false);
            root.transform.localPosition = new Vector3(-0.08f, -0.14f, camera.nearClipPlane + 0.18f);
            root.layer = GameLayers.Default;

            PlayerColdBreath effect = root.AddComponent<PlayerColdBreath>();
            effect.motor = motor;
            Material material = Resources.Load<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError($"[{nameof(PlayerColdBreath)}] Resources/{MaterialPath} 머티리얼을 찾을 수 없습니다.", root);
            }

            effect.breath = BuildSystem(root.transform, material);
            return effect;
        }

        public static void HideImmediate()
        {
            if (instance == null)
            {
                return;
            }

            instance.suspended = true;
            instance.queuedParticles = 0;
            if (instance.breath != null)
            {
                instance.breath.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private static ParticleSystem BuildSystem(Transform parent, Material material)
        {
            GameObject go = new GameObject("ColdBreathParticles");
            go.transform.SetParent(parent, false);
            go.layer = GameLayers.Default;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            // 방출량은 0으로 두고 Emit으로 호흡 타이밍만 제어한다. 시스템 자체는
            // 계속 재생해야 수동 방출한 입자가 WebGL에서도 확실히 갱신된다.
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.32f, 0.72f);
            main.startSize3D = false;
            main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.105f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.68f, 0.74f, 0.78f, 0.2f),
                new Color(0.9f, 0.94f, 0.96f, 0.38f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 64;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 17f;
            shape.radius = 0.016f;
            shape.length = 0.015f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.025f, 0.055f);
            velocity.z = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.strength = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            noise.frequency = 0.85f;
            noise.scrollSpeed = 0.3f;
            noise.damping = true;

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.76f, 0.82f, 0.86f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.16f),
                    new GradientAlphaKey(0.72f, 0.58f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.separateAxes = false;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f), new Keyframe(0.35f, 0.9f), new Keyframe(1f, 1.4f)));

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.pivot = Vector3.zero;
            renderer.sortingOrder = 20;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            ps.Play(true);
            return ps;
        }

        private static Camera FindWorldCamera(Camera preferred)
        {
            int mask = 1 << GameLayers.Default;
            if ((preferred.cullingMask & mask) != 0)
            {
                return preferred;
            }

            Camera[] cameras = preferred.transform.root.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                if ((camera.cullingMask & mask) != 0)
                {
                    return camera;
                }
            }

            return preferred;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            nextBreath = Time.time + 0.2f;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (suspended || breath == null)
            {
                return;
            }

            float now = Time.time;
            if (queuedParticles > 0 && now >= nextCluster)
            {
                EmitCluster(now);
            }

            if (queuedParticles == 0 && now >= nextBreath)
            {
                if (!breath.isPlaying)
                {
                    breath.Play(true);
                }

                bool sprinting = motor != null && motor.IsSprinting;
                queuedParticles = sprinting ? Random.Range(12, 19) : Random.Range(8, 13);
                EmitCluster(now);
                // 소리는 PlayerBodyAudio 가 낸다. 지쳐서 헐떡이는 동안에는 입김
                // 소리를 빼야 하는데 그 상태를 아는 것이 저쪽이다.
                PlayerBodyAudio.PlayColdBreathOnce();
                Vector2 interval = sprinting ? sprintInterval : calmInterval;
                nextBreath = now + Random.Range(interval.x, interval.y);
            }
        }

        private void EmitCluster(float now)
        {
            int clusterSize = Mathf.Min(queuedParticles, Random.Range(2, 5));
            breath.Emit(clusterSize);
            queuedParticles -= clusterSize;
            nextCluster = now + Random.Range(0.035f, 0.12f);
        }
    }
}
