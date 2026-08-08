> 상태: 30종 임포트 · 1 · 2장 완료 · 6장 대기 · 최종 수정 2026-08-09

# 소품 목록과 Meshy 프롬프트

무엇을 만들지 정한 목록과, Meshy 에 그대로 넣을 영문 프롬프트다. 조달 절차는
[`/prop` 스킬](../../.claude/skills/prop/SKILL.md), 방침은
[`world-assets.md`](world-assets.md) 에 있다.

**맵은 단조롭게 두고 그 위를 꾸민다.** 눈을 붙잡는 일은 여기 적힌 소품이 한다.

## 정해진 것 — 레벨은 큐브, 소품은 Meshy

**벽 · 바닥 · 천장 · 계단 · 난간은 소품이 아니라 레벨이다.** 세 장 모두 **스케일
큐브로 짓고 표면만 입힌다**(2-1b). 그 외의 배치물은 전부 Meshy 로 교체한다.

IMPORTANT: 벽을 Meshy 모델로 바꾸는 것은 R&D 가 배제했다. 생성물은 치수를 안 지켜 4m
칸에 안 맞고, 이어 붙이면 이음매가 보이고, 메시 콜라이더가 되어 NavMesh 가 나빠지며,
폴리곤이 감당이 안 된다.

### 2장 복도를 큐브로 다시 지었다 (2026-08-09)

2장 복도만 Blender FBX 였다. 처음에는 「UV 가 제대로 있으니 표면만 입히자」로 적었는데,
**아무도 형태를 못 고친다**는 것이 결정적이었다 — 소스 스크립트가 저장소에 없었다.
죽어 있던 `CorridorSection_Base` 가 치수까지 맞는 큐브 모듈이라 9 칸을 갈아끼웠다.

| | 전 | 후 |
| --- | --- | --- |
| 콜라이더 | `MeshCollider` 1 개 | `BoxCollider` 14 개 |
| **배관 · 조명** | **벽에 구워짐** | **개별 오브젝트 — 교체 가능** |
| 바닥 윗면 | y 0.06 (방 · 문간과 6cm 턱) | y 0.00 균일 |

**이 문서의 배관 · 조명 항목이 그래서 유효해졌다.** 구워져 있을 때는 복도에서 따로
바꿀 수가 없었다.

`Ch2_CorridorSection.fbx` 와 래퍼 `Ch2_Section.prefab`, 죽어 있던
`CorridorSection_Bunks` · `_Lockers`, 쓸 곳이 없어진 머티리얼 여덟은 **지웠다.**
FBX 는 git 이력에 남아 필요하면 되살릴 수 있다.

## 조사해서 확인한 것 (2026-08-09)

**에셋스토어 에셋은 없다.** `ThirdParty/` 폴더가 없고, 2장 FBX 의 메타데이터가 전부
`Blender (stable FBX IO) 5.2.0 LTS` 이며, `assets.md` 의 외부 에셋은 폰트 하나뿐이다.
**라이선스 문제는 없다.**

2장 소품은 **B 가 Blender FBX 로 만들어 둔 것**이 여덟이다. 전부 교체 대상이다 —
레버 · 히터 · 배전함은 **다른 장에서도 쓰는 공용**이라, 2장 전용으로 두면 6장에 같은
물건이 또 생긴다.

| 2장에 있는 FBX | 이 문서의 항목 |
| --- | --- |
| `Ch2_LockerBank` · `Ch2_Bunk` | 2번 사물함 · 3번 이층침대 |
| `Ch2_ShutterLever` | **7번 벽 레버** (공용 — 1 · 6장과 같은 물건) |
| `Ch2_Breaker` | **8번 배전함** (공용 — 4장 배전반으로 재등장) |
| `Ch2_FrozenDoor` · `Ch2_DoorIce` | 9번 철제 문 (얼음은 제외 — VFX 로 간다) |
| `Ch2_Heater` | **16번 히터 유닛** (공용 — 6장과 같은 크기) |
| `Ch2_Shutter` | 17번 비상 셔터 |

---

## 한 세계로 보이게 하는 공통 규칙

프롬프트마다 아래를 넣는다. 빠뜨리면 생성물마다 톤이 튀어 **한 건물로 안 보인다.**

```
Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
occlusion. No branding, no text, no decals. Worn matte grey-green industrial
paint over steel, bare metal showing at the edges, light rust at seams.
Neutral desaturated palette only.
```

| 넣는 말 | 왜 |
| --- | --- |
| `low-poly, game-ready, clean topology` | 폴리곤은 사후 감소가 안 된다 |
| `no baked shadows, no baked ambient occlusion` | 알베도에 그림자가 구워지면 손전등과 싸운다 |
| `no branding, no text, no decals` | 글자가 들어가면 시대 · 국적이 특정된다. 기획이 「현대 · 장소 불명」으로 못 박았다 |
| `worn matte grey-green ... desaturated` | 색을 못 박지 않으면 생성물마다 튄다 |

색이 튀는 것은 **소화기와 비상등 둘뿐**이다. 어둠 속에서 그 둘만 눈에 걸리게 한다.

IMPORTANT: **Meshy 는 치수를 안 지킨다.** 프롬프트의 길이는 비율을 잡는 용도이고 실제
크기는 임포트에서 맞춘다. 그래서 항목마다 **치수 · 축 · 피벗**을 적어 둔다 — 그 숫자가
없으면 후처리에서 맞출 기준이 사라진다.

---

## 부품이 나뉘어야 하는 것은 통짜로 뽑고 쪼갠다

