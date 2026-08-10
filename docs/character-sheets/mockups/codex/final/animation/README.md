# 주인공 애니메이션 제작 패키지

> 상태: 확정 · 최종 수정 2026-08-09

`PROTAGONIST-MC-FINAL-1.0`을 Blender에서 리깅해 Unity로 넘기기 위한 명세다.
`PlayerMotor`, `PlayerWeapon`, `PlayerHealth`, `PlayerInteractor`, `WeaponSwitcher`,
`Carriable`의 실제 수치와 입력 규칙을 기준으로 한다.

## 2026-08-09 · 전신 28종에서 20종으로 줄였다

초판은 전신 Humanoid 28종이었다. 3인칭으로 보이는 캐릭터를 전제한 목록인데,
확인해 보니 그 전제가 이 게임에 없다.

| 확인한 것 | 결과 |
| --- | --- |
| 기획에 플레이어 몸이 화면에 나오는 장면 | **없다.** 엔딩은 「그것의 시점」이지 3인칭이 아니고 거울·사망 카메라도 없다 |
| `Player.prefab` | 캡슐 `Body` + `Camera` + `Flashlight` 뿐. `Animator` 도 `SkinnedMeshRenderer` 도 없다 |
| 반동 · 흔들림 · 재장전 딥 | **이미 코드에 있다.** `PlayerWeapon.reloadDipMeters` · `reloadTiltDegrees` · `reloadRollDegrees`, `PlayerLook` 의 recoil/sway |
| `Player_ValveTurn` 에 대응하는 코드 | **없다.** 상호작용은 `Lever` · `Pickup` · `Readable` · `Carriable` 넷뿐이다 |
| 손전등 | `Light beam` 이 카메라 자식 Spot Light 다. **손에 들려 있지 않다** |

그래서 **화면에 실제로 나오는 것**만 만든다 — 1인칭 팔이다. 전신은 그림자와
엔딩 원경 두 자리에만 필요하고, 거기서는 세 클립이면 된다.

버린 것: 전신 `Walk` · `Sprint` · `ExhaustedWalk` · `Crouch` 4종 · `Jump/Airborne/Land`
3종 · 전신 `Hit`/`Death`, 그리고 `Player_ValveTurn`. 걷기·달리기 팔 흔들림도
클립으로 만들지 않는다 — 무기 상태마다 만들면 4배로 불어나는데 `PlayerLook` 의
sway 가 이미 하고 있고, 부족하면 Additive 하나로 덮는다.

## 최종 클립 · 30 fps

### 1인칭 팔 17종 — `Protagonist_Arms.fbx`

코드가 가진 상태는 넷이다. 맨손 · 권총 · 산탄총 · 운반 중
(`WeaponSwitcher.IsCarrying` 이면 무기가 비활성이다).

| # | 클립 | 프레임 / 시간 | 루프 | 코드 기준 |
| ---: | --- | ---: | :---: | --- |
| 1 | `Arms_Idle` | 90 / 3.00s | O | 맨손. 1·2장 전체 |
| 2 | `Arms_Interact` | 18 / 0.60s | X | `E` 일반 상호작용 |
| 3 | `Arms_LeverPull` | 36 / 1.20s | X | `Lever`. **런타임 IK** |
| 4 | `Arms_CarryLift` | 24 / 0.80s | X | `Carriable` 획득. **런타임 IK** |
| 5 | `Arms_CarryIdle` | 60 / 2.00s | O | 양손 운반. **런타임 IK** |
| 6 | `Arms_CarryDrop` | 18 / 0.60s | X | 허공 `E` 내려놓기 |
| 7 | `Arms_PistolEquip` | 15 / 0.50s | X | `WeaponKind.Pistol` |
| 8 | `Arms_PistolIdle` | 60 / 2.00s | O | 2손 로우 레디 |
| 9 | `Arms_PistolFire` | 8 / 0.27s | X | 2 shots/s |
| 10 | `Arms_PistolReload` | 54 / 1.80s | X | `magazineSize 8`, `reloadSeconds 1.8` |
| 11 | `Arms_ShotgunEquip` | 18 / 0.60s | X | `WeaponKind.Shotgun` |
| 12 | `Arms_ShotgunIdle` | 60 / 2.00s | O | 견착 로우 레디 |
| 13 | `Arms_ShotgunFirePump` | 30 / 1.00s | X | 0.9 shots/s |
| 14 | `Arms_ShotgunReload` | 78 / 2.60s | X | 4발. **인스턴스 `reloadSeconds` 확인 필요** |
| 15 | `Arms_Hit` | 12 / 0.40s | X | **Additive.** 무적 0.8s 와 별개 |
| 16 | `Arms_Death` | 36 / 1.20s | X | `Died` 후 입력 off |
| 17 | `Arms_Sway` | 30 / 1.00s | O | **Additive.** 이동 중 팔 흔들림 한 벌 |

