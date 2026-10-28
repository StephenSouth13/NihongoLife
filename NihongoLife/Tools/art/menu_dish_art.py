"""Draws the menu pictures of Sushi Hibari (Resources/Restaurants/Dishes/<dish id>.png).

Each dish is drawn the same way so the menu reads as one set: a soft shadow, a plate or bowl seen from three
quarters above, the food in flat colours with a light rim and a highlight. Run from the project folder:
    python Tools/art/menu_dish_art.py
The pictures are original (no third-party art). Add a dish = add an entry to DISHES.
"""
import math
import os
from PIL import Image, ImageDraw, ImageFilter

SIZE = 512
SS = 2  # supersampling
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "NihongoLife", "Resources", "Restaurants", "Dishes")

RICE = (250, 248, 240)
RICE_SHADE = (225, 220, 205)
NORI = (34, 48, 38)
WASABI = (140, 190, 80)
GINGER = (240, 170, 160)
SOY = (70, 40, 25)
PLATE = (246, 244, 238)
PLATE_RIM = (210, 206, 196)
WOOD = (190, 140, 90)
WOOD_DARK = (150, 105, 65)
LACQUER = (150, 30, 30)
LACQUER_DARK = (95, 18, 18)


def canvas():
    img = Image.new("RGBA", (SIZE * SS, SIZE * SS), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def s(v):
    return int(v * SS)


def ellipse(d, cx, cy, rx, ry, fill, outline=None, width=0):
    d.ellipse([s(cx - rx), s(cy - ry), s(cx + rx), s(cy + ry)], fill=fill, outline=outline, width=s(width) if width else 0)


def rrect(d, x0, y0, x1, y1, r, fill, outline=None, width=0):
    d.rounded_rectangle([s(x0), s(y0), s(x1), s(y1)], radius=s(r), fill=fill, outline=outline, width=s(width) if width else 0)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def shadow(img, cx, cy, rx, ry, alpha=90):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    ellipse(ld, cx, cy, rx, ry, (20, 14, 8, alpha))
    layer = layer.filter(ImageFilter.GaussianBlur(s(14)))
    img.alpha_composite(layer)


def plate(img, d, cx=256, cy=300, rx=210, ry=120, wood=False):
    shadow(img, cx, cy + 22, rx, ry * 0.9)
    if wood:  # a sushi geta board
        rrect(d, cx - rx, cy - ry * 0.75, cx + rx, cy + ry * 0.75, 18, WOOD_DARK)
        rrect(d, cx - rx, cy - ry * 0.85, cx + rx, cy + ry * 0.65, 18, WOOD)
        for i in range(5):
            y = cy - ry * 0.7 + i * ry * 0.3
            d.line([s(cx - rx + 20), s(y), s(cx + rx - 20), s(y + 4)], fill=shade(WOOD, 0.92), width=s(2))
        return
    ellipse(d, cx, cy, rx, ry, PLATE_RIM)
    ellipse(d, cx, cy - 6, rx - 6, ry - 6, PLATE)
    ellipse(d, cx, cy - 2, rx * 0.72, ry * 0.68, shade(PLATE, 0.97))


def nigiri(img, d, cx, cy, topping, pattern=None, w=150):
    # rice block
    ellipse(d, cx, cy + 18, w * 0.5, 38, RICE_SHADE)
    ellipse(d, cx, cy + 6, w * 0.48, 40, RICE)
    for i in range(14):
        a = i * 2.4
        ellipse(d, cx + math.cos(a) * w * 0.32, cy + 10 + math.sin(a) * 18, 7, 4, (255, 255, 252))
    # topping draped over
    pts = [(cx - w * 0.6, cy - 4), (cx - w * 0.4, cy - 34), (cx + w * 0.4, cy - 40), (cx + w * 0.62, cy - 10),
           (cx + w * 0.45, cy + 14), (cx - w * 0.45, cy + 18)]
    d.polygon([(s(x), s(y)) for x, y in pts], fill=topping)
    hi = shade(topping, 1.18)
    d.line([(s(cx - w * 0.35), s(cy - 26)), (s(cx + w * 0.38), s(cy - 32))], fill=hi, width=s(6))
    if pattern == "stripes":  # salmon / tuna fat lines
        for i in range(5):
            x = cx - w * 0.4 + i * w * 0.2
            d.line([(s(x), s(cy - 34)), (s(x + 18), s(cy + 14))], fill=shade(topping, 1.3), width=s(4))
    if pattern == "ebi":
        for i in range(4):
            x = cx - w * 0.35 + i * w * 0.22
            d.line([(s(x), s(cy - 34)), (s(x + 10), s(cy + 14))], fill=(235, 90, 50), width=s(7))
        d.polygon([(s(cx + w * 0.6), s(cy - 10)), (s(cx + w * 0.8), s(cy - 30)), (s(cx + w * 0.78), s(cy + 6))], fill=(235, 90, 50))
    if pattern == "nori_band":
        rrect(d, cx - 16, cy - 42, cx + 16, cy + 22, 4, NORI)
    if pattern == "glaze":
        for i in range(3):
            d.line([(s(cx - w * 0.4), s(cy - 22 + i * 12)), (s(cx + w * 0.42), s(cy - 26 + i * 12))], fill=shade(topping, 0.75), width=s(3))
        rrect(d, cx - 12, cy - 42, cx + 12, cy + 22, 4, NORI)


def gunkan(img, d, cx, cy, top, beads=False):
    ellipse(d, cx, cy + 30, 58, 26, shade(NORI, 0.8))
    rrect(d, cx - 58, cy - 20, cx + 58, cy + 30, 10, NORI)
    ellipse(d, cx, cy - 20, 58, 24, RICE)
    ellipse(d, cx, cy - 26, 50, 20, top)
    if beads:
        for i in range(22):
            a = i * 2.39
            r = 8 + (i % 5) * 7
            ellipse(d, cx + math.cos(a) * r * 1.4, cy - 26 + math.sin(a) * r * 0.5, 7, 6, shade(top, 1.15))
            ellipse(d, cx + math.cos(a) * r * 1.4 - 2, cy - 28 + math.sin(a) * r * 0.5, 2, 2, (255, 230, 200))


def maki(img, d, cx, cy, filling, r=46, inside_out=False, colors=None):
    ellipse(d, cx, cy + 16, r, r * 0.5, shade(NORI, 0.8) if not inside_out else RICE_SHADE)
    rrect(d, cx - r, cy - 8, cx + r, cy + 16, 6, NORI if not inside_out else RICE)
    ellipse(d, cx, cy - 8, r, r * 0.5, NORI if not inside_out else (240, 140, 60))
    ellipse(d, cx, cy - 8, r - 6, r * 0.5 - 4, RICE)
    if colors:
        for i, c in enumerate(colors):
            a = i * 2 * math.pi / len(colors)
            ellipse(d, cx + math.cos(a) * 12, cy - 8 + math.sin(a) * 6, 10, 6, c)
    else:
        ellipse(d, cx, cy - 8, r * 0.32, r * 0.17, filling)


def bowl(img, d, cx, cy, rx, ry, color, inner=None, lacquer=True):
    shadow(img, cx, cy + ry * 1.3, rx * 0.9, ry * 0.5)
    body = [(cx - rx, cy), (cx + rx, cy), (cx + rx * 0.62, cy + ry * 1.5), (cx - rx * 0.62, cy + ry * 1.5)]
    d.polygon([(s(x), s(y)) for x, y in body], fill=color)
    ellipse(d, cx, cy + ry * 1.5, rx * 0.62, ry * 0.3, shade(color, 0.7))
    ellipse(d, cx, cy, rx, ry, shade(color, 1.12))
    ellipse(d, cx, cy + 4, rx - 10, ry - 7, inner if inner else shade(color, 0.6))


def cup(img, d, cx, cy, color, drink):
    shadow(img, cx, cy + 120, 80, 26)
    body = [(cx - 78, cy - 20), (cx + 78, cy - 20), (cx + 66, cy + 115), (cx - 66, cy + 115)]
    d.polygon([(s(x), s(y)) for x, y in body], fill=color)
    ellipse(d, cx, cy + 115, 66, 16, shade(color, 0.75))
    ellipse(d, cx, cy - 20, 78, 22, shade(color, 1.1))
    ellipse(d, cx, cy - 16, 70, 18, drink)


def glass(img, d, cx, cy, drink, ice=True):
    shadow(img, cx, cy + 128, 70, 22)
    body = [(cx - 70, cy - 60), (cx + 70, cy - 60), (cx + 60, cy + 125), (cx - 60, cy + 125)]
    d.polygon([(s(x), s(y)) for x, y in body], fill=(220, 236, 245, 140))
    liquid = [(cx - 66, cy - 30), (cx + 66, cy - 30), (cx + 60, cy + 120), (cx - 60, cy + 120)]
    d.polygon([(s(x), s(y)) for x, y in liquid], fill=drink)
    if ice:
        for i in range(3):
            rrect(d, cx - 46 + i * 30, cy - 36 + (i % 2) * 14, cx - 18 + i * 30, cy - 8 + (i % 2) * 14, 5, (235, 245, 252, 200))
    ellipse(d, cx, cy - 60, 70, 16, (235, 245, 252, 170), outline=(200, 220, 235), width=2)
    d.line([(s(cx - 52), s(cy - 40)), (s(cx - 44), s(cy + 100))], fill=(255, 255, 255, 150), width=s(6))


def sauce_and_garnish(d, cx, cy):
    ellipse(d, cx, cy, 22, 14, WASABI)
    ellipse(d, cx - 4, cy - 4, 8, 4, shade(WASABI, 1.2))
    for i in range(3):
        ellipse(d, cx + 46 + i * 6, cy - 2 + i * 3, 16, 9, GINGER)


def finish(img, name):
    # Frame the dish: a square around what was drawn, with a margin, so every picture fills its card alike.
    box = img.getchannel("A").point(lambda a: 255 if a > 24 else 0).getbbox() or (0, 0, img.width, img.height)
    cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
    half = max(box[2] - box[0], box[3] - box[1]) / 2 * 1.12
    img = img.crop((int(cx - half), int(cy - half), int(cx + half), int(cy + half)))
    out = img.resize((SIZE, SIZE), Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    out.save(os.path.join(OUT, name + ".png"))


def two_nigiri(topping, pattern=None):
    def draw():
        img, d = canvas()
        plate(img, d, wood=True)
        nigiri(img, d, 180, 270, topping, pattern)
        nigiri(img, d, 330, 300, topping, pattern)
        sauce_and_garnish(d, 120, 360)
        return img
    return draw


def two_gunkan(top, beads=False):
    def draw():
        img, d = canvas()
        plate(img, d, wood=True)
        gunkan(img, d, 185, 285, top, beads)
        gunkan(img, d, 325, 305, top, beads)
        return img
    return draw


def maki_plate(filling, inside_out=False, colors=None, r=46):
    def draw():
        img, d = canvas()
        plate(img, d)
        for i, (x, y) in enumerate([(160, 270), (256, 252), (352, 270), (205, 330), (305, 330)]):
            maki(img, d, x, y, filling, r, inside_out, colors)
        sauce_and_garnish(d, 410, 340)
        return img
    return draw


def donburi(toppings):
    def draw():
        img, d = canvas()
        bowl(img, d, 256, 220, 190, 70, LACQUER, inner=RICE)
        for (x, y, rx, ry, c) in toppings:
            ellipse(d, x, y, rx, ry, c)
            ellipse(d, x - rx * 0.3, y - ry * 0.4, rx * 0.35, ry * 0.25, shade(c, 1.2))
        return img
    return draw


def chirashi():
    tops = []
    colors = [(200, 40, 50), (245, 120, 70), (250, 220, 90), (240, 236, 220), (240, 150, 60), (90, 160, 70)]
    for i in range(14):
        a = i * 2.3
        tops.append((256 + math.cos(a) * (40 + i * 9) * 0.9, 222 + math.sin(a) * (14 + i * 2.6), 24, 13, colors[i % len(colors)]))
    return donburi(tops)


def kaisendon():
    tops = [(200, 215, 46, 22, (200, 40, 50)), (300, 210, 46, 22, (245, 120, 70)), (250, 238, 40, 18, (240, 236, 220)),
            (330, 240, 30, 14, (220, 60, 30)), (175, 245, 28, 14, (250, 220, 90)), (256, 196, 20, 12, (90, 160, 70))]
    return donburi(tops)


def unadon():
    def draw():
        img, d = canvas()
        rrect(d, 70, 150, 442, 392, 30, LACQUER_DARK)
        rrect(d, 70, 140, 442, 372, 30, LACQUER)
        rrect(d, 92, 160, 420, 352, 22, RICE)
        for i in range(3):
            rrect(d, 112 + i * 104, 190, 200 + i * 104, 320, 14, (120, 70, 30))
            for k in range(4):
                d.line([(s(118 + i * 104), s(205 + k * 30)), (s(194 + i * 104), s(200 + k * 30))], fill=(85, 45, 18), width=s(4))
        return img
    return draw


def hibari_set():
    def draw():
        img, d = canvas()
        plate(img, d, wood=True, ry=140)
        nigiri(img, d, 150, 240, (200, 40, 50), w=110)
        nigiri(img, d, 250, 230, (245, 120, 70), "stripes", w=110)
        nigiri(img, d, 350, 245, (250, 220, 90), "nori_band", w=110)
        maki(img, d, 190, 330, (110, 170, 80), r=36)
        maki(img, d, 270, 340, (200, 40, 50), r=36)
        gunkan(img, d, 360, 330, (235, 90, 40), beads=True)
        return img
    return draw


def miso():
    def draw():
        img, d = canvas()
        bowl(img, d, 256, 230, 170, 66, LACQUER, inner=(190, 140, 80))
        for i in range(6):
            a = i * 1.1
            rrect(d, 220 + math.cos(a) * 60, 226 + math.sin(a) * 18, 240 + math.cos(a) * 60, 240 + math.sin(a) * 18, 3, (250, 248, 235))
        for i in range(8):
            a = i * 0.8
            ellipse(d, 256 + math.cos(a) * 90, 236 + math.sin(a) * 26, 7, 4, (110, 170, 70))
        return img
    return draw


def chawanmushi():
    def draw():
        img, d = canvas()
        cup(img, d, 256, 220, (230, 225, 215), (248, 220, 120))
        ellipse(d, 256, 198, 36, 12, (110, 170, 70))
        ellipse(d, 290, 205, 14, 8, (235, 90, 50))
        return img
    return draw


def edamame():
    def draw():
        img, d = canvas()
        bowl(img, d, 256, 230, 170, 60, (230, 225, 215), inner=(90, 150, 60))
        for i in range(16):
            a = i * 2.4
            x, y = 256 + math.cos(a) * (20 + i * 7), 230 + math.sin(a) * (8 + i * 2)
            rrect(d, x - 28, y - 9, x + 28, y + 9, 9, (110, 175, 70))
            for k in range(3):
                ellipse(d, x - 16 + k * 16, y, 7, 6, (140, 200, 90))
        return img
    return draw


def ocha():
    def draw():
        img, d = canvas()
        cup(img, d, 256, 230, (120, 140, 110), (160, 190, 90))
        for i in range(3):
            d.arc([s(220 + i * 24), s(120), s(250 + i * 24), s(200)], 200, 340, fill=(255, 255, 255, 120), width=s(4))
        return img
    return draw


def oolong():
    def draw():
        img, d = canvas()
        glass(img, d, 256, 220, (170, 100, 40, 230))
        return img
    return draw


def ramune():
    def draw():
        img, d = canvas()
        shadow(img, 256, 400, 60, 20)
        rrect(d, 206, 170, 306, 400, 40, (120, 200, 230, 210))
        rrect(d, 226, 90, 286, 190, 20, (120, 200, 230, 210))
        ellipse(d, 256, 160, 18, 18, (170, 225, 245))  # the marble
        rrect(d, 222, 70, 290, 100, 8, (40, 110, 200))
        rrect(d, 206, 260, 306, 330, 6, (40, 110, 200))
        d.line([(s(222), s(200)), (s(226), s(380))], fill=(255, 255, 255, 160), width=s(8))
        return img
    return draw


def matcha_ice():
    def draw():
        img, d = canvas()
        bowl(img, d, 256, 280, 130, 44, (230, 225, 215), inner=(200, 190, 175))
        ellipse(d, 256, 240, 90, 70, (140, 185, 80))
        ellipse(d, 230, 215, 34, 22, (170, 210, 110))
        ellipse(d, 300, 260, 16, 12, (120, 60, 50))  # red bean
        ellipse(d, 285, 268, 14, 10, (120, 60, 50))
        return img
    return draw


def mitarashi():
    def draw():
        img, d = canvas()
        plate(img, d)
        for k in range(2):
            y = 260 + k * 60
            d.line([(s(110), s(y + 20)), (s(400), s(y - 20))], fill=(220, 190, 140), width=s(6))
            for i in range(4):
                x = 160 + i * 62
                yy = y + 14 - i * 11
                ellipse(d, x, yy, 30, 26, (240, 230, 210))
                ellipse(d, x, yy - 4, 26, 18, (190, 120, 50, 220))
                ellipse(d, x - 8, yy - 10, 8, 5, (240, 200, 140))
        return img
    return draw


DISHES = {
    "maguro": two_nigiri((200, 40, 50)),
    "salmon": two_nigiri((245, 120, 70), "stripes"),
    "ebi": two_nigiri((250, 240, 230), "ebi"),
    "tamago": two_nigiri((250, 215, 90), "nori_band"),
    "ika": two_nigiri((245, 242, 235)),
    "unagi_nigiri": two_nigiri((130, 75, 30), "glaze"),
    "hotate": two_nigiri((250, 236, 215)),
    "ikura": two_gunkan((230, 80, 30), beads=True),
    "kappamaki": maki_plate((110, 175, 70)),
    "tekkamaki": maki_plate((200, 40, 50)),
    "futomaki": maki_plate(None, colors=[(250, 215, 90), (110, 175, 70), (240, 120, 150), (130, 80, 40)], r=58),
    "california": maki_plate(None, inside_out=True, colors=[(150, 200, 90), (245, 236, 225), (240, 120, 60)], r=50),
    "chirashi": chirashi(),
    "kaisendon": kaisendon(),
    "unadon": unadon(),
    "hibari_set": hibari_set(),
    "misoshiru": miso(),
    "chawanmushi": chawanmushi(),
    "edamame": edamame(),
    "ocha": ocha(),
    "oolong": oolong(),
    "ramune": ramune(),
    "matcha_ice": matcha_ice(),
    "mitarashi": mitarashi(),
}

if __name__ == "__main__":
    for name, draw in DISHES.items():
        finish(draw(), name)
    print(f"{len(DISHES)} dish pictures in {os.path.normpath(OUT)}")
