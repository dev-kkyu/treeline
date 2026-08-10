> 상태: **닫음 · 전부 붙었다** · 최종 수정 2026-08-10 (마감 시점)

# 사운드

`Assets/Game/Audio/` 에 71 개, `Assets/Game/Enemy/Audio/` 에 32 개,
`Assets/Game/Player/Audio/` 에 25 개, 모두 **128 개**가 들어 있다. **전부 만들어져
있고 임포트 설정도 끝났으며, 씬과 컴포넌트에 다 물렸다.**

2026-08-10 기준으로 **안 물린 자리는 없다.** 마지막까지 남아 있던 것은 씬에만 있고
컴포넌트에 안 걸린 상호작용 소리 51 자리였고, 그것을 채우면서 닫혔다. 6장을 다시
지으며 빠진 것(얼음 균열음 · 권양기 시동과 가동 루프 · 전력 투입 후 형광등 험 ·
문 여닫기 · 제설포 발사와 명중)도 같이 채웠다.

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

### `Electrical/` — 전기 6 종

정전과 복구가 이 무대의 두 축 중 하나라 따로 뒀다.

| 파일 | 어디에 |
| --- | --- |
| `light-switch-click` | 조명 스위치 |
| `breaker-throw` | 4 장 차단기 두 개, 6 장 배전반 |
| `power-down` | 정전되는 순간 |
| `power-up` | 4 장 전력 복구. 조명이 켜지는 순간 |
| `fluorescent-flicker` | 형광등이 들어오려다 마는 것 |
| `beacon-strike` | 5 장 유도등이 켜지는 순간. 접점이 닫히고 램프가 점화된다 (2026-08-10) |

### `Interaction/` — 상호작용 11 종

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
| `fuel-pour` | `CarrySlot` | 6 장 권양기 투입구에 연료를 붓는다 (2026-08-10) |
| `paper-handle` | `Readable` | 집어서 펼칠 때 (2026-08-10) |

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
| | `device-stop` | `Deactivate()`. 모터가 잦아들고 접점이 떨어진다 (2026-08-10) |
| | `ice-melt-crack` | 히터가 다 돌아 얼음이 깨질 때 |
| | `generator-start` | 4 장 발전기 기동 |
| | `generator-stop` | 발전기가 내려간다 (2026-08-09) |
| | `snowblower-start` | 5 장 제설기 시동 |
| | `cablecar-start` | 6 장 삭도 권양기 기동 |
| `SustainedDevice` | `snowblower-loop` | 제설기가 도는 동안 |
| | `cablecar-loop` | 삭도가 가는 동안 |
| | `searchlight-swivel-loop` | 서치라이트가 도는 동안 |
| | `hydrogen-hiss-loop` | 축전지실에 수소가 차 있는 동안 (2026-08-09) |

`TimedDevice` 는 `Started` 이벤트와 `Progress` 가 이미 있으므로 시작 · 완료
두 지점에 그대로 걸면 된다.

IMPORTANT: **`InstantDevice` 의 `deactivatedClip` 은 비워 둔다.** 그 클래스의
`Deactivate()` 는 본문이 없다 — 셔터 · 인화 · 폭발 · 제설포는 되돌릴 수 없는
장치라서 그렇게 만든 것이다. 채워 넣어도 영영 울리지 않는 데이터가 된다.
`device-stop` 이 가는 곳은 `SustainedDevice` 와 `TimedDevice` 뿐이다.

### 상호작용 소리 자리를 한 번에 훑는 법 (2026-08-10)

씬마다 사람이 손으로 물리는 값이라 **빠진 것이 조용히 남는다.** 2026-08-10 에
훑어보니 자리 92 개 중 **51 개가 비어 있었다** — Ch1 · Ch5 · Ch6 은 상호작용 소리가
하나도 물려 있지 않았고, Ch2 · Ch4 는 일부만 있었다. 소리가 없는 것이 아니라
**걸지 않은 것**이었으므로, 자산을 새로 만들 필요가 있었던 것은 넷뿐이다
(`device-stop` · `fuel-pour` · `paper-handle` · `beacon-strike`).

들여다볼 필드는 이것이 전부다. 씬을 열어 `SerializedObject` 로 비었는지만 본다.

| 컴포넌트 | 필드 |
| --- | --- |
| `Lever` | `pullClip` · `lockedClip` |
| `Pickup` | `takeClip` · `deniedClip` |
| `Carriable` | `liftClip` · `dropClip` |
| `CarrySlot` | `acceptClip` |
| `Readable` | `readClip` |
| `Device` 계열 | `activatedClip` · `deactivatedClip` · `startedClip` |

IMPORTANT: **`EditorSceneManager.OpenScene` 은 저장 안 된 변경을 그냥 버린다.**
여러 씬을 돌며 값을 채울 때 마지막에 한 번 `SaveOpenScenes()` 하면 **마지막 씬만
남는다.** 실제로 Ch1 · Ch2 · Ch5 의 작업이 그렇게 사라졌고, 다시 훑어보고 나서야
알았다. 다음 씬을 열기 **전에** 저장한다.

IMPORTANT: **`generator-stop` 은 `Electrical/power-down` 과 다른 소리다.** 저쪽은
건물의 조명과 팬이 죽는 소리이고 이쪽은 **기관이 멈추는** 소리다. 2.5 초 동안
발화 간격이 벌어지다가 플라이휠만 돌아 멈춘다. 둘을 겹쳐 쓸 수는 있지만 서로를
대신하지 못한다.

#### 4 장 축전지실 폭발은 소리 다섯이 이어진 사건이다 (2026-08-09)

`hydrogen-explosion` 하나로는 그 장면이 안 된다. 기획의 네 문장이 각각 다른
소리다 — 「문 옆 계기가 빨갛다 · 여기서 쏘면 터진다」, 「폭발이 무리를
쓸어버린다」, 「대신 축전지가 날아가 조명 일부가 안 들어온다」.

