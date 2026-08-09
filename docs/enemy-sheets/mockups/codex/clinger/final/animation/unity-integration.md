# Unity 연결 계약

상태: 2026-08-09. **FBX · 클립 9종 · `Clinger.cs` · `AC_Clinger` · `Clinger.prefab`
까지 끝났다.** 4장에 12 마리를 놓고 천장 → 낙하 → 착지 → 지상 → 사망을 플레이
모드에서 확인했다. 남은 것은 소리 배선뿐이다(아래 「아직 안 한 것」).

## 들어와 있는 것

```text
Assets/Game/Enemy/Models/Clinger.fbx          2.17 MB, 렌더러 1, 9,408 tris, 본 26
Assets/Game/Enemy/Textures/Clinger_BaseColor.png   2048²
Assets/Game/Enemy/Textures/Clinger_Normal.png      2048², Normal Map
Assets/Game/Enemy/Textures/Clinger_Roughness.png   2048², 현재 미사용
Assets/Game/Enemy/Materials/M_Clinger.mat     URP/Lit, metallic 0, smoothness 0.20
```

## FBX Import — 적용된 설정

- Rig = **Generic**, Avatar Definition = Create From This Model, Scale Factor 1,
  Use File Scale on. Humanoid가 아닌 이유는 리타게팅할 곳이 없고 Humanoid의 근육
  범위가 이 해부 구조의 포즈를 잘라내기 때문이다.
- Skin Weights 4 Bones. **Optimize Game Objects는 끈다** — 이벤트와 보조본을 확인할
  수 없게 된다.
- Animation Compression **Off**. Optimal은 45 mm 짜리 삼킴과 25 mm 짜리 여진을
  먼저 지운다.
- Loop Time은 `Cling_Folded`와 `Scuttle`만 켠다. `Cling_Tremor`는 개정에서
  One-shot이 됐다.
- Material: External(In Project) · BasedOnMaterialName · Search Everywhere.

## 클립 실측 (Unity에서 다시 읽은 값)

| 클립 | 프레임 | 길이 | Loop | 이벤트 |
| --- | ---: | ---: | :---: | --- |
| `Clinger_Cling_Folded` | 0..90 | 3.000s | O | — |
| `Clinger_Cling_Tremor` | 0..39 | 1.300s | X | — |
| `Clinger_Unfold_Drop` | 0..23 | 0.767s | X | `AE_DropRelease` f9 |
| `Clinger_Land` | 0..23 | 0.767s | X | `AE_EnableGroundMode` f14 |
| `Clinger_Scuttle` | 0..16 | 0.533s | O | — |
| `Clinger_Attack_Underjaw` | 0..25 | 0.833s | X | `AE_AttackHit` f12 |
| `Clinger_Hit` | 0..13 | 0.433s | X | — |
| `Clinger_Death_Fall` | 0..17 | 0.567s | X | `AE_DeathRelease` f4 |
| `Clinger_Death` | 0..35 | 1.167s | X | `AE_DeathRelease` f3 |

Unity 프레임은 Blender 프레임 −1이다. 문서가 「10f 해제」라고 하면 Unity에서는 f9다.

IMPORTANT: `ModelImporterClipAnimation`으로 이벤트를 넣을 때 `AnimationEvent.time`은
**초가 아니라 0~1 비율**이다. 초를 넣으면 클립 길이가 그만큼 늘어난다.
`(blenderFrame − 1) / lastFrame`으로 계산한다.

## Animator 파라미터

| 이름 | 타입 | 용도 |
| --- | --- | --- |
| `Mode` | Int | 0 천장, 1 낙하, 2 착지, 3 지상, 4 사망 |
| `Speed` | Float | 지상 Agent 속도, 0 → Scuttle 3.10 |
| `Tremor` | Trigger | Folded → Tremor 1회 재생 후 복귀 |
| `Drop` | Trigger | Unfold_Drop 시작 |
| `Attack` | Trigger | Underjaw one-shot |
| `Hit` | Trigger | 지상 피격. 천장에서는 `Tremor`를 대신 친다 |
| `Dead` | Bool | 최우선 Death, 복귀 없음 |

## 상태 흐름

IMPORTANT: **GroundPose ↔ Scuttle 전환은 0.12초 crossfade로 건다.** 걸음 주기에는
네 발이 다 닿아 있는 순간이 없어서 정지 포즈와 0 오차로 못 잇는다(실측 0.33 m / 37°).
나머지 지상 전환은 GroundPose 기준으로 2.6 cm 이하라 0.05초면 충분하다.

