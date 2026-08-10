using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 총구에서 착탄점까지 <b>날아가는</b> 예광탄 한 알. 탄마다 하나씩 뿜는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 예전에는 <c>LineRenderer</c> 로 총구와 착탄점을 이어 붙이고 옅어지게만 했다.
    /// 그러면 총알이 아니라 <b>레이저</b>로 보인다 — 한 프레임에 통째로 그어지니
    /// 지나가는 것이 없고, 선 하나뿐이라 <b>산탄 여덟 발도 선 한 줄</b>로 합쳐졌다.
    /// 여기서는 알갱이 하나가 실제로 날아가며, 탄마다 한 알이라 산탄은 퍼지는 다발이 된다.
    /// </para>
    /// <para>
    /// 늘여 그리기(<see cref="ParticleSystemRenderMode.Stretch"/>)라 <b>빠를수록 길어진다.</b>
    /// 그래서 속도 하나로 「짧고 빠른 것」과 「길게 늘어진 것」이 같이 정해진다.
    /// </para>
    /// <para>
    /// 수명은 거리에 맞춰 준다. 그래야 착탄점에서 정확히 사라진다 — 넘어가면 벽을
    /// 뚫고 지나간 것으로 보이고, 모자라면 허공에서 멈춘다.
    /// </para>
    /// </remarks>
    public sealed class ShotTracerVfx : MonoBehaviour
    {
        /// <summary>보이기 위한 탄속(m/s). 실제 탄도와는 상관없다.</summary>
        /// <remarks>
        /// 진짜 탄속(수백 m/s)을 쓰면 한 프레임에 도착해 아무것도 안 보인다. 60 fps 에서
        /// 10 m 를 대여섯 프레임에 지나가는 값이라, 눈이 「지나갔다」를 잡을 수 있다.
        /// </remarks>
        private const float Speed = 110f;

        /// <summary>아주 가까이 쏴도 이만큼은 날아간다. 코앞의 벽에서 안 보이는 것을 막는다.</summary>
        private const float MinSeconds = 0.035f;

        private const int MaxParticles = 64;

        /// <summary>총구에서 이만큼 앞에서 그리기 시작한다.</summary>
        /// <remarks>
        /// IMPORTANT: 총구는 카메라에서 0.5 m 도 안 떨어져 있다. 거기서 그리기 시작하면
        /// 원근 때문에 알갱이 하나가 <b>화면 절반을 덮는 흰 덩어리</b>로 보인다. 실제로
        /// 처음엔 혜성처럼 찍혔다. 조금 앞에서 시작하면 총구 섬광이 그 자리를 가린다.
        /// </remarks>
        private const float MuzzleOffset = 0.95f;

        private static ShotTracerVfx instance;

        private ParticleSystem rounds;
        private ParticleSystem sparks;
        private Material material;
        private Texture2D texture;

        /// <summary>탄 하나가 <paramref name="from"/> 에서 <paramref name="to"/> 까지 지나간다.</summary>
        public static void Spawn(Vector3 from, Vector3 to)
        {
            EnsureInstance();
            if (instance != null)
            {
                instance.Emit(from, to);
            }
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            var root = new GameObject(nameof(ShotTracerVfx));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<ShotTracerVfx>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            texture = VfxDots.BakeDot(16, 0.22f, "Runtime_TracerDot");
            material = VfxDots.CreateParticleMaterial(texture, "Runtime_Tracer", true);

            rounds = BuildRounds();
            sparks = BuildMuzzleSparks();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            Destroy(material);
            Destroy(texture);
        }

        private void Emit(Vector3 from, Vector3 to)
        {
            Vector3 path = to - from;
            float distance = path.magnitude;
            if (distance < 0.001f)
            {
                return;
            }

            Vector3 direction = path / distance;

            // 총구 앞에서 시작한다. 벽이 코앞이면 밀 자리가 없으니 절반만 민다.
            Vector3 start = from + direction * Mathf.Min(MuzzleOffset, distance * 0.5f);
            float flight = Vector3.Distance(start, to);
            float seconds = Mathf.Max(MinSeconds, flight / Speed);

            // 수명 안에 정확히 도착하도록 속도를 거리에서 되짚는다. 아주 가까운 표적은
            // MinSeconds 에 걸려 느려지는데, 그편이 안 보이는 것보다 낫다.
            var round = new ParticleSystem.EmitParams
            {
                position = start,
                velocity = direction * (flight / seconds),
                startLifetime = seconds,
                startSize = Random.Range(0.014f, 0.022f),
                startColor = Color.white
            };
            rounds.Emit(round, 1);

            // 총구에서 튀는 불똥. 섬광만으로는 총구가 어디인지 안 보인다.
            var spark = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            sparks.transform.SetPositionAndRotation(from + direction * 0.25f, Quaternion.LookRotation(direction));
            sparks.Emit(spark, Random.Range(2, 5));
        }

        private ParticleSystem BuildRounds()
        {
            ParticleSystem ps = VfxDots.NewSystem(transform, "Rounds");
            ParticleSystem.MainModule main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.86f, 0.55f, 1f),
                new Color(1f, 0.52f, 0.18f, 1f));
            main.maxParticles = MaxParticles;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            // 뒤로 갈수록 옅어져 꼬리가 생긴다. 앞머리는 끝까지 밝게 둔다.
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
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            // 속도를 길이로 바꾸는 값. 110 m/s 면 대략 2 m 짜리 줄이 된다.
            renderer.velocityScale = 0.02f;
            renderer.lengthScale = 1.2f;
            renderer.sharedMaterial = material;
            return ps;
        }

        private ParticleSystem BuildMuzzleSparks()
        {
            ParticleSystem ps = VfxDots.NewSystem(transform, "MuzzleSparks");
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.6f, 1f),
                new Color(1f, 0.6f, 0.2f, 1f));
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            main.maxParticles = 32;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 24f;
            shape.radius = 0.01f;
            shape.length = 0.02f;

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
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.02f;
            renderer.lengthScale = 1.4f;
            renderer.sharedMaterial = material;
            return ps;
        }
    }
}
