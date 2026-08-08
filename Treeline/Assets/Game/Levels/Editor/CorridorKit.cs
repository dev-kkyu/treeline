using System.IO;
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
    /// 형태만 세우고 표면은 비워 둔다. 텍스처를 CC0 로 입히든 에셋스토어 팩에서
    /// 얹든 나중에 머티리얼만 갈아끼우면 된다. 에셋을 먼저 고르면 그 팩에 형태가
    /// 끌려간다.
    /// </para>
    /// <para>
    /// IMPORTANT: 폭 2.4m 는 <c>Crawler</c> 보폭 1.52m 를 전제로 한 값이다.
    /// 시트에 「복도에서 정면으로 마주치면 보폭이 통로 폭을 거의 채운다」고 적힌
    /// 그 폭이다. 넓히면 「비켜 갈 수 없다」가 형태로 전달되지 않는다.
    /// </para>
    /// </remarks>
    public static class CorridorKit
    {
        private const string Dir = "Assets/Game/Levels/Corridor";

        private const float Width = 2.4f;    // 통로 폭 — Crawler 가 거의 채운다
        private const float Height = 2.8f;
        private const float Length = 4.0f;   // 한 칸
        private const float Thick = 0.15f;

        [MenuItem("Tools/Treeline/B — 2장/복도 모듈 만들기")]
        public static void BuildSections()
        {
            Directory.CreateDirectory(Dir);
            if (!AssetDatabase.IsValidFolder(Dir))
            {
                AssetDatabase.CreateFolder("Assets/Game/Levels", "Corridor");
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

            // 껍데기 — 바닥 · 천장 · 벽 둘. 원점은 칸의 중심 바닥이다.
            Box(root, "Floor", new Vector3(0f, -Thick * 0.5f, 0f), new Vector3(Width, Thick, Length));
            Box(root, "Ceiling", new Vector3(0f, Height + Thick * 0.5f, 0f), new Vector3(Width, Thick, Length));
            Box(root, "Wall_L", new Vector3(-hw - Thick * 0.5f, Height * 0.5f, 0f), new Vector3(Thick, Height, Length));
            Box(root, "Wall_R", new Vector3(hw + Thick * 0.5f, Height * 0.5f, 0f), new Vector3(Thick, Height, Length));

            // 천장 배관 두 줄 — 어둠 속에서 손전등이 훑을 때 깊이를 만든다
            Box(root, "Pipe_A", new Vector3(-0.55f, Height - 0.22f, 0f), new Vector3(0.16f, 0.16f, Length));
            Box(root, "Pipe_B", new Vector3(-0.32f, Height - 0.30f, 0f), new Vector3(0.10f, 0.10f, Length));

            // 형광등 — 대부분 꺼져 있다. 라이트가 아니라 형태만 둔다.
            // 실제 광원은 손전등 · 비상등 · 깜빡이 하나로 제한한다 (WebGL)
            Box(root, "LightFixture", new Vector3(0.35f, Height - 0.08f, 0f), new Vector3(0.28f, 0.10f, 1.20f));

            switch (v)
            {
                case Variant.Lockers:
                    for (int i = 0; i < 5; i++)
                    {
                        float z = -hl + 0.45f + i * 0.82f;
                        Box(root, $"Locker_{i}", new Vector3(-hw + 0.22f, 0.9f, z), new Vector3(0.42f, 1.8f, 0.78f));
                    }
                    // 하나는 열려 있다. 반복이 눈에 띄지 않게 하는 가장 싼 방법이다
                    Box(root, "LockerDoor_Open", new Vector3(-hw + 0.62f, 0.9f, -hl + 1.27f), new Vector3(0.72f, 1.7f, 0.04f));
                    break;

                case Variant.Bunks:
                    for (int i = 0; i < 2; i++)
                    {
                        float z = -hl + 1.1f + i * 1.9f;
                        Box(root, $"Bunk_Lower_{i}", new Vector3(hw - 0.45f, 0.42f, z), new Vector3(0.86f, 0.16f, 1.70f));
                        Box(root, $"Bunk_Upper_{i}", new Vector3(hw - 0.45f, 1.32f, z), new Vector3(0.86f, 0.16f, 1.70f));
                        Box(root, $"BunkFrame_{i}", new Vector3(hw - 0.06f, 0.9f, z), new Vector3(0.08f, 1.8f, 1.70f));
                    }
                    // 매트리스 하나가 바닥에 끌려 나와 있다. 1장의 「끌린 자국」과 이어진다
                    Box(root, "Mattress_Dragged", new Vector3(0.25f, 0.09f, hl - 1.0f), new Vector3(0.80f, 0.18f, 1.60f));
                    break;
            }

            return root;
        }

        private static void Box(GameObject parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
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
