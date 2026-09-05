"""Vector source for the WinCompose (PolyKybd fork) icon family.

Everything the app shows as "the icon" is one keycap drawn here and rasterised
by build_icons.py.  The cap is deliberately NOT upstream's cream keycap: this
fork ships with PolyKybd, whose keycaps are little OLED displays, so the cap is
dark with a lit legend.  That is the fork mark, and unlike a corner badge it is
still readable at the 16x16 the notification area actually uses.

Three rules the drawing has to respect, all of them learned from the 16px
render rather than from the 256px one:

  * The rim is thin (16 of 256, upstream used 34).  The rim costs the same
    fraction of the icon at every size, and at 16px upstream's rim left about
    5px for the legend.
  * Idle and composing differ in BRIGHTNESS over a large area, not only in hue.
    A hue swap on a shape that small is what made the old states hard to tell
    apart, and it disappears entirely for a red-green colour-blind reader.
  * The cap stays dark in every state, so the fork is recognisable while
    composing too.
"""

import io

import cairosvg
from PIL import Image

S = 256                       # everything is drawn on a 256x256 canvas
RIM = 16                      # cap rim thickness (upstream: 34)

CAP_EDGE = "#05070c"
CAP_A, CAP_B = "#55607a", "#232b3a"        # cap body, top-left -> bottom-right
FACE_A, FACE_B = "#1b2231", "#0b0f18"      # display face
LEGEND = "#e9eefa"                         # legend at rest
# The legend stays near-white in both states on purpose: it is the key's
# identity, and what changes is the FACE.  A legend that changed colour instead
# would put the whole state difference back into a five-pixel shape.
LEGEND_LIT = "#f2fbff"                     # legend while composing
# Cyan fading to PolyKybd blue, not the green an "active" state usually gets.
# Measured over the four candidates at 16px, as mean per-pixel RGB distance
# between the idle and composing renders: this pair scores 49 against 32 for a
# green wash, because cyan is far brighter than a mid-green and the state
# change here is carried by brightness over the whole face.  A blue CAP scored
# worst of all (27) — it reads as already lit, and leaves the lit state
# nowhere to go.
# Blue at the top-left corner, cyan at the bottom-right.  Note this runs
# COUNTER to the cap's own diagonal, which goes light at the top-left to dark at
# the bottom-right: the lit face brightens the way the cap darkens, so it reads
# as its own light source rather than as more of the cap's shading.
GLOW_A, GLOW_B = "#1550c8", "#3fe0ff"      # the "key is lit" wash: TL -> BR


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
    """The bare keycap — no legend.  Composited with a decal at runtime."""
    return f'''{_HEAD}
<defs>
 <linearGradient id="body" x1="0" y1="0" x2="0.65" y2="1">
  <stop offset="0" stop-color="{CAP_A}"/><stop offset="1" stop-color="{CAP_B}"/></linearGradient>
 <linearGradient id="face" x1="0.1" y1="0" x2="0.8" y2="1">
  <stop offset="0" stop-color="{FACE_A}"/><stop offset="1" stop-color="{FACE_B}"/></linearGradient>
</defs>
<path d="{_cap_d()}" fill="url(#body)" stroke="{CAP_EDGE}" stroke-opacity="0.55" stroke-width="4"/>
<path d="{_face_d()}" fill="url(#face)"/>
</svg>'''


def decal_idle():
    return f'{_HEAD}<path d="{_diamond_d()}" fill="{LEGEND}"/></svg>'


def decal_active():
    """Composing: the display lights up.  The wash covers the whole face so the
    change is a large brightness step, not a recoloured 5px diamond."""
    return f'''{_HEAD}
<defs>
 <linearGradient id="lit" x1="0" y1="0" x2="1" y2="1">
  <stop offset="0" stop-color="{GLOW_A}"/><stop offset="1" stop-color="{GLOW_B}"/></linearGradient>
</defs>
<path d="{_face_d()}" fill="url(#lit)"/>
<path d="{_face_d()}" fill="none" stroke="{GLOW_B}" stroke-opacity="0.5" stroke-width="5"/>
<path d="{_diamond_d()}" fill="{LEGEND_LIT}"/>
</svg>'''


def legend_only(scale=1.0, color=LEGEND):
    return f'{_HEAD}<path d="{_diamond_d(scale)}" fill="{color}"/></svg>'


def render(svg, size=S):
    return Image.open(io.BytesIO(cairosvg.svg2png(
        bytestring=svg.encode(), output_width=size, output_height=size))).convert("RGBA")
