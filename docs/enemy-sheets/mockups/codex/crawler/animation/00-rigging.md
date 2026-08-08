# 00 · 리깅 명세

## 좌표와 중립 자세

- Blender는 Z-up, 1 unit = 1 m, 모델 전방은 **-Y**로 통일한다. FBX 변환 뒤에는
  Unity 모델 로컬 **+Z**가 실제 머리/진행 방향이어야 한다.
- 최종 FBX: `-Z Forward / Y Up`, Apply Transform 켬, scale 1.0. Unity 프리팹에서
  파란 Z 화살표가 머리 방향인지 반드시 확인한다.
- `Crawler_Root` 원점은 꼬리까지 포함한 바운드 중앙이 아니라 **몸통 네 발 접점의
  좌우 중앙, 바닥 높이**다. 꼬리가 길다고 원점을 뒤로 옮기지 않는다.
- Rest Pose는 `Idle_Low` 1f와 같게 한다. 머리 밑면은 바닥에서 약 0.12 m,
  최고 관절은 0.98 m 안, 네 발은 같은 바닥면에 둔다.
- Armature와 Mesh 오브젝트 transform은 위치 0, 회전 0, 스케일 1을 적용한다.
  Root 본이나 Armature에 상쇄용 ±90° 회전을 남기지 않는다.

## Export deform bone · 22개

`CTRL_`, `MCH_`, IK target, pole은 non-deform이며 FBX에 내보내지 않는다.

```text
Crawler_Root                         non-deform, ground root
└─ DEF_BodyRoot                      1
   ├─ DEF_SpineFront                 2
   │  └─ DEF_HeadPlate               3
   │     └─ DEF_Mouth                4
   ├─ DEF_Tail01                     5
   │  └─ DEF_Tail02                  6
   │     └─ DEF_Tail03               7
   │        └─ DEF_Tail04             8
   ├─ DEF_FrontUpper.L               9
   │  └─ DEF_FrontLower.L            10
   │     └─ DEF_FrontFoot.L           11
   ├─ DEF_FrontUpper.R               12
   │  └─ DEF_FrontLower.R            13
   │     └─ DEF_FrontFoot.R           14
   ├─ DEF_HindThigh.L                15
   │  └─ DEF_HindShin.L              16
   │     └─ DEF_HindAnkle.L           17
   │        └─ DEF_HindFoot.L         18
   └─ DEF_HindThigh.R                19
      └─ DEF_HindShin.R              20
         └─ DEF_HindAnkle.R           21
            └─ DEF_HindFoot.R         22
```

머리판과 가슴은 하나의 큰 덩어리처럼 보여야 하지만 공격구를 열기 위해
`HeadPlate`와 `Mouth`는 분리한다. 꼬리는 무기가 아니므로 4본이면 충분하다.
앞다리는 3본, Z자로 두 번 접히는 뒷다리는 4본을 써서 실루엣 차이를 유지한다.

## 컨트롤 리그

- `CTRL_Master` → `CTRL_Root` → `CTRL_COG` 계층을 둔다. 액션에서는
  `CTRL_Master`와 `CTRL_Root`의 위치/회전을 0으로 유지한다.
- 네 발에 `CTRL_FootIK.*`와 `CTRL_Pole.*`를 둔다. 앞다리 IK chain 2,
  뒷다리 IK chain 3으로 설정하고 발본은 IK target의 회전을 Copy Rotation한다.
- 각 다리에 FK 컨트롤도 만들고 `IK_FK` 0~1 속성으로 전환한다. 이번 여섯 액션은
  접지가 중요한 보행/공격/사망 모두 IK로 키를 잡고, 필요할 때만 FK로 베이크한다.
- `CTRL_Body`, `CTRL_SpineFront`, `CTRL_HeadPlate`, `CTRL_Mouth`를 둔다.
  머리판은 단단한 삽날이므로 lattice처럼 휘게 만들지 않는다.
