using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 스케일한 큐브의 텍스처가 늘어나지 않게 타일링을 크기에서 계산한다.
    /// </summary>
    /// <remarks>
    /// 기본 큐브의 UV 는 면마다 0~1 이라 4m 로 늘리면 텍스처도 같이 늘어난다.
    /// 텍스처가 없을 때는 안 보이던 문제이고 <b>표면을 입히는 순간 드러난다.</b>
    /// 콘크리트는 「좀 뭉갰네」로 넘어가지만 <b>그레이팅 같은 규칙적 무늬는 줄무늬로
    /// 풀려 무엇인지 알 수 없게 된다.</b>
    /// <para>
    /// 지오메트리를 건드리지 않고 <see cref="MaterialPropertyBlock"/> 으로 넣으므로
    /// 프리팹도 머티리얼도 갈라지지 않는다. 크기가 다른 벽마다 머티리얼을 복제하면
    /// 나중에 색 하나 고칠 때 전부를 열어야 한다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>축 정렬된 상자에만 통한다.</b> 모서리를 깎거나 면을 뚫으면 새로
    /// 생긴 면의 UV 를 못 맞춘다. 2 장처럼 Blender 로 만든 것은 제대로 된 UV 를 갖고
    /// 들어오므로 이 컴포넌트가 필요 없다.
    /// </para>
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SurfaceTiling : MonoBehaviour
    {
        /// <summary>어느 두 축을 텍스처의 가로 · 세로로 쓸지.</summary>
        public enum Axes
        {
            /// <summary>가장 긴 두 축을 고른다. 납작한 판이면 이것으로 맞는다.</summary>
            Auto = 0,

            /// <summary>바닥 · 천장.</summary>
            XZ = 1,

            /// <summary>동서로 뻗은 벽.</summary>
            XY = 2,

            /// <summary>남북으로 뻗은 벽.</summary>
            ZY = 3,
        }

        [Tooltip("1m 당 텍스처가 몇 번 반복되나. 그레이팅 0.78, 콘크리트 · 눈 0.5.")]
        [SerializeField] private float perMeter = 0.5f;

        [Tooltip("텍스처의 가로 · 세로로 쓸 두 축. 보통 Auto 로 맞는다.")]
        [SerializeField] private Axes axes = Axes.Auto;

        [Tooltip("자식의 렌더러에도 각자 크기로 적용한다. 벽 수십 개에 하나씩 붙이지 않아도 된다.")]
        [SerializeField] private bool includeChildren = true;

        private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

        private MaterialPropertyBlock block;
        private int appliedHash;

        private void OnEnable()
        {
            appliedHash = 0;
            Apply();
        }

        private void Update()
        {
            // 편집 중에는 크기를 계속 바꾸므로 따라간다. 플레이 중에는 한 번이면 된다.
            if (!Application.isPlaying)
            {
                Apply();
            }
        }

        /// <summary>지금 크기로 다시 계산해 넣는다. 바깥에서 불러도 된다.</summary>
        public void Apply()
        {
            int hash = Signature();
            if (hash == appliedHash)
            {
                return;
            }

            appliedHash = hash;
            block ??= new MaterialPropertyBlock();

            if (includeChildren)
            {
                foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                {
                    ApplyTo(r);
                }

                return;
            }

            var self = GetComponent<Renderer>();
            if (self != null)
            {
                ApplyTo(self);
            }
        }

        private void ApplyTo(Renderer target)
        {
            Vector3 size = target.transform.lossyScale;
            Vector2 span = SpanOf(size);

            target.GetPropertyBlock(block);
            block.SetVector(BaseMapST, new Vector4(
                Mathf.Max(0.001f, span.x * perMeter),
                Mathf.Max(0.001f, span.y * perMeter),
                0f, 0f));
            target.SetPropertyBlock(block);
        }

        /// <summary>
        /// 텍스처의 가로 · 세로에 해당하는 실제 길이를 고른다.
        /// </summary>
        /// <remarks>
        /// <c>_BaseMap_ST</c> 는 <b>여섯 면에 똑같이</b> 적용된다. 바닥은 <c>x · z</c>,
        /// 동서 벽은 <c>x · y</c>, 남북 벽은 <c>z · y</c> 를 써야 하는데 셋을 동시에
        /// 맞출 수는 없다. 우리 회색 박스는 전부 납작한 판이므로 <b>가장 얇은 축을
        /// 버리면</b> 그 판이 실제로 보이는 두 면이 남는다.
        /// </remarks>
        private Vector2 SpanOf(Vector3 size)
        {
            switch (axes)
            {
                case Axes.XZ: return new Vector2(size.x, size.z);
                case Axes.XY: return new Vector2(size.x, size.y);
                case Axes.ZY: return new Vector2(size.z, size.y);
            }

            float x = Mathf.Abs(size.x), y = Mathf.Abs(size.y), z = Mathf.Abs(size.z);

            if (y <= x && y <= z)
            {
                return new Vector2(x, z);   // 납작한 바닥 · 천장
            }

            return x <= z
                ? new Vector2(z, y)         // 남북으로 선 벽
                : new Vector2(x, y);        // 동서로 선 벽
        }

        /// <summary>크기나 설정이 바뀌었는지 싸게 확인한다.</summary>
        private int Signature()
        {
            Vector3 s = transform.lossyScale;
            int hash = s.GetHashCode();
            hash = hash * 31 + perMeter.GetHashCode();
            hash = hash * 31 + (int)axes;
            hash = hash * 31 + (includeChildren ? 1 : 0);

            if (includeChildren)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    hash = hash * 31 + t.localScale.GetHashCode();
                }
            }

            return hash;
        }
    }
}
