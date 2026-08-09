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
| Treeline 메뉴 클릭음 1종 | 사운드 (WAV) | ElevenLabs Text to Sound Effects (Starter 유료 플랜) | 유료 플랜 상업 라이선스 | 타이틀 · 엔딩 메뉴의 버튼 | 2026-08-09 |
| Treeline 무기음 15종 | 사운드 (WAV · MP3) | ElevenLabs Text to Sound Effects (Starter 유료 플랜) | 유료 플랜 상업 라이선스 | 권총 · 산탄총의 발사 · 빈 방아쇠 · 탄창 · 슬라이드 · 펌프 · 탄 장전 · 장착 · 탄착 · 탄피 | 2026-08-09 |
| Treeline 주인공 소리 8종 | 사운드 (WAV · MP3) | ElevenLabs Text to Sound Effects (Starter 유료 플랜) | 유료 플랜 상업 라이선스 | 피격 2종 · 사망 · 도약 · 착지 · 힘쓰기 · 발버둥 · 지친 호흡 루프 | 2026-08-09 |
| Crawler | 3D 모델 · 텍스처 | Meshy AI (적응형 리메시 · 2048 텍스처 3장) | 미확인 | 적 「기는 것」 | 2026-08-08 |
| Crawler 리그 · 애니메이션 6종 | 스켈레탈 애니메이션 | Claude Code 가 작성한 Blender 파이썬 스크립트 (`Treeline/ModelSources/Crawler/`) | 프로젝트 자체 제작 | 적 「기는 것」 | 2026-08-08 |
| Colossus | 3D 모델 · 텍스처 | Meshy AI (적응형 리메시 8,634 tris · 2048 텍스처 3장) | 미확인 | 적 「죽지 않는 것」 | 2026-08-08 |
| Colossus 리그 · 애니메이션 15종 | 스켈레탈 애니메이션 | Claude Code 가 작성한 Blender 파이썬 스크립트 (`Treeline/ModelSources/Colossus/`) | 프로젝트 자체 제작 | 적 「죽지 않는 것」 | 2026-08-09 |
| 주인공 | 3D 모델 · 텍스처 | Meshy AI (T-pose, 8,706 tris · 2048 텍스처 한 벌) | 미확인 | 플레이어. 그림자 · 엔딩 원경 · 1인칭 팔의 원본 | 2026-08-09 |
| 주인공 Humanoid 리그 | 스켈레탈 리그 (본 60개) | Claude Code 가 작성한 Blender 파이썬 스크립트 (`Treeline/ModelSources/Protagonist/`) | 프로젝트 자체 제작 | 전신 3종의 바탕 | 2026-08-09 |
| 주인공 1인칭 팔 리그 | 스켈레탈 리그 (본 51개) + 팔 메시 6,332 tris | 〃 (`build_arms.py`). 메시는 전신에서 잘라내고 손·팔뚝 바깥을 섭디비전했으며 무기 7조각을 본에 물렸다 | 프로젝트 자체 제작 | 팔 17종의 바탕 | 2026-08-09 |
| 주인공 팔·전신 클립 21종 | 애니메이션 | 〃 (`animate_arms.py` · `animate_body.py`). 파지·팔꿈치·구도를 전부 메시와 프리팹에서 재서 푼다 | 프로젝트 자체 제작 | 1인칭 17종 + 전신 4종 | 2026-08-09 |
| `Pistol.fbx` · `Shotgun.fbx` · `PistolMag.fbx` · `ShotgunShell.fbx` | Unity 임포트 산출물 | Meshy 원본 4종을 `prep_weapons.py` 로 게임 크기·방향에 맞춘 뒤 `export_weapons.py` 로 합쳐 내보냈다 | 프로젝트 자체 제작 (원본 Meshy) | 레벨에 놓는 무기 프롭. 뷰모델과 같은 메시·텍스처 | 2026-08-09 |
| `Protagonist_Arms.fbx` · `Protagonist_Body.fbx` | Unity 임포트 산출물 | 〃 (`export_fbx.py`) 로 뽑고 임포터 설정은 Unity MCP 로 넣었다 | 프로젝트 자체 제작 | `Assets/Game/Player/Models/` | 2026-08-09 |
| 주인공·무기 텍스처 10장 | 2048 base color · normal | Meshy 생성물을 blend 에서 꺼낸 것 | 미확인 (Meshy 원본과 같다) | `Assets/Game/Player/Textures/`. metallic·roughness·emission 은 버리고 머티리얼에서 metallic 0 · smoothness 0.25 로 덮는다 | 2026-08-09 |
| 손 포즈 라이브러리 12종 | 포즈 데이터 (`hand_poses.py`) | 〃 (`solve_poses.py`) 가 실제 무기 메시에 손가락을 감아 풀어낸 값 | 프로젝트 자체 제작 | 팔 클립 17종의 손 모양 | 2026-08-09 |
| 권총 · 권총 탄창 · 산탄총 · 산탄총 탄 | 3D 모델 · 텍스처 | Meshy AI (각 600~8,999 tris) | 미확인 | 플레이어 무기 | 2026-08-09 |
| 무기 게임 크기 · 가동부 분리 | 3D 모델 (Blender) | Claude Code 가 작성한 Blender 파이썬 스크립트 (`Treeline/ModelSources/Weapons/prep_weapons.py`) | 프로젝트 자체 제작 | 슬라이드 · 펌프 · 탄창 · 탄피를 본에 물릴 수 있게 쪼갠 것 | 2026-08-09 |
| 월드 표면 텍스처 15종 (albedo · normal · mask 각 3장) | 텍스처 PNG | Claude Code 가 작성한 절차적 생성 스크립트 (`docs/world-textures/gen_textures.py`) | 프로젝트 자체 제작 | 벽 · 바닥 · 철판 · 그레이팅 · 눈 · 둥지 · 갱도 암반 · 고인 연료 · 그을림 · 둥지 바닥 · 섬유 · 다져진 눈 · 아연도금 · 착빙 | 2026-08-09 |
| 손전등 · 권총 메시 | 3D 모델 | Claude Code 가 Unity MCP 로 프리미티브를 조합하고 `Mesh.CombineMeshes` 로 병합 | 프로젝트 자체 제작 | R&D 검증용. 대안 경로로 보존 | 2026-08-09 |
| 2장 복도 칸 모듈 | 3D 모델 (FBX) | Claude Code 가 작성한 Blender 파이썬 스크립트 | 프로젝트 자체 제작 | 2장 숙소동 복도 한 칸 (벽 · 바닥 · 천장 · 배관 · 조명기구) | 2026-08-09 |
| 2장 복도 소품 2종 | 3D 모델 (FBX) | 〃 | 프로젝트 자체 제작 | ~~사물함 3연 · 이층침대~~ **Meshy 소품으로 교체, 파일 삭제** | 2026-08-09 |
| 2장 상호작용 소품 6종 | 3D 모델 (FBX) | 〃 | 프로젝트 자체 제작 | **얼음(6띠)만 남음** — 얼음문 · 배전함 · 히터 · 셔터 · 셔터 레버는 Meshy 소품으로 교체하고 파일 삭제 | 2026-08-09 |
| 2장 복도 머터리얼 13종 | URP 머터리얼 | Claude Code 가 `execute_code` 로 생성 (텍스처 없이 색 · 금속성 · 매끄러움만) | 프로젝트 자체 제작 | **5종만 남음** (얼음 · 배관 · 튜브 · 발광) — 나머지는 소품 머티리얼로 대체하고 삭제 | 2026-08-09 |
| 소품 30종 (모델 · 텍스처 · 머티리얼 · 프리팹) | 3D 모델 · 텍스처 | Meshy AI text-to-3D. 프롬프트는 [`tech-design/props.md`](tech-design/props.md) 에 있고, 임포트 후처리는 Claude Code 가 Unity MCP 로 | 미확인 | 1 · 2 · 6장 소품 전반 | 2026-08-09 |

