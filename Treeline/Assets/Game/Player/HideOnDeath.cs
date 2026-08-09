using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 죽는 순간 화면을 정리한다. <b>어떻게 죽었는지에 따라 다르게 정리한다.</b>
    /// </summary>
    /// <remarks>
    /// 게이지가 그대로 떠 있으면 <b>플레이어가 아직 조작하고 있는 화면</b>으로 읽혀서
    /// 어느 쪽이든 HUD 는 걷는다. 갈리는 것은 팔이다.
    /// <list type="bullet">
    /// <item><description><b>체력이 다해 죽으면</b> — 팔을 남기고 쓰러지는 동작을
    /// 재생한다. 죽는 것을 보여 주는 것이 이 1.5 초의 목적이고, 1 인칭에서 그것을
    /// 할 수 있는 것은 팔뿐이다.</description></item>
    /// <item><description><b>붙잡혀 죽으면</b>(6 장) — 팔과 총을 걷어낸다. 손아귀에
    /// 잡힌 몸을 보고 있는데 그 앞으로 자기 팔이 겹친다.</description></item>
    /// </list>
    /// <para>
    /// 두 죽음이 모두 <see cref="PlayerHealth.Died"/> 로 오지만, 처형은 <b>죽기
    /// 2.7 초 전</b>(잡히는 7f)에 <see cref="Hide"/> 를 직접 부른다. 그 뒤에 오는
    /// <c>Died</c> 는 이미 걷어낸 것을 보고 아무것도 하지 않는다.
    /// </para>
    /// <para>
    /// 되돌리는 함수를 두지 않았다. 여기서 빠져나오는 길은 <b>장을 다시 시작하는 것</b>
    /// 뿐이고 그때 플레이어가 새로 만들어진다 — <see cref="PlayerHud.HideForCutscene"/>
    /// 와 같은 이유다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class HideOnDeath : MonoBehaviour
    {
        private PlayerHealth health;
        private bool hidden;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        /// <summary>체력이 다해 죽었다. HUD 만 걷고 팔은 쓰러지게 둔다.</summary>
        private void OnDied()
        {
            if (hidden)
            {
                // 처형이 이미 팔까지 걷어간 뒤다. 없는 팔을 쓰러뜨릴 것은 없다.
                return;
            }

            HideHud();

            ArmsAnimator arms = GetComponentInChildren<ArmsAnimator>(true);
            if (arms != null)
            {
                arms.PlayDeath();
            }
        }

        /// <summary>팔까지 통째로 걷어낸다. 죽기 전에 조작을 뺏는 연출이 직접 부른다.</summary>
        public void Hide()
        {
            if (hidden)
            {
                return;
            }

            hidden = true;

            // 팔은 오브젝트째 끈다. 렌더러만 끄면 총구 섬광 · 예광탄이 남는다.
            ArmsAnimator arms = GetComponentInChildren<ArmsAnimator>(true);
            if (arms != null)
            {
                arms.gameObject.SetActive(false);
            }

            foreach (PlayerWeapon weapon in GetComponentsInChildren<PlayerWeapon>(true))
            {
                weapon.gameObject.SetActive(false);
            }

            HideHud();
        }

        private void HideHud()
        {
            PlayerHud hud = GetComponentInChildren<PlayerHud>(true);
            if (hud != null)
            {
                hud.HideForCutscene();
            }
        }
    }
}
