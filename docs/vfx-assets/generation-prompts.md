# Treeline 파티클 VFX 생성 프롬프트

도구: Codex 내장 이미지 생성. 세 결과 모두 `#00ff00` 크로마 배경으로 생성한 뒤
`remove_chroma_key.py --auto-key border --soft-matte --despill`로 알파 PNG를 만들었다.

## 얼음 파열

```text
Use case: stylized-concept
Asset type: Unity Particle System billboard texture for desktop WebGL horror game
Primary request: one isolated windblown snow crystal particle sprite, a compact irregular cluster of sharp ice grains with a short directional tail, designed to repeat with random rotation and size
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: one centered opaque pale snow-and-ice cluster, readable at 64 pixels, no surrounding scene
Style/medium: production-ready painted game particle sprite; restrained realistic frost, crisp silhouette, limited soft edge only inside the subject
Composition/framing: square canvas, centered subject occupying about 55 percent, generous empty padding, straight-on
Color palette: frost white #DDE4E2, cold gray #9EAAAC, tiny blue-gray #718087; do not use green
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; crisp separated edge; no cast shadow; no contact shadow; no text; no logo; no watermark
Avoid: decorative six-point holiday snowflake, glitter, bokeh, lens flare, scene background, multiple separate subjects
```

생성 결과가 미세한 눈송이보다 파열 중심에 가까워 런타임 이름을 `ice-burst`로
정했다. 눈보라 자체는 제작 스크립트의 가벼운 `snow-streak`를 사용한다.

## 연료 화염

```text
Use case: stylized-concept
Asset type: Unity Particle System billboard texture for desktop WebGL horror game
Primary request: one isolated compact fuel-fire flame tongue sprite for a burning diesel spill, shaped as a single tapered lick of flame with a broad base and two uneven tips, designed to repeat with random rotation and scale
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: one centered opaque stylized flame shape only, no smoke, no embers, no fuel puddle, no surrounding scene
Style/medium: production-ready painted game particle sprite; restrained realistic industrial fire; graphic readable silhouette with crisp outer edge and layered hot core
Composition/framing: square canvas, centered subject occupying about 55 percent, generous empty padding, straight-on
Color palette: pale hot core #F2E2BE, muted amber #C78345, dark bruised red #713E35; do not use green
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; crisp separated edge; no cast shadow; no contact shadow; no text; no logo; no watermark
Avoid: translucent smoke, realistic soft glow outside silhouette, campfire logs, magical flame, blue flame, multiple separate subjects, scene background
```

## 총구 섬광

```text
Use case: stylized-concept
Asset type: Unity Particle System billboard texture for desktop WebGL first-person horror game
Primary request: one isolated irregular muzzle-flash burst sprite, a compact hot central diamond with four uneven tapered rays and a few short side spikes, intended for a single-frame pistol or shotgun flash
Scene/backdrop: perfectly flat solid #00ff00 chroma-key background for local background removal
Subject: one centered opaque flash silhouette only, no gun, no smoke, no shell casing, no surrounding scene
Style/medium: production-ready painted game particle sprite; harsh industrial firearm flash; crisp graphic silhouette readable at 128 pixels
Composition/framing: square canvas, centered subject occupying about 55 percent, generous empty padding, straight-on
Color palette: hot white #F4E8C7, muted amber #D69A55, tiny bruised orange-red #8A4A38; do not use green
Constraints: background must be one perfectly uniform #00ff00 color with no shadow, gradient, texture, reflection, floor plane, or lighting variation; crisp separated edge; no cast shadow; no contact shadow; no lens flare circles; no text; no logo; no watermark
Avoid: weapon illustration, explosion cloud, magical star, symmetrical eight-point icon, soft glow outside the silhouette, multiple separate subjects, scene background
```
