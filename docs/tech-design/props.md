> 상태: 30종 임포트 · 1 · 2 · 6장 완료 · **배경 나무 3종 임포트 · 타이틀 · 엔딩
> 교체 완료** · 삭도 곤돌라 재생성 대기 · **4장 전용 5종 치수 확정** ·
> 최종 수정 2026-08-09

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

IMPORTANT: **Meshy 프롬프트는 800자까지다** (2026-08-09 확인). 위 30종은 500자 안팎이라
걸리지 않지만, 재질까지 지시하면 금방 넘는다. 넘칠 때는 **형태 묘사부터 깎고 금지
조항은 그대로 둔다.** 형태가 마음에 안 들면 다시 뽑으면 되지만, 알베도에 구워진 그림자 ·
불투명하게 칠해진 유리 · 높은 폴리곤은 **임포트 뒤에 고칠 방법이 없다.**

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

IMPORTANT: **앞이 `+Z` 가 아닌 것이 셋 있다.** 벽 레버 · 서치라이트는 구울 때 180° 를
먹여서고, **밸브 휠은 모델 자체가 그렇다.** 근무표는 90° 를 먹여 앞이 `+X` 다.
나머지 스물여섯은 `+Z` 다.

| 소품 | 앞 | 왜 |
| --- | --- | --- |
| 벽 레버 · 서치라이트 | `-Z` | 구울 때 180° 를 먹였다 |
| 근무표 | `+X` | 구울 때 90° 를 먹였다 |
| 나머지 27 종 | `+Z` | |

IMPORTANT: **아래 항목별 설명의 「축: 앞 `-Z`」 는 뽑을 때의 주문이지 결과가
아니다.** 실제로 온 것은 이 표다. 항목 설명만 읽고 배치하면 **전부 180° 뒤집힌다** —
2026-08-09 에 4장에서 29 개를 그렇게 놓았다. 배치할 때는 이 표를 본다.

그리고 **벽에 붙는 것은 피벗이 뒷면이 아니라 앞면**이다(배전함 · 벤트그릴 · 소화기 ·
비상등 · 세면대). 항목 설명의 「피벗: 벽에 닿는 뒷면 중앙」도 주문 쪽 문장이다.
몸통이 피벗의 `-Z` 쪽에 통째로 붙어 있으므로, 벽에 세우려면 **벽면에서 두께만큼
방 안쪽으로 밀어야** 몸통이 벽 밖에 남는다. 회전만 뒤집으면 벽 속으로 들어간다.

| 소품 | 두께 | 벽면에서 밀 거리 |
| --- | ---: | ---: |
| 배전함 `JunctionBox` | 0.72 | 0.72 |
| 소화기 `Extinguisher` | 0.20 | 0.20 |
| 비상등 `EmergencyLight` | 0.11 | 0.11 |
| 벤트그릴 `VentGrille` | 0.08 | 0.08 × 스케일 |
| 세면대 `Basin` | 0.73 | 0.73 |

**앞뒤를 눈으로 가릴 때 정점 수를 세지 않는다.** 4장에서 그렇게 재다 사물함을
거꾸로 읽었다 — 열린 문짝이 경첩 쪽에 정점을 얹어서 뒷면이 더 많아 보인다.
**밝은 빈 씬에 하나만 놓고 앞뒤로 한 장씩 찍는 것**이 확실하고 더 빠르다.

### 어디에 놓느냐가 방향만큼 자주 틀린다 (2026-08-09 · 4장)

4장을 플레이해 보니 방향과 별개로 **떠 있거나 벽에 박힌 것**이 잔뜩 나왔다.
전부 프리팹마다 「어디에 닿아야 하는가」가 다른데 그걸 안 보고 좌표만 준 탓이다.

| 소품 | 닿아야 하는 곳 | 4장에서 틀렸던 것 |
| --- | --- | --- |
| 조명 하우징 `LightHousing` | **피벗이 윗면.** 천장 안쪽 면 높이에 그대로 둔다 | 캣워크 위 `y 6.6` 에 두어 **통로 한가운데 머리 높이로 떠 있었다** |
| 케이블 트레이 `CableTray` | **사다리 면이 수평.** 천장 밑에 눕힌다 | 수직 벽에 눕혀 붙여 **갈빗대만 튀어나온 빗처럼** 보였다 |
| 배관 다발 `PipeRun` | 벽면에 닿게. 폭 0.22 의 절반을 벽에서 뺀다 | 벽에서 0.24m 떠 있었다 |
| 밸브 휠 `ValveWheel` | **반드시 배관 위.** 밑면이 배관 윗면에 닿는다 | 배관 없이 벽에 달아 허공에 떠 있었다 |
| 벽 레버 `WallLever` | 판이 벽에. 손잡이가 `-Z` 로 나온다 | 발전기에서 1.43m 떨어져 허공에 붙어 있었다 |

IMPORTANT: **놓고 나서 재본다.** 바닥에 서는 것은 바운즈 밑면에서 아래로 광선을
쏴 틈을 재고, 벽에 붙는 것은 앞 방향 반대로 쏴 벽까지 거리를 잰다. 어두워서 눈으로는
안 보이고, 플레이하면 바로 걸린다.

**배경 장식이 통로를 가로지르지 않는지도 같이 본다.** 4장 둥지 가닥 여덟이 캣워크와
계단을 관통해 **기둥처럼 보였다.** 무작위로 뿌리는 장식은 걸어 다니는 부피와 겹치는
것을 걸러낸다.

**밸브 휠은 배관 축이 로컬 `X` 다.** 배관 위에 얹는 물건이라 배관 방향을 먼저 정하고
회전을 맞춘다 — 배관이 `Z` 로 뻗으면 요 ±90, `X` 로 뻗으면 요 0 또는 180 이다.
허공에 두면 어떻게 돌려도 어색하다.

IMPORTANT: **밸브는 휠이 스템의 `+Z` 쪽에 치우쳐 달려 있다.** 돌리는 면이 방을 봐야
손이 닿는다. 눈으로는 양쪽이 비슷해 보여서 **정점을 세어 가렸다** — `+Z` 쪽 2631 개,
`-Z` 쪽 1352 개다. 대칭에 가까운 물건은 렌더를 봐도 앞뒤가 안 갈린다.

### 6장도 처음부터 다시 지었다 (2026-08-09)

옛 6장은 빈 마당에 큐브 엄폐물 다섯이었다. 42 × 51 승강장으로 다시 짓고 절차를
셋으로 늘렸다. 판단은 [`decisions.md`](decisions.md) 에 있다.

| 어디 | 무엇 |
| --- | --- |
| 권양기실(서) | 산업 기계 2 · 밸브 휠 · 드럼통 · 나무상자 · 배관 3 · 케이블 트레이 · 소화기 · 비상등 |
| 히터(동) | 히터 유닛 · 벽 레버 · 드럼통 2 · 선반 · 케이블 트레이 · 비상등 · 벤트그릴 |
| 배전반(북서) | 배전함 · 벽 레버 · 나무상자 · 케이블 트레이 · 비상등 |
| 연료 창고(남동) | 선반 2 · 드럼통 5 · 나무상자 2 · 비상등 · **연료통(드럼통 0.52 배)** |
| 승강장(북) | 삭도 곤돌라 · 벽 레버 · 선반 · 드럼통 · 소화기 |
| 마당 전체 | 산업 기계 5 · 나무상자 4 · 드럼통 4 — 시야를 끊는 엄폐물이다 |

**곤돌라는 피벗이 「행어 그립」이라 그대로 매달린다.** 승강장 상판 1.40 위 0.20 에
바닥이 뜨고, 탑승 지점은 그 안이다.

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

### 18. 삭도 곤돌라 `CableCar` — 2026-08-09 다시 뽑는다

    Large aerial tramway gondola for a dozen standing passengers. Riveted
    steel shell, rounded corners, shallow curved roof, plain interior with a
    handrail and two grab poles. The upper body is one wraparound picture
    window on all four sides, a low solid skirt below it, slim mullions,
    glazing carried up into the roof curve. Curved hanger arm on the roof
    ending in a grip clamp.

    The glass panes are separate thin sheets set in the frame with a clear
    air gap all around, not fused to it. One pane is cracked in a spider web
    from an impact point, the cracks modelled as geometry with the shards
    offset. One pane is missing entirely.

    Worn matte grey-green steel, bare metal at edges. Clear glass. Dark
    unlit albedo, no baked shadows or AO. Low-poly, clean topology, no text
    or decals.

`788자.` 문은 뺐다 — 유리 한 판이 통째로 없으므로 열린 구멍은 이미 있고, 문 분리는
엔딩 화면에 필요 없다.

- 치수: **4.00 × 2.60 × 4.00m** — `Ch6` 의 `CableCar/Car` 데크(`4.00 × 0.30 × 4.00`)에
  맞췄다. **창 윗변이 2.45m, 아래 스커트가 0.45m** 여야 한다
- 축: 진행 방향 `+Z` · 피벗: **행어 그립 중심** (케이블에 매달린다)
- 바꿀 곳: `Ch6` 의 `CableCar/Car`, 그리고 **`Ending` 에 새로 세운다** (지금 엔딩 씬에
  곤돌라가 없다)
- 부품 분리: **유리 판 전부 · 문** (덩어리 분리). 유리는 반투명 머티리얼을 따로 줘야 한다
- 폴리곤: 8,000 tris 이하 · 텍스처 **1024** (카메라가 안에 들어가는 유일한 소품이다)

IMPORTANT: **기존 임포트본(`Assets/Game/Props/CableCar/`)은 버린다.** 1.33 × 2.00 ×
1.28m 로 들어와 있는데, 이건 자기 바닥판인 4 × 4m 데크보다 작다. 원인은 이 항목의
옛 치수 「1.60 × 2.00 × 2.40m (`Ch6` 의 `Body` 실측)」가 **잘못 잰 값**이었기
때문이다 — 그 크기는 `Heater/Body` 이고 삭도가 아니다.

