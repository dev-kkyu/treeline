> 상태: 작성 중 · 최종 수정 2026-08-08

# 게임 시스템 구조

대부분은 아직 **세 사람이 동시에 붙기 위해 먼저 합의한 것**이다. 시스템 내부는
구현이 들어올 때부터 채우며, 2026-08-08 에 Player 가 첫 번째로 들어왔다.

무엇을 만드는지는 [`docs/game-design/treeline.md`](../game-design/treeline.md),
왜 이 순서인지는 [`decisions.md`](decisions.md), 누가 무엇을 맡는지는
[`docs/roles.md`](../roles.md) 에 있다.

## 여기에 담는 것 / 담지 않는 것

시스템 문서가 늘어나면 같은 시스템을 두 곳에서 설명하게 되고, 코드를 고칠 때
한쪽만 갱신되어 어긋난다. 경계를 이렇게 둔다.

| | 담는 것 |
| --- | --- |
| 이 문서 | 시스템 **목록과 책임**, 시스템 **간** 관계와 데이터 흐름, 새 컨텐츠 추가 절차 |
| `<시스템>.md` | 그 시스템 **내부** 동작과 확장 방법 |

**관계는 이 문서, 내부는 개별 문서.**

개별 문서는 미리 만들지 않는다. 이 문서를 훑을 때 특정 시스템 설명이 길어서
전체 지도가 안 보이면, 그때 `docs/tech-design/<시스템>.md` 로 떼어내고 여기에는
링크와 한 줄 요약만 남긴다.

IMPORTANT: 구조를 바꾸는 변경은 이 문서 갱신을 **같은 커밋**에 담는다.

---

## 씬은 한 사람만 만진다

IMPORTANT: 이 프로젝트에서 병렬 작업을 막는 유일한 구조적 장애물이다.

`.unity` 와 `.prefab` 은 두 사람이 동시에 고치면 병합할 수 없다. GUID 와 fileID 가
얽혀 있어 겉보기에 맞춰놔도 씬이 조용히 깨진다. 그래서 규칙은 하나다.

**한 씬에는 한 사람.** 남에게 줄 것은 씬이 아니라 **프리팹으로 준다.**

적을 배치해야 하면 적 담당이 프리팹을 만들어 넘기고, 레벨 담당이 자기 씬에
끌어다 놓는다. 이러면 두 사람이 같은 파일을 건드릴 일이 생기지 않는다.

각자 실험은 `Assets/Game/_Sandbox/<이름>.unity` 에서 한다. 남의 샌드박스를 열지
않는다.

## 씬 말고도 병합되지 않는 파일이 있다

`ProjectSettings/` 아래 몇 개는 **셋이 다 건드려야 하는데 병합이 안 된다.** 씬만
조심하면 된다고 생각하기 쉬운 자리다.

특히 레이어가 위험하다. 레이어는 씬과 프리팹에 이름이 아니라 **번호로** 저장된다
(`m_Layer: 8`). 각자 같은 번호에 다른 이름을 넣고 한쪽이 rebase 로 밀리면
오브젝트가 조용히 엉뚱한 레이어로 바뀌고, 레이캐스트가 어긋나기 시작한다.

그래서 2026-08-08 에 **쓸 레이어를 미리 다 잡고 담당별로 번호 구간을 나눴다.**

### 레이어

0~7 은 Unity 예약이라 쓸 수 없다.

| # | 이름 | 무엇 | 구간 |
| --- | --- | --- | --- |
| 8 | `Player` | 플레이어 콜라이더 | **A** |
| 9 | `ViewModel` | 1인칭 손 · 무기. 별도 카메라로 그려 벽에 안 박히게 한다 | **A** |
| 10~11 | | 예비 | **A** |
| 12 | `Enemy` | 적 본체 | **B** |
| 13~15 | | 예비 | **B** |
| 16 | `Interactable` | 문 · 레버 · 밸브 · 차단기 · 획득물 | **C** |
| 17 | `Carriable` | 양손 운반물. 연료통 · 축전지 | **C** |
| 18 | `Device` | 장치 본체. 셔터 · 히터 · 발전기 | **C** |
| 19 | `Trigger` | 스크립트 이벤트 · 체크포인트 볼륨. 안 보이고 안 막는다 | **C** |
| 20 | `Hazard` | 불 · 수소 폭발 · 제설기. 닿으면 피해 | **C** |
| 21~23 | | 예비 | **C** |
| 24~31 | | 예비 | 공용 |

