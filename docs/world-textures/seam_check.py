"""텍스처가 실제로 타일링되는지 수치로 잰다.

왜 필요한가
----------
「seamless」라고 적혀 있어도 이음매가 있는 CC0 에셋이 있다. 실제로 ambientCG 의
Concrete031 은 원본 1K 에서 세로 이음매가 내부 불연속의 3.7 배였다. 벽에 붙여
반복하면 일정 간격으로 줄이 보인다. **눈으로는 늦게 발견되고, 그때는 이미
씬에 깔려 있다.**

원리
----
좌우 끝단을 이어 붙였을 때의 픽셀 차이를, **이미지 내부의 이웃 픽셀 차이**와
비교한다. 타일링이 되는 텍스처라면 끝단도 내부와 다를 게 없으므로 비율이 1
근처가 된다. 절대값이 아니라 비율로 재는 이유는 텍스처마다 대비가 달라서다 —
거친 텍스처는 내부 차이도 크다.

판정
----
    비율 < 1.6   통과
    비율 >= 1.6  **의심.** 자동으로 확정하지 않는다 — 눈으로 본다

IMPORTANT: **이 도구는 후보를 걸러낼 뿐 판정하지 않는다.** 규칙적인 무늬는
정상인데도 걸린다. 금속 그레이팅 노멀맵이 7.68 로 걸렸는데, 내부 살 경계와
비교해 보니 1.00 이었다 — 타일 경계가 하필 살 경계와 겹쳤을 뿐이다.

자동 판별을 두 가지 시도했고 **둘 다 실패했다.** 기록해 둔다.

- **크기로 가르기** (내부 최대 불연속과 비교) — 실패. 광학 스캔은 내부에 균열이
  있어 진짜 이음매인 Concrete031 이 통과해 버렸다
- **주기성으로 가르기** (같은 크기의 각이 일정 간격으로 되풀이되는가) — 실패.
  역시 Concrete031 을 무늬로 통과시켰다. **오탐보다 이쪽이 위험하다.** 멀쩡한
  것을 버리는 것보다 깨진 것을 들여보내는 쪽이 나쁘다

그래서 확정은 사람이 한다. `--tile` 을 붙이면 2x2 로 이어 붙인 그림을 내주므로,
네 귀퉁이가 만나는 자리만 보면 2 초면 끝난다.

사용법
-----
    python docs/world-textures/seam_check.py <폴더 또는 파일...> [--tile]
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

THRESHOLD = 1.6

# 색을 담은 맵만 검사한다. metallic/smoothness 마스크는 이 잣대가 맞지 않는다.
PATTERNS = ("*_albedo.png", "*_normal.png", "*Color.jpg", "*NormalGL.jpg")


def ratios(path: Path) -> tuple[float, float, float]:
    """(가로 비율, 세로 비율, 끝단 절댓값) 을 돌려준다. 값은 8비트 계단 단위다."""
    a = np.asarray(Image.open(path).convert("RGB"), dtype=np.float64)

    seam_h = np.abs(a[:, -1] - a[:, 0]).mean()
    seam_v = np.abs(a[-1, :] - a[0, :]).mean()

    # 열/행 사이 불연속을 하나씩 모은다. 평균만 쓰면 규칙 패턴에서 오탐이 난다.
    dh = np.abs(a[:, 1:] - a[:, :-1]).mean(axis=(0, 2))
    dv = np.abs(a[1:, :] - a[:-1, :]).mean(axis=(1, 2))

    return (seam_h / max(dh.mean(), 1e-9), seam_v / max(dv.mean(), 1e-9),
            max(seam_h, seam_v))


# 8비트 한 계단. 끝단 차이가 이보다 작으면 **화면에 나타날 수가 없다.**
# IMPORTANT: 이건 판정 규칙이 아니라 산수다. 위에 적힌 실패한 자동 판별 둘과
# 성격이 다르다 — 그 둘은 「이 정도면 무늬겠지」라는 추측이었고 진짜 이음매를
# 통과시켰지만, 이것은 표현할 수 없는 값에는 이음매가 담길 수 없다는 뜻이다.
QUANT_STEP = 1.0


def write_tile(path: Path, out_dir: Path, cell: int = 240) -> Path:
    """2x2 로 이어 붙인 그림을 낸다. 네 귀퉁이가 만나는 자리를 보면 바로 안다."""
    out_dir.mkdir(parents=True, exist_ok=True)
    im = Image.open(path).convert("RGB").resize((cell, cell), Image.Resampling.LANCZOS)
    sheet = Image.new("RGB", (cell * 2, cell * 2))
    for x in (0, cell):
        for y in (0, cell):
            sheet.paste(im, (x, y))
    dst = out_dir / (path.stem + "_tile2x2.png")
    sheet.save(dst)
    return dst


def collect(target: Path) -> list[Path]:
    if target.is_file():
        return [target]
    found: list[Path] = []
    for pat in PATTERNS:
        found += sorted(target.rglob(pat))
    return found


def main() -> int:
    # IMPORTANT: Windows 콘솔은 기본이 cp949 라 «—» 같은 글자에서 죽는다. 걸린
    # 텍스처를 알려주는 그 줄에서 터지므로, 하필 **결과가 나쁠 때만** 죽어서
    # 통과한 줄만 보고 「이상 없다」로 읽게 된다. 출력 인코딩을 먼저 고정한다.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 2

    tile_dir = None
    if "--tile" in args:
        args = [a for a in args if a != "--tile"]
        tile_dir = Path("build/seam-tiles")

    flagged: list[Path] = []
    checked = 0
    for raw in args:
        for f in collect(Path(raw)):
            h, v, absmax = ratios(f)
            checked += 1
            bad = h >= THRESHOLD or v >= THRESHOLD
            if bad and absmax < QUANT_STEP:
                # 비율은 높지만 절댓값이 한 계단도 안 된다. 나눗셈의 분모가 0 에
                # 가까워서 나온 수치일 뿐이다.
                print(f"잡음  H {h:5.2f}  V {v:5.2f}  (끝단 {absmax:.2f}계단)   {f}")
                continue
            if bad:
                flagged.append(f)
            mark = "의심" if bad else "ok  "
            print(f"{mark}  H {h:5.2f}  V {v:5.2f}  (끝단 {absmax:5.2f}계단)   {f}")

    print(f"\n{checked} 장 중 {len(flagged)} 장이 걸렸다.")
    if flagged:
        print("걸린 것은 **눈으로 확인한다.** 규칙적인 무늬(그레이팅 등)는 여기서")
        print("정상인데도 걸린다 — 타일 경계가 무늬의 각과 겹치기 때문이다.")
        if tile_dir is not None:
            for f in flagged:
                print("  " + str(write_tile(f, tile_dir)))
        else:
            print("  --tile 을 붙이면 2x2 로 이어 붙인 그림을 내준다.")
    return 1 if flagged else 0


if __name__ == "__main__":
    raise SystemExit(main())
