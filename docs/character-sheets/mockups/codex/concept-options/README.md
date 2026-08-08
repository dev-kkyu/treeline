# Treeline 주인공 · 1차 후보 6종

> **최종 선택:** `M-C-cableway-electrical-technician.png` →
> [`PROTAGONIST-MC-FINAL-1.0`](../final/README.md)

`docs/game-design/treeline.md`의 확정 기획을 기준으로 만든 선택용 시안이다. 주인공은
폭설로 고립된 산악 통신 중계소를 복구하러 온 **민간 정비공 한 명**이다. 군인이나
특수요원이 아니고 총도 능숙하게 다루지 못한다. 현대이되 국적·회사·정확한 시대는
특정하지 않는다.

## 여성 주인공 3안

| 안 | 파일 | 역할형 방향 | 게임에서 읽히는 인상 |
| --- | --- | --- | --- |
| F-A | [`F-A-field-restoration-technician.png`](F-A-field-restoration-technician.png) | 현장 복구 기사, 30대 초반 | 작고 단단한 체형과 각진 외투. 유능하지만 액션 영웅처럼 보이지 않아 기획 균형이 가장 좋다 |
| F-B | [`F-B-veteran-electrical-technician.png`](F-B-veteran-electrical-technician.png) | 베테랑 전기 정비사, 40대 초반 | 무거운 방한 작업복과 안정적인 자세. 혼자 시설을 되살릴 설득력이 가장 강하다 |
| F-C | [`F-C-relay-tower-climber.png`](F-C-relay-tower-climber.png) | 철탑 등반 정비사, 20대 후반 | 가늘고 기동적인 실루엣과 추락 방지 하네스. 외부 철탑·사다리 구간과 가장 직접적으로 연결된다 |

여성안 추천은 **F-A → F-C → F-B**다. F-A는 정비 능력과 공포 장르의 취약함이
동시에 남는다. F-B는 가장 현실적인 숙련자지만, 초반의 불안과 미숙한 총기 조작이
조금 약하게 느껴질 수 있다.

## 남성 주인공 3안

| 안 | 파일 | 역할형 방향 | 게임에서 읽히는 인상 |
| --- | --- | --- | --- |
| M-A | [`M-A-junior-relay-technician.png`](M-A-junior-relay-technician.png) | 신입 중계기 기사, 20대 후반 | 큰 외투 안의 마른 체형과 피곤한 표정. 고립·탄약 부족·서툰 사격이 가장 자연스럽다 |
| M-B | [`M-B-veteran-facility-mechanic.png`](M-B-veteran-facility-mechanic.png) | 베테랑 설비 정비사, 40대 중반 | 낮고 넓은 체형과 공구 조끼. 힘은 체육이 아니라 오랜 노동에서 나온 것으로 읽힌다 |
| M-C | [`M-C-cableway-electrical-technician.png`](M-C-cableway-electrical-technician.png) | 삭도 전기 기사, 30대 중반 | 세로로 긴 체형과 얇은 추락 방지 벨트. 철탑과 삭도 수리 목적이 외형에 바로 드러난다 |

남성안 추천은 **M-A → M-C → M-B**다. M-A가 “평범한 정비공이 예상하지 못한
상황에 갇혔다”는 공포를 가장 잘 전달한다. M-B는 강한 안정감 때문에 괴물에게 쫓기는
취약함이 상대적으로 줄어든다.

최종 선택에서는 M-C의 세로로 긴 실루엣, 삭도·철탑 작업을 바로 설명하는 하네스,
왼쪽 소매 수선 패치의 1인칭 방향 판독성이 기획과 가장 잘 맞는 것으로 확정했다.

## 공통 조형 규칙

- 방한 외투, 작업복, 장갑, 안전화의 큰 덩어리만으로 직업을 읽게 한다.
- 흑연색·회청색·탁한 올리브를 기본으로 하고 적갈색 안전 표시는 2% 이내로 쓴다.
- 소매와 장갑은 1인칭 화면에서 가장 오래 보이므로 후보의 특징을 유지한다.
- 무릎·팔꿈치·소매 끝은 마찰로 닳고, 정강이와 부츠에는 눈과 염화물 흔적이 남는다.
- 회사 로고, 국기, 군 계급장, 경찰 표식, 읽을 수 있는 글자는 사용하지 않는다.
- 방탄복·탄입대·전술 레일 장비·가스마스크는 사용하지 않는다.
- 얼굴은 영웅적 자신감보다 피로, 경계, 추위에 그을린 피부를 우선한다.
- 최종 캐릭터는 한 명만 선택한다. 여섯 명이 별도 능력을 가진 플레이어 캐릭터가 아니다.

## 제작 우선순위

1. 선택 후 정·후·좌·우 4방향 전신 시트를 만든다.
2. 얼굴 중립·긴장·통증 3표정과 장갑 낀 손의 권총·산탄총 파지 시트를 만든다.
3. 1인칭 전용 팔은 전신과 같은 소매·장갑을 쓰되 별도 메시로 제작한다.
4. 공통 인간형 리그를 사용하고 외투 자락·후드 끈 같은 물리본은 만들지 않는다.

## 생성 프롬프트 기록

Codex 기본 내장 이미지 생성의 `stylized-concept` 방식으로 6명을 각각 독립 생성했다.
공통 프롬프트는 다음 기준을 사용했다.

```text
One adult civilian maintenance technician sent alone to an anonymous modern
snowbound mountaintop communications relay station. Grounded realistic 3D game
character, full body, face and both gloved hands visible. Country-neutral insulated
industrial workwear in charcoal, graphite, faded slate and muted olive with tiny
oxide-orange safety accents. Competent with tools but exhausted and isolated; an
ordinary worker, not an action hero. No weapon, logo, flag, readable text, military
uniform, tactical armor, police gear, gas mask, fashion styling or sexualization.
```
