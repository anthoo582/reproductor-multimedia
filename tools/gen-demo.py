from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

out = Path(__file__).parent / "out" / "demo"
out.mkdir(parents=True, exist_ok=True)


def sky(w, h, top, bottom):
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        t = y / (h - 1)
        px_row = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        for x in range(w):
            px[x, y] = px_row
    return img


def scene():
    w, h = 1600, 1000
    img = sky(w, h, (32, 44, 92), (238, 132, 96))
    draw = ImageDraw.Draw(img, "RGBA")

    # sol y halo
    draw.ellipse((610, 230, 990, 610), fill=(255, 214, 140, 45))
    draw.ellipse((720, 340, 880, 500), fill=(255, 236, 190, 230))

    # nubes suaves
    for x, y, rx, ry in [(190, 210, 180, 34), (260, 180, 120, 26), (1170, 260, 170, 30)]:
        draw.ellipse((x - rx, y - ry, x + rx, y + ry), fill=(255, 230, 220, 34))

    # montanas
    draw.polygon([(0, 650), (310, 330), (620, 650)], fill=(39, 54, 88))
    draw.polygon([(420, 650), (790, 290), (1180, 650)], fill=(29, 42, 72))
    draw.polygon([(930, 650), (1260, 390), (1600, 650)], fill=(35, 49, 80))
    draw.polygon([(0, 650), (180, 500), (360, 650)], fill=(46, 62, 96))

    # nieve
    draw.polygon([(790, 290), (740, 355), (777, 345), (800, 375), (826, 340), (845, 352)], fill=(243, 238, 235))
    draw.polygon([(310, 330), (272, 382), (300, 372), (320, 394), (338, 368), (355, 380)], fill=(240, 236, 232))

    # agua y reflejo
    draw.rectangle((0, 650, w, h), fill=(19, 27, 48))
    for i, y in enumerate(range(660, h, 18)):
        alpha = max(8, 60 - i * 3)
        pad = 120 + i * 14
        draw.rectangle((pad, y, w - pad, y + 6), fill=(255, 190, 140, alpha))

    img = img.filter(ImageFilter.GaussianBlur(1.2))
    img.save(out / "paisaje.png")

    # segunda imagen, vertical y colorida
    w, h = 900, 1200
    img = sky(w, h, (18, 84, 96), (232, 196, 108))
    draw = ImageDraw.Draw(img, "RGBA")
    for i in range(22):
        x = 40 + i * 40
        draw.ellipse((x - 90, 720 + (i % 5) * 28, x + 90, 900 + (i % 5) * 28), fill=(24, 120, 118, 40))
    draw.ellipse((250, 210, 650, 610), fill=(247, 224, 150, 220))
    draw.rectangle((0, 1020, w, h), fill=(15, 40, 48))
    img = img.filter(ImageFilter.GaussianBlur(0.8))
    img.save(out / "vertical.png")

    # tercera, detalle con textura
    w, h = 1200, 800
    img = Image.new("RGB", (w, h), (28, 30, 38))
    draw = ImageDraw.Draw(img)
    for y in range(0, h, 40):
        for x in range(0, w, 40):
            c = 30 + ((x * 7 + y * 3) % 70)
            draw.rectangle((x, y, x + 36, y + 36), fill=(c, c + 12, c + 34))
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    img.save(out / "textura.png")


scene()
print(out)
