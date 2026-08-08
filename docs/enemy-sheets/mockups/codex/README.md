# Treeline · Codex 모델링 설계안

`docs/game-design/treeline.md`의 확정 기획만 근거로 새로 만든 독립 시안이다.
기존 모델링 문서와 이미지는 스타일 레퍼런스로 사용하지 않았다.

## 방향

프로덕션용 스타일 이름은 **동결압 생체(Frost-pressure anatomy)**다. 설명 가능한
돌연변이나 기계 생물이 아니라, 차갑고 높은 곳의 압력에 눌린 듯한 생물 형태만
공통으로 반복한다.

- 공통 요소: 검푸른 무광 피부, 압력 균열, 백색 서리 흉터, 균열 안의 젖은 조직
- 금지 요소: 옷, 장비, 산업 부품, 케이블, 갑옷, 인간 얼굴, 뿔, 털
- 눈 규칙: Crawler와 Colossus Final은 눈 없음. Clinger v2만 비발광 다안을 쓴다
- 구분 요소: 색이 아닌 외곽선
  - `Crawler`: 바닥을 긁는 **쐐기형 수평선**
  - `Clinger`: 접힌 마름모에서 펴지는 **가는 수직선**
  - `Colossus`: 쌍봉 등각과 긴 팔이 만드는 **거대한 너클 보행 M자**

## 파일

| 파일 | 용도 |
| --- | --- |
| `treeline-creature-concept-v1.png` | 3종 통합 콘셉트 보드 |
| `modeling-design.md` | 치수, 토폴로지, 리그, 애니메이션 설계 |
| `material-unity-spec.md` | 팔레트, 맵, URP 머터리얼, 임포트 규격 |
| `generation-prompts.md` | 이미지/3D 생성용 프롬프트와 금지어 |
| [`colossus/final/MODEL-SPEC.md`](colossus/final/MODEL-SPEC.md) | Colossus Final 치수·메시·리그·충돌·머터리얼 확정표 |
| [`crawler/animation/`](crawler/animation/README.md) | Crawler 22본 리그와 6종 클립의 프레임별 제작·Unity 연결 명세 |
| [`../비교.md`](../비교.md) | Claude 본문과 Codex 후속 검토를 합친 비교 보고서 |

## 채택 버전

| 모델 | 현재 태그 | 상태 | 핵심 변경 |
| --- | --- | --- | --- |
| Crawler | `CRAWLER-V1.0` | 채택 | 삽날 머리, 바닥 쐐기, 눈 없음 |
| Clinger | `CLINGER-V2.0` | **최신** | 뒤틀린 4사지 관절, 전면 비대칭 8안 |
| Colossus | `COLOSSUS-FINAL-1.0` | **최종 채택** | D안, 쌍봉 등각, 매몰 쐐기 머리, 2팔·2다리 너클 보행 |

버전 태그는 폴더와 파일명에 함께 넣는다. v1 이미지는 덮어쓰지 않고 비교·회귀용으로
보존한다.

## 현재 방향 이미지

각 이미지는 1254×1254 PNG이며, 모델링 입력과 형태 비교를 위해 같은 스튜디오
조건으로 생성했다. 좌·우 이미지는 단순 반전이 아니라 표면과 비대칭 구조를 각각
생성한 독립 이미지다.

| 버전 | 정면 | 후면 | 좌측 | 우측 | 상면 | 하면 |
| --- | --- | --- | --- | --- | --- | --- |
| `CRAWLER-V1.0` | [`01-front.png`](crawler/01-front.png) | [`02-back.png`](crawler/02-back.png) | [`03-left.png`](crawler/03-left.png) | [`04-right.png`](crawler/04-right.png) | — | — |
| `CLINGER-V2.0` | [`front-v2`](clinger/v2/clinger-v2-01-front.png) | [`back-v2`](clinger/v2/clinger-v2-02-back.png) | [`left-v2`](clinger/v2/clinger-v2-03-left.png) | [`right-v2`](clinger/v2/clinger-v2-04-right.png) | — | — |
| `COLOSSUS-FINAL-1.0` | [`front`](colossus/final/colossus-final-01-front.png) | [`back`](colossus/final/colossus-final-02-back.png) | [`left`](colossus/final/colossus-final-03-left.png) | [`right`](colossus/final/colossus-final-04-right.png) | [`top`](colossus/final/colossus-final-05-top.png) | [`bottom`](colossus/final/colossus-final-06-bottom.png) |

- Crawler: 네 발이 지면에 닿는 낮은 중립 자세.
- Clinger: 골격 판독성을 위해 펼친 매달림 자세. 눈은 전면에만 있고 후면에는 없다.
- Colossus Final: 팔 2 + 다리 2, 손·발은 각 3지다. 쌍봉 등각이 4.50m
  최고점이며 노출 눈과 꼬리는 없다.
- 이미지 간 작은 표면 균열 차이보다 `modeling-design.md`의 치수와 큰 덩어리를
  우선한다.

### v1 보관본

- [`Clinger v1`](clinger/) — 정돈된 관절, 눈 없음
- [`Colossus v1`](colossus/) — 둥근 두 판, 눈 없음
- [`Colossus v2`](colossus/v2/) — 생체 방벽 판과 심연 속 15안
- [`Colossus 후보 5종`](colossus/concept-options/) — Final 선택 이전 탐색안

## 전체 모델 수치표

