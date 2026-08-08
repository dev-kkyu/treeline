# V2 음산한 다각도 생성 프롬프트

## 생성 정보

- 생성 방식: Codex 내장 이미지 생성의 로컬 참조 이미지 편집
- 참조 역할: 초기 단일 시안의 건축, 재질, 랜드마크, 대략적인 공간 관계 유지
- 산출물: 6개 챕터 × 4개 시점 = PNG 24장
- 공통 제외: 조감도, 아이소메트릭, 컷어웨이, 평면도, 콜라주, UI, 글자, 사람,
  선명한 적, 고어, 미래형 SF 장비

각 파일은 해당 시점의 공통 프롬프트와 장면별 지시를 연결해 한 번씩 생성했다.

## 01 · 진입 전경 공통 프롬프트

```text
Use case: lighting-weather edit and stylized-concept level visual development.
Input image role: architecture, material, landmark, and layout continuity reference. Preserve the same chapter identity and believable spatial relationships; do not copy the exact lighting.
Create V2 angle 01, ENTRY WIDE: a new, more ominous cinematic 2D environment concept painting from a normal in-world human eye-level camera. Make it genuinely eerie through motivated low light, large negative-space shadows, cold haze, partial visibility, condensation, subtle environmental disturbance, and one or two small warm utility lights. Do not merely crush the exposure: the playable path and key devices must remain readable. Suggest an unseen presence only through strange long shadows, disturbed frost, a slightly moving hanging cable, or traces; no clearly visible monster.
Wide landscape frame, single perspective, painterly production art. No aerial, bird's-eye, isometric, axonometric, cutaway, floor plan, diagram, 3D turntable, collage, text, labels, arrows, UI, logos, watermark, people, explicit creature, gore, sci-fi, cyberpunk, or steampunk.
```

### 장면별 지시

- **Ch01:** 관제실 입구 기록 보관소에서 콘솔·서치라이트 창·옥상 계단을 본다. 일부
  콘솔만 살아 있고 젖은 끌린 자국은 검은 출입문으로 사라진다.
- **Ch02:** 긴 숙소 복도의 검은 침상·사물함 틈 사이로 히터와 얼어붙은 문, 셔터를
  본다. 바닥 안개와 먼지만 움직인다.
- **Ch03:** 경비 착지점에서 펌프·연료통·누출 구간·연료고까지 내려다본다. 붉은
  작업등의 작은 반사 외에는 통로가 검게 끊긴다.
- **Ch04:** 진입 캣워크에서 발전기·두 차단기·축전지실·환기 팬을 본다. 둥지의 천장
  높이는 어둠 속에서 사라지고 막만 희미하게 움직인다.
- **Ch05:** 무너진 실내 통로에서 철탑 세 곳·제설기·창고·제설포를 본다. 극야의
  눈보라 속 유도등만 붉은 점으로 이어진다.
- **Ch06:** 승강장 입구에서 권양기·연료·히터·배전반·정비구·삭도·제설포를 본다.
  불규칙하게 켜지는 작업등이 긴 기계 그림자를 만든다.

## 02 · 역방향 동선 공통 프롬프트

```text
Use case: stylized-concept environment continuity edit.
Input image role: exact chapter design, palette, landmark and eerie-lighting reference. Recreate the same believable location from a substantially different camera, not a mirrored copy.
Create V2 angle 02, REVERSE PATH: normal in-world human standing eye level at the far objective or exit, camera turned back along the path the player came from. Wide cinematic 2D production concept painting with clear foreground, midground and background. Preserve the recognizable architecture, recurring props, material language and approximate spatial relationships from the reference. Keep the new ominous mood: deep but readable shadows, cold mist, minimal amber utility lamps, reflections, disturbed frost or dust, and large threatening negative space. Suggest an unseen presence without showing a monster.
No aerial, bird's-eye, isometric, axonometric, cutaway, dollhouse, floor plan, diagram, mirror image, collage, UI, text, labels, arrows, logos, watermark, people, visible creature, gore, sci-fi, cyberpunk, or steampunk.
```

### 장면별 지시

- **Ch01:** 파노라마 창과 콘솔 옆에서 출입문·기록실·옥상 계단을 향해 180도
  돌아본다. 움직이는 서치라이트가 뒤쪽 벽을 훑는다.
- **Ch02:** 히터와 얼어붙은 문 옆에서 긴 복도 입구를 되돌아본다. 중간의 사물함
  하나만 살짝 열려 있다.
- **Ch03:** 가장 깊은 연료고에서 누출 통로와 경비실의 작은 붉은 불빛을 향해
  돌아본다. 기름 먼지 위의 새 자국이 위쪽으로 이어진다.
- **Ch04:** 발전기 뒤 바닥에서 높은 진입 캣워크를 본다. 양쪽 차단기와 축전지실을
  유지하고 둥지 천장이 입구 쪽으로 처진다.
