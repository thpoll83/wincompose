#!/usr/bin/env python3
#
#  Render the tray menu's icons to PNG.
#
#  The menu used to draw these four as emoji through Emoji.Wpf. They were the
#  only emoji on the startup path, and drawing them made the library parse the
#  emoji font, which costs about 41 MB of managed heap that is never released --
#  measured on three machines, live heap settled at 54.1 MB and stayed there for
#  the life of the process. Pre-rendering them leaves Emoji.Wpf to the windows
#  that show user content, where it is the point and where the cost is only paid
#  if the window is opened.
#
#  Run this rather than editing the PNGs by hand:
#
#      pip install pillow
#      python3 src/render-menu-icons.py
#
#  Needs NotoColorEmoji (Debian/Ubuntu: fonts-noto-color-emoji). Pass a path as
#  the first argument to render from a different emoji font.
#

import os
import sys

from PIL import Image, ImageFont, ImageDraw

# Four times the 18px the menu draws them at, so they stay crisp at 400% DPI.
SIZE = 72

# NotoColorEmoji is a bitmap (CBDT) font with one fixed strike, so this is the
# size that hits it natively; anything else makes FreeType scale twice.
STRIKE_PT = 109

DEFAULT_FONT = "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf"

# Keyed by the resource name in ui/NotificationIcon.xaml, minus the "Emoji".
ICONS = {
    "sequences": "\U0001F4AC",  # speech balloon
    "options":   "\U0001F6E0",  # hammer and wrench
    "debug":     "\U0001F50E",  # magnifier tilted right
    "exit":      "\U0001F6AA",  # door
}


def render(font, char):
    """The glyph's ink, cropped and centred on a square, transparent canvas."""
    for text in (char, char + "️"):  # bare, then emoji presentation
        img = Image.new("RGBA", (160, 160), (0, 0, 0, 0))
        ImageDraw.Draw(img).text((12, 12), text, font=font, embedded_color=True)
        box = img.getbbox()
        if box:
            break
    if not box:
        raise SystemExit(f"nothing rendered for U+{ord(char[0]):04X}")

    glyph = img.crop(box)
    # A square canvas so every icon shares one scale and sits on one baseline;
    # a narrow glyph (the door) would otherwise come out taller than the rest.
    side = max(glyph.size)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(glyph, ((side - glyph.width) // 2, (side - glyph.height) // 2))
    return canvas.resize((SIZE, SIZE), Image.LANCZOS)


def main():
    font_path = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_FONT
    if not os.path.exists(font_path):
        raise SystemExit(f"no emoji font at {font_path} "
                         f"(Debian/Ubuntu: apt install fonts-noto-color-emoji)")

    font = ImageFont.truetype(font_path, STRIKE_PT)
    out_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                           "wincompose", "res", "menu")
    os.makedirs(out_dir, exist_ok=True)

    for name, char in ICONS.items():
        path = os.path.join(out_dir, name + ".png")
        render(font, char).save(path)
        print(f"{name:10s} U+{ord(char[0]):04X} -> {os.path.relpath(path)}")


if __name__ == "__main__":
    main()
