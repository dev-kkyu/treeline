# Treeline

폭설로 고립된 산 위 통신 중계소. 정비공 한 명이 시설을 되살려 삭도를 움직이고,
산 아래로 내려간다. **1인칭 공포 게임.**

**NHN GAME X AI HACKATHON (NAN2026) 사전 과제 · Team SBD**

[![플레이 영상](https://img.youtube.com/vi/MSelbFOCfLQ/maxresdefault.jpg)](https://youtu.be/MSelbFOCfLQ)

| | |
| --- | --- |
| ▶ **플레이** (브라우저, 설치 없음) | **https://dev-kkyu.github.io/treeline/** |
| 🎬 플레이 영상 (47초) | https://youtu.be/MSelbFOCfLQ |

데스크톱 Chrome · Edge 권장, 키보드와 마우스로. **소리를 켜고 해주세요** —
어둠과 소리가 이 게임의 절반입니다.

## 어떤 게임인가

총은 한 마리를 죽이고, 시설은 여럿을 죽이고, **죽지 않는 놈도 있다.**

위에서 아래로, 실내에서 실외로 **여섯 장소**를 거쳐 내려간다. 관제동에서
손전등을 줍고, 숙소동에서 무기 없이 도망치고, 연료 갱도에서 처음 쏘고,
발전동의 둥지에서 버티고, 눈보라 속 철탑을 지나, 승강장에서 죽지 않는 것에게
쫓기며 삭도를 기동한다. 목표가 사슬로 이어져 있어 플레이어는 언제나 **왜 여기
가는지**를 안다.

전체 기획은 [`docs/game-design/treeline.md`](docs/game-design/treeline.md),
소개 문서는 [`docs/game-design/overview.md`](docs/game-design/overview.md).

## AI 로 만들었다

이 과제는 **개발 과정에서 AI 를 어떻게 활용했는가**가 심사 대상이다. AI 는 게임
안에 들어가지 않는다 — 대신 개발의 거의 전부를 AI 가 했다.

- **커밋 416개 전부**가 Claude Code 세션에서 나왔다. 씬 · 프리팹 배치까지
  Unity MCP 로 AI 가 직접 했다
- 적 3종과 소품 55종은 **Meshy 생성 → Claude 가 쓴 Blender 파이썬으로 리깅** —
  사람이 Blender UI 를 열지 않았다
- 게임의 **모든 소리 137종**은 ElevenLabs 생성. AI 가 귀 없이 소리를 계측으로
  검수했다
- 프롬프트 **1,053개**가 훅으로 자동 기록되어
  [`docs/ai/prompts/`](docs/ai/prompts/) 에 그대로 있다

정리된 문서는 [`docs/ai/submission.md`](docs/ai/submission.md), 환경 구조는
[`docs/ai/setup.md`](docs/ai/setup.md).

## 제출물

| 제출물 | 위치 |
| --- | --- |
| 플레이 가능 빌드 | https://dev-kkyu.github.io/treeline/ (`gh-pages`) |
| 소스 코드 | 이 저장소 `develop` — 커밋 기록이 곧 AI 활용 근거다 |
| 플레이 영상 | https://youtu.be/MSelbFOCfLQ |
| 게임 소개 PDF | [`docs/submission/Treeline-게임소개.pdf`](docs/submission/Treeline-게임소개.pdf) |
| AI 활용 기술 PDF | [`docs/submission/Treeline-AI활용기술.pdf`](docs/submission/Treeline-AI활용기술.pdf) |
| 팀원 롤 기술서 PDF | [`docs/submission/Treeline-팀원롤기술서.pdf`](docs/submission/Treeline-팀원롤기술서.pdf) |

## Team SBD

| 이름 | 실제로 맡은 것 |
| --- | --- |
| **임성규** | 골격 · 진행 · 상호작용 · 레벨(1 · 2 · 5장) · 배포 · 운영 |
| **이정범** | 플레이어 · 캐릭터와 적 리깅 전량 · 4 · 6장 · 엔딩 연출 |
| **김도한** | 적 코드 · 사운드 전량 · 연출 · UI · 3장 · 타이틀과 엔딩 |

역할이 어떻게 배정됐고 실제로 어떻게 달라졌는지는
[`docs/roles.md`](docs/roles.md).

## 저장소 구조

```
Treeline/   Unity 프로젝트 (6000.3.21f1 · URP · WebGL)
docs/       문서 — 기획(game-design) · 기술(tech-design) · AI 환경(ai)
.claude/    팀 공용 Claude Code 설정 — 훅 · 규칙 · 스킬
```

문서가 많은 저장소다. 결정마다 **왜 그렇게 정했는지와 버린 안**을
`decisions.md` 에 남겼고(기술 202건 · 기획 21건), 그것이 이 프로젝트가 일한
방식이다.

## 개발 환경

팀원용 셋업 절차는 [`docs/team-setup.md`](docs/team-setup.md). 이 저장소는
Claude Code 설정을 공유하므로(`.claude/`) 클론 후 그 체크리스트를 먼저 본다.