### 전신 4종 — `Protagonist_Body.fbx`

| # | 클립 | 프레임 / 시간 | 루프 | 어디 |
| ---: | --- | ---: | :---: | --- |
| 18 | `Body_Idle` | 90 / 3.00s | O | 그림자. 정지 포즈만 두면 움직일 때 티가 난다 |
| 19 | `Body_Walk` | 30 / 1.00s | O | 그림자. `walkSpeed 2.8` |
| 20 | `Body_CableCarRide` | 60 / 2.00s | O | 엔딩. 「그것의 시점에서 삭도가 멀어지는 것이 보인다」 |
| 21 | `Body_Grabbed` | 132 / 4.40s | X | **처형.** `Colossus_Grab_Execute` 와 프레임이 같다 |

18~20 은 그림자와 엔딩 원경이라 LOD2 급이면 된다. **21 은 아니다** — 카메라가
0.3 m 앞에 4.4초 동안 붙어 있다.

## 처형 장면에는 몸이 필요하다 — 2026-08-09 추가

이 문서는 「플레이어 몸이 화면에 나오는 장면이 없다」는 확인 위에 서 있었다.
그 뒤 기획이 **즉결 처형**으로 바뀌었고([`12-grab-execute.md`](../../../../../enemy-sheets/mockups/codex/colossus/final/animation/12-grab-execute.md)),
그 클립은 플레이어 카메라를 4.40초 동안 `GrabAnchor` 에 가둔다. 그 4.4초 내내
**플레이어는 아래를 볼 수 있고, 거기에 아무것도 없다.**

| 안 | 왜 안 골랐나 / 골랐나 |
| --- | --- |
| 그냥 둔다 | 붙잡혀 들어올려지는 4.4초 동안 자기 몸이 없다. 이 연출의 전부인 구간이다 |
| 1인칭 팔로 버둥댄다 | 싸고 흔한 답이지만, 40f 에 **발이 땅에서 떨어지는 것이 보여야** 한다는 것이 12번 문서의 요구다. 팔로는 못 한다 |
| **전신을 붙인다 (택함)** | 이미 있는 리그와 메시다. 클립 하나가 는다 |

IMPORTANT: **두 리그를 같이 애니메이션하지 않는다.** 콜로서스의 손은 다른 파일에서
이미 구워졌고, 프레임마다 맞춰 잡는 것은 두 리그 문제다. 그럴 필요가 없는 이유는
`GrabAnchor` 가 **손에 달린 트랜스폼 하나**라는 데 있다 — 몸을 거기 붙이면 손이
가는 곳으로 몸이 따라간다. 그래서 이 클립이 지는 몫은 앵커가 못 주는 것뿐이다:
발버둥과, 그것이 멈추는 프레임.

비트는 12번 문서의 것을 그대로 쓴다 — 8 포획 · 40 들어올림 · 56 채감 · **90 치명**
· 104 흔듦 · 120 놓음.

### 붙일 때 머리를 지운다

카메라가 `GrabAnchor` 에 있고 near clip 이 0.01 이므로, 몸을 그대로 켜면 **자기
머리 안쪽이 보인다.** 처형 구간에만 켜는 렌더러이므로 머리·목 정점을 뺀 메시를
한 벌 두면 되고, 그림자용 전신은 그대로 둔다. 아직 안 만들었다 — 처형을 Unity 에
붙일 때 같이 한다.

