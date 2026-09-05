"""Vector source for the WinCompose (PolyKybd fork) icon family.

Everything the app shows as "the icon" is one keycap drawn here and rasterised
by build_icons.py.  The key is lit blue where upstream's is a cream keycap:
this fork ships with PolyKybd, whose keycaps are per-key OLED displays, and
unlike a corner badge a whole-cap change is still legible at the 16x16 the
notification area actually uses.  That is the fork mark.

Three rules the drawing has to respect, all of them learned from the 16px
render rather than from the 256px one:

  * The rim is thin (16 of 256, upstream used 34).  A rim costs the same
    fraction of the icon at every size, and at 16px upstream's left about five
    pixels for the legend.

  * The key is lit in both states and the LEGEND inverts between them, the way
    a real keycap's legend would.  The icon therefore keeps one identity at all
    times, at the cost of a smaller state difference: measured as mean
    per-pixel RGB distance between the two renders at 16px, inverting the
    legend scores 48, where darkening the whole face scored 150.  That trade
    was made deliberately; art/README.md lists the four candidates and their
    figures.

  * The difference is white-legend versus dark-legend, not a hue swap, so it
    survives greyscale and colour-blindness.  Upstream's states differed only
    in the hue of a five-pixel diamond, which does neither.
"""

import io

import cairosvg
from PIL import Image

S = 256                       # everything is drawn on a 256x256 canvas
RIM = 16                      # cap rim thickness (upstream: 34)

CAP_EDGE = "#05070c"
CAP_A, CAP_B = "#55607a", "#232b3a"        # cap body, top-left -> bottom-right

# The lit face: blue at the top-left corner, cyan at the bottom-right.  Note it
# runs COUNTER to the cap body's own diagonal, which goes light at the top-left
# to dark at the bottom-right — the face brightens as the cap darkens, so it
# reads as its own light source rather than as more of the cap's shading.
FACE_A, FACE_B = "#1550c8", "#3fe0ff"

# Two inks for anything drawn on the lit face.  Composing knocks the legend out
# rather than adding to it, so the mark keeps its shape and only its polarity
# changes; the dark ink is deep navy rather than black because it belongs to
# the blue the face is made of.
#
# Which ink to use is a measured question, not a taste one — the face runs from
# a dark blue at the top-left to a light cyan at the bottom-right, so the two
# inks are not interchangeable across it.  WCAG contrast against the face:
#
#                   top-left   centre   bottom-right
#     light ink       5.06        2.69       1.55
#     dark ink        3.01        5.66       9.84
#
# The resting diamond is light because it sits centred, is a large solid shape
# rather than text, and light-at-rest is the state pair that was chosen.
# Anything with thin strokes that reaches the bottom-right corner — the Aβ¿Δ
# and gear window legends — has to be dark, or it washes out; at 1.55:1 the
# gear's lower teeth disappeared.
LEGEND_LIGHT = "#e9eefa"
LEGEND_DARK = "#0a1830"


def _cap_d():
    r = 40
    return (f"M {r+2},2 H {254-r} A {r},{r} 0 0 1 254,{r+2} V {254-r} "
            f"A {r},{r} 0 0 1 {254-r},254 H {r+2} A {r},{r} 0 0 1 2,{254-r} "
            f"V {r+2} A {r},{r} 0 0 1 {r+2},2 Z")


def _face_d(rim=RIM):
    x, w = 2 + rim, 252 - 2 * rim
    r = max(10, 40 - rim * 0.6)
    R = x + w
    return (f"M {x+r},{x} H {R-r} A {r},{r} 0 0 1 {R},{x+r} V {R-r} "
            f"A {r},{r} 0 0 1 {R-r},{R} H {x+r} A {r},{r} 0 0 1 {x},{R-r} "
            f"V {x+r} A {r},{r} 0 0 1 {x+r},{x} Z")


def _diamond_d(scale=1.0):
    """Upstream's legend was 85x110 inside a 188-wide face; the thinner rim
    gives a 220-wide face, and the legend grows with it."""
    cx, cy = 128, 126
    hw, hh = 55 * scale, 70 * scale
    return f"M {cx},{cy-hh} L {cx+hw},{cy} L {cx},{cy+hh} L {cx-hw},{cy} Z"


_HEAD = f'<svg xmlns="http://www.w3.org/2000/svg" width="{S}" height="{S}" viewBox="0 0 {S} {S}">'


def cap():
    """The lit key with no legend.  Composited with a decal at runtime, and it
    carries the face in BOTH states — only the legend decal changes."""
    return f'''{_HEAD}
<defs>
 <linearGradient id="body" x1="0" y1="0" x2="0.65" y2="1">
  <stop offset="0" stop-color="{CAP_A}"/><stop offset="1" stop-color="{CAP_B}"/></linearGradient>
 <linearGradient id="face" x1="0" y1="0" x2="1" y2="1">
  <stop offset="0" stop-color="{FACE_A}"/><stop offset="1" stop-color="{FACE_B}"/></linearGradient>
</defs>
<path d="{_cap_d()}" fill="url(#body)" stroke="{CAP_EDGE}" stroke-opacity="0.55" stroke-width="4"/>
<path d="{_face_d()}" fill="url(#face)"/>
<path d="{_face_d()}" fill="none" stroke="{FACE_B}" stroke-opacity="0.5" stroke-width="5"/>
</svg>'''


def decal_idle():
    """Not composing: the legend sits light on the lit face."""
    return f'{_HEAD}<path d="{_diamond_d()}" fill="{LEGEND_LIGHT}"/></svg>'


def decal_active():
    """Composing: the same legend, knocked dark out of the same face."""
    return f'{_HEAD}<path d="{_diamond_d()}" fill="{LEGEND_DARK}"/></svg>'


def render(svg, size=S):
    return Image.open(io.BytesIO(cairosvg.svg2png(
        bytestring=svg.encode(), output_width=size, output_height=size))).convert("RGBA")
