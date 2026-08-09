using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 화면에 서리가 낀다. <b>모서리에 박혀 사라지지 않는 층</b>과 그 위에
    /// <b>불규칙하게 맺혔다 녹는 층</b> 두 겹이다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 처음에는 화면 전체를 덮는 <c>RawImage</c> 한 장이었다. 그것은 「화면에 낀
    /// 서리」가 아니라 <b>UI 에 띄운 그림</b>으로 보인다 — 언제나 같은 무늬가 같은
    /// 자리에 나타나고, 고개를 돌려도 따라오지 않으며, 알파만 오르내린다.
    /// </para>
    /// <para>
    /// 두 겹으로 나눈 이유가 있다. 전부 맺혔다 사라지게 하면 <b>서리가 없는
    /// 순간</b>이 생겨 화면이 맨얼굴로 돌아간다 — 눈보라 속인데 그렇다. 모서리
    /// 층은 계속 남아 「여기는 추운 곳」을 붙들고, 랜덤 층이 그 위에서 변화를 낸다.
    /// </para>
    /// <para>
    /// IMPORTANT: 결정의 <b>중심을 화면 밖으로</b> 민다. 중심이 화면 안에 있으면
    /// 눈 결정 아이콘을 붙여 놓은 것처럼 보이고, 밖으로 밀면 가지 끝만 들어와
    /// 유리 가장자리에서 자란 살얼음이 된다.
    /// </para>
    /// <para>
    /// IMPORTANT: 효과를 <b>실제로 그리는 카메라</b>에 붙여야 한다. 이 게임은
    /// 뷰모델 전용 카메라가 따로 있고 화각이 다르다 (본 70° · 뷰모델 50°).
    /// 본 카메라 화각으로 자리를 잡았더니 가장자리에 둘렀다고 생각한 조각들이
    /// 화면 한가운데 뭉쳤다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ScreenFrostParticles : MonoBehaviour
    {
        private const float RoofCheckDistance = 40f;
        private const string MaterialPath = "Effects/MAT_ScreenFrost";

        private static ScreenFrostParticles instance;

        [Tooltip("모서리에 박혀 남아 있는 서리.")]
        [SerializeField] private ParticleSystem rim;

        [Tooltip("불규칙하게 맺혔다 녹는 서리.")]
        [SerializeField] private ParticleSystem bursts;

        [Tooltip("서리를 그리는 카메라. 화면 크기를 여기서 잰다.")]
        [SerializeField] private Camera effectCamera;

        [Tooltip("지붕이 있는지 검사할 기준. 비우면 부모의 루트를 쓴다.")]
        [SerializeField] private Transform observer;

        [Tooltip("한 번에 맺히는 조각 수.")]
        [SerializeField] private Vector2Int burstCount = new Vector2Int(3, 6);

        [Tooltip("바깥에 있을 때 저절로 맺히는 간격(초).")]
        [SerializeField] private Vector2 autoInterval = new Vector2(4f, 9f);

        private float nextRoofCheck;
        private float nextBurst;
        private bool exposed;
        private bool suspended;
        private bool rimPlaced;

        /// <summary>서리를 한 번 강제로 맺는다. 파티클 충돌이나 연출에서 부른다.</summary>
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
            Clear(instance.bursts);
            Clear(instance.rim);
            instance.rimPlaced = false;
        }

        private static void Clear(ParticleSystem ps)
        {
            if (ps != null)
            {
                ps.Clear(true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// 카메라 앞에 서리 장치를 만들어 붙인다. 씬이나 프리팹을 고치지 않고
        /// 코드에서 짓는다 — 세 장에 같은 것을 손으로 넣지 않으려는 것이다.
        /// </summary>
        public static ScreenFrostParticles AttachTo(Camera camera, Transform observer)
        {
            if (camera == null)
            {
                return null;
            }

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
            // 근평면 바로 앞. 조금만 멀어도 벽이 서리를 가리고, 너무 가까우면 잘린다.
            root.transform.localPosition = new Vector3(0f, 0f, camera.nearClipPlane + 0.035f);
            root.layer = GameLayers.ViewModel;

            Material mat = Resources.Load<Material>(MaterialPath);

            ScreenFrostParticles effect = root.AddComponent<ScreenFrostParticles>();
            effect.effectCamera = camera;
            effect.observer = observer;
            effect.rim = BuildSystem(root.transform, "Rim", mat, true);
            effect.bursts = BuildSystem(root.transform, "Bursts", mat, false);
            return effect;
        }

        /// <param name="persistent">
        /// 참이면 맺힌 채로 남는다. 수명을 아주 길게 두고 <b>알파 변화를 걸지
        /// 않는다</b> — 수명이 길면 페이드 곡선이 몇 시간에 걸쳐 진행돼 있으나 마나다.
        /// </param>
        private static ParticleSystem BuildSystem(Transform parent, string name, Material mat, bool persistent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = GameLayers.ViewModel;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.startLifetime = persistent
                ? new ParticleSystem.MinMaxCurve(100000f)
                : new ParticleSystem.MinMaxCurve(2.4f, 5f);
            main.startSpeed = 0f;                    // 맺히는 것이지 날아가지 않는다
            // 이 숫자는 미터가 아니라 사실상 화면 비율이다. 카메라에서 7 cm 앞이라
            // 보이는 세로가 8 cm 뿐이고, 0.03 을 줬을 때 조각 하나가 화면의 3분의 1을
            // 덮었다. 모서리 층은 크게 잡아 밖에서 안으로 뻗어 들어오게 한다.
            main.startSize = persistent
                ? new ParticleSystem.MinMaxCurve(0.014f, 0.030f)
                : new ParticleSystem.MinMaxCurve(0.007f, 0.016f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            // 텍스처 자체가 꽤 파랗다. 곱해지는 색을 회백색 쪽으로 당기고 알파를
            // 크게 낮춘다 — 진하면 유리에 낀 서리가 아니라 붙여 놓은 장식이 된다.
            main.startColor = persistent
                ? new ParticleSystem.MinMaxGradient(
                    new Color(0.72f, 0.78f, 0.84f, 0.20f), new Color(0.88f, 0.92f, 0.96f, 0.34f))
                : new ParticleSystem.MinMaxGradient(
                    new Color(0.74f, 0.80f, 0.86f, 0.16f), new Color(0.90f, 0.94f, 0.98f, 0.30f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 96;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;   // Emit 으로만 뿌린다. 흘리면 「계속 끼는 중」이 된다

            // 자리는 Emit 에서 화면 비율을 재어 직접 준다. Rectangle 방출은 화면
            // 비율을 모르고 가운데 작은 영역에 겹쳐 뭉친다.
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = false;

            if (!persistent)
            {
                // 맺히고 → 머물고 → 녹는다.
                ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.10f),
                        new GradientAlphaKey(0.92f, 0.55f),
                        new GradientAlphaKey(0f, 1f)
                    });
                col.color = new ParticleSystem.MinMaxGradient(g);

                // 자라나야 「끼는 중」으로 보인다. 다 자란 채 나타나면 붙여 놓은 것 같다.
                ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.3f), new Keyframe(0.22f, 1f), new Keyframe(1f, 1f)));
            }

            ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.alignment = ParticleSystemRenderSpace.Local;
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pr.sortingOrder = persistent ? 200 : 201;
            if (mat != null)
            {
                pr.sharedMaterial = mat;
            }

            return ps;
        }

        /// <summary>주어진 레이어를 실제로 그리는 카메라를 찾는다.</summary>
        private static Camera FindLayerCamera(Camera preferred, int layer)
        {
            if (preferred == null)
            {
                return null;
            }

            int mask = 1 << layer;
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

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;

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
            if (suspended)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= nextRoofCheck)
            {
                nextRoofCheck = now + 1f;
                exposed = IsExposedToSky();
            }

            if (!exposed)
            {
                return;
            }

            // 모서리 층은 처음 바깥에 나갔을 때 한 번만 깐다. 그 뒤로는 남아 있다.
            if (!rimPlaced)
            {
                PlaceRim();
            }

            if (now >= nextBurst)
            {
                Burst();
            }
        }

        /// <summary>위로 아무것도 없으면 바깥이다. 실내에서는 저절로 끼지 않는다.</summary>
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

        /// <summary>화면 절반 크기. 카메라 화각과 비율에서 낸다.</summary>
        private Vector2 HalfExtents()
        {
            float distance = Mathf.Max(0.01f, transform.localPosition.z);
            float fov = effectCamera != null ? effectCamera.fieldOfView : 60f;
            float halfHeight = effectCamera != null && effectCamera.orthographic
                ? effectCamera.orthographicSize
                : Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * distance;
            float aspect = effectCamera != null ? effectCamera.aspect : 16f / 9f;
            return new Vector2(halfHeight * aspect, halfHeight);
        }

        /// <summary>네 모서리에 큰 결정을 박는다. 중심은 화면 밖이다.</summary>
        private void PlaceRim()
        {
            if (rim == null)
            {
                return;
            }

            rimPlaced = true;
            Vector2 half = HalfExtents();

            // 네 귀퉁이는 두 겹, 네 변은 한 겹. 귀퉁이가 더 두껍게 얼어붙는다.
            // 조각을 작게 줄인 대신 수를 늘려야 「가장자리가 얼었다」로 읽힌다.
            float[] cx = { -1f, 1f, -1f, 1f, -1f, 1f, -1f, 1f, 0f, 0f, -1f, 1f };
            float[] cy = { 1f, 1f, -1f, -1f, 1f, 1f, -1f, -1f, 1f, -1f, 0f, 0f };
            for (int i = 0; i < cx.Length; i++)
            {
                // 1 을 넘겨 화면 밖으로 민다. 가지 끝만 들어온다.
                float outX = cx[i] * half.x * Random.Range(1.02f, 1.16f);
                float outY = cy[i] * half.y * Random.Range(1.02f, 1.16f);
                // 변 가운데 것은 나머지 축을 흩뜨려 규칙적으로 안 보이게 한다.
                if (Mathf.Approximately(cx[i], 0f)) outX = Random.Range(-0.6f, 0.6f) * half.x;
                if (Mathf.Approximately(cy[i], 0f)) outY = Random.Range(-0.6f, 0.6f) * half.y;

                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(outX, outY, 0f)
                };
                rim.Emit(emit, 1);
            }
        }

        private void Burst()
        {
            suspended = false;
            nextBurst = Time.unscaledTime + Random.Range(autoInterval.x, autoInterval.y);

            if (bursts == null)
            {
                return;
            }

            Vector2 half = HalfExtents();
            int count = Mathf.Max(2, Random.Range(burstCount.x, burstCount.y + 1));
            for (int i = 0; i < count; i++)
            {
                // 가장자리에 치우치게 뽑는다. 한가운데 끼면 앞이 안 보여 답답하기만 하다.
                float x = Random.value < 0.5f
                    ? Random.Range(-1.05f, -0.45f) * half.x
                    : Random.Range(0.45f, 1.05f) * half.x;
                float y = Random.Range(-1.02f, 1.02f) * half.y;
                if (Random.value < 0.45f)
                {
                    // 절반쯤은 위아래 변에서 나오게 축을 바꾼다.
                    float t = x; x = y * (half.x / Mathf.Max(0.0001f, half.y)); y = t * (half.y / Mathf.Max(0.0001f, half.x));
                }

                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(x, y, 0f)
                };
                bursts.Emit(emit, 1);
            }
        }
    }
}