IMPORTANT: **레이어를 추가할 땐 자기 구간 안에서만 쓴다.** 그래야 셋이 동시에
추가해도 번호가 겹치지 않는다. 구간이 모자라면 24 번대에서 가져오되 셋이
합의한다.

`Interactable` · `Carriable` · `Device` 를 나눈 이유는 물리 처리가 다르기
때문이다. 운반물은 들었을 때 플레이어와 안 부딪혀야 하고, 장치는 발동 대상이다.
마스크로 합칠 수는 있어도 나눈 것을 나중에 쪼갤 수는 없다.

코드에서 번호를 직접 쓰지 않는다. `Shared/GameLayers.cs` 를 쓴다. 그 파일과
`TagManager.asset` 은 **같은 커밋**에 담는다.

### 물리 충돌 매트릭스

레이어만 만들고 `DynamicsManager.asset` 을 안 만지면 전부 서로 부딪힌다.

| 끄는 조합 | 이유 |
| --- | --- |
| `Enemy` ↔ `Enemy` | 여럿 나올 때 서로 밀어내며 엉킨다 |
| `ViewModel` ↔ 전부 | 렌더 전용이라 물리가 없다 |
| `Trigger` ↔ `Player` 외 전부 | 트리거는 플레이어만 감지한다 |
| `Carriable` ↔ `Player` | 들고 걸을 때 자기 몸에 걸린다 |

### NavMesh 는 캐릭터를 빼고 굽는다

`NavMeshSurface` 의 `layerMask` 기본값은 **전부 포함**이다. 그대로 구우면 씬에 놓인
플레이어와 적 캡슐까지 걷는 면이 된다. 원점을 샘플하면 바닥(`y≈0`)이 아니라 캡슐
윗면(`y≈0.9`)이 잡히고, 경로 계산이 `PathInvalid` 로 조용히 실패한다. 에러도 경고도
안 뜬다.

굽기 전에 `Player` · `ViewModel` · `Enemy` · `Trigger` 를 뺀다.

```csharp
surface.layerMask = ~((1 << GameLayers.Player) | (1 << GameLayers.ViewModel)
                    | (1 << GameLayers.Enemy)  | (1 << GameLayers.Trigger));
```

IMPORTANT: **구운 데이터를 씬에 묻으면 씬 파일 전체가 바이너리가 된다.** Asset
Serialization 을 `Force Text` 로 두어도 그렇다. 커밋 기록이 제출물인데 씬 diff 가
통째로 사라지고, 그 씬은 병합도 불가능해진다. 실제로 2026-08-08 에 한 번 밟았고
씬을 이전 커밋에서 복원해야 했다.

`BuildNavMesh()` 뒤에 데이터를 별도 에셋으로 뗀다.

```csharp
if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
    AssetDatabase.CreateAsset(surface.navMeshData, "<씬폴더>/NavMesh/<이름>.asset");
```

에디터 UI 에서 `Bake` 버튼으로 구우면 Unity 가 알아서 이렇게 한다. **스크립트로
구울 때만 직접 해야 한다.**

C 가 `Ch1` · `Ch2` · `Ch6` 을 구울 때도 같다.

### 레이캐스트 마스크

레이어를 나눠도 각자 다른 마스크를 쓰면 의미가 없다. 세 개로 고정한다.

| 용도 | 마스크 | 쓰는 쪽 |
| --- | --- | --- |
| 상호작용 조준 | `GameLayers.Interaction` | A 가 쏘고 C 가 받는다 |
| 사격 | `GameLayers.Shot` | A |
| 적 시야 | `GameLayers.EnemySight` | B |

`Trigger` 와 `ViewModel` 은 **어느 마스크에도 들어가지 않는다.**

세 개는 **공용** 마스크다. 한 컴포넌트만 쓰는 마스크는 `GameLayers` 에 올리지
않고 그 자리에서 만든다. `Shared/` 변경은 셋의 합의가 필요한데, 쓰는 곳이 하나면
합의할 것이 없기 때문이다. 지금 그런 것이 하나 있다 — `PlayerMotor` 가 웅크린 뒤
일어설 수 있는지 볼 때 쓰는 천장 검사 마스크이며, `Player` · `ViewModel` ·
`Trigger` 를 뺀 나머지다. 두 번째 담당이 같은 것을 필요로 하면 그때 올린다.

