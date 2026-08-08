using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Treeline.EnemyEditor
{
    /// <summary>
    /// 1단계용 <c>Crawler.prefab</c> 조립과 샌드박스 준비를 메뉴 두 개로 만든다.
    /// </summary>
    /// <remarks>
    /// 프리팹은 C 에게 넘기는 계약물이라 경로 · 이름 · 레이어가 정확해야 하는데,
    /// 손으로 만들면 매번 틀릴 수 있는 자리다. 회색 박스 단계에서만 쓰고
    /// 실제 모델이 들어오면 프리팹을 직접 편집한다.
    /// </remarks>
    public static class CrawlerSetup
    {
        private const string PrefabPath = "Assets/Game/Enemy/Crawler.prefab";

        [MenuItem("Tools/Treeline/B — 적/Crawler 프리팹 만들기")]
        public static void BuildCrawlerPrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            GameObject root = new GameObject("Crawler")
            {
                layer = GameLayers.Enemy
            };

            try
            {
                // 낮게 깔린 실루엣. 회색 박스 단계의 값이고 모델이 들어오면 맞춘다.
                CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
                collider.height = 0.9f;
                collider.radius = 0.4f;
                collider.center = new Vector3(0f, 0.45f, 0f);

                NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.4f;
                agent.height = 0.9f;
                agent.speed = 5.5f;          // 「빠르다」. 플레이어보다 빨라야 쫓기는 느낌이 난다
                agent.angularSpeed = 720f;
                agent.acceleration = 20f;
                agent.stoppingDistance = 1.2f;

                root.AddComponent<Crawler>();

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.layer = GameLayers.Enemy;
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                body.transform.localScale = new Vector3(0.8f, 0.45f, 0.8f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
                if (!ok)
                {
                    Debug.LogError($"프리팹 저장 실패: {PrefabPath}");
                    return;
                }

                AssetDatabase.Refresh();
                Debug.Log($"만들었다: {PrefabPath} (layer={LayerMask.LayerToName(GameLayers.Enemy)})");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [MenuItem("Tools/Treeline/B — 적/샌드박스 준비 (바닥 · NavMesh · 임시 플레이어)")]
        public static void PrepareSandbox()
        {
            if (!EditorUtility.DisplayDialog(
                    "샌드박스 준비",
                    "지금 열려 있는 씬에 바닥 · NavMeshSurface · 임시 플레이어를 추가한다.\n" +
                    "자기 샌드박스 씬이 맞는지 확인해라. 저장은 직접 해야 한다.",
                    "진행", "취소"))
            {
                return;
            }

            if (GameObject.Find("Sandbox_Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Sandbox_Ground";
                ground.transform.localScale = new Vector3(4f, 1f, 4f);
                Undo.RegisterCreatedObjectUndo(ground, "샌드박스 바닥");

                NavMeshSurface surface = ground.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;

                // IMPORTANT: 기본값(전부 포함)으로 구우면 씬에 놓인 플레이어와 적 캡슐까지
                // 걷는 면이 된다. 원점 샘플이 바닥이 아니라 캡슐 윗면(y≈0.9)으로 잡히고
                // 경로 계산이 조용히 실패한다. 캐릭터와 트리거는 빼고 굽는다.
                surface.layerMask = ~((1 << GameLayers.Player)
                                    | (1 << GameLayers.ViewModel)
                                    | (1 << GameLayers.Enemy)
                                    | (1 << GameLayers.Trigger));
                surface.BuildNavMesh();
                PersistNavMesh(surface);
            }

            // A 의 Player.prefab 이 오기 전까지 쓸 대역. 태그만 맞으면 Crawler 가 쫓는다.
            if (GameObject.FindWithTag("Player") == null)
            {
                GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                stand.name = "TEMP_Player (A 프리팹 오면 지운다)";
                stand.tag = "Player";
                stand.layer = GameLayers.Player;
                stand.transform.position = new Vector3(0f, 1f, 6f);
                Undo.RegisterCreatedObjectUndo(stand, "임시 플레이어");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"{PrefabPath} 가 없다. 「Crawler 프리팹 만들기」를 먼저 실행해라.");
            }
            else if (Object.FindFirstObjectByType<Crawler>() == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(instance, "Crawler 배치");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("샌드박스 준비 끝. Play 로 확인하고 마음에 들면 Ctrl+S.");
        }

        /// <summary>
        /// 구운 <see cref="NavMeshData"/> 를 별도 에셋으로 떼어낸다.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: 이걸 안 하면 NavMesh 데이터가 씬 안에 묻히고, **씬 파일 전체가
        /// 바이너리로 저장된다.** Force Text 로 설정해 두어도 그렇다. 커밋 기록이
        /// 제출물인 프로젝트에서 씬 diff 가 통째로 사라지고, 팀원과 병합할 수도 없어진다.
        /// </remarks>
        private static void PersistNavMesh(NavMeshSurface surface)
        {
            if (surface.navMeshData == null || AssetDatabase.Contains(surface.navMeshData))
            {
                return;
            }

            string sceneDir = Path.GetDirectoryName(EditorSceneManager.GetActiveScene().path).Replace("\\", "/");
            string dir = sceneDir + "/NavMesh";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder(sceneDir, "NavMesh");
            }

            string name = Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().path);
            AssetDatabase.CreateAsset(surface.navMeshData, $"{dir}/Sandbox-{name}.asset");
            AssetDatabase.SaveAssets();
        }
    }
}
