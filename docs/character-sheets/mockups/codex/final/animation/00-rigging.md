# 00 · Humanoid 리깅 명세

> 상태: 확정 · 최종 수정 2026-08-09

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

## 컨트롤 리그 — 제작용이다. 전부 베이크한다

IMPORTANT: 이 절의 IK 는 **클립을 만들 때만 존재한다.** FBX 로 나갈 때 deform bone
에 구워지고 런타임에는 남지 않는다. 씬의 실물에 손을 맞추는 것은 별개이며
[`README.md`](README.md) 의 「런타임 IK」 절이 다룬다.

컨트롤은 **사지당 IK 타깃 하나, 모두 네 개**다. 각각 그 사지의 마지막 본
(`Hand.L/R`, `Foot.L/R`) 위에 머리 · 꼬리 · roll 까지 똑같이 겹쳐 놓아, 하나가
IK 사슬을 겨누는 일과 그 본에 월드 회전을 건네는 일을 같이 한다.

- `CTRL_HandIK.L/R` — `LowerArm` 에 chain 2 IK, `Hand` 는 이 컨트롤을 월드
  COPY_ROTATION 한다.
- `CTRL_FootIK.L/R` — 같은 구조. 접지 구간에서 발바닥이 평평하게 남는 것이
  이 배치의 목적이다 (reverse-foot).
- 나머지는 전부 deform bone 을 직접 FK 로 키잉한다. 후드·가슴 스트랩·허리 벨트도
  별도 본 없이 UpperChest/Chest/Hips 에 스키닝한다.

IMPORTANT: **컨트롤의 roll 을 그 본의 roll 과 반드시 같게 둔다.** COPY_ROTATION 이
월드 공간이라, roll 이 다르면 리그를 여는 순간 손이 통째로 튄다. 처음 세울 때
컨트롤만 월드 X 로 두었더니 손가락 끝이 rest 에서 70 mm 어긋났다.

pole 은 두지 않는다. Rest 가 팔꿈치를 5° · 무릎을 3° 미리 접어 두고, 두 본의
roll 을 **사슬이 이루는 평면의 법선**에 맞춘 뒤 IK 의 Y · Z 를 잠그면 관절의
자유도가 하나만 남아 뒤집힐 곳이 없다. 들어올리기 · 보폭 · 웅크림 전부에서 접힘
각이 음수가 된 경우가 없다.

**`CTRL_InteractTarget.L/R` 을 씬 손잡이에 constraint 해 베이크하지 않는다.**
그렇게 만들면 레버 종류마다 클립이 하나씩 늘어난다. 손잡이를 잡는 구간은 손을
화면 기준 기본 위치에 두고, 실제 손잡이에는 런타임 IK 가 맞춘다.

`CTRL_Weapon` 과 `CTRL_CarryProp` 은 전신에 없다. 무기는 1인칭 팔 리그 안의 본이고
([`09-weapons.md`](09-weapons.md)), 운반물은 런타임 IK 가 잡는다.

## 만드는 것은 스크립트다

`Treeline/ModelSources/Protagonist/build_rig.py` 가 Meshy 원본을 읽어 본을 세우고
스키닝까지 하고, `rig_test.py` 가 아래 승인 항목을 재고 렌더한다. 손으로 만지지
않는다 — 모델을 다시 뽑으면 두 스크립트를 다시 돌리면 된다.

```bash
blender protagonist.blend -b --python build_rig.py
blender Protagonist_Rig.blend -b --python rig_test.py
```

## 런타임 IK 계약

1인칭 팔은 Generic 리그다 — Humanoid 전용인 `Animator.OnAnimatorIK` 를 못 쓴다.
2본 해석해(코사인 법칙 + pole 벡터)를 직접 짜서 `LowerArm` · `Hand` 에 건다.

- IK 체인은 `UpperArm → LowerArm → Hand` 한 쪽 2본. pole 은 `LowerArm` 의 rest
  굽힘 방향을 그대로 쓴다. 팔꿈치가 뒤집힐 자유도가 없어야 한다.
- 세기는 클립에 구운 **`IKWeight` 커브**로 준다. Unity 는 `AnimationClip` 의 커스텀
  커브를 `Animator.GetFloat` 로 읽는다. 항상 켜두면 손이 프롭에 끌려다닌다.
- 타깃은 프롭이 들고 있다 — `Lever.handleGrip`, `Carriable.gripL/gripR`.
  **애니메이션이 위치를 정하지 않는다.** 그래야 레벨과 애니메이션이 서로 안 묶인다.
- 총에는 걸지 않는다. 손 본 소켓에 붙이면 상대 위치가 고정이다.

## 1인칭 팔