```text
Cling_Folded --Tremor--> Cling_Tremor --> Cling_Folded
Cling_* --Drop--> Unfold_Drop --ground detected--> Land
Land --f14/Mode 3--> GroundPose(마지막 프레임 hold) <-> Scuttle
GroundPose/Scuttle --Attack--> Attack_Underjaw --> GroundPose
GroundPose/Scuttle --Hit--> Hit --> GroundPose
Cling_* --Hit--> Cling_Tremor          (천장 피격은 경련으로 대신한다)
천장 --Dead--> Death_Fall --바닥 접촉--> Death
지상 --Dead--> Death
```

Death는 모든 상태를 0.03초 안에 중단하며 Exit transition을 만들지 않는다.
`Unfold_Drop`은 낙하가 길면 마지막 프레임을 hold하고 바닥 감지 시 normalizedTime과
무관하게 `Land`로 전환한다. 지상 정지는 `Land` 마지막 프레임 단일 포즈다.

**사망이 두 클립인 이유**와 **천장 피격이 `Cling_Tremor`인 이유**는
[`08-death.md`](08-death.md)와 [`07-hit.md`](07-hit.md)에 있다. 요약하면 천장과 지상은
리그에서 2 m 짜리 평행이동으로 갈려서 한 액션이 둘 다 될 수 없다.

## 코드 이벤트

| 이벤트 | 프레임 | 필수 처리 |
| --- | ---: | --- |
| `AE_DropRelease` | Unfold f9 | 천장 고정만 해제, Sphere 유지, Capsule/Agent off, 낙하 시작 |
| `AE_EnableGroundMode` | Land f14 | Sphere off, Capsule/Agent on |
| `AE_AttackHit` | Attack f12 | 전방 Capsule 재검사 후 한 번만 피해 |
| `AE_DeathRelease` | Death_Fall f4 · Death f3 | 모든 collider/Agent off, 사망 낙하 시작 |

`AE_EnableGroundMode`는 더 이상 공간을 맞추지 않는다. Transform 이동은 `Land`가
시작되기 전에 코드가 끝내고, 이 이벤트는 충돌체와 Agent만 켠다.
→ [`00-rigging.md`](00-rigging.md)의 「공간 전환은 `Land` 시작에서 한 번 한다」

각 이벤트는 bool/sequence id로 멱등 처리한다. 프레임 드롭, transition 재진입,
애니메이터 재활성화에도 한 동작당 한 번만 실행돼야 한다.

## 충돌·이동 값

- 천장: Sphere r 0.52, center = 원점 아래 **y −0.65**(접힌 몸의 실제 중심), Agent off.
- 지상: Capsule h 1.20/r 0.52/center y 0.60.
- Agent: r 0.55, h 1.27, **speed 3.10**, angular 240°/s, acceleration 12,
  stopping distance 0.55. 반지름은 몸통이 아니라 **앞팔 접지점 사이 거리**에서 나온
  값이고, 더 작게 잡으면 여러 마리가 좁은 복도에서 서로를 통과한다.
- speed 3.10 은 걸음 클립이 미끄러지지 않는 속도다. 이 값을 올리면 발이 끌린다.
  → [`05-scuttle.md`](05-scuttle.md) 「속도는 보폭이 정한다」
- 공격: 전방 Capsule h 0.85/r 0.60. 사지별 물리 collider는 만들지 않는다.
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

Collider 두 개를 동시에 켜지 않는다. `Land` f14 이전에는 NavMeshAgent를 활성화하지
않고, Death에서는 Animator만 남겨 끝까지 재생한다.

## 레벨에 놓을 때

`ClingerAnchor` 는 **갈고리가 물 자리**다. 프리팹 원점이 천장 접점이라 앵커를 놓은
그 지점에서 갈고리가 24 mm 물어 들어가고, 몸은 거기서부터 아래로 매달린다.
몸통 중심이나 발밑이 아니다.

### 1. 앵커는 천장 면에 붙인다

앵커 y 를 천장보다 낮게 두면 그만큼 **갈고리가 허공을 쥔다.** 눈에 바로 보인다.

```
천장 면 y = A  →  앵커 y = A
접힌 몸통이 차지하는 구간 = A − 1.29 ~ A
전개(낙하 직전) 최저점 = A − 2.55
```

### 2. 천장은 바닥에서 **3.2 m 이상**이어야 한다

막는 것은 메시가 아니라 천장 Sphere 다. 중심 −0.65 · 반경 0.52 라 구의 밑면은
천장에서 **1.17 m** 아래고, 플레이어 `CharacterController` 는 1.8 m 다.
`3.2 = 1.8 + 1.17 + 0.23(여유)` 이 최소값이고, 이보다 낮으면 **플레이어가 지나가면서
놈에게 머리를 부딪는다.** 낙하 전에 닿으면 위협이 예고가 아니라 사고가 된다.

