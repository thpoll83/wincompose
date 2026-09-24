#!/usr/bin/env python3
"""Compose the tray icon's four states into art/preview/, for use in docs and
issue threads.

    pip install pillow
    python art/build_preview.py            # write the previews
    python art/build_preview.py --check    # report drift, write nothing

The tray icon does not exist as a file: NotificationIcon.GetIcon composes it at
runtime from key_empty.png plus decal_idle or decal_active, with decal_update
over the top.  So there is nothing to point at when someone asks what a state
looks like, which is what these are for.

They are composed from the SHIPPED res/*.png rather than re-rendered from
icons.py, so a preview always shows what the app actually draws.  No text is
burned in — the labels belong in whatever renders these, where they stay
selectable and translatable.
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

# name -> the decals GetIcon stacks on key_empty, in its order
STATES = {
    "tray_idle": ["decal_idle"],
    "tray_composing": ["decal_active"],
    "tray_idle_update": ["decal_idle", "decal_update"],
    "tray_composing_update": ["decal_active", "decal_update"],
}


def _load(name):
    return Image.open(RES / f"{name}.png").convert("RGBA")


def _state(decals):
    im = _load("key_empty")
    for decal in decals:
        im.alpha_composite(_load(decal))
    return im


def _png(im):
    buf = io.BytesIO()
    im.save(buf, "PNG")
    return buf.getvalue()


def outputs():
    """path -> bytes.  One dict so --check and the write path cannot diverge."""
    states = {name: _state(decals) for name, decals in STATES.items()}

    out = {PREVIEW / f"{name}.png": _png(im.resize((BIG, BIG), Image.LANCZOS))
           for name, im in states.items()}

    # The state pair was chosen against the 16px render, so show that too:
    # the argument is only checkable at the size the tray uses.
    cell = SMALL * ZOOM
    strip = Image.new("RGBA", (cell * len(states) + GAP * (len(states) - 1), cell),
                      (0, 0, 0, 0))
    for i, im in enumerate(states.values()):
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
    for path, data in outputs().items():
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

    if args.check and stale:
        print(f"\n{len(stale)} preview(s) differ from the shipped icons.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
