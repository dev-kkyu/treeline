# Treeline 총기 세트 · 1차 후보 5종

게임 기획의 권총·산탄총을 한 계열로 묶은 선택용 시안이다. 주인공은 군인이 아닌
정비공이고, 배경은 국적과 시대를 강하게 특정하지 않는 현대 산악 중계소다. 따라서
군용 커스텀 총기가 아니라 시설 비상 보관함이나 정비반에서 오래 관리한 공구 같은
총기를 기준으로 삼았다.

## 후보

| 안 | 파일 | 방향 | 장점 | 주의점 |
| --- | --- | --- | --- | --- |
| A | [`A-relay-locker-standard.png`](A-relay-locker-standard.png) | Relay Locker Standard | 가장 현실적이고 읽기 쉬운 시설 비상총. 기존 FPS 조작 기대와 충돌이 적다 | 고유성은 다섯 안 중 가장 낮다 |
| B | [`B-brake-house-heavy.png`](B-brake-house-heavy.png) | Brake-House Heavy | 짧고 둔중해 탄약 한 발의 무게와 큰 반동이 잘 느껴진다 | 산탄총 실루엣이 매우 짧아 일반 펌프액션보다 특수총처럼 보일 수 있다 |
| C | [`C-frostline-utility.png`](C-frostline-utility.png) | Frostline Utility | 쐐기형 큰 면과 넓은 펌프가 UI의 각진 산업 디자인에 가장 잘 맞는다 | A보다 현대 제품처럼 보이므로 마모 조절이 중요하다 |
| D | [`D-cable-tunnel-compact.png`](D-cable-tunnel-compact.png) | Cable-Tunnel Compact | 좁은 갱도와 1인칭 화면에서 차지하는 면적이 작고, 굵은 펌프가 잘 읽힌다 | 권총 탄창이 이미지에서 길게 보여 최종 모델에서는 8발 규격으로 줄여야 한다 |
| E | [`E-survey-crew-service-pair.png`](E-survey-crew-service-pair.png) | Survey Crew Service Pair | 수리판과 교체 부품이 정비공 서사에 자연스럽고 손때가 강하다 | 둥근 상부 때문에 오래된 특정 시대 총기로 읽히지 않게 세부를 단순화해야 한다 |

## 4방향 시트

각 시트는 `좌상단=총구 정면`, `우상단=후면`, `좌하단=좌측`, `우하단=우측` 순서다.
front/back은 총기의 길이축, left/right는 총기의 측면을 뜻한다.

| 안 | 권총 | 산탄총 |
| --- | --- | --- |
| A | [`pistol-turnaround.png`](turnarounds/A-relay-locker-standard/pistol-turnaround.png) | [`shotgun-turnaround.png`](turnarounds/A-relay-locker-standard/shotgun-turnaround.png) |
| B | [`pistol-turnaround.png`](turnarounds/B-brake-house-heavy/pistol-turnaround.png) | [`shotgun-turnaround.png`](turnarounds/B-brake-house-heavy/shotgun-turnaround.png) |
| C | [`pistol-turnaround.png`](turnarounds/C-frostline-utility/pistol-turnaround.png) | [`shotgun-turnaround.png`](turnarounds/C-frostline-utility/shotgun-turnaround.png) |
| D | [`pistol-turnaround.png`](turnarounds/D-cable-tunnel-compact/pistol-turnaround.png) | [`shotgun-turnaround.png`](turnarounds/D-cable-tunnel-compact/shotgun-turnaround.png) |
| E | [`pistol-turnaround.png`](turnarounds/E-survey-crew-service-pair/pistol-turnaround.png) | [`shotgun-turnaround.png`](turnarounds/E-survey-crew-service-pair/shotgun-turnaround.png) |

Codex 추천 순서는 **C → A → E → D → B**다. C는 제목·HUD의 각진 금속 프레임과
같은 큰 면을 쓰면서도 과도한 전술 장비 느낌이 없고, 어두운 1인칭 화면에서 펌프와
슬라이드가 가장 명확하게 분리된다.

## 기획 고정값

| 항목 | 권총 | 산탄총 |
| --- | --- | --- |
| 획득 | 3장 연료 갱도 | 5장 외부 철탑 |
| 장전 수 | 8발 | 4발 |
| 여분 최대 | 24발 | 12발 |
| 재장전 | 1.8초 | 2.6초 |
| 역할 | Crawler 한 마리를 1~2발로 처리 | Clinger 무리를 뚫는 강한 한 발 |
| 조작 인상 | 정비공의 불안정한 한손 반동 | 느리고 큰 양손 펌프·반동 |

## 공통 제작 방향

- 권총은 탈착식 8발 탄창, 산탄총은 4발 튜브와 왕복 가능한 펌프를 외형으로 판독한다.
- 무광 흑연색 블루잉 강철, 닳은 니켈 모서리, 흑갈색 페놀 수지 손잡이를 쓴다.
- 적갈색은 정비 확인선이나 안전 표시의 작은 면적에만 쓴다.
- 손이 닿는 면은 기름과 마찰로 조금 반들거리며, 보호된 홈에만 서리 찌꺼기를 둔다.
- 광학조준경, 레일, 레이저, 소음기, 군용 위장, 국가·회사 표식은 사용하지 않는다.
- 실제 총기 브랜드를 복제하지 않고, 작동 구조만 설득력 있게 유지한다.
- MeshyAI 변환을 고려해 작은 나사보다 큰 연속 면과 명확한 실루엣을 우선한다.

## 생성 프롬프트 세트

Codex 기본 내장 이미지 생성의 `stylized-concept` 방식으로 각 후보를 독립 생성했다.
공통 프롬프트는 다음 조건을 고정했다.

```text
Exactly two firearms: one compact 8-round semi-automatic pistol and one 4-shell
pump-action shotgun. Anonymous modern snowbound relay station, civilian maintenance
worker, realistic hard-surface game asset concept, matte graphite steel, worn nickel
edges, dark phenolic grips, tiny oxide-red maintenance marks and frost in protected
seams. Plausible controls and mechanisms. No brand, nation, text, ammunition, hands,
military optics, rails, suppressor, camouflage, sci-fi, steampunk or watermark.
```

후보별 변형은 A `시설 보관함 표준형`, B `전방 중량형`, C `동계 장갑용 쐐기형`,
D `좁은 갱도 압축형`, E `수리판이 남은 측량반 구형`이다.

4방향 시트는 각 후보 원본을 디자인 레퍼런스로 넣고, 권총과 산탄총을 분리해 같은
개체의 front/back/left/right 정투영을 2×2 배열로 재구성했다. 기본 내장 이미지
생성을 사용했으며 새 부품 추가와 디자인 변경을 금지했다.
