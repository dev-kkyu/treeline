# 07 · 산탄총

![키포즈](07-shotgun-keyposes.png)

보드의 회색 산탄총은 4발 펌프액션 파지 프록시다. 최종 무기 모델을 선택해도 오른손
grip, 왼손 pump, 오른쪽 shoulder socket 세 기준점은 유지한다.

| 클립 | 프레임 | 손·이벤트 계약 |
| --- | ---: | --- |
| `Player_ShotgunEquip` | 18f | 0 아래, 14 견착, **16 WeaponReady**, 18 Idle |
| `Player_ShotgunIdle` | 60f loop | 개머리판 우측 어깨, 총구 조준점 아래 9°, 왼손 pump |
| `Player_ShotgunFirePump` | 30f | **1 Fire**, 3 +3.4° 반동, 10 pump unlock, 15 후퇴, **16 Eject**, 23 전진·장전, 30 Idle |
| `Player_ShotgunReload` | 78f | 0 안쪽 roll, 12 포트 노출, 18/32/46/60 shell pose, **68 TubesClosed**, **78 ReloadComplete** |

- 현재 발사 간격은 0.9 shots/s, 약 33.3f다. 30f 액션 뒤 최소 4f cooldown을 코드가 보장한다.
- 카메라 반동 3.4°/±1.2°와 팔 반동이 합쳐져 과장되지 않게 팔에는 45%만 넣는다.
- 재장전 코드는 2.6초 끝에서 필요한 탄을 일괄 충전한다. 시각상 4개 shell pose가 있어도
  중간 취소 시 일부 탄이 들어가지 않는다. 부분 장전을 구현하기 전까지 이 계약을 유지한다.
- Fire가 Reload보다 우선하지 않는다. 재장전 중 발사 입력은 무시하는 현재 코드와 맞춘다.
- Crouch에서는 개머리판 위치를 유지하고 팔꿈치만 4cm 위로 보정해 무릎 관통을 막는다.

