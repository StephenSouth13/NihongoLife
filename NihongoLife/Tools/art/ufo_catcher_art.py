# -*- coding: utf-8 -*-
"""UFO catcher art for the Game Center mini game (one consistent kawaii plush style).

Writes Assets/NihongoLife/Resources/MiniGames/UfoCatcher/:
  plush_<id>.png   12 plush prizes, 256x256 (drawn at 4x, downsampled)
  cabinet.png      the machine: marquee with bulbs, glass window back wall, prize mound, chute, control deck
  glass.png        reflections drawn over the prizes (the window is glass)
  claw_hub.png     the claw's motor housing; claw_prong.png one prong (the other is mirrored in UI)
Run: python Tools/art/ufo_catcher_art.py  (Pillow). Deterministic, safe to re-run.
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "NihongoLife", "Resources", "MiniGames", "UfoCatcher")
S = 4  # supersampling


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3]) + (c[3],)


class Plush:
    """Draws on a 1024 canvas (256 x 4). Every shape gets a soft darker outline."""

    def __init__(self):
        self.im = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)
        self.ow = 14

    def ell(self, box, fill, outline=None, w=None):
        self.d.ellipse(box, fill=fill, outline=outline or shade(fill, 0.62), width=self.ow if w is None else w)

    def poly(self, pts, fill, outline=None, w=None):
        self.d.polygon(pts, fill=fill)
        self.d.line(pts + [pts[0]], fill=outline or shade(fill, 0.62), width=self.ow if w is None else w, joint="curve")

    def rrect(self, box, r, fill, outline=None, w=None):
        self.d.rounded_rectangle(box, r, fill=fill, outline=outline or shade(fill, 0.62), width=self.ow if w is None else w)

    def face(self, cx, cy, k=1.0, blush="#FF8FA8", mouth="w"):
        ex, ey, er = 88 * k, 6 * k, 30 * k
        for sx in (-1, 1):
            x = cx + sx * ex
            self.d.ellipse((x - er * 0.8, cy - er + ey, x + er * 0.8, cy + er + ey), fill=hexc("#2B2236"))
            self.d.ellipse((x - er * 0.38 + 6 * k, cy - er * 0.7 + ey, x + er * 0.12 + 6 * k, cy - er * 0.2 + ey), fill=(255, 255, 255, 255))
            bx = cx + sx * 150 * k
            self.d.ellipse((bx - 40 * k, cy + 44 * k, bx + 40 * k, cy + 72 * k), fill=hexc(blush, 150))
        my = cy + 52 * k
        if mouth == "w":  # ω
            for sx in (-1, 1):
                self.d.arc((cx + (sx - 1) * 22 * k, my - 18 * k, cx + (sx + 1) * 22 * k, my + 22 * k), 20, 160, fill=hexc("#2B2236"), width=int(9 * k))
        else:
            self.d.arc((cx - 30 * k, my - 30 * k, cx + 30 * k, my + 14 * k), 25, 155, fill=hexc("#2B2236"), width=int(9 * k))

    def shadow(self, cx=512, cy=930, w=330, h=46):
        sh = Image.new("RGBA", self.im.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).ellipse((cx - w, cy - h, cx + w, cy + h), fill=(40, 20, 50, 80))
        sh = sh.filter(ImageFilter.GaussianBlur(18))
        self.im = Image.alpha_composite(sh, self.im)
        self.d = ImageDraw.Draw(self.im)

    def shine(self, box):
        hl = Image.new("RGBA", self.im.size, (0, 0, 0, 0))
        ImageDraw.Draw(hl).ellipse(box, fill=(255, 255, 255, 70))
        hl = hl.filter(ImageFilter.GaussianBlur(10))
        self.im = Image.alpha_composite(self.im, hl)
        self.d = ImageDraw.Draw(self.im)

    def save(self, name):
        img = self.im.resize((256, 256), Image.LANCZOS)
        img.save(os.path.join(OUT, name + ".png"))


def bear(fill="#C98B57", inner="#F2D2AE"):
    p = Plush()
    f, i = hexc(fill), hexc(inner)
    p.ell((300, 640, 724, 920), f)                      # body
    p.ell((340, 700, 470, 820), f); p.ell((554, 700, 684, 820), f)  # paws
    p.ell((420, 700, 604, 880), i, w=0)                 # tummy
    for x in (250, 774):
        p.ell((x - 95, 140, x + 95, 330), f); p.ell((x - 50, 185, x + 50, 285), i, w=0)
    p.ell((180, 170, 844, 720), f)                      # head
    p.ell((400, 460, 624, 640), i, w=0)                 # muzzle
    p.ell((476, 470, 548, 520), hexc("#4A2E24"), w=0)   # nose
    p.face(512, 400, 1.0)
    p.d.polygon([(512, 690), (420, 640), (420, 740)], fill=hexc("#E84A6F")); p.d.polygon([(512, 690), (604, 640), (604, 740)], fill=hexc("#E84A6F"))
    p.ell((486, 664, 538, 716), hexc("#C9365A"), w=0)   # bow
    p.shine((260, 220, 460, 340)); p.shadow(); p.save("plush_kuma")


def cat():
    p = Plush()
    f, pink = hexc("#F7EEE2"), hexc("#FFB3C4")
    p.ell((300, 640, 724, 920), f)
    p.ell((560, 600, 840, 760), hexc("#F2A65A"))         # tail
    p.ell((340, 760, 470, 880), f); p.ell((554, 760, 684, 880), f)
    for s in (-1, 1):
        x = 512 + s * 230
        p.poly([(x - 120, 330), (x + s * 90, 110), (x + 110, 330)][::s], f)
        p.poly([(x - 60, 300), (x + s * 60, 180), (x + 60, 300)][::s], pink, outline=pink, w=1)
    p.ell((180, 210, 844, 720), f)
    p.ell((560, 230, 760, 400), hexc("#F2A65A"), w=0)   # orange patch
    p.d.polygon([(486, 500), (538, 500), (512, 530)], fill=hexc("#FF7E99"))
    for s in (-1, 1):
        for dy in (-14, 14):
            p.d.line([(512 + s * 200, 520 + dy), (512 + s * 320, 505 + dy * 2)], fill=hexc("#B9A99A"), width=8)
    p.face(512, 430, 1.0)
    p.shine((260, 250, 460, 360)); p.shadow(); p.save("plush_neko")


def shiba():
    p = Plush()
    f, w = hexc("#E99A4C"), hexc("#FFF4E6")
    p.ell((300, 640, 724, 920), f)
    p.ell((410, 690, 614, 900), w, w=0)
    p.ell((620, 560, 820, 740), f); p.ell((660, 600, 780, 700), w, w=0)  # curled tail
    for s in (-1, 1):
        x = 512 + s * 220
        p.poly([(x - 110, 320), (x + s * 70, 120), (x + 110, 320)][::s], f)
        p.poly([(x - 55, 295), (x + s * 40, 190), (x + 55, 295)][::s], w, outline=w, w=1)
    p.ell((180, 210, 844, 720), f)
    p.ell((250, 420, 512, 690), w, w=0); p.ell((512, 420, 774, 690), w, w=0)  # white cheeks
    p.ell((470, 500, 554, 556), hexc("#3A2A22"), w=0)
    p.face(512, 420, 1.0)
    p.shine((260, 250, 460, 360)); p.shadow(); p.save("plush_inu")


def bunny():
    p = Plush()
    f, pink = hexc("#FFFFFF"), hexc("#FFB8CB")
    p.ell((300, 660, 724, 920), f)
    for s in (-1, 1):
        x = 512 + s * 130
        p.ell((x - 80, 20, x + 80, 420), f); p.ell((x - 38, 70, x + 38, 380), pink, w=0)
    p.ell((190, 260, 834, 740), f)
    p.d.polygon([(490, 520), (534, 520), (512, 548)], fill=hexc("#FF8FA8"))
    p.face(512, 450, 0.95)
    p.ell((620, 720, 760, 850), f)  # paw
    p.shine((260, 300, 460, 400)); p.shadow(); p.save("plush_usagi")


def chick():
    p = Plush()
    f = hexc("#FFD84D")
    p.ell((190, 220, 834, 900), f)
    for s in (-1, 1):
        x = 512 + s * 300
        p.ell((x - 70, 520, x + 70, 720), shade(f, 0.92))
    p.poly([(470, 210), (500, 110), (530, 210)], f)
    p.poly([(520, 220), (580, 140), (580, 230)], f)
    p.poly([(462, 520), (562, 520), (512, 590)], hexc("#FF9A3C"))
    p.face(512, 450, 1.0)
    for s in (-1, 1):
        p.d.line([(512 + s * 70, 900), (512 + s * 70, 950)], fill=hexc("#FF9A3C"), width=22)
    p.shine((280, 270, 480, 390)); p.shadow(cy=960); p.save("plush_tori")


def fish():
    p = Plush()
    f, belly = hexc("#6FB7FF"), hexc("#E8F4FF")
    p.poly([(760, 520), (960, 340), (960, 700)], shade(f, 0.9))   # tail
    p.poly([(420, 280), (560, 180), (600, 300)], shade(f, 0.9))   # dorsal fin
    p.ell((90, 260, 800, 790), f)
    p.ell((180, 540, 700, 760), belly, w=0)
    for x in (560, 640):
        p.d.arc((x - 60, 330, x + 60, 700), 300, 60, fill=shade(f, 0.75), width=12)
    p.face(330, 480, 0.9)
    p.shine((180, 320, 400, 420)); p.shadow(cy=880, w=380); p.save("plush_sakana")


def apple():
    p = Plush()
    f = hexc("#FF5A5A")
    p.ell((170, 230, 854, 900), f)
    p.d.line([(512, 260), (530, 130)], fill=hexc("#7A4A2A"), width=34)
    p.poly([(540, 200), (690, 120), (720, 210), (600, 260)], hexc("#5CCB6B"))
    p.face(512, 560, 1.05, blush="#FFC2C2")
    p.shine((250, 330, 450, 450)); p.shadow(); p.save("plush_ringo")


def strawberry():
    p = Plush()
    f = hexc("#FF4F6D")
    pts = [(512, 930), (230, 560), (220, 380), (330, 260), (512, 240), (694, 260), (804, 380), (794, 560)]
    p.poly(pts, f)
    random.seed(4)
    for _ in range(26):
        x, y = random.randint(300, 724), random.randint(330, 760)
        if abs(x - 512) < (930 - y) * 0.65 and not (380 < x < 650 and 470 < y < 680):
            p.d.ellipse((x - 9, y - 13, x + 9, y + 13), fill=hexc("#FFE36E"))
    for a in range(5):
        ang = math.radians(-90 + a * 72)
        tip = (512 + math.cos(ang) * 210, 260 + math.sin(ang) * 70 - 40)
        p.poly([(512 - 50, 270), tip, (512 + 50, 270)], hexc("#4DBF68"))
    p.face(512, 520, 0.95, blush="#FFC2CC")
    p.shine((280, 330, 460, 430)); p.shadow(); p.save("plush_ichigo")


def star():
    p = Plush()
    f = hexc("#FFD447")
    pts = []
    for k in range(10):
        r = 420 if k % 2 == 0 else 200
        ang = math.radians(-90 + k * 36)
        pts.append((512 + math.cos(ang) * r, 540 + math.sin(ang) * r))
    p.poly(pts, f, w=22)
    p.face(512, 540, 0.95)
    p.shine((360, 330, 520, 420)); p.shadow(cy=950); p.save("plush_hoshi")


def flower():
    p = Plush()
    pet = hexc("#FF9CC6")
    for a in range(5):
        ang = math.radians(-90 + a * 72)
        cx, cy = 512 + math.cos(ang) * 245, 520 + math.sin(ang) * 245
        p.ell((cx - 175, cy - 175, cx + 175, cy + 175), pet)
    p.ell((292, 300, 732, 740), hexc("#FFE27A"))
    p.face(512, 500, 0.8)
    p.shine((340, 340, 480, 420)); p.shadow(cy=950); p.save("plush_hana")


def key():
    p = Plush()
    f = hexc("#FFC94A")
    p.rrect((460, 520, 564, 930), 40, f)                       # shaft
    p.rrect((560, 760, 680, 830), 24, f); p.rrect((560, 860, 650, 920), 24, f)  # teeth
    p.ell((232, 120, 792, 640), f)                              # bow (head)
    p.ell((470, 60, 554, 150), hexc("#B9C2D0"), w=10)          # ring
    p.face(512, 380, 0.95)
    p.shine((300, 200, 470, 300)); p.shadow(); p.save("plush_kagi")


def candy():
    p = Plush()
    f = hexc("#C79BFF")
    for s in (-1, 1):
        p.poly([(512 + s * 230, 530), (512 + s * 450, 330), (512 + s * 450, 730)], hexc("#FF9CC6"))
    p.ell((262, 300, 762, 760), f)
    for x in (350, 450, 560, 660):
        p.d.arc((x - 120, 300, x + 120, 760), 300, 60, fill=hexc("#FFFFFF", 170), width=22)
    p.face(512, 520, 0.85)
    p.shine((320, 340, 480, 430)); p.shadow(); p.save("plush_okashi")


# ─────────────── Machine parts ───────────────

def cabinet():
    W, H = 860 * 2, 600 * 2
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    pink, deep = hexc("#FF5FA8"), hexc("#B8277A")
    d.rounded_rectangle((0, 0, W - 1, H - 1), 60, fill=deep)
    d.rounded_rectangle((14, 14, W - 15, H - 15), 50, fill=pink)
    # Marquee band with bulbs
    d.rounded_rectangle((40, 30, W - 40, 150), 40, fill=hexc("#7A1F5C"))
    for k in range(26):
        x = 80 + k * (W - 160) / 25
        for y in (50, 130):
            d.ellipse((x - 9, y - 9, x + 9, y + 9), fill=hexc("#FFE38A") if (k + (y > 90)) % 2 == 0 else hexc("#FFF6D8"))
    d.rounded_rectangle((W // 2 - 330, 64, W // 2 + 330, 116), 26, fill=hexc("#4E1240"))  # name plate (text drawn in UI)
    # Window: back wall gradient with soft stars
    wx0, wy0, wx1, wy1 = 60, 170, W - 60, H - 170
    grad = Image.new("RGBA", (wx1 - wx0, wy1 - wy0))
    gd = ImageDraw.Draw(grad)
    for y in range(wy1 - wy0):
        t = y / (wy1 - wy0)
        c = tuple(int(a + (b - a) * t) for a, b in zip((214, 236, 255), (255, 214, 236))) + (255,)
        gd.line([(0, y), (wx1 - wx0, y)], fill=c)
    random.seed(7)
    for _ in range(40):
        x, y, r = random.randint(20, wx1 - wx0 - 20), random.randint(20, (wy1 - wy0) // 2), random.randint(5, 11)
        gd.regular_polygon((x, y, r), 4, rotation=45, fill=(255, 255, 255, 150))
    gw, gh = grad.size  # the prize mound, inside the glass (clipped with it)
    gd.ellipse((270, gh - 210, gw + 20, gh + 230), fill=hexc("#F7B6D6"))
    gd.ellipse((320, gh - 170, gw - 30, gh + 230), fill=hexc("#FAC9E1"))
    mask = Image.new("L", grad.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, grad.size[0] - 1, grad.size[1] - 1), 34, fill=255)
    im.paste(grad, (wx0, wy0), mask)
    d = ImageDraw.Draw(im)
    # Prize mound (soft pink pile) and the chute on the left
    d.rounded_rectangle((wx0 + 10, wy1 - 300, wx0 + 250, wy1 - 6), 22, fill=hexc("#5A2348"))
    d.rounded_rectangle((wx0 + 30, wy1 - 280, wx0 + 230, wy1 - 26), 16, fill=hexc("#2E1026"))
    for k in range(5):  # hazard stripes on the chute rim
        x = wx0 + 30 + k * 44
        d.polygon([(x, wy1 - 300), (x + 22, wy1 - 300), (x + 44, wy1 - 280), (x + 22, wy1 - 280)], fill=hexc("#FFD54A"))
    d.rounded_rectangle((wx0, wy0, wx1, wy1), 34, outline=hexc("#FFFFFF", 200), width=8)
    # Control deck: joystick + big button
    d.rounded_rectangle((60, H - 150, W - 60, H - 40), 30, fill=hexc("#7A1F5C"))
    d.ellipse((260, H - 120, 340, H - 70), fill=hexc("#2B2236")); d.line([(300, H - 95), (300, H - 150)], fill=hexc("#2B2236"), width=14)
    d.ellipse((268, H - 178, 332, H - 128), fill=hexc("#FF4F6D"))
    d.ellipse((W - 380, H - 132, W - 280, H - 60), fill=hexc("#B8277A")); d.ellipse((W - 372, H - 140, W - 288, H - 74), fill=hexc("#FFD54A"))
    im.save(os.path.join(OUT, "cabinet.png"))


def glass():
    W, H = 1720 - 120, 1200 - 340  # the window of cabinet.png (60..W-60, 170..H-170)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for x0, w in ((120, 90), (260, 34), (W - 520, 70)):
        d.polygon([(x0, 0), (x0 + w, 0), (x0 + w - 260, H), (x0 - 260, H)], fill=(255, 255, 255, 46))
    im = im.filter(ImageFilter.GaussianBlur(3))
    im.save(os.path.join(OUT, "glass.png"))


def claw_parts():
    hub = Image.new("RGBA", (240, 140), (0, 0, 0, 0))
    d = ImageDraw.Draw(hub)
    d.rounded_rectangle((10, 20, 230, 120), 40, fill=hexc("#D9DDE6"), outline=hexc("#8A92A3"), width=8)
    d.rounded_rectangle((40, 40, 200, 100), 26, fill=hexc("#FFC94A"), outline=hexc("#C9952A"), width=6)
    d.ellipse((100, 50, 140, 90), fill=hexc("#FF4F8F"))
    d.ellipse((108, 56, 122, 70), fill=(255, 255, 255, 200))
    hub.save(os.path.join(OUT, "claw_hub.png"))
    prong = Image.new("RGBA", (120, 220), (0, 0, 0, 0))
    d = ImageDraw.Draw(prong)
    # A bent metal finger: down from the pivot (top centre), curling inward at the tip (to the right).
    d.line([(60, 10), (40, 120), (70, 190), (108, 200)], fill=hexc("#8A92A3"), width=30, joint="curve")
    d.line([(60, 10), (40, 120), (70, 190), (108, 200)], fill=hexc("#E3E7EF"), width=18, joint="curve")
    d.ellipse((44, 0, 76, 32), fill=hexc("#8A92A3"))
    prong.save(os.path.join(OUT, "claw_prong.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for fn in (bear, cat, shiba, bunny, chick, fish, apple, strawberry, star, flower, key, candy):
        fn()
    cabinet()
    glass()
    claw_parts()
    print("ufo catcher art ->", os.path.normpath(OUT))