Meshy 는 프롬프트에 뭐라고 쓰든 **메시 하나 · 머티리얼 하나 · 텍스처 아틀라스 하나**로
내보낸다. `as separate objects` 같은 말은 안 먹는다.

**부품별로 따로 생성하지 않는다.** 손잡이만 따로 뽑으면 몸체와 페인트 톤 · 러스트 양 ·
마모 위치가 안 맞고, 생성물마다 2048 텍스처 3장이 딸려 와 물건 하나가 20MB 가 된다.
통짜로 한 번 뽑아 **임포트에서 쪼개면 UV 가 그대로라 두 조각이 같은 아틀라스를 쓴다** —
색이 어긋날 수가 없고 텍스처도 한 벌이다.

쪼개는 방식은 둘이고, 어느 쪽이 되는지는 뽑혀 온 모양이 정한다.

| 방식 | 조건 | 하는 일 |
| --- | --- | --- |
| **덩어리 분리** | 부품 사이에 틈이 있어 메시가 안 붙어 있다 | 삼각형 연결 성분을 찾아 나눈다. 깨끗하다 |
| **평면 절단** | 경첩 · 허브에서 붙어 있다 | 자를 높이를 정해 삼각형을 양쪽으로 배분한다. 단면은 안 막는다 |

평면 절단의 뚫린 단면이 안 보이는 이유는 **자르는 자리가 경첩 안쪽 · 허브 안쪽**이기
때문이다. 그 자리가 밖에서 보이는 부품이면 애초에 나눌 수 없다.

둘 다 Unity 안에서 처리한다. Blender 를 안 거친다.

IMPORTANT: 그래서 **덩어리 분리가 되도록 프롬프트에서 틈을 만들어 달라고 못 박는다.**
아래 항목들의 영문 블록에 그 문장이 이미 들어가 있다. 빼면 붙은 채로 나와 절단으로
떨어진다. 절단으로 갈 셋은 대신 **자르기 좋은 자세**를 못 박았다. 경첩으로 열리는 문은
어느 쪽이 될지 뽑아 봐야 안다 — 분리를 먼저 시도하고 붙어 있으면 자른다.

---

## 임포트 결과 (2026-08-09)

**30 종이 `Assets/Game/Props/<이름>/` 에 프리팹으로 들어갔다.** 각 폴더에 메시
`.asset`, 512 텍스처 두 장, 머티리얼, 프리팹이 있다. 폴더 전체가 40 MB 다.

폴리곤은 합격이다 — 2,600~19,800 삼각형. **Meshy 가 low-poly 는 지켰다.**

크기는 「목표 높이에 맞추고 비율은 Meshy 것을 따른다」로 넣었다. 그래서 폭이 계획과
다른 것이 있다 — 사물함 2.16 m (계획 0.90), 셔터 3.53 m (2.70), 세면장 칸막이
3.46 m (2.40). **높이가 맞으면 사람은 크기를 맞다고 읽는다.** 폭은 씬에 놓을 때
간격으로 흡수하거나 프리팹 루트 스케일로 조정한다.

세 개는 방향을 돌려 구웠다. **근무표 보드 90°**(면이 `-X` 를 보게), **배관 90°**
(길이가 `+Z` 가 되게), **서치라이트 180°**(빛이 `+Z` 로), 그리고 **벽 레버 180°** —
레버는 판이 앞, 손잡이가 뒤로 나와서 그대로면 벽에 붙일 수 없었다.

### 2장은 다 갈아끼웠다 (2026-08-09)

Blender 로 만들었던 `Ch2_*.fbx` 일곱과 안 쓰게 된 머티리얼 열셋을 지웠다. 남은
Blender 자산은 **`Ch2_DoorIce.fbx` 하나**뿐이다 — 얼음 여섯 띠는 `IceMelt` 가
높이별로 줄이는 것이라 소품으로 대체할 대상이 아니다.

바꾼 것은 이렇다.

| 어디 | 무엇 | 몇 개 |
| --- | --- | --- |
| `_Props` | 사물함 · 이층침대 | 14 |
| `_Gameplay` | 얼음문 · 히터 · 배전함 · 셔터 · 셔터 레버 | 5 |
| 복도 프리팹 | 조명 하우징 · 배관 | 6 프리팹 |
| 방 프리팹 | 이층침대 · 매트리스 · 사물함 · 책상 · 의자 · 선반 · 나무상자 · 드럼통 · 세면대 · 칸막이 | 18 묶음 |

IMPORTANT: **기존 렌더러 바운드에 맞춰 넣었다.** 소품 고유 비율보다 이쪽이
우선인 이유는 레벨이 그 크기로 설계됐고 NavMesh 도 그것으로 구워져 있어서다.
그래서 사물함 깊이처럼 눌린 축이 있다. 큐브 여러 개로 만들었던 것(이층침대 3,
책상 3, 선반 5, 세면대 3)은 합쳐서 하나로 넣었다.

셔터 레버만 구조를 바꿨다. `Lever` 는 자기 트랜스폼을 돌리므로 **판은
`ShutterLeverBase` 로 떼어 벽에 붙이고, 손잡이만 `ShutterLever` 아래**에 뒀다.
회전축을 손잡이 밑동에 맞췄고 조작 콜라이더는 세계 y 1.00~1.90 을 그대로 지켰다 —
눈높이 1.65 가 그 안에 있어야 잡힌다.

### 1장은 처음부터 다시 지었다 (2026-08-09)

옛 1장은 회색 박스로 관통만 시켜 둔 것이라 치수 · 동선 · 배치를 전부 새로 잡았다.
자세한 판단은 [`decisions.md`](decisions.md) 에 있고, 여기는 어떤 소품이 어디
쓰였는지만 적는다.

