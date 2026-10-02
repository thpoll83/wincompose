# WinCompose 0.9.19 — Dark theme 🌓

WinCompose follows your Windows theme now, title bars included, and the tray
icon shows that it is composing by moving rather than by going dark. The About
tab also gained a button to check for updates.

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
- **You can ask for an update check.** The About tab has a button for it. Until
  now the only trigger was restarting WinCompose: the check runs at startup and
  then sleeps a random 30 to 90 minutes, and even switching the automatic check
  back on did nothing until that sleep expired. It reports underneath what it
  found — including when it could not reach the server, rather than telling you
  that you are up to date off a check that never happened.
- **The About tab's links say where they go.** "Visit Website" is now "WinCompose
  on www.polykybd.org", and donations and the original project sit under their
  own "Original Project" heading. The issue tracker and the money go to different
  places and the buttons were not saying so.
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

The update checker was tightened while the button was being added to it, since a
button is what makes its failures visible: the manual-check flag is consumed
atomically, a throwing event subscriber can no longer silence the one that
reports completion, and a query that never reached the server no longer answers
"up to date".
