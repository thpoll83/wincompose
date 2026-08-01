# WinCompose 0.9.16 — the first PolyKybd build

The first release of the PolyKybd fork of WinCompose.

WinCompose gives Windows a compose key: press it, then a short sequence, and you
get a character your keyboard has no key for — `Compose` `'` `e` → é,
`Compose` `<` `3` → ❤. PolyKybd uses it for unicode and emoji output on Windows;
PolyKybdHost detects a running WinCompose and switches the keyboard into compose
mode automatically.

WinCompose is **Sam Hocevar's** project, and essentially all of the code, the
compose-sequence data and the design are his work. This fork exists to keep a
dependency healthy while the original is inactive, and builds on
[ell1010's fork](https://github.com/ell1010/wincompose), which had already
modernised the UI and retargeted to .NET Framework 4.8.1. If the original becomes
active again, these changes are meant to go back upstream.

## Fixed

- **The tray tooltip never updated.** The change notification was raised under
  the wrong property name, so the tooltip silently stayed empty.
- **The tray icon stopped reflecting state.** The composing indicator and the
  update marker had been dropped from the icon update path, and the "hide icon"
  setting did nothing at all. The icon is now also only reassigned when its state
  actually changes, instead of on every update.
- **The de-CH and it-CH catalogues** listed the wrong languages.
- **The update check** no longer polls the original project's server over plain
  HTTP for versions that do not correspond to this fork's builds — it reads our
  own status file.

## Changed

- **Settings dialog styling.** Text and controls were sized well above native
  Windows dialogs, tabs wrapped onto a second row, and the window could not be
  resized or scrolled. Tabs are now sized to their text, content scrolls, and the
  window resizes.
- **The UI is consolidated into the options dialog**, with Authors folded into
  About and a plain tray menu. About is localised, and contributors and the
  licence render as text rather than in an embedded browser.
- Unknown key names log at Info rather than Warn.

## Downloads

Two builds, both below:

- **`WinCompose-Setup-0.9.16.exe`** — the installer; registers WinCompose to
  start with Windows. This is what PolyKybdHost's tray entry downloads.
- **`WinCompose-NoInstall-0.9.16.zip`** — portable; runs from any folder.

⚠️ These builds are **not code-signed yet**, so Windows SmartScreen will warn that
the publisher is unrecognised — choose **More info → Run anyway** if you are happy
to proceed. Signing is planned and waiting on a registered business entity, since
the certificate has to be issued to one.

Because they are unsigned, `SHA256SUMS.txt` ships alongside and lists the SHA-256
of each asset, produced by the same CI run that built them:

```powershell
Get-FileHash .\WinCompose-NoInstall-0.9.16.zip -Algorithm SHA256
```

Compare against the matching line. That proves the file is byte-for-byte what our
CI produced — it does not prove who produced it, which is what a signature is for.

## Licence

WinCompose is © Sam Hocevar and contributors, released under the
[WTFPL](http://www.wtfpl.net/). This fork keeps that licence.