| 어디 | 무엇 |
| --- | --- |
| 현관 | 벤트그릴 · 소화기 · 나무상자 · 비상등 |
| 관제실 | 관제 콘솔 4 · 사무 의자 3 · 책상 · 커피잔 2 · 무전 일지 · 근무표 · 사물함 3 · 선반 3 · 드럼통 3 · 나무상자 3 · 산업 기계 · 손전등 · 조명 하우징 4 · 배관 5 · 케이블 트레이 |
| 뒷복도 | 조명 하우징 2 · 배관 · 소화기 |
| 창고 | 선반 2 · 나무상자 3 · 드럼통 2 · 밸브 휠 · 산업 기계 · 조명 하우징 |
| 계단실 | 조명 하우징 · 배관 |
| 옥상 | 서치라이트 · 벽 레버 · 산업 기계 2 · 드럼통 2 · 나무상자 · 벤트그릴 · 비상등 · 배관 3 |

**서치라이트만 앞이 `-Z` 다.** 구울 때 180° 를 먹여 두었기 때문이고, 그래서 북쪽을
비추게 하려면 다시 180° 를 준다. 벽 레버도 같은 이유로 `-Z` 다. 나머지 스물여덟은
`+Z` 다.

### 못 채운 것

- **배관 엘보와 케이블 트레이 코너가 없다.** 5-1 로 온 것은 엘보가 아니라 짧고
  뭉툭한 다발이라 버렸다. 5-2 가 진짜 직선 배관이고 그것을 `PipeRun` 으로 쓴다
- **배관과 트레이는 4 m 모듈이 아니다.** 배관은 0.79 m, 트레이는 0.40 m 로 나왔다.
  늘리면 텍스처가 뭉개지므로 **이어 붙여 채운다**
- **배전함 텍스처에 글자가 구워져 있다.** 프롬프트의 `no branding, no text` 가 안
  먹었다. 어두운 데 놓이는 작은 물건이라 일단 두고, 눈에 걸리면 다시 뽑는다

---

## 뽑을 때 정할 것

내보내기 설정은 되돌릴 수 없다. 뽑기 전에 맞춘다.

| 무엇 | 무엇으로 | 왜 |
| --- | --- | --- |
| 파일 형식 | **FBX** | Unity 가 기본으로 임포트한다. GLB 는 `glTFast` 같은 제3자 패키지가 필요하고, OBJ 는 계층 · 노멀 · 머티리얼 바인딩이 부실하다 |
| 텍스처 | PNG, **base color 와 normal 만** | metallic · roughness 는 버린다. 전부 무광 도장 강철이라 파라미터로 일괄해 덮는 편이 톤이 안 튀고, 소품당 텍스처가 절반이 된다 |
| 크기 | 2048 로 나와도 그대로 | 임포트에서 512 로 줄인다 |
| 폴리곤 | 낮게 | 사후 감소가 안 된다. 사물함처럼 오래 보이는 것도 몇천 tris 면 충분하다 |
| 리메시 | **약하게** | 세게 걸면 위에서 만든 틈이 메워진다 |

IMPORTANT: **리메시와 틈은 서로를 지운다.** 부품 분리가 걸린 아홉은 리메시를 약하게
두거나 원본 메시도 함께 받아 둔다. 틈이 살아 있는 쪽을 골라 쓸 수 있다.

---

## 순서 — 화면에 오래 있는 것부터

| 순위 | 무엇 | 개수 |
| --- | --- | --- |
| **0** | 1인칭 소지품 | 1 |
| **1** | 2장 복도에 반복 배치 | 5 |
| **2** | 세 장 공용 | 8 |
| **3** | 조작하는 장치 | 7 |
| **4** | 장 전용 분위기 | 9 |

**전부 30 개.** 자를 거면 4순위부터 자른다. 0 ~ 2 순위 열넷만 해도 세 장이 다 달라
보인다.

---

# 0순위 — 1인칭

### 1. 손전등 `Flashlight`

    Rugged industrial handheld flashlight, cylindrical aluminium body with
    three knurled grip rings, wider ribbed head, flat glass lens, side push
    switch, lanyard lug at the tail. Worn matte grey-green paint with bare
    metal showing at the edges, light rust at seams. Low-poly, game-ready,
    clean topology. No baked shadows, no baked ambient occlusion. No branding,
    no text, no decals. Neutral desaturated palette only.

- 치수: 길이 0.22m · 지름 0.05m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 손잡이 중앙
- 바꿀 곳: `Player.prefab` 의 `Flashlight/Model`, `Ch1` 의 `Pickup_Flashlight`
- 부품 분리: 불필요
- 비고: **이 게임에서 화면에 가장 오래 있는 물건이다.** 1 · 2 · 6 을 관통하는 동안
  손에 들리는 것은 이것 하나뿐이라 여기에 가장 공을 들인다. `World/Meshes/Flashlight.asset`
  의 R&D 대역을 대체한다

---

# 1순위 — 2장 복도에 반복 배치

**2장이 가장 긴 장이고 복도가 반복된다.** 여기 다섯이 2장 화면의 대부분이다.

### 2. 금속 사물함 `Locker`

    Bank of three tall steel personnel lockers standing side by side, each
    with a vented slot panel near the top, a recessed handle with a hasp, a
    piano hinge down one side, a shared plinth at the bottom. The leftmost
    door stands wide open on its hinges, a clear gap all around it. Worn matte
    grey-green paint over steel, bare metal at the edges, light rust at seams.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 0.90 × 1.80 × 0.50m (3연장 기준)
