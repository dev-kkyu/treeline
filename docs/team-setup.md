# 팀 셋업 체크리스트

이 저장소는 Claude Code 설정을 팀 공용으로 공유한다 (`.claude/`). 클론 후
아래를 한 번만 해두면 세 명이 동일한 환경에서 작업하게 된다.

이미 클론해 둔 사람은 아래 「이번 pull 후 할 것」만 보면 된다.

---

## 이번 pull 후 할 것

```bash
git pull --rebase --autostash
```

### 2026-08-09 · `PS_FuelFire` 가 콘솔에 경고를 뿌린다 — 원본은 안 건드렸다 (A)

**`VFX/Prefabs/PS_FuelFire.prefab` 의 속도 커브 축이 서로 다른 모드다.** `x` 와 `z`
는 Constant 인데 `y` 만 TwoConstants 라, 이 프리팹을 놓은 씬에서 매 프레임

```
Particle Velocity curves must all be in the same mode
```

가 쏟아진다. 4장에 불을 놓자마자 콘솔이 이걸로 덮였다.

**내 씬 인스턴스에서만 세 축을 맞춰 두었다.** 남의 프리팹이라 원본은 안 고쳤는데,
**3장 연료 인화에서 같은 프리팹을 쓰면 거기서도 뜬다.** 원본을 고치는 편이 맞다 —
`x` · `z` 도 TwoConstants 로 두면 된다.

### 2026-08-09 · 체크포인트가 진행 상태까지 되돌린다 — 공용 파일을 손댔다 (A)

IMPORTANT: **C 의 공용 파일 다섯을 고쳤다.** `GameFlow` · `Device` · `InstantDevice`
· `TimedDevice` · `SustainedDevice` · `Lever`. 새 파일은 `Progress/ChapterState.cs`
하나다.

이유는 이렇다. **재시작이 씬을 통째로 다시 읽어서**, 체크포인트만 두면 플레이어가
서는 자리만 앞당겨지고 레버와 장치는 전부 처음으로 돌아간다. 발전기 앞에서
부활했는데 차단기가 내려가 있으면 시작 지점에서 다시 하는 것보다 헷갈린다.

`ChapterState` 가 `PlayerLoadout` 과 같은 모양으로 그 간극을 메운다 — 체크포인트를
밟을 때 장치 상태를 베끼고, 같은 장을 다시 읽을 때 되돌린다. 장이 바뀌면 버린다.

각 파일에 더한 것은 **되돌리기 한 함수씩**이다.

| 파일 | 더한 것 |
| --- | --- |
| `Device` | `public abstract void Restore(bool)` |
| 장치 셋 | 그 구현. 전부 `ApplyState(notify: false)` 라 소리가 안 난다 |
| `Lever` | `public void Restore()` — 자기 `devices` 를 훑어 모습을 맞춘다 |
| `GameFlow` | 필드 하나와 훅 셋 (`SetCheckpoint` · 장 전환 · `EnterChapter`) |

**기존 동작은 안 건드렸다.** `Restore` 는 `ChapterState` 만 부르고, 체크포인트가
없는 장(1 · 2 · 3 · 6)은 지금까지와 똑같이 돈다.

**C 가 보고 아니라고 하면 되돌린다.** 원래 「셋으로 안 되는 것이 나오면 왜 안 되는지
먼저 확인한다」가 규칙인데, 여기는 세 패턴의 문제가 아니라 **재시작이 상태를
안 들고 간다**는 빈 자리였다. 5 · 6장에서도 같은 것이 필요해질 것으로 본다.

### 2026-08-09 · 관통이 다섯 장이 됐다 — 3 · 4장이 가운데 들어왔다 (A)

**`Chapters.Order` 를 고쳤다.** 이제 이렇게 간다.

```
Title → Ch1 → Ch2 → Ch3 → Ch4 → Ch6 → Ending
```

넣는 기준은 「걸어서 통과할 수 있는가」 하나로 두었고, 둘 다 `SpawnPoint` 에서
`ChapterExit` 까지 `PathComplete` 인 것을 확인했다. 플레이 모드에서 3장 → 4장 →
6장을 실제로 넘겨 보고 **소지품(손전등 · 배터리 · 권총 · 탄약)이 그대로 따라오는
것**과 스폰 자리가 맞는 것도 봤다.

- **C 에게** — 다음 배포 빌드부터 관통이 다섯 장이다. 영상도 여섯 장소 중 다섯이
  찍힌다. 5장만 남았다
- **B 에게** — 3장이 관통에 들어왔으니 이제 3장을 고칠 때 **앞뒤 장이 같이 깨질 수
  있다.** 4장은 3장이 주는 권총을 들고 들어오는 것을 전제로 한다

