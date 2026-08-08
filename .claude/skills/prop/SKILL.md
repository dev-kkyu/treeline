---
description: 소품 모델을 Meshy 로 조달하는 절차를 진행한다. 장별 목록을 뽑고, Meshy 에 넣을 영문 프롬프트를 쓰고, 사람이 만들어 온 모델을 Unity 에 임포트해 후처리한다. 소품이 필요하거나 모델을 받아왔을 때 사용한다.
---

# 소품 조달하기

레버 · 밸브 · 계기판 · 무전기 같은 **정적 소품**을 만드는 절차다. 적은 여기 해당하지
않는다 — 적은 [`docs/enemy-sheets/모델링-시트.md`](../../../docs/enemy-sheets/모델링-시트.md)
의 시트 방식을 쓴다.

방침의 근거는
[`docs/tech-design/world-assets.md`](../../../docs/tech-design/world-assets.md) 에 있다.

## 누가 무엇을 하나

```
① 목록 뽑기      세션    docs/game-design/treeline.md 에서 장별로 훑는다
② 프롬프트 쓰기   세션    영문 텍스트 + 임포트용 메모
③ 모델 만들기     사람    Meshy text-to-3D
④ 임포트 후처리   세션    MCP 로 Unity 에서
```

**②와 ④가 이 스킬이 하는 일이다.** ③은 사람이 하고, 결과를 받아 ④로 온다.

IMPORTANT: **모델링 시트(3면도)를 만들지 않는다.** 시트는 적 3종의 실루엣을 구분해야
해서 필요했던 것이고, 소품은 서로 헷갈릴 일이 없다. 텍스트 프롬프트로 충분하다.

---

## ① 목록 뽑기

`treeline.md` 의 「여섯 장소」와 「산과 중계소에서 나오는 장치」를 장별로 훑는다.
**이미 씬에 회색 박스로 서 있는 것**이 우선이다 — 그것들이 지금 화면에서 제일 초라하다.

목록은 사람이 확정한다. 세션은 초안만 낸다.

## ② 프롬프트 쓰기

물건 하나가 아래 한 덩어리다. **프롬프트는 영문, 메모는 한국어**로 쓴다.

```markdown
### 손전등 `Flashlight`

    Rugged industrial handheld flashlight, cylindrical aluminium body with
    three knurled grip rings, wider ribbed head, flat glass lens, side push
    switch, lanyard lug at the tail. Worn matte grey-green paint with bare
    metal showing at the edges. Low-poly, game-ready, clean topology.
    No baked shadows, no baked ambient occlusion. Neutral grey-green only,
    no branding, no text, no decals.

- 치수: 길이 0.22m
- 축: 앞 `+Z`, 위 `+Y`
- 피벗: 손잡이 중앙
- 용도: 1장에서 줍는다. 1인칭 손에 들리고 바닥에도 놓인다
- 부품 분리: 불필요 (움직이는 곳 없음)
```

메모 네 줄은 **프롬프트용이 아니라 ④를 위한 것**이다. Meshy 는 스케일 감각이 없어서
치수를 써도 지키지 않는다. 그 숫자가 있어야 임포트에서 맞출 수 있다.

### 반드시 넣을 것

임포트 뒤에는 **형태 · 폴리곤 · UV 를 고칠 수 없다.** 되돌릴 수 없는 것은 입구에서
막는다.

| 넣는 말 | 왜 |
| --- | --- |
| `low-poly, game-ready, clean topology` | 폴리곤은 사후 감소가 안 된다 |
| `no baked shadows, no baked ambient occlusion` | 알베도에 그림자가 구워지면 손전등과 싸운다 |
| `no branding, no text, no decals` | 글자가 들어가면 시대 · 국적이 특정된다 |
| 색을 한두 개로 못 박기 | 생성물마다 색이 튀면 한 세계로 안 보인다 |

### 움직이는 부품이 있으면

**나눠 뽑는다.** 통짜로 오면 못 나눈다. 슬라이드 · 탄창 · 뚜껑처럼 움직여야 하는
것은 별도 모델로 요청하고, 메모에 무엇이 어떻게 움직이는지 적는다.

리깅은 대개 필요 없다. 트랜스폼을 옮기는 것으로 반동 · 장전 · 여닫이가 다 된다.
손가락이 물건을 쥐는 것을 보여줄 때만 리깅이 필요하고, **손을 화면에 안 보여주면
그 경우도 사라진다.**

---

## ④ 임포트 후처리

모델을 받으면 `Treeline/Assets/Game/Props/<이름>/` 에 넣고 아래를 순서대로 한다.
전부 MCP 로 한다.

**1. 텍스처 임포트 설정**

