"""Build runtime-sized Treeline UI sprites and previews from alpha PNG masters."""

from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[1]
MASTERS = ROOT / "masters" / "alpha"
RUNTIME = ROOT / "runtime"
MENU = RUNTIME / "menu"
VITALS = RUNTIME / "hud" / "vitals"
FLASHLIGHT = RUNTIME / "hud" / "flashlight"
STAMINA = RUNTIME / "hud" / "stamina"
COMBAT = RUNTIME / "hud" / "combat"
INTERACTION = RUNTIME / "hud" / "interaction"
TUTORIAL = RUNTIME / "tutorial"
EFFECTS = RUNTIME / "effects"
PREVIEWS = ROOT / "previews"

FROST = (185, 193, 188, 255)
SLATE = (78, 88, 92, 255)
CHARCOAL = (37, 43, 46, 255)
DEEP = (12, 18, 23, 255)
RED = (138, 67, 55, 255)


def crop_alpha(image: Image.Image, padding: int = 6) -> Image.Image:
    image = image.convert("RGBA")
    box = image.getchannel("A").getbbox()
    if box is None:
        return image
    left = max(0, box[0] - padding)
    top = max(0, box[1] - padding)
    right = min(image.width, box[2] + padding)
    bottom = min(image.height, box[3] + padding)
    return image.crop((left, top, right, bottom))


def resize_width(image: Image.Image, width: int) -> Image.Image:
    height = max(1, round(image.height * width / image.width))
    return image.resize((width, height), Image.Resampling.LANCZOS)


def save_runtime(master_name: str, output_dir: Path, runtime_name: str, width: int) -> Image.Image:
    image = crop_alpha(Image.open(MASTERS / master_name))
    image = resize_width(image, width)
    if master_name == "tutorial-card-frame-full.png":
        pixels = image.load()
        for y in range(image.height):
            for x in range(image.width):
                red, green, blue, alpha = pixels[x, y]
                if alpha and green > 220 and red < 40 and blue < 40:
                    pixels[x, y] = (red, max(red, blue), blue, alpha)
    image.save(output_dir / runtime_name, optimize=True)
    return image


def tint_empty(image: Image.Image) -> Image.Image:
    alpha = image.getchannel("A")
    gray = ImageOps.grayscale(image.convert("RGB"))
    gray = ImageEnhance.Contrast(gray).enhance(0.72)
    gray = ImageEnhance.Brightness(gray).enhance(0.33)
    tinted = ImageOps.colorize(gray, black=(12, 18, 23), white=(74, 84, 88)).convert("RGBA")
    tinted.putalpha(alpha.point(lambda value: round(value * 0.72)))
    return tinted


def make_crosshair() -> None:
    image = Image.new("RGBA", (64, 64))
    draw = ImageDraw.Draw(image)
    shadow = (12, 18, 23, 210)
    for offset, color, width in ((1, shadow, 4), (0, FROST, 2)):
        draw.line((32 + offset, 9, 32 + offset, 23), fill=color, width=width)
        draw.line((32 + offset, 41, 32 + offset, 55), fill=color, width=width)
        draw.line((9, 32 + offset, 23, 32 + offset), fill=color, width=width)
        draw.line((41, 32 + offset, 55, 32 + offset), fill=color, width=width)
    draw.rectangle((30, 30, 33, 33), fill=(185, 193, 188, 210))
    image.save(COMBAT / "crosshair.png", optimize=True)


def make_interact_reticle() -> None:
    image = Image.new("RGBA", (96, 96))
    draw = ImageDraw.Draw(image)
    for color, width, inset in ((DEEP, 7, 17), (FROST, 3, 18)):
        length = 20
        draw.line((inset, inset + length, inset, inset, inset + length, inset), fill=color, width=width)
        draw.line((96 - inset - length, inset, 96 - inset, inset, 96 - inset, inset + length), fill=color, width=width)
        draw.line((inset, 96 - inset - length, inset, 96 - inset, inset + length, 96 - inset), fill=color, width=width)
        draw.line((96 - inset - length, 96 - inset, 96 - inset, 96 - inset, 96 - inset, 96 - inset - length), fill=color, width=width)
    image.save(INTERACTION / "interact-reticle.png", optimize=True)


