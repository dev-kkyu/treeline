# Treeline Particle VFX — V2 Art Direction

기존 V1의 게임 아이콘형 실루엣을 폐기하고, 레벨 콘셉트의 저채도 산업 공포 톤에 맞춘 재설계 시안이다.

## 시안

- `previews/treeline-vfx-direction-v2.png`
- 배열: 눈보라 / 서리 먼지 / 총구 섬광 / 연료 화염 / 전기 스파크 / 수소 폭발
- 현재 파일은 룩 검수용 콘셉트 시트다. 승인 전에는 V1 텍스처·머티리얼·프리팹을 교체하지 않는다.

## 핵심 방향

- 현상은 완결된 아이콘이 아니라 흩어지고 찢어진 입자 덩어리로 보이게 한다.
- 눈과 서리는 유리 결정 대신 얼음 가루, 작은 파편, 불규칙한 바람 결을 사용한다.
- 총구 섬광은 작은 비대칭 화약 섬광과 매연으로 제한한다.
- 연료 화염은 바닥을 기는 낮은 디젤 화염과 검댕을 중심으로 한다.
- 전기는 분절된 가지형 아크와 극소량의 금속 불티로 표현한다.
- 폭발은 마법성 얼음 파편이 아니라 압력파, 젖은 먼지, 암색 파편으로 표현한다.
- 네온, 판타지 크리스털, 대칭 별 모양, 혜성 모양, 둥근 발광 구체, 과도한 블룸을 사용하지 않는다.

## 생성 방식

Codex 내장 이미지 생성 도구로 아래 프로젝트 레퍼런스를 함께 사용했다.

- `docs/level-sheets/mockups/codex/ch03-fuel-mine/v2-eerie/01-entry-wide.png`
- `docs/level-sheets/mockups/codex/ch04-generator-nest/v2-eerie/01-entry-wide.png`
- `docs/level-sheets/mockups/codex/ch05-exterior-towers/v2-eerie/01-entry-wide.png`
- `docs/enemy-sheets/mockups/codex/treeline-creature-concept-v1.png`

## 최종 프롬프트

```text
Use case: stylized-concept
Asset type: professional game VFX art-direction contact sheet for Unity URP desktop WebGL
Primary request: redesign six grounded particle-effect looks for the bleak industrial survival-horror game TREELINE, using the supplied mine, generator nest, blizzard exterior, and creature references only as art-direction anchors.
Layout: exact 3-by-2 grid of six equal dark charcoal specimen bays, each bay showing exactly one isolated effect at useful game scale. Top row, left to right: irregular wind-torn granular snow streak cluster; subtle chipped frost grit and cold dust; small asymmetric dirty firearm muzzle flash. Bottom row, left to right: low crawling diesel spill flame sheet with soot-dark tips; broken branching electrical arc with a few hot metal flecks; dirty hydrogen pressure burst with dark debris, wet dust and only a trace of cold condensation.
Style/medium: production-ready realistic game VFX concept sheet, restrained cinematic industrial horror, physically grounded, photographic particulate texture, shapes designed to be recreated with Unity Particle System flipbooks and additive/alpha sprites.
Lighting/mood: nearly black wet industrial environment; effects illuminate only a tight local area; oppressive, cold, grimy and dangerous.
Color palette: charcoal black, slate blue-gray, dirty frost white; minimal desaturated tungsten amber for combustion and sparks; tiny bruised red accents only where natural.
Composition/framing: each effect centered with ample empty space and fully contained within its bay; effects must read as fragments and transient phenomena, never as emblems or icons.
Constraints: no text, no labels, no title, no logos, no watermark, no borders crossing effects, no characters, no weapons, no environment props. Preserve the supplied references' low-key exposure, wet grime, muted palette, biological-industrial unease.
Avoid: cartoon fire, fantasy crystals, magic, neon, colorful glow, clean symmetry, four-point starburst, comet shapes, smooth glowing orbs, clip-art, mobile-game icons, polished advertising, exaggerated bloom, blue plasma balls.
```

