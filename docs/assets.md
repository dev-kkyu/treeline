# 외부 에셋 및 오픈소스 출처

제출물인 "AI 활용 기술 문서"에 그대로 옮겨 쓸 원본이다. 출처와 라이선스 명시는
**제출 요건**이며, 누락하면 저작권 문제로 선발이 취소될 수 있다.

IMPORTANT: 에셋을 받은 **그 자리에서** 기록한다. 나중에 몰아서 쓰려 하면
"이 텍스처를 어디서 받았더라"를 복원할 수 없다. 이 파일에서 유일하게 중요한
규칙이다.

**커밋** 열은 원본 파일을 저장소에 넣었는지를 뜻한다. 판단 기준은 `CLAUDE.md`의
에셋 정책을 따른다. 요약하면 CC0 / MIT 등 재배포가 허용된 것만 커밋하고,
에셋스토어 표준 EULA 에셋은 `Assets/ThirdParty/` 에 두어 저장소에서 제외한다.

## IMPORTANT: 제3자 에셋을 쓰지 않는다 (2026-08-09 확정)

**무료 에셋을 포함해 남이 만든 것을 게임에 넣지 않는다.** 모델 · 텍스처 ·
사운드 · 이펙트는 전부 우리가 만든다. 이유는
[`tech-design/decisions.md`](tech-design/decisions.md) 에 있다.

2026-08-09 확인 결과 **지금 프로젝트에 제3자 에셋은 없다.**
`Assets/ThirdParty/` 폴더 자체가 없고, 2 장 씬이 참조하는 것은 모두
`Assets/Game/` 아래다. 아래 표에 남은 제3자 항목은 폰트 하나뿐이다.

예외로 볼 여지가 있는 것은 **IBM Plex Sans KR** 하나다. 한글 폰트는 자체 제작
대상이 아니고 OFL 이 임베드를 명시적으로 허용한다. 바꿀지는 사람이 정한다.

## 에셋

모델, 텍스처, 사운드, 폰트, 이펙트 등.

| 이름 | 종류 | 출처 | 라이선스 | 커밋 | 사용처 | 추가일 |
| --- | --- | --- | --- | --- | --- | --- |
| Treeline 동결압 UI 키트 | UI PNG | 팀 자체 제작 — Codex 내장 이미지 생성 + 로컬 후처리 | 프로젝트 자체 제작 | O | HUD · 타이틀 · 설정 · 피격 효과 | 2026-08-08 |
| IBM Plex Sans KR (Regular · SemiBold) | 폰트 TTF | https://github.com/IBM/plex — 받은 곳은 Google Fonts 저장소의 `ofl/ibmplexsanskr` | SIL Open Font License 1.1 (Reserved Font Name "Plex") | O | 게임 내 모든 한글 텍스트 | 2026-08-08 |

IMPORTANT: OFL 은 **제품에 임베드하고 재배포하는 것을 명시적으로 허용**한다. 그래서
원본 TTF 를 커밋한다. 조건은 두 가지다 — 라이선스 전문(`OFL.txt`)을 함께 두고,
**폰트 자체를 단독 상품으로 팔지 않는다.** Reserved Font Name 조항이 있으므로
글꼴을 고쳐 배포할 때는 이름에서 `Plex` 를 빼야 하지만, 우리는 고치지 않고 그대로
쓰므로 해당하지 않는다.

<!--
행 예시 (형식 참고용, 실제 데이터 아님):

| Ultimate Low Poly Nature | 3D 모델 | https://quaternius.com/... | CC0 | O | 야외 지형 | 2026-08-10 |
| Sci-Fi UI Pack | UI | Unity Asset Store (링크) | Asset Store 표준 EULA | X | HUD | 2026-08-12 |
-->

## AI 로 생성한 것

모델 · 텍스처 · 사운드 · 이미지. **우리가 만든 것이므로 그냥 커밋한다.** 여기 적는
것은 올리기 위한 절차가 아니라 **무엇을 AI 로 만들었는지의 기록**이다. 제출물인
"AI 활용 기술 문서" 의 핵심 재료이고, 이 과제에서는 많을수록 좋은 쪽이다.