3.2~3.6 m 구간에서는 **메시가 플레이어 머리를 스친다**(접힌 몸 밑면이 천장 −1.29 m
= 3.2 m 방에서 1.91 m, 머리 1.80 m 바로 위). 충돌은 없고 보이기만 하며, 위를 보게
만드는 장에서는 오히려 원하는 그림이다. 다만 **의도해서 놓는 것과 모르고 놓는 것은
다르다.** 4.0 m 이상이면 그런 스침도 없다.

전개까지 보려면 더 필요하다. `Unfold_Drop` 18f 는 천장 아래 **2.55 m** 까지 뻗으므로,
낙하 전 전개를 보여주려는 자리는 천장 4.4 m 이상이어야 발끝이 플레이어 키 위에 남는다.
낮은 곳에서는 전개와 낙하가 겹쳐 보이는데, 그건 그것대로 맞다.

### 3. 앵커끼리 2.6 m 안에 붙이지 않는다

`Unfold_Drop` 18f 폭이 **2.49 m** 다. 더 붙이면 전개할 때 서로를 관통한다.
접힘 상태 폭은 1.83 m 이므로 대기만 시킬 자리도 2.0 m 는 띄운다.

### 4. 낙하 지점에 착지 자리가 있어야 한다

- 착지 자세 바운드 **1.77 × 1.27 × 1.72 m**, Agent 반경 **0.55**.
- 앵커에서 수직으로 내린 지점 반경 **1.2 m** 안에 NavMesh 가 있어야 4점으로 선다.
- 낙하 거리가 **1.5 m 미만이면 놓지 않는다.** 접힌 몸이 1.29 m 라 그보다 낮으면
  대기 자세부터 바닥에 박혀 있다.

### 5. NavMesh 는 `Humanoid` 타입으로 굽는다

Clinger 는 `Humanoid`(반경 0.5 · 높이 2.0)를 쓴다.
→ [`architecture.md`](../../../../../tech-design/architecture.md) 「몸이 큰 적은…」

- 실제 몸높이는 1.27 m 인데 **경로는 높이 2.0 으로 구워진 면만 읽는다.** 1.5 m 짜리
  덕트나 낮은 통로로 기어들어가게 하고 싶으면 타입을 따로 굽지 않는 한 안 된다.
- Agent 반경 0.55 가 구운 반경 0.5 보다 크다. 통로 폭이 1.1 m 근처면 경로는 나오는데
  몸이 벽에 스친다. 좁은 통로에는 한 마리만 보낸다.

### 6. 여럿을 몰 때

`speed 3.10 m/s` 는 플레이어 걷기 2.8 보다 빠르고 전력질주 4.6 보다 느리다.
**한 마리는 절대 못 잡고, 길을 막는 것으로 잡는다.** 앵커를 통로 양쪽과 뒤쪽에
나눠 두고 동시에 떨어뜨리는 편이 한 곳에 몰아 두는 것보다 훨씬 위협적이다.

`Attack` 은 몸을 260 mm 앞으로 던지지만 **Capsule 은 제자리**다. 벽에 바짝 붙여
공격시켜도 벽을 뚫지 않는다. 대신 연출이 벽에 파묻히므로 벽에서 0.5 m 는 띄운다.

### Ch4 앵커 12개 — 점검하고 고쳤다 (2026-08-09)

| 앵커 | 전 | 후 | 고친 것 |
| --- | --- | --- | --- |
| 00~07 | y 7.70 | y 8.00 | 천장 면에서 0.30 m 떠 있어 갈고리가 허공을 쥐고 있었다 |
| 02 | (9.0, 7.70, −6) | (8.5, 8.00, −6) | 낙하 경로가 **캣워크 난간 위**로 떨어졌다. NavMesh 가 없는 자리라 착지해도 못 움직인다. x 를 0.5 m 물려 홀 바닥으로 보냈다 |
| 08 | y 5.00 | y 8.00 | 천장에서 3 m 아래 **허공에 떠 있었다.** 낙하 거리도 1.00 m 로 접힌 몸(1.29)보다 짧았다. 천장으로 올리니 캣워크까지 4.00 m |
| 09 · 10 | y 3.10 | y 3.20 | 천장 면으로 올림. 천장이 3.2 m 인 bay 라 이 둘은 **위 2절의 스침 구간**에 해당한다 |
| 11 | y 3.90 | y 4.00 | 천장 면으로 올림 |

고친 뒤 12 개 전부 통과한다 — 낙하 1.5 m 이상, 착지점에 NavMesh 있음, 앵커 간격
3.6 m 이상, 천장 Sphere 가 플레이어 머리 위.

bay(천장 3.2 m)에는 더 높은 자리가 없다. 09 · 10 은 옮기지 않고 스침을 의도로
남겼다 — bay 를 빠져나오는 순간 머리 바로 위에서 떨어지는 것이 이 방의 설계다.

