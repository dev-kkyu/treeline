# 생성 프롬프트

## 공통 네거티브

```text
no clothing, no armor, no machinery, no cables, no weapons, no tools,
no readable eyes, no human face, no fur, no antlers, no fantasy horns,
no exposed human teeth, no gore, no metal, no glowing parts, no text,
no logo, no base, no environment props, no xenomorph, no zombie
```

AI 생성 결과에서 금지 요소가 하나라도 나오면 리토폴로지로 고치지 말고 다시
생성한다. 생성 단계에서 큰 형태가 틀리면 이후 단계의 비용이 더 크다.

## Crawler · image-to-3D 보조 프롬프트

```text
Original game-ready quadruped horror creature for a dark snowbound relay station.
Extremely low horizontal wedge silhouette, 2.25 m body plus a thin 0.6 m tail,
maximum height 0.78 m. A broad shovel-shaped sensory skull nearly scrapes the
floor; its attack slit is hidden underneath. Short braced front legs, long folded
rear legs built for sudden acceleration. No neck and no human facial structure.
Frozen pressure-cracked blue-gray flesh, sparse pale frost scars, matte surface,
only the underside mouth fissure is wet. Neutral locomotion pose, feet separated,
clean topology-aware joints, 5200 triangle target, single material, no microdetail.
```

검수: 옆 실루엣이 삼각 쐐기인지, 정면 폭이 1 m 이내인지, 입이 위가 아니라 아래에
있는지 확인한다.

## Clinger · image-to-3D 보조 프롬프트

```text
Original ceiling ambush creature with two readable states. Four flat ribbon-like
limbs interlock into a compact closed diamond cocoon, 0.72 m tall, attached to a
ceiling. When unfolded, those same limbs make a very thin 2.20 m vertical spear
silhouette with a short spindle body inside. A narrow central slit opens only
during attack. Biological, not mechanical, not an insect, no wings and no spider
abdomen. Frozen pressure-cracked blue-gray flesh, chalky frost on outer faces,
dark bruised folds inside. Rig-friendly joints, 4200 triangle target, one material.
```

검수: 닫힌 자세와 펼친 자세가 같은 팔다리 구조로 연결되는지, 날개/거미로
읽히지 않는지 확인한다.

## Colossus · image-to-3D 보조 프롬프트

```text
Original unkillable corridor-blocking horror creature, 3.85 m tall and 2.80 m
wide. It is not a giant human. Its body is two enormous thick vertical biological
slabs split by one narrow central fissure, supported by four mismatched pillar
limbs at the corners. No separate head, neck, shoulders or hands. Heavy flat feet,
low center of gravity, always at least three supports on the ground. Attacks by
pushing the whole slab forward or closing the central fissure, never by punching.
Frozen pressure-cracked dark flesh, pale dry scars, sparse wet tissue inside the
fissure. Large simple surfaces, 11000 triangle target, one material, rig-friendly.
```

검수: 검은 실루엣이 인간/고릴라처럼 보이지 않는지, 다리 사이로 플레이어가 지나갈
구멍이 생기지 않는지 확인한다.

## 콘셉트 보드 생성에 사용한 최종 프롬프트

```text
Use case: stylized-concept
Asset type: original production concept board for a Unity first-person horror game
Primary request: Design an original family of three unexplained biological monsters
for TREELINE, a modern snowbound mountaintop relay-station horror game. The creatures
must feel related through frozen pressure-cracked anatomy, but their silhouettes
must be unmistakably different even as solid black shapes.
Scene/backdrop: neutral charcoal studio background, subtle floor line, no props
Subject: three-panel lineup. Left, an extremely low wedge-shaped quadruped Crawler
with a floor-scraping shovel skull and underside mouth. Center, a Clinger shown as
a closed ceiling diamond and opened thin vertical drop pose. Right, a Colossus made
of a broad door-like split slab on four pillar limbs, not a scaled humanoid.
Style/medium: realistic game-ready 3D sculpt paintover, restrained low-frequency
detail, topology-aware joints, unsettling but not gory
Lighting/mood: cold soft key plus narrow warm flashlight rim
Color palette: near-black blue-gray flesh, desaturated frozen flesh, pale frost
scars, oxidized bruises, tiny warm wet accents only inside mouths/fissures
Materials/textures: matte leathery skin, chalky frost scars, sparse wet fissures,
no metal
Constraints: practical for rigging; no clothing, armor, machinery, cables, tools,
weapons, eyes, human faces, fur, antlers, gore, logos, captions, watermark or text
Avoid: generic zombie, xenomorph, spider, gorilla, human giant, insect shell,
fantasy demon, cyborg, excessive spikes, noisy microdetail
```

생성 방식: Codex 기본 내장 이미지 생성. 결과는
`treeline-creature-concept-v1.png`로 프로젝트에 복사했다.

## 4방향 생성 규칙

통합 콘셉트와 각 적의 확정 정면을 레퍼런스로 함께 넣고 방향마다 내장 이미지
생성을 한 번씩 독립 실행했다. 공통 프롬프트 골격은 다음과 같다.

```text
Use case: stylized-concept
Asset type: single-view orthographic creature turnaround reference for 3D modeling
Input images: Image 1 is the original family concept; Image 2 is the authoritative
front view. Reconstruct the exact same individual, anatomy, dimensions, limb count,
surface language and neutral stance.
Primary request: Render the exact FRONT / BACK / LEFT SIDE / RIGHT SIDE view.
Composition: isolated single creature, entire body visible, near-orthographic 85mm
lens, centered, square canvas, generous padding.
Lighting: soft neutral cool studio lighting, even value readability, minimal shadow.
Constraints: no three-quarter rotation, no redesign, no pose change, no extra limbs,
no human, no props, no environment, no label, no text, no watermark.
```

방향 정의는 모델 로컬 +Z 전방 기준이다. `front`는 +Z에서 얼굴/공격구를 보는
방향, `back`은 -Z에서 보는 방향, `left/right`는 각각 개체의 좌우 측면이다.
Colossus 정면 1차 결과는 뒤쪽 두 기둥이 떠 있어 폐기하고, 네 발이 같은 지면에
닿도록 한 번 수정한 결과를 `01-front.png`로 채택했다.

## v2 생성 기록

### `CLINGER-V2.0`

```text
Preserve the thin vertical hanging silhouette. Twist the four ribbon limbs into
alternating anatomically wrong joint axes with corkscrew torsion between clear,
riggable joints. Embed asymmetrical wet lidless non-glowing eyes around the frontal
central fissure. Cold flashlight catchlights only; no human face, wings, gore or
extra limbs. Back view has no eyes.
```

최종 생성 결과가 프롬프트의 7안보다 자연스러운 8안으로 나와 8안을 채택했다.
저장 위치: `clinger/v2/clinger-v2-0{1..4}-*.png`.

### `COLOSSUS-V2.0`

```text
Transform the rounded v1 body into a 4.5m-tall unkillable moving biological
fortress: two layered frost-pressure slabs, exactly four pillar limbs, front pair
ending in closed prehensile clamps and rear pair load-bearing. A deep frontal abyss
contains three large and twelve tiny recessed non-glowing eyes at different depths.
No weak point, mouth, head, humanoid anatomy, stone golem or emission.
```

첫 v2 정면은 다리가 6개 생성되어 폐기하고, 안쪽 여분 다리 둘을 제거한 4사지
수정본을 채택했다. 저장 위치: `colossus/v2/colossus-v2-0{1..4}-*.png`.
