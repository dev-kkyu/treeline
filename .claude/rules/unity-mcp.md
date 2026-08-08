# Unity 에디터 작업은 MCP 로 한다

IMPORTANT: 씬 · GameObject · 컴포넌트 · 프리팹 · 머티리얼 · 빌드 설정처럼
에디터에서 하는 일은 사람에게 시키지 않고 **`mcp__UnityMCP__*` 도구로 직접
한다.** 이 프로젝트는 AI 활용 자체가 심사 대상이라, 수동으로 넘긴 만큼 근거가
사라진다.

사람에게 넘기는 것은 에디터 밖의 일뿐이다 — Unity Hub, 에셋스토어 임포트,
라이선스, 계정.

## 순서 — Unity 를 먼저 켠다

**Unity 를 완전히 띄운 뒤에 Claude 를 연다.** MCP 는 세션이 시작하는 순간에만
붙으므로, 에디터가 로딩 중일 때 세션을 열면 도구가 없는 채로 시작한다. 그러면
`exit` 후 다시 여는 수밖에 없다.

IMPORTANT: **`git pull` 이나 rebase 로 씬 파일이 바뀌면 열려 있는 에디터가 멈출 수
있다.** 받아올 것이 씬을 건드리는지 먼저 보고, 건드리면 에디터에서 씬을 저장한 뒤
받는다.

```bash
git fetch origin -q && git diff --stat HEAD..origin/develop -- <프로젝트>/Assets/
```

## 도구가 안 보이면

`mcp__UnityMCP__*` 가 없다고 수동 작업으로 우회하지 않는다. 에디터를 켜고 세션을
다시 여는 비용이 수동 작업보다 싸다. 순서대로 요청한다.

1. **Unity 에디터를 켜 달라고 한다.** 브리지가 에디터 안에서 도는 구조라 닫혀
   있으면 연결이 없다
2. **켜져 있는데도 도구가 없으면 세션이 시작할 때 서버가 없었던 것이다.** MCP 는
   세션 시작 시점에 붙는다. `exit` 후 **`claude -c`** 로 다시 열어 달라고 한다.
   `-c` 를 붙여야 대화가 이어진다

## 쓸 때

- 스크립트를 만들거나 고친 뒤에는 `refresh_unity` 로 컴파일하고 `read_console` 로
  에러를 확인한다. 컴파일이 끝나야 새 타입을 쓸 수 있다
- 파일로 직접 쓴 에셋은 Unity 가 모른다. `refresh_unity` 를 거쳐야 반영된다
- 결과를 짐작하지 않고 되읽어 확인한다. `unity_reflect` 로 타입이 올라왔는지,
  `manage_scene get_hierarchy` 로 씬이 의도대로인지 본다

## `execute_code` 의 함정

- **`in` 매개변수를 못 부른다.** 컴파일러가 구버전이라 `TakeDamage(in DamageInfo)`
  를 `ref` 로 보고 "missing `ref` modifier" 로 거절한다. 공용 계약인
  `IDamageable` 이 정확히 이 모양이므로 반드시 걸린다. 리플렉션으로 우회한다.
  ```csharp
  typeof(Crawler).GetMethod("TakeDamage").Invoke(c, new object[] { info });
  ```
- **`AssetDatabase.DeleteAsset` 과 `EditorApplication.Exit*` 은 안전 검사에
  막힌다.** 지울 것이 없다는 걸 먼저 확인했다면 `safety_checks: false` 로 넘기고,
  플레이 모드 종료는 `EditorApplication.isPlaying = false` 를 쓴다.
- **오래 걸리는 작업은 응답이 안 온다.** NavMesh 굽기나 대량 임포트가 그렇다.
  실패로 보고 다시 실행하지 말고, 다음 호출에서 **상태를 읽어 확인**한다.
- **플레이 모드는 에디터가 포커스를 잃으면 멈춘다.** 프레임이 안 도니 값이 그대로다.
  `Application.runInBackground = true` 를 켜고 나서 표본을 뜬다.
- **`EditorApplication.Step()` 은 에디터를 일시정지 상태로 둔다.** 그 상태에서
  `AudioSource.Play()` 는 안 걸리고 `PlayOneShot` 은 걸린다. **소리를 재려면**
  `EditorApplication.isPaused = false` 로 풀고 호출 사이에 실제 시간이 흐르게 둔다.
  루프 안에서 `Step()` 을 쓰면 `PlayerLoop ... called recursively` 경고가 콘솔에
  쌓이는데, 이건 게임이 아니라 계측 방식이 낸 것이다.
- **메인 스레드를 붙잡고 기다리면 `Update` 가 안 돈다.** `Stopwatch` 로 스핀하면
  오디오(별도 스레드)는 진행하지만 게임 로직은 한 프레임도 안 간다. 게임 상태를
  기다릴 때는 스핀하지 말고 호출을 나눈다.