| 순서 | 파일 | 무엇 |
| ---: | --- | --- |
| 0 | `hydrogen-hiss-loop` | 방에 들어서면 들린다. **경고다** |
| 1 | `hydrogen-explosion` | 터진다 |
| 2 | `battery-arc` | 축전지가 날아간다. 조명이 죽는 이유 |
| 3 | `debris-fall` | 방이 무너진다 |
| 4 | `explosion-ear-ring` | 사람 쪽 꼬리. 폭발이 카메라를 흔드는 것의 소리 짝 |

IMPORTANT: **0 번이 없으면 규칙을 죽어보고 배운다.** 계기 색은 보고 있어야
보이지만 소리는 방에 들어서면 들린다. `explosion-ear-ring` 은 4 초로 뽑았지만
**들리는 길이가 1.1 초**다 — 1.5 초를 넘으면 피크 대비 −54 dB 이하로 내려간다.

**2026-08-10 에 배선했고, 새 컴포넌트는 만들지 않았다.** `HydrogenPocket` 이
`exploded` UnityEvent 를 이미 내고, `AudioSource.PlayDelayed(float)` 는 **인자가
하나인 메서드라 UnityEvent 가 값을 넣어 부를 수 있다.** 그래서 폭발 뒤 세 소리는
지연값만 다르게 걸면 끝난다.

| 어디 | 무엇 | 지연 | 실측 |
| --- | --- | ---: | ---: |
| `HydrogenHaze/Snd_HydrogenHiss` | `hydrogen-hiss-loop` · loop · playOnAwake | — | 저절로 돈다 |
| `exploded` → | `Snd_HydrogenBlast.Play` (기존) | 0 | 0.02 s |
| `exploded` → | `Snd_BatteryArc.PlayDelayed` | 0.15 s | 0.18 s |
| `exploded` → | `Snd_EarRing.PlayDelayed` | 0.20 s | 0.25 s |
| `exploded` → | `Snd_Debris.PlayDelayed` | 0.50 s | 0.54 s |

경고 루프를 **가스 안개(`HydrogenHaze`)의 자식**으로 둔 것이 요점이다. `Ignite()` 가
끝에서 자기 오브젝트를 끄므로 폭발과 동시에 경고가 멈춘다 — `disables` 에 넣을
필요도, 끄는 코드도 없다.

IMPORTANT: **`PlayDelayed` 를 검증할 때 `isPlaying` 을 보면 안 된다.** 부른 즉시
참이 되므로 네 소리가 전부 같은 프레임에 시작한 것처럼 보인다. 실제로 소리가 나기
시작한 시점은 **재생 위치(`AudioSource.time`)가 0 을 벗어나는 순간**이다. 위 실측이
그 값이고, 30~40 ms 는 프레임 표본 간격이다.

가스가 옅어지면 **소리도 같이 옅어진다.** `GasVent` 가 이미 파티클과 조명을 팬의
진행도로 줄이고 있었고, 같은 자리에서 자식 `AudioSource` 의 음량도 줄인다. 보이는
것만 옅어지고 소리가 그대로면 팬을 다 돌린 뒤에도 방이 여전히 위험하게 들린다.

#### 폭발 · 제설포 · 눈사태는 장치 기준보다 크다

장치 카테고리의 기준(−22)은 모터와 셔터다. 그 장에서 가장 큰 사건을 레버와 같은
크기로 두면 최후의 수단이 최후로 들리지 않는다. `normalize.py` 의 `FILE_TARGETS` 가
`hydrogen-explosion` · `avalanche-cannon` · `avalanche-slide` 를 −14~−16 으로 잡는다.

#### 제설포와 눈사태를 다시 뽑았다 (2026-08-09)

| | 무게중심 | 200 Hz 아래 | 60 Hz HPF 손실 | |
| --- | ---: | ---: | ---: | --- |
| `avalanche-cannon` 1판 | 2762 Hz | — | 1.5 dB | 산을 흔드는 소리가 아니라 문 닫는 소리 |
| `avalanche-cannon` 2판 | 2341 Hz | **84.6 %** | 0.2 dB | 이것을 쓴다 |
| `avalanche-slide` 1판 | 4499 Hz | — | **8.0 dB** | 들리지 않는 초저역이 헤드룸을 먹는다 |
| `avalanche-slide` 3판 | 4385 Hz | 0.8 % | 0.0 dB | 이것을 쓴다 |

제설포는 「compressed gas」 를 **강철관 안에서 터지는 것**으로 바꾸자 저역이 왔다.
눈사태는 세 판을 뽑았지만 **끝까지 밝은 쪽으로만 나왔다** — 「무게」 를 형용사로
부탁하면 초저역이 오고, 질량이나 얼음덩이로 부탁하면 밝은 쉭 소리가 온다. 셋 중
초저역 쓰레기가 없는 것을 골랐다.

IMPORTANT: **눈사태의 무게는 이 파일에서 나오지 않는다.** 기획이 「눈사태와 폭발은
카메라를 흔들고 화면을 덮는 방식으로 만든다」로 이미 정해 두었으므로, 저역이
필요하면 파일을 다시 뽑는 것이 아니라 카메라 흔들림과 저역 레이어로 만든다.