- 별도 파일 `Protagonist_Arms.fbx`, root는 `ViewModelRoot`, deform는 UpperChest부터
  양손 손가락까지 39개를 사용한다. Generic 리그다.
- **메시는 새로 만들지 않고 전신에서 잘라낸다.** 전신 메시를 복제해 `UpperChest`
  아래·뒤를 지우고 소매 절단면을 막는다. 같은 텍스처와 비례가 그대로 따라오고,
  장갑을 낀 손이라 피부·얼굴을 만들 필요가 없다.
- 팔 클립은 전신에서 복사하지 않고 **처음부터 카메라 기준으로 만든다.** 1인칭
  구도에서 읽히는 팔 각도는 3인칭과 다르다.
- Main Camera near clip 0.03m에서 손목·팔꿈치·소매가 잘리지 않게 검수한다.
- FOV 70/80/90에서 총이 화면 중앙 조준점을 가리지 않게 한다. 총은 손 소켓에
  붙어 있으므로 보정은 `ViewModelRoot` 오프셋으로 하고 손을 움직이지 않는다.
- 왼쪽 소매 수선 패치와 적갈색 띠는 1인칭에서도 유지해 캐릭터 선택이 보이게 한다.

## 스키닝 기준

- 관절 중심 루프 50/50, 인접 루프 75/25에서 시작한다.
- 어깨는 UpperChest/Shoulder/UpperArm 20/45/35, 엉덩이는 Hips/UpperLeg 45/55.
- 재킷 팔꿈치와 무릎 패드는 관절 중심을 피해 단단하게 유지한다.
- 장갑 손바닥은 Hand 0.85 이상, 손가락 마디만 각 finger chain에 분배한다.
- 최대 4 weights, 0.01 미만 제거. 팔을 120° 올리고 웅크림 1.10m에서 관통을 확인한다.

## 승인 포즈 — 2026-08-09 통과

권총 파지 · 산탄총 견착은 여기서 보지 않는다. **무기는 1인칭 팔 리그 안에 있고**
전신은 그림자와 엔딩 원경에만 나오므로, 이 리그에서 그 둘을 재도 팔 리그에서
다시 재야 한다. 대신 전신 3종이 기대는 것 — 도달 · 접힘 방향 · 손가락 감김
방향 — 을 본다.

| # | 항목 | 기준 | 실측 |
| ---: | --- | --- | --- |
| 1 | T-pose 키 · 눈높이 · 접지 | 1.82 / 1.65 ±0.01, 발바닥 0 | **1.8200 / 1.6600 / 0.0006 · 0.0000** |
| 2 | 관절이 메시 안에 있는가 | 55개 전부 | **55/55** |
| 3 | 사지 IK 오차 | 3 mm 이하 | **0.05 mm 이하** |
| 4 | 접힘 방향 (들어올리기) | 음수 없음 | 팔 7.5°→78.9°, 다리 5.5°→98.8° |
| 5 | 보폭 ±0.30 m | 뒤집힘 없음 | 앞 46.5°, 뒤 40.9° |
| 6 | 웅크림 (골반 −0.35 m) | 발이 붙어 있을 것 | 무릎 108.4°, 발바닥 5.5 · 5.0 mm |
| 7 | 팔 120° 들기 | 몸통 관통 없음 | 팔–몸통 16.7 mm (rest 43.8) |
| 8 | 손가락 +45° | 손바닥 쪽으로 감길 것 | 끝 −55.4 mm (−45° 면 +84.6) |
| 9 | 엄지 +50° | 손바닥을 가로지를 것 | 끝 (−47, +89, −1) mm |
| 10 | 스키닝 | 정점당 4개, 빈 그룹 0 | **4 / 55 그룹 / 0** |

8·9 는 roll 축이 뒤집혔는지 보는 항목이다. 이 리그에서 조용히 틀릴 수 있는 곳이
거기뿐이라, 값이 아니라 **부호**를 본다.

모델이 A-pose 로 오면 Unity 아바타 설정에서 Enforce T-Pose 를 눌러야 자동 매핑이
맞는다. 이 모델은 T-pose 로 왔다.

### 아직 남은 것

- **런타임 IK 를 레버 하나로 검증한다.** 손잡이를 0.3 m 옮겨도 손이 따라오고
  팔꿈치가 뒤집히지 않는지 본다. Unity 쪽 일이라 여기서 못 재고, 여기가 안 되면
  클립을 만들어도 다 고쳐야 한다.
- 20 kg 운반 자세의 허리 과신전 8° 이하, 사망 포즈의 후드 · 벨트 관통은 해당
  클립을 만들 때 같이 본다. 웅크림 · 착지는 이번 범위가 아니다
  ([`README.md`](README.md) 참고).

