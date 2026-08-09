# Unity 연결 계약

현재 `Player.prefab`에는 전신 Animator와 애니메이션 bridge가 없다. 아래 구성은 기존
이동·전투 코드를 바꾸지 않고 시각 상태를 따라가게 하는 추가 계약이다.

## 필요한 컴포넌트

```text
Player
├─ PlayerMotor / PlayerHealth / WeaponSwitcher          기존
├─ CharacterController                                  기존
├─ PlayerAnimatorBridge                                  신규
├─ WorldBody / Animator (Humanoid)                       신규
└─ CameraPivot / ViewModelRoot / Animator (Humanoid)     신규
```

`PlayerAnimatorBridge`는 입력을 다시 읽지 않는다. 아래 기존 컴포넌트의 결과 상태와
이벤트만 Animator parameter로 전달한다.

## 파라미터

| 이름 | 형식 | 원천 |
| --- | --- | --- |
| `Speed` | float | CharacterController.velocity XZ magnitude |
| `MoveX`, `MoveY` | float | velocity를 Player local로 변환 |
| `VerticalSpeed` | float | velocity.y |
| `Grounded` | bool | CharacterController.isGrounded |
| `Crouched` | bool | PlayerMotor.IsCrouching |
| `Sprinting` | bool | PlayerMotor.IsSprinting |
| `Exhausted` | bool | PlayerMotor.IsExhausted |
| `Carrying` | bool | 활성 Carriable 소유 상태 |
| `Weapon` | int | -1 없음, 0 Pistol, 1 Shotgun |
| `Reloading` | bool | PlayerWeapon.IsReloading |
| `Alive` | bool | PlayerHealth.IsAlive |
| `Jump`, `Land`, `Interact`, `Lever`, `Valve` | trigger | 상태 전환 이벤트 |
| `CarryLift`, `CarryDrop`, `Equip`, `Fire`, `Hit`, `Death` | trigger | 해당 컴포넌트 이벤트 |

`Animator.StringToHash`를 Awake에서 캐시하고 매 프레임 문자열을 만들지 않는다.

## 레이어

| 레이어 | 모드 | 마스크 | 우선순위 |
| --- | --- | --- | ---: |
| `Base` | Override | 전신 | 0 |
| `UpperBodyAction` | Override | Spine 이상 | 1 |
| `HandsIK` | IK/constraint | 양팔·손 | 2 |
| `FullBodyAction` | Override | 전신 | 3 |

- Base는 Idle/Walk/Sprint/Exhausted/Crouch/Jump/Land를 담당한다.
- UpperBodyAction은 Interact/Carry 보조/Pistol/Shotgun을 담당한다.
- FullBodyAction은 CarryLift/Drop, Hit, Death를 담당하며 Death가 항상 최우선이다.
- 전신과 1인칭 Animator는 같은 parameter를 받되 전신은 Main Camera Culling Mask에서 숨긴다.

## 이벤트

| 이벤트 | 용도 |
| --- | --- |
| `InteractCommit` | 지연형 장치 동작 실행. 미구현 시 기존 즉시 호출 유지 |
| `AttachProp` / `DetachProp` | CarrySocket 부모 전환·물리 토글 |
| `WeaponReady` | Equip 종료 뒤 Fire 허용 |
| `Fire` | 시각·SFX만. 실제 Raycast는 기존 PlayerWeapon이 담당 |
| `MagazineIn`, `TubesClosed` | 손·효과음 동기화 |
| `ReloadComplete` | 시각 완료. 탄약 변경은 기존 코드 종료 시점이 우선 |
| `Eject` | 산탄총 탄피 효과. 풀링 사용 |

## 전환 시간

- Base locomotion 0.12s, Walk↔Sprint 0.10s, Crouch enter/exit 0.06s.
- Equip 0.08s, Fire 0.02s, Reload 0.06s, Hit 0.03s, Death 0.00s.
- Carry 시작·종료 0.08s. Death 진입 뒤 다른 상태로 나가는 transition은 만들지 않는다.

## 현재 코드와의 차이

1. `PlayerMotor`는 점프/착지 이벤트와 velocity를 공개하지 않는다. bridge가 같은
   GameObject의 CharacterController를 읽고 이전 Grounded와 비교한다.