### 실측 (2026-08-09)

| 클립 | f | 루프 이음새 | 최악 무릎 | 최저점 |
| --- | ---: | ---: | ---: | ---: |
| `Body_Idle` | 90 | 0.000 mm | 16.5° | −2.3 mm |
| `Body_Walk` | 30 | 0.000 mm | 59.2° | −6.5 mm |
| `Body_CableCarRide` | 60 | 0.000 mm | 18.6° | −2.4 mm |
| `Body_Grabbed` | 132 | — | 66.0° | −89 mm |

최저점은 발이 바닥을 뚫었는지를 본다. 서 있는 셋은 2~7 mm 로 장화 밑창 두께 안이다.
`Body_Grabbed` 의 −89 mm 는 뚫은 것이 아니라 **매달린 것**이다 — 이 클립에서 바닥은
없고, 몸은 앵커가 있는 곳에 있다.

`Body_Walk` 이 성큼거리는 것은 `walkSpeed` 가 2.8 m/s 라서다. 30프레임 한 바퀴가
2.8 m 이고 그건 1.40 m 짜리 두 걸음, 즉 조깅이다. 보폭을 줄이면 그림자의 발이
미끄러진다.

Root Motion 은 전 클립에서 끈다. 실제 이동 · 점프 · 낙하 · 콜라이더는 코드가
담당한다.


## 1인칭 팔 17종 완료 — 2026-08-09

`animate_arms.py` 한 파일이 전부 만든다. 손을 손으로 놓는 곳은 한 군데도 없다 —
파지는 [`10-hand-poses.md`](10-hand-poses.md) 의 이름이고, 팔꿈치는
[`06-pistol.md`](06-pistol.md) 의 한도 위반량으로 풀리며, 구도는
`ViewModelCamera`(수직 50°·near 0.01) 로 재서 정한다.

| 클립 | f | 루프 이음새 | 최악 손목 | 최악 회내 |
| --- | ---: | ---: | ---: | ---: |
| `Arms_Idle` | 90 | 0.000 mm | 49.9° | 66.6° |
| `Arms_Interact` | 18 | — | 49.9° | 66.6° |
| `Arms_LeverPull` | 36 | — | 56.6° | 70.7° |
| `Arms_CarryLift` | 24 | — | 3.4° | 62.7° |
| `Arms_CarryIdle` | 60 | 0.000 mm | 53.0° | 68.4° |
| `Arms_CarryDrop` | 18 | — | 53.0° | 68.4° |
| `Arms_PistolEquip` | 15 | — | 59.2° | 69.5° |
| `Arms_PistolIdle` | 60 | 0.000 mm | 59.8° | 68.6° |
| `Arms_PistolFire` | 8 | — | 59.2° | 69.2° |
| `Arms_PistolReload` | 54 | 0.000 mm | 60.1° | 68.6° |
| `Arms_ShotgunEquip` | 18 | — | 42.8° | 64.9° |
| `Arms_ShotgunIdle` | 60 | 0.000 mm | 43.1° | 65.2° |
| `Arms_ShotgunFirePump` | 30 | 0.000 mm | 44.0° | 65.0° |
| `Arms_ShotgunReload` | 78 | 0.000 mm | 53.1° | 68.7° |
| `Arms_Hit` | 12 | — | 49.9° | 66.6° |
| `Arms_Death` | 36 | — | 49.9° | 66.6° |
| `Arms_Sway` | 30 | 0.000 mm | 49.9° | 66.6° |

권총 계열 넷만 상한 55° 를 4~5° 넘는다. 파지 자체가 그렇고
([`10-hand-poses.md`](10-hand-poses.md)) 팔꿈치로 더 살 수 없다.

### 루프는 「같은 값을 다시 넣어」서는 안 닫힌다

마지막 프레임을 첫 프레임과 **같은 숫자로 다시 포즈시키면 0.16 m 어긋난다.**
그 숫자 뒤의 탐색 — 손의 비틀림, 팔꿈치 각도 — 이 직전 프레임 쪽으로 일부러
치우쳐 있고, 90프레임을 돌면 다른 답에 도착한다. **포즈 자체를 저장했다가 그대로
재생한다**(`snapshot`/`replay`). 다섯 루프 전부 0.000 mm 다.

