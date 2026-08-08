using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 체력 3칸. 회복 아이템은 없고 장이 바뀔 때만 <see cref="Restore"/> 로 회복한다.
    /// </summary>
    /// <remarks>
    /// 적이 때리는 것과 총이 적을 죽이는 것이 같은 모양이라
    /// <see cref="IDamageable"/> 을 플레이어도 그대로 쓴다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 3;

        [Tooltip("한 대 맞고 나서 잠시 무적. 없으면 한 프레임에 여러 번 맞아 즉사한다.")]
        [SerializeField] private float invulnerableSeconds = 0.8f;

        [Tooltip("사망하면 끈다. 입력 맵이 꺼지면서 이동 · 사격 · 상호작용이 함께 멈춘다.")]
        [SerializeField] private PlayerInputReader input;

        private float invulnerableUntil = float.NegativeInfinity;

        public int Health { get; private set; }

        public int MaxHealth => maxHealth;

        public bool IsAlive => Health > 0;

        /// <summary>남은 체력을 넘긴다. HUD 가 붙기 전까지는 구독자가 없다.</summary>
        public event Action<int> HealthChanged;

        /// <summary>사망. 재시작은 진행 담당(C)이 여기에 붙는다.</summary>
        public event Action Died;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            Health = Mathf.Max(1, maxHealth);
        }

        public void TakeDamage(in DamageInfo damage)
        {
            if (!IsAlive || Time.time < invulnerableUntil)
            {
                return;
            }

            // 붙잡히면 죽는다. 적은 큰 값을 보내 즉사시킬 수 있다.
            Health = Mathf.Max(0, Health - Mathf.Max(1, damage.Amount));
            invulnerableUntil = Time.time + invulnerableSeconds;
            HealthChanged?.Invoke(Health);

            if (IsAlive)
            {
                return;
            }

            if (input != null)
            {
                input.enabled = false;
            }

            Died?.Invoke();
        }

        /// <summary>장이 바뀔 때 회복한다.</summary>
        public void Restore()
        {
            Health = Mathf.Max(1, maxHealth);
            invulnerableUntil = float.NegativeInfinity;

            if (input != null)
            {
                input.enabled = true;
            }

            HealthChanged?.Invoke(Health);
        }
    }
}
