using UnityEditor;
using UnityEngine;

namespace Treeline.LevelEditorTools
{
    /// <summary>
    /// 2장 숙소동 복도 모듈. 4m 한 칸을 프리팹으로 만들고 이어 붙여 복도를 만든다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 모듈로 만드는 이유는 3인 단기 제작에서 통짜 레벨을 손으로 꾸미는 것이
    /// 끝나지 않기 때문이다. 한 칸을 잘 만들어 반복하면 3 · 4장에서 그대로 쓴다.
    /// </para>
    /// <para>
    /// 폴리곤을 늘리지 않고 **머티리얼과 트림**으로 때깔을 낸다. 걸레받이 · 패널
    /// 이음새 · 등 하우징은 전부 납작한 상자인데, 이것들이 면을 잘라 주어 손전등이
    /// 훑을 때 형태가 읽힌다. 넓은 민면 하나보다 훨씬 낫고 비용은 같다.
    /// </para>
    /// <para>
    /// IMPORTANT: 폭은 <c>Crawler</c> 보폭 1.52m 와 NavMesh 연결성 사이의 타협이다.
    /// 좁을수록 「비켜 갈 수 없다」가 형태로 전달되지만, 좌우 소품에 깎이고 나면
    /// 칸 경계에서 걷는 면이 끊긴다. 값을 바꿀 때는 반드시 다시 굽고
    /// 스폰에서 출구까지 <c>PathComplete</c> 가 나오는지 확인한다.
    /// </para>
    /// </remarks>
    public static class CorridorKit
    {
        private const string Dir = "Assets/Game/Levels/Corridor";

        // IMPORTANT: 2.4m 로 시작했다가 2.8m 로 넓혔다. 사물함(왼쪽)과 침대(오른쪽)가
        // 번갈아 나오면 걷는 띠가 좌우로 어긋나는데, 굽기 반경 0.5 로 깎이고 나면 두 띠가
        // 겹치지 않아 **칸 경계에서 NavMesh 가 끊긴다.** 좁은 것이 이 장의 설계지만
        // 길이 사라지면 설계가 아니라 버그다. 2.8m 는 겹침 0.7m 를 남기는 최소값이다.
        private const float Width = 2.8f;
        private const float Height = 2.8f;
        private const float Length = 4.0f;   // 한 칸
        private const float Thick = 0.15f;

        [MenuItem("Tools/Treeline/B — 2장/복도 모듈 만들기")]
        public static void BuildSections()
        {
            // IMPORTANT: Directory.CreateDirectory 와 같이 쓰지 않는다. 디스크에 먼저
            // 만들면 AssetDatabase 가 아직 모르는 상태라 IsValidFolder 가 false 를
            // 돌려주고, CreateFolder 가 "Corridor 1" 을 또 만든다.
            if (!AssetDatabase.IsValidFolder(Dir))
            {
                AssetDatabase.CreateFolder("Assets/Game/Levels", "Corridor");
            }

            if (CorridorMaterials.Get(CorridorMaterials.Wall) == null)
            {
                CorridorMaterials.BuildMaterials();
            }

            Save(Section("CorridorSection_Base", Variant.Base));
            Save(Section("CorridorSection_Lockers", Variant.Lockers));
            Save(Section("CorridorSection_Bunks", Variant.Bunks));

            AssetDatabase.Refresh();
            Debug.Log($"복도 모듈 3종을 {Dir} 에 만들었다.");
        }

        private enum Variant
        {
            Base,
            Lockers,
            Bunks
        }

