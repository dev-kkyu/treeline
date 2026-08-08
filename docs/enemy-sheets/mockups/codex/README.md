# Treeline · Codex 모델링 설계안

`docs/game-design/treeline.md`의 확정 기획만 근거로 새로 만든 독립 시안이다.
기존 모델링 문서와 이미지는 스타일 레퍼런스로 사용하지 않았다.

## 방향

프로덕션용 스타일 이름은 **동결압 생체(Frost-pressure anatomy)**다. 설명 가능한
돌연변이나 기계 생물이 아니라, 차갑고 높은 곳의 압력에 눌린 듯한 생물 형태만
공통으로 반복한다.

- 공통 요소: 검푸른 무광 피부, 압력 균열, 백색 서리 흉터, 균열 안의 젖은 조직
- 금지 요소: 옷, 장비, 산업 부품, 케이블, 갑옷, 눈, 인간 얼굴, 뿔, 털
- 구분 요소: 색이 아닌 외곽선
  - `Crawler`: 바닥을 긁는 **쐐기형 수평선**
  - `Clinger`: 접힌 마름모에서 펴지는 **가는 수직선**
  - `Colossus`: 복도 자체를 막는 **넓은 판과 네 기둥**

## 파일

| 파일 | 용도 |
| --- | --- |
| `treeline-creature-concept-v1.png` | 3종 통합 콘셉트 보드 |
| `modeling-design.md` | 치수, 토폴로지, 리그, 애니메이션 설계 |
| `material-unity-spec.md` | 팔레트, 맵, URP 머터리얼, 임포트 규격 |
| `generation-prompts.md` | 이미지/3D 생성용 프롬프트와 금지어 |
| [`../비교.md`](../비교.md) | Claude 본문과 Codex 후속 검토를 합친 비교 보고서 |

## 4방향 이미지

각 이미지는 1254×1254 PNG이며, 모델링 입력과 형태 비교를 위해 같은 스튜디오
조건으로 생성했다. 좌·우 이미지는 단순 반전이 아니라 표면과 비대칭 구조를 각각
생성한 독립 이미지다.

| 적 | 정면 | 후면 | 좌측 | 우측 |
| --- | --- | --- | --- | --- |
| Crawler | [`01-front.png`](crawler/01-front.png) | [`02-back.png`](crawler/02-back.png) | [`03-left.png`](crawler/03-left.png) | [`04-right.png`](crawler/04-right.png) |
| Clinger | [`01-front.png`](clinger/01-front.png) | [`02-back.png`](clinger/02-back.png) | [`03-left.png`](clinger/03-left.png) | [`04-right.png`](clinger/04-right.png) |
| Colossus | [`01-front.png`](colossus/01-front.png) | [`02-back.png`](colossus/02-back.png) | [`03-left.png`](colossus/03-left.png) | [`04-right.png`](colossus/04-right.png) |

- Crawler: 네 발이 지면에 닿는 낮은 중립 자세.
- Clinger: 골격 판독성을 위해 닫힌 대기 자세가 아니라 펼친 매달림 자세.
- Colossus: 네 기둥이 같은 지면을 지지하는 수정 정면을 최종 기준으로 사용.
- 이미지 간 작은 표면 균열 차이보다 `modeling-design.md`의 치수와 큰 덩어리를
  우선한다.

## 우선 제작

기획 순서대로 `Crawler` 한 종을 먼저 끝까지 통과시킨다.

1. 2.25 m 블록아웃과 검은 실루엣 검수
2. LOD0 5,200 tris 이하 리토폴로지
3. 1024 아틀라스와 공용 머터리얼 구성
4. 리그/6클립 제작
5. Unity 프리팹에서 손전등 2 m·6 m 검수
6. 통과 후 같은 규약으로 `Clinger`, 마지막에 `Colossus`

콘셉트 이미지는 방향 시안이며 정투영 도면이 아니다. 모델 치수와 기능은 문서의
수치가 우선한다.