## 왜 커져야 하나 — 창 윗변이 눈높이보다 낮았다 (2026-08-09 실측)

| | 값 |
| --- | --- |
| 임포트본 캐빈 | 1.33 × **2.00** × 1.28m (피벗이 위, 캐빈이 y −2.0 ~ 0) |
| 바닥 · 지붕 두께를 뺀 내부 높이 | 약 1.7m → **창 윗변 1.5m 언저리** |
| 플레이어 눈높이 | **1.70m** |
| `Colossus` 실측 | 6.44 × **5.33** × 4.99m |

**서 있으면 시선이 창 윗테두리에 걸린다.** 키 5.33m 짜리를 그 창으로 보려면 웅크려야
한다. 「적이 안 보인다」는 크기 문제가 아니라 **창 윗변 높이 문제**였다.

새 치수에서 눈높이 1.70m, 창 윗변 2.45m, 유리에서 1.2m 떨어져 서면 위로 32° 가
열린다. 거리 `d` 에서 보이는 높이는 `1.70 + 0.625d` 이므로 —

| 적까지 거리 | 보이는 높이 | 5.33m 가 다 들어오나 |
| --- | --- | --- |
| 5m | 4.8m | 아니오 (머리가 잘린다) |
| **6m** | **5.5m** | **예** |
| 10m | 7.9m | 여유 있게 |

IMPORTANT: 그래서 **적을 6m 보다 가까이 붙이면 안 된다.** 지금 `Ch6` 의 `Colossus`
는 `(-8.43, 0, 14.07)`, 삭도는 `(0, 0, 17)` 이라 8.9m 다 — 새 캐빈이면 들어온다.
엔딩 씬에 세울 때도 이 거리를 지킨다. **가까울수록 잘 보인다고 생각하면 반대로 된다.**

## 유리는 통째로 뽑으면 안 보인다

IMPORTANT: **Meshy 는 불투명 메시 하나로 내보내므로, 유리를 그냥 시키면 유리처럼
칠해진 벽이 온다.** 그 창으로는 적이 아니라 아무것도 안 보인다. 통창을 요구하면서
이걸 안 막으면 크기를 키운 의미가 사라진다.

세 겹으로 막았다.

| 프롬프트의 장치 | 무엇을 보장하나 |
| --- | --- |
| `separate thin sheets ... with a clear air gap all around each one` | 유리가 프레임에 안 붙어 나온다 → **덩어리 분리**로 떼어 반투명 머티리얼을 줄 수 있다 |
| `cracks modelled as real geometry with the shards slightly offset` | 금이 텍스처가 아니라 형태로 남는다. 반투명으로 바꿔도 금은 살아 있고 달빛을 받는다 |
| **`One pane is missing entirely, leaving an open empty frame`** | **분리가 실패해도 뚫린 창이 하나 남는다.** 이 한 줄이 안전망이다 — 유리를 못 떼어내도 적을 보여줄 시선 하나는 확보된다 |

떼어낸 유리에 줄 머티리얼은 URP `Lit` · Surface Type **Transparent** · alpha 0.12
근처 · smoothness 0.85. 금이 간 판은 **알파를 조금 올려**(0.2) 금선이 읽히게 한다.
프레임 · 셸은 다른 소품과 같이 metallic 0 · smoothness 0.25 다.

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

# 배경 — 타이틀 · 엔딩의 수목한계선

타이틀과 엔딩은 같은 설원 배경을 공유한다. 원래 `_Backdrop/Treeline` 아래에
**Blender 로 뽑은 침엽수 3종 74그루**가 서 있었다. 2026-08-09 에 **전부 지우고 Meshy
나무 3종 34그루로 갈아치웠다.**

`Title_Conifer_A/B/C.fbx` 와 `M_Title_Tree` · `M_Title_TreeSnow` 는 참조가 없어진 것을
확인하고 지웠다. 새 에셋은 `Assets/Game/Levels/Title/Trees/` 에 있고 두 씬이 공유한다.

## 74 → 34 로 줄인 이유 (2026-08-09 실측)

| | 옛 Blender 나무 | 새 Meshy 나무 |
| --- | --- | --- |
| 폴리곤 (한 그루) | 200 · 236 · 272 tris | **25,684 · 20,142 · 21,492 tris** |
| 그루 수 | 74 | **34** |
| 씬 전체 | 약 17,000 tris | **812,152 tris** |
| 텍스처 | 없음 (단색) | 1024 albedo + normal 3벌, 폴더 16 MB |

**한 그루가 옛 숲 전체보다 무겁다.** 74 를 그대로 두면 180만 tris 가 되어 WebGL 에서
감당이 안 된다. 그래서 그루 수를 절반 이하로 줄이고, **안개가 먹는 자리를 먼저
버렸다** — 안개가 ExpSq 0.014 라 z 70 밖은 화면 기여가 20% 아래다. 옛 배치는 z 65.7 ·
x ±80 까지 뿌려져 있었는데 거기를 걷어내고 앞쪽에 몰았다.

IMPORTANT: 가장 크게 달라진 것은 **근경 레이어가 생긴 것**이다. 옛 배치는 가장 가까운
나무가 카메라에서 **30.4m** 밖이라 z −4 ~ 22 의 26m 가 통째로 비어 있었다. 새 배치는
z 3~24 에 여섯 그루와 통나무 넷을 넣어 화면 앞을 채운다. **그루 수는 절반인데 화면은
더 꽉 찬다.**

- 형태를 못 고치는 것은 여기도 같았다 — 옛 3종을 만든 Blender 스크립트가 저장소에
  없다. `Ch2_CorridorSection.fbx` 와 같은 상황이라 새로 뽑는 쪽이 고치는 쪽보다 쌌다

## 배치할 때 지킨 것

| 씬 | 비워야 하는 자리 | 왜 |
| --- | --- | --- |
| 공통 | 화면 중앙 회랑 | 타이틀 문구 · 엔딩 헤드라인이 그 위에 올라간다 |
| `Title` | `RelayMast` (−16, 54) · `Apparition` (11, 37) 둘레 | 중계소 철탑과 「그것」의 실루엣이 이 화면의 주인공이다 |
| `Ending` | **`x 4~22` · `z 5~45`** | **삭도 케이블이 지면 위 3.5~8.5m 밖에 안 된다.** 9m 짜리 나무를 그 띠에 두면 케이블을 가린다. 케이블이 끝나는 z 49 부터는 가운데도 쓸 수 있다 |
| `Ending` | 화면 왼쪽 1/3 | **엔딩 메뉴 패널이 런타임에 왼쪽에 뜬다.** 처음엔 모르고 근경 나무를 거기 세웠다가 패널 뒤에 완전히 가려서 옮겼다 |

지면 높이는 **`Snowfield` 메시 정점을 직접 샘플링해서** 맞췄다. 설원이 좌우 · 앞뒤로
기울어 있어(타이틀 기준 +0.6 ~ −2.2m) 고정값을 쓰면 뜨거나 묻힌다. 밑동은 0.35m
파묻었다 — `ConiferDead` 의 넓은 밑동 원반이 그래야 눈에 안 걸린다.

통나무는 **잘린 단면이 카메라를 등지게** `yRot 250~292` 로 돌렸다. 단면이 밝은
원반이라 정면으로 두면 어두운 화면에서 그것만 눈에 걸린다.

## 이 셋의 공통 규칙 — 소품과 다르다

**위쪽 소품 30종의 공통 블록(`worn matte grey-green industrial paint`)을 쓰지
않는다.** 강철 도장 규칙이라 나무에 넣으면 초록색 페인트칠한 나무가 나온다.

셋 다 **세 문단**으로 쓴다. 재질을 Meshy 에서 칠해 오므로 형태만 적으면 안 된다.

| 문단 | 무엇을 적나 |
| --- | --- |
| ① 형태 | 실루엣과 덩어리. 눈은 여기서 **지오메트리로** 요구한다 |
| ② `Materials:` | 표면의 성질 — 거칠기 · 광택 · 색. **Meshy 텍스처 단계가 읽는 문단이다** |
| ③ 명도와 금지 | 알베도를 얼마나 어둡게 뽑을지, 그리고 굽지 말 것들 |

| 넣는 말 | 왜 |
| --- | --- |
| **`solid geometry only, no alpha-cut leaf planes, no transparency`** | **이 셋에서 가장 중요한 줄이다.** Meshy 는 메시 하나 · 아틀라스 하나로 내보내므로 알파 컷아웃 잎사귀를 제대로 못 만든다. 사진 같은 침엽수를 시키면 뭉개진 덩어리가 온다. 가지 뭉치를 **덩어리 지오메트리**로 만들게 해야 지금 3종과 실루엣이 이어진다 |
| `modelled as solid geometry` (눈) | 눈을 텍스처로만 칠하면 실루엣에 안 나온다. 지금 3종은 눈이 서브메시로 따로 있어 형태가 보인다. 그 인상을 이어받아야 한다 |
| **`Keep the albedo very dark ... five to ten percent grey`** | **재질을 Meshy 에서 하기로 했으므로 여기가 승부처다.** 생성기는 낮 조명 기준으로 칠하는데 기존 74그루의 줄기 값은 `0.03` 이다. 퍼센트로 못 박지 않으면 갈색 나무가 와서 검은 배경 앞에 혼자 뜬다 |
| `completely matte, no specular highlight, no wet sheen` · `metalness zero` | 젖은 듯이 반짝이면 눈보라 속 나무로 안 보인다. roughness 맵을 안 받고 파라미터로 덮을 것이므로, **광택을 텍스처에 칠해 오면 지울 방법이 없다** |
| `not ice, not glossy, no sparkle` (눈) | 「snow」만 쓰면 반짝이는 얼음 결정으로 칠해 온다. 이 게임의 눈은 마른 가루눈이다 |
| `no baked shadows, no baked ambient occlusion, no highlights or sky light painted into the colour map` | 소품과 같은 이유. 달빛 하나로 조명하는 화면이라 구워진 빛이 싸운다 |