URP 의 Rendering Layers 는 이것과 별개다. 손전등이 특정 물체만 비추게 하려면 그때
따로 잡는다. 지금은 쓰지 않는다.

### 태그는 늘리지 않는다

Unity 빌트인 `Player` 태그로 충분하다. 적은 `FindWithTag("Player")` 로 플레이어를
잡는다. 태그를 늘리면 문자열 오타가 컴파일을 통과해 런타임까지 간다.

### 씬 목록

`EditorBuildSettings.asset` 도 셋이 다 건드린다. 씬을 추가하면 여기 등록해야
한다. 샌드박스 씬은 빌드에 넣지 않는다.

2026-08-08 에 아래 넷을 등록했다.

| # | 씬 |
| --- | --- |
| 0 | `Game/Levels/Title.unity` |
| 1 | `Game/Levels/Ch1.unity` |
| 2 | `Game/Levels/Ch2.unity` |
| 3 | `Game/Levels/Ch6.unity` |
| 4 | `Game/Levels/Ending.unity` |

`Title` 이 맨 앞이어야 게임이 거기서 시작한다. 순서가 바뀌면 빌드가 엉뚱한 씬으로
열린다. `Title` 과 `Ending` 은 `Chapters.Order` 에 넣지 않는다 — 장이 아니라서
`GameFlow` 가 플레이어를 찾거나 배치하려 들면 안 된다.

`Ending` 은 마지막 장을 빠져나온 뒤 가는 곳이다. 없으면 `Ch6` 의 출구가 갈 데가
없어 관통이 끝에서 끊긴다. 지금은 검은 화면뿐이고 UI 담당이 채운다.

IMPORTANT: 씬은 **인덱스가 아니라 이름으로** 로드한다 (`LoadScene("Ch1")`).
인덱스로 하면 목록 순서가 바뀔 때마다 엉뚱한 씬이 열린다. 위 번호는 등록 결과일
뿐이고 코드는 이 번호를 쓰지 않는다.

## 폴더 구조

```
Assets/Game/
├── _Sandbox/         각자 실험용 씬. 소유자 외 접근 금지
├── Levels/           장별 씬. Ch1 · Ch2 · Ch6 (그 뒤 Ch3 · Ch4 · Ch5)
├── Player/           이동 · 카메라 · 손전등 · 체력 · 무기
├── Enemy/            적 · NavMesh · 상태 머신
├── Interaction/      문 · 레버 · 밸브 · 획득 · 운반
├── Devices/          장치. 즉시 발동 / 시간 발동 / 지속 상태 세 패턴
├── Progress/         씬 전환 · 체크포인트 · 사망 · 재시작
├── UI/               타이틀 · 설정 · 엔딩 · HUD · 로딩
└── Shared/           인터페이스 3종과 `GameLayers`. 변경은 셋의 합의가 필요하다
```

`Assets/ThirdParty/` 는 제3자 에셋 원본 자리이고 저장소에서 제외된다.
정책은 `CLAUDE.md` 참고.

### AI 로 만든 에셋은 담당 폴더에 둔다

기획은 적 3종 · 소품 · 사운드를 AI 로 생성한다. **이것은 제3자 에셋이 아니라 우리
것이므로 `ThirdParty/` 가 아니라 담당 폴더에 넣고 커밋한다.** 커밋하지 않으면 셋이
각자 다른 모델을 갖게 되고, 같은 프리팹이 사람마다 다른 것을 가리킨다.

```
Assets/Game/Enemy/
├── Crawler.prefab
├── Crawler.cs
├── Models/           Crawler.fbx
└── Textures/         Crawler_BaseMap.png
```

- **만든 사람의 담당 폴더**에 둔다. 적 모델은 `Enemy/`, 무기 · 손전등은 `Player/`,
  레버 · 계기판은 `Interaction/` 또는 `Devices/` 다
- 파일 이름은 **쓰는 프리팹과 같게** 한다. `Crawler.prefab` ↔ `Crawler.fbx`
- 남이 쓰는 것은 자유다. 폴더 규칙은 **같은 파일을 둘이 고치는 것**을 막으려는
  것이지 참조를 막으려는 것이 아니다