- **Ch05:** 두 번째 철탑 착지점에서 첫 철탑과 무너진 통로를 올려다본다. 제설기의
  파낸 길이 역방향으로 올라간다.
- **Ch06:** 삭도 문턱에서 승강장 입구를 본다. 히터·배전반·정비구·권양기의 뒷면과
  입구로 길게 뻗은 그림자가 보인다.

## 03 · 핵심 장치 공통 프롬프트

```text
Use case: stylized-concept environment continuity edit.
Input image role: chapter architecture, palette, material and landmark continuity reference.
Create V2 angle 03, OBJECTIVE CONTEXT: move the in-world camera close to the chapter's main device, but keep a wide environmental composition so surrounding routes and secondary landmarks remain visible. Normal human standing eye level, cinematic 2D hand-painted production concept art, not a product shot and not a diagram. Preserve the same old mountain-relay architecture, materials, weather, spatial logic and V2 eerie lighting. Use highly localized warm device light against cold darkness, damp reflections, frost, haze, hanging cables, and negative space. Devices must look practical and buildable in Unity, not futuristic. Suggest danger indirectly without a clearly shown monster.
No aerial, bird's-eye, isometric, axonometric, cutaway, floor plan, orthographic view, collage, UI, text, labels, arrows, logos, watermark, people, explicit creature, gore, sci-fi, cyberpunk, or steampunk.
```

### 장면별 지시

- **Ch01:** 옥상 수동 서치라이트와 손잡이 옆에서 관제실 창·옥상 계단·외부 통로를
  본다. 빛의 가장자리에 정체가 읽히지 않는 거대한 그림자만 스친다.
- **Ch02:** 얼어붙은 문 옆 홈에서 배전함·히터·케이블·셔터 레버와 되돌아가는
  복도를 함께 본다.
- **Ch03:** 고장 난 펌프와 파열된 경유 배관 옆에서 연료통·누출 웅덩이·양쪽 동선을
  함께 본다.
- **Ch04:** 축전지실 안에서 유선 유리를 통해 발전기와 반대쪽 차단기를 본다.
  축전지의 붉은 계기와 멈춘 환기 팬의 그림자를 강조한다.
- **Ch05:** 제설기 옆에서 오거 하우징·엔진·파낸 길·작은 창고·두 번째 철탑을 본다.
  눈 위 흔적은 기계 앞에서 갑자기 끊긴다.
- **Ch06:** 권양기 연료 주입구 옆에서 연료통·히터·배전반·정비구·제설포·삭도를
  대각선으로 함께 본다.

## 04 · 위협·은신 공통 프롬프트

```text
Use case: stylized-concept environment continuity edit.
Input image role: exact chapter identity, architecture, material, device, palette and eerie-lighting reference.
Create V2 angle 04, THREAT / HIDING VIEW: a substantially new in-world camera from a plausible crouched hiding or cover position, approximately 0.8 to 1.1 meters high. Use nearby geometry for dark foreground occlusion, with the wider level and its key landmarks readable beyond. Cinematic wide 2D hand-painted game environment concept art. Preserve the same spatial language and practical industrial construction. Push ominous atmosphere with deep layered darkness, cold haze, partial silhouettes, weak motivated light, wet or icy reflections, abnormal shadows and subtle physical traces. Do not show a clear monster; fear comes from framing and the unseen.
No aerial, bird's-eye, isometric, axonometric, cutaway, dollhouse, floor plan, diagram, fisheye, security-camera view, collage, UI, text, labels, arrows, logos, watermark, people, visible creature, gore, sci-fi, cyberpunk, or steampunk.
```

### 장면별 지시

- **Ch01:** 넘어간 의자와 콘솔 아래에서 출입문과 창을 본다. 바닥 손전등과 창에
  스치는 거대한 그림자만으로 위협을 암시한다.
- **Ch02:** 반쯤 열린 사물함 또는 침상 안쪽에서 복도와 멀리 있는 히터·얼음문을
  본다. 바닥 안개와 케이블 위 그림자만 움직인다.
- **Ch03:** 연료통 뒤의 좁은 철제 계단에서 파열 지점과 연료고를 본다. 기름 웅덩이의
  동심원만 보이지 않는 움직임을 알린다.
- **Ch04:** 발전기 옆 얕은 정비구에서 엔진·캣워크·두 차단기·둥지 천장을 올려다본다.
  트러스 사이의 검은 공간을 수직 위협으로 사용한다.
- **Ch05:** 제설기가 만든 낮은 도랑과 난간 뒤에서 기계와 철탑을 올려다본다. 철탑
  뒤에서 무언가 지나간 흔적만 눈보라에 남긴다.
- **Ch06:** 열린 정비구 또는 낮은 기계 뒤에서 권양기·히터·배전반·제설포·삭도를
  본다. 프레임 밖 Colossus는 승강장과 삭도 위의 거대한 그림자로만 암시한다.
