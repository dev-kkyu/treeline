> 상태: 작성 중 · 최종 수정 2026-08-05

# AI 개발 환경 구축 명세

이 팀은 Claude Code 로 개발한다. 그 환경을 어떻게 구성했고 왜 그렇게 했는지를
적는다. 제출물 **"AI 활용 기술 문서"** 의 구조 설명 부분이 여기서 나온다.

프롬프트 원본은 [`prompts/`](prompts/), 작업 단위 메모는
[`milestones/`](milestones/) 에 쌓인다. 게임 자체의 기술은
[`docs/tech-design/`](../tech-design/) 로 분리했다.

---

## 구성 요소

설정은 저장소에 넣어 팀 전체가 공유한다. 클론하고 Claude Code 를 실행하면
세 사람이 같은 환경에서 작업하게 된다.

| 위치 | 역할 | 로드 시점 |
| --- | --- | --- |
| `CLAUDE.md` | 프로젝트 지침 | 매 세션 |
| `.claude/rules/powershell.md` | `.ps1` 작성 제약 | `.ps1` 을 읽을 때 |
| `.claude/rules/unity-csharp.md` | C#·Unity 작성 제약 | `.cs`, `.unity` 등을 읽을 때 |
| `.claude/skills/milestone/` | `/milestone` 명령 | 호출 시 |
| `.claude/hooks/` | 도구 호출 차단·기록 | 해당 이벤트마다 |
| `.claude/statusline.ps1` | 상태줄 | 세션 중 계속 |
| `.mcp.json` | Unity MCP 서버 등록 | 세션 시작 시 |

### 훅

| 파일 | 이벤트 | 하는 일 |
| --- | --- | --- |
| `log-prompt.ps1` | `UserPromptSubmit` | 프롬프트를 `prompts/<이름>.md` 에 기록 |
| `block-force-push.ps1` | `PreToolUse` (`git push`) | force push 차단 |
| `check-korean-username.ps1` | `PreToolUse` (`git commit`) | `git user.name` 이 한글인지 확인 |

### 프롬프트 로깅

`git config user.name` 으로 파일을 나눠 사람별로 쌓는다. 개인정보와 API 키
형식을 정규식으로 치환하고, `[nolog]` 가 포함된 프롬프트는 기록하지 않는다.

리댁션은 안전망이지 보증이 아니다. 패턴이 없는 값은 잡지 못하므로 민감한 값을
대화창에 붙여넣지 않는 것이 우선이다.

---

## 설계 판단

### 강제할 제약은 규칙이 아니라 훅으로

`CLAUDE.md` 는 컨텍스트일 뿐 강제되지 않는다. 사용자가 auto mode 를 쓰므로 권한
프롬프트라는 최후 방어선도 없다. force push 금지처럼 "절대" 인 항목은
`PreToolUse` 훅으로 만들어 모델의 판단과 무관하게 차단되게 했다.

반대로 push 여부처럼 **지시받았는지에 따라 정당성이 갈리는 것**은 훅으로 만들 수
없다. 훅은 사용자의 의도를 모르기 때문이다. 그런 항목은 규칙으로 둔다.

버린 안: `permissions.deny` 로 막기. 인수 순서와 축약형 조합을 나열해야 해서
하나라도 빠뜨리면 뚫린다. 실제로 훅에서도 명령 전체를 한 덩어리로 검사했다가
`git commit -F -` 의 `-F` 를 force 플래그로 오인해 정상 커밋이 막힌 적이 있어,
서브커맨드 단위 검사로 고쳤다.

### 기록을 사람별 파일로 분리

세 명이 한 파일에 append 하면 커밋마다 충돌한다. 파일을 사람별로 나눠 각자 자기
파일만 건드리게 했다. 이정범 커밋이 원격에 들어왔을 때 실제로 충돌 없이
병합되어 설계가 검증됐다.

이름이 영문이면 한 사람의 기록이 두 파일로 갈리므로, 커밋 시 한글 이름인지
검사하는 훅을 뒀다.

### Unity MCP 를 붙인 이유와 구현체 선택

Claude 가 씬·GameObject·컴포넌트·프리팹을 직접 조작할 수 있게 한다. 이것이
없으면 C# 스크립트만 쓸 수 있고 씬 배치는 사람이 에디터에서 해야 한다. 웹
네이티브 대신 Unity 를 고른 근거가 여기에 있었으므로
(`docs/tech-design/decisions.md` 참고) 첫 구현 전에 붙였다.

**CoplayDev/unity-mcp v10.0.0** 을 쓴다. MIT 이고 무료다.

버린 안: Unity 공식 MCP(`com.unity.ai.assistant`). 설치가 간단하고 relay 바이너리를
직접 배포해 런타임 의존성이 없지만, 유료인 AI Assistant 패키지에 묶여 있다.

버전을 `#v10.0.0` 으로 고정한 이유는 `#main` 으로 두면 팀원마다 다른 커밋을 받아
재현되지 않는 문제가 생기기 때문이다.

**팀 공유 방식이 두 층으로 나뉜다.** Unity 패키지는 `Packages/manifest.json` 에
들어가 pull 만 받으면 설치되고, MCP 서버 등록은 `.mcp.json` 으로 공유된다. 후자가
가능한 이유는 이 구현이 stdio 가 아니라 **HTTP(`127.0.0.1:8080`)** 를 쓰기
때문이다. 절대 경로가 설정에 들어가지 않아 머신마다 달라지지 않는다.

이 패키지는 서버를 켤 때마다 **클라이언트 설정 파일을 스스로 다시 쓴다**
(`refreshed N client config(s)`). 그래서 개인 설정(`~/.claude.json`)에도 같은
항목이 생기고, 지워도 다음 기동에 되살아난다. 값이 같아 충돌은 없다.

그럼에도 `.mcp.json` 을 두는 이유는, 저장소만 봐도 이 프로젝트가 Unity MCP 를
쓴다는 사실이 드러나기 때문이다. 패키지의 부수 효과에 의존하지 않는다.

각자 설치해야 하는 것은 Python 3.10+ 와 `uv` 뿐이다. MCP 서버 본체가 Python 으로
돌아간다.

IMPORTANT: 브리지가 Unity 에디터 안에서 도는 구조라 **에디터가 떠 있어야 한다.**
서버는 기본적으로 자동 기동하지 않으므로 `Window → MCP for Unity` 의 `Advanced`
에서 `Auto-Start Server on Editor Load` 를 켜둔다. Claude 쪽에서 연결이 안 되면
이것부터 확인한다.

`8080` 을 쓰므로 로컬 웹서버 등 다른 용도로 그 포트를 잡지 않는다.

### 규칙과 문서의 역할 분리

`.claude/rules/` 는 **제약**을, `docs/` 는 **설명**을 담는다. 규칙은 매 세션
컨텍스트에 올라가므로 짧아야 하고, 문서는 필요할 때 읽으므로 길어도 된다.
같은 내용을 양쪽에 적으면 한쪽만 고쳐져 어긋난다.

경로 스코프(`paths`)가 있는 규칙은 해당 파일을 읽을 때만 로드되므로, 평소
컨텍스트를 차지하지 않는다.
