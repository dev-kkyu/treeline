using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 애니메이션 이벤트를 <see cref="Clinger"/> 로 넘기는 중계기. <b>Animator 와 같은
    /// 오브젝트</b>(자식 <c>Visual</c>)에 붙인다.
    /// </summary>
    /// <remarks>
    /// 이유는 <see cref="CrawlerAnimationEvents"/> 와 같다 — 애니메이션 이벤트는 Animator
    /// 가 붙은 GameObject 에서만 함수를 찾고, 부모는 보지 않는다. 못 찾아도 콘솔에는
    /// 「<c>has no receiver</c>」 만 남아서 원인이 안 보인다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ClingerAnimationEvents : MonoBehaviour
    {
        [Tooltip("비우면 부모에서 찾는다.")]
        [SerializeField] private Clinger owner;

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<Clinger>();
            }

            if (owner == null)
            {
                Debug.LogWarning($"{name}: 부모에 Clinger 가 없다. 애니메이션 이벤트가 아무 일도 하지 않는다.", this);
            }
        }

        /// <summary>`Unfold_Drop` 9f. 천장 고정 해제, 낙하 시작.</summary>
        public void AE_DropRelease()
        {
            if (owner != null)
            {
                owner.AE_DropRelease();
            }
        }

        /// <summary>`Land` 14f. 구를 끄고 캡슐과 Agent 를 켠다.</summary>
        public void AE_EnableGroundMode()
        {
            if (owner != null)
            {
                owner.AE_EnableGroundMode();
            }
        }

        /// <summary>`Attack_Underjaw` 12f. 다시 재서 한 번만 피해.</summary>
        public void AE_AttackHit()
        {
            if (owner != null)
            {
                owner.AE_AttackHit();
            }
        }

        /// <summary>`Death_Fall` 4f · `Death` 3f. 충돌체를 전부 끈다.</summary>
        public void AE_DeathRelease()
        {
            if (owner != null)
            {
                owner.AE_DeathRelease();
            }
        }
    }
}
