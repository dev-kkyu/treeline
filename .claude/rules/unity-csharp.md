---
paths:
  - "**/*.cs"
  - "**/*.unity"
  - "**/*.prefab"
  - "**/*.asmdef"
---

# C# / Unity 작성 규칙

## 빌드 대상은 WebGL이다

IMPORTANT: WebGL은 아래 제약이 있다. 에디터에서 동작해도 빌드에서 깨지는
전형적인 지점들이므로, 코드를 쓰기 전에 확인한다.

- **스레드를 쓸 수 없다.** `System.Threading.Thread`, `Task.Run`,
  `Parallel.For`는 WebGL에서 동작하지 않는다. 비동기는 코루틴이나
  단일 스레드 `async/await`로 처리한다.
- **파일 시스템에 직접 접근할 수 없다.** `System.IO`로 임의 경로를 읽고 쓰는
  코드는 실패한다. 저장은 `PlayerPrefs`를 쓰거나 브라우저 저장소를 경유한다.
- **블로킹 네트워크 호출을 쓸 수 없다.** `UnityWebRequest`를 코루틴이나
  `await`로 쓴다.
- **빌드 용량이 곧 첫 로딩 시간이다.** 심사자가 링크를 누르고 기다리는 시간이므로
  무거운 의존성을 추가하기 전에 사용자와 상의한다.

## 입력

IMPORTANT: 신규 Input System **전용** 프로젝트다 (`activeInputHandler: 1`).
`Input.GetKey`·`Input.GetAxis`·`Input.mousePosition` 은 컴파일은 되고 **런타임에
예외**가 난다. 입력은 `Assets/InputSystem_Actions.inputactions` 의 액션으로 받는다.
이 에셋의 소유자는 플레이어 담당(A)이다.

## 레이어

레이어 번호를 코드에 직접 쓰지 않는다. `Assets/Game/Shared/GameLayers.cs` 의
상수와 마스크를 쓴다. 레이어를 추가할 때는 담당별 번호 구간을 지킨다
(`docs/tech-design/architecture.md` 참고).

## 에셋 취급

- 제3자 에셋은 `Assets/ThirdParty/` 아래에 둔다. 이 폴더는 저장소에서 제외된다
  (정책은 CLAUDE.md 참고).
- IMPORTANT: 에셋의 이동·삭제·이름 변경은 반드시 Unity 에디터에서 한다.
  파일 시스템에서 직접 옮기면 `.meta`의 GUID 참조가 끊어져 씬과 프리팹의
  연결이 통째로 깨진다. 되돌리기 어렵다.

## 직렬화

- 인스펙터에 노출할 값은 `public` 필드 대신 `[SerializeField] private`을 쓴다.
- 씬과 프리팹은 텍스트 직렬화를 유지한다 (Project Settings → Editor →
  Asset Serialization = Force Text). 바이너리로 바꾸면 diff가 사라지는데,
  이 프로젝트는 커밋 기록 자체가 제출 요건이자 AI 활용의 근거다.

## 코드 스타일

팀 컨벤션이 아직 정해지지 않았다. 정해지기 전까지는 주변 코드의 방식을 따르고,
새로 만드는 경우 Unity와 C#의 일반적인 관례를 따른다.