2. `Carriable`은 플레이어가 현재 무엇을 드는지 전역으로 공개하지 않는다. Attach 시
   bridge에 상태를 전달하는 최소 이벤트가 필요하다.
3. `PlayerHealth`는 Hit 이벤트가 없고 Died만 있다. `HealthChanged`에서 이전 값 감소를
   감지해 Hit trigger를 낸다.
4. `PlayerInteractor`는 즉시 실행한다. 제출 범위에서는 애니메이션만 뒤따르게 하고,
   손 접촉과 실제 장치 시간을 맞출 때만 지연 호출을 추가한다.
5. 기존 `PlayerWeapon`의 transform 기반 reload dip은 Arms Animator 적용 시 꺼야 한다.
   둘을 함께 켜면 무기가 두 번 내려간다.

실제 코드를 붙이기 전까지 이 문서는 연결 계약이며 기존 동작을 수정하지 않는다.


## 임포트 실측 — 2026-08-09

`export_fbx.py` 가 blend 에서 FBX 와 텍스처를 뽑고, 임포터 설정은 Unity MCP 로
넣었다. 손으로 만진 곳은 없다.

```
Assets/Game/Player/
├── Models/     Protagonist_Arms.fbx (7.2 MB) · Protagonist_Body.fbx (2.7 MB)
├── Textures/   <이름>_BaseColor.png · <이름>_Normal.png  (2048, 10장)
└── Materials/  M_Protagonist · M_Pistol · M_PistolMag · M_Shotgun · M_Shell · M_TubeFill
```

| | 실측 |
| --- | --- |
| 본 | 팔 **47** (ViewModelRoot + deform 46) · 전신 **56** (Root + deform 55) |
| 렌더러 | 팔 **8** (팔 + 무기 7조각), 전신 1. 머티리얼 전부 연결 |
| 삼각형 | 팔 6,332 · 권총 5,062 · 슬라이드 4,046 · 산탄총 7,838 |
| 클립 | 17 + 4, 프레임 수와 이벤트 프레임이 명세와 일치 |
| 콘솔 | 경고 · 오류 0 |

`Hand.R` 이 Unity 에서 **x +0.205** 에 온다. 캐릭터의 오른쪽이 Unity 의 오른쪽이고,
Blender 의 −y 가 +z 로 나간다 ([`00-rigging.md`](00-rigging.md)).

### 컨트롤 본은 나가지 않는다

`use_armature_deform_only=True` 로 뽑는다. `CTRL_*` 넷은 deform 이 아니고 자식도
없으니 빠지고, `Root` · `ViewModelRoot` 는 deform 자식이 있어 남는다. 결과가 위의
47 · 56 이고 이것이 00-rigging.md 가 말한 내보내기 본 수다.

### `bake_anim_simplify_factor` 는 0 이어야 한다

기본값은 곡선을 줄인다. 25 mm 짜리 슬라이드 왕복이나 8 mm 짜리 호흡처럼 **작고
빠른 것이 먼저 없어진다.** 클립은 이미 프레임당 키 하나라 줄일 것도 없다.

### IMPORTANT: 임포터의 이벤트 시각은 초가 아니라 0~1 이다

`ModelImporterClipAnimation.events` 의 `time` 은 **클립 길이에 대한 비율**이다.
초를 넣으면 마지막 이벤트가 클립 끝에 오도록 **클립이 늘어난다** — 1.77초짜리
권총 재장전이 3.12초로, 4.37초짜리 `Body_Grabbed` 이 17.32초로 나왔다.
`(프레임 − 1) / 마지막프레임` 으로 넣는다.

### 애니메이션 타입은 Generic 이다

00-rigging.md 는 전신을 Humanoid 로 세웠지만 **임포트는 둘 다 Generic** 이다.
리타게팅하는 곳이 없고 — 이 리그로 만든 클립을 이 리그로만 튼다 — Humanoid 는
근육 범위로 포즈를 잘라낸다. `Body_Grabbed` 처럼 등을 젖히고 매달리는 자세가 그
대상이다. 본 이름은 Humanoid 규약을 그대로 따르므로 나중에 리타게팅이 필요해지면
아바타만 만들면 된다.
