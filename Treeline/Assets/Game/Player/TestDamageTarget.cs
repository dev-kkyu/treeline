using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 사격 검증용 표적 대역. **적이 아니다.**
    /// </summary>
    /// <remarks>
    /// 적은 B 담당이고 `Assets/Game/Enemy/` 에 온다. 이것은 A 가 자기 샌드박스에서
    /// 레이캐스트 · 마스크 · 피해 전달이 실제로 도는지 보려고 두는 물건이라
    /// `Player/` 에 있고 이름에 Test 가 붙어 있다. `Crawler.prefab` 이 오면 지워도 된다.
    ///
    /// 레이어는 <c>Enemy</c>(12) 로 둬야 <see cref="GameLayers.Shot"/> 에 잡힌다.
    /// B 의 구간이지만 새로 만드는 것이 아니라 이미 합의된 번호를 쓰는 것뿐이다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TestDamageTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 3;

        [Tooltip("맞을 때마다 콘솔에 남긴다. 자동 검증에서 읽는다.")]
        [SerializeField] private bool logHits = true;

        [Tooltip("살아 있을 때 색.")]
        [SerializeField] private Color aliveColor = new Color(0.55f, 0.62f, 0.58f);

        [Tooltip("맞았을 때 잠깐 바뀌는 색.")]
        [SerializeField] private Color hitColor = new Color(0.9f, 0.35f, 0.25f);

        [Tooltip("죽었을 때 색.")]
        [SerializeField] private Color deadColor = new Color(0.18f, 0.18f, 0.2f);

        [SerializeField] private float hitFlashSeconds = 0.12f;

        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private float flashUntil = float.NegativeInfinity;
        private bool flashing;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>총알이 몇 번 닿았는지. 산탄총은 한 발에 여러 번 는다.</summary>
        public int HitCount { get; private set; }

        /// <summary>받은 피해 총량.</summary>
        public int DamageTaken { get; private set; }

        public int Health { get; private set; }

        public bool IsAlive => Health > 0;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            ResetTarget();
        }

        private void Update()
        {
            if (!flashing || Time.time < flashUntil)
            {
                return;
            }

            flashing = false;
            Tint(IsAlive ? aliveColor : deadColor);
        }

        public void TakeDamage(in DamageInfo damage)
        {
            if (!IsAlive)
            {
                return;
            }

            HitCount++;
            DamageTaken += damage.Amount;
            Health = Mathf.Max(0, Health - damage.Amount);

            if (logHits)
            {
                Debug.Log($"[TestDamageTarget] {name} 피격 {HitCount}회 " +
                          $"누적 {DamageTaken} 남은 체력 {Health} 지점 {damage.Point}", this);
            }

            if (!IsAlive)
            {
                Tint(deadColor);
                flashing = false;
                return;
            }

            Tint(hitColor);
            flashing = true;
            flashUntil = Time.time + hitFlashSeconds;
        }

        /// <summary>다시 세운다. 검증을 반복할 때 쓴다.</summary>
        public void ResetTarget()
        {
            Health = Mathf.Max(1, maxHealth);
            HitCount = 0;
            DamageTaken = 0;
            flashing = false;
            Tint(aliveColor);
        }

        private void Tint(Color color)
        {
            if (renderers == null)
            {
                return;
            }

            // MaterialPropertyBlock 이라 머티리얼 인스턴스를 만들지 않는다.
            // 표적을 여럿 두어도 드로우콜과 누수가 늘지 않는다.
            foreach (Renderer r in renderers)
            {
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