IMPORTANT: **덜 붙은 것이 있어도 관통은 안 끊긴다.** 3장의 연료 운반 · 셔터,
4장의 축전지실 폭발은 아직 없지만 걸어서 지나가는 데는 문제가 없다. 반대로
**앞으로 3 · 4장을 만질 때는 관통을 깨지 않는지 먼저 본다.**

### 2026-08-09 · `Clinger` 를 게임에 붙였다 — 3장에서 또 만들지 말 것 (A)

IMPORTANT: **`Clinger.cs` · `AC_Clinger` · `Clinger.prefab` 을 A 가 만들었다.**
리깅 세션이 남긴 계약 문서에는 「3장 작업과 함께 붙인다」고 적혀 있었는데, 4장이
먼저 그것을 필요로 해서 앞당겼다. **3장을 짓는 B 는 새로 만들지 말고 이것을 쓴다.**
같은 적이 두 벌이 되는 것은 이 저장소에서 이미 두 번 겪었다(발소리 · 청각).

만든 것과 값은 전부
[`enemy-sheets/mockups/codex/clinger/final/animation/unity-integration.md`](enemy-sheets/mockups/codex/clinger/final/animation/unity-integration.md)
에 있다. 놓을 때 지킬 것만 여기 옮긴다.

- **앵커는 갈고리가 무는 자리다.** 몸통 중심도 발밑도 아니라 **천장 면**에 둔다.
  낮게 두면 그만큼 갈고리가 허공을 쥔다
- **천장은 바닥에서 3.2m 이상.** 그보다 낮으면 플레이어가 지나가다 머리를 부딪는다
- **앵커끼리 2.6m 이상** 띄운다. 전개 폭이 2.49m 라 붙이면 서로를 관통한다
- **낙하 1.5m 미만인 자리에는 놓지 않는다.** 접힌 몸이 1.29m 다
- 착지 지점 반경 1.2m 안에 **`Humanoid` NavMesh** 가 있어야 한다

속도 3.10 은 걷기 2.8 과 전력질주 4.6 사이다. **한 마리는 절대 못 잡는다 — 길을
막아서 잡는 적이다.** 앵커를 통로 양쪽과 뒤에 나눠 두고 동시에 떨어뜨린다.

**소리 9종은 아직 안 걸려 있다.** `Clinger.cs` 에 `UnityEvent` 여섯을 열어 두었으니
프리팹에서 `AudioSource` 를 꽂으면 된다. 이건 B 의 자리다.

### 2026-08-09 · `Ch4.unity` 가 한 번 깨진 채로 올라갔다 — 고쳤으니 받으면 된다 (A)

`ef0c0dc` 에 올라간 `Ch4.unity` 는 **열리지 않는다.** 그 사이에 받았다면 콘솔에
`Invalid serialized file header` 가 떴을 것이다. 다음 커밋에서 고쳤으므로 받기만
하면 된다. 아무것도 안 해도 된다.

원인은 NavMesh 였다. 구운 데이터가 에셋으로 안 떨어지고 씬에 묻혀 **씬 파일이
바이너리가 됐고**, `.gitattributes` 가 `*.unity` 를 텍스트로 다루므로 체크아웃에서
CRLF 가 섞여 파일이 깨졌다. 씬이 바이너리가 되는 것 자체는
[`architecture.md`](tech-design/architecture.md) 가 이미 경고해 둔 것인데, **다시
구울 때마다 참조가 런타임 객체로 되돌아간다**는 것을 놓쳤다. 거기에 그 내용을
덧붙였다.

**셋 다 해당하는 확인 하나.** 스크립트로 NavMesh 를 구웠다면 씬 파일 첫 줄이
`%YAML 1.1` 인지 본다. 아니면 같은 상태다.

### 2026-08-09 · 4장 `Ch4.unity` 를 A 가 짓기 시작한다 (A)

**`Assets/Game/Levels/Ch4.unity` 가 생긴다. 소유자는 A 다** — 씬은 한 사람만
만진다는 규칙 그대로다. 3단계 배정(`roles.md`)에서 4장이 A 로 온 것에 따른다.

셋이 함께 건드리는 파일 하나를 건드린다. **`EditorBuildSettings.asset` 에 `Ch4`
를 등록한다.** 이름으로 씬을 로드하므로 등록해야 열 수 있다.

IMPORTANT: **`Chapters.Order` 에는 아직 넣지 않는다.** 넣는 순간 2장 다음이
미완성 4장이 되어 **지금 되는 관통이 끊긴다.** 빌드 목록에만 올려 두면 씬은 열리고
관통은 그대로다. 넣는 시점은 `SpawnPoint` 에서 `ChapterExit` 까지 실제로 이어질
때다. **B 의 3장도 같은 방식으로 가자** — 둘 다 관통에 들어오면 그때 `Order` 를
한 번에 고친다.

