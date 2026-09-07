# WinCompose 0.9.18 — New icon, 41 MB lighter 🔑

A tray icon that reads as this fork at the size the notification area actually
uses, a memory footprint a quarter of what it was while it sits there, and 29
more languages with a complete interface.

## Changed

- **A new icon, drawn as a PolyKybd display key.** The old rim took 34 of its 256
  pixels and left about five for the legend; idle and composing differed only by
  the hue of that five-pixel diamond, which says nothing at 16px and nothing at
  all to a red-green colour-blind reader. The cap is now dark with a lit legend,
  and composing lights the whole face and knocks the legend dark out of it — a
  large-area brightness step that survives greyscale. Every state was scored as
  mean per-pixel distance at 16×16 rather than judged by eye; the table is in
  `art/README.md`.
- **The icon now ships every size Windows asks for** — 16/20/24/32/40/48/64/256,
  each rendered natively from the vector instead of scaled from whichever frame
  was nearest. 20 and 24 are the small icon at 125% and 150% display scaling, so
  this is what most high-DPI desktops were missing. The window icon went to 128px
  for the same reason.
- **About 41 MB less memory while WinCompose sits in the tray** — 54 MB of managed
  heap down to 13 MB. Four emoji icons in the tray menu were making the app load
  its whole emoji rendering engine at startup, which parses the emoji font and
  never releases it. They are pre-rendered images now. Opening the sequence or
  settings window still loads it, since drawing arbitrary emoji is what those
  windows are for — so the saving is for the case where WinCompose just runs.
- **29 more languages have a complete interface.** 39 of the 47 shipped locales
  are now fully translated, where 10 were. Human translations from the upstream
  Weblate project always win; machine translations only fill in what is missing,
  and correcting one upstream still replaces it everywhere.
- **"Open Log File Location" in About.** The log lives in `%LOCALAPPDATA%`, which
  is not a path anyone should have to type when something has gone wrong.

## Fixed

- **The Belarusian and Finnish "reset delay" labels ignored your setting.**
  Belarusian read "10 секунд" with the number written into the text, so it said
  ten seconds whatever the delay was set to; Finnish had `[0}` where `{0}` was
  meant, which is not a placeholder at all, so it printed literally. Both are
  corrected, and the build now checks every language for the same defect.

## Internal

WinCompose can now report its own memory honestly: readings bracket the two big
loads, one is taken once WPF is idle, and a `-memprofile` switch samples every 30
seconds with a real collection. That is what found the 41 MB above, and it settled
a long-standing question — **WinCompose does not leak.** Measured on three
machines, live heap is flat from the first second and stays flat for hours; the
large numbers Task Manager shows are committed memory the garbage collector has
simply not been asked to give back.
