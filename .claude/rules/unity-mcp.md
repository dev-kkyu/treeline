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
3. **사용자가 세션을 유지하고 싶어 하면 HTTP 로 직접 붙는다.** 도구로 안 붙었을
   뿐 서버는 살아 있다. `.claude/umcp.ps1 -List` 로 목록을 보고 `-Tool` 로 부른다.
   자세한 건 [`docs/ai/setup.md`](../../docs/ai/setup.md) 의
   「세션을 다시 열지 않고 붙이는 길」

## 쓸 때

- 스크립트를 만들거나 고친 뒤에는 `refresh_unity` 로 컴파일하고 `read_console` 로
  에러를 확인한다. 컴파일이 끝나야 새 타입을 쓸 수 있다
- 파일로 직접 쓴 에셋은 Unity 가 모른다. `refresh_unity` 를 거쳐야 반영된다
- 결과를 짐작하지 않고 되읽어 확인한다. `unity_reflect` 로 타입이 올라왔는지,
  `manage_scene get_hierarchy` 로 씬이 의도대로인지 본다

## 씬을 셀 때 — 꺼진 것과 면적

- IMPORTANT: **`FindObjectsByType` · `GameObject.Find` 는 비활성 오브젝트를
  건너뛴다.** 이 프로젝트에서 꺼져 있는 것은 대개 **연출의 일부**다 — 절차로
  켜지는 조명(`SiteLights`), 서치라이트가 부르는 실루엣(1장 `Colossus`), 나중에
  나타나는 적. 빼고 세면 **있는 것을 없다고 하게 된다.**
  `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)` 을 쓴다
- **개수가 아니라 면적을 센다.** 2장 복도에서 `W_painted_steel` 이 84 개라 벽인 줄
  알았는데 전부 이음매 · 걸레받이 같은 얇은 트림이었고, 겉넓이로는 4.3% 였다.
  벽은 개수가 적은 `W_concrete_wall`(48.6%) 이었다
- **촬영용 카메라를 새로 만들면 씬의 카메라 설정이 안 따라온다.** `clearFlags` 가
  기본값 `Skybox` 라 6장 밤하늘이 대낮처럼 파랗게 찍혔고, 씬이 잘못된 줄 알았다.
  `Camera.main` 에서 `clearFlags` · `backgroundColor` · `fieldOfView` 를 베낀다
- **씬을 열면 `Texture2D` 가 파괴된다.** 여러 씬을 돌며 한 장으로 이어 붙이려면
  `hideFlags` 를 주거나, 장면마다 파일로 떨구고 밖에서 잇는다
- IMPORTANT: **트랜스폼을 바꾼 뒤 `Collider.bounds` 는 낡은 값이다.**
  `Physics.SyncTransforms()` 를 부르기 전에는 물리 씬이 안 따라온다. 레버를 180°
  돌리고 재니 콜라이더만 옛 자리에 남은 것처럼 보여 「콜라이더가 안 따라왔다」로
  오진했다. **비활성 오브젝트의 콜라이더는 아예 크기 0 인 점으로 나온다** —
  `size` 는 멀쩡한데 `bounds` 가 한 점이면 그 오브젝트가 꺼진 것이다
- **`Bounds.Contains` 는 축정렬이라 비스듬한 것에서 거짓 양성이 난다.** 5장 레버가
  지선(`Guy_*`) 에 파묻혔다고 나왔는데 실제 정점까지는 5.3 m 였다. 대각선 케이블의
  AABB 가 공간을 통째로 덮은 것이다. **정점까지의 실거리로 한 번 더 거른다**

## 안 보이는 것을 「없다」고 하기 전에

IMPORTANT: 렌더러가 `enabled` 인데 화면에 없으면 셋을 먼저 본다. 이 셋을 안 보고
「에셋이 깨졌다」로 결론지었다가 총 모델을 손으로 다시 붙이는 우회로를 며칠 탔다.

- **`gameObject.activeInHierarchy`.** `Renderer.enabled` 와 별개다. 오브젝트가
  꺼져 있으면 렌더러를 켜도 아무 일도 없다
- **`SkinnedMeshRenderer.updateWhenOffscreen`.** 꺼져 있으면 바운즈를 **바인드
  포즈**로 잡는다. 1인칭 뷰모델처럼 본이 원점에서 멀리 바인드된 메시는 바운즈가
  엉뚱한 곳(팔에서 40 m 밖)에 생겨 프러스텀 컬링에 통째로 잘린다.
  `renderer.bounds` 를 찍어 카메라 근처인지 보면 바로 드러난다
- **카메라의 `cullingMask` 와 오브젝트 `layer`.** 뷰모델은 `ViewModel`(9) 이고
  이걸 그리는 것은 `ViewModelCamera`(FOV 50) 다. 본 카메라(FOV 70)와 다르다

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

## 연출은 값이 아니라 그림으로 본다

숫자가 다 맞아도 화면은 틀릴 수 있다. 6장 시점 전환은 시선각 · 롤 · 뷰포트 좌표가
전부 의도대로였는데, 프레임을 뽑아 보니 **카메라가 적의 두개골 안에 있어** 도는
1 초를 검은 구가 가리고 있었다.

- **`manage_camera screenshot` 은 에디터를 일시정지 상태로 남긴다.** 연속으로 두 장
  찍으면 같은 프레임이 나온다. 사이마다 `EditorApplication.isPaused = false` 를 넣는다
- 프레임을 여러 장 뜰 때는 호출마다 찍지 말고 **게임 안에서 스스로 찍게 한다.**
  `EditorApplication.update` 에 콜백을 걸고 `Time.time` 으로 시각표를 따라가며
  `ScreenCapture.CaptureScreenshot` 을 부르면, 정상 속도로 한 번에 다 나온다
  ```csharp
  var samples = new float[] { 0.2f, 0.8f, 1.6f };  int idx = 0;  float t0 = Time.time;
  UnityEditor.EditorApplication.CallbackFunction cb = null;
  cb = delegate {
      if (!EditorApplication.isPlaying || idx >= samples.Length) { EditorApplication.update -= cb; return; }
      if (Time.time - t0 >= samples[idx]) { ScreenCapture.CaptureScreenshot(dir + "\\" + idx + ".png"); idx++; }
  };
  UnityEditor.EditorApplication.update += cb;
  ```
- 저장 위치는 **스크래치패드로 잡는다.** `Assets/` 아래에 떨구면 `.meta` 까지 생겨
  커밋 대상이 된다
- 여러 장을 한 번에 보려면 `Texture2D.GetPixelBilinear` 로 격자에 이어 붙여
  한 장으로 만든다. 왕복이 8 번에서 1 번이 된다
