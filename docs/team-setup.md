# 팀 셋업 체크리스트

이 저장소는 Claude Code 설정을 팀 공용으로 공유한다 (`.claude/`). 클론 후
아래를 한 번만 해두면 세 명이 동일한 환경에서 작업하게 된다.

이미 클론해 둔 사람은 아래 「이번 pull 후 할 것」만 보면 된다.

---

## 이번 pull 후 할 것

```bash
git pull --rebase --autostash
```

### 2026-08-09 · 청각이 두 벌이었다 — **정해졌다. A 안 그대로** (B)

**`Crawler` 와 `Colossus` 가 서로 다른 규칙으로 소리를 듣는다.** 지금 터지지는
않지만, **발소리가 두 벌이었던 것과 같은 모양**이다 — 파일이 달라서 git 이 조용히
합쳐준다. 스텔스 밸런스를 고칠 때 두 군데를 고쳐야 하고 한쪽을 빠뜨린다.

| | `Colossus` (A) | `Crawler` (B) |
| --- | --- | --- |
| 소리 크기를 정하는 쪽 | **내는 쪽** — `PlayerMotor.NoiseRadius` | **듣는 쪽** — `hearRange` 13m 고정 |
| 조용함의 기준 | 웅크림 4 · 걷기 9 · 달리기 16m | `playerSpeed >= hearSpeed`(2.6) |
| 듣고 나서 | 즉시 추격 + 포효 | `Investigate` — 가서 본다 |

IMPORTANT: [`tech-design/architecture.md`](tech-design/architecture.md) 는
「적(B)은 `PlayerMotor.NoiseRadius` 를 읽어 청각 판정」이라고 **계약으로** 적어
두었는데 지금 그게 사실이 아니다. 문서가 틀린 채로 있으면 다음 사람이 그 전제로
짠다.

수치도 어긋난다. **걷기 2.8 m/s 가 `hearSpeed` 2.6 을 넘어 `Crawler` 에게는 걷는
것도 들린다.** 걷기 9m · 달리기 16m 로 갈라둔 것은 「뛰면 위험하다」를 가르치려는
장치인데, 2장에서는 그 규칙이 성립하지 않는다.

**A 의 제안 — 크기는 `NoiseRadius` 로 통일하고 반응은 적마다 다르게 둔다.**
`Crawler` 는 듣고 `Investigate`, `Colossus` 는 듣고 추격. 스텔스 수치가 한 곳에
모이고 성격은 갈린다. `Investigate` 를 거치는 것 자체는 A 쪽보다 나은 설계라
그건 그대로 두자는 뜻이다.

**정하는 것은 B 다.** `Crawler.cs` 는 B 소유이고, 위는 A 가 6장을 붙이다 발견한
것을 옮긴 것뿐이다. 정해지면 진 쪽을 지우고 `architecture.md` 를 같이 고친다.

#### 결론 — 크기는 `NoiseRadius`, 반응은 적마다 (2026-08-09, B)

A 안을 그대로 받았다. `Crawler` 의 `hearRange` · `hearSpeed` 를 지우고
`PlayerMotor.NoiseRadius * hearingScale` 을 읽는다. 반응은 `Investigate` 그대로다.

**받은 이유는 A 가 버그를 하나 찾아 줬기 때문이다.** 걷기가 2.8 m/s 인데
`hearSpeed` 가 2.6 이라 **걷기만 해도 들렸고**, 그러면 웅크림 4 · 걷기 9 ·
달리기 16m 로 갈라둔 것이 2장에서 무의미해진다. 「뛰면 위험하다」가 성립하지
않는 상태였다.

내가 굳이 다시 만든 이유는 「플레이어 속도를 밖에 내주는 계약이 없다」고 봤기
때문인데, **그게 틀렸다.** 계약은 *속도*가 아니라 *소음 반경*으로 이미 있었고
내가 엉뚱한 것을 찾았다. `architecture.md` 도 같이 고쳤다.

곁다리 둘 중 하나도 처리했다 — **`ChaseMusic` 을 `IHunter` 인터페이스로 열었다.**
`Crawler` 와 `Colossus` 가 둘 다 구현하므로, 6장 `_Audio` 에 `ChaseMusic` 을 놓고
`Mus_Ch6` · `Mus_Chase` 를 꽂으면 2장과 같은 페이드 · 더킹이 붙는다.
**`Cue_Chase` 를 생으로 켜고 끄던 것은 그때 지운다.** 6장 씬은 내 것이 아니라
건드리지 않았다.

