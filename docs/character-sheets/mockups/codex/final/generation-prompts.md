# 이미지 생성 기록

Codex 기본 내장 이미지 생성의 `stylized-concept` 방식으로 제작했다. CLI/API fallback은
사용하지 않았다.

## 최종 4면

입력 레퍼런스는 `../concept-options/M-C-cableway-electrical-technician.png`다.

```text
Reconstruct the exact selected M-C Cableway Electrical Technician in a neutral rigging
stance and rotate him into strict orthographic FRONT / BACK / LEFT / RIGHT views.
Preserve the exact face, tall wiry body, shaved hair, charcoal high-collar shell,
chest strap, waist belt, olive work trousers, repaired LEFT sleeve, black long-cuff
gloves, boots, colors and wear. One complete head-to-toe character, no redesign,
weapon, equipment, logo, text, perspective or watermark.
```

## 애니메이션 보드

동일 선택 원본을 레퍼런스로 사용하고 각 보드를 독립 생성했다.

```text
Create exactly four sequential full-body key poses of the exact same selected M-C
technician. Preserve identity, costume, straps, left-sleeve repair, colors and body
proportions. Grounded civilian mechanic motion with physical weight, neutral charcoal
background, complete silhouettes and no labels. Board groups: locomotion; crouch;
jump-land; facility interaction; two-handed carry; pistol; pump shotgun; hit-death.
Weapons are unbranded matte-gray animation proxies, not final weapon designs.
```

이미지는 방향과 동작 의도용이다. 본 회전각, 접지 프레임, 이벤트 시점은
`animation/*.md` 문서를 우선한다.