| 맵 | 설정 |
| --- | --- |
| albedo | sRGB 켬, 최대 1024 (소품은 512) |
| normal | `TextureImporterType.NormalMap` |
| roughness · metallic · AO | **sRGB 끔** — 색이 아니라 데이터다 |

Meshy 기본이 2048 이다. 그대로 두면 `Crawler` 처럼 **모델 하나가 10 MB** 를 먹는다.

**2. 축 · 스케일 · 피벗**

메모의 치수로 맞춘다. 앞이 `+Z`, 위가 `+Y`. 피벗이 엉뚱하면 빈 부모로 감싸 옮긴다.

**3. 머티리얼**

Meshy 가 준 텍스처를 쓰되 파라미터는 우리 기준으로 덮는다. 다른 소품과 나란히 놓고
튀지 않는지 본다.

**4. 콜라이더 · 프리팹**

박스 콜라이더를 붙인다. **메시 콜라이더는 쓰지 않는다** — 비싸고 NavMesh 굽기도
나빠진다. 줍는 물건이면 `Pickup`, 조작하는 물건이면 `Lever` 등을 붙여 프리팹으로
만든다. 계약은
[`docs/tech-design/architecture.md`](../../../docs/tech-design/architecture.md) 를 따른다.

**5. 확인**

`manage_camera` 로 손전등만 켠 조건에서 찍어 본다. 천장 조명을 켜고 판단하지 않는다.
**이 게임이 실제로 보이는 조건에서 봐야 한다.**

---

## 씬에 이미 놓인 큐브를 교체하기

레버 · 계기판 · 읽을거리는 지금 큐브로 서 있다. 모델이 오면 바꾼다.

**오브젝트를 지우지 않는다.** 메시만 갈아끼운다. 씬의 상호작용물은 로직 · 콜라이더 ·
메시가 한 GameObject 에 같이 있어서, 오브젝트를 남기면 참조가 전부 살아 있다.

```
Pickup_Flashlight  [Transform, MeshFilter, BoxCollider, MeshRenderer, Pickup]
                              ^^^^^^^^^^                ^^^^^^^^^^^^  여기만 바꾼다
```

| 바꾸는 것 | 그대로 두는 것 |
| --- | --- |
| `MeshFilter.sharedMesh` | `Pickup` · `Readable` · `Lever` 등 로직 |
| `MeshRenderer.sharedMaterials` | `BoxCollider` (**메시 콜라이더로 바꾸지 않는다**) |
| `Transform.localScale` (실제 치수로) | 오브젝트 이름 · 부모 · 씬에서의 위치 |

IMPORTANT: **콜라이더 크기를 새 메시에 맞춘다.** 큐브 기준으로 잡혀 있어서, 모델이
더 작거나 크면 손이 안 닿거나 허공에서 눌린다. 바꾼 뒤 실제로 플레이해서 눌러 본다.

피벗이 다르면 위치가 튄다. 그때는 오브젝트를 옮기지 말고 **빈 자식을 만들어 메시를
그 안에 넣고 자식만 오프셋**한다. 씬의 위치는 레벨 설계가 정한 값이라 건드리면 안 된다.

### 벽 · 바닥은 교체가 아니다

복도 벽을 육면체로 깔아둔 것은 **임시 대역이 아니라 레벨 그 자체**다. 모델로 바꾸지
않고 **표면만 입힌다.** 근거는
[`docs/tech-design/world-assets.md`](../../../docs/tech-design/world-assets.md) 이고,
작업은 `plan.md` 의 2-1b 다.

| | 무엇을 하나 |
| --- | --- |
| 소품 (레버 · 계기판 · 무전기) | 메시를 **교체**한다 |
| 구조 (벽 · 바닥 · 천장 · 계단) | 지오메트리는 그대로. **머티리얼만** 바꾼다 |

## 기록

IMPORTANT: 모델을 받은 **그 자리에서** [`docs/assets.md`](../../../docs/assets.md) 의
「AI 로 생성한 것」에 한 줄 적는다. 도구 이름과 생성일이면 충분하다. 나중에 몰아서
쓰려 하면 복원할 수 없고, 제출물인 "AI 활용 기술 문서" 의 재료다.

## 하지 말 것

- **에셋스토어 팩을 쓰지 않는다.** 저장소에 못 올려서 셋이 각자 임포트해야 하고,
  한 명만 버전이 달라도 씬 참조가 조용히 어긋난다
- **텍스처를 Meshy 로 만들지 않는다.** 벽 · 바닥 같은 타일링 표면은 생성물로
  안 된다. `docs/world-textures/gen_textures.py` 를 쓴다
- **모델 하나 때문에 레벨 구조를 바꾸지 않는다.** 1 · 2 · 6 은 관통이 검증된
  상태다