IMPORTANT: **수목한계선의 나무는 온전하지 않다.** 기획이 「그 위로는 나무가 자라지
못하는 고도」로 제목을 정했으므로, 곧고 풍성한 크리스마스트리가 오면 장소가 틀린다.
아래 셋 중 **둘이 죽은 나무**인 것이 그래서다. 바람에 한쪽으로 쓸린 가지 · 부러진 수관 ·
벗겨진 껍질이 이 게임의 나무다.

---

### 31. 눈 얹힌 침엽수 `Conifer`

    Tall subalpine spruce, one straight trunk tapering to a narrow spire,
    seven tiers of short stiff branches getting shorter toward the top, each
    tier a dense wedge-shaped solid mass of needles. Thick rounded caps of
    settled snow sit on the upper surface of every tier, modelled as solid
    geometry.

    Deeply furrowed rough bark, almost black. Needles almost black too,
    matte and dry. Snow is dry wind-packed powder, soft matte pale
    grey-blue, not ice, not glossy, no sparkle. Fully rough, metalness zero.

    Keep the albedo very dark, about five to ten percent grey for bark and
    needles, snow no brighter than mid grey; it must read as a night
    silhouette. Flat unlit albedo, no baked shadows or AO. Low-poly, clean
    topology, solid geometry only, no alpha leaf planes, no transparency.

`780자.`

- 치수: 높이 **9.0m** · 폭 3.2m (기존 `Title_Conifer_C` 10.44m 와 나란히 서도 어색하지
  않은 크기)
- 축: 위 `+Y` · 피벗: **줄기 밑동 중앙** (설원에 박아 넣는다)
- 놓을 곳: `Title` · `Ending` 의 `_Backdrop/Treeline` 에 **새 자식으로 추가.** z 4~18,
  x ±4~10 에 두세 그루. 기존 74그루는 건드리지 않는다
- 부품 분리: 불필요
- 비고: 셋 중 **이것 하나만 먼저 뽑아도 된다.** 근경 레이어가 생기는 것만으로 화면이
  달라진다

### 32. 죽은 침엽수 `ConiferDead`

    Dead weathered conifer at the tree line, bare trunk with the bark peeled
    away in long patches, broken stubby limbs, all branches swept to one side
    by the wind, the crown snapped off leaving a splintered stump. No
    needles. A little snow packed into the crooks of the limbs, modelled as
    solid geometry.

    Dry dead wood split along the grain, matte, no sheen. Exposed heartwood a
    dull mid grey, the deep cracks and remaining bark almost black. Snow is
    dry powder, soft matte pale grey-blue, not ice, no sparkle. Fully rough,
    metalness zero.

    Keep the albedo dark: the wood no brighter than a low-mid grey, snow no
    brighter than mid grey; it must read as a night silhouette. Flat unlit
    albedo, no baked shadows or AO. Low-poly, clean topology, solid geometry
    only, no transparency.

`776자.`

- 치수: 높이 **6.5m** · 폭 2.4m
- 축: **가지가 쓸린 방향이 `+Z`** · 피벗: 줄기 밑동 중앙
- 놓을 곳: 31 번 옆에 섞는다. **바람 방향을 맞춰 세운다** — 여러 그루가 같은 쪽으로
  쓸려 있어야 눈보라가 한 방향으로 읽힌다
- 부품 분리: 불필요
- 비고: **31 번과 실루엣이 정반대라 둘을 섞으면 반복이 안 보인다.** 하나는 눈이 얹힌
  삼각형, 하나는 앙상한 수직선이다

### 33. 쓰러진 통나무 `FallenLog`

    Fallen dead conifer trunk lying on its side, bark peeled off along the
    upper face, a ring of broken branch stubs around it, a splintered break
    at one end and a torn root plate at the other, a low separate stump
    beside it. A long ridge of drifted snow banked against one side and along
    the top, modelled as solid geometry.

    Dry dead wood split along the grain, matte, no sheen. The bark left on
    the underside is rough and almost black, the break end shows paler dry
    fibres. Snow is dry powder, soft matte pale grey-blue, not ice, no
    sparkle. Fully rough, metalness zero.

    Keep the albedo dark: wood no brighter than a low-mid grey, snow no
    brighter than mid grey. Flat unlit albedo, no baked shadows or AO.
    Low-poly, clean topology, solid geometry only, no transparency.

`771자.`

- 치수: 길이 **4.5m** · 지름 0.55m · 그루터기 높이 0.7m
- 축: **길이가 `+X`** · 피벗: 바닥면 중앙
- 놓을 곳: 카메라 바로 앞 z 2~6. **설원 평면이 끝나는 자리를 가리는 데 쓴다**
- 부품 분리: 불필요 (그루터기는 붙은 채로 온다. 떼고 싶으면 덩어리 분리)
- 비고: 셋 중 우선순위가 가장 낮다. 다만 **누운 물건이라 화면 아래쪽을 채운다** —
  서 있는 것만 놓으면 발밑이 계속 빈다

---

## 뽑을 때 — 나무는 소품과 설정이 다르다

| 무엇 | 소품 30종 | **나무 3종** | 왜 다른가 |
| --- | --- | --- | --- |
| 파일 형식 | FBX | FBX | 같다 |
| 텍스처 | base color + normal | **base color + normal** | 같다. **재질을 Meshy 에서 칠해도 metallic · roughness 맵은 안 받는다** — 셋 다 완전 무광 · 비금속이라 파라미터 두 개로 덮는 편이 텍스처가 절반이고 톤도 안 튄다 |
| 임포트 해상도 | 512 | **1024** | 근경에 서므로 512 는 껍질이 뭉갠다. 소품과 달리 가까이서 본다 |
| 리메시 | 약하게 | **약하게** | 세게 걸면 가지 뭉치가 하나로 뭉쳐 실루엣이 죽는다 |
| 폴리곤 | 낮게 | **낮게 — 8,000 tris 이하** | 근경 서너 그루면 충분하다. 74그루에 쓰는 것이 아니다 |

## 임포트 뒤 머티리얼 — 칠은 Meshy 가 한다

색과 표면은 **프롬프트 ②③ 문단이 정했고, 그대로 먹었다.** 화면에서 밤 실루엣으로
읽혀서 **틴트를 한 번도 안 건드렸다.** `_BaseColor` 는 흰색 그대로다.

`M_Tree_Conifer` · `M_Tree_ConiferDead` · `M_Tree_FallenLog` 셋을
`Assets/Game/Levels/Title/Trees/` 에 만들었다.

| 항목 | 값 |
| --- | --- |
| 셰이더 | `Universal Render Pipeline/Lit` |
| `_BaseMap` | Meshy albedo (sRGB 켬, 1024) |
| `_BumpMap` | Meshy normal (`TextureImporterType.NormalMap`) |
| `_BaseColor` 틴트 | **흰색 그대로** |
| `_Metallic` | 0 |
| `_Smoothness` | 0.1 |
| 그림자 | **끔** (`shadowCastingMode Off`, `receiveShadows false`) — 배경이고 그루가 많다 |

IMPORTANT: **명도는 틴트로 고치는 것이 아니라 뽑을 때 정하는 것이다.** 틴트로 곱해
내리면 노멀맵이 만드는 껍질 요철까지 같이 눌려 평평해진다. 그래서 프롬프트 ③문단에
「bark and needles 는 5~10% 회색」을 퍼센트로 못 박았고, 그 한 줄 덕분에 후처리가
없었다. 근거는 [`world-assets.md`](world-assets.md) 의 「이 게임의 조명에서는 알베도
색이 화면에 거의 안 나온다」 — **색이 아니라 명도로 맞춰야 하는 화면**이다.

## 임포트에서 손으로 고쳐야 했던 것

프롬프트가 못 막은 것들이다. 다음 모델을 받을 때도 같은 자리를 본다.

| 무엇 | 온 상태 | 고친 방법 |
| --- | --- | --- |
| **스케일** | 세 모델 다 **0.01 단위** (9m 나무가 0.008) | 목표 높이에 맞춰 정점을 직접 곱했다. 배율 749 · 342 · 236 |
| **축** | 세 모델 다 **Z 축이 위** — 누워서 왔다 | 정점 · 노멀 · 탄젠트를 `Euler(-90,0,0)` 으로 돌려 구웠다 |
| **피벗** | 메시 중심 | 밑동 중앙 `(centerX, minY, centerZ)` 으로 옮겼다 |
| **폴리곤** | 20,142 ~ 25,684 tris | **못 고친다.** 프롬프트에 8,000 이하를 넣었는데 3배로 왔다. 그루 수를 줄여 흡수했다 |
| **텍스처** | 2048 PNG (원본 4~9 MB) | 임포터 `maxTextureSize` 만으로는 **원본 파일이 그대로 커밋된다.** PIL 로 1024 로 다시 저장해 48 MB → 16 MB |

IMPORTANT: `maxTextureSize` 는 빌드에만 먹고 저장소 용량에는 안 먹는다. 소품 30종
때 「텍스처를 512 로 줄이면 40 MB」라고 적힌 것은 **파일 자체를 줄인 것**이다. 임포터
설정만 바꾸고 넘어가면 저장소가 조용히 세 배가 된다.

---

## 30종 전수 점검 (2026-08-09) — 씬을 열어 세고, 손전등 아래서 봤다

1 · 2 · 6장에 실제로 무엇이 놓였는지 세고, 30종을 **손전등만 켠 조건에서** 한 장으로
뽑아 봤다. 스킬이 정한 확인 조건이다 — 천장 조명을 켜고 판단하면 안 된다.

IMPORTANT: 처음에는 물건 크기에 비례해 카메라를 붙였더니 **작은 것들이 통째로 하얗게
날아갔다.** 0.16m 짜리 컵에 손전등을 코앞에 댄 셈이라 텍스처가 흰 것인지 조명이 센
것인지 구분이 안 됐다. **최소 거리를 1.4m 로 두고** 다시 재야 진짜가 갈린다. 그렇게
하니 대부분 0.0% 로 떨어졌고, 남은 셋이 진짜 문제였다.

