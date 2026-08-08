> 상태: 확정 · 최종 수정 2026-08-09

# Unity 연결 계약

이 문서는 애니메이션 제작 범위와 현재 코드 사이의 빈 연결부를 명시한다.
현재 `Colossus.prefab` 은 회색 박스이고 `Colossus.cs` 는 Animator 를 전혀
참조하지 않는다.

## FBX Import

- Rig: `Animation Type = Generic`, Avatar Definition = Create From This Model.
- Scale Factor 1, **Convert Units 켬**. Blender 가 FBX 를 cm 단위로 쓰기 때문에
  이 조합이 1:1 로 들어온다. Crawler 에서 확인했다.
- Skin Weights = 4 Bones. Import BlendShapes 끔.
- 모든 clip 의 Root Transform Rotation / Position 은 Bake Into Pose.
- Loop Time: `Idle_Weight` · `Walk_Knuckle` · `Run` 셋만 켬.
- Resample Curves 켬, Animation Compression 은 Optimal.
- **루프 클립은 복제 끝 프레임까지 포함해 내보낸다.** 빼면 클립 길이가 한 프레임
  짧아져 주기마다 건너뛴다.

## Animator 파라미터

| 이름 | 타입 | 용도 |
| --- | --- | --- |
| `Speed` | Float | `agent.velocity.magnitude`. Idle ↔ Walk ↔ Run |
| `IsChasing` | Bool | 추격 표현 |
| `Attack` | Trigger | `Attack_Push` one-shot |
| `Grab` | Trigger | `Grab_Execute` — **잡히면 즉사** |
| `Roar` | Trigger | 발견 순간 |
| `Threat` | Trigger | 추격 중 주기적 위협 (Upper 레이어) |
| `HitReact` | Trigger | 임계 미만 피격 (Upper 레이어) |
| `Stagger` | Trigger | 밀림 one-shot |
| `TurnDir` | Float | −1 좌 / +1 우. 제자리 회전 방향 |
| `Turn` | Trigger | 제자리 회전 시작 |
| `Buried` | Bool | 최우선. 되돌아오지 않는다 |
| `GripCable` | Trigger | 엔딩 연출 전용 |

Locomotion 은 1D Blend Tree 로 `Idle_Weight 0`, `Walk_Knuckle 1.15`,
`Run 2.60` 을 둔다. Speed 는 damping 0.15~0.20초를 쓴다. **Crawler 보다 길게
둔다** — 이 적은 가속이 2.20 m/s² 로 느려서 짧은 damping 이면 단계 사이가
덜컥거린다.

IMPORTANT: **`Death` 파라미터도 상태도 만들지 않는다.** `IsAlive` 는 항상 참이다.
`Grab_Execute` 는 플레이어가 죽는 연출이지 이것이 죽는 것이 아니다.

## 레이어

| 레이어 | 가중치 | Blending | 마스크 | 담는 것 |
| --- | ---: | --- | --- | --- |
| Base | 1.0 | Override | 없음 | 로코모션 · 공격 · 회전 · 밀림 · 처형 · 연출 |
| Upper | 1.0 | **Additive** | `Spine01~03` · `Chest` · `Hump*` · `Head*` · `Jaw` 만 | `Threat` · `Hit_React` |

IMPORTANT: Additive 레이어의 클립들은 **1프레임이 Rest 와 정확히 같아야 한다.**
Additive 는 첫 프레임과의 차이를 더하므로 기준이 어긋나면 걸을 때마다 상체가
누적되어 기울어진다. 두 클립 모두 1f 와 마지막 프레임을 Rest 와 맞춰 만든다.

Upper 레이어에는 **빈 기본 상태**를 두고 트리거가 없으면 거기 머문다. `Threat`
와 `Hit_React` 는 같은 레이어이므로 동시에 나가지 않으며 **피격이 우선**이다.

## 상태 우선순위 (Base)

```text
Any State --Buried--> Buried          최우선, transition 0.10s, interruption 없음
Any State --GripCable--> GripCable    Buried 가 아닐 때, transition 0.15s
Any State --Grab--> Grab_Execute      위 둘이 아닐 때, transition 0.05s
Any State --Stagger--> Stagger        위 셋이 아닐 때, transition 0.06s
Locomotion --Roar--> Roar             위 넷이 아닐 때, transition 0.10s
Locomotion --Attack--> Attack_Push    위 넷이 아닐 때, transition 0.08s
Locomotion --Turn--> Turn (BlendTree) Speed < 0.15 이고 위 넷이 아닐 때, 0.12s
Roar --exit--> Locomotion             36f 종료
Attack_Push --exit--> Locomotion      34f 종료 또는 normalizedTime >= 0.95
Turn --exit--> Locomotion             48f 종료
Stagger --exit--> Locomotion          48f 종료
Grab_Execute --exit--> Locomotion     132f 종료
GripCable --exit--> (없음)            엔딩이 이어서 Buried 를 켠다
Buried --exit--> (없음)
```

