# UI 이미지 생성 프롬프트

> 도구: Codex 내장 이미지 생성 · 생성일 2026-08-08

공통 팔레트는 적 모델링 규격의 `#252B2E`, `#4E585C`, `#B9C1BC`, `#72524D`,
`#8A4337`을 사용했다. 모든 원본은 단색 `#00ff00` 배경으로 생성하고 설치된
크로마 제거 도구의 border auto-key, soft matte, despill 설정으로 투명 PNG를 만들었다.

## 패널 프레임

```text
Use case: stylized-concept
Asset type: Unity game UI 9-slice panel frame
Primary request: one empty rectangular industrial horror UI frame for the first-person game Treeline, inspired by an abandoned mountain communications relay station and frost-pressure anatomy
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: a single wide rectangular frame only, no filled panel, with strong readable outer silhouette and large clean empty center
Style/medium: production-ready painted game UI sprite; restrained utilitarian analog equipment; cold worn steel, chipped enamel, subtle pressure cracks and frost scars; not ornate fantasy
Composition/framing: centered straight-on orthographic view, symmetrical overall but lightly distressed, generous padding, uninterrupted straight edges suitable for 9-slicing, corner details kept inside a 72px-style border
Lighting/mood: low-contrast neutral asset lighting, cold and tense
Color palette: charcoal #252B2E, slate #4E585C, frost #B9C1BC, tiny restrained rust-red #8A4337 under 3 percent
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; do not use #00ff00 anywhere in the frame; crisp edges; no cast shadow; no contact shadow; no text; no symbols; no logo; no watermark
Avoid: sci-fi hologram, cyberpunk neon, gothic ornament, skulls, gore, screws protruding outside silhouette, perspective tilt, rounded mobile-app card
```

## 버튼 프레임

```text
Use case: stylized-concept
Asset type: Unity game UI 9-slice button frame
Primary request: one empty compact horizontal button frame for the first-person horror game Treeline, matching an abandoned mountain communications relay station
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: a single narrow horizontal rectangular frame only, large completely empty center for Korean menu text added later in Unity; simple cut corners; restrained frost scars and pressure cracks
Style/medium: production-ready painted game UI sprite, utilitarian 1980s analog relay equipment, cold worn steel and chipped enamel, tactile not futuristic
Composition/framing: centered straight-on orthographic view, generous padding, clean continuous edges suitable for 9-slicing, approximately 4:1 width-to-height
Lighting/mood: neutral asset lighting, cold and tense
Color palette: charcoal #252B2E, slate #4E585C, frost #B9C1BC, tiny restrained rust-red #8A4337 under 2 percent
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; do not use #00ff00 in the frame; crisp edges; no cast shadow; no contact shadow; no text; no symbols; no logo; no watermark
Avoid: sci-fi hologram, cyberpunk neon, gothic ornament, skulls, gore, perspective tilt, rounded mobile-app button, excessive screws
```

## 체력 칸

```text
Use case: stylized-concept
Asset type: Unity game HUD health pip sprite
Primary request: one compact health marker for Treeline representing one of exactly three health segments, shaped like a rugged relay-station fuse plate fractured by frost pressure
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: a single small vertical rectangular metal-ceramic indicator plate with a blunt strong silhouette, one shallow central pressure split, readable at 32 to 64 pixels, no heart symbol
Style/medium: production-ready painted game UI icon; restrained analog industrial horror; cold worn enamel and dark steel; not decorative
Composition/framing: centered straight-on orthographic icon, generous padding, no perspective, no shadow, approximately 2:3 width-to-height
Lighting/mood: neutral high-readability asset lighting, cold and tense
Color palette: dark charcoal #252B2E border, frost ceramic #B9C1BC face, muted bruised red #72524D visible only inside the split
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; do not use #00ff00 in subject; crisp closed silhouette; no cast shadow; no contact shadow; no text; no numbers; no logo; no watermark
Avoid: heart icon, medical cross, skull, gore, fantasy rune, neon glow, perspective tilt, overly fine cracks
```

## 배터리 프레임

