# Clinger Final 애니메이션 제작 패키지

`CLINGER-FINAL-1.0`을 Blender에서 리깅하고 Unity Generic 리그로 넘기기 위한 최종
작업 명세다. 천장에 붙은 상태, 코드가 처리하는 낙하, 지상 4점 보행을 하나의 리그로
연결하는 데 필요한 키포즈·접지·이벤트·충돌 전환을 고정한다.

## 근거와 우선순위

1. [`treeline.md`](../../../../../../game-design/treeline.md)의 확정 기획: Clinger는
   천장에 붙어 있다 떨어지고, 약하지만 여러 마리가 등장하며 산탄총과 장치로 상대한다.
2. [`MODEL-SPEC.md`](../MODEL-SPEC.md)의 최종 수치: 전개 2.51 m, 접힘 높이 0.72 m,
   착지 높이 1.42 m, 총 26본과 8개 클립, `Land` 15f 충돌·Agent 전환.
3. [`modeling-design.md`](../../../modeling-design.md)와 이 폴더 위의 최종 4면 이미지:
   심장형 압착 몸통, 천장 사지 4개, 공격 팔 2개, 노출 눈·다리·꼬리 없음.

현재 저장소에는 `Clinger.cs`나 `Clinger.prefab`이 없다. 따라서 수치와 이벤트 이름은
제작 계약이고, 실제 구현 시 [`unity-integration.md`](unity-integration.md)대로 새 코드와
프리팹을 연결해야 한다.

## 최종 클립 목록 · 30 fps

| 순서 | 클립 | 프레임 / 시간 | 루프 | 상태 | 핵심 |
| ---: | --- | ---: | :---: | --- | --- |
| 1 | `Clinger_Cling_Folded` | 60f / 2.00s | O | 천장 대기 | 0.72 m 안에 접고 3~5 mm 압력 이동 |
| 2 | `Clinger_Cling_Tremor` | 36f / 1.20s | O | 천장 경계 | 봉우리 4개가 다른 위상으로 짧게 떨림 |
| 3 | `Clinger_Unfold_Drop` | 24f / 0.80s | X | 전개·낙하 | 10f 고정 해제, 18f 완전 전개, 낙하는 코드 |
| 4 | `Clinger_Land` | 18f / 0.60s | X | 착지 | 15f 천장 Collider off, 지상 Collider/Agent on |
| 5 | `Clinger_Scuttle` | 24f / 0.80s | O | 지상 추격 | 공격 팔 2 + 부착 사지 2의 비대칭 4점 보행 |
| 6 | `Clinger_Attack_Underjaw` | 22f / 0.73s | X | 공격 | 9f 예고, **13f 아래턱·양손 히트** |
| 7 | `Clinger_Hit` | 10f / 0.33s | X | 피격 | 굵은 공격 팔 한쪽의 과회전과 즉시 복원 |
| 8 | `Clinger_Death` | 28f / 0.93s | X | 사망 | 갈고리 해제, 몸통 옆 낙하, 물리 래그돌 없음 |

클립별 문서는 `01`부터 `08`까지 파일에 나뉘어 있다. 각 문서의 PNG는 최종 4면도를
참조해 만든 **키포즈 방향 보드**다. 가려진 반대편 사지, 정확한 접지 프레임, 회전각은
이미지보다 문서 표와 [`00-rigging.md`](00-rigging.md)를 우선한다.

## 제작 순서

1. 24 deform + 2 non-deform 본과 천장/지상 공간 전환 컨트롤을 만든다.
2. `Cling_Folded`에서 0.72 m 바운드와 갈고리 고정을 승인한다.
3. `Unfold_Drop`과 `Land`를 연속 제작해 10f/15f 이벤트 계약을 먼저 검증한다.
4. Land 18f를 지상 정지 포즈로 삼아 `Scuttle`, `Attack`, `Hit`, `Death`를 만든다.
5. Unity에서 높이가 다른 천장 3종으로 낙하를 시험하고 WebGL 빌드에서 재검수한다.

## 별도 클립으로 만들지 않는 것

- `Fall_Loop`: `Unfold_Drop` 24f 이후 마지막 전개 포즈를 유지하고 실제 높이 이동은
  코드/중력이 처리한다. 낙하 시간이 길어도 액션 속도를 늘이지 않는다.
- `Ground_Idle`: `Land` 18f의 마지막 포즈를 정지 포즈로 사용한다. 플레이테스트에서
  지상 대기 노출이 길 때만 별도 호흡 루프를 추가한다.
- 방향별 Hit/Death: 첫 완성 범위에서는 한 액션을 좌우 반전해 쓴다.
- 눈, 날개, 꼬리 동작: 최종 해부 규칙에 해당 부위가 없다.

## 산출물 이름

Blender 원본은 `Clinger_Rig.blend`, Unity용 FBX는 `Clinger.fbx`, 베이크된 뼈대 루트는
`Clinger_Armature`로 한다. 액션 이름은 표와 정확히 맞추고 공백·한글을 쓰지 않는다.