`Turn` 상태는 `TurnDir` 을 blend parameter 로 쓰는 1D Blend Tree 다
(`Turn_L −1`, `Turn_R +1`). 중간값을 쓰지 않으므로 threshold 는 −1 과 +1 둘뿐이다.

전이 시간이 Crawler(0.03~0.04초)보다 길다. 4.5 m 짜리가 0.03초 만에 자세를
바꾸면 질량이 사라진다. **예외는 `Grab_Execute` 의 0.05초** — `Attack_Push` 와
1~8f 가 같아야 하므로 짧게 끊는다.

## 코드에서 반드시 보완할 지점

1. `Awake()` 에서 자식 `Animator` 를 얻고 parameter hash 를 캐시한다.
2. 매 Update 뒤 `Speed`, `IsChasing` 을 갱신한다. `IsBuried` 면 `Speed` 는 0.
3. **`Attack()` 은 트리거만 보내고 피해를 주지 않는다.** 피해는 처형의
   `AE_GrabKill()` 에서만 나온다. 12번 참고.
4. `TakeDamage()` 는 누적이 임계에 닿으면 `Stagger`, **닿지 않으면 `HitReact`** 를
   보낸다. 지금은 둘 다 보내지 않는다.
5. `Bury()` 는 `Buried = true` 를 먼저 보내고 그 다음 Agent 를 끈다.
   Animator 는 끄지 않는다.
6. 처형 중복 방지용 `grabKillConsumed` 를 두어 transition · 프레임 드롭 · clip
   재진입에도 한 번만 죽게 한다.
7. **`Grip_Cable` 은 적 코드에 상태를 추가하지 않는다.** 엔딩을 진행하는 쪽이
   `Animator.SetTrigger("GripCable")` 을 직접 보내고, 그 직전에 `NavMeshAgent` 를
   끈다. 이 적의 상태 기계에 연출용 분기를 넣으면 6장 전체가 그것을 알아야 한다.
8. **`IsChasing` 이 거짓 → 참으로 바뀔 때 `Roar` 를 보낸다.** 단 `loseInterest`
   12초를 넘겨 완전히 놓친 뒤의 재발견에만. 시야가 끊길 때마다 1.2초씩 멈추면
   추격이 성립하지 않는다. 포효 중에는 `agent.isStopped = true`.
9. **`agent.speed` 를 상태에 따라 바꾼다.** 배회 · 수색 1.15, 추격(시야 안) 2.60.
   `angularSpeed` 는 55 를 유지한다 — `Turn` 클립이 그 값에 맞춰져 있다.
10. **제자리 회전.** `Speed` 가 0.15 미만이고 목표 방향과의 차이가 50° 이상이면
    `TurnDir` 을 설정하고 `Turn` 을 보낸다. 클립 재생 중에도 에이전트는 계속
    돌린다 — 클립은 90° 를 채우는 것이 목적이 아니라 그동안 발이 미끄러지지
    않게 하는 것이 목적이다.
11. **추격 중 위협.** `IsChasing` 이 참이고 거리가 공격 사거리(3.2 m) 밖이면
    최소 6초 간격으로 `Threat` 를 보낸다. `Attack_Push` · `Stagger` ·
    `Grab_Execute` 중에는 보내지 않는다.
12. **공격은 붙잡기 하나뿐이다.** `Attack()` 은 사거리 안이면 `Grab` 을 보내고
    피해를 적용하지 않는다. 체력을 확인하지 않는다 — 잡히면 즉사이므로 볼 이유가
    없다. `attackDamage = 2` 는 더 이상 쓰이지 않는다.
    8f 의 `AE_GrabLock()` 에서 사거리를 다시 검사해, 벗어났으면
    `Attack_Push`(피해 없음)로 넘긴다.

## 처형 시퀀스 — `Grab_Execute` 전용 계약

1인칭 게임이라 이 클립은 플레이어 카메라를 가져간다. 순서가 정확해야 한다.

