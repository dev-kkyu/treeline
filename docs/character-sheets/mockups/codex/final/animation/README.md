# 주인공 애니메이션 제작 패키지

`PROTAGONIST-MC-FINAL-1.0`을 Blender에서 Unity Humanoid 리그로 제작하기 위한 최종
명세다. 현재 `PlayerMotor`, `PlayerWeapon`, `PlayerHealth`, `PlayerInteractor`,
`Carriable`의 실제 수치와 입력 규칙을 기준으로 한다.

## 최종 클립 · 30 fps

| # | 클립 | 프레임 / 시간 | 루프 | 코드 기준 |
| ---: | --- | ---: | :---: | --- |
| 1 | `Player_Idle` | 90 / 3.00s | O | 정지 |
| 2 | `Player_Walk` | 30 / 1.00s | O | 2.8m/s |
| 3 | `Player_Sprint` | 24 / 0.80s | O | 4.6m/s |
| 4 | `Player_ExhaustedWalk` | 36 / 1.20s | O | 1.8m/s |
| 5 | `Player_CrouchEnter` | 6 / 0.20s | X | 코드 0.18s |
| 6 | `Player_CrouchIdle` | 60 / 2.00s | O | h 1.10m |
| 7 | `Player_CrouchWalk` | 36 / 1.20s | O | 1.4m/s |
| 8 | `Player_CrouchExit` | 6 / 0.20s | X | 천장 막힘 시 금지 |
| 9 | `Player_JumpStart` | 8 / 0.27s | X | jumpHeight 0.9m |
| 10 | `Player_Airborne` | 12 / 0.40s | O | 높이 이동은 코드 |
| 11 | `Player_Land` | 10 / 0.33s | X | 접지 시 재생 |
| 12 | `Player_InteractReach` | 18 / 0.60s | X | 일반 E 상호작용 |
| 13 | `Player_LeverPull` | 36 / 1.20s | X | 장치 손잡이와 IK |
| 14 | `Player_ValveTurn` | 60 / 2.00s | X | 양손 순차 회전 |
| 15 | `Player_CarryLift` | 24 / 0.80s | X | Carriable 획득 |
| 16 | `Player_CarryIdle` | 60 / 2.00s | O | 양손 운반 |
| 17 | `Player_CarryWalk` | 36 / 1.20s | O | 무기 비활성 |
| 18 | `Player_CarryDrop` | 18 / 0.60s | X | 허공 E 내려놓기 |
| 19 | `Player_PistolEquip` | 15 / 0.50s | X | WeaponKind 0 |
| 20 | `Player_PistolIdle` | 60 / 2.00s | O | 2손 로우 레디 |
| 21 | `Player_PistolFire` | 8 / 0.27s | X | 2 shots/s, recoil 1.6° |
| 22 | `Player_PistolReload` | 54 / 1.80s | X | 8발 탄창 |
| 23 | `Player_ShotgunEquip` | 18 / 0.60s | X | WeaponKind 1 |
| 24 | `Player_ShotgunIdle` | 60 / 2.00s | O | 견착 로우 레디 |
| 25 | `Player_ShotgunFirePump` | 30 / 1.00s | X | 0.9 shots/s, recoil 3.4° |
| 26 | `Player_ShotgunReload` | 78 / 2.60s | X | 4발, 끝에서 일괄 충전 |
| 27 | `Player_Hit` | 12 / 0.40s | X | 0.8s 무적과 별개 |
| 28 | `Player_Death` | 36 / 1.20s | X | `Died` 후 입력 off |

Root Motion은 전 클립에서 끈다. 실제 이동·점프·낙하·콜라이더는 코드가 담당한다.

## 보드·문서 묶음

| 묶음 | 문서 | 키포즈 |
| --- | --- | --- |
| 이동 | [`01-locomotion.md`](01-locomotion.md) | [`PNG`](01-locomotion-keyposes.png) |
| 웅크리기 | [`02-crouch.md`](02-crouch.md) | [`PNG`](02-crouch-keyposes.png) |
| 점프·착지 | [`03-jump-land.md`](03-jump-land.md) | [`PNG`](03-jump-land-keyposes.png) |
| 상호작용 | [`04-interaction.md`](04-interaction.md) | [`PNG`](04-interaction-keyposes.png) |
| 운반 | [`05-carry.md`](05-carry.md) | [`PNG`](05-carry-keyposes.png) |
| 권총 | [`06-pistol.md`](06-pistol.md) | [`PNG`](06-pistol-keyposes.png) |
| 산탄총 | [`07-shotgun.md`](07-shotgun.md) | [`PNG`](07-shotgun-keyposes.png) |
| 피격·사망 | [`08-damage-death.md`](08-damage-death.md) | [`PNG`](08-damage-death-keyposes.png) |

## 제작 순서

1. [`00-rigging.md`](00-rigging.md)의 Humanoid T-pose와 손가락 파지를 승인한다.
2. Idle→Walk→Sprint→Crouch로 Base Layer를 완성한다.
3. Jump/Land와 Carry로 전신 중심 이동과 접지를 검증한다.
4. 회색 프록시로 권총·산탄총 손 IK와 재장전 시간을 고정한다.
5. 시설 레버·밸브의 씬 IK를 연결하고 Hit/Death를 마지막에 만든다.
6. [`unity-integration.md`](unity-integration.md)와 [`qa-checklist.md`](qa-checklist.md)로
   기존 코드·WebGL 빌드를 검수한다.

