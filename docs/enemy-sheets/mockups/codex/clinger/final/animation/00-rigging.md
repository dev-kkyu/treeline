# 00 · 리깅 명세

상태: 2026-08-09 개정. 실제로 만든 리그(`ModelSources/Clinger/build_rig.py`)에 맞춤.

## 좌표·원점·두 공간

- Blender는 Z-up, 1 unit = 1 m, 모델 전방은 -Y. FBX는 `-Z Forward / Y Up`,
  scale 1.0으로 내보내 Unity 로컬 +Z가 쐐기 머리 방향이 되게 한다.
- Armature와 Mesh transform은 위치 0, 회전 0, 스케일 1.
- **원점은 천장 접점이다.** 생성 메시를 자기 높이(2.510 m)만큼 내려 갈고리 끝이
  z = 0에 오게 하고, 몸은 음수 z에 매단다. 천장에 붙은 적의 Transform은 천장에
  있어야 배치가 자명하다.
- `GroundPivot`이 지상 클립에서 몸 전체를 원점 위로 되올린다. 착지 클립에서는
  Transform이 다시 발밑에 있게 되므로 `NavMeshAgent`가 그대로 쓸 수 있다.

### 공간 전환은 `Land` 시작에서 한 번 한다

초판은 `Land` 15f에서 천장 공간과 지상 공간의 월드 행렬을 맞춘 뒤 전환하게 했다.
같은 액션 안에서 2 m 짜리 평행이동을 프레임 단위로 이어붙이는 일이고, 한 프레임만
어긋나도 렌더가 튄다.

개정: **`Land` 클립 전체를 지상 공간으로 만든다.** SphereCast가 바닥을 찾은
순간 코드가 Transform을 그 접점으로 옮기고 `Land`를 시작한다. `Land` 1f의 네 접지
포즈가 곧 그 접점이므로 화면에서는 튀지 않는다. 15f는 **충돌체와 Agent를 켜는
프레임**이라는 뜻만 남고, 행렬 맞추기는 사라진다.

- Root Motion은 전 클립에서 끈다. 낙하 거리와 NavMesh 이동은 코드가 담당한다.

## Export 본 · 총 26개

`CTRL_`은 non-deform이며 FBX에서 제외된다(`use_armature_deform_only`).
`Root`와 `GroundPivot`은 non-deform이지만 deform 자식이 있어 남는다.

```text
Root                                      non-deform 1, ceiling/object root
└─ GroundPivot                            non-deform 2, landed ground reference
   └─ DEF_TorsoBase                       deform 1   사지 뿌리가 붙는 등 쪽 끝
      ├─ DEF_TorsoMid                     deform 2
      │  ├─ DEF_TorsoTip                  deform 3
      │  │  └─ DEF_Head                   deform 4
      │  │     └─ DEF_Jaw                 deform 5
      │  ├─ DEF_FoldBody                  deform 6   배 쪽으로 뻗은 삼킴 본
      │  ├─ DEF_AttackUpper.L             deform 7
      │  │  └─ DEF_AttackFore.L           deform 8
      │  │     └─ DEF_AttackHand.L        deform 9
      │  │        └─ DEF_DigitsGroup.L    deform 10
      │  ├─ DEF_AttackUpper.R             deform 11 … 14
      │  ├─ DEF_FoldArm.L                 deform 15
      │  └─ DEF_FoldArm.R                 deform 16
      ├─ DEF_CeilUpper.LF                 deform 17
      │  └─ DEF_CeilLower.LF              deform 18
      ├─ DEF_CeilUpper.RF                 deform 19, 20
      ├─ DEF_CeilUpper.LR                 deform 21, 22
      └─ DEF_CeilUpper.RR                 deform 23, 24
```

**공격 팔이 `TorsoBase`가 아니라 `TorsoMid` 자식인 이유.** 팔뿌리는 실측 z 1.33 로
`TorsoMid`(1.40)와 `TorsoTip`(1.05) 사이에 있다. `TorsoBase`에 매달면 몸통이 말릴 때
팔만 제자리에 남아 두 마리처럼 읽힌다. 개정판의 몸통 진폭에서는 이 차이가 크다.

`LF/RF`는 모델 전방(-Y), `LR/RR`는 뒤쪽(+Y) 천장 사지다. 착지 뒤에는 LR/RR을
뒷 지지점으로 쓰고 LF/RF는 등 위로 접는다. 갈고리 2개는 각 `CeilLower`에,
손가락 3개는 각 `DigitsGroup`에 함께 스키닝해 개별 본을 추가하지 않는다.

## 컨트롤 리그 — IK 6개뿐

초판은 `CTRL_Master`부터 봉우리 컨트롤까지 20개 가까이 두게 했다. 손으로 만드는
리그가 아니라 스크립트가 포즈를 계산하는 리그라서, 실제로 필요한 것은 **월드
공간에 못 박아야 하는 접점**뿐이다.

| 컨트롤 | 대상 | 켜는 곳 |
| --- | --- | --- |
| `CTRL_CeilIK.{LF,RF,LR,RR}` | `DEF_CeilLower.*` 2본 체인 | 천장 4개, 지상은 뒤 2개만 |
| `CTRL_AttackIK.{L,R}` | `DEF_AttackFore.*` 2본 체인 | 지상 전 클립 |

- IK 영향력은 클립마다 키를 잡는다. 천장에서는 갈고리 4개가 1.0, 낙하 중에는
  전부 0.0, 지상에서는 손 2 + 뒤 갈고리 2가 1.0이다.