- 축: 문이 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch2` 의 `_Props/Locker_0`~`_7` · `Locker_L0`~`_L2` (11 개, `Ch2_LockerBank.fbx`),
  `HideRoom` 의 `Locker_Standing` · `Locker_Fallen`
- 부품 분리: **열린 문 한 짝을 나눈다.** 덩어리 분리를 먼저 보고, 경첩에서 붙어 있으면
  거기를 자른다
- 비고: 복도에 11 개가 늘어선다. **2장에서 화면에 가장 오래 있는 소품이다**

### 3. 이층침대 `Bunk`

    Steel two-tier bunk bed frame, tubular uprights and angle rails, flat
    slatted deck on each level, a fixed ladder at one end, low guard rail on
    the upper bunk. Worn matte grey-green paint over steel, bare metal at the
    edges, light rust at the welds. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. No branding, no text,
    no decals. Neutral desaturated palette only.

- 치수: 0.90 × 1.75 × 2.00m
- 축: 머리맡 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch2` 의 `_Props/Bunk_0`~`_2` (`Ch2_Bunk.fbx`),
  `HideRoom` 의 `Bunk_Frame` · `Bunk_Lower` · `Bunk_Upper`
- 부품 분리: 불필요. 매트리스는 26 번으로 따로 얹는다

### 4. 조명 하우징 `LightHousing`

    Industrial fluorescent light fixture housing, long rectangular steel body
    with a wire cage guard over the front, two end caps, mounting brackets on
    top. The tube inside is dark and unlit, sitting in the housing with a
    visible air gap all around it, not fused to the reflector. Worn matte
    grey-green paint over steel, bare metal at the edges, light rust at seams.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 길이 1.2m · 폭 0.16m · 높이 0.12m
- 축: 길이 `+X` · 피벗: 천장에 닿는 윗면 중앙
- 바꿀 곳: 복도 · 방 프리팹 전부의 `LightHousing` · `LightTube`
- 부품 분리: **튜브를 나눈다** (덩어리 분리). 깜빡임을 머티리얼로 조절해야 한다
- 비고: 모든 칸에 하나씩 들어간다. **꺼진 채로 있는 것이 기본**이고 켜지는 것은 4장이다

### 5. 배관 다발 `PipeRun` — 직선 · 엘보

    Bundle of four parallel industrial pipes of slightly different diameters
    running side by side, held by two U-bolt brackets, flanged joints at both
    ends, straight run. Worn matte grey-green paint over steel, bare metal at
    the edges, light rust at seams. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. No branding, no text,
    no decals. Neutral desaturated palette only.

엘보는 `straight run` 을 아래로 바꿔 한 번 더 뽑는다.

    ... ninety degree elbow bend, flanged joints at both ends.

- 치수: 직선 길이 4m · 단면 0.35 × 0.35m
- 축: 길이 `+Z` · 피벗: **한쪽 끝 단면 중심** (이어 붙이려면 끝이 원점이어야 한다)
- 바꿀 곳: 복도 · 문간 프리팹의 `Pipe_A` · `Pipe_B`
- 부품 분리: 불필요
- 비고: **4m 모듈이다.** Meshy 가 길이를 안 지키므로 임포트에서 **길이 축만** 늘려
  맞춘다. 단면이 정사각형에 가까워야 늘려도 안 뭉개진다

### 6. 케이블 트레이 `CableTray` — 직선 · 코너

    Open steel cable tray, ladder type with evenly spaced rungs, carrying a
    loose bundle of thick insulated cables secured with three zip ties,
    straight run. Worn matte grey-green paint over steel, bare metal at the
    edges, light rust at seams. Low-poly, game-ready, clean topology. No baked
    shadows, no baked ambient occlusion. No branding, no text, no decals.
    Neutral desaturated palette only.

- 치수: 직선 길이 4m · 폭 0.3m · 높이 0.12m
- 축: 길이 `+Z` · 피벗: 한쪽 끝
- 바꿀 곳: `Ch2` 의 `CableRun_Up` · `CableRun_Down`, 그 밖의 벽 · 천장
- 부품 분리: 불필요
- 비고: 4m 모듈. 위와 같다

---

# 2순위 — 세 장 공용

**한 번 만들어 1 · 2 · 6 에 흩뿌린다.** 단조로움을 가장 크게 푸는 자리다.

### 7. 벽 레버 `WallLever`

    Heavy industrial wall-mounted lever switch, cast steel base plate bolted
    to the wall, a thick pivoting handle with a rounded grip end, a curved
    slotted guide plate behind the handle, small hinge boss. The handle stands
    straight up, well clear of the guide plate. Worn matte grey-green paint
    over steel, bare metal worn shiny on the grip. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding, no
    text, no decals. Neutral desaturated palette only.

- 치수: 판 0.35 × 0.50m · 손잡이 길이 0.55m
- 축: 앞 `-Z`, 손잡이가 `+Y` 로 선다 · 피벗: **손잡이 회전축**
- 바꿀 곳: `Ch1/SearchlightSwitch`, `Ch2/ShutterLever`, `Ch6/HeaterLever` · `BoardLever`
- 부품 분리: **손잡이를 나눈다.** 평면 절단 — 경첩 보스에서 자른다. `Lever` 가 55°
  눕히므로 나뉘어야 한다
- 비고: **손잡이가 선 자세로 나와야 한다.** 눕은 채로 뽑히면 `Lever` 가 거기서 또 눕혀
  두 번 눕는다