```text
Use case: stylized-concept
Asset type: Unity game HUD flashlight battery gauge frame
Primary request: one empty horizontal battery gauge casing for Treeline, resembling a battered analog relay-station meter with frost pressure damage; the fill will be drawn separately in Unity
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: a single compact horizontal meter frame with a large clean empty rectangular inner slot, a small broken-terminal notch at one end, readable at about 180 by 36 pixels
Style/medium: production-ready painted game UI sprite; restrained analog industrial horror, worn steel and chipped off-white enamel
Composition/framing: centered straight-on orthographic view, approximately 5:1 width-to-height, generous padding, crisp silhouette, no perspective
Lighting/mood: neutral high-readability asset lighting, cold and tense
Color palette: charcoal #252B2E, slate #4E585C, frost #B9C1BC, tiny rust-red #8A4337 under 2 percent
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; do not use #00ff00 in subject; no fill inside the gauge; no cast shadow; no contact shadow; no text; no numbers; no battery lightning symbol; no logo; no watermark
Avoid: neon glow, sci-fi hologram, steampunk brass, fantasy ornament, gore, perspective tilt, excessive micro-detail
```

## 피격 오버레이

```text
Use case: stylized-concept
Asset type: Unity full-screen damage and cold vignette overlay sprite
Primary request: sparse frost-pressure cracks and chipped frozen grime creeping inward only from the four screen edges for Treeline; it appears briefly when the player is hurt
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: thin irregular frost scars, pressure cracks, and a few dark bruised-red smears restricted to the outer 12 percent of a wide 16:9 canvas; the middle 70 percent must remain completely unobstructed solid green
Style/medium: production-ready painted game HUD overlay, realistic restrained frozen-glass and cracked enamel texture, non-diegetic but subtle
Composition/framing: 16:9 wide frame, asymmetric edge clusters, strongest in two corners, large clean center, no enclosed border frame
Lighting/mood: cold, claustrophobic, low visibility
Color palette: frost white #B9C1BC, slate #4E585C, charcoal #252B2E, tiny muted bruised red #72524D under 2 percent
Constraints: background must be exactly uniform flat #00ff00 with no shadow, gradient, texture, reflection, or lighting variation; do not use #00ff00 in subject; no continuous opaque frame; no center obstruction; no cast shadow; no text; no logo; no watermark
Avoid: snowflakes, icicles, blood splatter, gore, spiderweb symmetry, camera lens flare, cinematic black bars, heavy opaque corners
```

## 스태미너 프레임

```text
Use case: stylized-concept
Asset type: Unity game HUD sprint stamina gauge frame
Primary request: one empty slim horizontal stamina gauge casing for Treeline, matching the existing abandoned mountain relay-station UI kit and frost-pressure anatomy
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: a single narrow mechanical pressure gauge frame with a large completely empty horizontal inner slot for a separate fill Image; subtly pinched ends and two small pressure-relief notches so it reads as exertion rather than a battery
Style/medium: production-ready painted game UI sprite; restrained analog industrial horror; worn dark steel, chipped frost enamel, sparse pressure cracks
Composition/framing: centered straight-on orthographic view, approximately 6:1 width-to-height, generous padding, crisp silhouette, no perspective
Lighting/mood: neutral high-readability asset lighting, cold and tense
Color palette: charcoal #252B2E, slate #4E585C, frost #B9C1BC, tiny muted bruised red #72524D under 2 percent
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; do not use #00ff00 in the subject; inner slot must be empty; no cast shadow; no contact shadow; no text; no numbers; no human figure; no running icon; no battery terminal; no lightning symbol; no logo; no watermark
Avoid: neon glow, sci-fi hologram, steampunk brass, fantasy ornament, gore, heart icon, perspective tilt, excessive micro-detail
```
## 튜토리얼 카드 프레임

```text
Use case: stylized-concept
Asset type: Unity first-person horror game tutorial card frame sprite
Primary request: one empty wide compact tutorial instruction card frame for Treeline, matching an abandoned mountain relay-station HUD and frost-pressure anatomy; it must hold a keycap or mouse icon on the left and two short lines of Unity TextMeshPro copy on the right
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: a single rectangular mechanical instruction card casing with a large completely empty dark inner field, clipped industrial corners, subtle frost scoring, one restrained bruised-red status notch, no dividers that force a specific text layout
Style/medium: production-ready painted game UI sprite; restrained analog industrial horror; worn charcoal steel, chipped pale frost enamel, sparse pressure cracks
Composition/framing: centered straight-on orthographic view, approximately 3.4:1 width-to-height, generous padding, crisp silhouette, no perspective
Lighting/mood: neutral high-readability asset lighting, cold and tense
Color palette: charcoal #252B2E, deep #0C1217, slate #4E585C, frost #B9C1BC, tiny muted bruised red #72524D under 2 percent
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; do not use #00ff00 in the subject; inner field must be empty; no cast shadow; no contact shadow; no text; no letters; no numbers; no keyboard keys; no mouse; no human figure; no logo; no watermark
Avoid: neon glow, sci-fi hologram, steampunk brass, fantasy ornament, gore, perspective tilt, excessive micro-detail
```