IMPORTANT: 소품은 **Meshy 가 준 FBX 를 저장소에 넣지 않는다.** 파일 하나가 4~9 MB 라
30 종이면 215 MB 인데, 대부분은 텍스처가 FBX 안에 묻혀 있고 정점 속성이 double 로
들어가서다. 임포트해 메시를 `.asset` 으로 뽑고 텍스처를 512 로 줄이면 40 MB 가 된다.
원본 zip 은 `Treeline/ModelSources/meshygen/` 에 있고 `.gitignore` 로 빠진다 —
**로컬에만 있으므로 다시 뽑을 일이 생기면 사람에게 물어야 한다.**
| 타이틀 설원 · 침엽수 3종 | 3D 모델 (FBX) | Claude Code 가 작성한 Blender 파이썬 스크립트 | 프로젝트 자체 제작 | **설원만 남음** — 침엽수 3종(`Title_Conifer_A/B/C`)은 아래 Meshy 나무로 교체하고 파일 삭제 | 2026-08-09 |
| 타이틀 · 엔딩 배경 4종 | 3D 모델 (FBX) | 〃 | 프로젝트 자체 제작 | 설산 능선 · 중계소 철탑 · 삭도 케이블. 두 화면이 같은 모델을 공유한다 | 2026-08-09 |
| 배경 나무 3종 (모델 · 텍스처 · 머티리얼 · 프리팹) | 3D 모델 · 텍스처 | Meshy AI text-to-3D. 프롬프트는 [`tech-design/props.md`](tech-design/props.md) 31~33 번, 임포트 후처리는 Claude Code 가 Unity MCP 로 | 미확인 | 타이틀 · 엔딩 배경의 수목한계선. 침엽수 · 죽은 침엽수 · 쓰러진 통나무 | 2026-08-09 |
| ~~플레이 영상 내레이션 7줄~~ | 음성 (WAV) | Windows SAPI 5 (Microsoft Heami, ko-KR) | 확인 필요 — **제출물에 안 쓴다** | **제출 영상에서 뺐다 (2026-08-09 팀 결정)** | 2026-08-09 |
| 3장 갱도 아치 메시 · 머티리얼 5종 | 3D 메시 · URP 머티리얼 | Claude Code 가 `execute_code` 로 단면을 만들어 z 로 뽑고, 링마다 흔들어 굴착면을 냄 | 프로젝트 자체 제작 | 3장 연료 갱도의 통로 껍데기 · 암반 · 젖은 바닥 · 고인 것 · 붉은 안전등 | 2026-08-09 |

