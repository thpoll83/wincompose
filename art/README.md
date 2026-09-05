Icon artwork
============

`icons.py` draws the WinCompose keycap as SVG; `build_icons.py` rasterises it
into the thirteen files the app and the installer actually load.  Nothing here
ships — it is the source the committed PNGs and ICOs are generated from, which
until now existed only as `web/icon.xcf` (a GIMP file of the old cream cap, now
superseded).

```sh
pip install pillow cairosvg
python art/build_icons.py            # regenerate the assets
python art/build_icons.py --check    # list what has drifted, write nothing
```

`glyph_sequences.png` and `glyph_settings.png` are alpha masks of the **Aβ¿Δ**
and gear legends, extracted from upstream's artwork so the two window icons keep
their original lettering on the new cap.

What the design is doing
------------------------

This fork's cap is dark with a lit legend, where upstream's is cream.  That is
the fork mark: PolyKybd's keycaps are per-key OLED displays, and unlike a corner
badge a whole-cap change is still legible at the 16×16 the notification area
actually uses.

Three things were measured at 16px rather than judged at 256px, and each is a
constraint on any future edit:

  * **The rim is 16 of 256, where upstream's was 34.**  A rim costs the same
    fraction of the icon at every size, and at 16px upstream's left about five
    pixels for the legend.

  * **Idle and composing differ over a large area, not in hue.**  Composing
    lights the whole face cyan-to-blue; the legend stays near-white in both
    states.  Upstream swapped a small dark diamond for a green one, which is a
    weak signal at 16px and no signal at all to a red-green colour-blind
    reader.  The wash is not the green an "active" state usually gets because
    the pairs were measured, as mean per-pixel RGB distance between the idle
    and composing renders at 16px.  As a soft radial glow, cyan scored 49
    against 32 for green — cyan being far brighter than a mid-green.  The
    diagonal fill that replaced it is opaque edge to edge rather than fading
    out, and that, more than the direction, is what takes the figure to 150.
    Colouring the CAP blue instead scored worst of all (27): it reads as
    already lit, and leaves the lit state nowhere to go.

  * **The cap stays dark while composing**, so the fork is recognisable in every
    state rather than only at rest.

The update marker (`decal_update.png`) and the unreferenced `decal_disabled.png`
are upstream's and are deliberately not generated here.

Two mechanical traps
--------------------

  * **DPI is load-bearing.**  `NotificationIcon.GetIcon` composes the tray icon
    at runtime with `Graphics.DrawImage`, which scales by the ratio of the two
    bitmaps' DPI — so `key_empty.png` and every decal must stay at the 72 DPI
    upstream used, or the decal lands at the wrong size.  The window icons are
    300 DPI because WPF sizes a `BitmapImage` from it.  `build_icons.py` names
    the DPI per output.

  * **The `.ico` container is written by hand.**  Pillow's ICO writer
    downsamples every frame from one source image and encodes the whole file
    either as DIB or as PNG.  These files render each frame natively and use DIB
    for 16/32/48 with PNG only for 256 — matching upstream, and what the shell
    and Inno Setup handle without argument.
