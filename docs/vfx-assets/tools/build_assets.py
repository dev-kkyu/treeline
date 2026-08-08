"""Build Treeline particle textures and a desktop WebGL preview."""

from pathlib import Path
import math
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
MASTERS = ROOT / "masters" / "alpha"
RUNTIME = ROOT / "runtime" / "textures"
PREVIEWS = ROOT / "previews"
RNG = random.Random(317)

FROST = (221, 228, 226, 255)
SLATE = (113, 128, 135, 255)
AMBER = (214, 154, 85, 255)
RED = (138, 74, 56, 255)


def font_for(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "malgunbd.ttf" if bold else "malgun.ttf"
    path = Path("C:/Windows/Fonts") / name
    fallback = Path("C:/Windows/Fonts/arial.ttf")
    return ImageFont.truetype(str(path if path.exists() else fallback), size)


def crop_alpha(image: Image.Image, padding: int = 12) -> Image.Image:
    image = image.convert("RGBA")
    box = image.getchannel("A").getbbox()
    if box is None:
        return image
    left = max(0, box[0] - padding)
    top = max(0, box[1] - padding)
    right = min(image.width, box[2] + padding)
    bottom = min(image.height, box[3] + padding)
    return image.crop((left, top, right, bottom))


def contain_square(image: Image.Image, size: int) -> Image.Image:
    canvas = Image.new("RGBA", (size, size))
    fitted = ImageOps.contain(image, (size - 12, size - 12), Image.Resampling.LANCZOS)
    canvas.alpha_composite(fitted, ((size - fitted.width) // 2, (size - fitted.height) // 2))
    return canvas


def remove_key_strays(image: Image.Image) -> Image.Image:
    image = image.copy().convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha and green > 210 and green > red * 1.8 and green > blue * 1.8:
                pixels[x, y] = (red, max(red, blue), blue, alpha)
    return image


def save_master(master_name: str, runtime_name: str, size: int = 256) -> None:
    source = crop_alpha(Image.open(MASTERS / master_name))
    result = contain_square(remove_key_strays(source), size)
    result = remove_key_strays(result)
    result.save(RUNTIME / runtime_name, optimize=True)


def make_soft_disc() -> None:
    size = 256
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    center = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            distance = math.hypot(x - center, y - center) / center
            alpha = round(255 * max(0.0, 1.0 - distance) ** 2.2)
            pixels[x, y] = (255, 255, 255, alpha)
    image.save(RUNTIME / "soft-disc.png", optimize=True)


def make_snow_streak() -> None:
    scale = 4
    image = Image.new("RGBA", (256 * scale, 64 * scale))
    draw = ImageDraw.Draw(image)
    for index, width in enumerate((19, 11, 7)):
        y = 128 + (index - 1) * 18
        color = (221, 228, 226, 225 - index * 48)
        draw.line((104, y, 904, y - 20), fill=color, width=width)
    draw.ellipse((70, 94, 170, 174), fill=(235, 240, 238, 245))
    image = image.filter(ImageFilter.GaussianBlur(4.5))
    image = image.resize((256, 64), Image.Resampling.LANCZOS)
    image.save(RUNTIME / "snow-streak.png", optimize=True)


def make_spark_streak() -> None:
    image = Image.new("RGBA", (256, 64))
    draw = ImageDraw.Draw(image)
    for width, color in ((13, (138, 74, 56, 80)), (7, (214, 154, 85, 190)), (2, (255, 244, 215, 255))):
        draw.line((18, 39, 238, 24), fill=color, width=width)
    image = image.filter(ImageFilter.GaussianBlur(0.7))
    image.save(RUNTIME / "spark-streak.png", optimize=True)


def make_ember() -> None:
    size = 64
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    center = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            distance = math.hypot(x - center, y - center) / center
            alpha = round(255 * max(0.0, 1.0 - distance) ** 2.8)
            heat = max(0.0, 1.0 - distance)
            pixels[x, y] = (255, round(90 + 150 * heat), 42, alpha)
    image.save(RUNTIME / "ember.png", optimize=True)


def make_smoke_puff() -> None:
    size = 256
    mask = Image.new("L", (size, size))
    draw = ImageDraw.Draw(mask)
    for _ in range(32):
        radius = RNG.randint(24, 72)
        x = RNG.randint(52, 204)
        y = RNG.randint(50, 206)
        alpha = RNG.randint(28, 80)
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=alpha)
    mask = mask.filter(ImageFilter.GaussianBlur(18))
    radial = Image.new("L", (size, size))
    radial_pixels = radial.load()
    for y in range(size):
        for x in range(size):
            distance = math.hypot(x - 127.5, y - 127.5) / 127.5
            radial_pixels[x, y] = round(255 * max(0.0, 1.0 - distance) ** 0.8)
    mask = ImageChops.multiply(mask, radial)
    image = Image.new("RGBA", (size, size), (145, 151, 150, 255))
    image.putalpha(mask)
    image.save(RUNTIME / "smoke-puff.png", optimize=True)


def make_debris_shard() -> None:
    image = Image.new("RGBA", (128, 128))
    draw = ImageDraw.Draw(image)
    draw.polygon(((29, 76), (60, 17), (95, 46), (78, 108)), fill=(96, 105, 106, 245), outline=(188, 198, 195, 255))
    draw.polygon(((60, 17), (68, 70), (95, 46)), fill=(157, 168, 167, 225))
    draw.line((68, 70, 78, 108), fill=(55, 63, 65, 240), width=3)
    image.save(RUNTIME / "debris-shard.png", optimize=True)


def make_preview() -> None:
    canvas = Image.new("RGBA", (1920, 1080), (7, 11, 15, 255))
    draw = ImageDraw.Draw(canvas)
    draw.text((90, 65), "TREELINE / PARTICLE VFX KIT", font=font_for(42, bold=True), fill=FROST)
    draw.text((90, 120), "desktop WebGL · URP Particle System", font=font_for(21), fill=(140, 151, 153, 255))
    entries = (
        ("snow-streak.png", "눈보라", "5장 · 6장 외부"),
        ("soft-disc.png", "서리 먼지", "실내 냉기 · 입자"),
        ("muzzle-flash.png", "총구 섬광", "3장 · 5장 전투"),
        ("fuel-flame.png", "연료 화염", "3장 인화 통로"),
        ("spark-streak.png", "전기 스파크", "4장 전력 복구"),
        ("ice-burst.png", "폭발 파편", "4장 · 6장 폭발"),
    )
    for index, (filename, title, usage) in enumerate(entries):
        column = index % 3
        row = index // 3
        x = 90 + column * 610
        y = 185 + row * 410
        draw.rounded_rectangle((x, y, x + 545, y + 350), radius=12, fill=(18, 25, 30, 255), outline=(60, 71, 75, 255), width=2)
        sprite = Image.open(RUNTIME / filename).convert("RGBA")
        sprite = ImageOps.contain(sprite, (300, 245), Image.Resampling.LANCZOS)
        canvas.alpha_composite(sprite, (x + 122 + (300 - sprite.width) // 2, y + 18 + (245 - sprite.height) // 2))
        draw.text((x + 28, y + 292), title, font=font_for(25, bold=True), fill=FROST)
        draw.text((x + 28, y + 327), usage, font=font_for(18), fill=(140, 151, 153, 255))
    canvas.convert("RGB").save(PREVIEWS / "vfx-kit-preview-1920x1080.jpg", quality=92, optimize=True)


def main() -> None:
    RUNTIME.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    save_master("ice-burst-full.png", "ice-burst.png")
    save_master("fuel-flame-full.png", "fuel-flame.png")
    save_master("muzzle-flash-full.png", "muzzle-flash.png")
    make_soft_disc()
    make_snow_streak()
    make_spark_streak()
    make_ember()
    make_smoke_puff()
    make_debris_shard()
    make_preview()


if __name__ == "__main__":
    main()
