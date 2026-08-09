using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Treeline
{
    /// <summary>
    /// 수소가 찬 공간. <b>이 부피를 지나는 총알 하나에 터진다.</b> 4장 축전지실이다.
    /// </summary>
    /// <remarks>
    /// 기획의 「여기서 쏘면 터진다. 총이 있는데 못 쓴다」가 이 컴포넌트다. 한 물건이
    /// 위협 · 제약 · 무기로 동시에 작동한다 — 지나가려면 위험하고, 총을 못 쓰게 만들고,
    /// 궁지에 몰리면 최후의 수단이 된다.
    /// <para>
    /// <b>가스 오브젝트에 붙인다.</b> 환기 팬이 그것을 끄면 이 컴포넌트도 같이 꺼져
    /// 위험이 사라진다. 「환기를 먼저 돌린다 / 그냥 들어간다」의 선택지가 켜고 끄는 것
    /// 하나로 떨어진다.
    /// </para>
    /// <para>
    /// IMPORTANT: 발사를 <see cref="PlayerWeapon.ShotFired"/> 로 듣는다. 탄 하나마다
    /// 총구에서 <b>실제로 멈춘 지점까지</b>의 선이 오므로, 안에서 쏘든 밖에서 안으로
    /// 쏘든 같은 판정 하나로 잡힌다. 트리거 콜라이더로 총알을 받으면 총알이 거기서
    /// 멈춰 버려 안 된다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class HydrogenPocket : MonoBehaviour
    {
        [Header("폭발이 닿는 범위")]
        [Tooltip("안에서 터뜨렸을 때만 이만큼 밖까지 휩쓴다. 밖에서 쏘면 방 안까지다.")]
        [SerializeField] private float blastMargin = 4f;

        [Header("피해")]
        [Tooltip("플레이어가 받는 칸. 체력 3 중 2 를 잃는다 — 쓸 수는 있되 공짜는 아니다.")]
        [SerializeField] private int playerDamage = 2;

        [Tooltip("적에게 주는 값. 무리를 통째로 쓸어야 하므로 크게 둔다.")]
        [SerializeField] private int enemyDamage = 999;

        [Header("터진 뒤")]
        [Tooltip("꺼질 것. 가스 표현과 날아간 축전지가 먹이던 조명이 여기 들어간다.")]
        [SerializeField] private GameObject[] disables;

        [Tooltip("켜질 것. 그을린 자국 같은 것.")]
        [SerializeField] private GameObject[] enables;

        [Header("연출")]
        [SerializeField] private ParticleSystem burst;
        [SerializeField] private AudioSource blast;

        [Tooltip("터지는 순간의 빛. 꺼둔 채로 두면 여기서 켜고 꺼준다.")]
        [SerializeField] private Light flash;

        [SerializeField] private float flashIntensity = 90f;

        [Tooltip("섬광이 남는 시간. 짧아야 번쩍인다.")]
        [SerializeField] private float flashSeconds = 0.45f;

        [Tooltip("화면 흔들림. 각도와 시간.")]
        [SerializeField] private float shakeDegrees = 3.4f;

        [SerializeField] private float shakeSeconds = 1.1f;

        [SerializeField] private UnityEvent exploded;

        private BoxCollider volume;
        private readonly List<PlayerWeapon> heard = new List<PlayerWeapon>();
        private bool spent;

        /// <summary>아직 터질 수 있는지. 환기로 뺐거나 이미 터졌으면 거짓이다.</summary>
        public bool IsCharged => !spent && isActiveAndEnabled;

        private void Awake()
        {
            volume = GetComponent<BoxCollider>();
            volume.isTrigger = true;
        }

        private void OnEnable()
        {
            Listen();
        }

        private void OnDisable()
        {
            Deafen();
        }

        private void Update()
        {
            // 플레이어가 늦게 생기거나 총을 나중에 줍는 경우가 있다. 조용히 안 터지는
            // 것보다 매 프레임 한 번 세는 편이 싸다.
            if (heard.Count == 0)
            {
                Listen();
            }
        }


        private void Listen()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return;
            }

            foreach (PlayerWeapon weapon in player.GetComponentsInChildren<PlayerWeapon>(true))
            {
                if (heard.Contains(weapon))
                {
                    continue;
                }

                weapon.ShotFired += OnShotFired;
                heard.Add(weapon);
            }
        }

        private void Deafen()
        {
            foreach (PlayerWeapon weapon in heard)
            {
                if (weapon != null)
                {
                    weapon.ShotFired -= OnShotFired;
                }
            }

            heard.Clear();
        }

        private void OnShotFired(Vector3 from, Vector3 to)
        {
            if (spent || !CrossesVolume(from, to))
            {
                return;
            }

            // 밖에서 안으로 쐈으면 방 안까지만 친다. 문간에서 겨눈 것이 복도의
            // 물건을 같이 날려 버리면, 이 방이 무기가 아니라 사고가 된다.
            Ignite(Contains(from));
        }

        /// <summary>밖에서도 터뜨릴 수 있게 열어 둔다. 불꽃 · 폭발 연쇄에 쓴다.</summary>
        public void Ignite()
        {
            Ignite(true);
        }

        private void Ignite(bool fromInside)
        {
            if (spent)
            {
                return;
            }

            spent = true;

            Vector3 center = transform.TransformPoint(volume.center);
            Blast(center, fromInside ? Mathf.Max(0f, blastMargin) : 0f);

            if (burst != null)
            {
                burst.transform.position = center;
                burst.Play();
            }

            if (blast != null)
            {
                blast.Play();
            }

            if (flash != null)
            {
                // 코루틴은 이 오브젝트가 꺼지면 죽는다. 빛 쪽에 붙여 끝까지 사그라들게 한다.
                FlashFade fade = flash.gameObject.GetComponent<FlashFade>();
                if (fade == null)
                {
                    fade = flash.gameObject.AddComponent<FlashFade>();
                }

                fade.Begin(flashIntensity, flashSeconds);
            }

            // 폭발이 몸으로 오게 한다. 소리와 빛만으로는 멀리서 터진 것처럼 읽힌다.
            GameObject who = GameObject.FindWithTag("Player");
            PlayerLook look = who != null ? who.GetComponentInChildren<PlayerLook>() : null;
            if (look == null && who != null)
            {
                look = who.GetComponentInParent<PlayerLook>();
            }

            if (look != null)
            {
                look.Shake(shakeDegrees, shakeSeconds);
            }

            exploded?.Invoke();

            SetAll(enables, true);
            SetAll(disables, false);

            // 가스가 사라졌으므로 이 오브젝트도 같이 꺼진다. disables 에 자기를 넣지
            // 않아도 되도록 여기서 한다 — 넣으면 위 두 줄 사이에 자기가 꺼져 헷갈린다.
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 범위 안의 모든 <see cref="IDamageable"/> 을 친다. 적은 쓸려 나가고 플레이어는
        /// 칸을 잃는다.
        /// </summary>
        private void Blast(Vector3 center, float margin)
        {
            Vector3 half = Vector3.Scale(volume.size * 0.5f, transform.lossyScale)
                           + Vector3.one * margin;

            var struck = new HashSet<IDamageable>();

            foreach (Collider hit in Physics.OverlapBox(center, half, transform.rotation,
                         ~0, QueryTriggerInteraction.Ignore))
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || !struck.Add(target))
                {
                    continue;
                }

                bool isPlayer = ((MonoBehaviour)target).gameObject.CompareTag("Player")
                                || hit.transform.root.CompareTag("Player");

                Vector3 at = hit.ClosestPoint(center);
                Vector3 away = at - center;
                if (away.sqrMagnitude < 0.0001f)
                {
                    away = Vector3.up;
                }

                target.TakeDamage(new DamageInfo(
                    isPlayer ? playerDamage : enemyDamage, at, away.normalized, gameObject));
            }
        }

        /// <summary>그 점이 가스 안인지. 「밖에서 쐈다」를 이걸로 가른다.</summary>
        private bool Contains(Vector3 point)
        {
            Vector3 local = transform.InverseTransformPoint(point) - volume.center;
            Vector3 half = volume.size * 0.5f;

            return Mathf.Abs(local.x) <= half.x
                   && Mathf.Abs(local.y) <= half.y
                   && Mathf.Abs(local.z) <= half.z;
        }

        /// <summary>선분이 이 상자를 지나는지. 상자 로컬로 옮겨 슬래브로 자른다.</summary>
        private bool CrossesVolume(Vector3 from, Vector3 to)
        {
            Vector3 a = transform.InverseTransformPoint(from) - volume.center;
            Vector3 b = transform.InverseTransformPoint(to) - volume.center;
            Vector3 half = volume.size * 0.5f;
            Vector3 d = b - a;

            float enter = 0f;
            float exit = 1f;

            for (int axis = 0; axis < 3; axis++)
            {
                float origin = a[axis];
                float delta = d[axis];
                float limit = half[axis];

                if (Mathf.Abs(delta) < 1e-6f)
                {
                    // 그 축으로는 안 움직인다. 밖에서 시작했으면 영영 안 들어온다.
                    if (origin < -limit || origin > limit)
                    {
                        return false;
                    }

                    continue;
                }

                float t1 = (-limit - origin) / delta;
                float t2 = (limit - origin) / delta;
                if (t1 > t2)
                {
                    float swap = t1;
                    t1 = t2;
                    t2 = swap;
                }

                enter = Mathf.Max(enter, t1);
                exit = Mathf.Min(exit, t2);

                if (enter > exit)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetAll(GameObject[] targets, bool active)
        {
            if (targets == null)
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider box = volume != null ? volume : GetComponent<BoxCollider>();
            if (box == null)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.9f, 0.9f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            Gizmos.DrawWireCube(box.center, box.size + Vector3.one * blastMargin * 2f);
        }
    }
}
