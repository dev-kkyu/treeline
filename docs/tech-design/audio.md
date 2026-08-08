> 상태: 작성 중 · 최종 수정 2026-08-08

# 사운드

`Assets/Game/Audio/` 에 59 개, `Assets/Game/Enemy/Audio/` 에 28 개가 들어 있다. **전부 만들어져 있고 임포트 설정도
끝났다. 가져다 붙이면 소리가 난다.** 아직 없는 것은 이것을 재생하는 코드다.

만든 사람은 B 김도한이고 도구는 ElevenLabs 다. 출처와 권리는
[`docs/assets.md`](../assets.md) 에 있다.

- 어디에 두는지와 왜 담당 폴더가 아닌지는
  [`architecture.md`](architecture.md) 의 「사운드」 절
- 무엇을 만드는 게임인지는 [`treeline.md`](../game-design/treeline.md)

---

## 1. 지금 있는 것

파일명은 소문자 케밥 케이스다. **`-loop` 로 끝나면 이음매 없이 반복되도록 만든
것**이고, 나머지는 한 번 나고 끝나는 단발성이다.

확장자가 갈린 것은 실수가 아니다. 지속음은 `.mp3`, 단발성은 `.wav` 다. 이유는
아래 [「단발성은 앞을 잘라냈다」](#단발성은-앞을-잘라냈다) 에 있다.

### `Ambience/` — 깔아두는 배경음 8 종

계속 재생한다. 장을 켜면 켜고 끄지 않는다.

| 파일 | 어디에 | 길이 |
| --- | --- | --- |
| `blizzard-outside-loop` | 5 장 철탑, 6 장 승강장 등 실외 전부 | 15s |
| `blizzard-muffled-loop` | 실내인데 창 · 벽 너머로 눈보라가 들리는 곳 | 15s |
| `room-tone-loop` | 실내 기본 바닥. 다른 앰비언스가 없는 방 | 15s |
| `heater-hum-loop` | 2 장 히터가 도는 동안, 6 장 케이블 히터 | 15s |
| `vent-fan-loop` | 4 장 환기 팬이 도는 동안 | 15s |
| `generator-rumble-loop` | 4 장 발전기 기동 후, 옆방에서 들리는 소리 | 15s |
| `fluorescent-buzz-loop` | 전력 복구 후 조명이 들어온 복도 | 12s |
| `radio-static-loop` | 1 장 관제동 무전기 | 12s |

### `Footsteps/` — 표면별 발소리 8 종

**표면당 2 종이다. 콘크리트만 1 종이다** — 아래 참고. 하나만 반복하면 기관총처럼
들려 걷는 것이 우스워진다. 번갈아 쓰는 법은
[3 장](#발소리는-직전-것을-빼고-고른다) 에 있다.

| 표면 | 파일 | 어디에 |
| --- | --- | --- |
| 콘크리트 | `footstep-concrete-01` · `-03` | 관제동 · 숙소동 · 갱도 |
| 금속 그레이팅 | `footstep-metal-grate-01` · `-03` | 갱도 통로 · 발전동 · 철탑 계단 |
| 눈 | `footstep-snow-01` · `-03` | 5 장 철탑, 6 장 승강장 |
| 나무 | `footstep-wood-01` · `-03` | 숙소동 바닥 |

IMPORTANT: **원래 표면당 3 종이었고, 다섯을 지웠다** (2026-08-08). 붙여서 들어보니
어색한 것이 있어서 파형을 재봤고, 두 종류의 결함이 나왔다. 마지막 하나는 파형이
아니라 귀로 걸렀다.

| 지운 것 | 왜 |
| --- | --- |
| `footstep-metal-grate-02` | 형제보다 **3~4 배 밝다.** 같은 표면인데 혼자 다른 재질처럼 들린다. 앞 180ms 도 비어 있었다 |
| `footstep-snow-02` | 밝기 6900 대 2140 · 3940. 같은 이유 |
| `footstep-concrete-01` | 소리가 **362ms 뒤에** 시작했다. 발을 딛고 한참 뒤에 났다. → **2026-08-09 에 앞을 잘라 되살렸다.** 아래 참고 |
| `footstep-wood-02` | 같은 이유로 254ms |
| `footstep-concrete-02` | **파형에는 문제가 없지만 소리가 이상하다.** 들어보고 걸렀다 |

IMPORTANT: **그 결과 콘크리트에 클립이 하나만 남았다.** 피치를 흔들어도 같은 소리가
반복되고, 2 장은 발소리가 곧 긴장인 장이라 하나로는 부족하다. **콘크리트 두 번째
클립을 다시 뽑아야 한다.** 같은 프롬프트로 다시 돌리면 같은 소리가 나올 수 있으므로
`gen_sfx.py` 의 해당 항목을 손봐야 한다.

IMPORTANT: **`trim_silence.py --report` 는 이 넷을 전부 0~10ms 로 보고한다.** 그 도구가
재는 것은 완전한 디지털 무음인데, 위 지연 구간에는 아주 작은 노이즈가 깔려 있어
무음으로 안 잡힌다. **도구가 통과시켰다고 앞이 비어 있지 않은 것이 아니다.** 새 단발성
소리를 넣을 때는 최대 진폭의 10% 를 넘는 첫 지점을 따로 재 본다.

#### 입력에 대한 응답은 즉시 나야 한다

IMPORTANT: `lever-locked` 은 **377ms 뒤에** 소리가 났다. 잠긴 레버를 당겼는데 0.4 초
동안 아무 반응이 없으면 입력이 안 먹은 것으로 읽힌다. `pickup-item` 도 133ms 였다.
둘 다 앞이 무음이 아니라 아주 작은 소리라 `trim_silence.py` 가 못 걸렀다 —
`footstep-concrete-01` 과 같은 증상이다. **타격 지점(피크의 10%)** 으로 잘라 둘 다
15ms 로 맞췄다.

**`lever-pull` 은 96ms 지만 그대로 뒀다.** 손잡이가 움직이다 철컹하는 소리라 앞부분이
소리의 일부다. 자르면 이동 없이 걸리는 소리만 남는다. **늦게 나는 것과 천천히
시작하는 것은 다르다** — 눈사태나 얼음도 마찬가지라 건드리지 않는다.

#### 되살린 것 — `footstep-concrete-01`

지운 판단이 틀렸던 게 아니라 **원인을 고칠 수 있었다.** 앞의 362ms 는 무음이
아니라 **들릴락 말락 한 잡음**이었고, 그래서 `trim_silence.py` 가 걸러내지 못했다.
그 도구는 피크의 2% 를 기준으로 삼는데 이 파일은 4ms 지점부터 이미 그 선을 넘는다.

```
피크의  2% 넘는 첫 지점 :   4 ms   ← 트림이 여기를 시작으로 봤다
피크의 10% 넘는 첫 지점 : 362 ms   ← 실제 발소리는 여기
```

**타격 지점(피크의 10%)** 을 기준으로 앞 347ms 를 잘라내고 15ms 만 남겼다.
0.67s → 0.32s 가 되었고 타격이 15ms 에 온다. `-03` 이 16ms 이므로 둘을 번갈아
재생해도 어긋나지 않는다.

**둘의 밝기를 좁혔다.** 원래 `-01` 이 70 Hz, `-03` 이 451 Hz 로 6.4 배 차이라 같은
표면인데 다른 재질처럼 들렸다. `-01` 에 고역을 올려 156 Hz 로 만들어 2.9 배까지
줄였다. **완전히 같게 맞추지는 않았다** — 그러면 번갈아 재생하는 의미가 없다.

`footstep-wood-03` 은 **뒤에 1194ms 가 비어 있지만 남겼다.** 뒤 무음은 `PlayOneShot`
에서 들리지 않고 빌드 용량만 먹는다. 지금 지우면 나무 표면에 소리가 하나만 남는다.

다시 만들 때는 `docs/sound/gen_sfx.py` 의 해당 항목을 **먼저 지우거나 고쳐야 한다.**
지금은 넷 다 그대로 남아 있어서, 사운드를 재생성하면 같은 파일이 되살아난다.

**점프와 착지에 쓸 클립은 따로 없다. 발소리를 재사용한다.** 발이 바닥을 미는 소리는
밟는 소리와 같은 재질에서 나므로, 도약은 같은 클립을 **피치를 내리고 음량을 줄여**
내고 착지는 음량을 올려 낸다. `PlayerFootsteps` 가 둘 다 처리한다.

그래서 표면을 늘릴 때 점프음을 따로 준비하지 않아도 된다. 클립을 더 뽑기 전에
이 방식으로 충분한지 먼저 들어본다.

### `Structure/` — 건물 7 종

| 파일 | 어디에 |
| --- | --- |
| `door-steel-open` · `door-steel-close` | 문 전반 |
| `door-handle-rattle` | 잠긴 문을 열려고 할 때 |
| `metal-stair-creak` | 철제 계단 |
| `window-wind-rattle` | 창이 있는 실내. 눈보라가 창을 흔든다 |
| `pipe-clank` | 갱도 배관. 인기척처럼 쓸 수 있다 |
| `roof-snow-shift` | 지붕 눈이 무너져 내리는 소리. 놀래키는 용도 |

### `Electrical/` — 전기 5 종

정전과 복구가 이 무대의 두 축 중 하나라 따로 뒀다.

| 파일 | 어디에 |
| --- | --- |
| `light-switch-click` | 조명 스위치 |
| `breaker-throw` | 4 장 차단기 두 개, 6 장 배전반 |
| `power-down` | 정전되는 순간 |
| `power-up` | 4 장 전력 복구. 조명이 켜지는 순간 |
| `fluorescent-flicker` | 형광등이 들어오려다 마는 것 |

### `Interaction/` — 상호작용 9 종

`Assets/Game/Interaction/` 의 컴포넌트에 대응한다. **파일 이름이 컴포넌트
이름과 맞다.**

| 파일 | 컴포넌트 | 언제 |
| --- | --- | --- |
| `lever-pull` | `Lever` | 당겨졌을 때 |
| `lever-locked` | `Lever` | 조건이 안 되어 안 당겨질 때 |
| `valve-turn` | 밸브 | 돌릴 때 |
| `pickup-item` | `Pickup` | `Flashlight` · `Pistol` · `Shotgun` |
| `pickup-ammo` | `Pickup` | 탄약 |
| `pickup-battery` | `Pickup` | `Battery` |
| `carriable-lift` | `Carriable` | 들었을 때 |
| `carriable-drop` | `Carriable` | `Drop()` |
| `interact-denied` | 공용 | 잠겨서 아무 일도 안 일어날 때 |

### `Devices/` — 장치 14 종

`Assets/Game/Devices/` 의 **세 패턴에 맞춰 나눴다.** 새 장치를 만들 때 어느
패턴인지만 정하면 쓸 소리가 정해진다.

| 패턴 | 파일 | 언제 |
| --- | --- | --- |
| `InstantDevice` | `shutter-close` | 비상 셔터가 내려온다 |
| | `fuel-ignite` | 고인 연료에 불이 붙는다 |
| | `hydrogen-explosion` | 축전지실 수소가 터진다 |
| | `avalanche-cannon` | 제설포 발사 |
| | `avalanche-slide` | 사면이 무너진다. `avalanche-cannon` 뒤에 잇는다 |
| `TimedDevice` | `device-start` | `Activate()` 직후 |
| | `device-complete` | `Complete()`. 히터 · 환기 팬 · 예열 공용 |
| | `ice-melt-crack` | 히터가 다 돌아 얼음이 깨질 때 |
| | `generator-start` | 4 장 발전기 기동 |
| | `snowblower-start` | 5 장 제설기 시동 |
| | `cablecar-start` | 6 장 삭도 권양기 기동 |
| `SustainedDevice` | `snowblower-loop` | 제설기가 도는 동안 |
| | `cablecar-loop` | 삭도가 가는 동안 |
| | `searchlight-swivel-loop` | 서치라이트가 도는 동안 |

`TimedDevice` 는 `Started` 이벤트와 `Progress` 가 이미 있으므로 시작 · 완료
두 지점에 그대로 걸면 된다.

### 장치와 레버 소리를 붙이는 길은 둘이다 — 한쪽만 쓴다

IMPORTANT: **같은 장치에 클립과 이벤트를 둘 다 걸면 소리가 두 번 난다.** 겹쳐도
에러가 나지 않고 그냥 두 번 울려서, 소리가 이상하다고만 느끼고 원인을 찾기
어렵다. 아래 둘 중 하나만 쓴다.

| | 어떻게 | 언제 쓰나 |
| --- | --- | --- |
| **컴포넌트** | `Device` · `Lever` · `Pickup` · `Carriable` 의 인스펙터에 클립을 넣는다 | **기본.** 한 번 넣으면 그 컴포넌트를 쓰는 모든 장에 적용된다 |
| **이벤트** | `UnityEvent` 에서 씬의 `AudioSource` 를 부른다 | 그 장에서만 나는 소리이거나, 루프를 직접 켜고 꺼야 할 때 |

컴포넌트 쪽은 클립을 **비워 두면 아무 일도 하지 않는다.** 그래서 이벤트로 붙여
둔 장치는 클립 칸을 비워 두기만 하면 되고, 반대로 클립을 넣었으면 그 장치의
이벤트에는 소리를 걸지 않는다.

2026-08-08 기준 — 2 장은 컴포넌트 쪽, 6 장은 이벤트 쪽이다. 6 장 장치의 클립
칸은 비어 있어 지금은 겹치지 않는다.

#### 이벤트로 붙이는 법

`Device` 에 이미 `started` · `activated` · `deactivated` 세 `UnityEvent` 가 있다.
소리를 낼 오브젝트에 `AudioSource` 를 붙이고 그 이벤트에서 `AudioSource.Play` 를
부르면 끝이고, **스크립트를 새로 쓰거나 남의 컴포넌트를 고칠 일이 없다.**

6 장이 그렇게 되어 있다. 레버 자체에는 이벤트가 없지만, **레버가 미는 장치**의
이벤트가 곧 당긴 시점이다.

| 이벤트 | 부르는 것 | 소리 |
| --- | --- | --- |
| `Heater.started` | `HeaterLever` 의 `Play` | `lever-pull` — 당기는 순간이다 |
| | `Heater/Snd_Start` 의 `Play` | `device-start` |
| | `Heater/Snd_Hum` 의 `Play` | `heater-hum-loop` — 도는 동안 |
| `Heater.activated` | `Heater/Snd_Complete` 의 `Play` | `ice-melt-crack` — 얼음이 깨진다 |
| `Colossus.noticed` | `Colossus/Snd_Roar` 의 `Play` | `colossus-roar` — 알아채는 순간 |
| | `Player` 의 `PlayerLook.Shake` | 화면이 짧게 떨린다 |
| `CableCar.activated` | `BoardLever` 의 `Play` | `lever-pull` |
| | `CableCar/Snd_Start` 의 `Play` | `generator-start` — 권양기가 돈다 |
| | `CableCar/Snd_Run` 의 `Play` | `generator-rumble-loop` — 가는 동안 |

IMPORTANT: **추격 음악은 이 표에 없다.** `noticed` · `lost` 에 걸어 두었다가
`ChaseMusic` 으로 옮겼다 (2026-08-09). 아래 「6 장 음악은 `ChaseMusic` 이 몬다」 참고.
**이벤트에 남긴 둘은 음악이 아니라 사건 표시**다 — 한 번 치고 끝나는 것이라
페이드가 필요 없다.

발각되는 순간에 칠 소리는 **`colossus-roar`** 다 (2026-08-09). 기획이 「멈춰 서서
돌아보고 포효한 뒤 온다」 고 못박은 그 2 초를 위한 것이다. 포효가 사건을 찍고
추격 루프가 그 뒤를 받는다.

**2026-08-09 에 2.36 초를 정확히 2.000 초로 잘랐다.** `roarSeconds` 가 2 이므로
소리가 0.36 초 더 남으면 이미 달려오고 있는데 포효가 이어진다. 자를 때 두 가지를
봤다.

- 앞 여백 20ms 를 버렸다. 포효는 **입력에 대한 응답**이라 즉시 나야 한다
- **2.00 초 지점의 크기가 피크의 90.9% 였다.** 그냥 자르면 딱 소리가 난다.
  마지막 120ms 에 제곱 페이드를 걸어 마지막 샘플이 `-0.00012` 로 끝난다.
  잘려 나간 뒤쪽 에너지는 전체의 2.16% 다

IMPORTANT: **길이를 소리에 맞추지 않고 소리를 길이에 맞췄다.** `roarSeconds` 를
2.4 로 올리는 쪽이 더 쉬웠지만, 그 2 초는 **플레이어가 달아날 시간**이라 밸런스
값이다. 연출 소재 때문에 밸런스를 움직이지 않는다.

씬에는 `Colossus/Snd_Roar` 로 걸었다 (3D · linear · 8~70m). **`Colossus.prefab` 이
아니라 `Ch6` 인스턴스에 붙였다** — 프리팹은 B 소유이고 이 적이 나오는 씬이 아직
6 장 하나뿐이다. 다른 장에 놓이면 그때 프리팹으로 올린다.

#### 6 장 음악은 `ChaseMusic` 이 몬다 (2026-08-09)

처음에는 `Colossus.noticed` → `Play`, `lost` → `Stop` 으로 생으로 켜고 껐다.
2 장에는 `ChaseMusic` 이 있었지만 그때는 `Crawler[]` 를 들고 있어 `Colossus` 를
받지 못했기 때문이다. 사운드 담당이 `IHunter` 로 열어 준 뒤 6 장도 같은 방식으로
바꿨다. **2 장과 같은 값을 그대로 쓴다.**

```
_Audio
 ├ ChaseMusic   chaseLoop=Mus_Chase  chapterLoop=Mus_Ch6
 │              chapterDuck 0.35  fadeSeconds 1.4  rescanInterval 2
 ├ Mus_Chase    chase-dread-loop   루프 · playOnAwake 끔 · 0.50 · 2D
 ├ Mus_Ch6      ch6-colossus       루프 · playOnAwake 켬 · 0.35 · 2D
 └ Ambience_Blizzard
```

실측 — 발각에 추격 루프가 1.4 초에 걸쳐 `0 → 0.500` 으로 차오르고 장 음악이
`0.350 → 0.123`(= 0.35 × 0.35)으로 눌린다. 추격이 끝나면 반대로 빠지고 루프가
`Stop` 된다.

IMPORTANT: **바꾸면서 `noticed` · `lost` 의 생배선을 지웠다.** 안 지우면
`ChaseMusic` 이 볼륨을 올리는 동안 이벤트가 `Stop` 을 때려 둘이 싸운다. 이벤트에
남긴 `Snd_Roar.Play` 와 `Player.Shake` 는 한 번 치고 끝나는 것이라 그대로 둔다.

IMPORTANT: **6 장에는 장 음악이 아예 없었다.** `Music/ch6-colossus`(67 초)가
만들어져 있는데 씬에 안 놓여 있었다. 1~5 장은 다 도는데 6 장만 눈보라 앰비언스
뿐이었다. 흩어져 있던 소리 오브젝트도 이때 `_Audio` 아래로 모았다.

IMPORTANT: `roar` 를 쓰면서 `not a lion, not a bear` 를 넣지 않으면 사자가 온다.
울음 계열 금지어와 같은 이유다. 나온 것의 플랫니스는 0.19 로 형제 범위 안이다.

**소리 하나에 오브젝트 하나**로 나눈다 (`Snd_<무엇>`). 한 오브젝트에 `AudioSource` 를
여럿 붙이면 인스펙터에서 어느 이벤트가 어느 소리를 부르는지 읽히지 않는다. 레버 소리만
레버 위에 둔다 — 소리가 나는 자리가 거기라서다.

멈추는 것도 코드가 필요 없다. `AudioSource.Stop` 도 인자 없는 public 함수라 `activated`
에 그대로 걸린다. **다만 6 장 히터는 안 멈춘다** — 다 녹은 뒤에도 계속 돈다. 켜 둔
기계가 할 일을 마쳤다고 저절로 꺼지지는 않고, 소리가 사라지면 오히려 그 순간에 시선이
간다. 얼음이 깨지는 소리가 「끝났다」를 알리는 몫을 한다.

삭도 소리는 **2026-08-09 에 전용으로 바꿨다.** 그전에는 4 장 발전기 소리를 빌려
썼는데, 권양기는 엔진이 아니라 **전동기와 강삭**이라 결이 달랐다. `cablecar-start` 는
브레이크가 풀리는 소리로 시작하고, `cablecar-loop` 는 강삭이 도르래를 지나며 우는
소리가 들어 있다. 씬의 `Snd_Start` · `Snd_Run` 을 이 둘로 갈아 끼우면 된다.

`CableCar/Snd_Run` 은 차체의 자식이라 **삭도와 같이 멀어지면서 저절로 작아진다.**
`Stop` 을 걸지 않은 이유가 이것이다.

**적이 내는 소리는 아직 없다.** `Audio/` 에 크리처 계열이 한 종도 없어서 발각 순간의
포효를 못 붙였다. 지금은 음악 큐와 화면 흔들림으로만 알린다. 적 소리를 뽑으면
`Colossus.noticed` 에 한 줄 더 걸면 된다.

IMPORTANT: **`EditorApplication.Step()` 으로 소리를 검증할 수 없다.** Step 은 에디터를
일시정지 상태로 두는데, 그 상태에서 `AudioSource.Play()` 는 걸리지 않아 `isPlaying`
이 계속 거짓이다. **소리가 안 나는 것처럼 보이지만 배선은 멀쩡하다.**
`PlayOneShot` 은 정지 중에도 걸려서 이 차이가 더 헷갈린다. 재려면
`EditorApplication.isPaused = false` 로 풀고 실제로 흐르게 둔 뒤에 본다.

### `Enemy/Audio/` — 적 소리 29 종

IMPORTANT: **적 소리만 `Audio/` 밖에 있다.** `Assets/Game/Enemy/Audio/` 다.
적은 담당(B)이 있고 그 폴더 안에서 모델 · 텍스처 · 애니메이션과 함께 관리되는
것이 맞기 때문이다. 파이프라인 스크립트는 이 경로를 알고 있다.

**클립 이름과 이벤트 프레임은 `docs/enemy-sheets/` 의 제작 명세를 그대로 따랐다.**
소리를 상상해서 만들지 않고 그 문서의 클립 표와 「프레임과 이벤트」에 맞췄으므로,
모델이 그 명세대로 나오면 이름만 보고 걸 수 있다.

세 종의 소리가 같은 결을 갖는 이유도 명세에 있다. 표면이 **검은 회청색 균열
피부에 흰 서리 흉터**로 고정되어 있고 **「젖은 값은 아래쪽 입과 관절 최심부에만
3% 미만」** 이며, `Colossus` 는 **「실제 석재 · 금속 재질 금지」** 다. 그래서 몸통은
전부 **마른 서릿결이 갈라지는 소리**이고 **젖은 소리는 입에서만** 난다. 셋 다
무안(無眼)이라 「노려본다」 류의 신호가 없어 **예고는 전부 호흡으로** 만들었다.

#### `Crawler` — 바닥 쐐기 10 종

`AC_Crawler` 에 이미 연결되어 있는 클립들이다. **여섯은 클립, 넷은 상태 전환이다.**

| 클립 / 상황 | 소리 | 거는 자리 |
| --- | --- | --- |
| `Crawler_Idle_Low` | `crawler-idle-loop` | 루프. `Speed` ≈ 0 |
| `Crawler_Skitter` | `crawler-skitter-loop` | 루프. 배회 속도 |
| `Crawler_Sprint` | `crawler-sprint-loop` | 루프. `IsChasing` |
| `Crawler_Attack_Underbite` **8f** | `crawler-attack-tell` | 예고. 명세의 「예고 정점」 |
| `Crawler_Attack_Underbite` **13f** | `crawler-attack` | `AE_AttackHit` 와 같은 프레임 |
| `Crawler_Hit` | `crawler-hit` | `Hit` 트리거 |
| `Crawler_Death` | `crawler-death` | `Dead` |
| 배회 → 추격 | `crawler-notice` | 전환에 **한 번만** |
| 배회 중 간헐 | `crawler-vocal-01` · `-02` | 몇 초에 한 번, 둘을 번갈아 |

공격을 **예고와 히트 둘로 나눈 것**이 핵심이다. 명세가 8f 예고 · 13f 히트로
나눠 놓았고, 기획도 「예고가 있으면 처음 만났을 때 억울하지 않다」고 못박았다.
하나로 합치면 그 0.17 초가 사라진다.

#### 2026-08-09 — 붙였다

위 표 열 줄이 전부 `Crawler.prefab` 에 연결되어 플레이 모드에서 확인됐다.
거는 쪽은 `CrawlerAudio` 컴포넌트이고, `Crawler` 의 이벤트
(`Noticed` · `AttackTell` · `AttackHit` · `Hurt` · `Died`) 를 구독한다. 발소리를
`PlayerFootsteps` 로 떼어낸 것과 같은 구조다 — 행동과 소리가 한 파일에 있으면
둘 중 하나를 고칠 때마다 다른 쪽 diff 가 섞인다.

`AudioSource` 는 둘이다. `Snd_Loop` 가 상태 루프(idle · skitter · sprint)를
갈아 끼우고, `Snd_OneShot` 이 나머지를 얹는다. 루프는 클립을 바꾸면 재생 위치가
0 으로 돌아가므로 **음량으로만 넘긴다** (`crossfade` 0.25 초).

| | 값 | 이유 |
| --- | --- | --- |
| Spatial Blend | 1 | 방향과 거리가 들려야 한다 |
| Volume | 0.9 | 이 문서의 `Enemy/Audio/` 출발값 |
| Rolloff | Linear, 1.5 ~ **24 m** | 기본값(Logarithmic, 500)이면 복도 저 끝의 것이 코앞처럼 들린다 |
| Doppler | 0 | 빠르게 달려들 때 음정이 흔들리면 안 된다 |

예고음 `crawler-attack-tell` 은 애니메이션 이벤트 `AE_AttackTell` (8f) 에서
난다. 그 이벤트는 원래 FBX 에 없어서 새로 넣었고, 같이 있던 `AE_AttackHit` 이
12f 로 한 프레임 어긋나 있던 것도 13f 로 맞췄다. 자세한 것은
[`decisions.md`](decisions.md) 의 「근접 피해는 13f 애니메이션 이벤트가 준다」.

배회 중에는 `crawler-skitter-loop` 이, 추격 중에는 `crawler-sprint-loop` 이
걸린다. 이 갈림은 상태가 아니라 **실제 속도**로 판단한다 — 추격 중에 멈춰서
공격할 때는 `crawler-idle-loop` 이 맞다.

IMPORTANT: **조사(소리를 들었을 때)에는 `crawler-notice` 를 쓰지 않는다.** 그것은
「봤다」의 소리라 여기 쓰면 플레이어가 이미 들킨 줄 알고 숨기를 포기한다.
배회 발성(`crawler-vocal-01` · `-02`)을 대신 낸다.

### 2 장 음악 — 장 트랙을 깔고 추격 루프를 겹친다 (2026-08-09)

2 장에는 음악이 아예 걸려 있지 않았다. `_Audio` 아래에 둘을 두었다.

| 오브젝트 | 클립 | Volume | Play On Awake |
| --- | --- | --- | --- |
| `Mus_Ch2` | `ch2-hide` | 0.35 | ○ |
| `Mus_Chase` | `chase-dread-loop` | 0.50 | ✕ — `ChaseMusic` 이 페이드로 켠다 |

`ChaseMusic` 컴포넌트가 씬의 `Crawler` 들을 훑어 하나라도 추격 중이면 추격
루프를 1.4 초 페이드로 올리고 **장 음악을 0.35 배로 낮춘다.** 확인값은 추격 중
`Mus_Chase` 0.500 / `Mus_Ch2` 0.123 이다.

덕킹이 선택이 아닌 이유는 이 문서의 「음악을 크게 깔면 발소리와 천장에서 나는
소리가 묻혀 오히려 덜 무서워진다」 다. 둘을 같은 크기로 겹치면 정확히 그렇게 된다.

6 장은 같은 일을 `Colossus.noticed` · `lost` UnityEvent 로 한다. 2 장이 컴포넌트
쪽인 이유는 **`Crawler` 는 죽으면 `Destroy` 되기 때문**이다. 이벤트를 구독하면
해지 시점을 관리해야 하는데, 읽는 쪽이 매 프레임 `IsChasing` 을 보면 그 문제가
아예 없다. `Colossus` 는 죽지 않아 구독이 안전하다.

IMPORTANT: **`moan` · `call` · `bellow` · `cry` · `snarl` · `growl` 을 프롬프트에
쓰면 소 울음이 온다.**
전부 생성기에게 포유류 발성 신호다. 처음 뽑은 `crawler-vocal` 둘이 실제로 그렇게
나왔고, 재보니 **스펙트럴 플랫니스가 0.0001** 이었다 — 음정이 완벽하게 또렷하다는
뜻이고 그게 곧 짐승 울음이다. 「울음」 을 뜻하는 단어 대신 **소리가 나는 기관**으로
지시해 0.34 까지 올렸다(잡음에 가까울수록 높다).

다시 뽑을 때는 **형제들의 범위 안에 드는지** 확인한다. 플랫니스 0.03~0.39,
무게중심 39~5800 Hz 다. 잡음을 최대화하는 쪽으로 고르면 범위를 넘어 쉭쉭거리는
소리가 된다. 실제로 한 번 그렇게 골랐다가(7112 Hz) 범위 안쪽 것으로 바꿨다.

`snarl` 하나만 남겨 둬도 걸린다. `-02` 가 그랬다 — 나머지를 다 빼고도 「짧은
그르렁으로 끝난다」 를 남겼더니 플랫니스가 0.046 에 머물렀다. **목으로 내는
소리를 아예 지우고 턱과 뼈판이 부딪히는 물리적인 소리로만 지시**해서 0.157 이
됐다. 지금 둘은 0.34(찢어지는 비명)와 0.16(뼈판 부딪힘)으로 결이 갈린다.

#### `Clinger` — 천장의 것 9 종

`Clinger.cs` 도 프리팹도 아직 없다. **명세가 계약이므로 소리를 먼저 만들어 둔
것**이고, 코드를 붙일 때 `unity-integration.md` 의 이벤트 이름에 그대로 걸면 된다.

| 클립 / 이벤트 | 소리 | 거는 자리 |
| --- | --- | --- |
| `Clinger_Cling_Folded` | `clinger-cling-loop` | 루프. 천장 대기 |
| `Clinger_Cling_Tremor` | `clinger-tremor-loop` | 루프. **곧 떨어진다는 예고** |
| `Clinger_Unfold_Drop` **10f** | `clinger-drop-release` | `AE_DropRelease` |
| `Clinger_Land` **15f** | `clinger-land` | `AE_EnableGroundMode` |
| `Clinger_Scuttle` | `clinger-scuttle-loop` | 루프. 지상 추격 |
| `Clinger_Attack_Underjaw` **9f** | `clinger-attack-tell` | 예고 |
| `Clinger_Attack_Underjaw` **13f** | `clinger-attack` | 히트 |
| `Clinger_Hit` | `clinger-hit` | 피격 |
| `Clinger_Death` | `clinger-death` | 사망 |

`clinger-cling-loop` 은 **3 장 끝의 예고에 그대로 쓴다.** 기획이 「3장 끝 — 천장에서
나는 소리, 모습은 안 보인다」를 4 장의 예고로 잡아 두었는데, 그 소리가 이것이다.
모델 없이도 먼저 쓸 수 있다.

`clinger-tremor-loop` 과 `clinger-cling-loop` 의 차이가 곧 난이도다. 떨림으로
바뀌면 곧 떨어진다는 뜻이고, 5 장은 **「보이기 전에 소리가 먼저 온다」** 로 설계된
장이라 이 둘을 구분해서 들려주는 것이 그 장의 규칙 자체다.

#### `Colossus` — 죽지 않는 것 9 종

IMPORTANT: **사망 소리가 없다.** 명세에 `Death` 클립이 없고 `IsAlive` 는 항상
참이다. 「죽지 않는 것이 있다」는 규칙이 1 장부터 6 장까지 한 번도 깨지지 않아야
하므로, 소리로도 죽는 인상을 주면 안 된다.

| 클립 / 상황 | 소리 | 거는 자리 |
| --- | --- | --- |
| `Idle_Weight` | `colossus-idle-loop` | 루프. 정지 |
| `Walk_Knuckle` | `colossus-walk-loop` | 루프. 이동 바닥 |
| `Walk_Knuckle` 접지 | `colossus-step-01` · `-02` | 발이 닿는 프레임마다 번갈아 |
| `Push` **20f** | `colossus-push` | 전방 밀기 |
| `Grip_Cable` | `colossus-grip-cable` | 6 장 삭도 케이블 파지 |
| `Stagger` | `colossus-stagger` | 누적 임계로 2.50 m 밀릴 때 |
| `Buried` | `colossus-buried` | 사면에 묻힌 뒤. **계속 들린다** |
| 알아채는 순간 | `colossus-roar` | `Colossus.noticed` 의 `Snd_Roar`. 2.000 초로 잘랐다 |
| 1 장 창밖 | `colossus-distant-loop` | 루프. 실물 없이 실루엣만 |

`colossus-buried` 를 엔딩에서 **끊지 않는 것**이 이 적의 마지막 규칙이다. 묻힌
뒤에도 숨소리가 들려야 「밀려나고 묻혔을 뿐 죽지 않았다」가 전달된다.

`colossus-distant-loop` 은 1 장용이다. 기획이 1 장에서는 **실물이 필요 없다**고
했으므로 창밖 실루엣 연출에 이 소리만 얹으면 된다. `Colossus` 는 처음과 끝을
잇는 유일한 적이고, 그 이음매가 이 소리다.

### `Music/` — 배경음악 10 종

장별 6 곡에 큐 2 개, 타이틀과 엔딩이다. 기획서의 「여섯 장소」를 그대로 따랐다.

| 파일 | 어디에 | 길이 |
| --- | --- | --- |
| `title-theme` | `Title.unity` | 57s |
| `ch1-control-room` | 1 장 관제동 | 86s |
| `ch2-hide` | 2 장 숙소동 | 85s |
| `ch3-tunnel-combat` | 3 장 연료 갱도 | 57s |
| `ch4-nest` | 4 장 발전동 | 83s |
| `ch5-blizzard` | 5 장 외부 철탑 | 59s |
| `ch6-colossus` | 6 장 삭도 승강장 | 67s |
| `power-restored` | **큐.** 4 장 조명이 켜지는 순간 | 29s |
| `chase-dread-loop` | **루프.** 추격이 이어지는 동안 | 22s |
| `ending-descent` | `Ending.unity` | 58s |

---

## 2. 쓰는 법

### `AudioSource` 설정은 카테고리마다 다르다

| 카테고리 | Spatial Blend | Loop | Play On Awake |
| --- | --- | --- | --- |
| `Ambience/` | 0 (2D) | ○ | ○ |
| `Music/` 장별 | 0 (2D) | ○ | ○ |
| `Music/` 큐 2 종 | 0 (2D) | ✕ | ✕ |
| `Footsteps/` | 1 (3D) | ✕ | ✕ |
| `Structure/` · `Interaction/` · `Electrical/` | 1 (3D) | ✕ | ✕ |
| `Devices/` 단발성 | 1 (3D) | ✕ | ✕ |
| `Devices/` `-loop` 2 종 | 1 (3D) | ○ | 장치가 켜질 때 |
| `Enemy/Audio/` `-loop` 9 종 | 1 (3D) | ○ | 그 상태인 동안 |
| `Enemy/Audio/` 나머지 | 1 (3D) | ✕ | ✕ |

앰비언스와 음악만 2D 다. 나머지는 **소리가 나는 물건이 공간에 있으므로** 3D 로
두어야 방향과 거리가 들린다. 그래서 그것들은 임포트 단계에서 이미 모노다.

IMPORTANT: 적 소리는 **거리 감쇠를 반드시 손본다.** 기본값(`Logarithmic`,
`Max Distance` 500)으로 두면 복도 저 끝의 것이 코앞처럼 들려 위치를 읽을 수
없다. 2 장은 소리로 위치를 가늠하는 장이므로 이것이 곧 난이도다. `Max Distance`
를 20~30 쯤으로 좁히는 데서 시작한다.

### 볼륨은 파일이 아니라 `AudioSource` 에서 정한다

IMPORTANT: 파일들은 **카테고리 안에서만** 크기가 맞춰져 있다. 카테고리 사이의
균형 — 폭발이 발소리보다 커야 하는 것 — 은 일부러 파일에 굽지 않았다. 한번 구우면
되돌릴 수 없기 때문이다.

그래서 `fuel-ignite` 의 피크가 `footstep-snow-01` 보다 낮은 것은 정상이다. 꽉 찬
소리와 순간음이 같은 RMS 를 가지면 그렇게 된다. **폭발이 작게 들리면 파일을 다시
뽑지 말고 `AudioSource.volume` 을 올린다.**

출발점으로 쓸 값이다. 여기서 귀로 맞춰 나가면 된다.

| 카테고리 | volume |
| --- | --- |
| `Ambience/` | 0.5 |
| `Music/` 장별 | 0.35 |
| `Music/` 큐 | 0.7 |
| `Footsteps/` | 0.6 |
| `Interaction/` · `Electrical/` | 0.8 |
| `Structure/` | 0.9 |
| `Devices/` | 1.0 |
| `Enemy/Audio/` | 0.9 |

### 발소리는 직전 것을 빼고 고른다

무작위로만 고르면 같은 것이 연달아 나오는 일이 생기고, 그게 제일 티가 난다. 직전
것만 빼면 해결된다.

```csharp
[SerializeField] private AudioClip[] clips;   // 표면당 2 종
private int last = -1;

private void PlayStep(AudioSource source)
{
    if (clips.Length == 0) return;
    int i = UnityEngine.Random.Range(0, clips.Length - 1);
    if (i >= last) i++;                       // 직전 것을 건너뛴다
    last = i;
    source.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
    source.PlayOneShot(clips[i]);
}
```

`pitch` 를 조금씩 흔드는 것까지 해야 적은 수로도 반복감이 줄어든다. **표면당 2 종이
되면서 이 흔들림이 더 중요해졌다** — 직전 것을 빼는 규칙과 겹쳐 실제로는 두 소리가
번갈아 나기 때문이다.

IMPORTANT: `UnityEngine.Random` 을 **끝까지 적는다.** 그냥 `Random` 으로 쓰면
`using System;` 이 있는 파일에서 `System.Random` 과 충돌해 `CS0104` 로 컴파일이
깨진다. `PlayerFootsteps.cs` 와 `PlayerMotor.cs` 가 그런 파일이다.

### 음악은 낮게 깔고 큐에서 올린다

IMPORTANT: **이 게임의 공포는 소리에서 나오고 그 소리는 `Ambience/` 가 맡는다.**
음악을 크게 깔면 발소리와 천장에서 나는 소리가 묻혀 오히려 덜 무서워진다.

장별 트랙은 배경으로 낮게 두고, 올리는 것은 큐 두 개에서만 한다.

- `power-restored` — 4 장에서 조명이 켜지는 순간. 기획서가 이 장의 핵심으로
  꼽은 지점이고, 장 전체에 깔린 음악으로는 이 순간이 안 만들어진다
`chase-dread-loop` 은 큐가 아니라 **추격 중에 깔아 두는 루프**다. 켜고 끄는 것은
`ChaseMusic` 이 페이드로 한다. 발각 순간에 치는 짧은 소리는 `colossus-roar` 이며,
위 「6 장 음악은 `ChaseMusic` 이 몬다」 참고.

### 장이 넘어갈 때

음악을 그냥 끊지 말고 1~2 초 페이드로 겹친다. 클립 자체에는 페이드가 없다 —
루프에 구멍이 생기지 않도록 앞뒤 무음을 잘라냈기 때문이다.

---

## 3. 새로 만드는 법

도구는 `docs/sound/` 에 넷이 있다. **프롬프트가 코드 안에 들어 있어서** 마음에
안 드는 것만 다시 뽑을 수 있고, 그 파일 자체가 AI 활용 근거로 남는다.

```bash
# 1. 생성. 프롬프트는 gen_sfx.py 의 PROMPTS 에 추가한다
python docs/sound/gen_sfx.py --category Devices --dry-run   # 예산 먼저 본다
python docs/sound/gen_sfx.py --category Devices

# 2. 앞뒤 무음 제거. 단발성은 여기서 wav 가 된다
python docs/sound/trim_silence.py --apply
python docs/sound/trim_silence.py --music --apply           # 음악은 이쪽

# 3. 원본 mp3 를 지운다 (단발성만. -loop 는 남긴다)

# 4. 음량 맞추기
python docs/sound/normalize.py --report                     # 편차를 먼저 본다
python docs/sound/normalize.py --apply

# 5. Unity 임포트 설정은 MCP 로 적용한다. 아래 「함정」 참고
```

루프를 만들었으면 `check_loop.py` 로 이음매를 확인한다. 귀보다 파형이 정확하다.

```bash
python docs/sound/check_loop.py Treeline/Assets/Game/Audio/Ambience
```

API 키는 저장소 루트 `elevenlabs.local.key` 에서 읽는다. 이 파일은 `.gitignore`
에 걸려 있으므로 **각자 자기 키를 넣어야 한다.** 대화창에 붙여넣지 않는다 —
프롬프트 로깅 훅이 `docs/ai/prompts/` 에 그대로 기록해 커밋한다.

요금은 효과음이 초당 40, 음악이 **분당 900 크레딧**이다. 음악이 훨씬 비싸므로
`--dry-run` 으로 먼저 본다.

---

## 4. 함정

전부 실제로 밟은 것들이다. 같은 데서 시간을 쓰지 않도록 남긴다.

### 생성기는 요청한 길이를 무음으로 채운다

`light-switch-click` 은 1.48 초 중 **앞 884ms 가 무음**이었다. 스위치를 누르고
1 초 가까이 지나야 딸깍 소리가 났다. 음악은 뒤가 더 심해서 `ch6-colossus` 는
75 초 중 뒤 7.1 초가 무음이었고, 그대로 루프시키면 매 바퀴 음악이 끊긴다.

`trim_silence.py` 가 이걸 자른다. **새로 뽑았으면 반드시 거친다.**

### 「조용한」 소리를 요청하면 들리지 않는 것이 온다

IMPORTANT: 프롬프트에 `quiet` · `soft` · `almost silent` 를 넣으면 정말 안 들리는
것을 준다. `footstep-concrete-03`(「quiet careful footfall」)은 피크가 -46.8 dBFS
였다. 증폭으로는 못 고친다 — 올리면 잡음이 같이 올라온다.

**크기를 요청하지 말고 마이크 거리로 지시한다.** 「soft hollow tap」 대신
「sharp hollow knock, **close microphone**」 이다. 크기는 `normalize.py` 와
`AudioSource` 에서 정하면 된다.

### 단발성은 앞을 잘라냈다

앞은 10ms 만 남긴다. 0 으로 딱 붙이면 첫 파형이 중간에서 시작해 딸깍거린다.

잘라낸 결과는 **wav 로 저장한다.** mp3 를 다시 mp3 로 인코딩하면 손실이 한 세대
더 쌓이는데, Unity 가 빌드에서 어차피 AAC 로 다시 굽기 때문에 원본까지 겹칠 이유가
없다. 반대로 **음악은 wav 로 풀지 않는다** — 90 초짜리 하나가 15MB 라 저장소가
감당하지 못한다. `--music` 은 ffmpeg 스트림 복사로 자르므로 재인코딩이 없다.

### 음량은 길이에 따라 측정 방식이 갈린다

긴 것은 LUFS, 3~5 초 미만은 RMS 다. R128 은 3 초 이상을 전제로 게이트를 걸어서
1 초짜리에 쓰면 신호 대부분을 버린다. 실제로 비슷하게 들리는 콘크리트 발소리
3 종이 **-66 · -54 · -43 LUFS** 로 나왔다.

카테고리별 목표와 맞춘 뒤의 편차다.

| 카테고리 | 목표 | 편차 |
| --- | --- | --- |
| `Ambience/` | -23 LUFS | 0.0 dB |
| `Music/` | -18 LUFS | 2.7 dB |
| 나머지 전부 | -22 dBFS RMS | 0.9 ~ 11.6 dB |

`Devices/` 의 11.6 dB 는 문제가 아니다. `ice-melt-crack` 처럼 띄엄띄엄 깨지는
소리는 사이가 비어 RMS 가 낮게 나올 뿐이고, 피크는 다른 것들과 같은 자리에 있다.

### Unity 임포트 — AAC 는 WebGL 오버라이드로만 들어간다

IMPORTANT: 기본 설정에 AAC 를 넣으면 Unity 가 **조용히 PCM 으로 되돌린다.** AAC 가
WebGL 전용 포맷이라 기본 플랫폼에서는 유효하지 않기 때문인데 경고가 없다. 설정한
뒤 되읽어 확인하지 않으면 빌드 용량이 몇 배로 불어난 채 넘어간다.

그리고 **WebGL 오버라이드에 `sampleRateSetting` 을 같이 넘기면 호출 전체가
거부된다.** `SetOverrideSampleSettings` 가 `false` 를 돌려주는데 이유는 안 알려준다.
그 필드만 빼면 통과한다.

```csharp
var d = imp.defaultSampleSettings;
d.compressionFormat = AudioCompressionFormat.Vorbis;   // 에디터 · 스탠드얼론
d.quality = 0.7f;
d.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
d.loadType = load;
d.preloadAudioData = !longform;
imp.defaultSampleSettings = d;

var w = imp.GetOverrideSampleSettings("WebGL");        // 반드시 Get 으로 시작한다
w.compressionFormat = AudioCompressionFormat.AAC;      // 배포 대상
w.quality = 0.7f;
w.loadType = load;
w.preloadAudioData = !longform;
// sampleRateSetting 은 건드리지 않는다
imp.SetOverrideSampleSettings("WebGL", w);

imp.forceToMono = !stereoWanted;    // 앰비언스와 음악만 스테레오
imp.loadInBackground = longform;
imp.SaveAndReimport();
```

한 번에 40 개 넘게 `SaveAndReimport` 를 돌리면 MCP 응답이 안 온다. 폴더별로 나눈다.

---

## 5. 아직 없는 것

- **재생 코드 전부.** 파일과 임포트 설정만 있다
- **무기 소리.** 권총 · 산탄총. 담당 폴더(`Player/`)로 간다
- **`Clinger` · `Colossus` 의 코드.** 소리는 명세대로 만들어 두었지만
  `Clinger.cs` · `Clinger.prefab` 이 아직 없다. 모델이 명세와 다르게 나오면
  그때 소리도 다시 본다
- **오디오 믹서.** 지금은 `AudioSource.volume` 으로 맞추면 되지만, 설정 화면에
  볼륨 슬라이더가 들어가면 그때 `AudioMixer` 로 묶는 것이 맞다

발소리 재생은 플레이어 이동에 붙으므로 **A 이정범** 담당과 겹친다. 장치 · 상호작용
소리는 **C 임성규** 담당이다. 파일만 있고 코드는 없는 상태라 지금은 충돌하지
않는다.
