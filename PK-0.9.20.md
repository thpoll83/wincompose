# WinCompose 0.9.20 — the version on the tin 🏷️

**These downloads were re-uploaded on 7 October.** 0.9.20 was first published
with its assets named `0.9.19`, for the same reason 0.9.19's were named
`0.9.18`: the shipped version is baked into the build, the release is tagged
independently, and both releases were published before their version bump had
merged. The files here now are built from the bumped tree, so they are named
`0.9.20`, report 0.9.20 in the About tab, and are what the update check
compares against.

**If you downloaded anything before that**, you have a working build that
reports the wrong version — `0.9.19` from the first 0.9.20 upload, or `0.9.18`
from 0.9.19. Nothing is wrong with it beyond the number, and this update is
offered to you normally. Re-download if you would rather the About tab agreed
with the release it came from.

Everything below shipped across 0.9.19 and 0.9.20. If you are coming from
0.9.18, all of it is new to you.

## Changed

- **It follows the Windows theme.** `System` is a real choice, and it is what an
  untouched setting means. An empty value used to fall through to Light, so
  WinCompose stayed white on a machine running Windows in dark mode, with nothing
  saying so and the theme box showing blank. A theme switch made while it is
  running is picked up without a restart.
- **Windows draws the title bars.** They are the colour every other title bar on
  the machine is, they dim when a window goes inactive, and they follow dark mode
  and high contrast on their own. The debug window was the last one still drawing
  a light caption strip above a dark body.
- **The composing state moves.** At rest the key carries a dark diamond; while a
  sequence is in progress one quarter of it lights and walks clockwise, about a
  second a turn. The old pair made composing the *darker* state, which reads as
  "off" — and on a PolyKybd whose Caps Lock LED lights while composing, the two
  indicators pointed opposite ways. (#21)
- **The sequence list draws the new key.** Those keycaps are drawn in XAML rather
  than loaded from image files, so they had kept the old cream cap straight
  through the icon redraw.
- **You can ask for an update check.** The About tab has a button for it. Until
  now the only trigger was restarting WinCompose: the check runs at startup and
  then sleeps a random 30 to 90 minutes, and switching the automatic check back
  on did nothing until that sleep expired. It reports what it found underneath —
  including when it could not reach the server, rather than telling you that you
  are up to date off a check that never happened.
- **The About tab's links say where they go.** "Visit Website" is now "WinCompose
  on www.polykybd.org", and donations and the original project sit under their own
  heading. The issue tracker and the money go to different places and the buttons
  were not saying so.
- **Settings tabs and the tray menu, tidied.** The selected tab sits on a recessed
  strip instead of being marked by a 1px frame, and the tray menu rows are tighter
  than the application-menu metrics they inherited.

The first four and the last shipped in 0.9.19's notes and in its build. The two
About-tab entries shipped in its notes only — they merged in the window between
the release going out and its bump landing.

## Fixed

- **A release can announce itself again.** The version a build reports is what
  the update check compares against the update file, so a build labelled with
  the previous version cannot be announced at all — announcing it would offer
  every install an endless update to itself. That is why 0.9.19 sat unannounced:
  not an oversight, an impossibility. Both halves are fixed here, and the
  announcement is written by CI from the binary it just shipped rather than by
  hand.

## Internal

Three gates, because the shipped version is hardcoded in one file while the tag
is chosen from somewhere else entirely. `publish_release.py` now refuses to
publish a tag the default branch has not been bumped to — its old note fired on
exactly this and read as benign. `release.yml` asserts the built exe's version
equals the tag before attaching anything to a release, since a hand-pushed tag
and a recovery dispatch never go through that script. And `status.txt` is written
by CI after the assets are attached, from the version resource of the binary it
just shipped and never backwards, so it cannot name a release that does not exist
or a version other than the one in the download.