**`colossus-roar.wav` 는 여전히 안 걸려 있다.** 6장 씬 작업이라 남겨 둔다.

곁다리로 둘 더 — **`ChaseMusic` 이 `Crawler[] hunters` 라 `Colossus` 를 못 받는다.**
그래서 2장은 페이드와 더킹이 있고 6장은 생으로 켜고 끈다. 인터페이스로 열면 6장도
같은 방식이 된다. 그리고 **`colossus-roar.wav` 를 2.36 초에서 2.000 초로 잘라
`Ch6` 의 `Colossus/Snd_Roar` 에 걸었다** (A). `roarSeconds` 가 2 라 남기면 이미
달려오는데 포효가 이어진다. 원본을 덮어썼으니 다시 뽑을 때 길이를 2 초로 맞춘다.

### 2026-08-09 · NavMesh 에이전트 타입이 둘이 되었다 (A)

`ProjectSettings/NavMeshAreas.asset` 에 **`Colossus` 타입**(ID `-1372625422`,
반경 2.4, 높이 4.4)을 넣었다. 레이어처럼 **셋이 함께 건드리는 파일**이라 알린다.
타입을 더할 일이 생기면 서로 말하고 넣는다. 두 사람이 각자 넣으면 ID 가 갈려
씬마다 다른 타입을 가리키게 된다.

- **`Colossus` 를 놓는 씬에는 `NavMeshSurface` 가 둘 있어야 한다.** 같은
  GameObject 에 컴포넌트를 두 개 붙이고 `agentTypeID` 만 다르게 둔다. 데이터는
  `<씬>.asset` 과 `<씬>_Colossus.asset` 으로 따로 뗀다. **하나만 두면 그 적은
  경로를 못 찾아 제자리에 선다 — 에러도 경고도 안 뜬다.** 지금은 `Ch6` 만 그렇다
- **`NavMeshAgent.radius` 만 올리는 것은 효과가 없다.** 그 값은 회피와 조향에만
  쓰이고 경로는 구워둔 면을 그대로 읽는다. 값과 굽기는 같이 간다
- **반경 2.4 는 지름 4.8m 다. 이 적이 다니는 통로는 7m 이상**으로 잡는다. 5.5m 로
  잡았다가 NavMesh 가 섬으로 끊겨 6장 엄폐물 셋을 옮겼다. 벽뿐 아니라 물체끼리의
  **대각선 간격**도 봐야 한다
- 기하를 고쳤으면 **두 면을 다 다시 굽는다.** 하나만 구우면 플레이어와 적이 서로
  다른 지형 위를 걷는다

자세한 것은 [`tech-design/architecture.md`](tech-design/architecture.md) 의
「몸이 큰 적은 에이전트 타입을 따로 쓴다」.

### 2026-08-08 · 0단계 골격이 들어갔다 (C)

레이어 · 인터페이스 · 폴더 · 샌드박스 씬.

- **`SampleScene` 이 없어졌다.** Unity 를 열면 빈 씬이 뜬다. Project 창에서
  `Assets/Game/_Sandbox/<자기 이름>.unity` 를 열면 된다. 소유자가 없는 공용 씬을
  셋이 같이 만지면 병합할 수 없어서 각자 하나씩 갖는 방식으로 바꿨다.
- **빌드 씬 목록이 비어 있다.** `Ctrl+B` 를 누르면 「씬이 없다」는 오류가 나는데
  정상이다. 1단계에서 C 가 `Ch1` · `Ch2` · `Ch6` 을 등록한다.
- **레이어가 8개 늘었다.** 번호를 코드에 직접 쓰지 말고
  `Assets/Game/Shared/GameLayers.cs` 를 쓴다. 레이어를 추가할 때는 담당별 번호
  구간을 지킨다.

무엇을 만들지는 [`roles.md`](roles.md) 의 「시작하기」, 구조 규칙은
[`tech-design/architecture.md`](tech-design/architecture.md) 를 본다.

---

## 1. git 사용자 이름 설정

**가장 먼저 할 것.** 프롬프트 자동 로그와 마일스톤 메모의 파일명이 이 값으로
정해진다. 설정하지 않으면 Windows 사용자명이나 `unknown` 으로 조용히 떨어지고,
영문 이름과 한글 이름을 섞어 쓰면 한 사람의 기록이 두 파일로 갈라진다.

