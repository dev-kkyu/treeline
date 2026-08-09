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
        private const int TextureSize = 256;
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

            if (frostTexture != null)
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

            frostTexture = BuildFrostTexture();
            RawImage image = root.GetComponent<RawImage>();
            image.texture = frostTexture;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static Texture2D BuildFrostTexture()
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime_ScreenFrost",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[TextureSize * TextureSize];
            const float seed = 37.14f;

            for (int y = 0; y < TextureSize; y++)
            {
                float ny = (y / (TextureSize - 1f)) * 2f - 1f;
                for (int x = 0; x < TextureSize; x++)
                {
                    float nx = (x / (TextureSize - 1f)) * 2f - 1f;
                    float edge = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                    float edgeMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 1f, edge));

                    float broadNoise = Mathf.PerlinNoise(seed + x * 0.035f, seed + y * 0.035f);
                    float fineNoise = Mathf.PerlinNoise(seed * 2f + x * 0.11f, seed * 3f + y * 0.11f);
                    float brokenEdge = Mathf.SmoothStep(0.25f, 0.82f, broadNoise * 0.74f + fineNoise * 0.26f);

                    float radius = Mathf.Sqrt(nx * nx + ny * ny);
                    float angle = Mathf.Atan2(ny, nx);
                    float veinWave = Mathf.Abs(Mathf.Sin(angle * 8f + radius * 28f + broadNoise * 5f));
                    float veins = Mathf.Pow(1f - veinWave, 11f) * edgeMask;

                    float alpha = edgeMask * brokenEdge * 0.68f + veins * 0.32f;
                    alpha *= Mathf.SmoothStep(0f, 1f, edgeMask);
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 205f), 0, 205);
                    pixels[y * TextureSize + x] = new Color32(205, 230, 237, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
