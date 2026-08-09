using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Treeline
{
    /// <summary>
    /// 카메라 앞에 화면을 덮는 삼각형 하나를 두고 <c>Treeline/HeightFog</c> 로 칠한다.
    /// 바닥에 깔리는 안개를 이 씬에서만 낸다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IMPORTANT: <b>렌더러 기능(Renderer Feature)으로 붙이지 않는다.</b> URP 렌더러
    /// 에셋은 여섯 장이 함께 쓰므로 거기 넣으면 남의 장에도 안개가 낀다. 씬에 놓인
    /// 오브젝트로 두면 켜고 끄는 주체가 씬이 된다.
    /// </para>
    /// <para>
    /// 정점 셋짜리 메시를 코드로 만든다. 클립 공간 <c>[-1, 3]</c> 이라 어떤 화면비
    /// 에서도 화면을 덮고, 사각형과 달리 대각선 이음매에서 두 번 칠하지 않는다.
    /// 셰이더가 트랜스폼을 통째로 무시하므로 <b>이 오브젝트의 위치는 그림에 영향을
    /// 주지 않는다</b> — 카메라 자식으로 두는 것은 프러스텀 컬링에 잘리지 않게
    /// 하려는 것뿐이고, 바운즈도 그래서 크게 잡는다.
    /// </para>
    /// <para>
    /// 값은 전부 머티리얼에 있다. <see cref="MaterialPropertyBlock"/> 을 쓰지 않는
    /// 이유는 <b>SRP Batcher 가 그 값을 무시</b>하기 때문이다. 달 방향처럼 씬마다
    /// 다른 값도 머티리얼을 따로 두어 맞춘다.
    /// </para>
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HeightFog : MonoBehaviour
    {
        [Tooltip("Treeline/HeightFog 셰이더를 쓰는 머티리얼. 씬마다 하나씩 둔다.")]
        [SerializeField] private Material fogMaterial;

        [Tooltip("깊이 텍스처를 요구할 카메라. 비우면 부모에서 찾고, 없으면 Camera.main.")]
        [SerializeField] private Camera targetCamera;

        private Mesh fullscreenTriangle;

        private void OnEnable()
        {
            var filter = GetComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();

            fullscreenTriangle = BuildFullscreenTriangle();
            filter.sharedMesh = fullscreenTriangle;

            if (fogMaterial != null)
            {
                renderer.sharedMaterial = fogMaterial;
            }

            // 그림자를 주고받을 것이 아니다. 켜 두면 그림자 패스에도 한 번씩 들어간다.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;

            RequestDepthTexture();
        }

        private void OnDisable()
        {
            if (fullscreenTriangle != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(fullscreenTriangle);
                }
                else
                {
                    DestroyImmediate(fullscreenTriangle);
                }
                fullscreenTriangle = null;
            }
        }

        /// <summary>
        /// 깊이 없이는 안개가 어디까지 낄지 알 수 없다. URP 에셋에 켜져 있어도
        /// 카메라 쪽에서 한 번 더 요구해 둔다 — 에셋은 여섯 장이 함께 쓰는 것이라
        /// 누군가 끄면 이 씬만 조용히 망가진다.
        /// </summary>
        private void RequestDepthTexture()
        {
            Camera camera = targetCamera;
            if (camera == null)
            {
                camera = GetComponentInParent<Camera>();
            }
            if (camera == null)
            {
                camera = Camera.main;
            }
            if (camera == null)
            {
                Debug.LogWarning($"[{nameof(HeightFog)}] 카메라를 못 찾았다. 깊이 텍스처를 요구하지 못한다.", this);
                return;
            }

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.requiresDepthTexture = true;
            }
        }

        private static Mesh BuildFullscreenTriangle()
        {
            var mesh = new Mesh
            {
                name = "HeightFog Fullscreen Triangle",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(3f, -1f, 0f),
                    new Vector3(-1f, 3f, 0f),
                },
                triangles = new[] { 0, 1, 2 },
            };

            // 셰이더가 정점을 클립 공간으로 그대로 쓰므로 진짜 바운즈란 것이 없다.
            // 작게 두면 카메라가 어디를 보든 컬링에 잘려 안개가 통째로 사라진다.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            return mesh;
        }
    }
}
