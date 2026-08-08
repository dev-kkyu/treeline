# 머터리얼 · 텍스처 · Unity 규격

## 1. 공통 팔레트

색은 종 구분용이 아니라 손전등 아래 형태 분리용이다. 세 적 모두 같은 팔레트를
쓰고 배치 비율만 달리한다.

| 이름 | sRGB | 용도 | Base smoothness |
| --- | --- | --- | ---: |
| 압력 피부 · 암부 | `#252B2E` | 배, 접힌 안쪽, 다리 뒤 | 0.16 |
| 압력 피부 · 기본 | `#4E585C` | 전체 면적의 55~65% | 0.20 |
| 압력 피부 · 돌출 | `#687276` | 등판, 판 모서리, 큰 관절 | 0.24 |
| 서리 흉터 | `#B9C1BC` | 가장 돌출된 5~8% | 0.08 |
| 멍든 접힘 | `#72524D` | 관절 골과 입 주변 | 0.32 |
| 젖은 균열 | `#8A4337` | 공격구/Colossus 중앙 틈 안쪽만 | 0.68 |
| 깊은 구강 | `#241416` | 틈의 최심부 | 0.52 |
| 눈 · 공막 | `#8F9897` | Clinger v2 눈 전용 | 0.72 |
| 눈 · 홍채 | `#161B1D` | 거의 검은 홍채 | 0.48 |
| 눈꺼풀 | `#4F3738` | 눈 주변의 멍든 접힘 | 0.42 |

화면상 면적 기준으로 붉은 계열은 3%를 넘지 않는다. 피나 발광으로 쓰지 않으며,
손전등이 직접 닿을 때만 보이는 재질 차이다.

## 2. 셰이더

- Shader: `Universal Render Pipeline/Lit`
- Workflow: Metallic
- Surface: Opaque, Alpha Clipping off, Render Face Front
- Metallic: 0.0 고정
- Smoothness Source: Albedo Alpha
- Normal Scale: 0.75 (`Colossus` 0.65)
- Specular Highlights: on
- Environment Reflections: off
- Receive Shadows: on
- Cast Shadows: on
- GPU Instancing: on (`Crawler`, `Clinger`), `Colossus`는 선택

Shader Graph, Parallax, Detail Map, Clear Coat, Emission, Subsurface, 투명 셰이더는
사용하지 않는다. 젖은 균열은 BaseMap alpha의 smoothness 차이로 한 슬롯 안에서
표현한다.

## 3. 텍스처 세트

적 하나당 정확히 두 장을 사용한다.

| 적 | BaseMap RGBA | Normal | 텍셀 밀도 |
| --- | --- | --- | --- |
| Crawler | 1024² | 1024² | 약 300 px/m |
| Clinger | 1024² | 1024² | 약 320 px/m |
| Colossus | 2048² | 2048² | 약 280 px/m |

- BaseMap RGB: 색, A: smoothness.
- Normal: 균열과 압력 주름만. 2 px 이하 피부 노이즈는 넣지 않는다.
- AO/Metallic/Emission/Height 전용 맵은 만들지 않는다.
- 방향성 조명과 접촉 그림자를 BaseMap에 강하게 굽지 않는다.
- UV island padding: 1024에서 16 px, 2048에서 24 px. 대칭 미러 UV는 균열이
  데칼처럼 반복되므로 팔다리 하부에만 허용한다.

### UV 배분

| 영역 | Crawler | Clinger | Colossus |
| --- | ---: | ---: | ---: |
| 핵심 실루엣/공격구 | 40% | 42% | 46% |
| 팔다리 | 36% | 38% | 34% |
| 몸통 보조면 | 18% | 14% | 14% |
| 여백 | 6% | 6% | 6% |

## 4. 종별 표면 배치

### Crawler

- 돌출색: 삽날 머리 상단, 어깨, 뒷다리 장골.
- 서리 흉터: 진행 방향과 평행한 긴 균열. 얼굴처럼 좌우 대칭시키지 않는다.
- 젖은 균열: 머리 아래 공격구에만 사용.

