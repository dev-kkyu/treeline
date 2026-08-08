using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 플레이어가 죽는 것을 지켜보다가 재시작을 건다.
    /// 씬에 빈 오브젝트 하나로 두거나 플레이어 프리팹에 붙인다.
    /// </summary>
    /// <remarks>
    /// 체력은 플레이어 담당(A) 이 갖고 있고 재시작은 진행 담당(C) 이 한다. 둘을
    /// 잇는 방법으로 <b>이미 합의된 <see cref="IDamageable"/> 의
    /// <see cref="IDamageable.IsAlive"/> 를 보는 쪽</b>을 골랐다. 죽음 알림용
    /// 인터페이스를 새로 만들면 <c>Shared/</c> 를 고치는 것이고 그건 셋의 합의가
    /// 필요하다. 지금은 그것 없이 진행할 수 있다.
    /// <para>
    /// A 가 나중에 이벤트로 밀어주는 쪽을 원하면 이 파일만 지우면 된다.
    /// 다른 코드는 <see cref="GameFlow.OnPlayerDied"/> 만 보고 있다.
    /// </para>
    /// </remarks>
    public sealed class PlayerDeathWatcher : MonoBehaviour
    {
        private IDamageable target;
        private bool reported;

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[PlayerDeathWatcher] Player 태그가 붙은 오브젝트가 없다.");
                enabled = false;
                return;
            }

            target = player.GetComponent<IDamageable>();
            if (target == null)
            {
                // A 의 체력 구현이 아직 안 붙은 상태다. 관통 확인에는 지장이 없으므로
                // 막지 않고 꺼두기만 한다.
                enabled = false;
            }
        }

        private void Update()
        {
            if (reported || target.IsAlive)
            {
                return;
            }

            reported = true;
            GameFlow.Instance.OnPlayerDied();
        }
    }
}
