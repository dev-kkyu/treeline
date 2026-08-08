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
            Save(Section("CorridorSection_Doorway", Variant.Doorway));
            Save(HideRoom());

            AssetDatabase.Refresh();
            Debug.Log($"복도 모듈 3종을 {Dir} 에 만들었다.");
        }

        private enum Variant
        {
            Base,
            Lockers,
            Bunks,
            Doorway
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
            if (v == Variant.Doorway)
            {
                // 오른쪽 벽에 문틀만큼 구멍을 낸다. 벽을 앞뒤로 쪼개고 위에 인방을 얹는다
                float openW = 1.5f;
                float openH = 2.1f;
                float side = (Length - openW) * 0.5f;
                Box(root, "Wall_R_Front", new Vector3(hw + Thick * 0.5f, Height * 0.5f, -(Length - side) * 0.5f), new Vector3(Thick, Height, side), CorridorMaterials.Wall);
                Box(root, "Wall_R_Back", new Vector3(hw + Thick * 0.5f, Height * 0.5f, (Length - side) * 0.5f), new Vector3(Thick, Height, side), CorridorMaterials.Wall);
                Box(root, "Wall_R_Header", new Vector3(hw + Thick * 0.5f, (Height + openH) * 0.5f, 0f), new Vector3(Thick, Height - openH, openW), CorridorMaterials.Wall);
                Box(root, "DoorJamb_A", new Vector3(hw + 0.02f, openH * 0.5f, -openW * 0.5f), new Vector3(0.10f, openH, 0.10f), CorridorMaterials.Trim);
                Box(root, "DoorJamb_B", new Vector3(hw + 0.02f, openH * 0.5f, openW * 0.5f), new Vector3(0.10f, openH, 0.10f), CorridorMaterials.Trim);
            }
            else
            {
                Box(root, "Wall_R", new Vector3(hw + Thick * 0.5f, Height * 0.5f, 0f), new Vector3(Thick, Height, Length), CorridorMaterials.Wall);
            }

            // ── 걸레받이. 바닥과 벽이 만나는 선을 어둡게 끊는다.
            // 이 선 하나로 바닥이 벽에서 떨어져 보이고 복도에 깊이가 생긴다
            Box(root, "Skirting_L", new Vector3(-hw + 0.045f, 0.10f, 0f), new Vector3(0.09f, 0.20f, Length), CorridorMaterials.Trim);
            if (v != Variant.Doorway)
            {
                Box(root, "Skirting_R", new Vector3(hw - 0.045f, 0.10f, 0f), new Vector3(0.09f, 0.20f, Length), CorridorMaterials.Trim);
            }

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


        /// <summary>
        /// 복도 옆에 붙는 숙소 골방. <b>막다른 방이다.</b>
        /// </summary>
        /// <remarks>
        /// <para>
        /// 히터가 도는 22초 동안 <b>숨을 곳</b>이다. 기획에 「숨는다. 히터가 웅웅거리는
        /// 동안 복도를 돌아다니는 소리를 듣는다」고 적혀 있는데, 뻥 뚫린 복도에서는
        /// 그냥 서서 기다리는 시간이 된다. 몸을 낮춰 가릴 것이 있어야 그 22초가
        /// 긴장이 된다.
        /// </para>
        /// <para>
        /// IMPORTANT: <b>지나가는 길로 만들지 않는다.</b> 출구가 둘이 되면 기획의
        /// 「도망칠 길이 하나여야 한다」가 깨지고, 미로에서 길 찾다 죽는 장이 된다.
        /// 들어온 문으로만 나간다.
        /// </para>
        /// <para>
        /// 원점은 복도 쪽 문지방 한가운데다. 문틀 칸의 <c>(hw, 0, 0)</c> 에 맞춰 놓으면
        /// 방이 복도 오른쪽으로 뻗는다.
        /// </para>
        /// </remarks>
        private static GameObject HideRoom()
        {
            GameObject root = new GameObject("HideRoom");

            const float W = 4.2f;   // 복도에서 뻗어나가는 깊이 (x)
            const float D = 5.0f;   // 복도와 나란한 폭 (z)
            const float H = 2.8f;

            float cx = W * 0.5f;    // 방 중심

            Box(root, "Floor", new Vector3(cx, -Thick * 0.5f, 0f), new Vector3(W, Thick, D), CorridorMaterials.Floor);
            Box(root, "Ceiling", new Vector3(cx, H + Thick * 0.5f, 0f), new Vector3(W, Thick, D), CorridorMaterials.Ceiling);
            Box(root, "Wall_Far", new Vector3(W + Thick * 0.5f, H * 0.5f, 0f), new Vector3(Thick, H, D), CorridorMaterials.Wall);
            Box(root, "Wall_A", new Vector3(cx, H * 0.5f, -(D + Thick) * 0.5f), new Vector3(W, H, Thick), CorridorMaterials.Wall);
            Box(root, "Wall_B", new Vector3(cx, H * 0.5f, (D + Thick) * 0.5f), new Vector3(W, H, Thick), CorridorMaterials.Wall);

            // 복도 쪽 벽 — 문틀 양옆만 남긴다
            float openW = 1.5f;
            float side = (D - openW) * 0.5f;
            Box(root, "Wall_Near_A", new Vector3(0f, H * 0.5f, -(D - side) * 0.5f), new Vector3(Thick, H, side), CorridorMaterials.Wall);
            Box(root, "Wall_Near_B", new Vector3(0f, H * 0.5f, (D - side) * 0.5f), new Vector3(Thick, H, side), CorridorMaterials.Wall);

            Box(root, "Skirting_Far", new Vector3(W - 0.045f, 0.10f, 0f), new Vector3(0.09f, 0.20f, D), CorridorMaterials.Trim);
            Box(root, "Skirting_A", new Vector3(cx, 0.10f, -D * 0.5f + 0.045f), new Vector3(W, 0.20f, 0.09f), CorridorMaterials.Trim);
            Box(root, "Skirting_B", new Vector3(cx, 0.10f, D * 0.5f - 0.045f), new Vector3(W, 0.20f, 0.09f), CorridorMaterials.Trim);

            // 꺼진 형광등 하나 — 방에도 이정표를 준다
            Box(root, "LightHousing", new Vector3(cx, H - 0.06f, 0f), new Vector3(1.30f, 0.08f, 0.30f), CorridorMaterials.Housing);
            Box(root, "LightTube", new Vector3(cx, H - 0.12f, 0f), new Vector3(1.20f, 0.05f, 0.20f), CorridorMaterials.Tube);

            // ── 가릴 것들. 문에서 안 보이는 안쪽 구석이 숨는 자리다
            Box(root, "Bunk_Frame", new Vector3(W - 0.05f, 0.9f, -1.2f), new Vector3(0.08f, 1.8f, 2.0f), CorridorMaterials.Metal);
            Box(root, "Bunk_Lower", new Vector3(W - 0.45f, 0.42f, -1.2f), new Vector3(0.80f, 0.10f, 2.0f), CorridorMaterials.Metal);
            Box(root, "Bunk_Upper", new Vector3(W - 0.45f, 1.32f, -1.2f), new Vector3(0.80f, 0.10f, 2.0f), CorridorMaterials.Metal);
            Box(root, "Mattress", new Vector3(W - 0.45f, 0.51f, -1.2f), new Vector3(0.74f, 0.14f, 1.90f), CorridorMaterials.Fabric);

            // 넘어진 사물함 — 문에서 사선으로 보이지 않는 자리를 만든다
            Box(root, "Locker_Fallen", new Vector3(2.4f, 0.21f, 1.7f), new Vector3(1.70f, 0.42f, 0.78f), CorridorMaterials.Metal);
            Box(root, "Locker_Standing", new Vector3(W - 0.25f, 0.9f, 1.9f), new Vector3(0.42f, 1.8f, 0.78f), CorridorMaterials.Metal);

            // 책상과 의자 — 사람이 살던 방으로 읽히게 한다
            Box(root, "Desk", new Vector3(1.1f, 0.72f, -D * 0.5f + 0.45f), new Vector3(1.60f, 0.06f, 0.70f), CorridorMaterials.Metal);
            Box(root, "DeskLeg_A", new Vector3(0.38f, 0.36f, -D * 0.5f + 0.45f), new Vector3(0.06f, 0.72f, 0.62f), CorridorMaterials.Trim);
            Box(root, "DeskLeg_B", new Vector3(1.82f, 0.36f, -D * 0.5f + 0.45f), new Vector3(0.06f, 0.72f, 0.62f), CorridorMaterials.Trim);
            Box(root, "Chair_Toppled", new Vector3(1.9f, 0.22f, -0.4f), new Vector3(0.50f, 0.44f, 0.50f), CorridorMaterials.Trim);

            // 끌린 매트리스 — 1장의 「끌린 자국」과 이어진다. 복도가 아니라 여기에 둔다
            Box(root, "Mattress_Dragged", new Vector3(1.5f, 0.08f, 0.9f), new Vector3(1.50f, 0.16f, 0.80f), CorridorMaterials.Fabric);

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
