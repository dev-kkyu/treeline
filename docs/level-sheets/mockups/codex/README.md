# Treeline 챕터별 레벨 배경 시안

`docs/game-design/treeline.md`의 여섯 장소를 기준으로 만든 레벨 비주얼 개발 시안이다.
조감도나 3D 블록아웃이 아니라, 플레이어가 실제 레벨 안에서 바라보는 사람 눈높이의
와이드 환경 콘셉트 아트로 통일했다.

## V2 · 음산한 다각도 시안 — 현재 기준

각 챕터를 같은 공간의 네 카메라로 나눴다. 단순 좌우 반전이 아니라 실제 플레이
위치가 달라진다.

| 장 | 01 · 진입 전경 | 02 · 역방향 동선 | 03 · 핵심 장치 | 04 · 위협·은신 |
| --- | --- | --- | --- | --- |
| 1 · 관제동 | [진입](ch01-control-building/v2-eerie/01-entry-wide.png) | [창가에서 입구](ch01-control-building/v2-eerie/02-reverse-path.png) | [옥상 서치라이트](ch01-control-building/v2-eerie/03-objective-context.png) | [콘솔 아래](ch01-control-building/v2-eerie/04-threat-hiding.png) |
| 2 · 숙소동 | [복도 진입](ch02-dormitory/v2-eerie/01-entry-wide.png) | [얼어붙은 문에서 입구](ch02-dormitory/v2-eerie/02-reverse-path.png) | [히터·배전함](ch02-dormitory/v2-eerie/03-objective-context.png) | [침상 안쪽](ch02-dormitory/v2-eerie/04-threat-hiding.png) |
| 3 · 연료 갱도 | [경비실 하강](ch03-fuel-mine/v2-eerie/01-entry-wide.png) | [연료고에서 귀환로](ch03-fuel-mine/v2-eerie/02-reverse-path.png) | [펌프·누출 구간](ch03-fuel-mine/v2-eerie/03-objective-context.png) | [연료통 뒤 계단](ch03-fuel-mine/v2-eerie/04-threat-hiding.png) |
| 4 · 발전동 | [상부 진입 캣워크](ch04-generator-nest/v2-eerie/01-entry-wide.png) | [발전기 뒤에서 입구](ch04-generator-nest/v2-eerie/02-reverse-path.png) | [축전지실 내부](ch04-generator-nest/v2-eerie/03-objective-context.png) | [정비구에서 천장](ch04-generator-nest/v2-eerie/04-threat-hiding.png) |
| 5 · 외부 철탑 | [붕괴 통로 출구](ch05-exterior-towers/v2-eerie/01-entry-wide.png) | [두 번째 철탑에서 역방향](ch05-exterior-towers/v2-eerie/02-reverse-path.png) | [제설기 옆](ch05-exterior-towers/v2-eerie/03-objective-context.png) | [제설 경로 아래](ch05-exterior-towers/v2-eerie/04-threat-hiding.png) |
| 6 · 삭도 승강장 | [승강장 진입](ch06-cable-car-terminal/v2-eerie/01-entry-wide.png) | [삭도에서 입구](ch06-cable-car-terminal/v2-eerie/02-reverse-path.png) | [권양기·연료](ch06-cable-car-terminal/v2-eerie/03-objective-context.png) | [정비구 은신](ch06-cable-car-terminal/v2-eerie/04-threat-hiding.png) |

네 시점의 역할은 다음과 같다.

1. `01-entry-wide` — 챕터에 들어왔을 때의 첫 인상과 진행 방향
2. `02-reverse-path` — 목표 지점에서 되돌아본 귀환·추격 동선
3. `03-objective-context` — 장치 재질과 주변 공간의 조명 관계
4. `04-threat-hiding` — 낮은 엄폐 시점에서의 가림, 수직 위협, 긴 그림자

음산함은 전체 노출만 낮추는 방식으로 만들지 않았다. 진행 장치의 작은 광원은 남기고,
그 빛이 닿지 않는 공간을 크게 두었으며, 흐트러진 눈·기름의 파문·움직이는 그림자처럼
적을 직접 보여주지 않는 흔적을 추가했다.

## 공통 미술 방향

- 현대의 국적 불명 산악 통신 중계소다. 미래 시설이나 군사 SF처럼 보이지 않는다.
- 낡은 회색 콘크리트, 먹색 도장 강재, 케이블 트레이와 보온 배관을 공통 자산으로
  반복한다.
- 안전색은 녹슨 적갈색, 진행 장치의 빛은 제한적인 주황색으로 묶는다.
- 실내는 청흑색 그림자, 실외는 청회색 백색광을 사용한다. 5장부터 공포의 색을
  검정에서 흰색으로 뒤집는다.
- 적은 넣지 않았다. 배경 시안만으로 공간의 용도와 장치, 긴장 방식이 먼저 읽혀야 한다.

## 제작에 적용할 때

이 이미지는 최종 평면도나 정확한 치수표가 아니라 **레벨의 화면 목표**다. 실제 동선과
충돌 판정은 Unity 회색박스를 우선하고, 이미지에서는 다음 세 가지를 가져간다.

1. 입구에서 바라본 첫 인상과 주 시선축
2. 챕터를 대표하는 랜드마크와 조명 대비
3. 반복 제작할 재질, 소품군, 눈·얼음·유기물의 밀도

V2 음산한 다각도 생성 프롬프트는
[v2-eerie-prompts.md](v2-eerie-prompts.md)에 기록했다.