| A 가 하는 것 | 남는 것 |
| --- | --- |
| `Ch4.unity` 공간 · 동선 · 충돌 · NavMesh | 축전지실 폭발 (수소 → 발포 → 폭발) |
| 기존 프리팹 배치, 조명, 앰비언스 | `Clinger` 리깅 · 애니메이션 (A, 다른 세션) |
| 차단기 둘 · 환기 팬 · 발전기 예열의 `Device` 배선 | 전력 복구 후 조명이 켜지는 연출 |
| `SpawnPoint` · `ChapterExit` · 빌드 설정 등록 | 4장 전용 소품 5종 (아래) |

**4장 전용 소품이 아직 없다** — 디젤 발전기 · 배전반 · 축전지 랙 · 환기 팬 ·
유기물 데코. [`tech-design/props.md`](tech-design/props.md) 의 후순위에 「레벨
없음」 으로 미뤄져 있던 것들이다. **이제 레벨이 생기니 치수를 재서 시트에 적는다.**
그때까지는 기존 30종으로 자리를 세운다(`MachineBlock` · `JunctionBox` ·
`VentGrille`). 세운 자리가 그대로 치수 근거가 된다.

표면은 **`W_nest` 를 쓴다.** 절차적 생성 7종에 이미 들어 있다. 미술 기준은
지어내지 않고 [`level-sheets/mockups/codex/ch04-generator-nest/`](level-sheets/mockups/codex/ch04-generator-nest/)
의 네 시안과 이미 지어진 1 · 2 · 6장에 맞춘다.

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

~~**`colossus-roar.wav` 는 여전히 안 걸려 있다.**~~ **걸었다 (A, 같은 날).**
`Ch6` 의 `Colossus/Snd_Roar` 다 (3D · linear · 8~70m). 붙이면서 **2.36 초를
2.000 초로 잘랐다** — `roarSeconds` 가 2 라 남기면 이미 달려오는데 포효가
이어진다. 2.00 초 지점 크기가 피크의 90.9% 여서 그냥 자르면 딱 소리가 나므로
마지막 120ms 에 페이드를 걸었다. **원본 wav 를 덮어썼으니 다시 뽑을 때 길이를
2 초로 맞춘다.**

**6장에도 `ChaseMusic` 을 놓았다 (A, 같은 날).** 2장과 같은 값 그대로다 —
`chapterDuck` 0.35 · `fadeSeconds` 1.4 · `rescanInterval` 2. 생배선
(`noticed` → `Play`, `lost` → `Stop`)은 지웠다. 안 지우면 `ChaseMusic` 이 볼륨을
올리는 동안 이벤트가 `Stop` 을 때려 둘이 싸운다. `noticed` 에 남은
`Snd_Roar.Play` 와 `Player.Shake` 는 음악이 아니라 사건 표시라 그대로 둔다.

붙이다 알게 된 것 — **6장에는 장 음악이 아예 없었다.** `Music/ch6-colossus`(67초)가
만들어져 있는데 씬에 안 놓여 있었다. 1~5장은 다 도는데 6장만 눈보라 앰비언스
뿐이었다. `Mus_Ch6` 로 놓았고 값은 2장 `Mus_Ch2` 와 같다(루프 · `playOnAwake` 켬 ·
0.35 · 2D).

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

### 2026-08-09 · 타이틀 화면을 둘이 동시에 만지고 있다 (B 가 알림)

**같은 Unity 에디터에서 두 세션이 `Title.unity` 를 동시에 열고 있다.** 지금은
영역이 갈려 있어 안 터졌지만, `.unity` 는 병합이 안 되므로 한쪽이 저장하면
다른 쪽 작업이 통째로 사라진다.

| | 누가 | 무엇 |
| --- | --- | --- |
| Canvas · `TitleScreen` | UI 쪽 세션 | 메뉴를 **코드로 짓는다.** 스프라이트 · 폰트 칸을 씬에 끼웠다 |
| `_Backdrop` (설원 · 수목한계선) | B | 3D 배경. Canvas 아래는 건드리지 않는다 |

IMPORTANT: **씬에 UI 를 손으로 배치하지 않는다.** `TitleScreen.Awake` 의
`ClearAuthoredPlaceholder()` 가 Canvas 자식을 **전부 `Destroy`** 한 뒤 `BuildInterface()`
로 다시 짓는다. 에디터에서 아무리 예쁘게 배치해도 플레이하면 사라지고, 씬에는
남아 있어서 원인을 찾기 어렵다. UI 를 고칠 곳은 씬이 아니라 그 함수다.