- 비고: 넷의 씬 크기가 제각각이다(`0.35×1.6×0.25` ~ `0.24×0.6×0.45`). **하나를 만들어
  스케일로 맞춘다** — 같은 물건이 반복되는 편이 세계에 일관성을 준다

### 8. 벽면 배전함 `JunctionBox`

    Wall-mounted industrial electrical junction box, rectangular steel
    enclosure with a hinged front door standing half open, two heavy latches,
    a small round indicator lamp, conduit stubs entering from the bottom. Worn
    matte grey-green paint over steel, bare metal at the edges, light rust at
    seams. Low-poly, game-ready, clean topology. No baked shadows, no baked
    ambient occlusion. No branding, no text, no decals. Neutral desaturated
    palette only.

- 치수: 0.30 × 0.90 × 0.62m (`Ch2` 의 `Breaker` 실측)
- 축: 문이 `-Z` · 피벗: 벽에 닿는 뒷면 중앙
- 바꿀 곳: `Ch2` 의 `_Gameplay/Breaker` (`Ch2_Breaker.fbx`). **`Lever` 가 붙어 있는
  오브젝트다** — 메시만 갈고 컴포넌트는 그대로 둔다
- 부품 분리: **문을 나눈다.** 덩어리 분리를 먼저 보고, 붙어 있으면 경첩에서 자른다
- 비고: 2장 히터 배전함이 이것이고, 4장 배전반으로 다시 나온다

### 9. 철제 문 · 문틀 `SteelDoor`

    Heavy industrial steel door in its frame, flush slab with a row of narrow
    horizontal louvre slits near the top, a lever handle with a round
    escutcheon, three barrel hinges down one side, a raised threshold plate at
    the bottom, the frame slightly proud of the wall. The door slab hangs
    slightly ajar in its frame with a clear gap along all four edges. Worn
    matte grey-green paint over steel, bare metal at the edges, light rust at
    the hinges. Low-poly, game-ready, clean topology. No baked shadows, no
    baked ambient occlusion. No branding, no text, no decals. Neutral
    desaturated palette only.

- 치수: 문짝 2.40 × 2.30 × 0.18m (`Ch2` 실측) · 문틀 포함 폭 2.7m
- 축: 앞 `-Z` · 피벗: **경첩 쪽 세로축**
- 바꿀 곳: `Ch2` 의 `_Gameplay/FrozenDoor/Model` (`Ch2_FrozenDoor.fbx`),
  `CorridorSection_Doorway` · `_DoorwayLeft` 의 `DoorJamb_*`
- 부품 분리: **문짝 · 문틀 둘** (덩어리 분리). 문짝만 돌아야 한다. **손잡이는 문짝에
  붙여 둔다** — 따로 돌리는 연출이 계획에 없어 절단을 한 번 아낀다
- 비고: **얼음은 모델에 넣지 않는다.** `Ch2_DoorIce.fbx` 로 여섯 띠가 따로 있고
  `IceMelt` 가 아래부터 녹인다. 그쪽은 건드리지 않는다

  IMPORTANT: `FrozenDoor` 에는 `IceMelt` · `NavMeshObstacle(carving)` · `BoxCollider`
  가 붙어 있다. **메시(`Model` 자식)만 갈고 나머지는 그대로 둔다.** 그리고 이 물건은
  **NavMesh 를 구울 때 꺼야 한다** — 안 그러면 구멍이 영구히 남아 녹아도 길이 안
  열린다. 근거는 [`architecture.md`](architecture.md) 의 「런타임에 막는 것은 굽기에서
  뺀다」

### 10. 환기 벤트 그릴 `VentGrille`

    Industrial wall vent grille, rectangular steel frame with fixed horizontal
    louvre blades, four corner screws, slightly recessed into its frame. Worn
    matte grey-green paint over steel, bare metal at the edges, light rust at
    seams. Low-poly, game-ready, clean topology. No baked shadows, no baked
    ambient occlusion. No branding, no text, no decals. Neutral desaturated
    palette only.

- 치수: 0.60 × 0.40 × 0.08m
- 축: 앞 `-Z` · 피벗: 벽에 닿는 뒷면 중앙
- 바꿀 곳: 없음 — **새로 놓는다.** 벽이 비어 보이는 자리에 붙인다
- 부품 분리: 불필요
- 비고: 4장 환기 팬의 예고이기도 하다

### 11. 드럼통 `Drum`

    Standard 200 litre steel industrial drum, two rolling ribs around the
    body, raised rim at top and bottom, sealed lid with a small bung. Worn
    matte grey-green paint over steel, bare metal at the edges, heavier rust
    streaks running down from the top rim. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding,
    no text, no decals. Neutral desaturated palette only.

- 치수: 지름 0.58m · 높이 0.88m
- 축: 위 `+Y` · 피벗: 바닥면 중앙
- 바꿀 곳: `Room_Store` 의 `Drum`
- 부품 분리: 불필요
- 비고: **6장 엄폐물 옆에 섞어 쌓는다.** 같은 기계 덩어리가 다섯이면 반복이 눈에
  띄는데, 드럼과 상자를 섞으면 실루엣이 흐려진다

### 12. 앵글 선반 `Shelf`

    Industrial slotted-angle steel shelving unit, four uprights and three flat
    shelf boards, open sides and back, diagonal cross brace at the rear. Worn
    matte grey-green paint over steel, bare metal at the edges, light rust at
    seams. Low-poly, game-ready, clean topology. No baked shadows, no baked
    ambient occlusion. No branding, no text, no decals. Neutral desaturated
    palette only.

