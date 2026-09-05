#!/usr/bin/env python3
"""Rasterise the icon family from icons.py into the places the app loads it.

    pip install pillow cairosvg
    python art/build_icons.py            # write the assets
    python art/build_icons.py --check    # report drift, write nothing

The tray icon is composed AT RUNTIME (NotificationIcon.GetIcon: key_empty.png +
decal_idle/decal_active, plus decal_update.png).  That composition uses
Graphics.DrawImage, which scales by the ratio of the two bitmaps' DPI — so the
cap and every decal must keep the 72 DPI the existing files carry, or the decal
lands at the wrong size.  Each output below therefore names the DPI it must
keep; do not "tidy" those away.

decal_update.png and decal_disabled.png are deliberately NOT generated: the
update marker stays upstream's yellow exclamation mark, and the disabled decal
is unreferenced by the current code.
"""

import argparse
import io
import pathlib
import struct
import sys

from PIL import Image

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import icons  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parent.parent
RES = ROOT / "src" / "wincompose" / "res"
UI = ROOT / "src" / "wincompose" / "ui"
ART = ROOT / "art"

ICO_SIZES = [(16, 16), (32, 32), (48, 48), (256, 256)]


def keycap_with(overlay=None, size=icons.S):
    """The cap with an optional legend composited on top."""
    im = icons.render(icons.cap(), size)
    if overlay is not None:
        im.alpha_composite(overlay if overlay.size == im.size
                           else overlay.resize(im.size, Image.LANCZOS))
    return im


def glyph(name, color=icons.LEGEND_DARK):
    """A legend lifted from upstream's artwork (art/glyph_*.png is an alpha
    mask extracted from it) and recoloured for the lit face.

    Dark, not light: these legends have thin strokes that reach the face's
    light-cyan corner, where a light ink measures 1.55:1 — see the contrast
    table in icons.py."""
    mask = Image.open(ART / f"glyph_{name}.png").convert("RGBA").split()[3]
    tinted = Image.new("RGBA", mask.size, color)
    tinted.putalpha(mask)
    return tinted


def png_bytes(im, dpi):
    buf = io.BytesIO()
    im.save(buf, "PNG", dpi=dpi)
    return buf.getvalue()


def _dib_frame(im):
    """One ICO frame as a 32-bit DIB, the way an .ico wants it: no file header,
    and biHeight doubled because the format still reserves room for an AND mask
    (a 32-bit frame has none — Windows uses the alpha channel)."""
    buf = io.BytesIO()
    im.save(buf, "dib")
    data = buf.getvalue()
    return data[:8] + struct.pack("<I", im.height * 2) + data[12:]


def ico_bytes(make):
    """Build the .ico from `make(size) -> Image`, one native render per frame.

    Written by hand rather than through Pillow's ICO writer for two reasons:
    Pillow downsamples every frame from one source image, and it encodes the
    whole file either as DIB or as PNG.  Upstream's icons — and what the shell
    and Inno Setup handle without argument — are DIB for 16/32/48 and PNG only
    for 256, where a DIB would cost 256 KB.
    """
    frames = []
    for (w, h) in ICO_SIZES:
        im = make(w)
        frames.append(((w, h), png_bytes(im, (96, 96)) if w >= 256 else _dib_frame(im)))

    out = io.BytesIO()
    out.write(struct.pack("<HHH", 0, 1, len(frames)))
    offset = 6 + 16 * len(frames)
    for (w, h), payload in frames:
        out.write(struct.pack("<BBBBHHII",
                              w if w < 256 else 0, h if h < 256 else 0,
                              0, 0, 1, 32, len(payload), offset))
        offset += len(payload)
    for _, payload in frames:
        out.write(payload)
    return out.getvalue()


def outputs():
    """path -> bytes.  One dict so --check and the write path cannot diverge."""
    cap = icons.render(icons.cap())
    idle = icons.render(icons.decal_idle())
    active = icons.render(icons.decal_active())
    normal = keycap_with(idle)

    def normal_at(size):
        return keycap_with(icons.render(icons.decal_idle(), size), size)

    def glyph_at(name):
        return lambda size: keycap_with(glyph(name), size)

    out = {
        # tray icon, composed at runtime — 72 DPI, see the module docstring
        RES / "key_empty.png": png_bytes(cap, (72, 72)),
        RES / "decal_idle.png": png_bytes(idle, (72, 72)),
        RES / "decal_active.png": png_bytes(active, (72, 72)),

        # exe + installer icon (InsertIcons picks these up alphabetically)
        RES / "icon_normal.ico": ico_bytes(normal_at),
        UI / "icon_normal.ico": ico_bytes(normal_at),

        # window icons — WPF sizes a BitmapImage from its DPI, so keep 300
        RES / "icon_sequences.png": png_bytes(keycap_with(glyph("sequences")), (300, 300)),
        RES / "icon_settings.png": png_bytes(keycap_with(glyph("settings")), (300, 300)),
        RES / "icon_sequences.ico": ico_bytes(glyph_at("sequences")),
        RES / "icon_settings.ico": ico_bytes(glyph_at("settings")),

        # small window/menu icon
        RES / "key_compose.png": png_bytes(normal.resize((32, 32), Image.LANCZOS), (96, 96)),
        UI / "key_compose.png": png_bytes(normal.resize((32, 32), Image.LANCZOS), (96, 96)),

        # loose artwork
        ROOT / "src" / "icon.png": png_bytes(normal.resize((32, 32), Image.LANCZOS), (96, 96)),
        ROOT / "web" / "icon.png": png_bytes(normal.resize((128, 128), Image.LANCZOS), (72, 72)),
    }
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--check", action="store_true",
                    help="report which outputs differ, write nothing")
    args = ap.parse_args()

    stale = []
    for path, data in outputs().items():
        rel = path.relative_to(ROOT)
        current = path.read_bytes() if path.exists() else None
        if current == data:
            print(f"  ok      {rel}")
            continue
        stale.append(rel)
        if args.check:
            print(f"  STALE   {rel}")
        else:
            path.write_bytes(data)
            print(f"  written {rel}")

    if args.check and stale:
        print(f"\n{len(stale)} file(s) differ from the artwork in art/.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