def make_keycap() -> None:
    image = Image.new("RGBA", (56, 56))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((4, 4, 51, 51), radius=7, fill=(24, 31, 35, 238), outline=FROST, width=2)
    draw.line((12, 46, 44, 46), fill=RED, width=2)
    font = font_for(27, bold=True)
    draw.text((28, 27), "E", font=font, fill=FROST, anchor="mm")
    image.save(INTERACTION / "keycap-e.png", optimize=True)


def draw_keycap(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], label: str, size: int = 23) -> None:
    draw.rounded_rectangle(box, radius=7, fill=(24, 31, 35, 238), outline=FROST, width=2)
    draw.line((box[0] + 7, box[3] - 6, box[2] - 7, box[3] - 6), fill=RED, width=2)
    draw.text(((box[0] + box[2]) // 2, (box[1] + box[3]) // 2), label,
              font=font_for(size, bold=True), fill=FROST, anchor="mm")


def make_tutorial_controls() -> None:
    for filename, label, width, font_size in (
        ("keycap-shift.png", "SHIFT", 128, 20),
        ("keycap-space.png", "SPACE", 144, 20),
        ("keycap-e.png", "E", 56, 27),
        ("keycap-f.png", "F", 56, 27),
        ("keycap-c.png", "C", 56, 27),
        ("keycap-r.png", "R", 56, 27),
    ):
        image = Image.new("RGBA", (width, 56))
        draw_keycap(ImageDraw.Draw(image), (4, 4, width - 5, 51), label, font_size)
        image.save(TUTORIAL / filename, optimize=True)

    numbers = Image.new("RGBA", (120, 56))
    numbers_draw = ImageDraw.Draw(numbers)
    draw_keycap(numbers_draw, (4, 4, 55, 51), "1")
    draw_keycap(numbers_draw, (64, 4, 115, 51), "2")
    numbers.save(TUTORIAL / "keycap-1-2.png", optimize=True)

    wasd = Image.new("RGBA", (176, 116))
    wasd_draw = ImageDraw.Draw(wasd)
    for box, label in (
        ((62, 4, 113, 55), "W"),
        ((4, 60, 55, 111), "A"),
        ((62, 60, 113, 111), "S"),
        ((120, 60, 171, 111), "D"),
    ):
        draw_keycap(wasd_draw, box, label)
    wasd.save(TUTORIAL / "keycap-wasd.png", optimize=True)

    for filename, left_fill, arrows in (
        ("mouse-left-click.png", True, False),
        ("mouse-look.png", False, True),
    ):
        image = Image.new("RGBA", (72, 96))
        draw = ImageDraw.Draw(image)
        draw.rounded_rectangle((9, 3, 62, 92), radius=24, fill=(24, 31, 35, 238), outline=FROST, width=2)
        draw.line((36, 4, 36, 40), fill=SLATE, width=2)
        draw.line((10, 41, 61, 41), fill=SLATE, width=2)
        draw.rounded_rectangle((32, 12, 40, 29), radius=3, fill=SLATE, outline=FROST, width=1)
        if left_fill:
            draw.pieslice((10, 4, 61, 55), 180, 270, fill=(114, 82, 77, 235))
            draw.line((36, 4, 36, 40), fill=FROST, width=2)
        if arrows:
            draw.polygon(((2, 48), (8, 43), (8, 47), (17, 47), (17, 51), (8, 51), (8, 55)), fill=FROST)
            draw.polygon(((70, 48), (64, 43), (64, 47), (55, 47), (55, 51), (64, 51), (64, 55)), fill=FROST)
        image.save(TUTORIAL / filename, optimize=True)


def make_battery_fill() -> None:
    for name, ratio, color in (("battery-fill-full.png", 1.0, FROST), ("battery-fill-low.png", 0.22, RED)):
        image = Image.new("RGBA", (384, 32))
        draw = ImageDraw.Draw(image)
        draw.rounded_rectangle((1, 5, 382, 27), radius=4, fill=(17, 23, 27, 215), outline=(78, 88, 92, 180), width=1)
        end = 7 + round(368 * ratio)
        draw.rounded_rectangle((7, 9, end, 23), radius=2, fill=color)
        for x in range(76, 376, 74):
            draw.line((x, 9, x, 23), fill=(37, 43, 46, 210), width=2)
        image.save(FLASHLIGHT / name, optimize=True)


def make_stamina_fill() -> None:
    for name, color in (
        ("stamina-fill-ready.png", (185, 193, 188, 255)),
        ("stamina-fill-recovery-locked.png", (114, 82, 77, 255)),
    ):
        image = Image.new("RGBA", (384, 24))
        draw = ImageDraw.Draw(image)
        draw.rounded_rectangle((1, 3, 382, 21), radius=4, fill=(17, 23, 27, 215), outline=(78, 88, 92, 180), width=1)
        draw.rounded_rectangle((6, 7, 377, 17), radius=2, fill=color)
        for x in range(80, 376, 74):
            draw.line((x, 7, x, 17), fill=(37, 43, 46, 210), width=2)
        image.save(STAMINA / name, optimize=True)


def make_ammo_round() -> None:
    image = Image.new("RGBA", (28, 52))
    draw = ImageDraw.Draw(image)
    draw.polygon(((8, 3), (20, 3), (24, 11), (24, 39), (20, 48), (8, 48), (4, 39), (4, 11)), fill=CHARCOAL, outline=FROST)
    draw.rectangle((8, 12, 20, 38), fill=SLATE)
    draw.line((8, 39, 20, 39), fill=RED, width=2)
    image.save(COMBAT / "ammo-round.png", optimize=True)


def make_divider() -> None:
    image = Image.new("RGBA", (512, 16))
    draw = ImageDraw.Draw(image)
    draw.line((0, 8, 512, 8), fill=(78, 88, 92, 180), width=1)
    draw.line((0, 7, 128, 7), fill=FROST, width=2)
    draw.line((128, 7, 146, 7), fill=RED, width=2)
    image.save(MENU / "menu-divider.png", optimize=True)


def font_for(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    font_name = "malgunbd.ttf" if bold else "malgun.ttf"
    candidates = [Path("C:/Windows/Fonts") / font_name, Path("C:/Windows/Fonts/arial.ttf")]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def dark_scene() -> Image.Image:
    image = Image.new("RGB", (1920, 1080), (7, 11, 15))
    draw = ImageDraw.Draw(image)
    draw.polygon(((0, 0), (650, 0), (820, 1080), (0, 1080)), fill=(12, 18, 23))
    draw.polygon(((1920, 0), (1270, 0), (1100, 1080), (1920, 1080)), fill=(10, 16, 21))
    draw.rectangle((650, 0, 1270, 1080), fill=(15, 21, 25))
    for y in range(120, 1040, 150):
        draw.line((650, y, 1270, y + 55), fill=(30, 37, 40), width=7)
    for x in (710, 1210):
        draw.line((x, 0, x - 70 if x < 900 else x + 70, 1080), fill=(42, 50, 53), width=13)
    draw.ellipse((650, 120, 1270, 1120), fill=(31, 37, 38))
    image = image.filter(ImageFilter.GaussianBlur(16))
    veil = Image.new("RGBA", image.size, (5, 9, 12, 92))
    return Image.alpha_composite(image.convert("RGBA"), veil)


def fit(image: Image.Image, width: int | None = None, height: int | None = None) -> Image.Image:
    ratio = min(width / image.width if width else 999, height / image.height if height else 999)
    return image.resize((round(image.width * ratio), round(image.height * ratio)), Image.Resampling.LANCZOS)


def make_hud_preview(health: Image.Image, health_empty: Image.Image, battery: Image.Image) -> None:
    canvas = dark_scene()
    draw = ImageDraw.Draw(canvas)
    frost_overlay = Image.open(EFFECTS / "damage-overlay.png").convert("RGBA").resize(canvas.size, Image.Resampling.LANCZOS)
    frost_overlay.putalpha(frost_overlay.getchannel("A").point(lambda v: round(v * 0.36)))
    canvas.alpha_composite(frost_overlay)

    pip = fit(health, height=112)
    pip_empty = fit(health_empty, height=112)
    for index, sprite in enumerate((pip, pip, pip_empty)):
        canvas.alpha_composite(sprite, (62 + index * 48, 920))

    stamina_frame = Image.open(STAMINA / "stamina-frame.png").convert("RGBA")
    stamina_frame = fit(stamina_frame, width=380)
    canvas.alpha_composite(stamina_frame, (60, 805))
    stamina_fill = Image.open(STAMINA / "stamina-fill-ready.png").convert("RGBA")
    stamina_fill = stamina_fill.resize((292, 18), Image.Resampling.LANCZOS)
    canvas.alpha_composite(stamina_fill, (104, 832))

    frame = fit(battery, width=390)
    canvas.alpha_composite(frame, (1455, 918))
    fill = Image.open(FLASHLIGHT / "battery-fill-low.png").convert("RGBA").resize((278, 23), Image.Resampling.LANCZOS)
    canvas.alpha_composite(fill, (1502, 973))

    ammo = Image.open(COMBAT / "ammo-round.png").convert("RGBA")
    for index in range(4):
        canvas.alpha_composite(ammo, (1662 + index * 25, 848))
    draw.text((1798, 870), "04", font=font_for(36, bold=True), fill=FROST, anchor="rm")

    reticle = Image.open(INTERACTION / "interact-reticle.png").convert("RGBA")
    canvas.alpha_composite(reticle, (912, 492))
    key = Image.open(INTERACTION / "keycap-e.png").convert("RGBA")
    canvas.alpha_composite(key, (790, 640))
    draw.rounded_rectangle((852, 645, 1068, 695), radius=6, fill=(12, 18, 23, 205), outline=(78, 88, 92, 150), width=1)
    draw.text((874, 670), "레버 당기기", font=font_for(23), fill=FROST, anchor="lm")
    draw.text((78, 785), "호흡", font=font_for(18), fill=(140, 151, 153, 210), anchor="lm")
    draw.text((78, 900), "생체 신호", font=font_for(18), fill=(140, 151, 153, 210), anchor="lm")
    draw.text((1455, 895), "손전등", font=font_for(18), fill=(140, 151, 153, 210), anchor="lm")
    canvas.convert("RGB").save(PREVIEWS / "hud-mockup-1920x1080.jpg", quality=92, optimize=True)


def make_title_preview(panel: Image.Image, button: Image.Image) -> None:
    canvas = dark_scene()
    draw = ImageDraw.Draw(canvas)
    panel_view = fit(panel, width=1200)
    panel_view.putalpha(panel_view.getchannel("A").point(lambda v: round(v * 0.78)))
    canvas.alpha_composite(panel_view, (360, 120))
    draw.text((960, 245), "TREELINE", font=font_for(92, bold=True), fill=FROST, anchor="mm")
    draw.text((960, 318), "신호는 끊겼고, 아래로 내려가야 한다", font=font_for(23), fill=(140, 151, 153, 230), anchor="mm")
    draw.line((710, 370, 1210, 370), fill=SLATE, width=1)
    draw.line((710, 369, 840, 369), fill=RED, width=3)
    compact = fit(button, width=460)
    for y, text_value in ((420, "시작"), (530, "설정"), (640, "종료")):
        canvas.alpha_composite(compact, (730, y))
        draw.text((960, y + compact.height // 2), text_value, font=font_for(30, bold=True), fill=FROST, anchor="mm")
    canvas.convert("RGB").save(PREVIEWS / "title-menu-mockup-1920x1080.jpg", quality=92, optimize=True)


def make_tutorial_preview(card: Image.Image) -> None:
    canvas = dark_scene()
    veil = Image.new("RGBA", canvas.size, (5, 9, 12, 165))
    canvas = Image.alpha_composite(canvas, veil)
    draw = ImageDraw.Draw(canvas)
    draw.text((960, 92), "생존 조작", font=font_for(48, bold=True), fill=FROST, anchor="mm")
    draw.text((960, 142), "행동을 완료하면 해당 안내가 사라집니다", font=font_for(21),
              fill=(140, 151, 153, 235), anchor="mm")

    cards = (
        ("keycap-wasd.png", "이동", "눈보라 속에서 방향을 잡습니다"),
        ("mouse-look.png", "시야", "마우스로 주변을 살핍니다"),
        ("keycap-e.png", "상호작용", "장치 · 문 · 아이템을 사용합니다"),
        ("keycap-f.png", "손전등", "배터리를 아껴 어둠을 밝힙니다"),
        ("keycap-shift.png", "달리기", "이동 중 길게 눌러 달립니다"),
        ("keycap-c.png", "웅크리기", "낮은 통로를 지나고 몸을 숨깁니다"),
        ("keycap-space.png", "점프", "낮은 장애물을 넘습니다"),
        ("mouse-left-click.png", "전투", "발사 · R 재장전 · 1/2 무기 교체"),
    )
    card_view = fit(card, width=650)
    card_view.putalpha(card_view.getchannel("A").point(lambda value: round(value * 0.9)))
    positions = [(260 + column * 720, 190 + row * 205) for row in range(4) for column in range(2)]
    for (icon_name, title, detail), (x, y) in zip(cards, positions):
        canvas.alpha_composite(card_view, (x, y))
        icon = Image.open(TUTORIAL / icon_name).convert("RGBA")
        icon = ImageOps.contain(icon, (112, 92), Image.Resampling.LANCZOS)
        canvas.alpha_composite(icon, (x + 62 + (112 - icon.width) // 2, y + 48 + (92 - icon.height) // 2))
        draw.text((x + 205, y + 70), title, font=font_for(25, bold=True), fill=FROST, anchor="lm")
        draw.text((x + 205, y + 112), detail, font=font_for(18), fill=(154, 163, 163, 235), anchor="lm")
    canvas.convert("RGB").save(PREVIEWS / "tutorial-controls-mockup-1920x1080.jpg", quality=92, optimize=True)


def make_contact_sheet() -> None:
    assets = [
        (MENU / "panel-frame-9slice.png", "menu/panel-frame-9slice.png"),
        (MENU / "button-frame-9slice.png", "menu/button-frame-9slice.png"),
        (VITALS / "health-pip-active.png", "hud/vitals/health-pip-active.png"),
        (VITALS / "health-pip-empty.png", "hud/vitals/health-pip-empty.png"),
        (FLASHLIGHT / "battery-frame.png", "hud/flashlight/battery-frame.png"),
        (FLASHLIGHT / "battery-fill-full.png", "hud/flashlight/battery-fill-full.png"),
        (FLASHLIGHT / "battery-fill-low.png", "hud/flashlight/battery-fill-low.png"),
        (STAMINA / "stamina-frame.png", "hud/stamina/stamina-frame.png"),
        (STAMINA / "stamina-fill-ready.png", "hud/stamina/stamina-fill-ready.png"),
        (STAMINA / "stamina-fill-recovery-locked.png", "hud/stamina/stamina-fill-recovery-locked.png"),
        (COMBAT / "crosshair.png", "hud/combat/crosshair.png"),
        (INTERACTION / "interact-reticle.png", "hud/interaction/interact-reticle.png"),
        (INTERACTION / "keycap-e.png", "hud/interaction/keycap-e.png"),
        (TUTORIAL / "tutorial-card-frame.png", "tutorial/tutorial-card-frame.png"),
        (TUTORIAL / "keycap-wasd.png", "tutorial/keycap-wasd.png"),
        (TUTORIAL / "mouse-look.png", "tutorial/mouse-look.png"),
        (TUTORIAL / "mouse-left-click.png", "tutorial/mouse-left-click.png"),
        (TUTORIAL / "keycap-shift.png", "tutorial/keycap-shift.png"),
        (TUTORIAL / "keycap-space.png", "tutorial/keycap-space.png"),
        (TUTORIAL / "keycap-e.png", "tutorial/keycap-e.png"),
        (TUTORIAL / "keycap-f.png", "tutorial/keycap-f.png"),
        (TUTORIAL / "keycap-c.png", "tutorial/keycap-c.png"),
        (TUTORIAL / "keycap-r.png", "tutorial/keycap-r.png"),
        (TUTORIAL / "keycap-1-2.png", "tutorial/keycap-1-2.png"),
        (COMBAT / "ammo-round.png", "hud/combat/ammo-round.png"),
        (MENU / "menu-divider.png", "menu/menu-divider.png"),
        (EFFECTS / "damage-overlay.png", "effects/damage-overlay.png"),
    ]
    rows = (len(assets) + 2) // 3
    sheet = Image.new("RGBA", (1600, 150 + rows * 260 + 30), (12, 18, 23, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((60, 45), "TREELINE / UI ASSET KIT", font=font_for(36, bold=True), fill=FROST)
    draw.text((60, 95), "relay hardware + frost-pressure scars", font=font_for(20), fill=(140, 151, 153, 255))
    x, y = 60, 150
    for source_path, label in assets:
        source = Image.open(source_path).convert("RGBA")
        thumb = ImageOps.contain(source, (440, 190), Image.Resampling.LANCZOS)
        tile = Image.new("RGBA", (480, 240), (18, 25, 30, 255))
        tile.alpha_composite(thumb, ((480 - thumb.width) // 2, 20 + (170 - thumb.height) // 2))
        ImageDraw.Draw(tile).text((18, 215), label, font=font_for(16), fill=FROST, anchor="ls")
        sheet.alpha_composite(tile, (x, y))
        x += 510
        if x + 480 > sheet.width:
            x = 60
            y += 260
    sheet.convert("RGB").save(PREVIEWS / "asset-contact-sheet.jpg", quality=92, optimize=True)


def main() -> None:
    for directory in (MENU, VITALS, FLASHLIGHT, STAMINA, COMBAT, INTERACTION, TUTORIAL, EFFECTS):
        directory.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    panel = save_runtime("panel-frame.png", MENU, "panel-frame-9slice.png", 1024)
    button = save_runtime("button-frame.png", MENU, "button-frame-9slice.png", 768)
    health = save_runtime("health-pip-full.png", VITALS, "health-pip-active.png", 96)
    health_empty = tint_empty(health)
    health_empty.save(VITALS / "health-pip-empty.png", optimize=True)
    battery = save_runtime("battery-frame-full.png", FLASHLIGHT, "battery-frame.png", 512)
    save_runtime("stamina-frame-full.png", STAMINA, "stamina-frame.png", 512)
    tutorial_card = save_runtime("tutorial-card-frame-full.png", TUTORIAL, "tutorial-card-frame.png", 768)
    save_runtime("damage-overlay.png", EFFECTS, "damage-overlay.png", 1672)
    make_crosshair()
    make_interact_reticle()
    make_keycap()
    make_tutorial_controls()
    make_battery_fill()
    make_stamina_fill()
    make_ammo_round()
    make_divider()
    make_hud_preview(health, health_empty, battery)
    make_title_preview(panel, button)
    make_tutorial_preview(tutorial_card)
    make_contact_sheet()


if __name__ == "__main__":
    main()
