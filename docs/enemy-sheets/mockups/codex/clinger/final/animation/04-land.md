# 04 · `Clinger_Land`

## 역할

- 18f, 30 fps, 0.60초, One-shot, in-place.
- 공격 손 2개와 뒤쪽 부착 사지 LR/RR을 바닥에 놓아 4점 보행 자세로 바꾼다.
- 15f에 충돌체와 NavMeshAgent를 바꾸는 게임플레이 계약이 있다.

![착지 키포즈](land-keyposes.png)

## 프레임과 이벤트

| 프레임 | 접지 | 포즈 / 이벤트 |
| ---: | --- | --- |
| 1 | 공격 손 L/R | 손바닥/손가락 그룹 선접촉, Torso 70 mm 압축 |
| 4 | 손 L/R + LR/RR | 네 점 접지, 공격 팔꿈치와 뒤 사지 최대 굽힘 |
| 8 | 네 점 | 반동 정점, 몸통 roll 3°, LF/RF 사지는 등 위로 회수 |
| 12 | 네 점 | 착지 높이 1.42 m로 회복, Head 전방 +Z 정렬 |
| **15** | 네 점 | `AE_EnableGroundMode`: Ground 공간, Capsule/Agent on |
| 18 | 네 점 | 지상 정지 포즈. Scuttle 1f와 2 cm/3° 안에서 호환 |

## 제작값

- 실제 낙하 속도를 animation에 bake하지 않는다. 1f는 충돌 감지 시점의 상대 포즈다.
- 손 IK와 LR/RR IK는 Ground/World 고정, LF/RF는 몸통 위 Root space로 접는다.
- COG 하강은 70 mm, 몸통 회전은 pitch 7°/roll 3° 이내. 바닥을 뚫는 squash 금지.
- 손가락 그룹은 접촉 때 8~12°만 닫아 충격을 받고 개별 발톱은 움직이지 않는다.
- f15 공간 전환은 행렬 매치 후 한 프레임에 실행한다. Root/GroundPivot 보정으로
  렌더가 위아래로 튀면 안 된다.

## 코드 계약

- f15에 천장 Sphere off, 지상 Capsule h 1.34/r 0.48/center y 0.67 on.
- 같은 프레임에 Agent r 0.48/h 1.42를 NavMesh에 배치하고 speed는 아직 0에서 시작한다.
- NavMesh sample 실패 시 Agent를 공중에 켜지 말고 착지 위치를 유효 지점으로 보정한다.

## 검수

- f4 충격이 가장 크고 f15 전에는 지상 추격이나 공격을 시작하지 않는다.
- GroundPivot이 바닥 y=0에 있고 18f 렌더 높이는 1.42±0.03 m이다.
- Land→Scuttle 전환에서 네 접지점 pop이 2 cm/3° 이하다.