- 치수: 2.00 × 1.55 × 0.80m (`Ch1` 실측)
- 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch1` 의 `Shelf`, `Room_Store` 의 `Shelf_*` · `ShelfPost_*` · `Shelf_Fallen`
- 부품 분리: 불필요

### 13. 소화기 `Extinguisher`

    Wall-mounted fire extinguisher on a simple steel bracket, cylindrical
    bottle with a rounded top, squeeze handle, short hose looped to the side,
    pressure gauge on the neck. Dull red bottle, everything else worn matte
    grey-green steel with bare metal at the edges. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding,
    no text, no decals. Neutral desaturated palette otherwise.

- 치수: 높이 0.55m · 지름 0.16m
- 축: 앞 `-Z` · 피벗: 벽에 닿는 뒷면
- 바꿀 곳: 없음 — 새로 놓는다
- 부품 분리: 불필요
- 비고: **색이 튀는 둘 중 하나다.** 어둠 속에서 눈이 걸린다

### 14. 비상등 `EmergencyLight`

    Wall-mounted industrial emergency light, a small steel bracket carrying
    two round lamp heads side by side behind wire guards, a slim battery box
    behind them, conduit entering from below. Dull red lamp lenses, everything
    else worn matte grey-green steel with bare metal at the edges. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals. Neutral desaturated palette otherwise.

- 치수: 0.32 × 0.18 × 0.14m
- 축: 앞 `-Z` · 피벗: 벽에 닿는 뒷면 중앙
- 바꿀 곳: `Ch2` 의 `EmergencyLight`
- 부품 분리: 불필요. 렌즈만 머티리얼로 켠다
- 비고: 색이 튀는 둘 중 둘째. 2장 정전 구간의 유일한 빛이다

---

# 3순위 — 조작하는 장치

플레이어가 다가가 들여다보는 것들이라 **가까이서 버텨야 한다.**

### 15. 서치라이트 `Searchlight`

    Large industrial searchlight on a short mast, cylindrical drum housing
    with a wide glass lens at the front, a ribbed cooling collar, side yoke
    mount allowing tilt, a cable gland at the rear, mounted on a square base
    plate. The lamp head is aimed straight forward and level. Worn matte
    grey-green paint over steel, bare metal at the edges, light rust at seams.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 마스트 0.40 × 1.40 × 0.40m · 헤드 1.00 × 0.80 × 1.00m (`Ch1` 실측)
- 축: 빛이 `+Z` · 피벗: 마스트 바닥면 중앙
- 바꿀 곳: `Ch1` 의 `SearchlightMast` · `SearchlightHead`
- 부품 분리: **헤드를 나눈다.** 평면 절단 — 요크 축에서 자른다. 돌아가는 연출이 붙을 수
  있다
- 비고: 5장에서 같은 물건이 생존 수단이 된다

### 16. 히터 유닛 `HeaterUnit`

    Industrial cable heater control unit, a squat steel box with a finned heat
    exchanger on one face, a thick armoured cable leaving the bottom through a
    gland, two small dials and a round indicator lamp on the front, carry
    handles on the sides. Worn matte grey-green paint over steel, bare metal
    at the edges, light rust at seams. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. No branding, no text,
    no decals. Neutral desaturated palette only.

- 치수: 1.00 × 1.00 × 1.00m (`Ch2` · `Ch6` 실측, 둘 다 같다)
- 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch2/Heater` · `HeaterCable` · `HeaterGlow`, `Ch6/Heater`
- 부품 분리: 불필요. 표시등만 머티리얼로 켠다
- 비고: **2장에서 배우고 6장에서 쫓기며 다시 쓴다.** 기획의 「각 장치는 두 번 쓰인다」가
  모델 재사용으로 그대로 떨어진다

### 17. 비상 셔터 `Shutter`

    Industrial roll-down emergency shutter, horizontal slat curtain with a
    reinforced bottom rail, vertical guide channels at both sides, a roller
    housing box across the top. The slat curtain hangs free inside the side
    guides without touching them, the roller housing a separate box above.
    Worn matte grey-green paint over steel, bare metal at the edges, light
    rust running down the slats. Low-poly, game-ready, clean topology. No baked
    shadows, no baked ambient occlusion. No branding, no text, no decals.
    Neutral desaturated palette only.

- 치수: 2.70 × 2.80 × 0.14m (`Ch2` 실측)
- 축: 앞 `-Z`, 내려오는 방향 `-Y` · 피벗: **커튼 윗변**
- 바꿀 곳: `Ch2` 의 `Shutter`
- 부품 분리: **커튼 · 가이드 · 롤러함 셋** (덩어리 분리). 커튼만 내려와야 한다
- 비고: 2장에서 살아남는 수단. 3장 이후 같은 물건이 무기가 된다

### 18. 삭도 곤돌라 `CableCar`

    Small aerial tramway gondola cabin for two people, riveted steel shell
    with rounded corners, a large window on each side, a sliding door on one
    side standing half open with a clear gap between door and body, a curved
    hanger arm on the roof ending in a grip clamp for the haul rope. Worn matte
    grey-green paint over steel, bare metal at the edges, light rust at seams.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 1.60 × 2.00 × 2.40m (`Ch6` 의 `Body` 실측)
- 축: 진행 방향 `+Z` · 피벗: **행어 그립 중심** (케이블에 매달린다)
- 바꿀 곳: `Ch6` 의 `Body` · `Car` · `CableCar`
- 부품 분리: **문을 나눈다** (덩어리 분리). 미닫이라 경첩이 없어 깨끗하게 떨어진다
- 비고: 올라타면 화면이 뒤집힌다. **안이 보이므로 내부도 필요하다**

