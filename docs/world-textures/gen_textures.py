"""월드 표면 텍스처를 절차적으로 만든다 — 타일링이 보장되는 PBR 세트.

왜 절차적인가
-------------
이미지 생성 AI 는 **타일링을 보장하지 못한다.** 벽에 반복해 붙이면 이음매가
그대로 보이고, 후처리로 억지로 이어 붙이면 그 자리에 뭉갠 자국이 남는다.
주기 함수로 만들면 이음매가 애초에 생기지 않는다 — 격자 인덱스를 period 로
나눈 나머지로 감기 때문에 좌우/상하 끝이 수학적으로 일치한다.

덤으로 얻는 것
- API 키가 필요 없다
- 해상도를 자유롭게 바꾼다. WebGL 용량을 숫자 하나로 조절한다
- 파라미터만 바꿔 변주를 만든다 (마른 콘크리트 -> 젖은 콘크리트)
- **스크립트가 그대로 AI 활용 근거가 된다.** 결과물이 아니라 생성 과정이 남는다

출력
----
URP Lit 이 쓰는 세 장을 낸다.

    <이름>_albedo.png   베이스 컬러
    <이름>_normal.png   노멀맵 (하이트에서 Sobel 로 유도)
    <이름>_mask.png     R=metallic, A=smoothness  (URP _MetallicGlossMap 규약)

사용법
-----
    python docs/world-textures/gen_textures.py
    python docs/world-textures/gen_textures.py --size 512 --out <경로>
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image

# 저장소 루트 기준. Unity 가 읽는 자리에 바로 떨어뜨린다.
DEFAULT_OUT = Path(__file__).resolve().parents[2] / "Treeline/Assets/Game/World/Textures"

# 결정론적으로 만든다. 같은 커밋에서 같은 텍스처가 나와야 셋이 같은 화면을 본다.
SEED = 20260808


# --------------------------------------------------------------------------
# 주기 노이즈 — 타일링의 근거
# --------------------------------------------------------------------------

def _periodic_value_noise(size: int, cells: int, rng: np.random.Generator) -> np.ndarray:
    """cells x cells 격자의 값 노이즈. 끝이 감기므로 이어 붙여도 이음매가 없다."""
    # 격자에 난수를 깔고, 마지막 행/열을 첫 행/열로 되감아 연속을 만든다.
    lattice = rng.random((cells, cells), dtype=np.float64)

    # 각 픽셀이 격자 어디에 떨어지는지
    t = (np.arange(size, dtype=np.float64) + 0.5) * cells / size
    i0 = np.floor(t).astype(np.int64) % cells
    i1 = (i0 + 1) % cells               # <- 여기서 감긴다
    frac = t - np.floor(t)

    # smoothstep. 선형 보간으로 두면 격자 경계가 격자무늬로 드러난다.
    f = frac * frac * (3.0 - 2.0 * frac)

    fx = f[None, :]
    fy = f[:, None]

    v00 = lattice[np.ix_(i0, i0)]
    v01 = lattice[np.ix_(i0, i1)]
    v10 = lattice[np.ix_(i1, i0)]
    v11 = lattice[np.ix_(i1, i1)]

    top = v00 * (1.0 - fx) + v01 * fx
    bottom = v10 * (1.0 - fx) + v11 * fx
    return top * (1.0 - fy) + bottom * fy


def fbm(size: int, base_cells: int, octaves: int, persistence: float,
        rng: np.random.Generator) -> np.ndarray:
    """옥타브를 겹친 주기 노이즈. 0~1 로 정규화해 돌려준다.

    IMPORTANT: 옥타브마다 cells 가 2 배가 되는데, size 가 그 배수여야 격자가
    픽셀에 딱 떨어진다. 1024 / 512 는 base_cells=4 에서 6 옥타브까지 안전하다.
    """
    total = np.zeros((size, size), dtype=np.float64)
    amplitude = 1.0
    norm = 0.0
    cells = base_cells

    for _ in range(octaves):
        total += _periodic_value_noise(size, cells, rng) * amplitude
        norm += amplitude
        amplitude *= persistence
        cells *= 2

    total /= norm
    lo, hi = total.min(), total.max()
    return (total - lo) / max(hi - lo, 1e-9)


def worley(size: int, cells: int, rng: np.random.Generator,
           order: int = 0) -> np.ndarray:
    """주기 셀룰러(Worley) 노이즈. 유기적인 것에는 이것이 필요하다.

    value 노이즈는 뭉게구름이라 살 · 섬유질 · 갈라진 껍질이 안 나온다. 특징점까지의
    거리를 재면 세포막 같은 경계가 생기고, 그것이 둥지 표면의 근거가 된다.

    격자 인덱스를 cells 로 감아 이웃을 찾으므로 타일링이 유지된다.
    """
    # 셀마다 특징점 하나. 위치는 셀 안에서 무작위.
    px = (np.arange(cells)[:, None] + rng.random((cells, cells))) / cells
    py = (np.arange(cells)[None, :] + rng.random((cells, cells))) / cells

    u = (np.arange(size) + 0.5) / size
    gx = np.floor(u * cells).astype(np.int64)

    best = np.full((size, size), 1e9)
    second = np.full((size, size), 1e9)

    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ix = (gx[:, None] + dy) % cells          # 행 방향
            iy = (gx[None, :] + dx) % cells          # 열 방향
            # 감긴 이웃은 좌표도 함께 밀어 줘야 거리가 맞는다
            ox = px[ix, iy] + ((gx[:, None] + dy) // cells)
            oy = py[ix, iy] + ((gx[None, :] + dx) // cells)
            d = np.sqrt((u[:, None] - ox) ** 2 + (u[None, :] - oy) ** 2)
            second = np.minimum(second, np.maximum(best, d))
            best = np.minimum(best, d)

    f = second - best if order == 1 else best        # F2-F1 이면 막(膜)이 된다
    lo, hi = f.min(), f.max()
    return (f - lo) / max(hi - lo, 1e-9)


def normal_from_height(height: np.ndarray, strength: float) -> np.ndarray:
    """하이트맵을 노멀맵으로. np.roll 로 미분해서 경계도 감긴다."""
    # roll 이라 좌우/상하가 이어진다. 노멀맵도 타일링이 유지된다.
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5

    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(height)

    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx, ny, nz = nx / length, ny / length, nz / length

    # 접선 공간 노멀맵 규약: [-1,1] -> [0,1]
    return np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, nz * 0.5 + 0.5], axis=-1)


# --------------------------------------------------------------------------
# 표면 정의
# --------------------------------------------------------------------------

def _write(out: Path, name: str, albedo: np.ndarray, normal: np.ndarray,
           metallic: np.ndarray, smoothness: np.ndarray) -> None:
    out.mkdir(parents=True, exist_ok=True)

    def to8(a: np.ndarray) -> np.ndarray:
        return np.clip(a * 255.0 + 0.5, 0, 255).astype(np.uint8)

    Image.fromarray(to8(albedo), "RGB").save(out / f"{name}_albedo.png")
    Image.fromarray(to8(normal), "RGB").save(out / f"{name}_normal.png")

    # URP _MetallicGlossMap: R = metallic, A = smoothness. G·B 는 안 쓴다.
    zeros = np.zeros_like(metallic)
    mask = np.stack([metallic, zeros, zeros, smoothness], axis=-1)
    Image.fromarray(to8(mask), "RGBA").save(out / f"{name}_mask.png")

    print(f"  {name}_albedo.png / _normal.png / _mask.png")


def concrete(out: Path, size: int, name: str, base: tuple[float, float, float],
             wet: bool) -> None:
    """콘크리트. 큰 얼룩 + 잔 알갱이. 젖으면 어둡고 매끈해진다."""
    rng = np.random.default_rng(SEED + (1 if wet else 0))

    stain = fbm(size, 4, 5, 0.55, rng)      # 넓은 얼룩
    grain = fbm(size, 64, 3, 0.5, rng)      # 표면 알갱이
    height = stain * 0.75 + grain * 0.25

    # 밝기 변화를 크게 주면 손전등이 훑을 때 얼룩만 보이고 형태가 안 읽힌다.
    shade = 0.82 + stain * 0.26 + (grain - 0.5) * 0.10
    albedo = np.stack([np.clip(np.asarray(base[i]) * shade, 0, 1) for i in range(3)], axis=-1)

    normal = normal_from_height(height, strength=size / 190.0)

    metallic = np.zeros((size, size))
    if wet:
        # 물이 고인 곳만 매끈하게. 전면을 올리면 통째로 번들거려 플라스틱이 된다.
        pool = np.clip((stain - 0.45) * 2.4, 0, 1)
        smooth = 0.14 + pool * 0.52
        albedo *= (1.0 - pool * 0.35)[..., None]
    else:
        smooth = 0.10 + grain * 0.06

    _write(out, name, albedo, normal, metallic, smooth)


def painted_steel(out: Path, size: int, name: str,
                  base: tuple[float, float, float]) -> None:
    """도장 철판. 평평하되 긁힘과 도장 얼룩. metallic 은 낮게 둔다.

    IMPORTANT: metallic 을 높이면 손전등을 정면으로 받는 면이 통째로 하얗게 날아가
    형태가 사라진다. CorridorMaterials 가 단색으로 이미 같은 판단을 해두었다.
    """
    rng = np.random.default_rng(SEED + 2)

    coat = fbm(size, 6, 4, 0.5, rng)
    scratch = fbm(size, 160, 2, 0.45, rng)
    # 긁힘은 가늘어야 한다. threshold 를 높여 얇게만 남긴다.
    scratch = np.clip((scratch - 0.72) * 7.0, 0, 1)

    # IMPORTANT: 긁힘을 하이트의 65% 로 두었다가 천장이 통째로 얼룩졌다. threshold 로
    # 자른 자국은 경계가 각져서 노멀이 세게 서고, 스폿 손전등이 그것만 잡아낸다.
    # 도장면은 거의 평평한 것이 맞다. 긁힘은 알베도로만 남기고 형태로는 거의 안 준다.
    height = coat * 0.88 + scratch * 0.12
    shade = 0.90 + (coat - 0.5) * 0.16 + scratch * 0.10
    albedo = np.stack([np.clip(np.asarray(base[i]) * shade, 0, 1) for i in range(3)], axis=-1)

    normal = normal_from_height(height, strength=size / 900.0)

    # IMPORTANT: 1차 시도에서 metallic 0.5 · smoothness 0.52 를 줬다가 천장이 통째로
    # 하얗게 날아갔다. 손전등은 스폿이라 정반사가 한 점에 몰리는데, 그 값이면 긁힘이
    # 형태가 아니라 노이즈로 보인다. CorridorMaterials 가 단색 머티리얼에서 이미 같은
    # 판단을 해두었다 — 도장면은 metallic 을 낮게, smoothness 를 중간으로.
    metallic = np.full((size, size), 0.08) + scratch * 0.10
    smooth = 0.26 + (coat - 0.5) * 0.08 + scratch * 0.10

    _write(out, name, albedo, normal, metallic, smooth)


def rusted_steel(out: Path, size: int, name: str) -> None:
    """녹슨 철판. 녹이 번진 자리는 거칠고 metallic 이 죽는다."""
    rng = np.random.default_rng(SEED + 3)

    patch = fbm(size, 5, 5, 0.58, rng)
    grit = fbm(size, 80, 3, 0.5, rng)
    rust = np.clip((patch - 0.40) * 2.2, 0, 1)

    height = patch * 0.55 + grit * 0.45 + rust * 0.25

    steel = np.array([0.37, 0.40, 0.42])
    iron = np.array([0.42, 0.22, 0.13])
    shade = (0.88 + (grit - 0.5) * 0.22)[..., None]
    albedo = np.clip((steel * (1 - rust[..., None]) + iron * rust[..., None]) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 170.0)
    metallic = np.clip(0.45 * (1.0 - rust), 0, 1)
    smooth = np.clip(0.34 * (1.0 - rust) + 0.04, 0, 1)

    _write(out, name, albedo, normal, metallic, smooth)


def snow(out: Path, size: int, name: str) -> None:
    """눈. 5장 야외. 밝고 균질하되 미세하게 반짝인다.

    밝은 표면이라 오히려 쉽다 — 손전등이 닿으면 다 보이므로 형태만 맞으면 된다.
    """
    rng = np.random.default_rng(SEED + 4)

    drift = fbm(size, 3, 4, 0.6, rng)       # 바람에 쓸린 큰 결
    crust = fbm(size, 48, 3, 0.5, rng)      # 표면 결정
    height = drift * 0.8 + crust * 0.2

    shade = 0.90 + drift * 0.10 + (crust - 0.5) * 0.06
    base = np.array([0.86, 0.89, 0.94])     # 눈은 살짝 푸르다
    albedo = np.clip(base * shade[..., None], 0, 1)

    normal = normal_from_height(height, strength=size / 260.0)
    metallic = np.zeros((size, size))
    # 결정이 반짝이는 자리만 매끈하게. 전면을 올리면 얼음판이 된다.
    sparkle = np.clip((crust - 0.72) * 6.0, 0, 1)
    smooth = 0.16 + sparkle * 0.45

    _write(out, name, albedo, normal, metallic, smooth)


def metal_grating(out: Path, size: int, name: str, cells: int = 8) -> None:
    """금속 그레이팅. 5장 철탑 · 6장 승강장 바닥.

    IMPORTANT: **노이즈로 만들지 않는다.** 규칙적인 격자라 도형으로 그리는 것이
    정확하고 싸다. 절차적 생성의 강점이 노이즈에만 있는 것이 아니다 — 이런 것은
    사진 텍스처를 구하는 것보다 오히려 쉽다.
    """
    rng = np.random.default_rng(SEED + 5)

    u = (np.arange(size) + 0.5) / size * cells
    fx = np.abs((u % 1.0) - 0.5) * 2.0            # 셀 안에서 0(중앙)~1(경계)
    bar_x = fx[None, :]
    bar_y = fx[:, None]

    bar_w = 0.62                                   # 살의 두께
    solid = np.maximum(bar_x, bar_y)
    metal = np.clip((solid - bar_w) / (1.0 - bar_w), 0, 1)   # 1 = 살, 0 = 구멍

    grit = fbm(size, 64, 3, 0.5, rng)
    height = metal * 0.9 + grit * 0.10

    steel = np.array([0.30, 0.32, 0.33])
    void = np.array([0.02, 0.02, 0.03])            # 구멍은 아래가 안 보이는 어둠
    shade = (0.88 + (grit - 0.5) * 0.24)[..., None]
    albedo = np.clip((void + (steel - void) * metal[..., None]) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 130.0)
    metallic = metal * 0.55
    smooth = 0.12 + metal * 0.30

    _write(out, name, albedo, normal, metallic, smooth)


def nest(out: Path, size: int, name: str) -> None:
    """둥지. 4장 발전동. 벽과 천장을 덮은 살과 섬유질.

    셀룰러 노이즈의 F2-F1 이 세포막 같은 경계를 만든다. value 노이즈는 뭉게구름이라
    이것이 안 나온다. 젖은 자리만 smoothness 를 올려 「물컹하다」를 만든다 —
    바닥 웅덩이와 같은 수법이고, 팀이 그쪽을 무섭다고 한 이유이기도 하다.
    """
    rng = np.random.default_rng(SEED + 6)

    membrane = worley(size, 18, rng, order=1)      # 막
    sinew = worley(size, 44, rng, order=1)         # 잔 섬유
    blotch = fbm(size, 5, 4, 0.6, rng)             # 넓은 얼룩

    height = membrane * 0.55 + sinew * 0.25 + blotch * 0.20

    # IMPORTANT: 처음에 (0.19, 0.055, 0.045) 로 어둡게 깔았다가 **차가운 손전등 아래서
    # 붉은기가 사라지고 회색 돌처럼 보였다.** 이 게임의 유일한 광원이 흰 손전등이므로
    # 채도를 눈에 보일 만큼 남겨야 한다. 어둡게 만드는 것은 조명이 이미 하고 있다.
    flesh = np.array([0.34, 0.085, 0.070])
    pale = np.array([0.50, 0.24, 0.19])
    t = np.clip(membrane * 0.7 + blotch * 0.3, 0, 1)[..., None]
    shade = (0.80 + blotch * 0.30)[..., None]
    albedo = np.clip((flesh + (pale - flesh) * t) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 220.0)
    metallic = np.zeros((size, size))

    # IMPORTANT: 1차에서 젖은 정도를 0.55 까지 줬다가 **살이 아니라 얼음으로 보였다.**
    # 이 게임의 빛은 차가운 손전등뿐이라, smoothness 가 높으면 정반사가 그 색을
    # 그대로 되돌려 붉은 알베도를 통째로 덮는다. 골에만 조금 고이게 둔다.
    wet = np.clip((1.0 - membrane - 0.25) * 1.8, 0, 1)
    smooth = 0.10 + wet * 0.22

    _write(out, name, albedo, normal, metallic, smooth)


# 기획서 여섯 장이 요구하는 표면 전부.
SURFACES = {
    "concrete_wall":  lambda o, s: concrete(o, s, "concrete_wall", (0.55, 0.57, 0.56), wet=False),
    "concrete_floor": lambda o, s: concrete(o, s, "concrete_floor", (0.40, 0.43, 0.46), wet=True),
    "painted_steel":  lambda o, s: painted_steel(o, s, "painted_steel", (0.36, 0.40, 0.42)),
    "rusted_steel":   lambda o, s: rusted_steel(o, s, "rusted_steel"),
    "snow":           lambda o, s: snow(o, s, "snow"),
    "metal_grating":  lambda o, s: metal_grating(o, s, "metal_grating"),
    "nest":           lambda o, s: nest(o, s, "nest"),
}


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--size", type=int, default=1024,
                    help="한 변 픽셀. 지시서 기준은 1K 통일 (기본 1024)")
    ap.add_argument("--out", type=Path, default=DEFAULT_OUT)
    ap.add_argument("--only", nargs="*", choices=sorted(SURFACES), default=None)
    args = ap.parse_args()

    names = args.only or sorted(SURFACES)
    print(f"{args.size}px, {len(names)}종 -> {args.out}")
    for n in names:
        SURFACES[n](args.out, args.size)
    print("완료")


if __name__ == "__main__":
    main()
