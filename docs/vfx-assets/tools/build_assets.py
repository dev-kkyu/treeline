"""Build Treeline V2 particle sprites from the approved art-direction sheet."""

from pathlib import Path
import math

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "v2" / "previews" / "treeline-vfx-direction-v2.png"
MASTERS = ROOT / "v2" / "masters" / "alpha"
PUBLISHED_MASTERS = ROOT / "masters" / "alpha"
CHROMA_MASTERS = ROOT / "masters" / "chroma"
RUNTIME = ROOT / "runtime" / "textures"
PREVIEWS = ROOT / "previews"

FROST = (221, 228, 226, 255)
MUTED = (140, 151, 153, 255)


def font_for(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "malgunbd.ttf" if bold else "malgun.ttf"
    path = Path("C:/Windows/Fonts") / name
    fallback = Path("C:/Windows/Fonts/arial.ttf")
    return ImageFont.truetype(str(path if path.exists() else fallback), size)


def panel(sheet: Image.Image, index: int, inset: int = 4) -> Image.Image:
    """Return one cell from the exact 3x2 approved concept-sheet layout."""
    cell_w = sheet.width // 3
    cell_h = sheet.height // 2
    col = index % 3
    row = index // 3
    return sheet.crop(
        (
            col * cell_w + inset,
            row * cell_h + inset,
            (col + 1) * cell_w - inset,
            (row + 1) * cell_h - inset,
        )
    ).convert("RGB")


def black_to_alpha(source: Image.Image, threshold: int = 7, gain: float = 1.55) -> Image.Image:
    """Reconstruct straight-alpha RGBA from an effect rendered over near-black."""
    source = source.convert("RGB")
    result = Image.new("RGBA", source.size)
    src = source.load()
    dst = result.load()
    for y in range(source.height):
        for x in range(source.width):
            red, green, blue = src[x, y]
            red = max(0, red - threshold)
            green = max(0, green - threshold)
            blue = max(0, blue - threshold)
            peak = max(red, green, blue)
            if peak <= 1:
                dst[x, y] = (0, 0, 0, 0)
                continue
            alpha = min(255, round(peak * gain))
            # Limit un-premultiplication so very dark sensor/color noise does not
            # turn into saturated cyan/magenta pixels in the Unity alpha blend.
            scale = min(3.2, 255.0 / peak)
            dst[x, y] = (
                min(255, round(red * scale)),
                min(255, round(green * scale)),
                min(255, round(blue * scale)),
                alpha,
            )
    return result


def crop_alpha(image: Image.Image, padding: int = 18) -> Image.Image:
    box = image.getchannel("A").getbbox()
    if box is None:
        raise ValueError("Generated sprite has no visible pixels")
    return image.crop(
        (
            max(0, box[0] - padding),
            max(0, box[1] - padding),
            min(image.width, box[2] + padding),
            min(image.height, box[3] + padding),
        )
    )


def contain(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    canvas = Image.new("RGBA", size)
    fitted = ImageOps.contain(image, (size[0] - 12, size[1] - 12), Image.Resampling.LANCZOS)
    canvas.alpha_composite(fitted, ((size[0] - fitted.width) // 2, (size[1] - fitted.height) // 2))
    return canvas


def save_sprite(name: str, image: Image.Image, size: tuple[int, int] = (256, 256)) -> None:
    image = contain(crop_alpha(image), size)
    image.save(MASTERS / name, optimize=True)
    published_name = name.replace(".png", "-full.png") if name in {
        "ice-burst.png", "fuel-flame.png", "muzzle-flash.png"
    } else name
    image.save(PUBLISHED_MASTERS / published_name, optimize=True)
    image.save(RUNTIME / name, optimize=True)
    if name in {"ice-burst.png", "fuel-flame.png", "muzzle-flash.png"}:
        chroma = Image.new("RGB", image.size, (0, 255, 0))
        chroma.paste(image.convert("RGB"), mask=image.getchannel("A"))
        chroma.save(CHROMA_MASTERS / name.replace(".png", "-chroma.png"), optimize=True)


def make_smoke(explosion: Image.Image) -> Image.Image:
    smoke = black_to_alpha(explosion, threshold=4, gain=1.15)
    gray = ImageOps.grayscale(smoke.convert("RGB"))
    color = ImageOps.colorize(gray, black=(32, 37, 39), white=(151, 158, 157)).convert("RGBA")
    alpha = smoke.getchannel("A").filter(ImageFilter.GaussianBlur(2.2))
    alpha = ImageEnhance.Brightness(alpha).enhance(0.52)
    color.putalpha(alpha)
    return color


def make_ember(sparks: Image.Image) -> Image.Image:
    rgba = black_to_alpha(sparks, threshold=8, gain=1.7)
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            red, green, blue, alpha = pixels[x, y]
            warmth = max(0, red - blue)
            new_alpha = min(alpha, warmth * 4)
            pixels[x, y] = (255, min(225, max(110, green)), 55, new_alpha)
    return rgba


def make_debris(explosion: Image.Image) -> Image.Image:
    """Extract one irregular dark rock fragment from the pressure-burst panel."""
    width, height = explosion.size
    fragment = explosion.crop((round(width * 0.10), round(height * 0.18), round(width * 0.36), round(height * 0.48)))
    rgba = black_to_alpha(fragment, threshold=5, gain=2.1)
    rgba = ImageEnhance.Contrast(rgba).enhance(1.18)
    return rgba


def make_preview() -> None:
    canvas = Image.new("RGBA", (1920, 1080), (7, 11, 15, 255))
    draw = ImageDraw.Draw(canvas)
    draw.text((90, 65), "TREELINE / PARTICLE VFX KIT V2", font=font_for(42, bold=True), fill=FROST)
    draw.text((90, 120), "desktop WebGL · URP Particle System · approved grounded horror direction", font=font_for(21), fill=MUTED)
    entries = (
        ("snow-streak.png", "눈보라", "불규칙한 바람 결 · 얼음 가루"),
        ("soft-disc.png", "서리 먼지", "잘게 깨진 냉기 입자"),
        ("muzzle-flash.png", "총구 섬광", "비대칭 화약 섬광 · 매연"),
        ("fuel-flame.png", "연료 화염", "바닥을 기는 디젤 화염"),
        ("spark-streak.png", "전기 스파크", "분절된 가지형 아크"),
        ("ice-burst.png", "압력 폭발", "젖은 먼지 · 암색 파편"),
    )
    for index, (filename, title, usage) in enumerate(entries):
        col = index % 3
        row = index // 3
        x = 90 + col * 610
        y = 185 + row * 410
        draw.rounded_rectangle((x, y, x + 545, y + 350), radius=12, fill=(15, 20, 24, 255), outline=(51, 61, 65, 255), width=2)
        sprite = Image.open(RUNTIME / filename).convert("RGBA")
        sprite = ImageOps.contain(sprite, (330, 255), Image.Resampling.LANCZOS)
        canvas.alpha_composite(sprite, (x + (545 - sprite.width) // 2, y + 12 + (255 - sprite.height) // 2))
        draw.text((x + 28, y + 292), title, font=font_for(25, bold=True), fill=FROST)
        draw.text((x + 28, y + 327), usage, font=font_for(18), fill=MUTED)
    canvas.convert("RGB").save(PREVIEWS / "vfx-kit-preview-1920x1080.jpg", quality=93, optimize=True)


def main() -> None:
    if not SOURCE.exists():
        raise FileNotFoundError(f"Approved V2 direction sheet is missing: {SOURCE}")
    MASTERS.mkdir(parents=True, exist_ok=True)
    PUBLISHED_MASTERS.mkdir(parents=True, exist_ok=True)
    CHROMA_MASTERS.mkdir(parents=True, exist_ok=True)
    RUNTIME.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)

    sheet = Image.open(SOURCE).convert("RGB")
    if sheet.width % 3 or sheet.height % 2:
        raise ValueError(f"Expected a 3x2 sheet, got {sheet.size}")

    snow = panel(sheet, 0)
    frost = panel(sheet, 1)
    muzzle = panel(sheet, 2)
    flame = panel(sheet, 3)
    sparks = panel(sheet, 4)
    explosion = panel(sheet, 5)

    save_sprite("snow-streak.png", black_to_alpha(snow, threshold=5, gain=1.65))
    save_sprite("soft-disc.png", black_to_alpha(frost, threshold=4, gain=1.38))
    save_sprite("muzzle-flash.png", black_to_alpha(muzzle, threshold=6, gain=1.72))
    save_sprite("fuel-flame.png", black_to_alpha(flame, threshold=5, gain=1.58))
    save_sprite("spark-streak.png", black_to_alpha(sparks, threshold=5, gain=1.8))
    save_sprite("ice-burst.png", black_to_alpha(explosion, threshold=4, gain=1.42))
    save_sprite("smoke-puff.png", make_smoke(explosion))
    save_sprite("ember.png", make_ember(sparks), (128, 128))
    save_sprite("debris-shard.png", make_debris(explosion), (128, 128))
    make_preview()


if __name__ == "__main__":
    main()
