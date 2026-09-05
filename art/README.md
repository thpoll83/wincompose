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

The key is lit blue where upstream's is a cream keycap.  That is the fork mark:
PolyKybd's keycaps are per-key OLED displays, and unlike a corner badge a
whole-cap change is still legible at the 16×16 the notification area actually
uses.  The key is lit in **both** states — only the legend changes, white at
rest and knocked out dark while composing, the way a real keycap's legend would
invert.

Three things were measured at 16px rather than judged at 256px, and each is a
constraint on any future edit:

  * **The rim is 16 of 256, where upstream's was 34.**  A rim costs the same
    fraction of the icon at every size, and at 16px upstream's left about five
    pixels for the legend.

  * **The state pair was chosen from four candidates**, scored as mean
    per-pixel RGB distance between the idle and composing renders at 16px:

    | states | score |
    |---|---|
    | dark key ⇄ lit key (either direction) | 150 |
    | lit key, legend lights only | 173 |
    | **lit key, legend inverts** | **48** |
    | bare lit key ⇄ diamond appears | 28 |

    Inverting the legend is not the strongest signal, and was picked anyway:
    it keeps one identity in the tray at all times, and the difference is
    black-versus-white rather than a hue swap, so it survives greyscale and
    colour-blindness.  Upstream's states differed only in the hue of a
    five-pixel diamond, which does neither.  If the state ever needs to shout
    louder, the 150 and 173 rows are where to go.

  * **Which ink a legend takes is measured, not chosen.**  The face runs dark
    blue at the top-left to light cyan at the bottom-right, so the two inks are
    not interchangeable across it — the contrast table is in `icons.py`.  The
    resting diamond is light (it sits centred, and is a solid shape rather than
    text); the Aβ¿Δ and gear window legends are dark, because their thin
    strokes reach the light corner where a light ink measures 1.55:1 and the
    gear's lower teeth disappeared.

    Those two legends are **raised** rather than flat: their ink runs dark navy
    to a lighter slate along the same diagonal, with a lit sliver on their
    top-left edges and a shadow on their bottom-right.  A conventional shine —
    a bright band across the glyph — was tried first and does not work here,
    because a light area anywhere on the face's cyan half measures 1.2–1.5:1.
    The lift on its own also costs ink at small sizes; the emboss shadow is
    what pays it back, which is why they arrived together.  Measured as how
    much ink survives the downscale to 16px, the raised legends land at 41.1
    and 31.9 against 41.5 and 33.6 for the flat navy they replace — parity,
    which is the constraint any future tweak here has to meet.

The update marker (`decal_update.png`) and the unreferenced `decal_disabled.png`
are upstream's and are deliberately not generated here.

Two mechanical traps
--------------------

  * **DPI is load-bearing, in two different ways.**  `NotificationIcon.GetIcon`
    composes the tray icon at runtime with `Graphics.DrawImage`, which scales by
    the ratio of the two bitmaps' DPI — so `key_empty.png` and every decal must
    stay at the 72 DPI upstream used, or the decal lands at the wrong size.
    Separately, WPF sizes a `BitmapImage` as `pixels * 96 / DPI`, so for
    anything it renders the DPI *is* the size: the window icons are 300 DPI, and
    `key_compose.png` is 128 px at 384 DPI, which is 32 DIP — the same size the
    32 px original was, with pixels left over at 150% and 200% display scaling.
    Shipping it at 96 DPI would make it a 128 DIP icon and blow the AboutBox
    title bar apart.  `build_icons.py` names the DPI per output.

  * **The `.ico` container is written by hand.**  Pillow's ICO writer
    downsamples every frame from one source image and encodes the whole file
    either as DIB or as PNG.  These files render each frame natively and use DIB
    up to 64 with PNG only for 256, which is what the shell and Inno Setup
    handle without argument.  They carry every size the shell asks for —
    16/20/24/32/40/48/64/256 — not upstream's four: the shell derives a missing
    size by scaling whichever frame it has, so 20 and 24 (the small icon at 125%
    and 150% scaling) and 40 and 64 (large icons) used to come from 16 or 32.

Not covered here
----------------

The tray icon is **not** a multi-size icon, and no amount of resolution fixes
that.  `GetIcon` calls `Bitmap.GetHicon()` on the 256x256 composite, which
yields a single 256x256 `HICON`; the shell then scales it to 16-32 px with no
correctly-sized frame to choose.  Drawing the PNGs larger would only widen that
reduction.  If the tray reads soft on real hardware, the fix is to compose at
the system small-icon size in `NotificationIcon.xaml.cs` — a C# change, and one
worth confirming against the real shell first, since a LANCZOS preview of the
256 px art is kinder than what Windows does with it.
