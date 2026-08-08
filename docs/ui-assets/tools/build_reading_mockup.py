"""Build the Ch1 field-record reading UI placement mockup."""

from pathlib import Path
import random

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[1]
PREVIEWS = ROOT / "previews"
RNG = random.Random(1041)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "malgunbd.ttf" if bold else "malgun.ttf"
    path = Path("C:/Windows/Fonts") / name
    fallback = Path("C:/Windows/Fonts/arial.ttf")
    return ImageFont.truetype(str(path if path.exists() else fallback), size)


def corridor() -> Image.Image:
    image = Image.new("RGBA", (1920, 1080), (7, 13, 17, 255))
    draw = ImageDraw.Draw(image, "RGBA")
    draw.polygon([(0, 0), (1920, 0), (1430, 420), (490, 420)], fill=(10, 17, 21, 255))
    draw.polygon([(0, 1080), (1920, 1080), (1420, 620), (500, 620)], fill=(18, 25, 28, 255))
    draw.polygon([(0, 0), (490, 420), (500, 620), (0, 1080)], fill=(12, 20, 24, 255))
    draw.polygon([(1920, 0), (1430, 420), (1420, 620), (1920, 1080)], fill=(9, 16, 20, 255))
    draw.rectangle((500, 420, 1425, 625), fill=(15, 23, 27, 255))
    for x in range(550, 1420, 145):
        draw.line((x, 420, x - 35, 625), fill=(70, 78, 78, 55), width=2)
    glow = Image.new("RGBA", image.size)
    gdraw = ImageDraw.Draw(glow, "RGBA")
    for radius in range(420, 30, -20):
        alpha = round(2 + 16 * (1 - radius / 420))
        gdraw.ellipse((960 - radius, 530 - radius // 3, 960 + radius, 530 + radius // 3),
                     fill=(175, 190, 184, alpha))
    glow = glow.filter(ImageFilter.GaussianBlur(25))
    image = Image.alpha_composite(image, glow)
    for _ in range(70):
        x, y = RNG.randrange(1920), RNG.randrange(1080)
        ImageDraw.Draw(image).ellipse((x, y, x + 1, y + 1), fill=(145, 156, 154, RNG.randrange(10, 40)))
    return Image.alpha_composite(image, Image.new("RGBA", image.size, (1, 5, 8, 185)))


def main() -> None:
    image = corridor()
    draw = ImageDraw.Draw(image, "RGBA")
    draw.rectangle((413, 165, 1543, 955), fill=(0, 0, 0, 155))
    draw.rectangle((395, 145, 1525, 935), fill=(31, 37, 38, 255), outline=(89, 98, 97, 255), width=5)
    draw.rectangle((456, 205, 1448, 889), fill=(126, 120, 103, 255))
    draw.polygon([(466, 199), (1458, 211), (1446, 895), (454, 883)],
                 fill=(188, 181, 158, 255), outline=(82, 78, 69, 255))
    draw.rectangle((466, 199, 1458, 271), fill=(53, 58, 57, 255))
    draw.text((498, 222), "TREELINE RELAY STATION  /  FIELD RECORD", font=font(15),
              fill=(201, 205, 198, 230))
    draw.line((558, 291, 558, 822), fill=(126, 62, 52, 175), width=3)
    for line in range(12):
        y = 373 + line * 39
        draw.line((504, y, 1410, y), fill=(79, 88, 87, 42), width=1)
    draw.text((504, 304), "무전 일지", font=font(38, True), fill=(38, 39, 35, 255))
    draw.rectangle((1290, 302, 1396, 342), outline=(116, 51, 43, 180), width=2)
    draw.text((1343, 322), "기록 보존", font=font(14, True), fill=(116, 51, 43, 210), anchor="mm")

    lines = [
        "01:40   적설 관측. 도로 통제 요청 — 응답 없음",
        "02:14   중계기 3번 응답 없음. 옥상 확인 예정",
        "02:52   옥상 다녀옴. 안테나는 멀쩡함. 회선이 끊긴 듯",
        "03:20   발전기 수동 전환. 계기가 자꾸 튄다",
        "04:07   밖에서 소리. 눈 무너지는 소리치고는 규칙적임",
        "04:41   김 주임이 안 돌아옴. 숙소동 확인하러 감",
    ]
    y = 390
    for value in lines:
        draw.text((586, y), value, font=font(25), fill=(47, 46, 40, 255))
        y += 58
    draw.text((1386, 824), "MOUSE WHEEL  /  PAGE UP · DOWN", font=font(13),
              fill=(62, 61, 54, 180), anchor="ra")
    draw.rectangle((858, 123, 1068, 165), fill=(96, 103, 102, 255), outline=(39, 44, 45, 255), width=3)
    draw.text((960, 915), "[ E / ESC ]  기록 닫기", font=font(17),
              fill=(194, 201, 198, 210), anchor="mm")
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(PREVIEWS / "reading-panel-mockup-1920x1080.jpg", quality=93, optimize=True)


if __name__ == "__main__":
    main()