### Clinger

- 닫힌 자세 바깥면은 거의 무광 서리막, 안쪽은 암부 피부.
- 펼칠 때 리본 안쪽의 멍든 접힘이 순서대로 드러난다.
- 중앙 공격구만 젖게 하고 끝부분은 건조하게 유지한다.
- v2 눈 8개는 같은 아틀라스의 눈 타일을 공유한다. 눈은 비발광이며 정면 고정이다.

### Colossus

- Final은 최초 Colossus의 검은 회청색 균열 피부를 유지한다.
- 쌍봉 등각과 팔 바깥면은 돌출색, 중앙 골짜기와 겨드랑이는 암부색으로 분리한다.
- 마른 균열은 BaseMap/Normal로만 표현하고 바위 표면처럼 두껍게 융기시키지 않는다.
- 젖은 값은 머리 아래 입과 쌍봉 중앙의 가장 깊은 틈에만 3% 미만으로 사용한다.
- 노출 눈이 없으므로 Colossus UV에서 눈 타일을 제거하고 몸통·손 3지에 재배분한다.
- Emission, Clear Coat, 투명, 실제 석재·금속 재질은 사용하지 않는다.

## 5. Unity Texture Import

### BaseMap

- Texture Type: Default
- sRGB: on
- Alpha Source: Input Texture Alpha
- Alpha Is Transparency: off
- Generate Mip Maps: on, Mip Streaming: off (Web 빌드)
- Wrap Mode: Clamp, Filter Mode: Bilinear, Aniso Level: 2
- Max Size: 원본 크기 유지
- Compression: Normal Quality, Use Crunch Compression on
- Crunch Quality: 65

### Normal

- Texture Type: Normal map
- Create from Grayscale: off
- Generate Mip Maps: on
- Wrap Mode: Clamp, Filter Mode: Bilinear, Aniso Level: 2
- Max Size: BaseMap과 동일
- Compression: Normal Quality, Use Crunch Compression off

Normal Crunch는 블록 노이즈가 손전등 하이라이트를 흔들 수 있어 사용하지 않는다.

## 6. 머터리얼 이름과 프리팹

| 적 | 머터리얼 | 슬롯 수 |
| --- | --- | ---: |
| Crawler | `M_Crawler_FrostPressure` | 1 |
| Clinger | `M_Clinger_FrostPressure` | 1 |
| Colossus | `M_Colossus_FrostPressure` | 1 |

파일명은 `T_<Enemy>_BaseSmooth`, `T_<Enemy>_Normal`로 고정한다. 젖은 부분 때문에
서브메시나 두 번째 머터리얼을 만들지 않는다.

## 7. 조명 검수 씬

- 완전 검정 배경과 18% 회색 바닥.
- 플레이어 손전등과 같은 Spot Light 하나: 4500 K, inner 18°, outer 32°.
- 2 m, 6 m, 12 m 마커에서 각각 캡처한다.
- 2 m: 피부/서리/균열 세 재질이 갈려야 한다.
- 6 m: 돌출면과 공격구만 읽히면 된다.
- 12 m: 색이 사라져도 실루엣으로 종을 구분해야 한다.
- 발전동 조명 점등 상태에서는 BaseMap이 회색 덩어리로 날아가지 않아야 한다.

## 8. Web 빌드 예산

- 동시 렌더 기준: Crawler 6, Clinger 8, Colossus 1마리 상한.
- 화면 내 적 머터리얼 종류: 최대 3, 적당 슬롯은 1.
- LOD 전환 화면 상대 높이: 0.32 / 0.12 / 0.04, Cross Fade 0.12.
- Skinned Mesh: Update When Offscreen off, Dynamic Occlusion on.
- 그림자는 LOD2에서 Cast Shadows off. Colossus는 LOD1까지 on.
- 타겟 빌드에서 60 fps가 아니라 안정적인 30 fps를 최저 합격선으로 둔다.