`avalanche-cannon` 은 **0.76 초**다. 3 초로 뽑았지만 그 뒤는 피크 대비 −48 dB
이하라 잘라냈다 — 골짜기로 굴러가는 꼬리가 필요하면 별도 레이어로 얹는다.

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
| `Colossus.roarSound` | `Colossus/Snd_Roar` 의 `Play` | `colossus-roar` — 포효 클립 21f, 턱이 열리는 지점 |
| `Colossus.noticed` | `Player` 의 `PlayerLook.Shake` | 화면이 짧게 떨린다 |
| `Colossus.grabSound` | `Colossus/Snd_Grab` 의 `Play` | `colossus-grab`(0.34초) — 잡기 클립 8f. **놓쳤을 때는 안 울린다** |
| | `Colossus/Snd_GrabRoar` 의 `Play` | `colossus-roar` 를 **피치 0.80** 으로. 아래 참고 |
| `Colossus.killSound` | `Colossus/Snd_Kill` 의 `Play` | `colossus-kill`(1.99초) — 90f. 화면이 닫히는 것과 같은 프레임 |
| `CableCar.activated` | `BoardLever` 의 `Play` | `lever-pull` |
| | `CableCar/Snd_Start` 의 `Play` | `generator-start` — 권양기가 돈다 |
| | `CableCar/Snd_Run` 의 `Play` | `generator-rumble-loop` — 가는 동안 |

#### 붙잡을 때의 포효는 피치로 길이를 맞췄다

붙잡는 8f 에서 포효를 한 번 더 내고 90f 의 즉사 소리로 잇는다. 클립이 하나뿐이라
발각 포효와 같은 것을 쓰는데, **그대로 쓰면 2.00 초라 2.27 초에 끝나고 무는 순간
(2.97 초)까지 0.7 초가 빈다.**

`pitch` 를 **0.80** 으로 내려 2.50 초로 늘렸다. 8f(0.27 초)에 시작하면 **2.77 초 —
물기 시작(84f)에 정확히 닿는다.** 남는 0.2 초는 턱이 닫히는 구간이고, 그 침묵 뒤에
즉사 소리가 온다.

피치를 내린 것은 길이 때문만이 아니다. **같은 클립이 두 번 울리는 것을 가리고**,
낮은 쪽이 더 가깝고 큰 것으로 들린다. 발각은 멀리서, 붙잡을 때는 얼굴 앞에서다.

#### 발소리는 거리로 낸다 — 클립에 접지 이벤트가 없다

`Colossus.fbx` 의 애니메이션 이벤트는 넷뿐이고(`GrabLock` · `GrabKill` · `Roar` ·
`Threat`) **접지 이벤트가 없다.** 그래서 플레이어 발소리와 같은 방식으로, 간 거리를
쌓아 보폭마다 낸다.

| | 값 |
| --- | --- |
| 보폭 | **1.1 m** — `Walk_Knuckle` 한 주기(1.80초)에 두 발 |
| 배회(1.2 m/s) | 0.92 초 간격 |
| 추격(3.4 m/s) | **0.32 초 간격** |
| 크기 | 배회 0.55 / 추격 0.90 |

**거리로 재면 배회와 추격이 저절로 갈린다.** 시간으로 하면 속도를 고칠 때마다
간격도 같이 고쳐야 하고, 벽에 붙어 제자리걸음 할 때도 소리가 난다.

`colossus-walk-loop` 은 그 아래에 깔린다. 움직이는 동안만 재생하고 멈추면 끊는다.

IMPORTANT: **루프는 스스로 안 멈춘다.** `Update` 가 도는 동안만 관리되므로
`OnDisable` 에서 끊어야 한다. 삭도가 떠날 때 `StandDown()` 이 컴포넌트를 끄는데,
안 끊으면 걷는 소리만 승강장에 남는다.

IMPORTANT: **소리를 애니메이션 이벤트에 걸면 「놓쳤다」가 공짜로 표현된다.**
`grabSound` 는 8f 재검사를 통과했을 때만 불리므로, 헛손질에는 아무 소리도 나지
않는다. 사거리로 갈리는 분기를 소리 쪽에 다시 만들 필요가 없다.

IMPORTANT: **`colossus-kill` 은 1.99 초인데 장이 1.5 초 뒤에 다시 시작한다.**
`GameFlow.DeathToRestartSeconds` 가 1.5 라 뒤 0.5 초가 씬 재시작에 잘린다. 검은
화면에서 끊기는 것이라 지금은 그럴듯하지만, 온전히 들리게 하려면 그 값을 2.2 로
올려야 한다 — `GameFlow` 는 C 소유다.

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

### `Enemy/Audio/` — 적 소리 31 종

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
| `Clinger_Cling_Tremor` | `clinger-tremor-loop` | **One-shot.** 곧 떨어진다는 예고 |
| `Clinger_Unfold_Drop` **f9** | `clinger-drop-release` | `AE_DropRelease` |
| `Clinger_Land` **f14** | `clinger-land` | `AE_EnableGroundMode` |
| `Clinger_Scuttle` | `clinger-scuttle-loop` | 루프. 지상 추격 (콘크리트) |
| `Clinger_Scuttle` | `clinger-scuttle-snow-loop` | 루프. **5 장 눈 위** (2026-08-09) |
| `Clinger_Attack_Underjaw` **f8** | `clinger-attack-tell` | 예고 |
| `Clinger_Attack_Underjaw` **f12** | `clinger-attack` | 히트 |
| `Clinger_Hit` | `clinger-hit` | 지상 피격 |
| `Clinger_Death_Fall` **f4** | `clinger-death` | 천장 사망. `AE_DeathRelease` |
| `Clinger_Death` **f3** | `clinger-death` | 지상 사망. `AE_DeathRelease` |

#### 5 장은 같은 걸음이 눈에서 난다 (2026-08-09)

`Clinger` 는 4 장과 5 장에 같이 나오는데 「실내가 아니라 눈보라 속」이다. 같은
`Clinger_Scuttle` 클립에 **바닥이 바뀌면 소리가 통째로 바뀐다** — 딱딱한 충격이
사라지고 묻히는 소리와 파헤치는 소리만 남는다. 그래서 이동 루프만 눈 판을 따로
만들었다. 나머지 여덟은 바닥과 무관하다.

