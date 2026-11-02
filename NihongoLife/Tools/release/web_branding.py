#!/usr/bin/env python3
"""The web page's brand pictures, all made from the game's own logo (Resources/Branding):
TemplateData/logo.png (light lettering for the dark loader), favicon, Apple touch icon and the link preview picture.
Run from the Unity project folder after the logo or the town pictures change:  python Tools/release/web_branding.py
"""
import os

from PIL import Image, ImageDraw, ImageEnhance

BRAND = os.path.join('Assets', 'NihongoLife', 'Resources', 'Branding')
OUT = os.path.join('Assets', 'WebGLTemplates', 'NihongoLife', 'TemplateData')


def light_logo():
    """The logo's dark lettering turned light; the mark on the left keeps its colours."""
    logo = Image.open(os.path.join(BRAND, 'nihongolife_logo.png')).convert('RGBA')
    pixels = logo.load()
    for y in range(logo.height):
        for x in range(430, logo.width):
            r, g, b, a = pixels[x, y]
            if a == 0 or r > 150 and g > 120 and b < 140:  # the gold "Life" stays gold
                continue
            pixels[x, y] = (255, 255, 255, a) if r + g + b < 200 else (205, 215, 230, a)
    return logo.resize((720, 200), Image.LANCZOS)


def main():
    light = light_logo()
    light.save(os.path.join(OUT, 'logo.png'), optimize=True)
    light.save(os.path.join(BRAND, 'nihongolife_logo_light.png'), optimize=True)  # the main menu's logo
    mark = Image.open(os.path.join(BRAND, 'nihongolife_mark.png')).convert('RGBA')
    mark.resize((32, 32), Image.LANCZOS).save(os.path.join(OUT, 'favicon.png'), optimize=True)
    mark.resize((180, 180), Image.LANCZOS).save(os.path.join(OUT, 'apple-touch-icon.png'), optimize=True)

    town = Image.open(os.path.join(OUT, 'art', '90-testsandbox.jpg')).convert('RGB')
    scale = max(1200 / town.width, 630 / town.height)
    town = town.resize((round(town.width * scale), round(town.height * scale)), Image.LANCZOS)
    left, top = (town.width - 1200) // 2, (town.height - 630) // 2
    card = ImageEnhance.Brightness(town.crop((left, top, left + 1200, top + 630))).enhance(0.55).convert('RGBA')
    band = Image.new('RGBA', card.size, (0, 0, 0, 0))
    ImageDraw.Draw(band).rectangle((0, 190, 1200, 440), fill=(14, 17, 22, 170))
    card = Image.alpha_composite(card, band)
    wide = light.resize((792, 220), Image.LANCZOS)
    card.alpha_composite(wide, ((1200 - wide.width) // 2, 205))
    card.convert('RGB').save(os.path.join(OUT, 'og.png'), optimize=True)
    print('logo, favicon, touch icon and preview written')


if __name__ == '__main__':
    main()