같은 이유로 **손의 비틀림에도 연속성 항을 넣었다.** 자유로운 손은 원을 다 훑는데
180° 떨어진 두 값이 손목을 똑같이 굽히므로, 숨 쉬는 루프 한가운데서 손이 뒤집혔다.

### `IKWeight` 는 클립 안에 굽는다

`Arms_LeverPull` · `Arms_Carry*` 넷에 오브젝트 커스텀 프로퍼티로 키를 넣는다.
베이크가 액션을 다시 쓰므로 **베이크 뒤에** 넣는다. 잡는 구간만 1 이다.

## IK 를 두 종류로 나눈다

IMPORTANT: **제작용 IK 와 런타임 IK 는 다른 것이다.** 초판 명세의 IK 는 전부
제작용이었고, 그것만으로는 「레버 높이가 씬마다 다른 것」이 안 풀린다.

| | 제작용 IK (Blender) | 런타임 IK (Unity) |
| --- | --- | --- |
| 언제 | 클립을 만들 때만. 베이크하면 사라진다 | 매 프레임 |
| 비용 | 0 | 2본 해석해 한 번 |
| 푸는 것 | 발 접지, 손목 위치 잡기, 총 파지 | 씬의 실물 위치에 손 맞추기 |

### 제작용 IK — 전면 사용

`00-rigging.md` 의 컨트롤 리그를 쓴다. 전신은 사지당 IK 타깃 하나씩 넷, 1인칭
팔은 손 IK 둘에 팔꿈치 pole 둘이다. FBX 로 나갈 때 전부 deform bone 에 굽는다.
`CTRL_Weapon` 과 `CTRL_CarryProp` 은 만들지 않았다 — 무기는 팔 리그 안의 본이고
([`09-weapons.md`](09-weapons.md)) 운반물은 런타임 IK 가 잡는다.

### 런타임 IK — 두 곳만

손 IK 를 아무 데나 켜면 포즈가 뭉개진다. 붙는 곳은 둘뿐이다.

| 클립 | 타깃 | 프롭 쪽에 필요한 것 |
| --- | --- | --- |
| `Arms_LeverPull` | 오른손 | `Lever.handleGrip` |
| `Arms_CarryLift` · `CarryIdle` · `CarryDrop` | 양손 | `Carriable.gripL` · `gripR` |

- **총에는 안 건다.** 총을 손 본 소켓에 붙이면 손과 총의 상대 위치가 고정이고,
  보조손도 같은 클립 안에서 같이 움직이므로 구워두면 된다.
- 클립마다 **`IKWeight` 커브**를 구워 잡는 구간에만 1 로 올린다. 항상 켜두면
  손이 프롭에 끌려다닌다.
- 구현은 **2본 해석해를 직접 짠다**(코사인 법칙 + pole 벡터). `com.unity.animation.rigging`
  을 넣지 않는 이유는 팔 하나짜리에 패키지를 들이는 값이 크고, 2026-08-09 에
  확정한 「제3자 에셋을 쓰지 않는다」와도 결이 다르기 때문이다. 1인칭 팔은 Generic
  리그라 Humanoid 전용인 `OnAnimatorIK` 를 쓸 수 없다.

## 1인칭 팔은 전신 메시에서 잘라 만든다

새로 모델링하지 않는다. 전신 메시를 복제해 `UpperChest` 아래·뒤를 지우고 소매
절단면만 막는다. 같은 텍스처와 같은 비례가 그대로 따라오고, 장갑을 낀 손이라
피부나 얼굴을 만들 필요가 없다.

## 무기

권총 · 산탄총과 부속의 실측 · 배율 · 가동부 분리 · 본 배치는
[`09-weapons.md`](09-weapons.md) 에 있다. **무기 모델이 팔 클립보다 먼저다** —
그립 위치가 바뀌면 팔 클립 8개를 다시 만든다.

## 제작 순서

0. [`09-weapons.md`](09-weapons.md) 대로 무기를 게임 크기로 맞추고 가동부를
   떼어낸다. **끝** (2026-08-09, 모델 4종 수령)
