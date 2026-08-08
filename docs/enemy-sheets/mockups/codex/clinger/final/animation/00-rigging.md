# 00 · 리깅 명세

## 좌표·원점·두 공간

- Blender는 Z-up, 1 unit = 1 m, 모델 전방은 -Y. FBX는 `-Z Forward / Y Up`,
  Apply Transform, scale 1.0으로 내보내 Unity 로컬 +Z가 쐐기 머리 방향이 되게 한다.
- Armature와 Mesh transform은 위치 0, 회전 0, 스케일 1. 상쇄용 ±90° 회전,
  음수 스케일, 두 번째 export root를 남기지 않는다.
- `Root`는 천장 접점 공간, `GroundPivot`은 착지 후 바닥 접점 공간이다. 둘 다
  non-deform이며 위치 이동을 메시 변형에 직접 쓰지 않는다.
- 컨트롤 리그에는 `CTRL_CeilingSpace`, `CTRL_GroundSpace`, `CTRL_Body`를 두고
  `BodySpace` 속성으로 몸통 전체가 어느 공간을 따를지 전환한다.
- `Unfold_Drop` 10f 전에는 Ceiling 1.0, 공중에서는 Root/Object 공간,
  `Land` 15f에는 Ground 1.0이 되게 행렬을 맞춘 뒤 베이크한다. 전환 프레임에
  몸통이 튀면 안 된다.
- Root Motion은 전 클립에서 끈다. 낙하 거리와 NavMesh 이동은 코드가 담당한다.

## Export 본 · 총 26개

`CTRL_`, `MCH_`, IK target, pole은 non-deform이며 FBX에서 제외한다.

```text
Root                                      non-deform 1, ceiling/object root
└─ GroundPivot                            non-deform 2, landed ground reference
   └─ DEF_TorsoBase                       deform 1
      ├─ DEF_TorsoMid                     deform 2
      │  └─ DEF_TorsoTip                  deform 3
      │     └─ DEF_Head                   deform 4
      │        └─ DEF_Jaw                 deform 5
      ├─ DEF_CeilUpper.LF                 deform 6
      │  └─ DEF_CeilLower.LF              deform 7
      ├─ DEF_CeilUpper.LR                 deform 8
      │  └─ DEF_CeilLower.LR              deform 9
      ├─ DEF_CeilUpper.RF                 deform 10
      │  └─ DEF_CeilLower.RF              deform 11
      ├─ DEF_CeilUpper.RR                 deform 12
      │  └─ DEF_CeilLower.RR              deform 13
      ├─ DEF_AttackUpper.L                deform 14
      │  └─ DEF_AttackFore.L              deform 15
      │     └─ DEF_AttackHand.L            deform 16
      │        └─ DEF_DigitsGroup.L        deform 17
      ├─ DEF_AttackUpper.R                deform 18
      │  └─ DEF_AttackFore.R              deform 19
      │     └─ DEF_AttackHand.R            deform 20
      │        └─ DEF_DigitsGroup.R        deform 21
      ├─ DEF_FoldBody                     deform 22
      ├─ DEF_FoldArm.L                    deform 23
      └─ DEF_FoldArm.R                    deform 24
```

`LF/RF`는 모델 전방(+Z), `LR/RR`는 뒤쪽(-Z) 천장 사지다. 착지 뒤에는 LR/RR을
뒷 지지점으로 쓰고 LF/RF는 등 위로 접는다. 갈고리 2개는 각 `CeilLower`에,
손가락 3개는 각 `DigitsGroup`에 함께 스키닝해 개별 본을 추가하지 않는다.

## 컨트롤 리그

- `CTRL_Master`, `CTRL_Root`, `CTRL_COG`, `CTRL_TorsoBase/Mid/Tip`,
  `CTRL_Head`, `CTRL_Jaw`를 둔다. 액션의 Master/Root 이동과 scale 키는 항상 0/1이다.
- 천장 사지 4개에 `CTRL_CeilIK.*`와 pole 4개를 둔다. Cling 상태의 갈고리 끝은
  Ceiling Space IK로 고정하고 전개·공중·지상에서는 Root/Ground space로 전환한다.