### 배치 현황

| 장 | 소품 종수 | 인스턴스 | 남은 프리미티브 |
| --- | --- | --- | --- |
| 1 관제동 | 21 | 70 | 데칼 9 · 뷰모델 11(꺼짐) |
| 2 숙소동 | 17 | 74 | 조명 튜브 15 · 케이블 3 · 거울 · 웅덩이 |
| 6 승강장 | 14 | 56 | 연료 주입구 |

**밀도는 모자라지 않다.** 6장이 14종으로 가장 적지만 실외라 지형과 구조물이 화면을
채운다. 문제는 개수가 아니라 **어떤 것이 튀는가**다.

### 그대로 두는 것 — 19종

`Bunk` `CableCar` `Chair` `ControlConsole` `Crate` `Desk` `Drum` `HeaterUnit`
`LightHousing` `Locker` `MachineBlock` `Mattress` `NoticeBoard` `Searchlight`
`Shelf` `Shutter` `SteelDoor` `ValveWheel` `WallLever` `WashPartition`

무광 회녹색으로 톤이 맞고 손전등 아래서 형태가 읽힌다. 손대지 않는다.

### 다시 뽑을 것

IMPORTANT: 처음에 **소화기가 빨간 것**을 결함으로 적었는데 **틀렸다.** 이 문서 위쪽에
「색이 튀는 것은 소화기와 비상등 둘뿐이다. 어둠 속에서 그 둘만 눈에 걸리게 한다」고
이미 적혀 있다 — **의도된 것**이다. 눈에 띄는 것과 잘못된 것을 구분해야 한다.
그래서 아래는 **의도로 설명되지 않는 것만** 남겼다.

| 소품 | 무엇이 문제인가 | 쓰는 곳 |
| --- | --- | --- |
| `Extinguisher` | 색은 의도대로다. 다만 **흰 라벨에 글자가 구워져 있다** | 1장 4 · 6장 2 |
| `JunctionBox` | 표면에 **글자가 찍혀 있다** (「TDAY J3CJ8」). 시대 · 국적을 특정한다 | 2장 1 · 6장 1 |
| `Mug` | 순백 도자기. 1.4m 거리에서도 **90% 가 날아간다.** 눈에 띄어야 할 이유가 없는 물건이다 | 1장 2 |
| `EmergencyLight` | 눈에 띄는 것은 의도지만 **70% 가 날아가 형태가 없다.** 발광은 `Light` 가 하므로 알베도는 어두워도 된다 | 1장 3 · 6장 4 |
| `Basin` | 크림색이라 회녹색 세계에서 톤이 다르다 | 2장 3 |
| `Logbook` | 검은 판 두 장. 「펼쳐진 무전 일지」로 안 읽힌다 | 1장 1 |

IMPORTANT: **`no branding, no text, no decals` 를 넣었는데도 글자가 나왔다.** 배전함과
소화기 둘이다. 프롬프트로 막는 것에는 한계가 있으므로 **받은 즉시 텍스처를 눈으로
확인**한다. 글자는 알베도에 구워져 들어와 지울 수 없다.

### 형태가 요구와 다른 것

| 소품 | 무엇이 문제인가 | 쓰는 곳 |
| --- | --- | --- |
| `CableTray` | **케이블 트레이가 아니다.** 가로등 갓처럼 뽑혔다 | 1장 1 · 6장 4 |
| `PipeRun` | 「직선 · 엘보 4m 모듈」이어야 하는데 **짧은 토막 다발**이다. 이어 붙일 수 없다 | 1장 10 · 2장 24 · 6장 3 |

`PipeRun` 이 37 개로 가장 많이 배치된 소품인데 모듈이 아니라는 것이 가장 크다.
지금은 벽에 다발째 붙여 쓰고 있어 **배관이 어디서 와서 어디로 가는지 안 읽힌다.**

### 없어서 다른 것으로 대신하고 있는 것

| 무엇 | 지금 | 왜 문제인가 |
| --- | --- | --- |
| **삭도 권양기** | `MachineBlock` 2 + **연료 드럼통** + 밸브 + 배관 | 케이블을 감는 드럼이 있어야 하는데 연료통을 쓰고 있다. 6장의 주 장치다 |
| **형광등 튜브** | 큐브 15 개 (`LightTube`) | `LightHousing` 과 짝인데 튜브가 각졌다 |
| **세면장 거울** | 큐브 + 얼음 머티리얼 | 반사가 없으면 거울로 안 읽힌다 |
| **제설포** | 없음 | 6장 피날레의 물건. 5장 작업과 같이 온다 |

## 2차 프롬프트 — 점검에서 나온 11종

앞의 30종과 **같은 공통 블록**을 쓴다. 글자가 뚫린 둘(`Extinguisher` · `JunctionBox`)
에는 금지 문구를 한 겹 더 얹었다.

**R9 벽 레버가 이 묶음에서 가장 급하다.** 뒤늦게 찾았는데 20 개가 다섯 씬에 깔려
있고 이 게임의 모든 조작이 그것 하나를 거친다.

### R1. 소화기 `Extinguisher` (재생성)

    Wall-mounted fire extinguisher, cylindrical body with a domed top, black
    squeeze handle, curved hose clipped to the side, pressure gauge at the
    neck, mounting bracket behind. Faded dark red paint, chipped and dulled by
    age, bare steel showing at the base rim. Completely blank surface with no
    label, no sticker, no printed panel, no writing anywhere. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals.

- 치수: 높이 0.55m · 지름 0.16m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 바꿀 곳: `Ch1` 4 · `Ch6` 2 (`Winch_Ext` 포함)
- 부품 분리: 불필요
- 비고: **빨간 것은 의도다.** 어둠에서 눈에 걸려야 하는 둘 중 하나이므로 색은 유지하고
  채도만 조금 낮춘다. 고칠 것은 **라벨의 글자**뿐이다

### R2. 배전함 `JunctionBox` (재생성)

    Industrial wall junction box, rectangular steel enclosure with a hinged
    front door, four corner latches, a conduit gland entering the bottom and
    one at the side, small hinge barrels along one edge. Worn matte grey-green
    industrial paint over steel, bare metal at the edges, light rust at seams.
    Completely blank door with no plate, no sticker, no printed marking, no
    writing anywhere. Low-poly, game-ready, clean topology. No baked shadows,
    no baked ambient occlusion. No branding, no text, no decals.

- 치수: 0.45 × 0.60 × 0.20m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 뒷면 중앙 (벽에 붙는다)
- 바꿀 곳: `Ch2` 1 · `Ch6` 1
- 부품 분리: 불필요 (문은 안 연다)

### R3. 커피잔 `Mug` (재생성)

    Heavy enamelware mug, straight tapered sides, thick rolled rim, single
    sturdy handle, slight dent in the side. Dark grey-green enamel with the rim
    chipped to bare dark metal, stained interior. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding, no
    text, no decals. Neutral desaturated palette only.

- 치수: 높이 0.10m · 지름 0.09m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 바꿀 곳: `Ch1` 관제 콘솔 · 책상
- 부품 분리: 불필요
- 비고: 지금 것은 **순백 도자기라 1.4m 거리에서도 90% 가 날아간다.** 눈에 띄어야 할
  이유가 없는 물건이다. 「커피가 아직 따뜻하다」는 김이나 내용물이 보여줄 것이지
  흰 컵이 보여줄 것이 아니다

### R4. 비상등 `EmergencyLight` (재생성)

    Industrial emergency light fixture, dark boxy housing with a wire cage over
    a small rectangular lens, conduit stub at one end, wall bracket behind.
    Dark grey-green painted steel housing, almost black, with only the recessed
    lens lighter. Low-poly, game-ready, clean topology. No baked shadows, no
    baked ambient occlusion. No branding, no text, no decals.

- 치수: 0.24 × 0.16 × 0.12m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 뒷면 중앙
- 바꿀 곳: `Ch1` 3 · `Ch6` 4 (`Winch_EmLight` 포함)
- 부품 분리: 불필요
- 비고: **눈에 걸리는 것은 의도지만 지금은 70% 가 날아가 형태가 없다.** 붉게 빛나는
  일은 씬의 `Light` 가 이미 하므로 **알베도는 어두워야** 한다. 몸체를 어둡게 하면
  켜졌을 때 렌즈만 살아난다

### R5. 세면대 `Basin` (재생성)

    Wall-hung utilitarian washbasin, rectangular bowl with rounded inner
    corners, a single mixer tap, exposed P-trap and supply pipes below, wall
    bracket. Chipped grey-green enamel over cast iron with bare dark metal at
    the chips, dull steel pipework. Low-poly, game-ready, clean topology. No
    baked shadows, no baked ambient occlusion. No branding, no text, no decals.
    Neutral desaturated palette only.

- 치수: 0.60 × 0.45 × 0.40m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 뒷면 중앙
- 바꿀 곳: `Ch2` 세면장 3
- 부품 분리: 불필요

### R6. 무전 일지 `Logbook` (재생성)

    Open hardbound logbook lying flat, thick cover folded back, a visible block
    of pages with slightly uneven edges, a pen resting in the gutter, a ribbon
    marker hanging out. Dark grey-green worn cover, pale grey pages left
    completely blank with no writing, no ruling, no printed lines. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals.

- 치수: 펼친 상태 0.42 × 0.30 × 0.05m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 바꿀 곳: `Ch1` `RadioLog_Reach`
- 부품 분리: 불필요
- 비고: 내용은 `Readable` 이 화면에 띄우므로 **종이에 글자를 넣지 않는다.** 지금 것은
  검은 판 두 장이라 펼쳐진 책으로 안 읽힌다

### R7. 케이블 트레이 `CableTray` (재생성)

    Straight run of industrial cable tray, a horizontal steel ladder rack with
    two side rails and evenly spaced rungs, filled with a bundle of thick
    cables running along its length, mounted on two L-shaped wall brackets.
    Worn matte grey-green paint over steel, bare metal at the edges, light rust
    at seams, dark grey cable insulation. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. No branding, no text, no
    decals.

