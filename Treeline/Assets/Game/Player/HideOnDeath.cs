using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 죽는 순간 <b>1 인칭 팔 · 총 · HUD 를 화면에서 걷어낸다.</b>
    /// </summary>
    /// <remarks>
    /// 죽는 연출은 카메라가 넘어가거나 화면이 닫히는 동안 이어지는데, 그 위에 팔과
    /// 게이지가 그대로 떠 있으면 <b>플레이어가 아직 조작하고 있는 화면</b>으로 읽힌다.
    /// 6 장에서는 손아귀에 잡힌 몸을 보고 있는데 그 앞으로 자기 팔이 겹쳐 뜬다.
    /// <para>
    /// 체력이 다해 죽든 물려 죽든 <see cref="PlayerHealth.Died"/> 하나로 모인다.
    /// 붙잡히는 연출처럼 <b>죽기 전부터</b> 걷어내야 하는 곳은
    /// <see cref="Hide"/> 를 직접 부른다 — 6 장 처형은 잡히는 7f 와 죽는 89f 사이가
    /// 2.7 초라, 죽을 때까지 기다리면 그 동안 팔이 떠 있다.
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
                health.Died += Hide;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= Hide;
            }
        }

        /// <summary>지금 걷어낸다. 죽기 전에 조작을 뺏는 연출이 직접 부른다.</summary>
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

            PlayerHud hud = GetComponentInChildren<PlayerHud>(true);
            if (hud != null)
            {
                hud.HideForCutscene();
            }
        }
    }
}