## 붙인 결과 (2026-08-09 · A)

계약을 그대로 옮겼다. 값은 위 절들과 같고, 갈린 자리만 적는다.

| 만든 것 | 어디 |
| --- | --- |
| `Clinger.cs` | `Assets/Game/Enemy/Clinger.cs` — `IDamageable` · `IHunter` |
| `ClingerAnimationEvents.cs` | 같은 폴더. `Crawler` 와 같은 이유로 중계기를 둔다 |
| `AC_Clinger.controller` | `Assets/Game/Enemy/Animation/` — 상태 10 · 파라미터 8 |
| `Clinger.prefab` | `Assets/Game/Enemy/` |

**파라미터가 계약보다 하나 많다 — `Grounded`(Trigger).** `Death_Fall → Death` 를
「바닥 접촉」으로 잇는 데 쓴다. 시간으로 재면 8m 에서 떨어질 때 `Death_Fall`(0.567초)
이 먼저 끝나 **공중에서 지상 사망 자세가 된다.** 시체가 바닥에 닿는 프레임에 코드가
친다.

`GroundPose` 는 `Clinger_Land` 를 **speed 0 · cycleOffset 1** 로 둔 상태다. 마지막
프레임 단일 포즈라는 계약을 클립 하나로 만족시킨다.

**천장에서 맞으면 떨어진다.** 계약에 없던 판단이라 적어 둔다 — 천장의 놈을 안전하게
쏠 수 있으면 위협이 통째로 사라진다. 한 대라도 맞으면 `Drop()` 이 걸린다.

### 4장 배치 (2026-08-09)

앵커 12 개에 12 마리. `dropRadius` 3.2m 안에 플레이어가 들어오고 **1.5m 이상 아래**
에 있을 때 떨어진다. 떨어지기 전에 `Cling_Tremor` 가 0.45초 예고로 나간다.

**발전기 예열 30초에 남은 것을 떨군다.** `GeneratorDevice.started` 에 홀 중앙 다섯
(`Clinger_01`~`05`)의 `Drop()` 을 걸어 두었다. 이미 내려온 것은 `Drop()` 이 무시하므로
**플레이어가 피해 다닌 만큼 그때 한꺼번에 온다.**

### 소리 9종 (2026-08-09)

`Crawler` 처럼 별도 `ClingerAudio` 를 만들지 않았다. 그 파일이 거의 복사본이 되기
때문이고, 대신 **루프 둘은 코드가 상태로 몰고 한 번짜리는 `UnityEvent` 로 뺐다.**

| 소스 | 클립 | 언제 |
| --- | --- | --- |
| `Snd_Cling` | `clinger-cling-loop` | 천장에 매달려 있는 동안. 떨어질 때 코드가 끊는다 |
| `Snd_Scuttle` | `clinger-scuttle-loop` | 지상에서 0.15 m/s 이상 움직일 때 |
| `Snd_Tremor` | `clinger-tremor-loop` | `tremored` → `Play`, `dropped` → `Stop` |
| `Snd_Voice` | (비움) | 나머지 여섯을 `PlayOneShot` 으로 낸다 |

`attackTell` 은 클립 이벤트가 아니라 `Attack()` 에서 낸다. 계약의 이벤트 넷에는
예고가 없고, 애니메이션에 이벤트를 더하면 리깅 쪽 에셋을 건드리게 된다. 트리거를
치는 순간이 물기(12f)보다 0.4 초 앞이라 예고로 정확히 맞는다.

IMPORTANT: **`clinger-tremor-loop` 은 6.06 초짜리다.** 경련 애니메이션(1.3 초)보다
길어서 자주 떨면 자기 소리와 겹친다. 그래서 `tremorInterval` 을 **7~14 초**로 잡았다.
원래 3.5~9 초였는데 그 간격에서는 소리가 끊이지 않아 배경이 됐다.

IMPORTANT: **매달린 소리는 13 m 까지만 들린다.** 26 m 로 두었더니 스폰 자리에서
**일곱 마리가 한꺼번에 들려** 한 덩어리가 됐다. 「위에 뭔가 있다」는 머리 위 한두
마리일 때 성립한다.

## 아직 안 한 것

- 여럿을 놓았을 때의 밸런스. 4장 12 마리가 적당한지는 사람이 플레이해야 안다
- LOD1/LOD2 — 프로파일에서 실제로 걸릴 때 만든다.
  → [`MODEL-SPEC.md`](../MODEL-SPEC.md) 4절
- `Clinger_Roughness.png`는 임포트만 돼 있고 머터리얼이 쓰지 않는다. 생성 텍스처의
  metallic/roughness 값이 에셋마다 제각각이라 평평한 값으로 덮는 것이 프로젝트 기준이다.
  → `docs/tech-design/decisions.md`