**B 가 만든 것** — `Assets/Game/Levels/Title/` 에 설원 1 · 침엽수 3 종 FBX 와
머터리얼 3 종. 바닥은 `World/Textures/snow_*` 를 그대로 쓴다. 배치 · 조명 ·
눈 파티클은 에디터가 비는 대로 `_Backdrop` 루트 아래에 넣는다.

**내가 만들던 `OptionsPanel.cs` 는 지웠다.** UI 쪽 세션의 「사운드 설정」과 같은
것을 두 벌 만드는 셈이라 — 발소리가 두 벌이었던 것과 같은 모양이다. 옵션 화면은
UI 쪽에서 이어서 만든다.

### 2026-08-09 · `Colossus` 애니메이션 이벤트에 받는 쪽을 만들었다 (B)

`Colossus.fbx` 에 이벤트가 **넷** 들어 있는데 프로젝트에 받는 쪽이 하나도
없었다. 타이틀 배경에 실루엣을 올리자 콘솔에 이게 떴다.

```
'Silhouette' AnimationEvent 'AE_ColossusRoar' on animation 'Colossus_Roar'
has no receiver! Are you missing a component?
```

IMPORTANT: **애니메이션 이벤트는 Animator 가 붙은 GameObject 의 컴포넌트에서만
함수를 찾는다.** 부모도 자식도 보지 않는다. `Crawler` 에서 겪은 것과 같은
함정이라 같은 모양으로 `ColossusAnimationEvents` 를 만들어 Animator 와 같은
오브젝트에 붙인다.

| 이벤트 | 클립 · 프레임 | 지금 |
| --- | --- | --- |
| `AE_ColossusRoar` | `Roar` 22f | 신호 + 선택적 소리 |
| `AE_ColossusThreat` | `Threat` 26f | 신호만 |
| `AE_GrabLock` | `Grab_Execute` 8f | 신호만 — **판정은 아직 없다** |
| `AE_GrabKill` | `Grab_Execute` 90f | 신호만 — **즉사 처리 아직 없다** |

#### 6 장 프리팹에는 이 중계기가 필요 없다 (확인함)

내가 이 글을 쓴 뒤에 A 가 `Colossus.prefab` 에 모델과 애니메이션을 붙이면서
**Animator 를 루트에 뒀다.** `Colossus.cs` 와 같은 오브젝트이므로 거기 있는
`AE_` 함수 넷이 정상으로 불린다 — 프리팹을 열어 확인했다. **6 장은 그대로 두면
된다.**

그래서 같은 계약을 구현한 것이 둘이 되었지만 **겹쳐서 두 번 울리지는 않는다.**
쓰는 자리가 갈린다.

| | 받는 쪽 | 왜 |
| --- | --- | --- |
| 6 장 `Colossus.prefab` | `Colossus.cs` 의 `AE_*` | Animator 가 루트에 있고 게임 로직도 거기 있다 |
| 타이틀 배경 실루엣 | `ColossusAnimationEvents` | Animator 가 모델 자식에 있고 `Colossus` 컴포넌트가 없다 (AI 를 배경에 끌어들일 이유가 없다) |

**새로 붙일 때 판단 기준** — Animator 와 같은 오브젝트에 `Colossus` 가 있으면
중계기가 필요 없고, 없으면 필요하다.

소리 칸은 **비워 두어도 된다.** 타이틀은 `TitleApparition` 이 포효음을 내고
있어서 비워 뒀다 — 두 군데서 내면 두 번 울린다.

## 1 장 관제실 바닥에 검은 띠가 생긴다 (2026-08-09 · B 가 영상 찍다 발견)

제출 영상용으로 1 장을 찍다 보니 관제실 바닥을 **하드한 검은 띠**가 대각선으로
가로지른다. 형광등 기구가 자기 그림자를 바닥에 던지는 것인데, 실내인데도
`Directional Light` 가 살아 있어서 생긴다. 실제 그림자 방향도 천장 조명과
안 맞는다.

영상에서는 구도로 피했지만 플레이하면 그냥 보인다. 고칠 사람은 1 장 담당이다.
후보는 셋 — 실내에서 디렉셔널 라이트를 끄거나, 형광등 기구의 `Shadow Casting`
을 Off 로 두거나, 천장이 그림자를 막게 하는 것. **어느 쪽이 맞는지는 1 장의
조명 의도를 아는 사람이 정해야 한다.**

같이 본 것 하나 더. **1 장은 실내가 z ≈ −33 ~ −8 이고 그 너머 야외 구역은
하얗게 뜬다.** 아직 손이 안 간 구간으로 보여 영상에서 뺐다.