- 공격 팔 2개는 Upper/Fore 2본 IK + Hand 회전 + DigitsGroup 굽힘으로 구성한다.
  `CTRL_AttackIK.L/R`과 pole을 두고 Scuttle 접지 구간만 World/Ground 고정한다.
- `FoldBody`는 몸통 아래 접힘 볼륨, `FoldArm.L/R`은 공격 팔이 몸통 옆에 감길 때
  팔뿌리 찌그러짐만 보정한다. 새 관절처럼 실루엣을 만들면 안 된다.
- 네 봉우리에는 별도 본이 없다. `TorsoMid`, 사지 뿌리, FoldBody의 작은 위상차로
  Tremor를 만든다.

## 본 롤과 회전 제한

- 모든 사지 본의 로컬 Y를 자식 방향, 로컬 X를 주 굽힘축으로 정렬한다.
  좌우 본은 같은 roll을 복사해 mirror하고 pole이 Rest에서 이미 5° 이상 꺾이게 한다.
- 천장 Upper: 굽힘 0~115°, 벌림 ±32°, twist ±18°.
- 천장 Lower: 굽힘 5~135°, 갈고리 방향 roll ±15°.
- 공격 Upper: 굽힘 -25~105°, 벌림 ±40°, twist ±25°.
- 공격 Fore: 굽힘 0~125°, Hand pitch ±35°, roll ±18°.
- DigitsGroup: 닫힘 0°, 최대 32°. 손가락끼리 따로 움직이지 않는다.
- TorsoBase/Mid/Tip: 각 pitch ±10°, yaw ±12°, roll ±9°. 세 본에 곡률을 분산한다.
- Head: pitch ±14°, yaw ±16°, roll ±8°. Jaw는 0~36°, 좌우 비틀기 금지.

축의 +/− 부호는 실제 roll에 따라 바뀔 수 있으므로 숫자 부호보다 해부학적 굽힘
방향과 제한각을 우선한다.

## 스키닝

- 버텍스당 최대 4 weights, 0.01 미만 제거, Normalize All. Unity는 4 Bones.
- 심장형 몸통 큰 면은 TorsoBase/Mid/Tip 세 본에 길이 방향으로 70/25/5 →
  20/60/20 → 5/25/70 비율로 넘기고 좌우가 젤리처럼 따로 흔들리지 않게 한다.
- 쐐기 머리는 Head 0.85 이상으로 강체에 가깝게 묶고 목 경계 2~3루프만 TorsoTip과 섞는다.
- 입 3루프는 Head/Jaw를 80/20, 45/55, 10/90으로 시작한다. 36°에서 이빨과 입술
  관통이 생기면 각도를 늘리지 말고 weight를 고친다.
- 사지 관절 중심 루프는 부모/자식 50/50, 인접 루프는 75/25. 봉우리 외곽이
  납작해지면 corrective shape key를 쓰고 FBX에 bake한다.
- 갈고리와 손가락 끝은 Lower/DigitsGroup 90% 이상. 개별 발톱이 고무처럼 휘지 않는다.
- Fold 보조본 영향은 해당 접힘 안쪽 2~3루프에만 두고 한 vertex 0.35를 넘기지 않는다.

## 리그 검수 포즈

1. 갈고리 8개를 동일 천장 평면에 고정하고 몸통을 ±0.15 m 움직여 IK/pole을 확인한다.
2. 공격 팔을 몸통에 감아 전체 접힌 바운드 `0.88×0.72×0.62 m` 안에 들어가는지 본다.
3. 전개 높이 2.51 m와 공격 팔 길이 1.39 m를 재고 좌우 오차를 3 mm 이하로 맞춘다.
4. GroundPose에서 공격 손 2 + LR/RR 갈고리 2가 같은 바닥면에 닿는지 확인한다.
5. Jaw 36°, Digits 32°, 모든 사지 최대 굽힘에서 메시 관통과 normal 뒤집힘을 확인한다.
6. Ceiling→Object→Ground 공간 전환 전후 같은 프레임의 월드 행렬 차가 2 mm/1° 이하인지 본다.
