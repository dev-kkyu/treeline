using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Treeline.EnemyEditor
{
    /// <summary>
    /// 회색 박스 <c>Colossus.prefab</c> 을 시트 실측값으로 조립한다.
    /// </summary>
    /// <remarks>
    /// 치수는 <c>docs/enemy-sheets/colossus-sheet.png</c> 를 그대로 옮긴 것이다.
    /// 회색 박스여도 **실루엣 규칙은 지킨다** — 최고점이 어깨 쌍봉이고 머리가 그 사이
    /// 골에 묻혀 밖에서 안 보인다. 이 형태가 「죽지 않는 상대」를 설명 없이 전달하므로
    /// 모델이 오기 전에도 틀리면 안 된다.
    /// </remarks>
    public static class ColossusSetup
    {
        private const string PrefabPath = "Assets/Game/Enemy/Colossus.prefab";

        // 시트 실측. Transform 스케일로 키우지 않는다.
        private const float Height = 4.50f;
        private const float ShoulderWidth = 2.64f;
        private const float TotalWidth = 3.44f;

        [MenuItem("Tools/Treeline/B — 적/Colossus 프리팹 만들기")]
        public static void BuildColossusPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            GameObject root = new GameObject("Colossus")
            {
                layer = GameLayers.Enemy
            };

            try
            {
                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.height = 4.2f;
                collider.radius = 1.1f;
                collider.center = new Vector3(0f, 2.1f, 0f);

                NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 1.3f;
                agent.height = 4.4f;
                agent.speed = 1.2f;          // 플레이어보다 반드시 느리다. 도망칠 수 있어야 한다
                agent.angularSpeed = 120f;   // 크고 둔하다. 제자리에서 홱 돌지 않는다
                agent.acceleration = 4f;
                agent.stoppingDistance = 2.2f;

                root.AddComponent<Colossus>();

                // 몸통 — 두꺼운 수직 덩어리. 앞으로 웅크린 자세라 z 가 조금 앞이다
                Box(root, "Torso", new Vector3(0f, 2.40f, 0.05f), new Vector3(2.00f, 2.40f, 1.50f));

                // 어깨 쌍봉 — 실루엣의 최고점. 이 둘이 4.50m 를 만든다
                float humpY = Height - 0.55f;
                Sphere(root, "ShoulderHump.L", new Vector3(-ShoulderWidth * 0.5f + 0.55f, humpY, 0f), 1.10f);
                Sphere(root, "ShoulderHump.R", new Vector3(ShoulderWidth * 0.5f - 0.55f, humpY, 0f), 1.10f);

                // 머리 — 쌍봉 사이 골에 묻는다. 어깨선 위로 절대 올리지 않는다
                Sphere(root, "Head(sunken)", new Vector3(0f, humpY - 0.45f, 0.25f), 0.70f);

                // 팔 — 무릎을 지나 바닥에 닿는다. 앞으로 뻗어 깊이 2.10m 의 앞쪽 끝을 만든다.
                // 총폭 3.44m 를 만드는 것도 이 둘이다
                float armX = TotalWidth * 0.5f - 0.25f;
                Box(root, "Arm.L", new Vector3(-armX, 1.90f, 0.80f), new Vector3(0.50f, 3.40f, 0.60f));
                Box(root, "Arm.R", new Vector3(armX, 1.90f, 0.80f), new Vector3(0.50f, 3.40f, 0.60f));

                // 다리 — 짧고 무겁다. 뒤로 빠져 깊이의 뒤쪽 끝을 만든다
                Box(root, "Leg.L", new Vector3(-0.60f, 0.80f, -0.50f), new Vector3(0.70f, 1.60f, 1.00f));
                Box(root, "Leg.R", new Vector3(0.60f, 0.80f, -0.50f), new Vector3(0.70f, 1.60f, 1.00f));

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
                if (!ok)
                {
                    Debug.LogError($"프리팹 저장 실패: {PrefabPath}");
                    return;
                }

                AssetDatabase.Refresh();
                Debug.Log($"만들었다: {PrefabPath}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void Box(GameObject parent, string name, Vector3 pos, Vector3 size)
        {
            Shape(PrimitiveType.Cube, parent, name, pos, size);
        }

        private static void Sphere(GameObject parent, string name, Vector3 pos, float diameter)
        {
            Shape(PrimitiveType.Sphere, parent, name, pos, Vector3.one * diameter);
        }

        /// <summary>보이기만 하는 부품. 콜라이더는 루트 캡슐 하나로 충분하다.</summary>
        private static void Shape(PrimitiveType type, GameObject parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = GameLayers.Enemy;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
        }
    }
}
