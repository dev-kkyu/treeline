> 상태: 작성 중 · 최종 수정 2026-08-08

# 사운드

`Assets/Game/Audio/` 에 63 개가 들어 있다. **전부 만들어져 있고 임포트 설정도
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

### `Footsteps/` — 표면별 발소리 12 종

**표면당 3 종이다.** 하나만 반복하면 기관총처럼 들려 걷는 것이 우스워진다.
번갈아 쓰는 법은 [3 장](#발소리는-직전-것을-빼고-고른다) 에 있다.

| 표면 | 파일 | 어디에 |
| --- | --- | --- |
| 콘크리트 | `footstep-concrete-01` ~ `-03` | 관제동 · 숙소동 · 갱도 |
| 금속 그레이팅 | `footstep-metal-grate-01` ~ `-03` | 갱도 통로 · 발전동 · 철탑 계단 |
| 눈 | `footstep-snow-01` ~ `-03` | 5 장 철탑, 6 장 승강장 |
| 나무 | `footstep-wood-01` ~ `-03` | 숙소동 바닥 |

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

### `Devices/` — 장치 12 종

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
| `SustainedDevice` | `snowblower-loop` | 제설기가 도는 동안 |
| | `searchlight-swivel-loop` | 서치라이트가 도는 동안 |

`TimedDevice` 는 `Started` 이벤트와 `Progress` 가 이미 있으므로 시작 · 완료
두 지점에 그대로 걸면 된다.

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
| `chase-stinger` | **큐.** 발각되는 순간 | 19s |
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

앰비언스와 음악만 2D 다. 나머지는 **소리가 나는 물건이 공간에 있으므로** 3D 로
두어야 방향과 거리가 들린다. 그래서 그것들은 임포트 단계에서 이미 모노다.

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

### 발소리는 직전 것을 빼고 고른다

3 종을 완전 무작위로 고르면 같은 것이 연달아 나오는 일이 생기고, 그게 제일
티가 난다. 직전 것만 빼면 해결된다.

```csharp
[SerializeField] private AudioClip[] clips;   // 표면당 3 종
private int last = -1;

private void PlayStep(AudioSource source)
{
    if (clips.Length == 0) return;
    int i = Random.Range(0, clips.Length - 1);
    if (i >= last) i++;                       // 직전 것을 건너뛴다
    last = i;
    source.pitch = Random.Range(0.94f, 1.06f);
    source.PlayOneShot(clips[i]);
}
```

`pitch` 를 조금씩 흔드는 것까지 해야 3 종으로도 반복감이 거의 사라진다.

### 음악은 낮게 깔고 큐에서 올린다

IMPORTANT: **이 게임의 공포는 소리에서 나오고 그 소리는 `Ambience/` 가 맡는다.**
음악을 크게 깔면 발소리와 천장에서 나는 소리가 묻혀 오히려 덜 무서워진다.

장별 트랙은 배경으로 낮게 두고, 올리는 것은 큐 두 개에서만 한다.

- `power-restored` — 4 장에서 조명이 켜지는 순간. 기획서가 이 장의 핵심으로
  꼽은 지점이고, 장 전체에 깔린 음악으로는 이 순간이 안 만들어진다
- `chase-stinger` — 발각되는 순간. 이것만 `Decompress On Load` + preload 로
  두었으므로 지연 없이 터진다

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
- **적 소리 · 무기 소리.** 대상이 확정되지 않아 지금 뽑으면 버리게 된다.
  이것들은 `Audio/` 가 아니라 담당 폴더로 간다
- **오디오 믹서.** 지금은 `AudioSource.volume` 으로 맞추면 되지만, 설정 화면에
  볼륨 슬라이더가 들어가면 그때 `AudioMixer` 로 묶는 것이 맞다

발소리 재생은 플레이어 이동에 붙으므로 **A 이정범** 담당과 겹친다. 장치 · 상호작용
소리는 **C 임성규** 담당이다. 파일만 있고 코드는 없는 상태라 지금은 충돌하지
않는다.
