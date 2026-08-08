# Crawler 애니메이션 제작 패키지

`CRAWLER-V1.0`을 Blender에서 리깅하고 Unity Generic 리그로 넘기기 위한 최종
작업 명세다. 단순 아이디어 목록이 아니라, 이 폴더의 값대로 키를 잡고 검수할 수
있도록 클립별 접지·타이밍·이벤트까지 고정했다.

## 근거와 우선순위

1. [`treeline.md`](../../../../../game-design/treeline.md)의 확정 기획: Crawler는
   네발로 낮게 깔려 빠르고, 권총 한두 발에 죽으며 적 상태는 배회·추격·공격·피격·사망이다.
2. [`Crawler.cs`](../../../../../../Treeline/Assets/Game/Enemy/Crawler.cs)와
   `CrawlerSetup.cs`의 실측: 이동 5.5 m/s, 회전 720°/s, 공격 거리 1.6 m,
   재사용 1.2초, 시체 유지 3초, Root 이동은 NavMeshAgent가 담당한다.
3. [`modeling-design.md`](../../modeling-design.md)의 외형/클립 초안과 이 폴더 위의
   4방향 렌더. 총길이 2.88 m, 최고 높이 0.98 m, 폭 1.52 m를 기준으로 한다.

문서와 코드가 다르면 **게임플레이 수치는 코드**, 외형과 포즈는 이 문서를 따른다.
현재 `Crawler.cs`에는 Animator 연결이 없으므로 실제 모델을 넣을 때
[`unity-integration.md`](unity-integration.md)의 연결 작업이 필요하다.

## 최종 클립 목록 · 30 fps

| 순서 | 클립 | 프레임 / 시간 | 루프 | 게임 상태 | 핵심 |
| ---: | --- | ---: | :---: | --- | --- |
| 1 | `Crawler_Idle_Low` | 45f / 1.50s | O | 배회 정지 | 머리는 낮게, 등 압력만 이동 |
| 2 | `Crawler_Skitter` | 24f / 0.80s | O | 배회 이동 | 대각선 2박 보행, 항상 2발 이상 접지 |
| 3 | `Crawler_Sprint` | 18f / 0.60s | O | 추격 | 뒷다리 압축→전방 낚아채기, 5.5 m/s |
| 4 | `Crawler_Attack_Underbite` | 22f / 0.73s | X | 공격 | 8f 예고, **13f 피해**, 아래 공격구 개방 |
| 5 | `Crawler_Hit` | 12f / 0.40s | X | 피격 | 낮은 중심을 유지한 짧은 옆 밀림 |
| 6 | `Crawler_Death` | 30f / 1.00s | X | 사망 | 네 다리가 안으로 접혀 쐐기가 바닥에 붙음 |

상세 제작서는 각각
[`01-idle-low.md`](01-idle-low.md), [`02-skitter.md`](02-skitter.md),
[`03-sprint.md`](03-sprint.md), [`04-attack-underbite.md`](04-attack-underbite.md),
[`05-hit.md`](05-hit.md), [`06-death.md`](06-death.md)에 있다.

각 문서의 PNG는 원본 4면 이미지를 참조해 만든 **키포즈 방향 보드**다. 실루엣과
체중 이동을 빠르게 읽는 용도이며, 가려진 반대편 다리 위치·정확한 접지 프레임·각도는
이미지보다 문서의 표와 22본 리그 명세를 우선한다.

## 제작 순서

1. [`00-rigging.md`](00-rigging.md)대로 22 deform bone 리그와 컨트롤 리그를 만든다.
2. `Idle_Low`에서 중립 자세와 변형을 먼저 승인한다.
3. 느린 `Skitter`로 접지와 IK를 검증한 뒤 같은 리그로 `Sprint`를 만든다.
4. `Attack_Underbite`의 13f 포즈를 먼저 만들고 예고와 복귀를 앞뒤로 채운다.
5. `Hit`, `Death`를 만든다. Death 마지막 포즈가 3초간 유지되어도 메시가 떨리거나
   지면 아래로 들어가지 않아야 한다.
6. [`unity-integration.md`](unity-integration.md)대로 FBX/Animator/Event를 연결하고
   [`qa-checklist.md`](qa-checklist.md)로 검수한다.

## 이번 범위에서 만들지 않는 것

- `Turn_L/R`: 회전은 NavMeshAgent의 720°/s가 담당한다. 별도 회전 클립은 발 방향과
  실제 회전량이 쉽게 어긋난다.
- `Alert/Roar`: 눈도 발성기관도 없는 약한 첫 적이며, 반응 지연은 빠른 위협이라는
  기획을 흐린다.
- 점프, 꼬리 공격, 처형: 기획·코드·히트 판정에 없다.
- 방향별 피격/사망 변형: 1차 완성 범위에서는 한 클립으로 끝낸다. 플레이테스트에서
  반복이 두드러질 때만 두 번째 Death를 추가한다.

## 산출물 이름

Blender 액션과 Unity 클립 이름을 위 표와 정확히 맞춘다. 리그 원본은
`Crawler_Rig.blend`, Unity용 FBX는 `Crawler.fbx`, 컨트롤 리그가 제거된 베이크본
루트는 `Crawler_Armature`로 한다. 파일명·액션명에 공백이나 한글을 쓰지 않는다.