        private static GameObject Section(string name, Variant v)
        {
            GameObject root = new GameObject(name);

            float hw = Width * 0.5f;
            float hl = Length * 0.5f;

            // ── 껍데기. 원점은 칸의 중심 바닥이다
            Box(root, "Floor", new Vector3(0f, -Thick * 0.5f, 0f), new Vector3(Width, Thick, Length), CorridorMaterials.Floor);
            Box(root, "Ceiling", new Vector3(0f, Height + Thick * 0.5f, 0f), new Vector3(Width, Thick, Length), CorridorMaterials.Ceiling);
            Box(root, "Wall_L", new Vector3(-hw - Thick * 0.5f, Height * 0.5f, 0f), new Vector3(Thick, Height, Length), CorridorMaterials.Wall);
            Box(root, "Wall_R", new Vector3(hw + Thick * 0.5f, Height * 0.5f, 0f), new Vector3(Thick, Height, Length), CorridorMaterials.Wall);

            // ── 걸레받이. 바닥과 벽이 만나는 선을 어둡게 끊는다.
            // 이 선 하나로 바닥이 벽에서 떨어져 보이고 복도에 깊이가 생긴다
            Box(root, "Skirting_L", new Vector3(-hw + 0.045f, 0.10f, 0f), new Vector3(0.09f, 0.20f, Length), CorridorMaterials.Trim);
            Box(root, "Skirting_R", new Vector3(hw - 0.045f, 0.10f, 0f), new Vector3(0.09f, 0.20f, Length), CorridorMaterials.Trim);

            // ── 패널 이음새. 칸 경계에 세로 홈을 넣어 4m 리듬이 읽히게 한다.
            // 반복이 눈에 띄는 것을 막는 게 아니라, 반복을 **의도된 것으로** 보이게 한다
            foreach (float z in new[] { -hl + 0.02f, hl - 0.02f })
            {
                Box(root, "Seam_L", new Vector3(-hw + 0.03f, Height * 0.5f, z), new Vector3(0.06f, Height, 0.05f), CorridorMaterials.Trim);
                Box(root, "Seam_R", new Vector3(hw - 0.03f, Height * 0.5f, z), new Vector3(0.06f, Height, 0.05f), CorridorMaterials.Trim);
            }

            // ── 천장 배관 두 줄. 손전등이 위를 훑을 때 깊이를 만든다
            Box(root, "Pipe_A", new Vector3(-0.55f, Height - 0.22f, 0f), new Vector3(0.16f, 0.16f, Length), CorridorMaterials.Pipe);
            Box(root, "Pipe_B", new Vector3(-0.32f, Height - 0.30f, 0f), new Vector3(0.10f, 0.10f, Length), CorridorMaterials.Pipe);

            // ── 형광등. 광원은 없고 하우징과 관만 둔다.
            // 관은 약하게 발광해서 꺼져 있어도 위치가 보인다 — 어둠 속의 이정표다
            Box(root, "LightHousing", new Vector3(0.35f, Height - 0.06f, 0f), new Vector3(0.30f, 0.08f, 1.30f), CorridorMaterials.Housing);
            Box(root, "LightTube", new Vector3(0.35f, Height - 0.12f, 0f), new Vector3(0.20f, 0.05f, 1.20f), CorridorMaterials.Tube);

            switch (v)
            {
                case Variant.Lockers:
                    for (int i = 0; i < 5; i++)
                    {
                        float z = -hl + 0.45f + i * 0.82f;
                        Box(root, $"Locker_{i}", new Vector3(-hw + 0.22f, 0.9f, z), new Vector3(0.42f, 1.8f, 0.78f), CorridorMaterials.Metal);
                        // 손잡이 — 작지만 사물함을 사물함으로 만든다
                        Box(root, $"LockerHandle_{i}", new Vector3(-hw + 0.44f, 1.05f, z + 0.28f), new Vector3(0.04f, 0.16f, 0.04f), CorridorMaterials.Trim);
                    }
                    // 하나는 열려 있다. 반복이 눈에 띄지 않게 하는 가장 싼 방법이다
                    Box(root, "LockerDoor_Open", new Vector3(-hw + 0.62f, 0.9f, -hl + 1.27f), new Vector3(0.72f, 1.7f, 0.04f), CorridorMaterials.Metal);
                    Box(root, "Locker_Rust", new Vector3(-hw + 0.23f, 0.18f, -hl + 2.09f), new Vector3(0.44f, 0.36f, 0.80f), CorridorMaterials.Rust);
                    break;

                case Variant.Bunks:
                    for (int i = 0; i < 2; i++)
                    {
                        float z = -hl + 1.1f + i * 1.9f;
                        // 침대는 얕게 문다. 깊으면 맞은편 사물함과 합쳐져 길이 끊긴다
                        Box(root, $"BunkFrame_{i}", new Vector3(hw - 0.05f, 0.9f, z), new Vector3(0.08f, 1.8f, 1.70f), CorridorMaterials.Metal);
                        Box(root, $"BunkPlate_L_{i}", new Vector3(hw - 0.33f, 0.42f, z), new Vector3(0.62f, 0.10f, 1.70f), CorridorMaterials.Metal);
                        Box(root, $"BunkPlate_U_{i}", new Vector3(hw - 0.33f, 1.32f, z), new Vector3(0.62f, 0.10f, 1.70f), CorridorMaterials.Metal);
                        Box(root, $"Mattress_L_{i}", new Vector3(hw - 0.33f, 0.51f, z), new Vector3(0.58f, 0.14f, 1.60f), CorridorMaterials.Fabric);
                    }
                    // IMPORTANT: 바닥에 끌려 나온 매트리스를 여기 두지 않는다. 침대가
                    // 이미 오른쪽 x≥0.32 를 먹고 있어서, 가운데에 매트리스를 더 놓으면
                    // 남는 폭이 에이전트 반경 둘을 못 담아 **NavMesh 가 그 칸에서 끊긴다.**
                    // 통로가 좁은 것이 이 장의 설계라 소품으로 더 좁히면 길이 사라진다.
                    // 끌린 매트리스는 레벨에서 여유 있는 칸에 한 번만 놓는다.
                    break;
            }

            return root;
        }

        private static void Box(GameObject parent, string name, Vector3 pos, Vector3 size, string material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;

            Material m = CorridorMaterials.Get(material);
            if (m != null)
            {
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
            }

            // 정적 배칭 — 부품이 많아지므로 드로우콜을 묶어야 WebGL 에서 버틴다
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        private static void Save(GameObject root)
        {
            string path = $"{Dir}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
            if (!ok)
            {
                Debug.LogError("프리팹 저장 실패: " + path);
            }

            Object.DestroyImmediate(root);
        }
    }
}
