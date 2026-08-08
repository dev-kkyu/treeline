using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Treeline.LevelEditorTools
{
    /// <summary>
    /// NavMesh 를 굽고 결과를 반드시 별도 에셋으로 떼어낸다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// IMPORTANT: **스크립트로 구울 때는 이 함수만 쓴다.** <see cref="NavMeshSurface.BuildNavMesh"/>
    /// 를 직접 부르면 안 된다.
    /// </para>
    /// <para>
    /// 이유는 두 가지가 겹치기 때문이다. 첫째, 캐릭터를 포함해 구우면 플레이어와 적
    /// 캡슐이 걷는 면이 되어 경로 계산이 조용히 실패한다. 둘째, 구운
    /// <see cref="NavMeshData"/> 를 씬에 묻으면 <b>씬 파일 전체가 바이너리로
    /// 저장된다.</b> Asset Serialization 이 Force Text 여도 그렇다. 커밋 기록이
    /// 제출물인데 씬 diff 가 통째로 사라지고 병합도 불가능해진다.
    /// </para>
    /// <para>
    /// 특히 두 번째는 <b>한 번 떼어냈다고 끝이 아니다.</b> <c>BuildNavMesh</c> 는 부를
    /// 때마다 새 <see cref="NavMeshData"/> 를 만들므로, 다시 구우면 다시 묻힌다.
    /// 2026-08-08 에 이것으로 씬을 세 번 깨뜨렸다.
    /// </para>
    /// </remarks>
    public static class NavMeshBaking
    {
        /// <summary>캐릭터와 트리거를 뺀 굽기 마스크. 걷는 면이 될 수 없는 것들이다.</summary>
        public static int WalkableMask =>
            ~((1 << GameLayers.Player) | (1 << GameLayers.ViewModel)
            | (1 << GameLayers.Enemy) | (1 << GameLayers.Trigger));

        /// <summary>
        /// 굽고 별도 에셋으로 떼어낸다. 에셋은 <c>&lt;씬폴더&gt;/NavMesh/&lt;씬이름&gt;.asset</c> 이다.
        /// </summary>
        /// <param name="surface">구울 표면.</param>
        /// <param name="excludeWhileBaking">
        /// 굽는 동안 잠시 끌 것들. 문 · 셔터처럼 <b>열리고 닫히는 물건</b>을 넣는다.
        /// 세운 채로 구우면 구운 면에 구멍이 남아 열려도 지나갈 수 없다. 이런 물건은
        /// 대신 <see cref="NavMeshObstacle"/> 로 런타임에 파고들게 한다.
        /// </param>
        public static void BakeAndPersist(NavMeshSurface surface, params GameObject[] excludeWhileBaking)
        {
            if (surface == null)
            {
                Debug.LogError("NavMeshSurface 가 없다.");
                return;
            }

            var wasActive = new bool[excludeWhileBaking?.Length ?? 0];
            for (int i = 0; i < wasActive.Length; i++)
            {
                GameObject o = excludeWhileBaking[i];
                if (o == null)
                {
                    continue;
                }

                wasActive[i] = o.activeSelf;
                o.SetActive(false);
            }

            surface.collectObjects = CollectObjects.All;
            surface.layerMask = WalkableMask;
            surface.BuildNavMesh();

            for (int i = 0; i < wasActive.Length; i++)
            {
                if (excludeWhileBaking[i] != null)
                {
                    excludeWhileBaking[i].SetActive(wasActive[i]);
                }
            }

            Persist(surface);
        }

        /// <summary>구운 데이터를 씬 밖 에셋으로 옮긴다. 이미 에셋이면 아무것도 하지 않는다.</summary>
        public static void Persist(NavMeshSurface surface)
        {
            NavMeshData data = surface.navMeshData;
            if (data == null)
            {
                Debug.LogWarning("구운 데이터가 없다. 표면에 걸리는 지오메트리가 있는지 확인해라.");
                return;
            }

            if (AssetDatabase.Contains(data))
            {
                return;
            }

            string scenePath = EditorSceneManager.GetActiveScene().path;
            string sceneDir = Path.GetDirectoryName(scenePath).Replace("\\", "/");
            string dir = sceneDir + "/NavMesh";

            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder(sceneDir, "NavMesh");
            }

            // 같은 경로에 CreateAsset 하면 기존 것을 대체한다. 굽을 때마다 갱신되는 것이 맞다.
            AssetDatabase.CreateAsset(data, $"{dir}/{Path.GetFileNameWithoutExtension(scenePath)}.asset");
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 씬을 저장하기 전에 부른다. 떼어내지 않은 데이터가 남아 있으면 막는다.
        /// </summary>
        /// <returns>안전하면 true.</returns>
        public static bool VerifyBeforeSave()
        {
            bool safe = true;

            foreach (NavMeshSurface s in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
            {
                if (s.navMeshData != null && !AssetDatabase.Contains(s.navMeshData))
                {
                    Debug.LogError($"{s.name}: NavMesh 데이터가 씬에 묻혀 있다. 이대로 저장하면 씬이 바이너리가 된다.", s);
                    safe = false;
                }
            }

            return safe;
        }
    }
}