- 치수: **길이 4.0m** · 폭 0.30m · 높이 0.20m
- 축: 길이가 `+Z`, 위 `+Y` · 피벗: 한쪽 끝 중앙 (이어 붙인다)
- 바꿀 곳: `Ch1` 1 · `Ch6` 4 (`Winch_Tray` 포함)
- 부품 분리: 불필요
- 비고: 지금 것은 **케이블 트레이가 아니라 가로등 갓처럼 뽑혔다.** 명백한 오생성이라
  형태부터 다시 잡는다. 길이를 4m 로 못 박아 복도 칸과 맞춘다

### R8. 배관 모듈 `PipeRun` (재생성 · 두 벌)

직선과 엘보를 **따로 뽑는다.** 이어 붙이려면 끝면이 같아야 한다.

    Straight horizontal run of three parallel industrial pipes of slightly
    different diameters, held together by two evenly spaced pipe clamps, with a
    flanged joint in the middle and bolted flanges at both open ends. Worn
    matte grey-green paint over steel, bare metal at the edges, light rust at
    seams. Low-poly, game-ready, clean topology. No baked shadows, no baked
    ambient occlusion. No branding, no text, no decals.

엘보는 첫 문장만 바꾼다.

    Ninety-degree elbow bend of three parallel industrial pipes turning
    together, bolted flanges at both open ends

- 치수: 직선 **길이 4.0m** · 다발 폭 0.35m / 엘보 0.6 × 0.6m
- 축: 길이가 `+Z`, 위 `+Y` · 피벗: 한쪽 끝 중앙
- 바꿀 곳: `Ch1` 10 · `Ch2` 24 · `Ch6` 3 — **가장 많이 배치된 소품이다**
- 부품 분리: 불필요
- 비고: 지금 것은 짧은 토막 다발이라 **배관이 어디서 와서 어디로 가는지 안 읽힌다.**
  양끝 플랜지가 핵심이다 — 이음매가 보여야 이어진 것으로 읽힌다

### R9. 벽 레버 `WallLever` (재생성) — **우선순위 최상**

IMPORTANT: 1 차 프롬프트가 **계속 걸쇠로 나왔다.** 원인은 첫머리에 둔
`wall-mounted ... back plate` 다. Meshy 는 앞부분에 크게 끌리는데, 「벽 + 판」이
먼저 오면 **문고리 · 걸쇠 · 경첩** 쪽으로 가버린다. 지금 있는 모델이 정확히 그
결과다.

그래서 **판과 벽을 프롬프트에서 뺐다.** 판이 없으면 걸쇠가 될 수 없다. 벽에
붙이는 일은 Unity 에서 위치만 잡으면 되고, 필요하면 기존 `WallLever_Body` 의 판을
그대로 재활용할 수도 있다. **막대가 주인공이 되게 첫 문장에 둔다.**

두 벌을 뽑아 나은 쪽을 쓴다.

**V1 — 공 손잡이 (권장)**

    A tall industrial pull lever. A single straight round steel rod half a
    metre long stands bolt upright, topped with a large heavy ball grip sized
    for two hands. The rod is thick and cylindrical along its whole length.
    At the bottom the rod enters a chunky cylindrical pivot barrel carried
    between two triangular steel cheek plates on a small square mounting foot.
    The upright rod is by far the largest part; the foot is small and low.
    Worn matte grey-green paint over steel, bare metal at the ball grip, rust
    at the pivot barrel. Low-poly, game-ready, clean topology. No baked
    shadows, no baked ambient occlusion. No branding, no text, no decals.

**V2 — T 손잡이 + 부채꼴 가이드**

    A tall industrial pull lever. A single straight round steel rod half a
    metre long stands bolt upright, topped with a horizontal T-shaped cross
    grip with knurled ends. At the bottom the rod enters a chunky cylindrical
    pivot barrel on a small square mounting foot, with a curved slotted
    quadrant arc rising beside the rod and a notch at each end of the slot.
    The upright rod is by far the largest part; the foot is small and low.
    Worn matte grey-green paint over steel, bare metal at the grip, rust at
    the pivot barrel. Low-poly, game-ready, clean topology. No baked shadows,
    no baked ambient occlusion. No branding, no text, no decals.

- 치수: 손잡이 길이 0.50m · 받침 0.22 × 0.10 × 0.22m · 전체 높이 약 0.62m
- 축: 막대가 `+Y` 로 곧게 섬, 앞 `+Z` · 피벗: **받침 바닥 중앙**
- 바꿀 곳: `Ch1` 2 · `Ch2` 4 (`ShutterLever` 포함) · `Ch4` 8 · `Ch6` 6 — **약 20개**
- 부품 분리: **막대 + 손잡이.** 피벗 배럴 위에서 자른다. `Lever.handle` 이 그것만 돌린다
- 비고: 지금 것은 **레버가 아니라 문고리 걸쇠다.** 판이 1m 인데 손잡이는 판에
  눌러붙은 작은 막대뿐이라 잡을 것이 없고, `Lever` 가 `pulledRotation (-55, 0, 0)` 으로
  눕히면 **판 속으로 파고든다**

IMPORTANT: 이것이 **이 게임의 주 상호작용물**이다. 서치라이트 · 비상 셔터 · 히터 ·
차단기 · 환기 팬 · 발전기 · 삭도 권양기가 전부 이 하나로 조작되고, 6장의 절차 셋도
그렇다. **화면에서 가장 자주 눌리는 물건**이라 우선순위가 가장 높다.

프롬프트가 붙잡고 있는 것 셋.

| 넣은 말 | 왜 |
| --- | --- |
| 첫 문장에 `A tall industrial pull lever` | Meshy 는 앞부분에 크게 끌린다. 「벽」 · 「판」을 앞에 두면 걸쇠가 된다 |
| `stands bolt upright` | 눕은 채로 뽑히면 `Lever` 가 거기서 또 눕혀 **두 번 눕는다** |
| `the upright rod is by far the largest part` | 비율을 말로 못 박지 않으면 받침이 커지고 막대가 줄어든다 |

**그래도 걸쇠가 나오면 받침을 통째로 뺀다** — `A tall industrial pull lever, a
single straight round steel rod ... with a large heavy ball grip` 까지만 남기고
돌린다. 막대 하나만 와도 쓸 수 있다. 받침은 기존 모델에서 떼어 쓰거나 Unity 에서
상자 두 개로 만들면 되고, 어차피 벽에 반쯤 묻힌다.

임포트에서 손잡이 피벗을 **배럴 축**에 맞추고, `pulledRotation` 이 **벽 반대쪽으로**
눕는지 실제로 돌려서 확인한다. 지금 프리팹은 구울 때 180° 를 먹인 것이라 그 규약이
새 모델에서 그대로일 거라고 가정하면 안 된다.

### N1. 삭도 권양기 `CableWinch` (신규)

    Large industrial cable winch, a wide horizontal drum wound with steel rope
    mounted between two heavy end frames, the drum sitting between the frames
    with a visible air gap on both sides and not fused to them, a toothed
    reduction gear on one side, an electric drive motor bolted to the base, a
    band brake lever on top, anchored to a steel bedplate. Worn matte
    grey-green paint over steel, heavy rust at the drum flanges. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals.

- 치수: 2.2 × 1.6 × 1.4m
- 축: 드럼 축이 `+X`, 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: `Ch6` `Winch` — 지금은 `MachineBlock` 둘과 **연료 드럼통**으로 대신하고 있다
- 부품 분리: **드럼.** 감기는 것을 보여주려면 축 둘레로 돌려야 하므로 프롬프트에 틈을
  넣어 두었다. 리메시를 약하게 걸어 달라고 같이 말한다
- 비고: 6장의 주 장치인데 전용 모델이 없다. **연료통을 케이블 드럼으로 쓰고 있는 것이
  이번 점검에서 가장 어색한 자리다**

### N2. 눈사태 제설포 `AvalancheCannon` (신규)

    Fixed avalanche control gas cannon, a long angled barrel on a swivel yoke
    mounted on a heavy tripod base anchored to concrete, a gas cylinder rack
    strapped beside it, a control box with a lever on one leg, guy wires to the
    ground. Worn matte grey-green paint over steel, heavy rust at the base,
    snow crust on the upper surfaces. Low-poly, game-ready, clean topology. No
    baked shadows, no baked ambient occlusion. No branding, no text, no decals.

- 치수: 높이 2.4m · 포신 길이 2.0m
- 축: 포구가 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: **5장** 세 번째 철탑 옆 · **6장** 승강장 옆
- 부품 분리: **포신.** 사면을 겨누는 동작이 있으면 요크에서 돌린다
- 비고: 기획의 5장과 6장 피날레가 같이 쓰는 물건이다. 6장 마지막은
  [`decisions.md`](decisions.md) 대로 5장이 붙을 때 하므로 **모델만 미리 받아 둔다**

### Meshy 로 만들지 않는 것

| 무엇 | 대신 | 왜 |
| --- | --- | --- |
| 형광등 튜브 (`LightTube` 15) | Unity 실린더 | 매끈한 원통 하나다. 받을 이유가 없고 텍스처 한 벌이 더 붙는다 |
| 세면장 거울 (`MirrorStrip`) | 머티리얼 | 형태가 아니라 **반사**의 문제다. smoothness 를 올린 판이면 된다 |
| 벽 · 바닥 · 천장 | 표면 텍스처 | [`world-assets.md`](world-assets.md) |

## 3 · 4 · 5장 점검 (2026-08-09) — 지금 무엇으로 대신하고 있나

앞의 점검은 1 · 2 · 6장만 봤다. 그 사이 **3장과 4장이 지어지고 있고 5장은 아직
씬이 없다.** 두 씬을 열어 보니 팀이 있는 소품으로 버티고 있었다.