**용량은 신경 쓰지 않고 원본 그대로 넣는다.** 저장소 용량과 빌드 용량은 별개다.
Unity 는 임포트 설정에 따라 텍스처를 압축하고 메시를 최적화해서 굽기 때문에, 4K
원본을 넣어도 빌드에는 설정한 크기로 들어간다. 원본을 미리 줄이면 나중에 더
키우고 싶을 때 되돌릴 수 없다.

빌드가 무거워지면 **원본이 아니라 임포트 설정을 만진다** — 텍스처의 `Max Size` 와
압축 포맷이다. WebGL 이라 빌드 용량이 곧 심사자의 첫 로딩 시간이므로, 배포 전에
한 번 보는 것으로 충분하다.

사운드는 담당이 정해지지 않았다. 정해지면 같은 규칙을 따른다.

## 서로 붙는 지점

**이 세 개만 고정하면 서로의 구현을 몰라도 각자 진행된다.** 최소한으로 잡았고,
바꾸려면 셋이 합의한다.

| 인터페이스 | 만드는 쪽 | 쓰는 쪽 |
| --- | --- | --- |
| `IInteractable` | 상호작용 담당 | 플레이어가 바라보는 대상에 호출 |
| `IDamageable` | 공용 | 플레이어와 적이 서로에게 호출 |
| `IDeviceTrigger` | 장치 담당 | 레버 · 스크립트 이벤트가 장치를 발동 |

`IDamageable` 을 양쪽이 함께 쓰는 이유는, 총이 적을 죽이는 것과 적이 플레이어를
때리는 것이 같은 모양이기 때문이다. 하나로 두면 A 와 B 가 서로를 기다리지 않는다.

## 남에게 넘기는 프리팹

씬 대신 프리팹으로 주고받으므로 **경로와 이름을 미리 박아둔다.** 나중에 이름을
바꾸면 이미 참조를 건 쪽이 전부 깨진다.

| 프리팹 | 만드는 쪽 | 쓰는 쪽 | 언제 |
| --- | --- | --- | --- |
| `Player/Player.prefab` | A | B 는 추격 대상으로, C 는 레벨에 배치 | 1단계 |
| `Enemy/Crawler.prefab` | B | C | 1단계 |
| `Enemy/Colossus.prefab` | B | 6장 담당 | 2단계 |
| `Enemy/Clinger.prefab` | B | 4 · 5장 담당 | 3단계 |

기획의 「기는 것 · 큰 것 · 매달린 것」이 각각 `Crawler` · `Colossus` · `Clinger`
다. 프리팹 이름은 영문 PascalCase 로 쓰고, 폴더는 만드는 쪽의 담당 폴더다.

IMPORTANT: **A 는 `Player.prefab` 을 완성 전에 먼저 올린다.** 캡슐에
`CharacterController` 와 `Player` 태그만 붙은 껍데기여도 된다. B 의 적은 추격할
대상이 있어야 하고 C 의 관통 확인에는 Player 와 Enemy 가 둘 다 필요하다. 완성을
기다리면 두 사람이 자기 샌드박스에서 아무것도 검증하지 못한다.

---

## 새 컨텐츠를 추가하려면

### 새 장(레벨)

1. `Assets/Game/Levels/Ch<번호>.unity` 를 만든다. **그 씬의 소유자는 만든 사람**이다
2. 필요한 것은 다른 담당이 만든 **프리팹을 배치**한다. 로직을 씬에 직접 쓰지 않는다
3. 장 진입 · 이탈은 `Progress/` 의 씬 전환을 쓴다
4. `Chapters.Order` 에 넣고 빌드 씬 목록에도 등록한다. 둘 중 하나만 하면 조용히
   어긋난다

씬에 무엇을 두는지는 아래 「Progress」 절의 표를 따른다.

IMPORTANT: `ChapterExit` 의 `BoxCollider` 는 **`size` 를 직접 확인한다.** MCP 로
컴포넌트를 붙이면서 준 `size` 가 반영되지 않고 `(1,1,1)` 로 남는 일이 있었다.
1m 큐브라 통로를 못 막고, 플레이어가 옆으로 지나가면 장이 안 넘어간다. 겉으로는
멀쩡해 보여서 알아채기 어렵다.