- 꼬리는 `CTRL_Tail01~04` FK 체인으로 둔다. 자동 Secondary Motion이나 물리본은
  쓰지 않는다. WebGL과 리플레이에서 같은 결과가 나와야 한다.
- 발 IK에는 World/Root space 전환을 둔다. 보행의 접지 구간은 World, 공중 구간과
  Death는 Root space가 편하다. 전환 프레임에는 양쪽 공간을 같은 행렬로 맞춘 뒤 키를 찍는다.

## 본 롤과 회전 제한

- 모든 deform bone의 로컬 Y는 자식 방향이다. 같은 종류의 좌우 본은 roll을 복사한
  뒤 미러링하며, 무릎/발목의 주 굽힘축이 로컬 X 하나가 되게 한다.
- 앞 팔꿈치: Rest에서 5° 이상 굽혀 pole 방향을 확정하고, 굽힘 0~115°.
- 뒷 무릎: Z자 방향으로 10~130°, 역관절처럼 반대로 꺾이지 않게 Limit Rotation.
- 뒷 발목: -65~45°, 발 pitch ±30°, roll ±12°.
- `SpineFront`: pitch ±10°, yaw ±12°, roll ±8°.
- `HeadPlate`: pitch ±12°, yaw ±15°, roll ±6°. 이 이상이면 삽날 실루엣이 깨진다.
- `Mouth`: 닫힘 0°, 최대 개방 38°. 좌우 비틀기 금지.
- 꼬리 각 마디: yaw ±22°, pitch는 루트부터 ±14/18/22/28°. 한 마디에 모든
  곡률을 몰지 않는다.

축 부호는 실제 bone roll에 따라 달라질 수 있으므로 숫자의 +/−를 억지로 맞추지
말고, **해부학적 굽힘 방향과 최대 각도**를 맞춘다.

## 스키닝

- 한 vertex 최대 4 weights, 0.01 미만 제거, Normalize All. Unity Import의
  Skin Weights는 4 Bones로 둔다.
- 머리판 큰 면은 `HeadPlate` 0.85 이상으로 거의 강체 스키닝한다. 변형은 머리와
  몸통 사이 2~3개 루프에만 분산한다.
- Mouth 3개 루프는 입술 가장자리→안쪽 순으로 `HeadPlate/Mouth` 가중치를
  80/20, 45/55, 10/90 정도로 넘긴다. 닫힘에서 관통하지 않는 것이 우선이다.
- 어깨·고관절의 관절 봉우리는 해당 Upper/Thigh와 BodyRoot 사이에만 섞는다.
  반대편 다리나 Spine의 weight가 넘어오면 안 된다.
- 무릎/발목은 관절 중심 루프 50/50, 앞뒤 한 루프는 75/25로 시작해 볼륨 보존을
  확인한다. 필요하면 corrective shape key를 써도 되지만 FBX에는 최종 bake한다.
- 발가락은 개별 본 없이 Foot 본에 100% 가까이 묶는다. 접지 중 발가락이 고무처럼
  휘면 안 된다.
- 꼬리 루트는 BodyRoot와 Tail01을 60/40으로 섞고 끝으로 갈수록 단일 본 비중을
  높인다. 꼬리 끝이 진동하면 마지막 두 본의 F-curve를 정리한다.

## Rig 검수 포즈

애니메이션 전에 한 액션 `RIG_TEST`에서 다음을 확인하고 이 액션은 export하지 않는다.

1. 네 발을 제자리에서 각각 0.25 m 들어 IK/pole 뒤집힘이 없는지 확인.
2. 뒷다리를 최대 압축해 허벅지와 배가 관통하지 않는지 확인.
3. Mouth 38° 개방에서 공격구 안쪽 면이 뒤집히거나 사라지지 않는지 확인.
4. Spine/Head 최대 회전에서 머리판이 휘지 않고 목 경계만 변형되는지 확인.
5. 꼬리를 좌우 S자로 만들어 루트가 찢어지지 않는지 확인.
6. 모든 컨트롤을 Rest로 되돌렸을 때 좌우 발바닥 높이 차가 2 mm 이하인지 확인.
