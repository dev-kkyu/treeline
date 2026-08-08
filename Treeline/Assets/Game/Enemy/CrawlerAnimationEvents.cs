using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 애니메이션 이벤트를 <see cref="Crawler"/> 로 넘기는 중계기. <b>Animator 와 같은
    /// 오브젝트</b>(자식 <c>Visual</c>)에 붙인다.
    /// </summary>
    /// <remarks>
    /// IMPORTANT: 애니메이션 이벤트는 Animator 가 붙은 GameObject 의 컴포넌트에서만
    /// 함수를 찾는다. 부모에 있는 <see cref="Crawler"/> 는 절대 찾지 않는다.
    /// 이걸 모르면 이벤트가 조용히 아무 일도 하지 않고, 콘솔에도
    /// 「<c>AnimationEvent has no receiver</c>」 정도만 남아서 원인이 안 보인다.
    /// <para>
    /// 반대로 Animator 를 루트로 올리면 이 파일이 필요 없어지지만, 그때는 모델
    /// 교체가 루트 컴포넌트 구성을 건드린다. 중계기 하나가 더 싸다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CrawlerAnimationEvents : MonoBehaviour
    {
        [Tooltip("비우면 부모에서 찾는다.")]
        [SerializeField] private Crawler owner;

        private void Awake()
        {
            if (owner == null)
            {
                owner = GetComponentInParent<Crawler>();
            }

            if (owner == null)
            {
                Debug.LogWarning($"{name}: 부모에 Crawler 가 없다. 애니메이션 이벤트가 아무 일도 하지 않는다.", this);
            }
        }

        /// <summary>예고 정점. `Crawler_Attack_Underbite` 8f.</summary>
        public void AE_AttackTell()
        {
            if (owner != null)
            {
                owner.OnAttackTell();
            }
        }

        /// <summary>피해가 나가는 프레임. `Crawler_Attack_Underbite` 13f.</summary>
        public void AE_AttackHit()
        {
            if (owner != null)
            {
                owner.OnAttackHit();
            }
        }
    }
}