### 새 적

1. `Assets/Game/Enemy/` 에 프리팹으로 만든다
2. `IDamageable` 을 구현한다. 그래야 총에 맞는다
3. 레벨에는 **프리팹만 넘긴다.** 남의 씬에 직접 배치하지 않는다

### 새 장치

1. 세 패턴 중 하나를 고른다 — 즉시 발동 / 시간 발동 / 지속 상태
2. 기존 컴포넌트를 **파라미터만 바꿔** 쓴다. 새 스크립트를 만들기 전에 세 패턴으로
   안 되는 이유를 먼저 확인한다
3. `IDeviceTrigger` 로 발동을 받는다

### 새 상호작용 대상

`IInteractable` 을 구현하면 플레이어 쪽 수정 없이 붙는다.

---

## 시스템 목록

기획의 「만들 시스템」과 같다. 하나씩 들어올 때마다 책임과 관계를 여기에 적는다.

| 시스템 | 책임 | 상태 |
| --- | --- | --- |
| Player | 1인칭 이동 · 카메라 · 손전등 · 체력 · 무기 | 1단계 구현됨 |
| Enemy | 추격 · 공격 · 피격 · 사망. NavMesh 기반 | `Crawler` · `Colossus` 회색 박스 |
| Interaction | 바라보는 대상과의 상호작용, 획득, 양손 운반 | 1단계 구현됨 |
| Devices | 장치 발동. 세 패턴으로 수렴 | 미구현 |
| Progress | 씬 전환 · 체크포인트 · 사망 · 재시작 | 1단계 구현됨 |
| UI | 타이틀 · 설정 · 엔딩 · 최소 HUD · WebGL 로딩 | 미구현 |

### Player

`Assets/Game/Player/`. 컴포넌트를 하나씩 나눠 `Player.prefab` 루트에 모두 붙였다.
하나가 커지면 그때 쪼갠다.

| 컴포넌트 | 책임 |
| --- | --- |
| `PlayerInputReader` | 입력을 읽는 **유일한** 곳. 나머지는 여기만 참조한다 |
| `PlayerMotor` | 걷기 · 달리기 · 웅크리기 · 점프. 중력과 콜라이더 높이 |
| `PlayerLook` | 좌우는 몸체, 상하는 카메라. 포인터 잠금도 여기 |
| `PlayerHealth` | 체력 3칸. `IDamageable` 구현 |
| `WeaponSwitcher` | 어느 총을 드는가. 양손 운반 중 총 잠금 |
| `PlayerWeapon` | 총 한 자루. `GameLayers.Shot` 레이캐스트, 탄약, 반동 |
| `PlayerInteractor` | `GameLayers.Interaction` 조준, `IInteractable` 호출 |
| `Flashlight` | 조명 토글과 배터리 소모 |
| `PlayerHud` | 체력 3칸 · 탄약 · 조준선. 값을 폴링하지 않고 이벤트로 받는다 |

프리팹 구조는 이렇다.

```
Player                  태그 Player, 레이어 Player(8)
├── Body                회색 박스 캡슐
├── Camera              본 카메라. Player 와 ViewModel 을 그리지 않는다
│   ├── ViewModelCamera URP 오버레이. ViewModel(9) 만, 깊이를 지우고 겹쳐 그린다
│   ├── Flashlight      Spot Light
│   ├── Pistol          PlayerWeapon + Model
│   └── Shotgun         PlayerWeapon + Model
└── HUD                 Screen Space Overlay 캔버스
```

`PlayerWeapon` 만 루트가 아니라 `Camera` 밑에 **총마다 하나씩** 붙는다
(`Camera/Pistol` · `Camera/Shotgun`). 권총과 산탄총은 클래스가 아니라 **값만
다르다** — 산탄총은 탄자 수를 늘리고 퍼짐을 벌린 것이다. 장치를 세 패턴으로
수렴시킨 것과 같은 기준이고, 세 번째 총이 생겨도 값만 채우면 된다.

전환은 `WeaponSwitcher` 가 `PlayerWeapon` 컴포넌트를 켜고 끄는 것으로 한다. 꺼진
무기는 `Update` 가 돌지 않아 입력을 보지 않으므로, 총마다 "내가 장착됐나" 를
확인하는 분기가 없다. 사망 처리가 입력 컴포넌트 하나만 끄는 것과 같은 방식이다.