### 19. 관제 콘솔 `ControlConsole`

    Industrial control console desk, angled instrument panel with rows of
    round analogue gauges, toggle switches and indicator lamps, a flat writing
    surface below, boxed steel body with a kick plate at the floor, cable
    conduit leaving the back. Worn matte grey-green paint over steel, bare
    metal at the edges, light rust at seams. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding,
    no text, no decals. Neutral desaturated palette only.

- 치수: 4.00 × 1.00 × 1.20m (`Ch1` 실측)
- 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch1` 의 `Console`
- 부품 분리: 불필요. 계기 바늘은 지금 안 움직여도 된다
- 비고: 기획의 「계기판이 돌아가고」가 이것이다

### 20. 산업 기계 덩어리 `MachineBlock`

    Large industrial machinery housing, a boxy steel cabinet with a ribbed
    side panel, a raised motor cowling on top, heavy lifting eyes at the
    corners, a bolted access hatch on the front, thick conduit leaving the
    base. Worn matte grey-green paint over steel, bare metal at the edges,
    light rust at seams. Low-poly, game-ready, clean topology. No baked
    shadows, no baked ambient occlusion. No branding, no text, no decals.
    Neutral desaturated palette only.

- 치수: 3.00 × 3.00 × 3.00m
- 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch6` 의 `Cover_N` · `Cover_E` · `Cover_S` · `Cover_W` · `Cover_SE`,
  `Ch6` 의 `WatchStand`
- 부품 분리: 불필요
- 비고: **6장 화면의 절반을 차지한다.** 다섯이 같은 모델이면 반복이 보이므로 회전 ·
  스케일을 달리 주고 드럼통 · 나무 상자를 섞는다

### 21. 밸브 휠 `ValveWheel`

    Industrial gate valve with a spoked hand wheel, five spokes and a raised
    hub, rising stem above the bonnet, flanged valve body below, bolted
    joints. The hand wheel sits high on the stem, its spokes well clear of the
    valve body. Worn matte grey-green paint over steel, bare metal worn shiny on
    the wheel rim, light rust at seams. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. No branding, no text,
    no decals. Neutral desaturated palette only.

- 치수: 휠 지름 0.36m · 전체 높이 0.45m
- 축: 휠 축이 `+Z` · 피벗: 배관에 닿는 밑면
- 바꿀 곳: 없음 — 배관에 붙는 장식으로 새로 놓는다
- 부품 분리: **휠을 나눈다.** 평면 절단 — 스템 중간에서 자른다. 3 · 4장에서 돌리는
  연출이 붙는다

---

# 4순위 — 장 전용 분위기

한 번 지나가는 것들이다. 자를 거면 여기부터.

### 22. 사무 책상 `Desk`

    Plain steel office desk, flat top with a thin lip, a drawer pedestal on
    one side, open knee space, angle-braced legs. Worn matte grey-green paint
    over steel, bare metal at the edges, light rust at seams. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 1.40 × 0.74 × 0.70m · 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `HideRoom` 의 `Desk` · `DeskLeg_A` · `DeskLeg_B`
- 부품 분리: 불필요

### 23. 사무 의자 `Chair`

    Simple industrial swivel chair, round padded seat and a small padded
    backrest, single central column, five-star base with castors. Worn matte
    grey-green frame, dark grey padding, bare metal at the edges. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 0.60 × 0.95 × 0.60m · 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `HideRoom` 의 `Chair_Toppled`, `Ch1` 관제실에 새로 놓기
- 부품 분리: 불필요
- 비고: **넘어진 것을 따로 뽑지 않는다.** 회전으로 눕힌다. 기획의 「의자가 넘어져
  있고」가 이것이다

### 24. 근무표 보드 `NoticeBoard`

    Wall-mounted notice board, a shallow steel frame with a flat backing
    panel, several loose sheets of paper pinned in a grid, one sheet curling
    at a corner, a small clip at the top. Worn matte grey-green frame, pale
    off-white paper, bare metal at the edges. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No printed words,
    no readable text, no branding, no decals. Neutral desaturated palette only.

- 치수: 0.10 × 1.00 × 1.40m (`Ch1` 실측) · 축: 앞 `-X` · 피벗: 벽에 닿는 뒷면 중앙
- 바꿀 곳: `Ch1` 의 `DutyRoster`
- 부품 분리: 불필요
- 비고: **모델에 글자를 넣지 않는다.** 읽는 내용은 `Readable` 패널이 띄운다

### 25. 무전 일지 `Logbook`

    Open hardcover logbook lying flat, ruled pages with faint horizontal
    lines, a pen resting in the gutter, cloth-bound spine, slightly curled
    page corners. Dull dark green cover, pale off-white pages. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No printed words, no readable text, no branding, no decals. Neutral
    desaturated palette only.

- 치수: 0.70 × 0.06 × 0.50m (`Ch1` 실측) · 축: 위 `+Y` · 피벗: 바닥면 중앙
- 바꿀 곳: `Ch1` 의 `RadioLog`
- 부품 분리: 불필요
- 비고: **콜라이더는 지금 눈높이까지 키워 트리거로 두었다. 메시를 갈아도 그대로 둔다**
  (근거는 `architecture.md` 의 「Readable」)

### 26. 침대 매트리스 `Mattress`

    Thin worn foam mattress lying flat, quilted surface with a seam piping
    around the edge, one corner sagging, a few dark damp patches. Dull grey
    ticking fabric, no pattern. Low-poly, game-ready, clean topology. No baked
    shadows, no baked ambient occlusion. No branding, no text, no decals.
    Neutral desaturated palette only.