| 어디 | 무엇이어야 하나 | 지금 |
| --- | --- | --- |
| 4장 | 디젤 발전기 | `MachineBlock` (범용 기계 덩어리) |
| 4장 | 축전지 랙 | `ControlConsole` (관제 콘솔) |
| 4장 | 배전반 | `JunctionBox` (0.45m 짜리 작은 함) |
| 4장 | 환기 팬 | **없다.** `VentGrille` 은 0.55m 짜리 그릴이다 |
| 4장 | 둥지 촉수 · 알집 | `Cylinder` 수십 개 (`Web_Ceil_*`) |
| 3장 | 배터리 | `Crate` (나무상자) |
| 3장 | 이송 펌프 | 꺼진 `Cube` |
| 3장 | 연료 탱크 · 연료통 | **없다.** 연료 절차가 아직 미구현이다 |

**무기는 뺐다.** `Pistol` · `Shotgun` · `PistolMag` · `ShotgunShell` 이
`Player/Models/` 에 이미 들어와 있다. 탄약 픽업이 탄창 메시를 쓰는 것도 맞다.

우선순위는 여기서도 **화면에 오래 있는 순서**다. 연료통이 첫째인 이유는 그것이
3장과 6장에서 **양손에 안겨 화면 아래를 채운 채 걷는** 물건이기 때문이다.

## 3차 프롬프트 — 3 · 4 · 5장 16종

공통 블록은 앞과 같다. 우선순위 A 는 지금 대역이 어색한 것, B 는 그 장이 지어질 때
필요한 것이다.

### A1. 연료통 `FuelCan` — 우선순위 A

    Portable jerrycan for diesel, rectangular pressed-steel body with a
    recessed X-shaped rib on each side, three-piece top carrying handle, short
    pouring spout with a screw cap, welded seam down the edges. Worn matte
    grey-green paint over steel, bare metal at the edges, light rust at seams.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals.

- 치수: 0.34 × 0.47 × 0.18m
- 축: 주둥이가 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: **3장** 펌프실 · **6장** 권양기 주입구 (`CarrySlot` 이 이미 있다)
- 부품 분리: 불필요
- 비고: **이번 목록에서 가장 오래 화면에 있는 물건이다.** 양손으로 안고 걷는 동안
  화면 아래를 채우므로 1인칭 거리에서 견뎌야 한다. 「무거운 것을 들면 총을 못 쓴다」가
  이 물건 하나로 성립한다

### A2. 디젤 발전기 `DieselGenerator` — 우선순위 A · **모듈 하나만 뽑는다**

    One repeating module of a very large skid-mounted diesel generator set, a
    ribbed engine block section on a heavy steel base frame, cooling pipework
    and a small access panel with dials on one side, anti-vibration mounts
    along the bottom, and a flat bolted flange face at each end so identical
    modules butt together in a straight line. Worn matte grey-green paint over
    steel, heavy rust at the base, soot staining on the upper surfaces.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals.

- 치수: **2.40 × 3.00 × 3.50m (모듈 하나)** — 네 번 이어 붙여 14m 가 된다
- 축: 길이가 `+Z`, 위 `+Y` · 피벗: **바닥면 중앙**
- 바꿀 곳: `Ch4` 홀 `Props/Generator` (지금 `MachineBlock` 4 + 0.72 배 1)
- 부품 분리: 불필요
- 비고: **14m 를 통짜로 뽑지 않는다.** Meshy 가 그 비율을 못 지키고, 길면 텍스처가
  늘어난다. 배관 · 밸브 · 드럼은 기존 소품을 그대로 얹는다. 치수는 `Ch4` 에 실제로
  서 있는 대역을 A 가 잰 값이다

### A3. 축전지 랙 `BatteryRack` — 우선순위 A

    Industrial battery rack, an open steel shelving frame two tiers high
    holding rows of large rectangular lead-acid cells, thick insulated
    interconnect straps running between the terminals, a drip tray under the
    bottom tier. Worn matte grey-green painted frame, dark cell casings, pale
    corrosion crust on the terminals. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. No branding, no text,
    no decals.

- 치수: **2.00 × 0.95 × 1.40m (2칸 모듈)** — 네 번 이어 붙여 8칸 8m 가 된다
- 축: 앞 `-Z`, 위 `+Y` · 피벗: **바닥면 중앙**
- 바꿀 곳: `Ch4` 축전지실 `Props/Battery` (지금 받침 큐브 + `Crate` 8)
- 부품 분리: 불필요
- 비고: 축전지실이 이 장에서 밀도가 가장 높은 장치다 — 위협 · 제약 · 무기가 한
  물건에 겹친다. **여기서 쏘면 터진다**가 화면에서 읽히려면 축전지로 보여야 한다.
  발전기와 같은 이유로 **8m 를 통짜로 뽑지 않고 모듈로 뽑는다.** 총 치수는 `Ch4` 에
  서 있는 대역을 A 가 잰 값이다

### A4. 환기 팬 `VentFan` — 우선순위 A

    Large industrial axial extraction fan set in a square wall housing, four
    wide blades behind a heavy wire guard, a motor hub at the centre with
    support struts, a hinged backdraught damper flap, bolted mounting flange
    around the frame. Worn matte grey-green paint over steel, bare metal at the
    edges, dust caked on the guard. Low-poly, game-ready, clean topology. No
    baked shadows, no baked ambient occlusion. No branding, no text, no decals.

- 치수: **지름 2.60m · 두께 0.35m**
- 축: 축이 `+Z`, 위 `+Y` · 피벗: **하우징 뒷면** (벽에 박힌다)
- 놓을 곳: `Ch4` 축전지실 북벽 · 홀 북벽(3.4 배로 키워 쓴다). `FanDevice` 는 이미
  있고 메시가 없다
- 부품 분리: **날개.** 돌아야 하므로 허브에서 나눈다. 프롬프트에
  `the bladed rotor sitting inside the housing with a visible air gap around it,
  not fused to the frame` 를 덧붙인다
- 비고: 기획의 「환기 팬을 먼저 돌린다 — 시간이 걸리고 그동안 밖에서 버텨야 한다」가
  이 물건이다. **도는 것이 보여야 기다리는 시간이 읽힌다.** 날개가 따로 있어야
  `Spin` 이 그것만 돌린다 — 하우징까지 같이 돌면 벽이 돈다. 치수는 A 가 잰 값이다

### A5. 배전반 `Switchboard` — 우선순위 A

    Floor-standing industrial switchboard cabinet, a tall steel enclosure with
    a hinged front door left ajar, rows of moulded-case circuit breakers on a
    din rail inside, thick busbars behind them, a large rotary isolator handle
    on the door, cable gland plate at the bottom. Worn matte grey-green paint
    over steel, bare metal at the edges, light rust at seams. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals.

- 치수: **1.00 × 2.00 × 0.35m**
- 축: 앞 `-Z`, 위 `+Y` · 피벗: **벽에 닿는 뒷면**
- 놓을 곳: `Ch4` 정비구 서벽 · 축전지실 서벽 (지금 `JunctionBox` 0.80 × 0.90 × 0.72)
- 부품 분리: 불필요 (문은 열린 채로 뽑는다)
- 비고: 「차단기 두 개를 찾아 올린다」의 무대다. 레버는 `WallLever` 가 맡으므로
  이것은 배경이지만, 작은 정션박스로는 「배전반을 찾아다닌다」가 안 된다.
  IMPORTANT: **`JunctionBox` 를 키운 것으로 만들지 않는다.** 그것은 2장에서 배우는
  물건이라, 같은 모델이 커져서 나오면 「2장에서 본 그 배전함」으로 읽혀 4장이 새
  장소로 안 보인다. 치수는 A 가 잰 값이다

### A6. 배터리 `BatteryCell` — 우선순위 A

    Portable sealed lead-acid battery block, a rectangular hard casing with a
    recessed carry strap on top, two stud terminals under small caps, moulded
    ribs down the sides. Dark grey-green casing, dull metal terminals with pale
    corrosion. Low-poly, game-ready, clean topology. No baked shadows, no baked
    ambient occlusion. No branding, no text, no decals.

- 치수: 0.26 × 0.22 × 0.17m
- 축: 단자가 `+Y`, 앞 `+Z` · 피벗: 바닥 중앙
- 바꿀 곳: `Ch3` `Pickup_Battery` (지금 **나무상자**)
- 부품 분리: 불필요
- 비고: 손전등 배터리를 줍는 것이라 여러 곳에 놓인다. 나무상자로는 무엇을 줍는지 모른다

### A7. 유기물 덩어리 `NestGrowth` — 우선순위 A

    Cluster of organic growth for a wall and ceiling, thick fibrous strands
    stretched between lumpy sacs, a few bulbous pods hanging low on cords,
    membranous webbing filling the gaps, wet ropey texture throughout. Dull
    dark red-brown flesh tones with paler sinew, damp sheen in the crevices.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals.

- 치수: **막은 2~5m, 늘어진 가닥은 1.5~4.5m** — `Ch4/Nest` 76 조각이 그 범위다.
  덩어리 하나를 3m 안팎으로 뽑아 스케일로 맞춘다
- 축: 붙는 면이 `-Z`, 위 `+Y` · 피벗: 붙는 면 중앙
- 바꿀 곳: `Ch4` `Nest` 76 조각 (지금 실린더 · 납작 큐브에 `W_nest`)
- 부품 분리: 불필요
- 비고: **이 장의 정체가 이 물건이다.** 표면(`nest` · `nest_floor` · `nest_fiber`)은
  이미 있으므로 여기서 얻을 것은 **덩어리진 형태**다. 원통은 촉수로 안 읽힌다.
  세 덩어리쯤 다르게 뽑아 섞으면 반복이 덜 보인다

### B1. 연료 탱크 `FuelTank` — 우선순위 B

    Large horizontal cylindrical fuel storage tank on two steel saddle
    cradles, a domed end cap with a bolted manway, a filler neck with a screw
    cap on top, a sight gauge tube down one side, an outlet valve and flange
    at the bottom. Worn matte grey-green paint over steel, heavy rust
    streaking below the fittings. Low-poly, game-ready, clean topology. No
    baked shadows, no baked ambient occlusion. No branding, no text, no decals.

