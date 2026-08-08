# 00 · Humanoid 리깅 명세

## 좌표·원점

- Blender Z-up, 1 unit=1m, 모델 전방 -Y. FBX `-Z Forward / Y Up`, scale 1.0.
- Unity에서 발바닥 중앙이 원점 `(0,0,0)`, 전방은 로컬 +Z다.
- Armature/Mesh transform은 위치·회전 0, 스케일 1. 음수 스케일을 남기지 않는다.
- T-pose는 어깨 90°, 팔꿈치 2° 굽힘, 손바닥 아래, 엄지 전방으로 맞춘다.
- 전신과 1인칭 팔은 같은 Rest Pose를 공유한다. Root Motion은 사용하지 않는다.

## Export 본 · 56개

`Root` 1개는 non-deform, Humanoid deform는 55개다.

```text
Root
└─ Hips
   ├─ Spine ─ Chest ─ UpperChest ─ Neck ─ Head
   │                              ├─ Eye.L / Eye.R
   │                              └─ Jaw
   ├─ Shoulder.L ─ UpperArm.L ─ LowerArm.L ─ Hand.L ─ Fingers 15
   ├─ Shoulder.R ─ UpperArm.R ─ LowerArm.R ─ Hand.R ─ Fingers 15
   ├─ UpperLeg.L ─ LowerLeg.L ─ Foot.L ─ Toes.L
   └─ UpperLeg.R ─ LowerLeg.R ─ Foot.R ─ Toes.R
```

손가락은 엄지·검지·중지·약지·소지 각 3본이다. Twist 본은 export하지 않고 필요하면
UpperArm/LowerArm/UpperLeg weight와 corrective shape로 해결한다. Unity Humanoid의
모든 Required bone과 UpperChest, Toes, Eyes, Jaw를 매핑한다.

## 컨트롤 리그

- `CTRL_Master`, `CTRL_Root`, `CTRL_COG`, `CTRL_Chest`, `CTRL_Head`.
- 팔·다리에 IK/FK switch와 pole을 두고 IK/FK 전환 행렬 차를 1mm/0.5° 이하로 맞춘다.
- 손은 `CTRL_HandIK.L/R`, 무기는 `CTRL_Weapon`, 운반물은 `CTRL_CarryProp`을 쓴다.
- 발은 heel/toe/ball roll과 floor pin을 제공한다. Walk/Sprint 접지 구간은 World pin이다.
- 상호작용은 `CTRL_InteractTarget.L/R`을 씬 손잡이에 constraint한 뒤 FBX에 베이크한다.
- 후드·가슴 스트랩·허리 벨트는 별도 본 없이 UpperChest/Chest/Hips에 스키닝한다.

## 1인칭 팔

- 별도 파일 `Protagonist_Arms.fbx`, root는 `ViewModelRoot`, deform는 UpperChest부터
  양손 손가락까지 39개를 사용한다.
- 전신 애니메이션에서 상체·손 키를 복사한 뒤 카메라 기준 보정 레이어를 별도로 둔다.
- Main Camera near clip 0.03m에서 손목·팔꿈치·소매가 잘리지 않게 검수한다.
- FOV 70/80/90에서 총이 화면 중앙 조준점을 가리지 않도록 손 IK만 보정한다.
- 왼쪽 소매 수선 패치와 적갈색 띠는 1인칭에서도 유지해 캐릭터 선택이 보이게 한다.

## 스키닝 기준

- 관절 중심 루프 50/50, 인접 루프 75/25에서 시작한다.
- 어깨는 UpperChest/Shoulder/UpperArm 20/45/35, 엉덩이는 Hips/UpperLeg 45/55.
- 재킷 팔꿈치와 무릎 패드는 관절 중심을 피해 단단하게 유지한다.
- 장갑 손바닥은 Hand 0.85 이상, 손가락 마디만 각 finger chain에 분배한다.
- 최대 4 weights, 0.01 미만 제거. 팔을 120° 올리고 웅크림 1.10m에서 관통을 확인한다.

## 승인 포즈

1. T-pose와 A-pose에서 키 1.82m, 눈높이 1.65m를 확인한다.
2. 권총 양손 파지에서 손목 꺾임 35° 이하, 검지 방아쇠 밖 위치를 확인한다.
3. 산탄총 견착에서 개머리판·오른쪽 어깨 간격 5mm 이하, 왼손은 펌프 중앙에 둔다.
4. 20kg 운반물을 든 자세에서 손 IK 오차 3mm 이하, 허리 과신전 8° 이하로 한다.
5. 웅크림·착지·사망 포즈에서 후드, 벨트, 포켓, 무릎 패드 관통을 검사한다.