도구 이름과 생성일만 있으면 충분하다. 커밋을 미루면서까지 확인할 것은 없다.
「결과물 권리」 열은 나중에 약관을 볼 일이 생겼을 때 채운다. 비워 두어도 된다.

| 이름 | 종류 | 도구 | 결과물 권리 | 사용처 | 생성일 |
| --- | --- | --- | --- | --- | --- |
| Treeline 튜토리얼 카드 프레임 | UI PNG | Codex 내장 이미지 생성 + 로컬 크로마 제거 | 프로젝트 자체 제작 | 튜토리얼 · 조작법 화면 | 2026-08-08 |
| Treeline 파티클 VFX 키트 | 파티클 PNG · Unity 프리팹 | Codex 내장 이미지 생성 + Pillow + Unity ParticleSystem | 프로젝트 자체 제작 | 눈보라 · 화염 · 총구 섬광 · 전기 · 폭발 | 2026-08-08 |
| Treeline 환경음 32종 | 사운드 (MP3) | ElevenLabs Text to Sound Effects (Starter 유료 플랜) | 유료 플랜 상업 라이선스. 해지 후에도 생성분의 권리는 유지된다 | 앰비언스 · 발소리 · 문 · 전기 | 2026-08-08 |
| Treeline 상호작용 · 장치음 21종 | 사운드 (WAV · MP3) | ElevenLabs Text to Sound Effects (Starter 유료 플랜) | 유료 플랜 상업 라이선스 | 레버 · 밸브 · 획득 · 운반 · 셔터 · 인화 · 폭발 · 발전기 · 제설기 | 2026-08-08 |
| Treeline 배경음악 10종 | 음악 (MP3) | ElevenLabs Eleven Music `music_v2` (Starter 유료 플랜) | 유료 플랜에 음악 상업 이용 포함. 대형 스튜디오 게임 배급은 별도 라이선스가 필요하나 이 과제 범위에는 해당 없다 | 타이틀 · 1~6장 · 조명 복구 · 발각 · 엔딩 | 2026-08-08 |
| Treeline 적 소리 28종 | 사운드 (WAV · MP3) | ElevenLabs Text to Sound Effects (Starter 유료 플랜) | 유료 플랜 상업 라이선스 | Crawler · Clinger · Colossus. enemy-sheets 의 클립 명세와 이벤트 프레임에 맞춰 생성 | 2026-08-08 |
| Crawler | 3D 모델 · 텍스처 | Meshy AI (적응형 리메시 · 2048 텍스처 3장) | 미확인 | 적 「기는 것」 | 2026-08-08 |
| Crawler 리그 · 애니메이션 6종 | 스켈레탈 애니메이션 | Claude Code 가 작성한 Blender 파이썬 스크립트 (`Treeline/ModelSources/Crawler/`) | 프로젝트 자체 제작 | 적 「기는 것」 | 2026-08-08 |
| Colossus | 3D 모델 · 텍스처 | Meshy AI (적응형 리메시 8,634 tris · 2048 텍스처 3장) | 미확인 | 적 「죽지 않는 것」 | 2026-08-08 |
| Colossus 리그 · 애니메이션 15종 | 스켈레탈 애니메이션 | Claude Code 가 작성한 Blender 파이썬 스크립트 (`Treeline/ModelSources/Colossus/`) | 프로젝트 자체 제작 | 적 「죽지 않는 것」 | 2026-08-09 |
| 월드 표면 텍스처 7종 (albedo · normal · mask 각 3장) | 텍스처 PNG | Claude Code 가 작성한 절차적 생성 스크립트 (`docs/world-textures/gen_textures.py`) | 프로젝트 자체 제작 | 벽 · 바닥 · 철판 · 그레이팅 · 눈 · 둥지 | 2026-08-09 |
| 손전등 · 권총 메시 | 3D 모델 | Claude Code 가 Unity MCP 로 프리미티브를 조합하고 `Mesh.CombineMeshes` 로 병합 | 프로젝트 자체 제작 | R&D 검증용. 대안 경로로 보존 | 2026-08-09 |
| 2장 복도 칸 모듈 | 3D 모델 (FBX) | Claude Code 가 작성한 Blender 파이썬 스크립트 | 프로젝트 자체 제작 | 2장 숙소동 복도 한 칸 (벽 · 바닥 · 천장 · 배관 · 조명기구) | 2026-08-09 |
| 2장 복도 소품 2종 | 3D 모델 (FBX) | 〃 | 프로젝트 자체 제작 | 사물함 3연 · 이층침대 | 2026-08-09 |
| 2장 상호작용 소품 6종 | 3D 모델 (FBX) | 〃 | 프로젝트 자체 제작 | 얼음문 · 얼음(6띠) · 배전함 · 히터 · 셔터 · 셔터 레버 | 2026-08-09 |
| 2장 복도 머터리얼 13종 | URP 머터리얼 | Claude Code 가 `execute_code` 로 생성 (텍스처 없이 색 · 금속성 · 매끄러움만) | 프로젝트 자체 제작 | 2장 전체 | 2026-08-09 |
| 소품 30종 (모델 · 텍스처 · 머티리얼 · 프리팹) | 3D 모델 · 텍스처 | Meshy AI text-to-3D. 프롬프트는 [`tech-design/props.md`](tech-design/props.md) 에 있고, 임포트 후처리는 Claude Code 가 Unity MCP 로 | 미확인 | 1 · 2 · 6장 소품 전반 | 2026-08-09 |

