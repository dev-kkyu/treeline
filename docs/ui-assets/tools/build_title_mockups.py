"""Build snowy main-menu and sound-options placement mockups."""

from pathlib import Path
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
MENU = ROOT / "runtime" / "menu"
PREVIEWS = ROOT / "previews"
RNG = random.Random(6021)

FROST = (221, 228, 226, 255)
MUTED = (140, 151, 153, 235)
RED = (126, 62, 52, 255)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "malgunbd.ttf" if bold else "malgun.ttf"
    path = Path("C:/Windows/Fonts") / name
    fallback = Path("C:/Windows/Fonts/arial.ttf")
    return ImageFont.truetype(str(path if path.exists() else fallback), size)


def snow_scene() -> Image.Image:
    width, height = 1920, 1080
    image = Image.new("RGBA", (width, height))
    pixels = image.load()
    for y in range(height):
        t = y / (height - 1)
        if t < 0.58:
            local = t / 0.58
            top, bottom = (8, 16, 23), (48, 62, 71)
        else:
            local = (t - 0.58) / 0.42
            top, bottom = (49, 61, 68), (17, 27, 33)
        color = tuple(round(top[i] * (1 - local) + bottom[i] * local) for i in range(3))
        for x in range(width):
            pixels[x, y] = (*color, 255)

    draw = ImageDraw.Draw(image, "RGBA")
    draw.polygon([(560, 690), (1030, 270), (1430, 700)], fill=(20, 31, 39, 255))
    draw.polygon([(1030, 270), (1170, 485), (1115, 438), (1068, 540), (1002, 440),
                  (920, 520), (970, 390)], fill=(134, 148, 155, 120))
    draw.polygon([(1120, 720), (1515, 390), (1919, 710)], fill=(14, 24, 31, 255))
    draw.polygon([(1515, 390), (1610, 535), (1550, 510), (1505, 585), (1435, 510)],
                 fill=(104, 119, 127, 105))
    draw.polygon([(0, 705), (420, 650), (890, 726), (1260, 680), (1920, 735),
                  (1920, 1080), (0, 1080)], fill=(76, 91, 100, 255))

    for x, base, scale in ((990, 760, 1.0), (1280, 742, 0.78), (1690, 775, 1.12),
                           (1530, 735, 0.62), (820, 730, 0.58)):
        trunk = round(18 * scale)
        draw.rectangle((x - trunk // 2, base - round(220 * scale), x + trunk // 2, base),
                       fill=(8, 15, 19, 245))
        for row in range(6):
            y = base - round((70 + row * 34) * scale)
            half = round((95 - row * 10) * scale)
            draw.polygon([(x, y - round(78 * scale)), (x - half, y + round(35 * scale)),
                          (x + half, y + round(35 * scale))], fill=(7, 17, 22, 245))

    mist = Image.new("RGBA", image.size)
    mist_draw = ImageDraw.Draw(mist, "RGBA")
    for _ in range(85):
        x = RNG.randint(600, 2000)
        y = RNG.randint(250, 950)
        length = RNG.randint(80, 310)
        alpha = RNG.randint(10, 35)
        mist_draw.line((x, y, x - length, y + RNG.randint(8, 35)), fill=(220, 232, 235, alpha),
                       width=RNG.randint(1, 4))
    mist = mist.filter(ImageFilter.GaussianBlur(1.2))
    image = Image.alpha_composite(image, mist)
    image = Image.alpha_composite(image, Image.new("RGBA", image.size, (2, 7, 11, 55)))
    return image


def compose_panel(image: Image.Image) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    panel = Image.open(MENU / "panel-frame-9slice.png").convert("RGBA").resize((650, 880), Image.Resampling.LANCZOS)
    panel.putalpha(panel.getchannel("A").point(lambda value: round(value * 0.94)))
    image.alpha_composite(panel, (72, 100))
    shade = Image.new("RGBA", (554, 772), (5, 12, 16, 220))
    image.alpha_composite(shade, (120, 154))
    draw = ImageDraw.Draw(image)
    draw.text((150, 180), "설산 중계소  /  신호 두절", font=font(16), fill=MUTED)
    draw.text((150, 225), "TREELINE", font=font(72, True), fill=FROST)
    draw.text((152, 323), "신호는 끊겼고, 아래로 내려가야 한다", font=font(20), fill=MUTED)
    divider = Image.open(MENU / "menu-divider.png").convert("RGBA").resize((470, 12), Image.Resampling.LANCZOS)
    image.alpha_composite(divider, (152, 370))
    return image, draw


def draw_button(image: Image.Image, draw: ImageDraw.ImageDraw, y: int, label: str) -> None:
    button = Image.open(MENU / "button-frame-9slice.png").convert("RGBA").resize((470, 86), Image.Resampling.LANCZOS)
    image.alpha_composite(button, (152, y))
    draw.text((387, y + 43), label, font=font(25, True), fill=FROST, anchor="mm")


def main_menu() -> None:
    image, draw = compose_panel(snow_scene())
    draw_button(image, draw, 452, "게임 시작")
    draw_button(image, draw, 566, "사운드 설정")
    draw.text((156, 670), "첫 번째 중계소부터 시작합니다", font=font(15), fill=MUTED)
    draw.text((156, 852), "DESKTOP WEBGL  ·  1920×1080", font=font(13), fill=(140, 151, 153, 165))
    image.convert("RGB").save(PREVIEWS / "title-menu-mockup-1920x1080.jpg", quality=93, optimize=True)


def sound_menu() -> None:
    image, draw = compose_panel(snow_scene())
    draw.text((152, 424), "사운드", font=font(34, True), fill=FROST)
    draw.text((156, 505), "전체 음량", font=font(18), fill=MUTED)
    draw.text((617, 505), "80%", font=font(18, True), fill=FROST, anchor="ra")
    draw.rectangle((156, 559, 616, 571), fill=(8, 15, 19, 245), outline=(73, 86, 91, 220), width=1)
    draw.rectangle((156, 559, 524, 571), fill=RED)
    draw.rectangle((516, 550, 532, 580), fill=FROST)
    draw_button(image, draw, 628, "음소거")
    draw_button(image, draw, 742, "뒤로")
    draw.text((156, 852), "변경한 음량은 브라우저에 자동 저장됩니다", font=font(14), fill=MUTED)
    image.convert("RGB").save(PREVIEWS / "title-sound-options-mockup-1920x1080.jpg", quality=93, optimize=True)


def compose_ending_panel(image: Image.Image) -> tuple[Image.Image, ImageDraw.ImageDraw]:
    panel = Image.open(MENU / "panel-frame-9slice.png").convert("RGBA").resize((650, 880), Image.Resampling.LANCZOS)
    panel.putalpha(panel.getchannel("A").point(lambda value: round(value * 0.94)))
    image.alpha_composite(panel, (1198, 100))
    image.alpha_composite(Image.new("RGBA", (554, 772), (5, 12, 16, 225)), (1246, 154))
    draw = ImageDraw.Draw(image)
    draw.text((1278, 180), "삭도 운행 기록  /  수신 종료", font=font(16), fill=MUTED)
    draw.text((1278, 235), "신호 종료", font=font(56, True), fill=FROST)
    draw.text((1280, 323), "케이블은 계속 아래로 움직인다", font=font(20), fill=MUTED)
    divider = Image.open(MENU / "menu-divider.png").convert("RGBA").resize((470, 12), Image.Resampling.LANCZOS)
    image.alpha_composite(divider, (1280, 370))
    return image, draw


def draw_ending_button(image: Image.Image, draw: ImageDraw.ImageDraw, y: int, label: str) -> None:
    button = Image.open(MENU / "button-frame-9slice.png").convert("RGBA").resize((470, 86), Image.Resampling.LANCZOS)
    image.alpha_composite(button, (1280, y))
    draw.text((1515, y + 43), label, font=font(25, True), fill=FROST, anchor="mm")


def ending_menu() -> None:
    image, draw = compose_ending_panel(snow_scene())
    draw_ending_button(image, draw, 444, "다시 하기")
    draw_ending_button(image, draw, 558, "사운드 설정")
    draw_ending_button(image, draw, 672, "타이틀로")
    draw.text((1284, 778), "처음부터 다시 내려갑니다", font=font(14), fill=MUTED)
    draw.text((1284, 852), "TREELINE  ·  END OF TRANSMISSION", font=font(13), fill=(140, 151, 153, 165))
    image.convert("RGB").save(PREVIEWS / "ending-menu-mockup-1920x1080.jpg", quality=93, optimize=True)


def ending_sound_menu() -> None:
    image, draw = compose_ending_panel(snow_scene())
    draw.text((1280, 424), "사운드", font=font(34, True), fill=FROST)
    draw.text((1284, 505), "전체 음량", font=font(18), fill=MUTED)
    draw.text((1745, 505), "80%", font=font(18, True), fill=FROST, anchor="ra")
    draw.rectangle((1284, 559, 1744, 571), fill=(8, 15, 19, 245), outline=(73, 86, 91, 220), width=1)
    draw.rectangle((1284, 559, 1652, 571), fill=RED)
    draw.rectangle((1644, 550, 1660, 580), fill=FROST)
    draw_ending_button(image, draw, 628, "음소거")
    draw_ending_button(image, draw, 742, "뒤로")
    draw.text((1284, 852), "변경한 음량은 브라우저에 자동 저장됩니다", font=font(14), fill=MUTED)
    image.convert("RGB").save(PREVIEWS / "ending-sound-options-mockup-1920x1080.jpg", quality=93, optimize=True)


def main() -> None:
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    main_menu()
    sound_menu()
    ending_menu()
    ending_sound_menu()


if __name__ == "__main__":
    main()