IMPORTANT: **한글 이름이어야 한다.** 커밋 시 훅이 확인하며, 한글이 아니면
커밋이 차단된다.

```bash
git config user.name
```

한글 이름이 아니거나 비어 있으면 이 저장소에서만 쓸 이름을 지정한다.
`--global` 을 붙이지 않는 것이 중요하다. 붙이면 다른 프로젝트의 신원까지
바뀐다.

```bash
git config user.name "홍길동"
git config user.email "메일주소"
```

어느 단계의 설정이 적용됐는지 확인하려면:

```bash
git config --show-scope user.name
```

## 2. Claude Code 실행 및 훅 승인

저장소 루트에서 `claude` 를 실행한다. 처음 열면 **훅 신뢰 승인 다이얼로그**가
뜬다. 승인해야 상태줄과 훅이 동작한다.

## 3. 재시작 후 상태줄 확인

설정은 시작할 때 읽히므로 한 번 재시작한다. 화면 하단에 이런 줄이 보이면 정상이다.

```
Projects/treeline | develop | Opus 5 | [▓▓▓░░░░░░░] 32% | usage: $0.42 | limit: 5h 18%
```

작업 경로 / 브랜치 / 모델 / 컨텍스트 사용률 / 세션 비용 / 5시간 사용량 한도 순이다.
맨 뒤 `limit` 은 Claude.ai 구독자에게만 나오므로 안 보여도 정상이다.

**안 보이면** `.claude/settings.json` 의 `statusLine` 이 개인 설정
(`~/.claude/settings.json`) 에 밀렸을 수 있다. 확인이 필요하면 공유해 달라.

## 4. 설정이 로드됐는지 확인

세션에서 `/context` 를 실행하고 **Memory files** 에 `CLAUDE.md` 가 있는지 본다.
없으면 Claude 가 프로젝트 규칙을 읽지 못한 상태다.

## 5. Unity 설치

**Unity 6.3 LTS — `6000.3.21f1`**

IMPORTANT: 반드시 이 버전이어야 한다. 상위 버전으로 프로젝트를 열면 자동으로
업그레이드되고 되돌리기 어렵다. `ProjectSettings/ProjectVersion.txt` 도 매번
충돌한다.

Unity Hub 에서 이 버전을 설치하고, 모듈로 **WebGL Build Support** 를 함께
체크한다. 나중에 추가할 수도 있지만 빌드하려는 시점에 없으면 다시 받느라
시간을 버린다.

프로젝트는 저장소 루트가 아니라 **`Treeline/`** 폴더다. Unity Hub 에서 열 때
`treeline/Treeline` 을 지정한다.

## 6. 빌드 타깃을 Web 으로 전환

IMPORTANT: **각자 한 번씩 해야 한다.** 활성 빌드 타깃은 `UserSettings/` 에
저장되는데 이 폴더는 저장소에서 제외되므로 팀원 간에 공유되지 않는다. 안 하면
빌드가 PC 용으로 나온다.

`File → Build Profiles` (`Ctrl+Shift+B`) → 플랫폼 목록에서 **`Web`** 선택 →
`Switch Platform`.

Unity 6 에서 `WebGL` 은 **`Web`** 으로 이름이 바뀌었다. 목록에서 `WebGL` 을
찾으면 안 나온다.

전환하면 텍스처를 플랫폼에 맞게 다시 압축하느라 몇 분 걸린다. `Web` 이 회색으로
비활성이면 모듈이 없는 것이니 Unity Hub → Installs → 해당 버전 → `Add modules`
에서 **Web Build Support** 를 설치한다.

## 7. 자기 샌드박스 씬 열기

클론 후 처음 열면 Unity 가 **빈 씬**을 띄운다. 마지막으로 연 씬 기록은
`Library/` 에 저장되는데 이 폴더는 저장소에서 제외되기 때문이다.

Project 창에서 **`Assets/Game/_Sandbox/<자기 이름>.unity`** 를 더블클릭해 연다.
한 번 열어두면 다음부터는 Unity 가 기억한다.

빈 씬에도 `Main Camera` 와 `Directional Light` 는 있으므로 얼핏 정상으로 보인다.
`Global Volume` 이 없거나 상단 씬 이름이 `Untitled` 면 잘못된 씬을 보고 있는
것이다.

