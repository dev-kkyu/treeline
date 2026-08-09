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


def cellular(size: int, cells: int, rng: np.random.Generator, order: int = 1,
             layers: int = 3, lacunarity: float = 2.0, persistence: float = 0.55) -> np.ndarray:
    """셀룰러 노이즈를 여러 겹 쌓는다. 유기물에는 이쪽을 쓴다.

    IMPORTANT: `worley` 를 한 겹만 쓰면 **셀이 고르게 배열된 점무늬**로 보인다.
    격자 한 칸에 특징점이 하나씩이라 크기가 거의 같아지기 때문이다. 렌더해 보니
    둥지가 「붉은 물방울무늬」, 다져진 눈이 「눈에 핀 파란 곰팡이」가 됐다.

    fbm 이 value 노이즈에 하는 것을 그대로 셀룰러에 한다 — 잘게 겹칠수록 큰 셀
    안이 작은 셀로 갈려서 크기가 흐트러진다. 이 함수를 통과하면 같은 무늬가
    **덩어리진 살**로 보인다.
    """
    total = np.zeros((size, size))
    amp, norm, c = 1.0, 0.0, float(cells)
    for _ in range(layers):
        total += worley(size, max(2, int(round(c))), rng, order=order) * amp
        norm += amp
        amp *= persistence
        c *= lacunarity
    out = total / max(norm, 1e-9)
    lo, hi = out.min(), out.max()
    return (out - lo) / max(hi - lo, 1e-9)


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


def stretch(a: np.ndarray, axis: int, span: int) -> np.ndarray:
    """한 축으로만 뭉갠다. 방향이 있는 무늬를 만드는 유일한 도구다.

    fbm 과 worley 는 등방(等方)이라 어느 쪽으로도 같은 무늬가 나온다. 그런데
    자연에서 무늬가 생기는 이유는 대개 **한 방향으로 작용한 힘**이다 — 물은
    아래로 흐르고, 바람은 한쪽으로 불고, 섬유는 결을 따라 자란다. 그 방향을
    노이즈에 넣으려면 축 하나로만 평균을 내면 된다.

    `np.roll` 로 감아서 더하므로 타일링이 그대로 유지된다.

    axis=0 이면 세로로 흐른 자국(행끼리 섞임), axis=1 이면 가로로 쓸린 결이다.
    """
    acc = np.zeros_like(a)
    for k in range(-span, span + 1):
        acc += np.roll(a, k, axis=axis)
    return acc / (2 * span + 1)


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

    membrane = cellular(size, 12, rng, order=1)    # 막 — 크기가 흐트러진 셀
    sinew = worley(size, 44, rng, order=1)         # 잔 섬유
    blotch = fbm(size, 5, 4, 0.6, rng)             # 넓은 얼룩

    height = membrane * 0.55 + sinew * 0.25 + blotch * 0.20

    # IMPORTANT: 처음에 (0.19, 0.055, 0.045) 로 어둡게 깔았다가 **차가운 손전등 아래서
    # 붉은기가 사라지고 회색 돌처럼 보였다.** 이 게임의 유일한 광원이 흰 손전등이므로
    # 채도를 눈에 보일 만큼 남겨야 한다. 어둡게 만드는 것은 조명이 이미 하고 있다.
    # IMPORTANT: 2 차 개정. **조명이 켜지면 결이 사라졌다.** 4장은 마지막에 전력이
    # 들어와 「내가 무엇 안에서 싸우고 있었는지」를 보여주는 장인데, 밝은 환경광이
    # 노멀이 만드는 그늘을 사방에서 채워버려 살덩이가 붉은 판때기가 됐다.
    # 재본 값 — 결/밝기가 손전등 0.256 에서 대낮 0.093 으로 떨어진다.
    #
    # 그늘이 지워져도 남는 것은 **알베도 자체의 명암**뿐이다. 그래서 색을 두 점이
    # 아니라 세 점으로 늘렸다. 깊은 골은 빛이 애초에 닿지 않는 자리라 어둡게
    # 두어도 위 IMPORTANT 가 경고한 「통째로 어두워져 회색 돌이 되는 것」과 다르다 —
    # 밝은 쪽(pale)은 오히려 올렸으므로 평균 채도는 유지된다.
    dark = np.array([0.075, 0.018, 0.016])      # 골 — 빛이 안 닿는 자리
    flesh = np.array([0.34, 0.085, 0.070])
    pale = np.array([0.56, 0.28, 0.22])
    t = np.clip(membrane * 0.7 + blotch * 0.3, 0, 1)
    lo = np.clip(t / 0.42, 0, 1)[..., None]
    hi = np.clip((t - 0.42) / 0.58, 0, 1)[..., None]
    shade = (0.80 + blotch * 0.30)[..., None]
    albedo = np.clip((dark + (flesh - dark) * lo + (pale - flesh) * hi) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 220.0)
    metallic = np.zeros((size, size))

    # IMPORTANT: 1차에서 젖은 정도를 0.55 까지 줬다가 **살이 아니라 얼음으로 보였다.**
    # 이 게임의 빛은 차가운 손전등뿐이라, smoothness 가 높으면 정반사가 그 색을
    # 그대로 되돌려 붉은 알베도를 통째로 덮는다. 골에만 조금 고이게 둔다.
    wet = np.clip((1.0 - membrane - 0.25) * 1.8, 0, 1)
    smooth = 0.10 + wet * 0.22

    _write(out, name, albedo, normal, metallic, smooth)


