"""ambientCG 의 CC0 텍스처 세트를 Unity URP 규약으로 변환해 넣는다.

왜 변환이 필요한가
-----------------
ambientCG 는 Roughness 와 Metalness 를 **따로** 준다. 그런데 URP Lit 의
`_MetallicGlossMap` 은 한 장에 R=metallic, A=smoothness 로 담기를 요구하고,
smoothness 는 roughness 의 **반대값**이다. 그대로 넣으면 거칠어야 할 면이
거울처럼 번들거린다.

노멀은 `NormalGL` 을 쓴다. `NormalDX` 는 Y 가 뒤집혀 있어 Unity 에서 요철이
반대로 파인다.

받는 곳
------
https://ambientcg.com — 전부 CC0. 라이선스상 커밋해도 되고, 커밋하면
`Assets/ThirdParty/` 동기화 문제(셋이 각자 임포트해야 GUID 가 맞는 것)가
통째로 사라진다.

사용법
-----
    python docs/world-textures/import_cc0.py --src <압축 푼 폴더> [--size 512]
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image

DEFAULT_OUT = Path(__file__).resolve().parents[2] / "Treeline/Assets/Game/World/TexturesCC0"

# ambientCG 에셋 ID -> 이 프로젝트에서 쓸 이름. 절차적 생성분과 짝을 맞춰
# 같은 자리에 놓고 비교할 수 있게 한다.
MAPPING = {
    "Concrete034": "cc0_concrete_wall",
    "Concrete030": "cc0_concrete_floor",
    "Metal032": "cc0_painted_steel",
    "Metal021": "cc0_rusted_steel",
}

# IMPORTANT: **CC0 라고 타일링이 보장되지 않는다.** 처음 고른 Concrete031 은 원본
# 1K 에서 이미 세로 이음매가 있었다 (끝단 불연속이 내부의 3.7 배). 리사이즈 탓인 줄
# 알고 원본을 따로 쟀다가 확인했다. 받은 즉시 검사하고 통과한 것만 쓴다.
#   python docs/world-textures/seam_check.py <폴더>



def _load(folder: Path, asset: str, suffix: str) -> Image.Image | None:
    hits = list(folder.glob(f"{asset}_*_{suffix}.jpg"))
    return Image.open(hits[0]) if hits else None


def convert(folder: Path, asset: str, name: str, out: Path, size: int) -> str:
    color = _load(folder, asset, "Color")
    normal = _load(folder, asset, "NormalGL")
    rough = _load(folder, asset, "Roughness")
    metal = _load(folder, asset, "Metalness")
    ao = _load(folder, asset, "AmbientOcclusion")

    if color is None:
        return f"{asset}: Color 없음 — 건너뜀"

    out.mkdir(parents=True, exist_ok=True)
    box = Image.Resampling.LANCZOS

    color.convert("RGB").resize((size, size), box).save(out / f"{name}_albedo.png")
    if normal is not None:
        normal.convert("RGB").resize((size, size), box).save(out / f"{name}_normal.png")

    # mask 합성 — R=metallic, A=smoothness(=1-roughness)
    r = np.asarray(rough.convert("L").resize((size, size), box), dtype=np.float32) / 255.0 \
        if rough is not None else np.full((size, size), 0.6, dtype=np.float32)
    m = np.asarray(metal.convert("L").resize((size, size), box), dtype=np.float32) / 255.0 \
        if metal is not None else np.zeros((size, size), dtype=np.float32)

    smooth = 1.0 - r                      # <- 여기가 이 스크립트의 핵심
    zeros = np.zeros_like(m)
    mask = np.stack([m, zeros, zeros, smooth], axis=-1)
    Image.fromarray(np.clip(mask * 255 + 0.5, 0, 255).astype(np.uint8), "RGBA") \
        .save(out / f"{name}_mask.png")

    made = ["albedo", "mask"]
    if normal is not None:
        made.append("normal")
    if ao is not None:
        ao.convert("L").resize((size, size), box).save(out / f"{name}_ao.png")
        made.append("ao")
    return f"{asset} -> {name} ({', '.join(made)})"


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--src", type=Path, required=True,
                    help="ambientCG 압축을 푼 상위 폴더 (에셋 ID 별 하위 폴더)")
    ap.add_argument("--out", type=Path, default=DEFAULT_OUT)
    ap.add_argument("--size", type=int, default=512,
                    help="출력 한 변. 절차적 생성분과 맞춰 비교하려면 같은 값")
    args = ap.parse_args()

    print(f"{args.size}px -> {args.out}")
    for asset, name in MAPPING.items():
        folder = args.src / asset
        if not folder.is_dir():
            print(f"  {asset}: 폴더 없음")
            continue
        print("  " + convert(folder, asset, name, args.out, args.size))


if __name__ == "__main__":
    main()