- 치수: 길이 3.2m · 지름 1.4m
- 축: 축이 `+X`, 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: `Ch3` 갱도 아래
- 부품 분리: 불필요

### B2. 이송 펌프 `TransferPump` — 우선순위 B

    Skid-mounted industrial transfer pump, a squat electric motor coupled to a
    volute pump casing, suction and discharge flanges with short pipe stubs, a
    pressure gauge on the discharge side, a small terminal box on the motor,
    bolted to a low steel base. Worn matte grey-green paint over steel, thick
    ice crust over the casing and pipe stubs. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding, no
    text, no decals.

- 치수: 1.1 × 0.8 × 0.6m
- 축: 토출이 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 바꿀 곳: `Ch3` `Room_Pump/Pump` (지금 꺼진 큐브)
- 부품 분리: 불필요
- 비고: 기획은 「이송 펌프가 얼어 죽었다」이므로 **얼음이 형태의 일부**다. 얼음을
  프롬프트에 넣어 두면 「왜 못 쓰는가」가 화면에서 읽힌다

### B3. 경비 캐비닛 `GunCabinet` — 우선순위 B

    Narrow steel weapons cabinet, a tall thin locker with a single hinged door
    standing ajar, a keypad lock plate beside the handle, an interior rack with
    upright slots and a small shelf at the bottom, ventilation slits near the
    top. Worn matte grey-green paint over steel, bare metal at the edges, light
    rust at seams. Low-poly, game-ready, clean topology. No baked shadows, no
    baked ambient occlusion. No branding, no text, no decals.

- 치수: 0.5 × 1.9 × 0.4m
- 축: 문이 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: `Ch3` `Room_Guard` (`Spot_Cabinet` 자리가 이미 있다)
- 부품 분리: 불필요 (문은 열린 채로 뽑는다)
- 비고: **1장 근무표의 번호가 여기로 이어진다.** 키패드가 형태에 있어야 그 연결이
  화면에서 성립한다

### B4. 갱도 지지 프레임 `TunnelFrame` — 우선순위 B

    Mine tunnel support set, two steel H-section legs with an arched top beam
    bolted across them, tie plates at the joints, timber lagging boards packed
    between the beam and the rock above. Worn matte grey-green steel with heavy
    rust at the joints, weathered grey timber. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding, no
    text, no decals.

- 치수: 폭 3.0m · 높이 2.8m · 두께 0.3m
- 축: 통로 방향이 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: `Ch3` 갱도 통로에 일정 간격으로 반복
- 부품 분리: 불필요
- 비고: 3장을 1장 복도와 구분 짓는 가장 싼 방법이다. 표면(`rock_hewn`)과 짝이 된다

### B5. 손수레 `HandCart` — 우선순위 B

    Two-wheeled industrial hand truck, an L-shaped steel frame with a toe
    plate at the bottom, a ladder-style back frame, two solid rubber wheels on
    a fixed axle, a horizontal grab handle at the top. Worn matte grey-green
    paint over steel, bare metal at the wear points, dark rubber tyres.
    Low-poly, game-ready, clean topology. No baked shadows, no baked ambient
    occlusion. No branding, no text, no decals.

- 치수: 0.6 × 1.3 × 0.5m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: `Ch3` 창고 · 갱도
- 부품 분리: 불필요

### B6. 철탑 구간 `TowerSection` — 우선순위 B

    Section of a lattice steel mast, four corner angle-iron legs joined by
    diagonal cross bracing and horizontal ties, bolted gusset plates at every
    joint, a flanged splice at both open ends, a cable ladder running up one
    face. Galvanised steel with a dull spangled finish, rust bleeding from the
    bolt heads, snow crust on the upper surfaces. Low-poly, game-ready, clean
    topology. No baked shadows, no baked ambient occlusion. No branding, no
    text, no decals.

- 치수: **높이 4.0m** · 한 변 1.2m
- 축: 위 `+Y`, 앞 `+Z` · 피벗: 바닥 중앙 (쌓아 올린다)
- 놓을 곳: **5장** 철탑 셋 · 6장 삭도 지주
- 부품 분리: 불필요
- 비고: **격자 구조는 큐브로 못 만든다.** 5장은 철탑이 유일한 지형지물이라 여기가
  약하면 장 전체가 빈 눈밭이 된다. 4m 로 못 박아 쌓아 올린다. 표면은 `galvanized`

### B7. 유도등 `BeaconLight` — 우선순위 B

    Aviation obstruction beacon for a mast top, a squat cylindrical base
    housing with cooling fins, a domed glass lens cage above it protected by a
    heavy wire guard, a conduit stub and bracket underneath. Dark grey-green
    painted housing, almost black, with only the recessed lens lighter, ice
    crust on the guard. Low-poly, game-ready, clean topology. No baked shadows,
    no baked ambient occlusion. No branding, no text, no decals.

- 치수: 0.35 × 0.45 × 0.35m
- 축: 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: **5장** 철탑마다 하나
- 부품 분리: 불필요
- 비고: 기획의 「하나를 켜면 다음 철탑이 흐릿하게 보인다. 그 불빛만이 방향이다」가
  이것이다. **몸체는 어둡게 뽑는다** — 빛은 `Light` 와 발광 카드(`MAT_GuideGlow`)가
  내므로 알베도가 밝으면 낮에도 켜진 것처럼 보인다

### B8. 로터리 제설기 `SnowBlower` — 우선순위 B

    Walk-behind rotary snow blower, a wide auger housing at the front with a
    spiral auger inside and a discharge chute curving up from the top, a boxy
    engine deck behind it, two ribbed crawler tracks, long handlebars with
    control levers at the rear. Worn matte grey-green paint over steel, bare
    metal at the auger edges, packed snow in the housing. Low-poly, game-ready,
    clean topology. No baked shadows, no baked ambient occlusion. No branding,
    no text, no decals.

- 치수: 1.3 × 1.2 × 1.9m
- 축: 앞 `+Z`, 위 `+Y` · 피벗: 바닥 중앙
- 놓을 곳: **5장** 두 번째 철탑 아래
- 부품 분리: **오거(나선 날).** 돌아야 한다. 프롬프트에
  `the auger sitting inside the housing with a visible air gap around it, not
  fused to the shroud` 를 덧붙인다
- 비고: 기획의 **움직이는 엄폐물**이다. 「제설기 뒤에 붙어 있는 동안은 안전하지만
  느리고 소리가 크다」 — 날이 도는 것이 보여야 「닿으면 갈린다」가 성립한다

### B9. 안테나 접시 `AntennaDish` — 우선순위 B

    Parabolic microwave antenna on a mast bracket, a shallow solid dish with a
    rolled rim, a feed horn on three struts at the focus, a tilt adjustment
    strut behind, a pipe clamp mount. Worn matte grey-green paint over steel,
    rust at the clamp, ice crust down one side of the dish. Low-poly,
    game-ready, clean topology. No baked shadows, no baked ambient occlusion.
    No branding, no text, no decals.

- 치수: 지름 1.2m · 깊이 0.5m
- 축: 접시가 보는 쪽이 `+Z`, 위 `+Y` · 피벗: 클램프 중앙
- 놓을 곳: **5장** 철탑 · 1장 옥상
- 부품 분리: 불필요
- 비고: 여기가 **통신 중계소**라는 것을 화면에서 말하는 유일한 물건이다. 기획의
  제목과 무대가 그것인데 지금 씬 어디에도 안테나가 없다

## 2차 임포트 결과 (2026-08-09) — 29 종 받아 25 종을 넣었다

규약은 1차와 같다. 축 · 크기 · 피벗을 정점에 굽고 FBX 는 지웠다(220 MB). 텍스처는
파일 자체를 512 로 줄여 base color 와 normal 만 남겼다. `Props/` 가 40 MB 에서
71 MB 가 됐다.

IMPORTANT: `.gitignore` 가 새 폴더를 못 막고 있었다. `**/ModelSources/meshygen/` 이라
`meshygen_new` 가 빠져 **zip 600 MB 가 커밋 대상**이었다. `meshygen*` 로 넓혔다.
다음에 폴더 이름을 바꿔 받을 때 같은 일이 난다.

### 두 번 구웠다 — Meshy 는 앞 방향을 안 지킨다

첫 굽기에서 **배관 · 트레이 · 발전기의 길이가 Z 가 아니라 X** 로 나왔다. 케이블
트레이가 4m 가 아니라 8.7m 가 됐고, 이송 펌프는 15 × 0.8 × 15m 였다.

Meshy 는 Z-up 이라 X 로 −90° 를 먹이면 위아래는 맞지만 **앞뒤 좌우는 생성물마다
다르다.** 그래서 축을 고정값으로 쓰면 안 된다. 긴축을 재서 의도와 다르면 Y 로 90° 를
더 먹이도록 고쳤다.

### 레버는 평면 절단으로 나눴다

덩어리 분리를 먼저 시도했으나 **한 덩어리라 틈이 없었다.** 삼각형 분포를 y 로 훑으니
0.23 에서 1264 → 291 → 55 로 끊겨 거기가 받침과 팔의 경계였다. **0.215 에서 자르고
회전축을 0.195 에 두었다.** 실제로 `-55°` 돌려 확인했고 절단면은 안 보인다.

IMPORTANT: 씬 20 곳이 옛 이름(`WallLever_Body` · `WallLever_Handle`)을 참조하고 있어
**새 메시를 그 이름에 부어 넣었다.** 새 파일로 두면 씬은 옛 형상에 새 텍스처를 물어
UV 가 어긋난 채 남는다. 이름을 지키는 것이 교체의 핵심이다.