반동은 무기가 직접 카메라를 돌리지 않고 `PlayerLook.AddRecoil` 로 값만 넘긴다.
카메라 회전을 만지는 곳이 둘이 되면 매 프레임 서로 덮어쓴다.

**1인칭 총 모델은 별도 카메라로 그린다.** `ViewModelCamera` 는 URP 오버레이라
본 카메라 위에 깊이를 지우고 겹쳐 그린다. 총이 벽에 파고들지 않고, 시야각을 따로
줄 수 있어(본 카메라 70도 / 뷰모델 50도) 총이 덜 왜곡된다. 그래서 본 카메라의
컬링 마스크에서 `ViewModel`(9) 을 뺐다 — **둘 다 그리면 총이 두 번 그려진다.**

총 모델은 `PlayerWeapon` 이 자기 `OnEnable`/`OnDisable` 에서 켜고 끈다. 장착
상태와 화면의 총이 어긋날 자리를 만들지 않으려는 것이다.

`PlayerHud` 는 `HealthChanged` · `AmmoChanged` · `Equipped` 를 구독한다. 무기를
바꾸면 이전 무기의 구독을 끊는다 — 안 끊으면 안 든 총의 탄약이 화면에 남는다.
초기값은 `OnEnable` 이 아니라 `Start` 에서 읽는다. `OnEnable` 은 `PlayerHealth.Awake`
보다 먼저 돌 수 있어 체력을 0 으로 읽는다.

`TestDamageTarget` 은 A 의 사격 검증용 표적 대역이고 **적이 아니다.** 적은 B 가
`Enemy/` 에 만든다. `Crawler.prefab` 이 왔으므로 사격 감각을 맞추는 용도로만 남기고,
필요 없어지면 지운다.

입력을 한 곳으로 모은 이유는 **사망 처리 때문**이다. `PlayerHealth` 가 죽을 때
`PlayerInputReader` 하나만 끄면 액션 맵이 꺼지고, 나머지 컴포넌트는 전부 0 을
읽는다. 각자 "죽었는지" 를 확인하는 분기를 두지 않아도 된다.

IMPORTANT: 입력 액션은 프리팹에 참조를 끼우지 않고 **프로젝트 전역 에셋**
(`InputSystem.actions`) 을 쓴다. 프리팹마다 끼우면 하나 빠뜨렸을 때 그 프리팹만
조용히 입력을 못 받는다.

### Enemy

`Assets/Game/Enemy/`. 1단계는 `Crawler` 하나다. 회색 박스이며 외형을 전혀 모른다 —
모델 파이프라인이 늦어져도 1단계가 멈추지 않도록 나중에 자식으로 끼운다.

**Enemy 는 Player 의 코드를 하나도 참조하지 않는다.** 접점이 셋뿐이고 전부 공용
계약이라, A 가 무엇을 어떻게 만들든 B 는 영향을 받지 않는다.

| 접점 | 무엇 |
| --- | --- |
| `Player` 태그 | 추격 대상을 `FindWithTag` 로 찾는다. 매 프레임이 아니라 놓쳤을 때만 다시 찾으므로 늦게 스폰돼도 붙는다 |
| `IDamageable` | 적이 플레이어를 때리는 것과 총이 적을 죽이는 것이 같은 호출이다 |
| `GameLayers.EnemySight` | 시야 레이캐스트. 맨 앞에 맞은 것이 플레이어여야 본 것으로 친다 |

플레이어 참조가 없어도 예외 없이 대기하다 생기면 붙는다. `Player.prefab` 이 오기
전에도 태그만 붙인 캡슐로 검증할 수 있게 한 것이다.

등 뒤에서 맞으면 추격을 시작한다. 안 그러면 뒤에서 쏘는 것이 무위험 행동이 되어
기획의 「빠르지만 약하다」가 성립하지 않는다.

#### `Colossus` — 죽지 않는다

`Crawler` 와 코드를 공유하지 않는다. **규칙이 반대**이기 때문이다. `IsAlive` 가
언제나 참이고 `Death` 클립도 만들지 않는다. 체력을 아주 크게 주는 방식을 쓰지
않은 이유는, 큰 값은 언젠가 깎이기 때문이다. 탄약을 다 쓰면 죽는 적이 되는 순간
「죽지 않는 것이 있다」가 1장부터 6장까지 유지되지 않는다.