| 시점 | 무엇 |
| --- | --- |
| `Grab` 트리거 | `agent.isStopped = true`. 이후 이동 없음 |
| `AE_GrabLock()` (8f) | **사거리를 다시 검사한다.** 안이면 입력 차단 · `CharacterController` 끄기 · `GrabAnchor` 에 부착하고 카메라 제어를 **0.3초에 걸쳐** 넘긴다. 밖이면 `Attack_Push` 로 넘긴다 |
| 40~64f | 입이 화면 중앙에 오는 구간. 카메라 각속도 90°/s 이하 |
| `AE_GrabKill()` (90f) | **여기서 즉사시킨다.** 체력을 깎는 것이 아니라 `PlayerHealth` 를 0 으로 만든다. `Died` 가 여기서 발생하고 화면 암전도 여기다 |
| 132f 종료 | 플레이어 분리, `CharacterController` 복구, `agent.isStopped = false` |

IMPORTANT: **`GameFlow` 의 재시작은 `AE_GrabKill` 기준으로 흐른다.** 잡히는 순간이
아니라 물리는 순간에 죽어야 한다. 그리고 클립이 중간에 끊겨도 플레이어가 손에
붙은 채 남지 않도록 `OnDisable` 에서 분리를 보장한다.

> 초판은 「죽는 타격일 때만 처형」이라 `IDamageable` 에 체력을 노출해야 했고,
> 그것이 셋의 합의가 필요한 미해결이었다. **2026-08-09 에 기획이 즉결 처형으로
> 바뀌면서 그 문제가 사라졌다**(`03b3e90`). 공용 계약을 건드리지 않는다.

## 수치 계약 — 실측과 어긋난 것

IMPORTANT: 아래는 **애니메이션이 아니라 프리팹과 레벨의 문제**이지만, 애니메이션이
전제하는 값이므로 여기 적는다. 실측 폭 4.78 m 에서 전부 파생된다.

| 항목 | 시트 · 코드 | 실측 기준 | 왜 |
| --- | --- | --- | --- |
| `NavMeshAgent radius` | 1.35 | **2.40** | 폭 4.78 의 절반 |
| `stopping distance` | 1.60 | **2.60** | 1.60 이면 몸통이 플레이어를 덮는다 |
| 통로 유효폭 | 4.00 m | **5.30 m** | 폭 + 에이전트 여유 |
| 본체 Box Collider | 2.90 × 4.20 × 1.60 | 재설계 필요 | 지금 값이면 팔을 통과한다 |
| `OnDrawGizmosSelected` 통로 상자 | 3.44 × 4.5 × 2.1 | 4.78 × 4.53 × 4.23 | 기즈모가 옛 수치를 그린다 |

콜라이더는 단순 확대로 풀리지 않는다. 팔까지 담으면 다리 사이의 빈 통로
(폭 0.94 m)까지 막게 된다. 팔에 별도 콜라이더를 두는 쪽이 맞아 보이지만
판단이 필요하다. [`README.md`](README.md) 의 미해결 목록 참고.

## 프리팹 권장 구조

```text
Colossus                        Colossus.cs, NavMeshAgent, BoxCollider
├─ Visual                       Animator, Colossus avatar/model
│  └─ Colossus_Armature
│     ├─ ... DEF_HeadTip
│     │        └─ EyeAnchor     엔딩 카메라 자리
│     └─ ... DEF_Digits.R
│              └─ GrabAnchor    처형 중 플레이어가 붙는 자리
└─ AttackOrigin                 흉곽 전면, 로컬 +Z 방향
```

`EyeAnchor` · `GrabAnchor` 를 본으로 만들지 않는 이유는 시트의 30본 규격을
지키기 위해서다. 둘 다 빈 트랜스폼이다.

`GrabAnchor` 가 오른손 자식인 것은 트랜스폼의 부모가 하나뿐이기 때문이다. 두 손
사이에 두고 싶지만 불가능하므로 **오른손을 기준으로 두고 왼손을 거기 맞춰
잠근다.** 애니메이션이 그렇게 만들어져야 한다.

`AttackOrigin` 은 피해 판정 위치를 읽기 위한 transform 일 뿐 물리 hitbox 가
아니다.

이 항목은 애니메이션을 실제 게임에 붙일 때 필요한 코드 변경 계약이다.
**FBX 만 넣으면 현재 게임에서는 Idle 조차 자동 재생되지 않는다.**