IMPORTANT: 프롬프트를 「눈 위 발소리」로 쓰면 **사람 발소리 넷이 온다.**
`05-scuttle.md` 가 이 걸음의 정체를 「앞팔(1.55 m)이 몸을 던지고 짧은 뒷사지
(0.82 m)가 못 따라가 바닥을 긁으며 끌려온다 — 뒤 갈고리가 1.5 m/s 미끄러지는 것은
버그가 아니라 이 걸음」으로 못박았으므로, **앞발이 박히는 소리와 뒷발이 눈을
파헤치는 소리를 따로** 지시해야 그 절뚝임이 나온다.

`PlayerFootsteps` 가 「밟고 있는 바닥은 레벨을 만든 쪽만 안다」로 장마다 표면을
정하는 것과 같은 문제다. 적도 같은 길을 따른다 — `ClingerAudio.surface` 를 씬에
놓인 인스턴스에서 덮어쓴다. 4 장은 `Concrete`, 5 장은 `Snow` 다.

#### 이동 루프 둘은 아무도 재생하지 않고 있었다 (2026-08-09)

`Clinger.prefab` 은 소리를 **UnityEvent 로** 걸어 두었다 — 일곱 개가 `Snd_Voice` ·
`Snd_Tremor` 로 간다. 그런데 `Snd_Cling`(천장 대기)과 `Snd_Scuttle`(지상 추격)에는
아무 이벤트도 없고 `playOnAwake` 도 꺼져 있어서 **두 루프가 한 번도 나지 않았다.**

이벤트로는 안 되는 것이 맞다. 「천장에 붙어 있는 동안」과 「달리는 동안」은 사건이
아니라 **상태**라 매 프레임 판단해야 한다. 그래서 `ClingerAudio` 를 만들었고
**그 두 루프만** 맡는다 — 단발은 그대로 UnityEvent 가 낸다. 같은 것을 양쪽에서
내면 소리가 두 번 난다.

IMPORTANT: **두 루프를 소스 하나로 합쳐 클립만 갈지 않는다.** 프리팹이 천장 쪽을
0.45 · 13 m, 지상 쪽을 0.80 · 22 m 로 잡아 두었다. 매달린 것은 가까이 와야 들리고
달려오는 것은 멀리서 들려야 하므로, 그 차이가 곧 설계다. 소스를 합치면 사라진다.

속도는 `ClingerAudio` 가 `NavMeshAgent.velocity` 를 직접 읽는다. `Clinger` 는 속도를
밖으로 내지 않고, 그것 하나 때문에 `Clinger` 를 고칠 이유는 없다.

2026-08-09 갱신: 애니메이션 명세가 개정되면서 세 가지가 바뀌었다.

- **`Cling_Tremor` 가 Loop 에서 One-shot 이 됐다.** 소리 파일은 그대로 쓰되
  루프로 걸지 않는다. 반복되면 경고가 배경이 되기 때문이다. 플레이어가 계속
  가까이 있으면 간격을 두고 다시 재생한다.
- **천장 피격 반응이 `Cling_Tremor` 다.** 매달린 채 총에 맞으면 이 클립과
  이 소리가 난다. `clinger-hit` 은 지상 전용이다.
- **사망이 두 클립으로 갈렸다.** `clinger-death` 를 둘 다에 걸되, 천장 사망은
  `Death_Fall` → 코드 낙하 → 바닥 접촉 → `Death` 로 이어지므로 **소리는 첫
  클립에서 한 번만** 낸다. 두 번 나면 두 마리로 들린다.

프레임 번호는 Unity 기준이며 Blender 문서의 값보다 1 작다.

`clinger-cling-loop` 은 **3 장 끝의 예고에 그대로 쓴다.** 기획이 「3장 끝 — 천장에서
나는 소리, 모습은 안 보인다」를 4 장의 예고로 잡아 두었는데, 그 소리가 이것이다.
모델 없이도 먼저 쓸 수 있다.

`clinger-tremor-loop` 과 `clinger-cling-loop` 의 차이가 곧 난이도다. 떨림으로
바뀌면 곧 떨어진다는 뜻이고, 5 장은 **「보이기 전에 소리가 먼저 온다」** 로 설계된
장이라 이 둘을 구분해서 들려주는 것이 그 장의 규칙 자체다.

#### `Colossus` — 죽지 않는 것 11 종

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
| 붙잡는 순간 | `colossus-grab` | 양손이 닫힌다. 즉사 연출 시작 |
| 끝나는 순간 | `colossus-kill` | 목이 뜯긴다. 여기서 화면이 넘어간다 |
| 1 장 창밖 | `colossus-distant-loop` | 루프. 실물 없이 실루엣만 |

IMPORTANT: **걷는 소리를 저역으로만 만들면 안 들린다.** 처음 만든 것이 그랬다 —
`colossus-step-01` 의 무게중심이 **29 Hz** 라 노트북 스피커에서 아예 사라졌고
`walk-loop` 도 88 Hz 였다. 거대함을 서브로만 표현한 결과다. 서브는 몸으로 느끼는
몫이고 **귀에 남는 것은 충격체(200~500 Hz)와 부스러기 소리**다. WebGL 이라 더 그렇다.

프롬프트를 고쳐 다시 뽑으니 이번엔 4633 Hz 로 튀어 자잘해졌다. **생성기는 무게와
충격체를 동시에 못 준다.** 그래서 옛것(무게)과 새것(충격체)을 **타격 지점을 맞춰
겹쳤다.**

그런데 그래도 가볍게 들렸다. 대역을 잘게 재고서야 이유가 나왔다.

```
대역 %                20-60  60-120  120-250   합
colossus-step-01       20.6    2.1     0.9    25.6   ← 74% 가 20Hz 아래
hydrogen-explosion     34.3   16.2     9.3    97.3   ← 무겁게 들리는 것
```

IMPORTANT: **에너지의 3/4 이 20Hz 아래에 있었다.** 사람이 못 듣고 스피커도 못 내는
대역인데, 헤드룸을 다 먹어서 정규화할 때 나머지를 눌러 버린다. 그래서 「저역 96%」
인데도 귀에는 밝은 부분만 남아 가볍게 들렸다. **저역 비율만 보면 이걸 놓친다 —
대역을 잘게 나눠 봐야 한다.**