총이 만드는 유일한 효과는 **밀려남**이다. 누적 피해가 임계에 닿으면 뒤로 2.5m
밀려나 잠시 멈춘다. 밀려나 있는 동안 맞은 것은 다음 밀림에 쌓지 않는다 — 안 그러면
탄약만 있으면 영구히 묶어둘 수 있어 「도망친다」가 성립하지 않는다.

IMPORTANT: **총폭 3.44m 다. 이 적이 지나갈 통로는 4m 이상이어야 한다.** 6장을
만드는 A 가 레벨을 잡기 전에 알아야 하는 수치다. 좁으면 벽에 끼어 안 온다.

IMPORTANT: 크기를 **Transform 스케일로 만들지 않는다.** 스케일을 쓰면
`NavMeshAgent` 반경과 물리가 어긋난다. 실측 크기로 만든다.

아직 안 정한 것 — `Colossus` 의 에이전트 반경은 1.3m 인데 지금 레벨의 NavMesh 는
기본 에이전트(반경 0.5m) 기준으로 구워져 있다. 큰 적이 벽에 더 가까이 붙어 걷고
좁은 곳에서 낀다. 제대로 하려면 **Agent Type 을 하나 더 만들어 두 번 구워야**
하는데, Agent Type 은 `NavMeshAreas.asset` 에 저장되어 레이어처럼 셋이 함께
건드리는 파일이다. 회색 박스 단계에서는 통로를 넉넉히 잡는 것으로 넘기고,
6장 레벨이 잡힐 때 셋이 정한다.

### Interaction

`Assets/Game/Interaction/`. 1단계는 `Lever` 하나다. `IInteractable` 의 첫
구현체이고, 6장 삭도 기동이 이것이다.

당기면 지정한 오브젝트를 켠다. 6장에서는 `ChapterExit` 를 켜므로 **레버를 당기기
전에는 장을 나갈 수 없다.** 장치 시스템(`IDeviceTrigger`)이 들어오면 그쪽을
발동시키는 것으로 바꾼다. 지금 미리 추상화하지 않는 이유는 없는 시스템에 맞추면
상상으로 만들게 되기 때문이다.

IMPORTANT: **상호작용 대상은 눈높이에 둔다.** A 의 조준 광선은 카메라에서 나가고
1인칭 눈높이는 바닥에서 `y≈1.73` 이다. 허리 높이에 두면 광선이 위로 지나가 아무리
가까이 가도 안 잡힌다. 콜라이더 세로 범위가 그 높이를 지나야 한다.

IMPORTANT: **동선 위에 두지 않는다.** 상호작용 대상은 보통 막는 콜라이더라
통로 한가운데 두면 플레이어가 거기서 걸린다. 6장 레버를 출구 통로(`x -3~3`)
바깥인 `x=6` 에 둔 것이 그 이유다.

### Progress

| 파일 | 하는 일 |
| --- | --- |
| `Chapters` | 장 씬 이름과 진행 순서. **장을 추가할 때 고칠 곳은 여기 하나다** |
| `GameFlow` | 장 전환 · 재시작 · 체크포인트 보관 · 죽음 구독. 게임에 하나 |
| `SpawnPoint` | 장이 시작될 때 플레이어가 서는 자리 |
| `Checkpoint` | 지나가면 다시 시작할 자리가 바뀐다 |
| `ChapterExit` | 장의 끝. 들어오면 다음 장으로 |

`GameFlow` 는 **씬에 배치하지 않는다.** 첫 접근 때 스스로 생기고 씬이 바뀌어도
살아남는다. 씬에 두면 세 장 모두에 같은 오브젝트를 넣어야 하고, 고칠 일이 생길
때마다 병합할 수 없는 파일을 세 개씩 건드리게 된다.

다음 장이 어디인지는 `Chapters.Order` 만 안다. `ChapterExit` 에 다음 장 이름을
적지 않는 이유는, 적어두면 3 · 4 · 5 를 끼워 넣을 때 씬을 열어 고쳐야 하기
때문이다.

## 시스템 간 관계

지금 존재하는 관계는 이것뿐이다. 시스템이 들어올 때마다 여기에 더한다.

