# Unity 연결 계약

이 문서는 애니메이션 제작 범위와 현재 코드 사이의 빈 연결부를 명시한다.
현재 `Crawler.prefab`은 회색 Capsule이고 `Crawler.cs`는 Animator를 전혀 참조하지 않는다.

## FBX Import

- Rig: `Animation Type = Generic`, Avatar Definition = Create From This Model.
- Scale Factor 1, Bake Axis Conversion은 축 시험 후 한 번만 적용한다.
- Skin Weights = 4 Bones, Optimize Game Objects는 본/이벤트 확인이 끝난 뒤 켠다.
- 모든 clip의 Root Transform Rotation/Position은 Bake Into Pose.
- Loop Time: Idle/Skitter/Sprint만 켬. Loop Pose로 생긴 발 보정이 접지를 망치면 끄고
  Blender 원본 loop를 수정한다.
- Resample Curves 켬, Animation Compression은 Optimal. 발/입 이벤트가 1f 밀리면
  Off로 비교한 뒤 Keyframe Reduction Error를 낮춘다.

## Animator 파라미터

| 이름 | 타입 | 용도 |
| --- | --- | --- |
| `Speed` | Float | `agent.velocity.magnitude`, locomotion blend |
| `IsChasing` | Bool | 배회/추격 표현과 Sprint 진입 |
| `Attack` | Trigger | Underbite one-shot |
| `Hit` | Trigger | 비치명 피격 one-shot |
| `Dead` | Bool | 최우선 Death, 되돌아오지 않음 |

Locomotion은 1D Blend Tree로 `Idle 0`, `Skitter 2.4`, `Sprint 5.5`를 둔다.
Speed는 매 프레임 그대로 넣지 말고 damping 0.08~0.12초를 사용한다.

## 상태 우선순위

```text
Any State --Dead--> Death       최우선, transition 0.03s, interruption 없음
Any State --Hit--> Hit          Dead가 아닐 때, transition 0.03s
Locomotion --Attack--> Attack   Dead가 아닐 때, transition 0.04s
Attack --exit--> Locomotion     22f 종료 또는 normalizedTime >= 0.95
Hit --exit--> Locomotion        12f 종료
```

Attack은 Hit로 중단될 수 있고, Hit/Attack 모두 Death가 즉시 중단할 수 있어야 한다.
Death에서는 Exit transition을 만들지 않는다.

## 코드에서 반드시 보완할 지점

1. `Awake()`에서 자식 `Animator`를 얻고 parameter hash를 캐시한다.
2. 매 Update 뒤 `Speed`, `IsChasing`을 갱신한다. Agent가 꺼진 Death에서는 Speed 0.
3. `Attack()`은 trigger만 보내고 즉시 피해를 주지 않는다. 13f Animation Event
   `AE_AttackHit()`에서 거리·생존을 재검사해 1회 피해를 준다.
4. `TakeDamage()`는 health 감소 후 `health <= 0`이면 Death만, 아니면 Hit만 보낸다.
5. `Die()`는 `Dead=true`를 먼저 보내고 Agent/Collider를 끈다. Animator까지 끄지 않는다.
6. 공격 중복 방지용 `attackSequenceId` 또는 `attackHitConsumed`를 두어 transition,
   프레임 드롭, clip 재진입에도 한 공격당 이벤트가 한 번만 적용되게 한다.

이 항목은 애니메이션을 실제 게임에 붙일 때 필요한 코드 변경 계약이다. 애니메이션
FBX만 넣으면 현재 게임에서는 Idle조차 자동 재생되지 않는다.

## 프리팹 권장 구조

```text
Crawler                         Crawler.cs, NavMeshAgent, CapsuleCollider
├─ Visual                       Animator, Crawler avatar/model
│  └─ Crawler_Armature
└─ AttackOrigin                 head underside, local +Z direction
```

본체 Capsule 값은 h 0.90, r 0.40, center y 0.45를 유지한다. 렌더 다리는 콜라이더에
포함하지 않는다. `AttackOrigin`은 피해 판정 위치를 읽기 위한 transform일 뿐 별도
물리 hitbox로 만들지 않는다.