32Hz 아래를 잘라냈다. 그것만으로 들리는 무게 대역(60~250 Hz)이 **3.0% 에서 34.1%**
가 됐다. 감쇠하는 저음 몸통을 합성해 얹는 것도 시도했지만, 크기를 탐색해 보니
**안 얹는 쪽이 기준 프로파일에 가장 가까웠다** — 얹으면 원래 충격음을 덮는다.

`colossus-walk-loop` 은 아예 **발소리를 배열해 만들었다.** 네 번 요청해도 그 사이가
안 나와서, 명세의 `Walk_Knuckle`(54f/1.8s · 접지 넷)에 맞춰 0.45 초 간격으로 20 개를
놓고 **숨소리(`colossus-idle-loop`)를 질감으로 아래에 깔았다** — 걷는 동안에도 숨은
쉰다. 발소리를 고치면 루프도 다시 조립한다. 이러면 **루프의 발소리와 애니메이션
이벤트로 트리거할 발소리가 같은 소리**가 되고, 주기의 정수배(9.0s)라 이음매도
구조적으로 맞는다. `check_loop.py` 가 「페이드 있음」 으로 표시하는데 발소리 사이의
정적을 오인한 것이고 단차는 0.2 라 클릭이 없다.

즉사 소리 둘은 기획의 **「붙잡히면 한 번에 죽는다. 양손으로 붙들려 목이 뜯긴다.
체력이 깎이는 것이 아니라 그 자리에서 끝난다」** 를 위한 것이다. `Colossus` 만 한다 —
다른 적에게는 체력 세 칸이 있다. **둘로 나눈 이유**는 붙잡는 순간과 끝나는 순간
사이에 간격을 둘 수 있어야 해서다. 한 덩어리면 타이밍을 못 맞추고 「그 자리에서
끝난다」 의 갑작스러움도 안 산다.

#### 공간과 크기 — `Colossus` 만 다르게 둔 두 가지

**하나, 잔향을 파일에 구웠다.** 무대가 6 장 삭도 승강장이라 콘서트홀이 아니라
**바위면에서 늦게 돌아오는 반사**가 맞다. 선반사 45 · 120 · 210ms 에 RT60 2.2 초,
공기 흡수로 3.2kHz 위를 깎은 임펄스 응답을 만들어 감았다. 발소리 하나가 2.6 초에서
5.2 초가 되고 충격 뒤 1.31 초 동안 잔향이 남는다.

젖은 비율은 소리마다 다르다. 전부 같으면 거리감이 뭉개진다 — 포효 0.60, 발소리
0.50~0.55, 케이블 파지 0.35, 숨소리 0.30, **즉사 0.25~0.30**. 즉사는 코앞에서
벌어지므로 넓게 하면 남 일처럼 들린다.

IMPORTANT: **루프는 순환 컨볼루션으로 감는다.** 그냥 감으면 잔향 꼬리가 파일 끝을
넘어 잘려서 한 바퀴마다 잔향이 뚝 끊긴다. 꼬리를 앞으로 감아 더해야 이음매가
유지된다. `walk-loop` 단차 0.2 · `idle-loop` 3.9 로 확인했다.

`colossus-buried`(눈에 묻힘)와 `colossus-distant-loop`(1 장 창밖)에는 걸지 않았다.
전자는 갇힌 소리라 반향이 없고 후자는 이미 거리감이 있어 더하면 뭉갠다.

**둘, 음량을 -15 로 올렸다.** 다른 적은 -20 이다. 4.5m 짜리가 기는 것과 같은 크기면
크기가 안 읽힌다. 피크가 이미 -1~-5 라 리미터로 꼭지를 누르며 올렸고, 눌린 양을
재서 확인했다 — 대부분 0.2~1.5 dB 이고 `grip-cable` 만 2.4 dB 다.

이건 **「카테고리 사이 균형은 파일에 굽지 않는다」 의 예외**다. 카테고리 사이가 아니라
같은 카테고리 안에서 개체가 다르다는 표현이고, 프리팹 배선이 아직 없어 `AudioSource`
로 확인할 방법이 없었다. 배선이 되면 그쪽에서 다시 잡아도 된다.

`colossus-buried` 만 올리지 않았다. 눈 아래에서 나는 소리가 커지면 앞뒤가 안 맞는다.

`colossus-buried` 를 엔딩에서 **끊지 않는 것**이 이 적의 마지막 규칙이다. 묻힌
뒤에도 숨소리가 들려야 「밀려나고 묻혔을 뿐 죽지 않았다」가 전달된다.

`colossus-distant-loop` 은 1 장용이다. 기획이 1 장에서는 **실물이 필요 없다**고
했으므로 창밖 실루엣 연출에 이 소리만 얹으면 된다. `Colossus` 는 처음과 끝을
잇는 유일한 적이고, 그 이음매가 이 소리다.

### `Music/` — 배경음악 10 종

**타이틀과 엔딩은 루프다** (2026-08-09 교체). 화면이 계속 떠 있으므로 44 초에서
이음매 없이 이어진다. 음악 API 에는 loop 파라미터가 없어 `make_loop.py` 로 꼬리를
머리에 얹고 음량 기울기를 폈다 — 타이틀은 15.3 dB, 엔딩은 11.5 dB 차이가 있어서
안 펴면 한 바퀴마다 부풀었다 꺼진다.

둘 다 레벨 시안의 원칙을 따랐다. 「작은 광원은 남기고 그 빛이 닿지 않는 공간을 크게
둔다」 를 소리로 옮기면 **다 지워서 어둡게 만들지 않고 작고 연약한 것 하나를 남긴 뒤
둘레를 비우는 것**이다. 타이틀이 남긴 것은 두고 간 피아노이고, 엔딩이 남긴 것은
멀어지는 삭도 소리다.