def rock_hewn(out: Path, size: int, name: str) -> None:
    """굴착 암반. 3장 연료 갱도의 벽과 천장.

    갱도를 콘크리트로 덮으면 1장 복도와 구분이 안 간다. 기획이 3장을 「지하
    통로」로 둔 것은 **여기가 지어진 곳이 아니라 파낸 곳**이기 때문이다. 그래서
    깨낸 덩어리가 각지게 갈리고(worley F1), 그 사이 골로 물이 흘러내려 세로로
    검은 자국이 남는다.
    """
    rng = np.random.default_rng(SEED + 7)

    # 암반은 크기가 제각각인 덩어리로 깨진다. 셀룰러를 겹쳐야 그 «제각각»이 나온다.
    chunk = cellular(size, 5, rng, order=0, layers=3)
    facet = cellular(size, 13, rng, order=1, layers=2)   # 덩어리 사이의 각진 골
    grit = fbm(size, 96, 3, 0.5, rng)

    height = chunk * 0.55 + facet * 0.30 + grit * 0.15

    # 물은 아래로만 흐른다. 등방 노이즈를 세로로 뭉개야 「흘러내린 것」이 된다.
    # 길게 뭉개고 좁게 잘라야 «줄»이 된다 — 넓게 남기면 그냥 얼룩이다.
    seep = stretch(fbm(size, 12, 3, 0.55, rng), axis=0, span=max(6, size // 8))
    seep = (seep - seep.min()) / max(seep.max() - seep.min(), 1e-9)
    seep = np.clip((seep - 0.52) * 3.2, 0, 1)

    dry = np.array([0.34, 0.33, 0.31])
    soak = np.array([0.10, 0.11, 0.12])
    shade = (0.72 + chunk * 0.44 + (grit - 0.5) * 0.16)[..., None]
    albedo = np.clip((dry + (soak - dry) * seep[..., None]) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 120.0)
    metallic = np.zeros((size, size))
    smooth = 0.06 + seep * 0.40                    # 젖은 줄만 빛을 되돌린다

    _write(out, name, albedo, normal, metallic, smooth)


def fuel_pool(out: Path, size: int, name: str) -> None:
    """바닥에 고인 연료. 3장. 여기에 대고 쏘면 통로가 불바다가 된다.

    IMPORTANT: **이 프로젝트에서 유일하게 「눈에 띄어야 하는」 표면이다.** 나머지는
    전부 어둠에 형태를 숨기는 쪽으로 조율했지만, 이것은 손전등이 스치는 순간
    번들거려 「저기다」를 알려야 한다. smoothness 를 0.9 까지 올린 것은 분위기가
    아니라 **가독성** 때문이다. 못 알아보면 기획의 선택지가 통째로 사라진다.

    노멀은 거의 평평하게 둔다. 고인 액체는 실제로 평평하고, 요철을 주면 정반사가
    잘게 부서져 오히려 안 보인다.
    """
    rng = np.random.default_rng(SEED + 8)

    ripple = fbm(size, 12, 4, 0.5, rng)
    film = fbm(size, 5, 3, 0.6, rng)               # 표면에 뜬 유막

    normal = normal_from_height(ripple, strength=size / 900.0)

    # 경유 자체는 거의 검다. 얇은 유막이 내는 간섭색만 얹는다. 어두운 곳에서는
    # 정반사가 화면을 정하므로 이 색은 거의 안 보이지만, 불이 붙기 전 한 번
    # 비출 때 「물이 아니라 기름」으로 읽히게 한다.
    ph = film * (2.0 * np.pi)
    tint = np.stack([0.5 + 0.5 * np.cos(ph),
                     0.5 + 0.5 * np.cos(ph + 2.094),
                     0.5 + 0.5 * np.cos(ph + 4.189)], axis=-1)
    base = np.array([0.020, 0.020, 0.018])
    albedo = np.clip(base + tint * 0.06 * film[..., None], 0, 1)

    metallic = np.zeros((size, size))
    smooth = np.clip(0.90 - (ripple - 0.5) * 0.10, 0, 1)

    _write(out, name, albedo, normal, metallic, smooth)


def scorched(out: Path, size: int, name: str) -> None:
    """그을린 콘크리트. 3장에서 연료를 태우고 지나간 구간에 쓴다.

    태운 자리가 그대로면 「태웠다」가 화면에 안 남는다. 3장의 밀도는 **같은
    통로를 두 번 지나는 것**이라 두 번째 통과가 달라 보여야 하고, 그 차이를
    새 공간이 아니라 표면이 만든다.
    """
    rng = np.random.default_rng(SEED + 9)

    stain = fbm(size, 4, 5, 0.55, rng)
    grain = fbm(size, 64, 3, 0.5, rng)
    soot = np.clip((stain - 0.34) * 2.6, 0, 1)     # 검댕이 덮인 정도
    spall = np.clip((grain - 0.74) * 6.0, 0, 1)    # 열로 튄 자리 — 속살이 드러난다

    height = stain * 0.55 + grain * 0.25 + spall * 0.40

    carbon = np.array([0.035, 0.032, 0.030])
    conc = np.array([0.48, 0.49, 0.47])
    t = np.clip(soot - spall, 0, 1)[..., None]
    shade = (0.86 + (grain - 0.5) * 0.20)[..., None]
    albedo = np.clip((conc + (carbon - conc) * t) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 150.0)
    metallic = np.zeros((size, size))
    smooth = 0.04 + spall * 0.07                   # 검댕은 빛을 되돌리지 않는다

    _write(out, name, albedo, normal, metallic, smooth)


def nest_floor(out: Path, size: int, name: str) -> None:
    """둥지 바닥. 4장. 기획서의 「바닥이 물컹하다」가 이것이다.

    벽(`nest`)과 같은 재료지만 **밟는 것**이라 다르게 만든다. 큰 주머니가 눌려
    낮은 자리에 액체가 고이고, 그 자리만 smoothness 를 올린다. 젖은 콘크리트
    바닥에서 팀이 「완전 호러」라고 한 그 수법을 살에 옮긴 것이다.
    """
    rng = np.random.default_rng(SEED + 10)

    sac = cellular(size, 6, rng, order=0)          # 큰 주머니
    membrane = cellular(size, 18, rng, order=1, layers=2)
    blotch = fbm(size, 4, 4, 0.6, rng)

    height = sac * 0.50 + membrane * 0.30 + blotch * 0.20
    pool = np.clip((0.55 - sac) * 2.2, 0, 1)       # 낮은 자리에 고인다

    dark = np.array([0.055, 0.014, 0.013])
    flesh = np.array([0.26, 0.070, 0.058])
    pale = np.array([0.44, 0.20, 0.16])
    t = np.clip(sac * 0.6 + membrane * 0.4, 0, 1)
    lo = np.clip(t / 0.40, 0, 1)[..., None]
    hi = np.clip((t - 0.40) / 0.60, 0, 1)[..., None]
    shade = (0.78 + blotch * 0.34)[..., None]
    albedo = np.clip((dark + (flesh - dark) * lo + (pale - flesh) * hi) * shade, 0, 1)
    albedo *= (1.0 - pool * 0.40)[..., None]       # 고인 자리는 더 어둡다

    normal = normal_from_height(height, strength=size / 200.0)
    metallic = np.zeros((size, size))
    smooth = 0.08 + pool * 0.55

    _write(out, name, albedo, normal, metallic, smooth)


def nest_fiber(out: Path, size: int, name: str) -> None:
    """섬유 다발. 4장에서 기둥 · 배관 · 케이블을 감는 데 쓴다.

    벽과 바닥만 있으면 둥지가 **덮인 방**으로 보인다. 기획은 「살과 섬유질로
    덮여 있다」이고, 섬유질은 결이 있어야 섬유질로 보인다. 결의 방향은 감는
    물건의 축과 맞춘다 — 원통에 감을 때 UV 를 돌리면 된다.
    """
    rng = np.random.default_rng(SEED + 11)

    strand = stretch(fbm(size, 36, 3, 0.5, rng), axis=0, span=max(6, size // 9))
    strand = (strand - strand.min()) / max(strand.max() - strand.min(), 1e-9)
    knot = cellular(size, 7, rng, order=0, layers=2)     # 다발이 뭉친 매듭
    grit = fbm(size, 80, 2, 0.5, rng)

    height = strand * 0.52 + (1.0 - knot) * 0.34 + grit * 0.14

    # 매듭은 결을 끊어 놓는다. 결만 있으면 나뭇결로 보이므로 여기서 갈라준다.
    dark = np.array([0.070, 0.022, 0.020])
    sinewy = np.array([0.52, 0.30, 0.24])
    t = np.clip(strand * 0.62 + (1.0 - knot) * 0.38, 0, 1)[..., None]
    shade = (0.80 + knot * 0.28 + (grit - 0.5) * 0.20)[..., None]
    albedo = np.clip((dark + (sinewy - dark) * t) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 160.0)
    metallic = np.zeros((size, size))
    smooth = 0.10 + np.clip(1.0 - strand - 0.2, 0, 1) * 0.26

    _write(out, name, albedo, normal, metallic, smooth)


def snow_packed(out: Path, size: int, name: str) -> None:
    """다져진 눈. 5장에서 사람이 지나간 통로와 바람이 쓸어놓은 자리.

    IMPORTANT: 깨끗한 눈(`snow`)은 알베도 대비 0.020 으로 이 프로젝트에서 가장
    낮다. 물리적으로는 맞지만 **대낮 눈보라에서 표면이 통째로 사라진다** — 밝은
    환경광이 노멀의 그늘을 사방에서 채우기 때문이고, 재보니 결/밝기가 0.018
    까지 떨어졌다(손전등에서는 0.257).

    기획이 원하는 「하얘서 안 보인다」는 **시야 거리**로 만드는 것이 맞다.
    발밑까지 안 보이면 게임이 안 되므로, 플레이어가 걷는 자리에는 이것을 깔아
    지면이 읽히게 한다. `snow` 를 대체하는 것이 아니라 짝이다.
    """
    rng = np.random.default_rng(SEED + 12)

    # IMPORTANT: 처음에 눌린 자리를 worley 로 만들었다가 **눈에 파란 곰팡이가 핀 것처럼**
    # 보였다. 셀룰러는 세포 · 자갈 · 갈라진 진흙처럼 «경계로 나뉜 것»의 무늬이고,
    # 눈은 그런 것이 아니다. 눈밭의 무늬를 만드는 것은 오직 **바람**이라, 방향이
    # 있는 결(sastrugi) 하나로 충분하다. 노이즈 종류는 형태가 정한다.
    def _norm(a):
        return (a - a.min()) / max(a.max() - a.min(), 1e-9)

    sastrugi = _norm(stretch(fbm(size, 14, 4, 0.55, rng), axis=1, span=max(5, size // 12)))
    ridge = _norm(stretch(fbm(size, 34, 3, 0.5, rng), axis=1, span=max(3, size // 26)))
    zone = fbm(size, 5, 3, 0.6, rng)               # 다져진 구역 — 지나다닌 자리
    crust = fbm(size, 40, 3, 0.5, rng)

    press = np.clip((zone - 0.44) * 3.0, 0, 1)
    height = sastrugi * 0.42 + ridge * 0.24 + crust * 0.14 + (1.0 - press) * 0.20

    # 눌린 눈은 공기가 빠져 어둡고 푸르다. 얼음 쪽으로 간다.
    fresh = np.array([0.88, 0.91, 0.95])
    packed = np.array([0.52, 0.58, 0.68])
    t = np.clip(press * 0.75 + (1.0 - sastrugi) * 0.25, 0, 1)[..., None]
    shade = (0.86 + sastrugi * 0.20 + ridge * 0.10 + (crust - 0.5) * 0.08)[..., None]
    albedo = np.clip((fresh + (packed - fresh) * t) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 210.0)
    metallic = np.zeros((size, size))
    smooth = 0.14 + press * 0.34                   # 다져진 자리가 얼어 번들거린다

    _write(out, name, albedo, normal, metallic, smooth)


def galvanized(out: Path, size: int, name: str) -> None:
    """아연도금 강재. 5장 철탑 구조재 · 6장 삭도 구조물.

    도장 철판(`painted_steel`)과 다른 물건이다. 도금은 굳을 때 **다각형 결정
    (spangle)** 이 생겨 면마다 빛을 되돌리는 각도가 다르다. worley 의 F1 이 바로
    그 다각형이다 — 그레이팅을 노이즈가 아니라 도형으로 그린 것과 같은 판단이고,
    **무엇을 노이즈로 만들고 무엇을 구조로 만들지는 형태가 정한다.**

    IMPORTANT: metallic 을 0.45 로 묶어 두었다. 도금은 실제로 더 금속이지만,
    `painted_steel` 에서 이미 겪은 대로 손전등은 스폿이라 정반사가 한 점에 몰려
    형태가 날아간다. 5장은 대낮이라 견디지만 6장은 밤이므로 낮은 쪽에 맞춘다.
    """
    rng = np.random.default_rng(SEED + 13)

    spangle = worley(size, 22, rng, order=0)       # 결정 낱장
    edge = worley(size, 22, rng, order=1)          # 결정 경계
    grit = fbm(size, 70, 3, 0.5, rng)
    weather = fbm(size, 6, 4, 0.6, rng)            # 비바람에 흐려진 자리

    height = edge * 0.40 + grit * 0.22 + spangle * 0.12

    zinc = np.array([0.52, 0.54, 0.56])
    dull = np.array([0.28, 0.29, 0.28])
    t = np.clip((weather - 0.42) * 2.0, 0, 1)[..., None]
    shade = (0.78 + spangle * 0.42)[..., None]
    albedo = np.clip((zinc + (dull - zinc) * t) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 320.0)
    metallic = np.clip(0.45 * (1.0 - t[..., 0] * 0.6), 0, 1)
    smooth = np.clip(0.26 + spangle * 0.20 - t[..., 0] * 0.16, 0, 1)

    _write(out, name, albedo, normal, metallic, smooth)


def rime_ice(out: Path, size: int, name: str) -> None:
    """착빙. 5장 철탑에 얼어붙은 것 · 6장 삭도 케이블과 곤돌라.

    IMPORTANT: **반투명은 만들지 않는다.** URP Lit 를 투명으로 돌리면 정렬 순서와
    그림자가 무너지는데, 이 게임은 어두워서 얻는 것이 거의 없다. 밝은 알베도 ·
    높은 smoothness · 센 노멀로 「단단하고 젖어 보이는 것」까지만 간다.

    바람 부는 곳의 착빙은 바람 쪽으로 자란다. 그래서 결을 가로로 뭉갠다.
    """
    rng = np.random.default_rng(SEED + 14)

    # 착빙은 바람이 실어온 물방울이 부딪친 자리에 얼어붙어 **바람 쪽으로 자란다.**
    # 그래서 결이 방향을 갖는다. 셀룰러는 덩어리를 흐트러뜨리는 데만 조금 쓰고
    # 주역으로 두지 않는다 — 앞세우면 얼음이 아니라 거품으로 보인다.
    grow = stretch(fbm(size, 18, 4, 0.55, rng), axis=1, span=max(4, size // 14))
    grow = (grow - grow.min()) / max(grow.max() - grow.min(), 1e-9)
    lump = cellular(size, 9, rng, order=0, layers=3)
    frost = fbm(size, 60, 3, 0.5, rng)

    height = grow * 0.58 + (1.0 - lump) * 0.24 + frost * 0.18

    clear = np.array([0.52, 0.62, 0.74])           # 언 자리 — 푸르고 어둡다
    milky = np.array([0.88, 0.92, 0.96])           # 공기가 섞인 자리 — 희다
    t = np.clip(frost * 0.35 + grow * 0.65, 0, 1)[..., None]
    shade = (0.82 + grow * 0.24 + (1.0 - lump) * 0.14)[..., None]
    albedo = np.clip((clear + (milky - clear) * t) * shade, 0, 1)

    normal = normal_from_height(height, strength=size / 110.0)
    metallic = np.zeros((size, size))
    # 매끈한 얼음과 서리가 섞인다. 전면을 올리면 유리가 된다.
    smooth = np.clip(0.62 - t[..., 0] * 0.34, 0, 1)

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
    # 3장 연료 갱도
    "rock_hewn":      lambda o, s: rock_hewn(o, s, "rock_hewn"),
    "fuel_pool":      lambda o, s: fuel_pool(o, s, "fuel_pool"),
    "scorched":       lambda o, s: scorched(o, s, "scorched"),
    # 4장 발전동 둥지
    "nest_floor":     lambda o, s: nest_floor(o, s, "nest_floor"),
    "nest_fiber":     lambda o, s: nest_fiber(o, s, "nest_fiber"),
    # 5장 외부 철탑
    "snow_packed":    lambda o, s: snow_packed(o, s, "snow_packed"),
    "galvanized":     lambda o, s: galvanized(o, s, "galvanized"),
    "rime_ice":       lambda o, s: rime_ice(o, s, "rime_ice"),
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