```
PlayerWeapon ──레이캐스트(Shot)──▶ IDamageable   (적: B)
PlayerInteractor ──레이캐스트(Interaction)──▶ IInteractable   (문·레버: C)
Enemy ──▶ IDamageable   (PlayerHealth: A)
PlayerHealth ──Died 이벤트──▶ 재시작   (Progress: C, 아직 구독자 없음)
```

**Player 는 Enemy 와 Interaction 의 타입을 하나도 모른다.** 전부
`Shared/` 의 인터페이스를 통해서만 닿는다. 그래서 A 는 B 와 C 의 구현을 기다리지
않고, 반대로 B 와 C 도 A 를 기다리지 않는다.

바깥에서 Player 를 부르는 자리는 아래 셋이다. 다른 곳에서 부를 일이 생기면 여기에
적고 나서 부른다.

| 부르는 쪽 | 무엇 |
| --- | --- |
| 적 (B) | `PlayerHealth.TakeDamage` |
| 획득물 (C) | `WeaponSwitcher.AddAmmo(종류, 발수)` · `Flashlight.AddBattery` |
| 총 획득 (C) | `WeaponSwitcher.Equip(종류)` — 권총 3장, 산탄총 5장 |
| 양손 운반 (C) | `WeaponSwitcher.SetCarrying(true/false)` |
| 진행 (C) | `PlayerHealth.Died` 구독, 장 전환 시 `PlayerHealth.Restore` |

IMPORTANT: 「무거운 것을 들면 총을 못 쓴다」 는 **운반 쪽이 총을 끄는 것이 아니라**
`SetCarrying` 을 부르는 것으로 한다. 내려놓으면 들고 있던 총으로 알아서 돌아오므로
운반 쪽이 무엇을 들고 있었는지 기억하지 않아도 된다.

**의존은 한 방향이다.** `Progress` 가 `Player` 를 찾아가고, `Player` 는
`Progress` 를 모른다. 그래야 A 가 자기 작업만으로 완결된다.

```
Progress ──찾는다──▶ "Player" 태그        (GameObject.FindWithTag)
Progress ──구독──▶ PlayerHealth.Died      (죽었을 때 재시작)
Progress ──옮긴다──▶ CharacterController  (스폰 · 체크포인트로 순간이동)
```

`GameFlow` 는 장이 로드될 때마다 그 씬의 `PlayerHealth` 를 찾아 `Died` 를 다시
건다. 씬이 바뀌면 `PlayerHealth` 도 새로 생기기 때문이다.

IMPORTANT: **첫 씬에는 `sceneLoaded` 가 오지 않는다.** 씬이 먼저 로드되고 그 뒤에
`GameFlow` 가 생기기 때문이다. 그래서 `RuntimeInitializeOnLoadMethod` 로 시작
시점에 한 번 더 붙인다. 이것이 없으면 **빌드의 첫 장에서 죽어도 재시작이 걸리지
않는다.** 씬 전환으로 들어온 장에서는 멀쩡해서, 관통 테스트만으로는 안 잡힌다.

한동안은 매 프레임 `IDamageable.IsAlive` 를 보는 방식이었다. A 의 구현을 기다리지
않으려고 쓴 임시 방편이었고, `Died` 가 생기면서 걷어냈다. 그 폴링을 담고 있던
`PlayerDeathWatcher` 와 씬마다 두던 `[Progress]` 오브젝트도 함께 사라졌다.

IMPORTANT: `CharacterController` 가 붙은 오브젝트는 `transform` 만 옮기면 다음
프레임에 되돌아온다. 컨트롤러를 껐다 옮기고 다시 켜야 한다. `GameFlow.Teleport`
가 그렇게 한다.

### 장 씬에 두는 것

`Ch1` · `Ch2` · `Ch6` 이 이 구성이다.

| 오브젝트 | 무엇 |
| --- | --- |
| `Player` | A 의 `Player/Player.prefab` 을 배치. 카메라와 체력을 들고 온다 |
| `SpawnPoint` | 장이 시작될 때 서는 자리 |
| `ChapterExit` | 장의 끝. 트리거 콜라이더, 레이어 `Trigger` |
| `Directional Light` | |
| 바닥 · 벽 | 회색 박스 |

진행용 오브젝트를 따로 두지 않는다. `GameFlow` 는 스스로 생기고 죽음 구독도
자기가 한다.