- 치수: 0.80 × 0.14 × 1.90m · 축: 위 `+Y` · 피벗: 바닥면 중앙
- 바꿀 곳: `HideRoom` 의 `Mattress` · `Mattress_Dragged`
- 부품 분리: 불필요

### 27. 세면대 `Basin`

    Wall-hung industrial wash basin, plain rectangular ceramic bowl with a
    rolled rim, exposed chrome trap and supply pipes below, a single lever tap
    at the back. Dull off-white ceramic, dull steel pipework, light rust at
    the wall brackets. Low-poly, game-ready, clean topology. No baked shadows,
    no baked ambient occlusion. No branding, no text, no decals. Neutral
    desaturated palette only.

- 치수: 0.50 × 0.85 × 0.40m · 축: 앞 `-Z` · 피벗: 벽에 닿는 뒷면
- 바꿀 곳: `Room_Wash` 의 `Basin_*` · `BasinPipe_*` · `Tap_*`
- 부품 분리: 불필요 (수도꼭지 포함 통짜)

### 28. 세면장 칸막이 `WashPartition`

    Row of three shoulder-height washroom partition panels on slim steel
    posts, plain flat panels with a rounded top edge, a long narrow mirror
    strip fixed to the wall above, small shelf lip under the mirror. Worn
    matte grey-green panels, dull steel posts, bare metal at the edges.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals. Neutral desaturated palette only.

- 치수: 2.40 × 1.70 × 0.10m · 축: 앞 `-Z` · 피벗: 바닥면 중앙
- 바꿀 곳: `Room_Wash` 의 `Partition` · `MirrorStrip`
- 부품 분리: 불필요

### 29. 나무 상자 `Crate`

    Rough wooden shipping crate, plank sides with visible nail heads, corner
    battens, a lid with a slight gap at one edge. Weathered pale grey timber,
    dull steel corner brackets, no pattern. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding,
    no text, no decals. Neutral desaturated palette only.

- 치수: 0.80 × 0.60 × 0.80m · 축: 위 `+Y` · 피벗: 바닥면 중앙
- 바꿀 곳: `Room_Store` 의 `Crate_A` · `Crate_B` · `Crate_C`
- 부품 분리: 불필요
- 비고: 6장 엄폐물 옆에 쌓아 실루엣을 흐리는 데도 쓴다

### 30. 커피잔 `Mug`

    Simple ceramic mug with a plain round handle, slightly thick rim, a little
    dark liquid left inside, a faint ring stain on the outside near the base.
    Dull off-white ceramic. Low-poly, game-ready, clean topology. No baked
    shadows, no baked ambient occlusion. No branding, no text, no decals.
    Neutral desaturated palette only.

- 치수: 0.09 × 0.10 × 0.12m · 축: 위 `+Y` · 피벗: 바닥면 중앙
- 바꿀 곳: 없음 — **새로 놓는다**
- 부품 분리: 불필요
- 비고: 기획의 「커피가 아직 따뜻한데 사람이 없다」가 이것이다

---

## 후순위 — 그 장이 생긴 뒤에 만든다

레벨이 없으면 크기도 배치도 정할 수 없다.

| 소품 | 어느 장 | 왜 미루나 |
| --- | --- | --- |
| 권총 (슬라이드 · 탄창 분리) | 3장 | **지금 빌드에서 화면에 안 나온다** |
| 산탄총 (펌프 분리) | 5장 | 같음 |
| 연료통 | 3장 · 6장 | 이 단계의 6장에 연료 절차가 없다 |
| 배터리 · 탄약 상자 | 3장~ | 지금 배치된 곳이 없다 |
| 갱도 지지 프레임 · 손수레 | 3장 | 레벨 없음 |
| 디젤 발전기 · 배전반 · 축전지 랙 · 환기 팬 | 4장 | 레벨 없음 |
| 유기물 데코 (둥지) | 4장 | 레벨 없음 |
| 철탑 구조재 · 안테나 접시 · 로터리 제설기 · 제설포 | 5장 | 레벨 없음 |

## 세지 않은 것

- **적 3종** — [`enemy-sheets/`](../enemy-sheets/) 의 시트 방식으로 따로 간다
- **벽 · 바닥 · 천장 · 계단 · 난간** — 레벨이다. 표면만 입힌다 (2-1b)
- **걸레받이 · 배튼 · 레일** (`Skirting_*` · `Seam_*`) — 모서리를 살리려고 얇은 상자를
  덧댄 벽 마감이다. 소품이 아니다
- **얼음 · 서리 · 고드름** (`Icicle_*` · `Frost_*` · `CableIce` · `FrozenPuddle`) —
  Meshy 로 뽑기 나쁜 형태다. 반투명 셰이더와 파티클로 가고, VFX 키트가 이미 있다
- **UI · 아이콘** — 이미 들어와 있다

## 2장을 손볼 때 순서

1. **표면부터.** 2-1b 를 2장에 적용한다. 지오메트리를 안 건드려 되돌리기 쉽고,
   **이것만으로 1장과 톤이 맞는지 먼저 본다**
2. 그다음 소품을 교체한다. 위 표의 「바꿀 곳」을 따라 **메시만** 갈아끼운다
3. **배치는 지금 구성을 그대로 둔다.** 관통이 검증된 레벨이다

IMPORTANT: **1 번을 하고 2 번을 판단한다.** 표면을 안 입힌 채 소품만 바꾸면 새 모델이
단색 벽 위에 떠 보인다.
