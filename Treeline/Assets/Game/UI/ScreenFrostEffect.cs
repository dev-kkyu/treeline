using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 눈보라에 노출되면 화면 가장자리에 서리가 잠깐 맺혔다 녹는다.
    /// 위쪽으로 지붕을 검사하므로 실내에서는 자동 발생하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFrostEffect : MonoBehaviour
    {
        private const int TextureWidth = 512;
        private const int TextureHeight = 288;
        private const string FrostResourcePath = "Effects/ScreenFrostShards";
        private const float RoofCheckDistance = 40f;
        private const float FadeSpeed = 0.7f;

        private static ScreenFrostEffect instance;

        private CanvasGroup frostGroup;
        private Transform observer;
        private Texture2D frostTexture;
        private float nextRoofCheck;
        private float nextFrostBurst;
        private float holdUntil;
        private float fadeUntil;
        private float peakAlpha;
        private bool exposed;
        private bool suspended;
        private bool ownsFrostTexture;

        public static ScreenFrostEffect AttachTo(Transform hudRoot, Transform player)
        {
            ScreenFrostEffect effect = hudRoot.GetComponent<ScreenFrostEffect>();
            if (effect == null)
            {
                effect = hudRoot.gameObject.AddComponent<ScreenFrostEffect>();
            }

            effect.observer = player;
            return effect;
        }

        /// <summary>파티클 충돌이나 연출 이벤트에서 서리를 강제로 한 번 발생시킨다.</summary>
        public static void SnowHit()
        {
            if (instance != null)
            {
                instance.TriggerFrost();
            }
        }

        public static void HideImmediate()
        {
            if (instance != null && instance.frostGroup != null)
            {
                instance.frostGroup.alpha = 0f;
                instance.holdUntil = 0f;
                instance.fadeUntil = 0f;
                instance.suspended = true;
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
            BuildOverlay();
            nextFrostBurst = Time.unscaledTime + Random.Range(4f, 8f);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (ownsFrostTexture && frostTexture != null)
            {
                Destroy(frostTexture);
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

            if (exposed && now >= nextFrostBurst)
            {
                TriggerFrost();
            }

            float target = 0f;
            if (now < holdUntil)
            {
                target = peakAlpha;
            }
            else if (now < fadeUntil)
            {
                float duration = Mathf.Max(0.01f, fadeUntil - holdUntil);
                target = peakAlpha * (1f - Mathf.Clamp01((now - holdUntil) / duration));
            }

            float speed = target > frostGroup.alpha ? 2.8f : FadeSpeed;
            frostGroup.alpha = Mathf.MoveTowards(frostGroup.alpha, target, speed * Time.unscaledDeltaTime);
        }

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

        private void TriggerFrost()
        {
            if (suspended)
            {
                return;
            }

            float now = Time.unscaledTime;
            peakAlpha = Random.Range(0.28f, 0.46f);
            holdUntil = now + Random.Range(0.45f, 0.8f);
            fadeUntil = holdUntil + Random.Range(4.5f, 7f);
            nextFrostBurst = now + Random.Range(9f, 17f);

            RectTransform rect = frostGroup.transform as RectTransform;
            if (rect != null)
            {
                rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(-2.5f, 2.5f));
                rect.localScale = Random.value > 0.5f
                    ? new Vector3(-1.03f, 1.03f, 1f)
                    : new Vector3(1.03f, 1.03f, 1f);
            }
        }

        private void BuildOverlay()
        {
            GameObject root = new GameObject("ScreenFrost", typeof(RectTransform), typeof(CanvasGroup), typeof(RawImage));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(transform, false);
            root.transform.SetAsFirstSibling();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-18f, -18f);
            rect.offsetMax = new Vector2(18f, 18f);

            frostGroup = root.GetComponent<CanvasGroup>();
            frostGroup.alpha = 0f;
            frostGroup.interactable = false;
            frostGroup.blocksRaycasts = false;

            frostTexture = Resources.Load<Texture2D>(FrostResourcePath);
            if (frostTexture == null)
            {
                // 에셋이 빠진 개발 브랜치에서도 효과 자체가 죽지는 않게 이전 절차형을
                // 비상 대체재로 둔다. 정상 빌드에서는 이 경로를 타지 않는다.
                frostTexture = BuildFrostTexture();
                ownsFrostTexture = true;
                Debug.LogWarning($"{nameof(ScreenFrostEffect)}: {FrostResourcePath}를 찾지 못해 절차형 서리를 사용한다.", this);
            }
            RawImage image = root.GetComponent<RawImage>();
            image.texture = frostTexture;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static Texture2D BuildFrostTexture()
        {
            Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime_ScreenFrost",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[TextureWidth * TextureHeight];
            float aspect = TextureWidth / (float)TextureHeight;

            // x, y는 화면 비율, z는 결정 반경, w는 회전이다. 중심을 화면 밖으로 조금
            // 밀어 완전한 아이콘이 아니라 유리 가장자리에서 자란 살얼음처럼 보이게 한다.
            Vector4[] crystals =
            {
                new Vector4(-0.025f, 0.18f, 0.30f, 0.10f),
                new Vector4(0.035f, 0.76f, 0.23f, 0.43f),
                new Vector4(0.20f, 1.025f, 0.25f, 0.22f),
                new Vector4(0.73f, 1.035f, 0.31f, 0.50f),
                new Vector4(1.025f, 0.70f, 0.29f, 0.08f),
                new Vector4(1.018f, 0.17f, 0.24f, 0.35f),
                new Vector4(0.52f, -0.035f, 0.25f, 0.17f)
            };

            for (int y = 0; y < TextureHeight; y++)
            {
                float v = y / (TextureHeight - 1f);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float u = x / (TextureWidth - 1f);
                    Vector2 point = new Vector2(u * aspect, v);

                    float crystal = 0f;
                    for (int i = 0; i < crystals.Length; i++)
                    {
                        Vector4 spec = crystals[i];
                        Vector2 center = new Vector2(spec.x * aspect, spec.y);
                        crystal = Mathf.Max(crystal,
                            SampleIceCrystal(point, center, spec.z, spec.w));
                    }

                    // 얼룩을 만드는 넓은 노이즈는 거의 없애고, 테두리에만 얇은 얼음막을
                    // 남긴다. 결정 주변의 작은 입자가 가지 사이를 자연스럽게 연결한다.
                    float edgeDistance = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    float rim = 1f - Mathf.SmoothStep(0.008f, 0.055f, edgeDistance);
                    float grain = Mathf.PerlinNoise(31.7f + x * 0.075f, 18.2f + y * 0.075f);
                    float iceDust = Mathf.SmoothStep(0.72f, 0.94f, grain) *
                                    Mathf.SmoothStep(0.02f, 0.45f, crystal);

                    float alpha = Mathf.Clamp01(Mathf.Max(crystal, rim * (0.16f + grain * 0.14f)) + iceDust * 0.16f);
                    byte a = (byte)Mathf.RoundToInt(alpha * 235f);

                    // 굵은 중심맥은 희고, 가는 주변 가지는 살짝 푸른 살얼음색이다.
                    byte red = (byte)Mathf.RoundToInt(Mathf.Lerp(194f, 232f, crystal));
                    byte green = (byte)Mathf.RoundToInt(Mathf.Lerp(224f, 244f, crystal));
                    byte blue = (byte)Mathf.RoundToInt(Mathf.Lerp(233f, 248f, crystal));
                    pixels[y * TextureWidth + x] = new Color32(red, green, blue, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float SampleIceCrystal(Vector2 point, Vector2 center, float radius, float rotation)
        {
            Vector2 local = point - center;
            if (local.sqrMagnitude > radius * radius * 1.28f)
            {
                return 0f;
            }

            float core = 0f;
            float halo = 0f;
            float mainWidth = radius * 0.012f;
            float branchWidth = radius * 0.009f;

            for (int arm = 0; arm < 6; arm++)
            {
                float angle = rotation + arm * Mathf.PI / 3f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tip = direction * radius;

                float mainDistance = DistanceToSegment(local, Vector2.zero, tip);
                core = Mathf.Max(core, SharpLine(mainDistance, mainWidth));
                halo = Mathf.Max(halo, SharpLine(mainDistance, mainWidth * 3.6f) * 0.24f);

                // 바깥쪽을 향한 V자 잔가지를 세 단으로 붙인다. 가지 길이가 끝으로 갈수록
                // 짧아져 뭉툭한 별이 아니라 뾰족한 눈 결정 실루엣이 된다.
                for (int tier = 0; tier < 3; tier++)
                {
                    float along = 0.42f + tier * 0.19f;
                    float branchLength = radius * (0.25f - tier * 0.035f);
                    Vector2 joint = direction * radius * along;

                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 branchDirection = Rotate(direction, side * 0.68f);
                        Vector2 branchTip = joint + branchDirection * branchLength;
                        float branchDistance = DistanceToSegment(local, joint, branchTip);
                        core = Mathf.Max(core, SharpLine(branchDistance, branchWidth));
                        halo = Mathf.Max(halo, SharpLine(branchDistance, branchWidth * 3.2f) * 0.19f);
                    }
                }
            }

            float centerChip = 1f - Mathf.SmoothStep(radius * 0.025f, radius * 0.07f, local.magnitude);
            return Mathf.Clamp01(Mathf.Max(core, halo) + centerChip * 0.32f);
        }

        private static float SharpLine(float distance, float width)
        {
            return 1f - Mathf.SmoothStep(width, width * 1.8f, distance);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * t);
        }

        private static Vector2 Rotate(Vector2 value, float radians)
        {
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