IMPORTANT: **엔딩은 두 생성물을 겹친 것이다.** 생성기가 「아래에 묻힌 무게 + 위로
멀어지는 기계 소리 + 빈 공간」 셋을 동시에 못 준다. 따로 뽑으니 마침 서로 다른
대역에 있어서 겹칠 수 있었다. 다시 만들 때의 비율은 `gen_music.py` 에 적어 두었다.

장별 6 곡에 큐 2 개, 타이틀과 엔딩이다. 기획서의 「여섯 장소」를 그대로 따랐다.

| 파일 | 어디에 | 길이 |
| --- | --- | --- |
| `title-theme` | `Title.unity`. **루프** | 44s |
| `ch1-control-room` | 1 장 관제동 | 86s |
| `ch2-hide` | 2 장 숙소동 | 85s |
| `ch3-tunnel-combat` | 3 장 연료 갱도 | 57s |
| `ch4-nest` | 4 장 발전동 | 83s |
| `ch5-blizzard` | 5 장 외부 철탑 | 59s |
| `ch6-colossus` | 6 장 삭도 승강장 | 67s |
| `power-restored` | **큐.** 4 장 조명이 켜지는 순간 | 29s |
| `chase-dread-loop` | **루프.** 추격이 이어지는 동안 | 22s |
| `ending-descent` | `Ending.unity`. **루프** | 44s |

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

### 타이틀과 엔딩은 음악이 앞에 선다 (2026-08-09)

IMPORTANT: 위 표의 `Music/` 장별 **0.35 는 플레이 중인 장 기준**이다. 근거가
「음악을 크게 깔면 발소리와 천장에서 나는 소리가 묻혀 오히려 덜 무서워진다」인데,
**타이틀과 엔딩에는 묻힐 발소리가 없다.** 오히려 음악이 그 화면의 내용이다.
두 화면만 예외로 잡는다.

| | 음악 | 앰비언스 | 그 외 |
| --- | ---: | ---: | --- |
| `Title` | `Mus_Title` **0.62** | `Amb_Blizzard` 0.26 | — |
| `Ending` | `Mus_Ending` **0.62** | `Amb_Blizzard` 0.26 | `Snd_Buried` 0.50 |

처음에 표대로 0.35 로 두었더니 **음악이 셋 중 가장 작았다.** 눈보라 0.45 와
묻힌 숨소리 0.70 에 깔려 「노래가 없다」로 들렸다.

IMPORTANT: 숫자만 보고 판단하면 늘 작게 잡는다. `TitleScreen` 과 `EndingScreen`
이 부팅 때 `AudioListener.volume = 저장값²` 을 걸기 때문이다. 기본 0.8 이면
**0.64 가 전체에 곱해진다** — 인스펙터의 0.35 는 귀에 0.22 로 닿는다.

`Snd_Buried` 를 0.70 → 0.50 으로만 내리고 끄지 않은 것은 이 적의 마지막 규칙
때문이다. 음악에 묻히면 「죽지 않았다」가 전달되지 않는다.

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

### 어느 장에 무엇이 깔려 있나 (2026-08-10 실측)

**팀원이 자기 장을 열었을 때 무엇이 이미 있는지 보려면 이 표를 본다.** 값은 씬에
들어 있는 실제 음량이다.

| 장 | 장 음악 | 추격 | `ChaseMusic` | 앰비언스 (2D 바닥) |
| --- | --- | ---: | :---: | --- |
| Title | `title-theme` 0.62 | — | — | `blizzard-outside` 0.26 |
| 1 장 | `ch1-control-room` 0.35 | — | — | `room-tone` 0.30 · `blizzard-muffled` 0.30 · `fluorescent-buzz` 0.22 |
| 2 장 | `ch2-hide` 0.35 | 0.50 | O | `room-tone` 0.50 · `blizzard-muffled` 0.30 |
| 3 장 | `ch3-tunnel-combat` 0.35 | 0.50 | O | `generator-rumble` 0.22 · `vent-fan` 0.14 |
| 4 장 | `ch4-nest` 0.35 | 0.50 | O | `room-tone` 0.30 |
| 5 장 | `ch5-blizzard` 0.35 | 0.50 | O | `blizzard-outside` 0.70 (플레이어 카메라) |
| 6 장 | `ch6-colossus` 0.35 | 0.50 | O | `blizzard-outside` 0.50 |
| Ending | `ending-descent` 0.62 | — | — | `blizzard-outside` 0.26 |

장 음악은 전부 **0.35** 이고 타이틀 · 엔딩만 0.62 다. 두 화면은 그 음악이 주인공이고
장 안에서는 배경이기 때문이다. 놓는 자리는 씬의 `_Audio` 루트이며 이름은
`Mus_*` · `Amb_*` 로 맞춘다. 소리가 나는 장치(히터 · 팬 · 발전기 · 삭도)는 3D 로
그 물건에 붙어 있으므로 이 표에 없다.

**1 장에 `ChaseMusic` 이 없는 것은 빠뜨린 것이 아니다.** 그 장의 `Colossus` 는
`Props/Silhouette` 이고 컴포넌트 · NavMeshAgent 가 꺼진 채 76 m 밖에 있다 —
창밖으로 지나가는 실루엣이라 쫓아오지 않는다. 1 장이 도망치는 장이 되면 그때 붙인다.

IMPORTANT: **`ChaseMusic` 은 추격 음악의 목표 음량을 `Awake` 에서 `chaseLoop.volume`
로 읽는다.** 그래서 씬에서 그 소스의 음량을 **0 으로 두면 추격 음악이 영영 안
난다** — 페이드 폭도 0 이 되어 아무 일도 일어나지 않는다. 3 장이 실제로 그 상태였고
(2026-08-10 에 0.50 으로 고쳤다) 콘솔에 아무 경고도 남지 않으므로 눈으로 잡아야 한다.
새 장을 만들면 **다른 장과 같은 0.50 을 넣고 `playOnAwake` 는 끈다.**

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

