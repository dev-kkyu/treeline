# 애니메이션 검수 체크리스트

## Blender

- [ ] 총 26본이며 export deform은 24개, Root/GroundPivot은 non-deform이다.
- [ ] Armature/Mesh transform이 0/0/0, scale 1이고 상쇄 회전·음수 스케일이 없다.
- [ ] vertex당 weight 4개 이하, 0.01 미만 weight가 제거됐다.
- [ ] Folded 전체가 `0.88×0.72×0.62 m`, Extended가 `1.10×2.51×0.82 m` 안이다.
- [ ] 갈고리 8개·공격 손가락 6개이며 개별 손가락 본을 추가하지 않았다.
- [ ] Folded/Tremor/Scuttle loop 시작·끝 값과 tangent가 같다.
- [ ] Root 위치/회전과 scale 키가 모든 액션에서 0/1이다.
- [ ] Ceiling→Object→Ground 공간 전환 pop이 2 mm/1° 이하이다.
- [ ] Death 28f 이후 Noise/Cycles가 없어 완전히 멈춘다.

## Unity

- [ ] 모델 local +Z가 쐐기 머리 방향이고 Generic Avatar가 정상이다.
- [ ] Folded/Tremor/Scuttle만 loop하고 나머지는 one-shot 또는 clamp다.
- [ ] Unfold 10f에 낙하가 정확히 한 번 시작한다.
- [ ] Land 15f 전에 Agent/Capsule이 켜지지 않고 15f에만 전환된다.
- [ ] GroundPivot y=0, Land 18f 렌더 높이가 1.42±0.03 m이다.
- [ ] 4.2 m/s Scuttle과 240°/s 회전에서 접지 미끄러짐·pole flip이 없다.
- [ ] Attack 13f에 전방 Capsule 피해가 정확히 한 번 발생한다.
- [ ] 비치명은 Hit, 치명타는 Hit를 건너뛰고 Death가 최우선이다.
- [ ] Death에서 collider/Agent가 꺼지고 28f 애니메이션은 끝까지 재생된다.

## 시각·기획

- [ ] 천장에서는 접힌 덩어리, 전개 시 굵은 수직 추, 지상에서는 4점 보행으로 읽힌다.
- [ ] 천장 사지 4 + 공격 팔 2 외의 다리·꼬리·날개가 생기지 않는다.
- [ ] LF/RF 사지는 지상에서 등 위에 접혀 여섯 다리처럼 보이지 않는다.
- [ ] 눈처럼 보이는 발광점이 없고 의도는 쐐기 입과 양손으로 전달된다.
- [ ] Attack 9f 예고와 13f 히트가 무음 1인칭 화면에서도 구분된다.
- [ ] 64px 검정 실루엣에서 Crawler의 수평선·Colossus의 넓은 M자와 혼동되지 않는다.

## 테스트 장면

1. 천장 2.6 m Folded: 0.72 m 바운드와 플레이어 통과 확인.
2. 천장 3.4 m Extended: 전개 2.51 m와 낙하 예고 확인.
3. 천장 2.6/3.4/4.0 m: 같은 clip 속도, 다른 코드 낙하 시간 시험.
4. Land 직후 4.2 m/s 직선 10 m와 반경 2 m 회전 시험.
5. Attack 13f 전후로 대상이 범위를 벗어나는 경계 시험.
6. 천장/지상 각각 치명타: Death pose, collider, 바닥 정지 확인.
7. 0.5배속·2배속과 WebGL Development Build에서 이벤트 중복/압축 오차 재검수.
