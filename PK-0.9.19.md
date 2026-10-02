# WinCompose 0.9.19 — Dark theme 🌓

WinCompose follows your Windows theme now, title bars included, and the tray
icon shows that it is composing by moving rather than by going dark.

## Changed

- **It follows the Windows theme.** `System` is a real choice now, and it is what
  an untouched setting means. Before, an empty value quietly fell through to
  Light — so WinCompose stayed white on a machine running Windows in dark mode,
  by design, with nothing saying so and the theme box showing blank. An explicit
  Light or Dark still wins, and a theme switch made while it is running is picked
  up without a restart.
- **Windows draws the title bars.** They are the colour every other title bar on
  the machine is, they dim when a window goes inactive, and they follow dark mode
  and high contrast with nothing for us to match. A window opened in the dark
  theme used to get a light caption strip above a dark body; the debug window was
  the last one still doing it.
- **The composing state moves.** At rest the key carries a dark diamond; while a
  sequence is in progress one quarter of that diamond lights and walks clockwise,
  about a second a turn. The old pair made composing the *darker* state, which
  reads as "off" — and on a PolyKybd whose Caps Lock LED lights while composing,
  the two indicators pointed opposite ways. The app icon matches the resting tray
  icon, so the exe, the title bars and the Start menu all show the same cap. (#21)
- **The sequence list draws the new key.** Those keycaps are XAML rather than
  image files, so they had kept the old cream cap straight through the icon
  redraw. Every colour now comes from the same drawing the icon itself is
  rasterised from, so the two cannot drift apart again.
- **Settings tabs and the tray menu, tidied.** The selected tab sits on a recessed
  strip instead of being marked by a 1px frame that was the only thing
  distinguishing it from the window behind it, and the tray menu rows are tighter
  than the application-menu metrics they inherited.

## Internal

The animation is four pre-rendered quarter decals over the resting legend, driven
off a clock rather than a frame counter, with every frame cached — a tray icon is
changed with one `Shell_NotifyIcon` call, so animating one is cheap, which our own
source comment had claimed otherwise. `art/build_preview.py` renders the tray
states, animation included, as files: the tray icon exists nowhere as one
otherwise, so there was nothing to point at when someone asked what a state looks
like.