IMPORTANT: 소품은 **Meshy 가 준 FBX 를 저장소에 넣지 않는다.** 파일 하나가 4~9 MB 라
30 종이면 215 MB 인데, 대부분은 텍스처가 FBX 안에 묻혀 있고 정점 속성이 double 로
들어가서다. 임포트해 메시를 `.asset` 으로 뽑고 텍스처를 512 로 줄이면 40 MB 가 된다.
원본 zip 은 `Treeline/ModelSources/meshygen/` 에 있고 `.gitignore` 로 빠진다 —
**로컬에만 있으므로 다시 뽑을 일이 생기면 사람에게 물어야 한다.**
| 타이틀 설원 · 침엽수 3종 | 3D 모델 (FBX) | Claude Code 가 작성한 Blender 파이썬 스크립트 | 프로젝트 자체 제작 | 타이틀 화면 배경. 바닥은 위의 월드 눈 텍스처를 그대로 쓴다 | 2026-08-09 |

IMPORTANT: 월드 표면 텍스처는 **파일이 아니라 스크립트가 원본이다.** 결정론적이라
같은 커밋에서 같은 결과가 나오므로, 값을 고치려면 PNG 가 아니라
`gen_textures.py` 를 고치고 다시 돌린다. 절차와 판정 기준은
[`tech-design/world-assets.md`](tech-design/world-assets.md) 에 있다.

<!--
행 예시 (형식 참고용, 실제 데이터 아님):

| Crawler | 3D 모델 | Meshy AI (요금제) | 약관 확인 결과를 적는다 | 적 · 기는 것 | 2026-08-09 |
| 복도 발소리 | 사운드 | (도구명) | 미확인 | 2장 | 2026-08-10 |
-->

## 오픈소스 및 패키지

Unity 패키지, 라이브러리, 코드 스니펫 등.

| 이름 | 버전 | 출처 | 라이선스 | 용도 |
| --- | --- | --- | --- | --- |
| MCP for Unity | v10.0.0 | https://github.com/CoplayDev/unity-mcp | MIT | Claude 가 Unity 에디터를 직접 조작 |

Unity 가 기본으로 넣어주는 패키지(URP, Input System, AI Navigation 등)는 적지
않는다. Unity Companion License 로 엔진에 딸려 오는 것이라 별도 출처가 없고,
`Packages/manifest.json` 이 버전까지 이미 기록하고 있다. **직접 추가한 제3자
패키지만** 여기 적는다.

## 기록할 때 확인할 것

- **라이선스 이름을 정확히 적는다.** "무료"는 라이선스가 아니다. CC0, CC-BY 4.0,
  MIT, Asset Store 표준 EULA처럼 명시된 이름을 쓴다.
- **CC-BY 계열은 저작자 표기가 의무다.** 표기해야 할 문구가 있으면 비고에 남긴다.
- **출처는 페이지 URL로 남긴다.** 다운로드 직링크는 나중에 깨진다.
- 판단이 서지 않으면 커밋하지 말고 사용자에게 확인한다.