### `Player/Audio/` — 무기 15 종 · 주인공 8 종 (2026-08-09)

IMPORTANT: **무기와 주인공 소리도 `Audio/` 밖이다.** `architecture.md` 가 무기 ·
손전등의 담당 폴더를 `Player/` 로 정했으므로 적 소리와 같은 이유로 여기 있다.
폴더는 `Player/Audio/Weapons/` 와 `Player/Audio/Body/` 둘이다.

**클립 이름과 시각은 팔 애니메이션 명세에서 그대로 가져왔다.** `06-pistol.md` 의
「f8 탄창 멈치 · f39 MagazineIn · f43 슬라이드」, `07-shotgun.md` 의 「f18/32/46/60
탄 4발 · f69 PumpBack · f73 전진」 이 그대로 `WeaponAudio` 의 박자표다.

총성과 탄창 딸깍은 같은 폴더에 있어야 하지만 같은 음량이면 안 된다 — 카테고리로
맞추면 딸깍이 총성만큼 커진다. `normalize.py` 의 `FILE_TARGETS` 가 파일 단위로
목표를 덮어쓰고, 같은 표의 `max_gain` 이 이득 상한을 그 파일에서만 푼다.

#### 총성은 두 번, 펌프는 한 번 다시 뽑았다

판정은 **상승 시간**과 **60 Hz 아래가 먹는 에너지**, 그리고 **채널별 피크**다.

| | 상승 시간 / 무게중심 | 60 Hz HPF 손실 | |
| --- | ---: | ---: | --- |
| `shotgun-fire` | 8 ms | 2.2 dB | 처음부터 제대로 나왔다 |
| `pistol-fire` 1판 | 130 ms | — | 부풀어 오르는 소리고, 얇다 |
| `pistol-fire` 2판 | 174 ms | **13 dB** | 들리지 않는 초저역이 헤드룸을 먹었다 |
| `pistol-fire` 3판 | 20 ms | 1.5 dB | 이것을 쓴다 |
| `shotgun-pump-back` 1판 | 1493 Hz | 0.4 dB | 강철 긁는 결이 없고 둔탁하다 |
| `shotgun-pump-back` 3판 | 5390 Hz | 0.0 dB | 이것을 쓴다 |

`pistol-fire` 2판의 원인은 프롬프트의 「deep low end」 다. **저역을 달라고 하면
들리는 저역이 아니라 초저역 덩어리가 온다.** 화약이 터지는 몸통으로 지시하면
들리는 대역에 온다.

IMPORTANT: **클리핑은 채널별 피크로 본다.** `-ac 1` 다운믹스로 재면 1.0 을 넘는
값이 나오는데 그건 측정 방식이 만든 것이고 실제 클리핑이 아니다. 펌프 후퇴 2판은
채널별로 0 dBFS 에 **218 샘플**이 붙어 진짜 클리핑이었고, 프롬프트에서 극단적인
낱말(hauled · hard)을 덜어 다시 뽑아 14 샘플(0.3 ms)로 줄였다.

#### 금속에 맞은 것을 어떻게 고르나

발사 레이캐스트가 `Default` 와 `Enemy` 만 때리므로 **레이어로는 벽과 철판이
구분되지 않는다.** `WeaponAudio` 는 `metalLayers` 마스크를 먼저 보고, 비어 있으면
맞은 것의 이름 · 머티리얼 이름에 든 조각(`metal` · `grate` · `steel` · `pipe` ·
`drum` · `shutter` …)으로 짐작한다. 소품 이름이 규칙을 따르는 동안은 이것으로
충분하고, 어긋나면 `metalLayers` 에 레이어를 넣어 덮는다.

#### 부정문을 넣으면 소리가 사라진다

「no deep boom, no rumble」 을 넣은 `bullet-impact-concrete` 가 **피크 0.017 로
거의 무음**으로 왔다. 뺄 것을 적지 말고 **남길 것만** 적는다.

(적 소리의 「소가 아니다」 는 통했다 — 차이는 **대상이 있는지**다. 정체를 좁히는
부정은 되고, 대역을 빼라는 부정은 소리 자체를 지운다.)

#### 입김 소리는 `PlayerColdBreath` 가 부르고 `PlayerBodyAudio` 가 낸다 (2026-08-10)

입김 파티클은 `PlayerHud.Awake` 가 런타임에 붙이므로 **인스펙터로 클립을 물릴 수
없다.** 그래서 소리는 몸 소리 쪽에 두고 `PlayerBodyAudio.PlayColdBreathOnce()`
정적 진입점을 하나 뒀다 — 저쪽 파일에 남는 자리가 한 줄이면 담당이 갈리는 파일을
덜 건드린다. 플레이어는 씬에 하나뿐이라 정적으로 잡아도 된다.

IMPORTANT: **지쳐서 헐떡이는 동안에는 입김 소리를 내지 않는다.** 이미 깔린 호흡
루프 위에 느긋한 숨을 얹으면 두 사람이 숨 쉬는 것으로 들린다. 그 상태를 아는 것이
`PlayerBodyAudio` 뿐이라는 것이 소리를 저쪽에 두지 않은 이유이기도 하다.
**파티클은 그대로 나온다** — 보이는 것을 멈추면 추운 구간을 벗어난 것처럼 읽히는데,
지친 것과 추운 것은 다른 일이다.

두 종을 만든 것은 **2~3 초마다 나는 소리**라서다. 한 종이면 두 번째부터 티가 난다 —
발소리와 피격이 직전 것을 빼고 고르는 것과 같은 이유이고, 이제 세 곳이 같은
`PickIndex` 를 쓴다. 「quiet」 를 넣은 판이 피크 0.017 로 왔으므로 **호흡의 크기는
형용사가 아니라 마이크와의 거리로 지시한다.**

