#!/usr/bin/env python3
"""Compose the tray icon's states into art/preview/, for use in docs and issue
threads.

    pip install pillow
    python art/build_preview.py            # write the previews
    python art/build_preview.py --check    # report drift, write nothing

The tray icon does not exist as a file: NotificationIcon.GetIcon composes it at
runtime from key_empty.png plus decal_active, with a decal_spin* quarter over
the top while composing and decal_update over that.  So there is nothing to
point at when someone asks what a state looks like, which is what these are
for -- and the composing state is an animation, which a still cannot show at
all, hence the GIFs.

They are composed from the SHIPPED res/*.png rather than re-rendered from
icons.py, so a preview always shows what the app actually draws.  No text is
burned in -- the labels belong in whatever renders these, where they stay
selectable and translatable, and a font in the image would make the output
depend on which DejaVu the machine happens to have.
"""

import argparse
import io
import pathlib
import sys

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent.parent
RES = ROOT / "src" / "wincompose" / "res"
PREVIEW = ROOT / "art" / "preview"

BIG = 96        # the state pictures
SMALL = 16      # what the notification area actually asks for
ZOOM = 5        # nearest-neighbour, so the 16px pixel grid stays visible
GAP = 16
FRAME_MS = 140  # keep in step with NotificationIcon's SpinFrameMs

SPIN = 4        # keep in step with icons.SPIN_QUARTERS


def _load(name):
    return Image.open(RES / f"{name}.png").convert("RGBA")


def _compose(*decals):
    im = _load("key_empty")
    for decal in decals:
        im.alpha_composite(_load(decal))
    return im


def _png(im):
    buf = io.BytesIO()
    im.save(buf, "PNG")
    return buf.getvalue()


def _gif(frames, size, zoom):
    """An animated GIF at `size`, magnified `zoom` times with no interpolation.

    Kept transparent so it reads on a light or a dark page.  GIF transparency
    is one bit, so the cap's anti-aliased corners are thresholded; at these
    sizes that costs a pixel of rounding and nothing else.
    """
    out = []
    for im in frames:
        small = im.resize((size, size), Image.LANCZOS)
        big = small.resize((size * zoom,) * 2, Image.NEAREST)
        rgb = big.convert("RGB").quantize(colors=255, method=Image.MEDIANCUT)
        # index 255 is free after quantising to 255 colours: make it the
        # transparent one and stamp it wherever the alpha says so
        mask = big.split()[3].point(lambda a: 255 if a < 128 else 0)
        rgb.paste(255, (0, 0), mask)
        out.append(rgb)
    buf = io.BytesIO()
    out[0].save(buf, "GIF", save_all=True, append_images=out[1:],
                duration=FRAME_MS, loop=0, transparency=255, disposal=2)
    return buf.getvalue()


def outputs():
    """path -> bytes.  One dict so --check and the write path cannot diverge."""
    idle = _compose("decal_active")
    spin = [_compose("decal_active", f"decal_spin{i}") for i in range(SPIN)]

    out = {
        PREVIEW / "tray_idle.png": _png(idle.resize((BIG, BIG), Image.LANCZOS)),
        PREVIEW / "tray_idle_update.png":
            _png(_compose("decal_active", "decal_update")
                 .resize((BIG, BIG), Image.LANCZOS)),
    }
    for i, im in enumerate(spin):
        out[PREVIEW / f"tray_composing_{i}.png"] = _png(
            im.resize((BIG, BIG), Image.LANCZOS))

    # The composing state IS the motion, so the GIFs are the real artefact and
    # the stills above are only for a reader who cannot see one.
    out[PREVIEW / "tray_composing.gif"] = _gif(spin, BIG, 1)
    out[PREVIEW / "tray_composing_16px.gif"] = _gif(spin, SMALL, ZOOM)

    # Idle then the whole cycle, at the size the state pair has to survive.
    strip_frames = [idle] + spin
    cell = SMALL * ZOOM
    strip = Image.new("RGBA",
                      (cell * len(strip_frames) + GAP * (len(strip_frames) - 1), cell),
                      (0, 0, 0, 0))
    for i, im in enumerate(strip_frames):
        small = im.resize((SMALL, SMALL), Image.LANCZOS)
        strip.alpha_composite(small.resize((cell, cell), Image.NEAREST),
                              (i * (cell + GAP), 0))
    out[PREVIEW / "tray_16px.png"] = _png(strip)
    return out


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--check", action="store_true",
                    help="report which previews differ, write nothing")
    args = ap.parse_args()

    stale = []
    PREVIEW.mkdir(parents=True, exist_ok=True)
    wanted = outputs()
    for path, data in wanted.items():
        rel = path.relative_to(ROOT)
        if (path.read_bytes() if path.exists() else None) == data:
            print(f"  ok      {rel}")
            continue
        stale.append(rel)
        if args.check:
            print(f"  STALE   {rel}")
        else:
            path.write_bytes(data)
            print(f"  written {rel}")

    # a preview left behind by an older state set is worse than none: it still
    # renders, and nothing says it is describing an icon the app stopped drawing
    for path in sorted(PREVIEW.glob("*")):
        if path not in wanted:
            rel = path.relative_to(ROOT)
            stale.append(rel)
            if args.check:
                print(f"  ORPHAN  {rel}")
            else:
                path.unlink()
                print(f"  removed {rel}")

    if args.check and stale:
        print(f"\n{len(stale)} preview(s) differ from the shipped icons.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