IMPORTANT: **내레이션 음성은 제출 영상에 넣지 않기로 했다 (2026-08-09).**
합성음 티가 남고 게임 소리를 덮는 쪽이 손해라는 판단이다. 자막은 그대로 두므로
같은 문장은 전달된다. **이 결정으로 아래의 권리 문제도 함께 사라진다** — 쓰지
않는 것에는 라이선스가 필요 없다. 파일과 타이밍은 지우지 않고 남겨 두었다.

경위는 기록으로 남긴다. Windows 에 딸려 오는 SAPI 음성이라 받은 것도 비용도
없었지만 **「무료로 깔려 있다」와 「출력물을 공개 제출물에 써도 된다」는 다른
문제**여서 미확인으로 두고 있었다. 그 전에 **Typecast 를 먼저 시도했고 실패했다** —
키는 환경변수 `TYPECAST_API_TOKEN` 에 있고 형식도 정상(`__plt…`)인데
`api.typecast.ai` 가 **키를 안 보냈을 때와 똑같은 401** 을 준다. 만료·폐기이거나
계정에 API 권한이 없는 것이다.

나중에 목소리를 다시 넣기로 하면 **ElevenLabs 유료 플랜**이 답이다. 게임의 소리를
전부 그것으로 만들었고 상업 이용을 포함하므로 권리 문제가 없다. 영상
파이프라인은 음성 파일만 갈아 끼우면 그대로 돌아간다 — 자막과 믹스 타이밍이 파일
길이를 측정해서 따라오게 되어 있다.

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
