using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Treeline
{
    /// <summary>
    /// 장 전환 · 사망 · 재시작을 관리한다. 게임에 하나만 존재한다.
    /// </summary>
    /// <remarks>
    /// <b>씬에 배치하지 않는다.</b> 첫 접근 때 스스로 생기고 씬이 바뀌어도 살아남는다.
    /// 씬에 두면 세 장 모두에 같은 오브젝트를 넣어야 하고, 고칠 일이 생길 때마다
    /// 병합할 수 없는 파일을 세 개씩 건드리게 된다. 씬 편집을 줄이는 것이
    /// 이 프로젝트에서는 그 자체로 이득이다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameFlow : MonoBehaviour
    {
        private const float DeathToRestartSeconds = 1.5f;

        private static GameFlow instance;

        private bool hasCheckpoint;
        private Vector3 checkpointPosition;
        private Quaternion checkpointRotation;
        private bool isTransitioning;
        private PlayerHealth watchedHealth;

        public static GameFlow Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[GameFlow]");
                    instance = go.AddComponent<GameFlow>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        /// <summary>지금 진행 중인 장의 씬 이름.</summary>
        public string CurrentChapter => SceneManager.GetActiveScene().name;

        /// <summary>장을 옮기거나 재시작하는 중인지. 트리거가 두 번 걸리는 것을 막는다.</summary>
        public bool IsTransitioning => isTransitioning;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>첫 장부터 시작한다. 타이틀 화면의 「시작」이 부른다.</summary>
        public void StartNewGame()
        {
            hasCheckpoint = false;
            LoadChapter(Chapters.First);
        }

        /// <summary>다음 장으로 넘어간다. 마지막 장이면 엔딩으로 간다.</summary>
        public void NextChapter()
        {
            LoadChapter(Chapters.Next(CurrentChapter));
        }

        /// <summary>
        /// 지금 장을 다시 시작한다. 붙잡혀 죽었을 때 부른다.
        /// 체크포인트를 지났으면 거기서, 아니면 장 시작 지점에서 다시 시작한다.
        /// </summary>
        public void RestartChapter()
        {
            LoadChapter(CurrentChapter);
        }

        /// <summary>
        /// 다시 시작할 위치를 갱신한다. <see cref="Checkpoint"/> 가 부른다.
        /// </summary>
        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            hasCheckpoint = true;
            checkpointPosition = position;
            checkpointRotation = rotation;
        }

        /// <summary>
        /// 플레이어가 죽었다. 잠깐 두고 나서 재시작한다.
        /// 즉시 재시작하면 무엇에 죽었는지 볼 새가 없다.
        /// </summary>
        public void OnPlayerDied()
        {
            if (isTransitioning)
            {
                return;
            }

            isTransitioning = true;
            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(DeathToRestartSeconds);

            isTransitioning = false;
            RestartChapter();
        }

        private void LoadChapter(string sceneName)
        {
            if (isTransitioning)
            {
                return;
            }

            // 장이 바뀌면 체크포인트는 버린다. 되돌아갈 수 없는 게임이라
            // 이전 장의 위치는 다시 쓸 일이 없다.
            if (sceneName != CurrentChapter)
            {
                hasCheckpoint = false;
            }

            isTransitioning = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            // WebGL 은 스레드를 못 쓰므로 씬 로드는 코루틴으로 기다린다.
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                // 빌드 씬 목록에 없는 이름이다. 등록을 잊었을 때 여기로 온다.
                Debug.LogError($"[GameFlow] 씬 '{sceneName}' 을 열 수 없다. " +
                               "Build Settings 의 씬 목록에 등록했는지 확인한다.");
                isTransitioning = false;
                yield break;
            }

            while (!op.isDone)
            {
                yield return null;
            }

            isTransitioning = false;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Chapters.IsChapter(scene.name))
            {
                return;
            }

            PlacePlayer();
            WatchPlayerDeath();
        }

        /// <summary>
        /// 새 장의 플레이어가 죽는 것을 듣는다.
        /// </summary>
        /// <remarks>
        /// 매 프레임 <c>IsAlive</c> 를 보던 것을 A 가 만든 <see cref="PlayerHealth.Died"/>
        /// 구독으로 바꿨다. 폴링은 A 의 구현을 기다리지 않으려고 쓴 임시 방편이었고,
        /// 이제 이벤트가 있으므로 그럴 이유가 없다.
        /// <para>
        /// 씬마다 <c>PlayerHealth</c> 가 새로 생기므로 장이 바뀔 때마다 다시 건다.
        /// 이전 것은 씬과 함께 사라져 따로 해제할 필요가 없지만, 같은 인스턴스에
        /// 두 번 걸리는 것을 막으려고 빼고 넣는다.
        /// </para>
        /// </remarks>
        private void WatchPlayerDeath()
        {
            if (watchedHealth != null)
            {
                watchedHealth.Died -= OnPlayerDied;
            }

            GameObject player = GameObject.FindWithTag("Player");
            watchedHealth = player != null ? player.GetComponent<PlayerHealth>() : null;

            if (watchedHealth == null)
            {
                // A 의 Player.prefab 이 아직 안 놓인 씬이다. 관통 확인에는 지장이 없다.
                return;
            }

            watchedHealth.Died += OnPlayerDied;
        }

        /// <summary>
        /// 플레이어를 시작 위치로 옮긴다.
        /// 체크포인트가 있으면 거기, 없으면 씬의 <see cref="SpawnPoint"/> 다.
        /// </summary>
        private void PlacePlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[GameFlow] 씬에 Player 태그가 붙은 오브젝트가 없다.");
                return;
            }

            Vector3 position;
            Quaternion rotation;

            if (hasCheckpoint)
            {
                position = checkpointPosition;
                rotation = checkpointRotation;
            }
            else
            {
                SpawnPoint spawn = FindFirstObjectByType<SpawnPoint>();
                if (spawn == null)
                {
                    // 씬에 배치된 자리를 그대로 쓴다. 회색 박스 단계에서는 이래도 된다.
                    return;
                }

                position = spawn.transform.position;
                rotation = spawn.transform.rotation;
            }

            Teleport(player, position, rotation);
        }

        /// <summary>
        /// <c>CharacterController</c> 는 자기가 위치를 물고 있어서 <c>transform</c> 만
        /// 바꾸면 다음 프레임에 되돌아간다. 껐다 옮기고 다시 켠다.
        /// </summary>
        private static void Teleport(GameObject target, Vector3 position, Quaternion rotation)
        {
            var controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            target.transform.SetPositionAndRotation(position, rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }
        }
    }
}