1. [`00-rigging.md`](00-rigging.md) 대로 전신 Humanoid 리그와 컨트롤 리그를 세운다.
   **끝** (2026-08-09, 본 60개 = deform 55 + Root + IK 타깃 4)
2. `RIG_TEST` 로 파지 · 관통 · 접지를 승인받는다. **여기를 통과하기 전에는 클립을
   만들지 않는다** — 리그가 틀린 채로 만든 클립은 전부 다시 만든다.
   **끝** (2026-08-09, 10항목 전부 통과 · [`00-rigging.md`](00-rigging.md) 승인 포즈)
3. 팔 메시를 분리하고 팔 리그를 만든다.
   **끝** (2026-08-09, 팔 2,520 tris · deform 44 · 무기 7조각까지 물림)
4. [`10-hand-poses.md`](10-hand-poses.md) 대로 손 포즈 12종을 만든다. **여기서
   그립이 확정된다** — 클립은 이 열두 개 사이를 오갈 뿐이다.
   **끝** (2026-08-09, 손목 전부 35° 안)
5. `HandIK` 와 프롭 그립 트랜스폼을 붙여 **레버 하나로 런타임 IK 를 먼저 검증한다.**
6. 맨손 → 운반 → 권총 → 산탄총 → 피격·사망 순으로 팔 17종. **끝**
   (2026-08-09, 17종 전부 · 루프 다섯 전부 0.000 mm).
   아래는 만들어 가며 남긴 기록이다.
   **재장전 둘을 먼저 만들었다** — 제일 어려운 것이 통과하면 나머지는 기계적이다.
   `Arms_PistolReload` 끝 (2026-08-09, 이음새 0.000 mm · 최악 손목 29.4°).
   `Arms_ShotgunReload` 도 끝 (2026-08-09, 이음새 0.000 mm · 끝에 펌프 왕복 포함).
   `Arms_ShotgunFirePump` 도 끝 (30f, 최악 손목 32.2°).
7. 전신 4종. **끝** (2026-08-09, `animate_body.py` · 루프 셋 전부 0.000 mm).
   `Body_Grabbed` 이 처형용으로 늘었다.
8. [`unity-integration.md`](unity-integration.md) 와 [`qa-checklist.md`](qa-checklist.md)
   로 검수한다.

## 상세 문서

IMPORTANT: 아래 여덟 문서는 **전신 28종 기준으로 쓰였다.** 위 표에 남은 클립의
포즈 · 타이밍 설명은 그대로 유효하지만, 전신 전용 클립과 `ValveTurn` 서술은
이 문서가 대체한다. 각 묶음을 만들 때 그 문서를 위 범위에 맞춰 고친다.

| 묶음 | 문서 | 이번 범위에서 |
| --- | --- | --- |
| 이동 | [`01-locomotion.md`](01-locomotion.md) | 전신 `Idle` · `Walk` 만 남는다 |
| 웅크리기 | [`02-crouch.md`](02-crouch.md) | **전부 뺀다** |
| 점프·착지 | [`03-jump-land.md`](03-jump-land.md) | **전부 뺀다** |
| 상호작용 | [`04-interaction.md`](04-interaction.md) | `Interact` · `LeverPull`. `ValveTurn` 은 뺀다 |
| 운반 | [`05-carry.md`](05-carry.md) | 4종 그대로. `CarryWalk` 는 Sway 로 대체 |
| 권총 | [`06-pistol.md`](06-pistol.md) | 4종 그대로 |
| 산탄총 | [`07-shotgun.md`](07-shotgun.md) | 4종 그대로 |
| 피격·사망 | [`08-damage-death.md`](08-damage-death.md) | 팔 기준으로 다시 쓴다 |

무기와 손은 위 여덟 문서보다 뒤에 정해졌고 이번 범위 기준으로 쓰였다.

| 묶음 | 문서 |
| --- | --- |
| 무기 모델·가동부 | [`09-weapons.md`](09-weapons.md) |
| 손 포즈 12종 | [`10-hand-poses.md`](10-hand-poses.md) |
