using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Treeline
{
    /// <summary>
    /// 시동을 걸면 정해진 길을 따라 스스로 나아가는 로터리 제설기. 눈을 치우고,
    /// 앞으로 나온 것을 갈아버린다. 5장의 「움직이는 엄폐물」이 이것이다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 4장이 「한곳에서 버틴다」였으므로 여기서는 <b>안전한 자리 자체가 움직인다.</b>
    /// 뒤에 붙어 있으면 안전하지만 느리고, 떨어지면 눈보라 속에 혼자 남는다. 그
    /// 두 가지가 이 장의 긴장이고, 그래서 <see cref="metersPerSecond"/> 는 걷는
    /// 속도보다 확실히 느려야 한다 — 빠르면 그냥 따라가는 연출이 된다.
    /// </para>
    /// <para>
    /// IMPORTANT: <b>갈아버리는 판정은 날 앞쪽 반구다.</b> 구 전체로 잡으면 뒤에
    /// 붙어 걷는 플레이어 옆의 적까지 죽어서 「뒤가 안전하다」가 아니라 「어디든
    /// 안전하다」가 된다. 뒤를 남겨 둬야 플레이어가 총을 쓴다.
    /// </para>
    /// <para>
    /// 치우는 눈은 <see cref="clears"/> 에 나열한다. 지오메트리를 깎지 않고 통째로
    /// 끄는 이유는, 눈더미가 <b>길을 막는 콜라이더</b>이기도 해서다 — 깎는 시늉을
    /// 하면 어디까지 지나갈 수 있는지가 애매해진다.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SnowPlow : MonoBehaviour
    {
        [Tooltip("이것이 켜져 있는 동안만 나아간다. 레버가 켜는 장치를 물린다.")]
        [SerializeField] private Device device;

        [Tooltip("지나갈 자리. 이 오브젝트의 자식 순서대로 간다.")]
        [SerializeField] private Transform path;

        [Tooltip("나아가는 속도. 걷는 속도보다 느려야 「뒤에 붙어 간다」가 된다.")]
        [SerializeField] private float metersPerSecond = 1.15f;

        [Tooltip("방향을 트는 속도.")]
        [SerializeField] private float turnDegreesPerSecond = 70f;

        [Header("가는 것")]
        [Tooltip("날의 자리. 여기 앞쪽에 든 것이 갈린다. 비우면 자기 자신을 쓴다.")]
        [SerializeField] private Transform blade;

        [Tooltip("날 앞으로 얼마까지 닿나.")]
        [SerializeField] private float grindReach = 2.3f;

        [Tooltip("한 번에 넣는 피해. Clinger 는 2 라 한 번에 갈린다.")]
        [SerializeField] private int grindDamage = 9;

        [Tooltip("같은 것을 연달아 갈지 않게 두는 간격.")]
        [SerializeField] private float grindInterval = 0.35f;

        [Header("치우는 눈")]
        [Tooltip("지나가며 끄는 눈더미들.")]
        [SerializeField] private GameObject[] clears;

        [Tooltip("이 거리 안에 들어오면 치운다.")]
        [SerializeField] private float clearRadius = 3.4f;

        [Header("소리")]
        [Tooltip("도는 동안 도는 소리. 켜지고 꺼지는 것만 여기서 한다.")]
        [SerializeField] private AudioSource engine;

        [Header("끝")]
        [Tooltip("마지막 자리에 닿았을 때.")]
        [SerializeField] private UnityEvent arrived;

        private readonly Collider[] hits = new Collider[12];
        private int next;
        private float nextGrindAt;
        private bool done;
        private bool running;

        /// <summary>길 끝까지 갔는지.</summary>
        public bool Arrived => done;

        private void Awake()
        {
            if (blade == null)
            {
                blade = transform;
            }

            SetEngine(false);
        }

        private void Update()
        {
            bool active = device != null && device.IsActive && !done;

            if (active != running)
            {
                running = active;
                SetEngine(active);
            }

            if (!active)
            {
                return;
            }

            Advance();
            Clear();
            Grind();
        }

        /// <summary>다음 자리 쪽으로 돌면서 나아간다.</summary>
        private void Advance()
        {
            if (path == null || next >= path.childCount)
            {
                Finish();
                return;
            }

            Vector3 target = path.GetChild(next).position;
            Vector3 flat = target - transform.position;
            flat.y = 0f;

            if (flat.sqrMagnitude < 0.36f)
            {
                next++;
                if (next >= path.childCount)
                {
                    Finish();
                }

                return;
            }

            Quaternion want = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, want, turnDegreesPerSecond * Time.deltaTime);

            // 방향을 다 틀기 전에도 나아간다. 제자리에서 도는 중장비는 어색하다.
            transform.position += transform.forward * (metersPerSecond * Time.deltaTime);
        }

        private void Finish()
        {
            done = true;
            running = false;
            SetEngine(false);
            arrived?.Invoke();
        }

        /// <summary>닿은 눈더미를 끈다.</summary>
        private void Clear()
        {
            if (clears == null)
            {
                return;
            }

            float squared = clearRadius * clearRadius;

            foreach (GameObject snow in clears)
            {
                if (snow == null || !snow.activeSelf)
                {
                    continue;
                }

                if ((snow.transform.position - blade.position).sqrMagnitude <= squared)
                {
                    snow.SetActive(false);
                }
            }
        }

        /// <summary>날 앞쪽에 든 것을 간다.</summary>
        private void Grind()
        {
            if (Time.time < nextGrindAt)
            {
                return;
            }

            Vector3 centre = blade.position + transform.forward * (grindReach * 0.5f);
            int found = Physics.OverlapSphereNonAlloc(
                centre, grindReach, hits, 1 << GameLayers.Enemy, QueryTriggerInteraction.Ignore);

            for (int index = 0; index < found; index++)
            {
                Collider hit = hits[index];
                if (hit == null)
                {
                    continue;
                }

                // 앞쪽 반구만. 뒤에 붙어 걷는 자리는 남겨 둬야 총을 쓴다.
                Vector3 towards = hit.bounds.center - blade.position;
                if (Vector3.Dot(towards, transform.forward) <= 0f)
                {
                    continue;
                }

                var target = hit.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                target.TakeDamage(new DamageInfo(
                    grindDamage, hit.ClosestPoint(blade.position), transform.forward, gameObject));

                nextGrindAt = Time.time + grindInterval;
            }
        }

        private void SetEngine(bool on)
        {
            if (engine == null)
            {
                return;
            }

            if (on)
            {
                engine.Play();
            }
            else
            {
                engine.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = blade != null ? blade : transform;
            Gizmos.color = new Color(0.9f, 0.35f, 0.25f, 0.5f);
            Gizmos.DrawWireSphere(origin.position + transform.forward * (grindReach * 0.5f), grindReach);

            if (path == null)
            {
                return;
            }

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            Vector3 from = transform.position;
            for (int index = 0; index < path.childCount; index++)
            {
                Vector3 to = path.GetChild(index).position;
                Gizmos.DrawLine(from, to);
                Gizmos.DrawWireCube(to, Vector3.one * 0.6f);
                from = to;
            }
        }
    }
}
