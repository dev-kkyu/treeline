# 애니메이션 검수 체크리스트

## Blender

- [ ] Armature/Mesh transform이 0/0/0, scale 1이고 상쇄용 ±90°가 없다.
- [ ] Export deform bone은 22개, vertex당 weight는 4개 이하이다.
- [ ] 1f Rest와 `Idle_Low` 시작 포즈가 일치한다.
- [ ] Loop 3종은 시작/끝 값과 tangent가 같고 발 pop이 없다.
- [ ] 접지발 미끄러짐: Blender 월드 기준 2 mm 이하.
- [ ] Mouth 38°에서 관통/뒤집힌 normal/늘어난 이빨이 없다.
- [ ] 모든 액션의 Root location/rotation이 0이며 scale 키가 없다.
- [ ] Death 30f 이후 Noise/Cycles가 없어 완전히 멈춘다.

## Unity

- [ ] 모델 local +Z가 머리 방향이고 바닥 원점 y=0이다.
- [ ] Generic Avatar가 초록색이며 missing transform이 없다.
- [ ] Idle/Skitter/Sprint만 loop하고 나머지는 one-shot이다.
- [ ] 2.4 m/s Skitter와 5.5 m/s Sprint에서 눈에 띄는 foot slide가 없다.
- [ ] 720°/s 회전 중 무릎 pole이 뒤집히지 않는다.
- [ ] Attack 13f에 피해가 정확히 한 번 발생한다.
- [ ] 공격 사거리 1.6 m 밖으로 빠진 대상은 event 시점에 피해를 받지 않는다.
- [ ] 한 발 피격은 Hit, 치명타는 Hit를 건너뛰고 Death로 간다.
- [ ] Death가 Attack/Hit를 즉시 중단하고 3초 뒤 오브젝트가 제거된다.
- [ ] 시체 동안 Collider와 Agent는 꺼지고 Animator는 30f까지 재생된다.

## 시각/기획

- [ ] 64px 검정 실루엣에서도 모든 클립이 낮은 수평 쐐기로 읽힌다.
- [ ] 머리가 사람/개처럼 들리지 않고 바닥 0.12 m 부근을 겨눈다.
- [ ] 앞다리는 제동, 긴 Z형 뒷다리는 추진 역할이 동작에서 구분된다.
- [ ] 꼬리는 균형 보조일 뿐 공격/전갈 침처럼 보이지 않는다.
- [ ] Attack 8f 예고와 13f 히트가 무음 1인칭 화면에서도 구분된다.
- [ ] Hit는 Attack보다 작은 형태 변화, Death는 10f 안에 가장 명확한 상태 변화다.
- [ ] 6 m 손전등 거리에서 균열 디테일 없이도 발·머리·몸통 방향이 읽힌다.

## 테스트 장면

1. 평지 직선 10 m: Idle→Skitter→Sprint 속도 blend.
2. 반경 1.5 m 원: Agent 720°/s 방향 전환과 발/무릎 안정성.
3. 벽 앞 1.6 m: Attack 예고, 13f 피해, 벽/플레이어 관통 확인.
4. 권총 1발 후 0.2초 뒤 2발째: Hit→Death 우선순위.
5. 0.5배속과 2배속: event 중복, loop seam, IK pop 확인.
6. WebGL Development Build: Generic Avatar/Animation Event/압축 후 결과 재확인.
