using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 눈보라에 노출되면 <b>카메라 바로 앞에</b> 서리 조각이 맺혔다 사라진다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 전에는 화면 전체를 덮는 <c>RawImage</c> 한 장이었다. 그것은 「화면에 낀 서리」가
    /// 아니라 <b>UI 에 띄운 그림</b>으로 보인다 — 언제나 같은 무늬가 같은 자리에
    /// 나타나고, 고개를 돌려도 따라오지 않으며, 알파만 오르내린다.
    /// </para>
    /// <para>
    /// 파티클로 바꾸면 조각이 <b>매번 다른 자리에 다른 크기로</b> 맺힌다. 카메라의
    /// 자식이라 시선을 그대로 따라오고, 하나하나가 제 수명을 갖고 사라지므로
    /// 「녹는다」가 그림으로 나온다.
    /// </para>
    /// <para>
    /// IMPORTANT: 근평면 바로 앞에 둔다. 조금만 멀어도 벽이 서리를 가리고, 너무
    /// 가까우면 잘린다. 렌더러는 <see cref="GameLayers.ViewModel"/> 로 두어 뷰모델
    /// 카메라가 그리게 한다 — 월드 카메라가 그리면 총이 서리를 뚫고 나온다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ScreenFrostParticles : MonoBehaviour
    {
        private const float RoofCheckDistance = 40f;

        private static ScreenFrostParticles instance;

        [Tooltip("서리 조각을 뿜을 파티클. 비우면 이 오브젝트에서 찾는다.")]
        [SerializeField] private ParticleSystem frost;

        [Tooltip("서리 파티클을 표시할 카메라.")]
        [SerializeField] private Camera effectCamera;

        [Tooltip("지붕이 있는지 검사할 기준. 비우면 부모의 루트를 쓴다.")]
        [SerializeField] private Transform observer;

        [Tooltip("한 번에 맺히는 조각 수.")]
        [SerializeField] private Vector2Int burstCount = new Vector2Int(5, 8);

        [Tooltip("바깥에 있을 때 저절로 맺히는 간격(초).")]
        [SerializeField] private Vector2 autoInterval = new Vector2(6f, 13f);

        private float nextRoofCheck;
        private float nextBurst;
        private bool exposed;
        private bool suspended;

        /// <summary>
        /// 카메라 앞에 서리 장치를 만들어 붙인다. 씬이나 프리팹을 고치지 않고
        /// 코드에서 짓는다 — 앞의 UI 판이 그랬고, 세 장에 같은 것을 손으로 넣지
        /// 않으려는 이유도 같다.
        /// </summary>
        public static ScreenFrostParticles AttachTo(Camera camera, Transform observer)
        {
            if (camera == null)
            {
                return null;
            }

            // IMPORTANT: 서리를 <b>실제로 그리는 카메라</b>에 붙여야 한다.
            // 이 게임은 뷰모델을 따로 그리는 카메라가 있고 화각이 다르다 (본 카메라
            // 70° · 뷰모델 50°). 본 카메라에 붙여 놓고 그쪽 화각으로 자리를 잡았더니
            // 가장자리에 둘렀다고 생각한 조각들이 화면 한가운데 뭉쳤다.
            camera = FindLayerCamera(camera, GameLayers.ViewModel);

            ScreenFrostParticles existing = camera.GetComponentInChildren<ScreenFrostParticles>(true);
            if (existing != null)
            {
                existing.observer = observer;
                existing.effectCamera = camera;
                return existing;
            }

            GameObject root = new GameObject("ScreenFrost");
            root.transform.SetParent(camera.transform, false);
            // 근평면 바로 앞. 벽에 가려지지 않으면서 잘리지도 않는 자리다.
            root.transform.localPosition = new Vector3(0f, 0f, camera.nearClipPlane + 0.035f);
            root.layer = GameLayers.ViewModel;

            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.4f);
            main.startSpeed = 0f;                       // 맺히는 것이지 날아가지 않는다
            // IMPORTANT: 카메라에서 7 cm 앞이라 보이는 세로가 8 cm 밖에 안 된다.
            // 처음에 0.03 을 줬더니 조각 하나가 화면 높이의 3분의 1을 덮었다.
            // 여기 숫자는 「미터」가 아니라 사실상 「화면 비율」로 읽어야 한다.
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.012f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            // 파랗고 진하면 눈 결정 아이콘을 붙여 놓은 것처럼 보인다. 거의 흰색에
            // 가깝게 두고 알파를 낮춰 「유리에 낀 것」으로 남긴다.
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.90f, 0.95f, 1f, 0.34f), new Color(1f, 1f, 1f, 0.58f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 64;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;                       // Emit 으로만 뿌린다

            // 실제 위치는 Burst에서 현재 카메라 비율에 맞춰 네 변에 직접 배치한다.
            // Rectangle 방출은 화면 비율을 모르고 중앙의 작은 영역에 겹쳐 뭉친다.
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = false;

            // 맺히고 → 머물고 → 녹는다. 알파만으로 표현한다.
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.9f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            // 자라나야 「끼는 중」으로 보인다. 처음부터 다 자란 조각은 붙여 놓은 것 같다.
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve grow = new AnimationCurve(
                new Keyframe(0f, 0.35f), new Keyframe(0.25f, 1f), new Keyframe(1f, 1f));
            size.size = new ParticleSystem.MinMaxCurve(1f, grow);

            ParticleSystemRenderer pr = root.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.alignment = ParticleSystemRenderSpace.Local;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pr.sortingOrder = 200;

            // IMPORTANT: 머티리얼은 코드에서 짓지 않고 에셋을 쓴다.
            // URP 는 <c>_Surface</c> · <c>_Blend</c> 를 넣는 것만으로 반투명이 되지
            // 않는다 — 실제 블렌딩은 <c>_SrcBlend</c> · <c>_DstBlend</c> · <c>_ZWrite</c>
            // 와 셰이더 키워드가 정한다. 그걸 빠뜨렸더니 서리가 <b>불투명한 검은
            // 사각형</b>으로 나왔다. 에셋으로 두면 그 설정이 파일에 박혀 있다.
            Material mat = Resources.Load<Material>("Effects/MAT_ScreenFrost");
            if (mat == null)
            {
                mat = pr.sharedMaterial;
            }

            pr.sharedMaterial = mat;

            ScreenFrostParticles effect = root.AddComponent<ScreenFrostParticles>();
            effect.frost = ps;
            effect.effectCamera = camera;
            effect.observer = observer;
            return effect;
        }

        /// <summary>
        /// 주어진 레이어를 실제로 그리는 카메라를 찾는다. 자식 중에 전용 카메라가
        /// 있으면 그쪽이고, 없으면 넘겨받은 것을 그대로 쓴다.
        /// </summary>
        private static Camera FindLayerCamera(Camera preferred, int layer)
        {
            int mask = 1 << layer;
            if (preferred == null)
            {
                return null;
            }

            Camera[] children = preferred.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if ((children[i].cullingMask & mask) != 0)
                {
                    return children[i];
                }
            }

            return preferred;
        }

        /// <summary>파티클 충돌이나 연출에서 서리를 한 번 강제로 맺는다.</summary>
        public static void SnowHit()
        {
            if (instance != null)
            {
                instance.Burst();
            }
        }

        /// <summary>장 전환이나 사망 화면에서 즉시 지운다.</summary>
        public static void HideImmediate()
        {
            if (instance == null)
            {
                return;
            }

            instance.suspended = true;
            if (instance.frost != null)
            {
                instance.frost.Clear(true);
                instance.frost.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;

            if (frost == null)
            {
                frost = GetComponentInChildren<ParticleSystem>(true);
            }

            if (observer == null)
            {
                observer = transform.root;
            }

            if (effectCamera == null)
            {
                effectCamera = GetComponentInParent<Camera>();
            }

            nextBurst = Time.unscaledTime + Random.Range(autoInterval.x, autoInterval.y);
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
            if (suspended || frost == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= nextRoofCheck)
            {
                nextRoofCheck = now + 1f;
                exposed = IsExposedToSky();
            }

            if (exposed && now >= nextBurst)
            {
                Burst();
            }
        }

        /// <summary>위로 아무것도 없으면 바깥이다. 실내에서는 저절로 맺히지 않는다.</summary>
        private bool IsExposedToSky()
        {
            if (observer == null)
            {
                return false;
            }

            Vector3 origin = observer.position + Vector3.up * 1.75f;
            return !Physics.Raycast(origin, Vector3.up, RoofCheckDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private void Burst()
        {
            suspended = false;
            nextBurst = Time.unscaledTime + Random.Range(autoInterval.x, autoInterval.y);

            if (frost == null)
            {
                return;
            }

            int count = Mathf.Max(4, Random.Range(burstCount.x, burstCount.y + 1));
            float distance = Mathf.Max(0.01f, transform.localPosition.z);
            float fieldOfView = effectCamera != null ? effectCamera.fieldOfView : 60f;
            float halfHeight = effectCamera != null && effectCamera.orthographic
                ? effectCamera.orthographicSize
                : Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float aspect = effectCamera != null ? effectCamera.aspect : 16f / 9f;
            float halfWidth = halfHeight * aspect;
            int firstEdge = Random.Range(0, 4);

            // 네 변을 차례로 사용하므로 한쪽에만 몰리지 않는다. 남는 조각만 다시 순환한다.
            for (int i = 0; i < count; i++)
            {
                int edge = (firstEdge + i) % 4;
                float along = Random.Range(-0.72f, 0.72f);
                float edgeInset = Random.Range(0.82f, 0.94f);
                Vector3 position;

                switch (edge)
                {
                    case 0: // 위
                        position = new Vector3(along * halfWidth, edgeInset * halfHeight, 0f);
                        break;
                    case 1: // 오른쪽
                        position = new Vector3(edgeInset * halfWidth, along * halfHeight, 0f);
                        break;
                    case 2: // 아래
                        position = new Vector3(along * halfWidth, -edgeInset * halfHeight, 0f);
                        break;
                    default: // 왼쪽
                        position = new Vector3(-edgeInset * halfWidth, along * halfHeight, 0f);
                        break;
                }

                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
                {
                    position = position
                };
                frost.Emit(emit, 1);
            }
        }
    }
}
