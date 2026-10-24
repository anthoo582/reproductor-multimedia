from PIL import Image

for name in ["readme-main", "readme-playlist", "readme-menu"]:
    im = Image.open(f"docs/img/{name}.png").convert("RGB")
    w, h = im.size
    print(name, im.size)
    for p in [(5, 5), (10, 20), (500, 5), (w - 5, 5), (w - 5, h // 2), (w - 5, h - 5), (500, h - 5), (500, h - 15), (w // 2, h // 2)]:
        print(" ", p, im.getpixel(p))
