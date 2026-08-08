# Unity 연결 계약

현재 저장소에는 `Clinger.cs`, `Clinger.prefab`, Animator Controller가 없다. 아래는
최종 모델과 8개 클립을 실제 적으로 구현할 때 지켜야 할 새 계약이다.

## FBX Import

- Rig = Generic, Avatar Definition = Create From This Model, Scale Factor 1.
- Skin Weights = 4 Bones. Optimize Game Objects는 이벤트와 보조본 확인 뒤 켠다.
- 모든 클립 Root Transform Rotation/Position은 Bake Into Pose.
- Loop Time은 Folded/Tremor/Scuttle만 켠다. 나머지는 one-shot 또는 마지막 포즈 clamp.
- Resample Curves on, Animation Compression Optimal. 10f/13f/15f 이벤트가 밀리면
  compression off와 비교해 오차를 낮춘다.

## Animator 파라미터

| 이름 | 타입 | 용도 |
| --- | --- | --- |
| `Mode` | Int | 0 천장, 1 낙하, 2 착지, 3 지상, 4 사망 |
| `Speed` | Float | 지상 Agent 속도, 0→Scuttle 4.2 |
| `Tremor` | Bool | Folded↔Tremor 전환 |
| `Drop` | Trigger | Unfold_Drop 시작 |
| `Attack` | Trigger | Underjaw one-shot |
| `Hit` | Trigger | 비치명 additive 피격 |
| `Dead` | Bool | 최우선 Death, 복귀 없음 |

## 상태 흐름

```text
Cling_Folded <-> Cling_Tremor
Cling_* --Drop--> Unfold_Drop --ground detected--> Land
Land --15f/Mode 3--> GroundPose(18f hold) <-> Scuttle
GroundPose/Scuttle --Attack--> Attack_Underjaw --> GroundPose
Any living state --Hit--> additive Hit
Any State --Dead--> Death
```

Death는 모든 상태를 0.03초 안에 중단하며 Exit transition을 만들지 않는다.
`Unfold_Drop`은 낙하가 길면 f24를 hold하고 바닥 감지 시 normalizedTime과 무관하게
Land로 전환한다. 지상 정지는 Land f18 단일 포즈를 사용한다.

## 코드 이벤트

| 이벤트 | 프레임 | 필수 처리 |
| --- | ---: | --- |
| `AE_DropRelease` | Unfold 10f | 천장 고정만 해제, Sphere 유지, Capsule/Agent off, 낙하 시작 |
| `AE_EnableGroundMode` | Land 15f | Sphere off, Capsule/Agent on, GroundPivot 정렬 |
| `AE_AttackHit` | Attack 13f | 전방 Capsule 재검사 후 한 번만 피해 |
| `AE_DeathRelease` | Death 4f | 모든 collider/Agent off, 사망 낙하 시작 |

각 이벤트는 bool/sequence id로 멱등 처리한다. 프레임 드롭, transition 재진입,
애니메이터 재활성화에도 한 동작당 한 번만 실행돼야 한다.

## 충돌·이동 값

- 천장: Sphere r 0.46, center=몸통, Agent off.
- 지상: Capsule h 1.34/r 0.48/center y 0.67.
- Agent: r 0.48, h 1.42, speed 4.20, angular 240°/s, acceleration 12,
  stopping distance 0.55.
- 공격: 전방 Capsule h 0.85/r 0.52. 사지별 물리 collider는 만들지 않는다.
- 피격 Hitbox가 필요하면 Head/Torso/Limbs 세 그룹만 사용한다.

## 프리팹 권장 구조

```text
Clinger                         Clinger.cs, 상태별 collider 관리
├─ CeilingCollider              SphereCollider
├─ GroundCollider               CapsuleCollider
├─ Visual                       Animator, Generic avatar/model
│  └─ Clinger_Armature
├─ GroundProbe                  SphereCast origin
└─ AttackOrigin                 아래턱 전방 +Z
```

Collider 두 개를 동시에 켜지 않는다. Land 15f 이전에는 NavMeshAgent를 활성화하지 않고,
Death에서는 Animator만 남겨 28f를 끝까지 재생한다.