IMPORTANT: **남의 샌드박스를 열지 않는다.** `.unity` 는 두 사람이 고치면 병합할
수 없어서, 실험은 각자 자기 씬에서만 한다. 자세한 규칙은
[`tech-design/architecture.md`](tech-design/architecture.md) 참고.

빌드 씬 목록은 아직 비어 있다. 1단계에서 `Levels/` 에 `Ch1` · `Ch2` · `Ch6` 이
들어오면 그때 등록한다. 그전에 `Ctrl+B` 를 누르면 「씬이 없다」는 오류가 나는데
정상이다.

## 8. Unity MCP 준비

Claude 가 Unity 에디터의 씬과 컴포넌트를 직접 조작할 수 있게 하는 연동이다.
Unity 를 스택으로 고른 핵심 근거이므로 붙여두는 편이 좋다.

**Unity 패키지는 저장소로 공유된다.** `Packages/manifest.json` 에 들어 있어 pull
받으면 자동으로 설치된다. **MCP 서버 설정도 `.mcp.json` 으로 공유된다.**

각자 설치해야 하는 것은 두 가지다.

- **Python 3.10 이상**
- **`uv`** — MCP 서버가 Python 으로 돌아가고 `uv` 가 그것을 격리 환경에서 실행한다

```bash
pip install uv
```

공식 설치 스크립트(`irm https://astral.sh/uv/install.ps1 | iex`)도 있지만 이
환경에서 실패한 사례가 있다. Python 이 이미 있으므로 `pip` 쪽이 확실하다.

서버는 기본적으로 **자동으로 뜨지 않는다.** `Window → MCP for Unity` 의
`Advanced` 에서 **`Auto-Start Server on Editor Load`** 를 켜두면 에디터를 열 때
알아서 기동한다. 한 번만 설정하면 되므로 켜는 편이 좋다.

켜지 않았다면 Unity 를 열 때마다 같은 창에서 `Start Server` 를 눌러야 한다.

**첫 실행은 1분 넘게 걸릴 수 있다.** `uv` 가 Python 의존성을 받는다. 콘솔에
`Starting local HTTP server…` 가 뜬 뒤 `Server ready` 가 나올 때까지 기다린다.
안 켜진다고 성급히 판단하지 않는다.

Claude Code 를 처음 실행하면 **`.mcp.json` 에 등록된 서버를 승인할지 묻는다.**
저장소에 커밋된 설정이 임의로 프로세스에 접속하면 안 되므로 한 번은 사람이
확인하는 구조다. 승인하면 이후로는 묻지 않는다.

같은 창에서 **`Install Skills`** 도 눌러둔다. Claude 가 Unity MCP 도구를 어떻게
써야 하는지 담은 안내서가 `~/.claude/skills/` 에 설치된다. 스크립트 작성 후
컴파일을 기다려야 한다거나, 여러 작업은 `batch_execute` 로 묶어야 빠르다는
식의 내용이라 결과 품질이 달라진다.

IMPORTANT: 이건 **개인 전역 스킬이라 저장소로 공유되지 않는다.** 각자 눌러야
한다. 안 누르면 그 사람 세션에서만 Unity 작업이 서툴러진다.

`/mcp` 로 `UnityMCP` 가 연결됐는지 확인한다.

| 상태 | 뜻 |
| --- | --- |
| `Connected` | 정상 |
| `Pending approval` | 아직 승인하지 않음 |
| 연결 실패 | Unity 가 꺼져 있거나 서버가 안 떠 있음 |

Claude 가 Unity 도구를 쓰려 할 때 아래 에러가 나면 **거의 항상 서버가 안 떠
있는 것**이다. 방화벽이나 네트워크를 뒤질 필요 없다.

```
Unable to connect. Is the computer able to access the url?
```

IMPORTANT: **Unity 에디터가 떠 있어야 동작한다.** 브리지가 에디터 안에서 도는
구조라, 에디터를 닫으면 연결이 끊긴다.

---

## 알아둘 것

**프롬프트가 자동으로 기록된다.** 입력하는 모든 프롬프트가
`docs/ai/prompts/<이름>/<날짜>-<세션>.md` 에 쌓이고 저장소에 커밋된다. 제출물인 "AI 활용
기술 문서"의 근거 자료다.