#### 「공기가 지나가는 길」 로 지시하면 가스가 샌다 (2026-08-10)

적 소리에서 통했던 방법 — 감정을 빼고 **소리 나는 기관**으로 지시하기 — 을 숨에
그대로 옮겼더니 **새는 밸브 소리가 왔다.** 사람이 듣고 「가스 세는 소리 같다」고
지적해 주었고, 재보니 근거가 분명했다.

| | 무게중심 | 4 kHz 위 | 포락선 |
| --- | ---: | ---: | --- |
| 버린 판 | 7061 Hz | **72.9 %** | 1.4 초 동안 평평 |
| 쓰는 판 | 3021 Hz | 2.4 % | 앞에서 봉우리, 1 초 안에 사라짐 |

**고역 잡음이 일정하게 이어지는 것이 누출의 정체다.** 숨은 두 가지가 다르다 —
봉우리가 있고(시작이 곧 최대), 목과 가슴의 몸통이 들린다(500 Hz 아래가 있다).
그래서 `hiss` · `rasp` · `airflow` · `release` 를 전부 버리고 **「사람이 숨을
내쉰다」** 로 지시하고 모양을 문장으로 줬다 — 「입이 열리는 순간 최대이고 1 초 안에
잦아든다」.

IMPORTANT: **판정 기준을 짐작하지 말고 이 폴더의 기존 사람 소리에서 가져온다.**
처음에 「4 kHz 위 25~55 %」 라고 적었는데 근거 없는 값이었다. `player-hurt-01/02` ·
`exert` · `death` · `jump` 를 재보면 **0.6~16.3 %** 이고 무게중심은 2116~4605 Hz 다.
그것이 이 게임의 사람 소리 대역이고, 버린 판만 72.9 % 로 혼자 튀어 있었다.

여기서도 **정체를 좁히는 부정은 통했다** — 「새는 밸브가 아니고, 증기가 아니고,
바람이 아니다」 를 넣은 판이 제대로 왔다. 대역을 빼라는 부정과 다르다는 것이
위 「부정문을 넣으면 소리가 사라진다」 항목의 요점이다.

### 파이프라인 스크립트는 대상을 좁혀서 돌린다

IMPORTANT: `trim_silence.py` · `normalize.py` 를 `--apply` 로 돌릴 때 **폴더 전체가
대상이면 남이 방금 다시 뽑아 아직 손대지 않은 파일까지 같이 처리된다.** 두 번
겪었다 — 콜로서스 소리 8 개의 잔향을 1.4~2.4 초씩 깎았고, 남의 `ice-melt-crack` 을
11.6 dB 올렸다. 둘 다 되돌렸다. 그래서 두 스크립트에 `--only` 가 있다.

```bash
python docs/sound/trim_silence.py --only avalanche-cannon --apply
python docs/sound/normalize.py --category Devices --only avalanche-cannon --apply
```

또 하나. **다시 뽑으면 같은 이름의 옛 `.wav` 가 남는다.** `gen_sfx.py --overwrite`
는 mp3 만 쓰므로, 트림이 새 wav 를 덮어쓰지 않는 경우(자를 것이 없을 때) 옛 판과 새
판이 한 폴더에 같은 이름으로 공존한다. 어느 것이 물릴지 알 수 없으므로 mp3 정리와
함께 반드시 확인한다.

## 5. 아직 없는 것

**2026-08-10 기준으로 소리는 대체로 다 난다.** 남은 것만 적는다.

- **`generator-stop` 을 부를 규칙.** 파일도 있고 4 장 `GeneratorDevice.deactivatedClip`
  에 걸어 두었지만, 발전기를 끄는 규칙이 기획에 아직 없어 실제로 울릴 일이 없다
- **건물 소리 여섯.** `Structure/` 의 `metal-stair-creak` · `window-wind-rattle` ·
  `pipe-clank` · `roof-snow-shift` · `door-handle-rattle` · `door-steel-close` 는
  2026-08-10 확인 결과 **아무 씬에도 없다.** 사건이 아니라 분위기라 코드가 부를
  자리가 없고, 「어느 계단이 삐걱대고 어느 창이 흔들리는가」는 레벨을 놓는 쪽이
  정할 일이다. 자리만 정해 주면 3D 소스로 붙이는 것은 금방이다
- **`valve-turn`.** 밸브를 돌리는 상호작용이 코드에 없다. 6 장 연료 밸브가 실제로
  돌리는 물건이 되면 그때 붙는다
- **오디오 믹서.** 지금은 `AudioSource.volume` 으로 맞추면 되지만, 설정 화면에
  볼륨 슬라이더가 들어가면 그때 `AudioMixer` 로 묶는 것이 맞다

발소리 재생은 플레이어 이동에 붙으므로 **A 이정범** 담당과 겹친다. 장치 · 상호작용
소리는 **C 임성규** 담당이다.

IMPORTANT: **2026-08-09 부터 A 의 파일에 B 가 들어간 자리가 있다.** 무기 · 주인공
소리를 붙이면서 `Player/` 아래를 고쳤다. 전부 **소리를 내는 쪽이 아니라 소리를 낼
시점을 알리는 쪽**만 건드렸다.

| 파일 | 무엇 |
| --- | --- |
| `PlayerWeapon.cs` | `DryFired` · `Impacted` 이벤트와 `ReloadSeconds`. 발사 · 재장전 규칙은 그대로다 |
| `PlayerFootsteps.cs` | `Landed` 이벤트 하나. 착지 판정을 두 벌 두지 않으려고 이미 있는 자리에서 알린다 |
| `Player.prefab` | `PlayerBodyAudio` 1개, `WeaponAudio` 2개를 붙이고 클립을 물렸다 |
| `Colossus.cs` (B) | 처형이 시작될 때 플레이어의 발버둥 소리를 부른다 |