같은 이유로 다시 뽑은 여덟(`Extinguisher` · `JunctionBox` · `Mug` · `EmergencyLight` ·
`Basin` · `Logbook` · `CableTray` · `WallLever`)은 **씬을 안 건드려도 자동으로 새 모델이
됐다.** 에셋 이름과 GUID 를 유지했기 때문이다.

### 씬에서 갈아끼운 것

| 어디 | 무엇 | 지금까지 |
| --- | --- | --- |
| `Ch4` 홀 | 발전기 모듈 5 | `MachineBlock` |
| `Ch4` 정비구 · 축전지실 | 배전반 2 | `JunctionBox` (0.45m 짜리) |
| `Ch4` 축전지실 북벽 | 환기 팬 | **메시가 없었다** |
| `Ch3` | 배터리 픽업 | `Crate` (나무상자) |
| `Ch6` | 삭도 권양기 | `MachineBlock` 2 + **연료 드럼통** |

넣고 나서 넷을 고쳤다. **발전기는 모듈 길이가 Z 인데 오브젝트가 90° 돌아 있어** 네
개가 한 줄로 안 붙고 나란히 섰다 — `rotY` 를 0 으로 내렸다. **배전반은 2m 짜리
바닥형인데 피벗이 뒷면 «중앙»** 이라 0.6m 떠 있었다 — y 를 1.00 으로 내려 바닥에
앉혔다. **환기 팬은 피벗이 뒷면이라 +Z 로 뻗어 벽을 뚫고 나갔다** — 180° 돌려 방
안으로 넣었다. **배터리는 나무상자 기준 0.34 배** 스케일이 남아 있어 10cm 로 줄어
있었다 — 1.0 으로 되돌렸다.

**대역을 갈아끼울 때는 스케일과 회전이 옛 메시에 맞춰져 있다.** 메시만 바꾸고 끝내면
크기와 방향이 조용히 틀어진다.

### 다시 뽑아야 하는 넷

| 무엇 | 나온 것 |
| --- | --- |
| `BatteryRack` 축전지 랙 | **뼈대만 남은 파편.** 셀이 없다 |
| `TransferPump` 이송 펌프 | **납작한 원판.** 15 × 0.8 × 15m |
| `PipeElbow` 배관 엘보 | **굽지 않은 곧은 관.** 90° 가 없다 |
| `NestGrowth` 유기물 | **위에서 본 접시.** 벽에 붙는 막이 아니다 |

반쯤 된 것을 남기지 않으려고 폴더째 지웠다. 프롬프트는 위 3차 목록에 그대로 있다.

`DieselGenerator_Whole`(통짜 발전기)도 지웠다. 4장이 모듈판을 쓰기로 해서 중복본이
됐다. 원본 zip 은 뽑아 온 사람의 로컬에 남아 있다.

### 아직 배치 안 한 11 종 — 할 일

IMPORTANT: 그중 **5장 것 넷**(`TowerSection` · `BeaconLight` · `SnowBlower` ·
`AvalancheCannon`)의 치수 · 피벗과 5장을 지을 때의 함정은
[`chapter-atmosphere.md`](chapter-atmosphere.md) 「5장을 지을 때」에 모아 두었다.


레벨이 없거나 아직 손이 안 간 자리다. **에셋은 들어와 있으므로 배치만 하면 된다.**

| 소품 | 어디에 | 왜 아직인가 |
| --- | --- | --- |
| `FuelCan` 연료통 | 3장 펌프실 · 6장 권양기 | 3장 연료 절차가 미구현 |
| `FuelTank` 연료 탱크 | 3장 갱도 아래 | 〃 |
| `GunCabinet` 경비 캐비닛 | 3장 `Room_Guard` (`Spot_Cabinet` 자리가 있다) | 배치만 하면 된다 |
| `TunnelFrame` 갱도 지지 프레임 | 3장 통로 반복 | 〃 |
| `HandCart` 손수레 | 3장 창고 · 갱도 | 〃 |
| `PipeStraight` 배관 4m 모듈 | 전 장 | **`PipeRun` 73 개를 갈아끼우는 일이라 레벨 작업이다.** 크기가 0.22 × 0.35 × 0.79 에서 1.20 × 0.95 × 4.00 으로 바뀌어 그대로 못 바꾼다 |
| `TowerSection` 철탑 구간 | **5장** 철탑 셋 · 6장 지주 | 5장 씬이 아직 없다 |
| `BeaconLight` 유도등 | **5장** 철탑마다 | 〃 |
| `SnowBlower` 로터리 제설기 | **5장** 두 번째 철탑 아래 | 〃 |
| `AvalancheCannon` 제설포 | **5장** · 6장 피날레 | 〃 |
| `AntennaDish` 안테나 접시 | 5장 철탑 · **1장 옥상** | 1장은 지금 바로 놓을 수 있다 |

IMPORTANT: **`AntennaDish` 는 1장 옥상에 지금 놓을 수 있다.** 여기가 통신 중계소라는
것을 화면에서 말하는 유일한 물건인데 아직 어느 씬에도 없다.

## 후순위 — 그 장이 생긴 뒤에 만든다

레벨이 없으면 크기도 배치도 정할 수 없다.

IMPORTANT: 1 · 2 · 6장을 실제로 재보고 나온 추가 항목이
[`chapter-atmosphere.md`](chapter-atmosphere.md) 「콘텐츠 · 모델로 넘길 것」에 있다.
6장 **제설포**와 1장 **움직이는 계기**가 거기 있고, 둘 다 이 목록에 없다.

| 소품 | 어느 장 | 왜 미루나 |
| --- | --- | --- |
| ~~권총 (슬라이드 · 탄창 분리)~~ | 3장 | **들어왔다** — `Player/Models/Pistol.fbx` |
| ~~산탄총 (펌프 분리)~~ | 5장 | **들어왔다** — `Player/Models/Shotgun.fbx` |
| ~~연료통~~ | 3장 · 6장 | **3차 A1** |
| ~~배터리~~ · 탄약 상자 | 3장~ | 배터리는 **3차 A6**. 탄약은 `PistolMag` 픽업이 맞아 뺐다 |
| ~~갱도 지지 프레임 · 손수레~~ | 3장 | **3차 B4 · B5** |
| ~~디젤 발전기 · 배전반 · 축전지 랙 · 환기 팬~~ | 4장 | **레벨이 생겼다. 치수는 아래에** |
| ~~유기물 데코 (둥지)~~ | 4장 | **같음** |
| ~~철탑 구조재 · 안테나 접시 · 로터리 제설기~~ · ~~제설포~~ | 5장 | **3차 B6 · B9 · B8 · 2차 N2** |

**이 표는 이제 비었다.** 남은 미해결은 1장 「움직이는 계기」뿐이고 그것은 모델이
아니라 회전 · 점멸 스크립트다 ([`chapter-atmosphere.md`](chapter-atmosphere.md)).

### 4장 전용 5종 — 자리를 세워 치수를 잡았다 (2026-08-09)

IMPORTANT: **이 표의 치수가 3차 프롬프트의 A2~A5 · A7 에 반영되어 있다.** 처음에는
이 표를 안 읽고 상상으로 치수를 써서 발전기를 14m 가 아니라 3m 로, 축전지 랙을 8m 가
아니라 2m 로, 환기 팬을 지름 2.6m 가 아니라 1.4m 로 적었다. **씬에 대역이 서 있으면
그것을 재는 것이 항상 먼저다.**

`Ch4.unity` 를 지으면서 **기존 30종으로 자리를 대신 세웠다.** 아래 치수는 상상이
아니라 씬에 실제로 서 있는 물건의 크기다. 뽑아 오면 그 자리를 갈아끼운다.

| # | 이름 | 치수 (W × H × L) | 축 · 피벗 | 지금 그 자리에 선 것 | 어디 |
| --- | --- | --- | --- | --- | --- |
| 31 | 디젤 발전기 `Generator` | 2.40 × 3.00 × **14.0** | 길이 `+Z` · 바닥면 중앙 | `MachineBlock` 4 + 0.72 배 1 | `Ch4` 홀 `Props/Generator` |
| 32 | 배전반 `Switchboard` | 1.00 × 2.00 × 0.35 | 앞 `-Z` · 벽에 닿는 뒷면 | `JunctionBox` (0.80 × 0.90 × 0.72) | 정비구 서벽 · 축전지실 서벽 |
| 33 | 축전지 랙 `BatteryRack` | 8.00 × 0.95 × 1.40 (8 칸) | 앞 `-Z` · 바닥면 중앙 | 받침 큐브 + `Crate` 8 | 축전지실 `Props/Battery` |
| 34 | 환기 팬 `VentFan` | 지름 2.60 · 두께 0.35 | 축이 `+Z` · **하우징 뒷면** | 큐브 · 실린더 조합 | 축전지실 북벽 · 홀 북벽(3.4 배) |
| 35 | 유기물 데코 `NestSheet` | 막 2~5m · 가닥 1.5~4.5m | — | 실린더 · 납작 큐브에 `W_nest` | `Ch4/Nest` 76 조각 |

IMPORTANT: **환기 팬은 날개를 반드시 나눠 뽑는다.** `Blades` 가 따로 있어야
`Spin` 이 그것만 돌린다. 하우징까지 같이 돌면 벽이 돈다. 지금 조합품은 그렇게
나뉘어 있다.

**발전기는 14m 짜리 통짜로 뽑지 않는다.** Meshy 가 그 비율을 못 지키고, 길면
텍스처가 늘어난다. **3.5m 모듈 하나를 뽑아 네 번 이어 붙인다** — 지금 자리가
그렇게 서 있다. 배관 · 밸브 · 드럼은 기존 것을 그대로 얹는다.

배전반은 지금 세워둔 `JunctionBox` 보다 **크고 세로로 긴 것**이 맞다. 시안의
축전지실 벽면이 계기로 덮여 있다. 다만 `JunctionBox` 는 2장에서 배우는 물건이라
**둘을 다른 물건으로 만든다** — 같은 모델을 키우면 「2장에서 본 그 배전함」으로
읽혀 4장이 새 장소로 안 보인다.

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