- 개인정보와 API 키는 정규식으로 가려지지만 **완전하지 않다.** 민감한 값을
  대화창에 붙여넣지 않는다.
- 남기고 싶지 않은 프롬프트에는 `[nolog]` 를 넣으면 그 프롬프트는 기록되지 않는다.

**의미 있는 작업을 마치면 `/milestone`** 을 실행한다. 이번에 무엇을 AI로 했는지
3~5줄 메모가 남는다. 마감 때 이 기록으로 기술 문서를 쓴다.

**저장소 밖에서 한 AI 실험도 `/milestone` 으로 남긴다.** 별도 프로젝트에서
프로토타입을 뽑아보거나 다른 도구를 붙여본 것도 제출물의 재료다. 프롬프트 훅은
이 저장소에서 실행한 것만 기록하므로, 안 적으면 그대로 사라진다.

**무언가를 정했으면 `/decision`** 을 실행한다. 기획·기술·AI 환경 중 알맞은
`decisions.md` 에 이유와 버린 안까지 기록된다. 결과는 코드에 남지만 이유는
여기에만 남는다.

**기획은 `/design`** 으로 진행한다. 컨셉 → 핵심 루프 → 승패 조건 → 최소 컨텐츠 →
컷라인 순으로 단계를 밟고, 정해진 것이 `docs/game-design/` 에 바로 기록된다.
목적은 빈틈을 메우는 것이 아니라 **범위를 좁히는 것**이다. 3인이 짧은 기간에 만드는
규모에서는 빈틈보다 미완성이 치명적이다.

**기획안이 정해지기 전까지는 각자 초안을 쓴다.** `/design` 이
`docs/game-design/drafts/<이름>/<게임이름>.md` 에 기록한다. 셋이 한자리에서 AI 와
논의하면 한 사람만 말하게 되므로, 각자 좁혀본 뒤 결과를 놓고 비교하는 편이 낫다.
파일 하나가 기획안 하나라 여러 안을 내도 된다. 초안도 커밋한다.

**force push 는 Claude 가 실행하지 못하도록 훅으로 막혀 있다.** 필요하면 직접
실행해야 한다.

**브랜치는 `develop` 하나를 쓴다.** 별도 브랜치를 만들지 않는다.

**커밋 메시지는 한국어로 쓰고 본문에 "왜"를 남긴다.** `feat:` 같은 접두사는
쓰지 않는다. 커밋 기록 자체가 제출물이라 사람이 읽는 문장이 우선이다.
자세한 기준은 `CLAUDE.md` 의 Git 항목 참고.

**빌드할 때 출력 폴더는 반드시 `Treeline/Build/` 로 지정한다.** 출력 경로는 빌드
시점에 직접 고르는 값이라 `.gitignore` 가 이름을 예측해 막을 수 없다. 다른
이름을 쓰면 WebGL 산출물 수백 MB 가 그대로 커밋 대상이 된다.

**빌드 확인은 로컬 웹서버로 한다.** 배포할 때마다 산출물이 `gh-pages` 에 쌓이므로
매번 올릴 필요가 없다. 실제 배포와 거의 같은 조건이다.

```bash
cd Treeline/Build && python -m http.server 8000
```

`8080` 은 Unity MCP 브리지가 쓰므로 피한다.

**공유가 필요할 때 `/deploy`** 를 실행한다. 배포 주소는
https://dev-kkyu.github.io/treeline/ 이고 갱신까지 1~2분 걸린다.

**Unity 가 에셋을 자동으로 고쳐놓는 경우가 있다.** URP 설정 에셋에서
`k_AssetVersion` 이 올라가고 새 필드가 붙는 식이다. 패키지가 구버전 포맷을
로드하면서 마이그레이션한 것이라 정상이고, **되돌리지 말고 커밋한다.** 안 그러면
팀원 전원이 같은 변경을 계속 보게 된다.

**제3자 에셋 원본은 커밋하지 않는다.** `Assets/ThirdParty/` 아래에 두면 자동으로
제외된다. 자세한 정책은 `CLAUDE.md` 참고.

**에셋을 받으면 그 자리에서 [`docs/assets.md`](assets.md)에 출처와 라이선스를
적는다.** 제출 요건이라 빠뜨리면 안 되고, 나중에 몰아서 쓰려 하면 어디서 받았는지
복원할 수 없다. 받은 직후 한 줄이 제일 싸다.