- pole target은 두지 않는다. 대신 각 체인의 두 번째 본만 `lock_ik_y/z` +
  `use_ik_limit_x`로 한 평면에 묶는다. pole 하나가 더 있으면 클립마다 pole도
  같이 키를 잡아야 하고, 그것이 flip의 흔한 원인이다.
- 뿌리 본(`CeilUpper`, `AttackUpper`)은 잠그지 않는다. 잠그면 어깨가 앞뒤로 못
  흔들려 `Scuttle`이 만들어지지 않는다.

## 본 롤 — 척추와 사지가 다른 축을 쓴다

IMPORTANT: 모든 본의 로컬 X를 주 굽힘축으로 삼되, **롤을 재는 기준축이 부위마다
다르다.**

| 부위 | 롤 기준 | 로컬 X가 향하는 곳 | `rot(x)`의 의미 |
| --- | --- | --- | --- |
| 척추·머리·턱·삼킴 본 | 월드 X | 월드 X | 앞뒤로 숙이고 젖힘 |
| 천장 사지·공격 팔·접힘 보조 | 월드 Y | 월드 Y | 사지 자기 평면에서 접힘 |

척추까지 월드 Y로 롤을 재면 로컬 X가 시상면 밖을 향해서, 몸통을 숙이려고 준 값이
몸통을 **옆으로 기울인다.** 팔이 0.3 m 씩 좌우로 밀려나는데 회전값만 보면 정상으로
보이므로 반드시 실측으로 확인한다.

### 좌우 미러 규칙은 통념과 반대다

두 쪽 본을 같은 월드 기준축으로 롤을 재면 `.L` 프레임은 `.R` 프레임과 **로컬 X와
Y가 뒤집힌** 관계가 된다. 그래서 포즈를 좌우 대칭으로 만들려면

> `rx`와 `ry`는 부호를 뒤집고, **`rz`는 그대로 둔다.**

`poses.py`의 `srot()` 하나만 이 규칙을 알고 있고, 다른 곳에서 좌우 부호를 손으로
쓰지 않는다. IK 각도 제한도 같은 이유로 좌우가 `(lo, hi) → (−hi, −lo)`다. 제한을
그대로 복사하면 양쪽이 서로 다른 깊이로 접힌다.

## 회전 제한

| 부위 | 초판 | 개정 | 비고 |
| --- | --- | --- | --- |
| 천장 Upper | 굽힘 0~115° | 0~115° | 유지 |
| 천장 Lower | 굽힘 5~135° | **−135~−5°** | 부호를 뒤집었다. 아래 참조 |
| 공격 Upper | -25~105° | -25~105° | 유지 |
| 공격 Fore | 0~125° | 0~125° | 유지 |
| DigitsGroup | 0~32° | **-30~40°** | 음수는 손가락이 젖혀지는 쪽 |
| Torso 3본 | 각 pitch ±10° | **각 pitch ±26°** | 세 본 합계 ±78° |
| Head | pitch ±14° | **pitch ±30°** | |
| Jaw | 0~36° | **0~62°** | 실측으로 관통 없음을 확인 |

**천장 Lower의 부호를 뒤집은 이유.** 양수 반쪽으로 두면 몸이 천장 쪽으로 올라올 때
관절이 갈고리보다 **위로** 꺾인다. 접힘 자세에서 0.45 m가 천장 안으로 들어간다.
음수 반쪽이면 팔꿈치가 바깥·아래로 접혀 컨셉 시트의 Z자 접힘이 나온다.

**턱 62°.** 초판은 36°에서 이빨·입술 관통을 걱정했다. 0/20/36/48/62/75°를 실제로
재봤고 62°까지 메시 최저점이 바뀌지 않는다(관통 없음). 이 개체가 가진 유일한 붉은
색이 입 안이라, 열리지 않으면 실루엣이 끝까지 검은 덩어리로만 읽힌다.

## 스키닝

- Blender `ARMATURE_AUTO`(heat weights)로 굽고, 버텍스당 4 weights로 제한한 뒤
  0.01 미만을 지우고 Normalize All. Unity는 4 Bones.
- 실측: 최대 weight 수 4, 빈 vertex group 0개.
- 손으로 칠하지 않는 이유는 리그가 스크립트 산출물이라 메시나 본 위치가 바뀌면
  다시 굽는 쪽이 싸기 때문이다. 대신 **굽고 나서 반드시 극단 포즈를 렌더로 본다.**
- 갈고리와 손가락 끝은 `CeilLower`/`DigitsGroup`이 90% 이상 가져간다.
- corrective shape key는 쓰지 않는다. 필요해지면 그때 만들고 FBX에 bake한다.

## 리그 검수 포즈

`preview.py`가 세 장을 뽑고 `rig_test.py`가 숫자를 뽑는다.

1. `folded` — 갈고리 8개 고정, 몸을 0.42 m 끌어올린 접힘. 바운드와 천장 관통량.
2. `hang` — 완전 전개. 좌우 대칭 오차와 전개 높이.
3. `stance` — 착지 4점. 손 2 + 뒤 갈고리 2의 접지 오차, 바닥 관통, 등 높이.

승인 기준:

- 좌우 대칭 오차 **0.000 m** (모든 `.R`/`.L` 본 tail, 미러 비교)
- 접지 IK 오차 **0.000 m** (손 2, 뒤 갈고리 2)
- 바닥 관통 **2 mm 이하**
- 천장 관통 **35 mm 이하** — 갈고리가 물어 들어간 만큼이라 0은 오히려 틀리다
