using UnityEditor;
using UnityEngine;

namespace Treeline.LevelEditorTools
{
    /// <summary>
    /// 복도 모듈이 쓰는 URP 머티리얼 세트. 텍스처 없이 색과 표면값만으로 만든다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 회색 박스가 싸구려로 보이는 진짜 이유는 폴리곤이 적어서가 아니라 **모든 면이
    /// 같은 회색이고 같은 광택**이어서다. 면마다 색과 매끄러움이 다르면 손전등이
    /// 훑을 때 재질이 갈려 보이고, 그것만으로 형태가 읽힌다.
    /// </para>
    /// <para>
    /// 색은 세 갈래로 나눈다 — 벽은 차가운 회녹색, 바닥은 더 어둡고 푸른 콘크리트,
    /// 금속은 밝고 매끄럽다. 여기에 녹 하나를 강조색으로 섞는다. 어두운 장면에서
    /// 색상 대비는 밝기 대비보다 훨씬 싸게 먹힌다.
    /// </para>
    /// <para>
    /// IMPORTANT: Smoothness 를 0.5 이상으로 올리는 것은 금속뿐이다. 벽까지 반들거리면
    /// 손전등 반사가 화면을 덮어 어둠이 사라진다.
    /// </para>
    /// </remarks>
    public static class CorridorMaterials
    {
        public const string Dir = "Assets/Game/Levels/Corridor/Materials";

        public const string Wall = "M_Corr_Wall";
        public const string Floor = "M_Corr_Floor";
        public const string Ceiling = "M_Corr_Ceiling";
        public const string Trim = "M_Corr_Trim";
        public const string Metal = "M_Corr_Metal";
        public const string Pipe = "M_Corr_Pipe";
        public const string Housing = "M_Corr_Housing";
        public const string Tube = "M_Corr_Tube";
        public const string Fabric = "M_Corr_Fabric";
        public const string Rust = "M_Corr_Rust";
        public const string Ice = "M_Corr_Ice";
        public const string Signal = "M_Corr_Signal";
        public const string Hazard = "M_Corr_Hazard";

        [MenuItem("Tools/Treeline/B — 2장/복도 머티리얼 만들기")]
        public static void BuildMaterials()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
            {
                AssetDatabase.CreateFolder("Assets/Game/Levels/Corridor", "Materials");
            }

            //     이름         색(sRGB)                 금속   매끄러움  발광
            Make(Wall, new Color32(0x54, 0x5B, 0x57, 255), 0.00f, 0.22f);
            Make(Floor, new Color32(0x33, 0x38, 0x3E, 255), 0.00f, 0.14f);
            Make(Ceiling, new Color32(0x3E, 0x43, 0x42, 255), 0.00f, 0.18f);
            // IMPORTANT: 사물함과 침대틀은 **도장 강철**이다. 맨 금속이 아니다.
            // metallic 을 높게 주면 손전등을 정면으로 받는 면이 통째로 하얗게 날아가
            // 형태가 사라진다. 도장면은 metallic 을 낮게, smoothness 를 중간으로 둔다.
            Make(Trim, new Color32(0x2A, 0x2E, 0x31, 255), 0.25f, 0.35f);
            Make(Metal, new Color32(0x5E, 0x65, 0x68, 255), 0.15f, 0.34f);
            Make(Pipe, new Color32(0x45, 0x4A, 0x4D, 255), 0.35f, 0.30f);
            Make(Housing, new Color32(0x8A, 0x8F, 0x8C, 255), 0.30f, 0.30f);
            Make(Fabric, new Color32(0x4A, 0x42, 0x3C, 255), 0.00f, 0.05f);
            Make(Rust, new Color32(0x6B, 0x3B, 0x24, 255), 0.20f, 0.20f);

            // 얼음 — 이 복도에서 유일하게 푸르고 반들거리는 것이다. 나머지가 전부
            // 거친 무광이라 손전등이 닿는 순간 「여기만 얼었다」가 바로 읽힌다.
            // smoothness 0.88 로 시작했다가 0.62 로 내렸다. 너무 매끄러우면 손전등
            // 반사가 한 점에 몰려 하얗게 날아가고, 얼음이 아니라 흰 구멍으로 보인다.
            Make(Ice, new Color32(0xA8, 0xC8, 0xD8, 255), 0.00f, 0.62f);

            // 위험 표시 — 배전함 전면. 노랑은 이 복도에 없는 색이라 눈이 먼저 간다.
            Make(Hazard, new Color32(0xC9, 0xA1, 0x22, 255), 0.10f, 0.35f);

            // 대기 표시등 — 광원 없이 발광 머티리얼로만. 어둠 속에서 배전함의 위치를
            // 알려주는 유일한 단서라 이것만은 확실히 눈에 띄어야 한다.
            Material signal = Make(Signal, new Color32(0xFF, 0x6A, 0x1E, 255), 0.00f, 0.40f);
            signal.EnableKeyword("_EMISSION");
            signal.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            signal.SetColor("_EmissionColor", new Color(3.2f, 0.85f, 0.15f));

            // 형광등 관 — 꺼져 있어도 형태가 읽히도록 아주 약하게만 발광한다.
            // 광원이 아니라 발광 머티리얼이라 WebGL 에서 공짜다.
            Material tube = Make(Tube, new Color32(0xC7, 0xD2, 0xDA, 255), 0.00f, 0.60f);
            tube.EnableKeyword("_EMISSION");
            tube.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            tube.SetColor("_EmissionColor", new Color(0.10f, 0.12f, 0.14f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"복도 머티리얼 10종을 {Dir} 에 만들었다.");
        }

        /// <summary>이름으로 머티리얼을 가져온다. 없으면 null 이다.</summary>
        public static Material Get(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{Dir}/{name}.mat");
        }

        private static Material Make(string name, Color color, float metallic, float smoothness)
        {
            string path = $"{Dir}/{name}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (m == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null)
                {
                    Debug.LogError("URP Lit 셰이더를 못 찾았다. 렌더 파이프라인 설정을 확인해라.");
                    return null;
                }

                m = new Material(lit);
                AssetDatabase.CreateAsset(m, path);
            }

            m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