아래 값은 이미지 픽셀을 재서 얻은 값이 아니라 **Blender/Unity에서 맞출 제작 목표치**다.
단위는 m, 삼각형 수는 tris다. 닫힌 메시가 나오기 전에는 실제 체적(L)을 정확히
계산할 수 없으므로 리터 대신 게임 제작에 필요한 바운드 치수를 적는다.

### 크기

| 모델 | 기본 바운드(길이/깊이 × 폭 × 높이) | 추가 자세·제약 |
| --- | --- | --- |
| `CRAWLER-V1.0` | `2.88 × 1.52 × 0.98 m` | 몸통+머리 1.90m, 꼬리 약 0.98m |
| `CLINGER-V2.0` | 펼침 `0.48 × 1.10 × 2.51 m` | 접힘 `0.28 × 0.42 × 0.72m`, 착지 높이 1.42m, 천장 3.4m 권장 |
| `COLOSSUS-FINAL-1.0` | `2.10 × 3.44 × 4.50 m` | 몸통 폭 2.64m, 바운딩박스 32,508L, 실제 체적은 최종 메시에서 측정 |

### 메시·리그·텍스처 예산

| 모델 | LOD0 시작 / 상한 | LOD1 / LOD2 | deform 본 상한 | 아틀라스 | 슬롯 | 동시 화면 상한 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Crawler | 4,000 / 5,200 | 2,000 / 700 | 24 | 1024²×2 | 1 | 6 |
| Clinger v2 | 3,500 / 4,200 | 1,800 / 600 | 24 | 1024²×2 | 1 | 8 |
| Colossus Final | 9,000 / 11,000 | 4,500 / 1,500 | 28 | 2048²×2 | 1 | 1 |

`×2`는 BaseMap(RGB+Smoothness A)과 Normal 두 장이다. 눈 때문에 별도 머터리얼
슬롯을 늘리지 않고 같은 아틀라스 안에 눈 영역을 배치한다.

### 눈

| 모델 | 수 | 위치 | 동작·머터리얼 |
| --- | ---: | --- | --- |
| Crawler | 0 | — | 머리 방향과 아래 공격구로 의도 전달 |
| Clinger v2 | 8 | 중앙 균열 전면 상·중부 | 고정 시선, 비발광, Smoothness 0.72, 작은 손전등 반사만 허용 |
| Colossus Final | 0 | — | 정면의 작은 구멍은 감각공이며 안구·약점이 아님 |

### Unity 충돌·이동 초안

| 모델 | 본체 콜라이더 | NavMeshAgent | 이동 메모 |
| --- | --- | --- | --- |
| Crawler | Capsule h 0.90, r 0.40, center y 0.45 | r 0.40, h 0.90, speed 5.5 | 커밋된 프리팹 값 유지 |
| Clinger v2 | 천장 Sphere r 0.32 / 착지 Capsule h 1.30, r 0.35 | r 0.35, h 1.42, speed 4.2 | Land 마지막 프레임에 Agent 켬 |
| Colossus Final | Box `2.90 × 4.20 × 1.60` | r 1.35, h 4.50, speed 1.15 | 체력 사망 없음, 임계 피격 시 2.50m 밀림 |

## v2 변경 기록

### `CLINGER-V2.0`

- 네 리본 사지의 관절 회전축을 번갈아 90° 뒤틀어 정상 해부학을 제거했다.
- 중앙 균열에 크기가 다른 비발광 눈 8개를 매립했다.
- 눈은 앞면에만 있으며 후면은 건조하게 봉합한다.

### `COLOSSUS-V2.0`

- 둥근 몸을 겹겹의 동결압 생체 방벽 판으로 교체했다.
- 중앙 심연 안에 큰 눈 3개와 작은 눈 12개를 깊이별로 배치했다.
- 앞 사지 2개는 보행과 케이블 붙잡기를 겸하는 갈라진 집게, 뒤 사지 2개는 지지 기둥이다.
- 눈은 약점이 아니며 `IDamageable` 체력 사망과 Death 클립을 만들지 않는다.

## 최종 채택 기록

### `COLOSSUS-FINAL-1.0`

- 후보 D `Twin-ridge Juggernaut`를 최종 정면으로 원본 그대로 채택했다.
- 쌍봉 등각, 매몰 쐐기 머리, 바깥쪽 장완과 안쪽 중량 다리로 Crawler 계보를 확장했다.
- 팔 2·다리 2, 손·발 각 3지, 눈 0, 꼬리 0으로 해부 수량을 고정했다.
- 정·후·좌·우·상·하 6면 이미지와 별도 [`MODEL-SPEC.md`](colossus/final/MODEL-SPEC.md)를 제작 기준으로 사용한다.

## 우선 제작

기획 순서대로 `Crawler` 한 종을 먼저 끝까지 통과시킨다.

1. 2.88m 블록아웃과 검은 실루엣 검수
2. LOD0 4,000 tris 시작, 필요할 때만 5,200까지 올림
3. 1024 아틀라스와 공용 머터리얼 구성
4. 리그/6클립 제작
5. Unity 프리팹에서 손전등 2 m·6 m 검수
6. 통과 후 같은 규약으로 `Clinger`, 마지막에 `Colossus`

콘셉트 이미지는 방향 시안이며 정투영 도면이 아니다. 모델 치수와 기능은 문서의
수치가 우선한다.
