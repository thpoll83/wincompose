# WinCompose 0.9.17 — Missing emoji, restored 🏳️

Every flag, skin tone and hair variant has been unreachable since the emoji rules
shipped. They compose now — along with a lighter compose tree, a safer updater,
and the app's first memory reporting.

## Fixed

- **Emoji whose name contains a colon now compose.** `Compose f l a g : d e` and
  the 257 others like it never worked. The rule parser matched the line but stored
  the sequence *truncated at the colon*, so all 260 colon rules in `Emoji.txt`
  collapsed onto two useless entries — every flag, skin tone and hair variant was
  lost. **1192 emoji sequences now load where 934 did.**
- **Reloading rules no longer swallows a compose sequence.** Toggling a rule set in
  Settings, or opening the sequence window, emptied the compose tree and refilled it
  in place; anything composed during that window silently did nothing and looked
  like a key that had not registered. The rules are now built aside and swapped in,
  so a lookup sees the whole old set or the whole new one.
- **The updater can no longer offer you an older version.** Its comparison never
  stopped at the first differing component, so a running 0.10.0 would accept 0.9.20
  as an "update". Dormant today, and live the moment the minor version passes the
  patch number.

## Changed

- **The compose tree is about a third smaller** — roughly 6.7 MB down to 4.6 MB —
  by sharing key instances and allocating node storage only where it is used. It
  now holds ~2,000 more nodes than before, since the restored emoji sequences are
  long ones.
- **Memory reporting.** The log records working set, private bytes and managed heap
  at startup, at shutdown and every five minutes. If WinCompose feels heavy, that is
  the line worth sending — the managed-heap figure separates WinCompose's own
  objects from WPF's rendering, which is most of what a WPF process costs.
- **The sequence window releases its data when hidden** instead of holding a view
  model per sequence for the rest of the session.

## Internal

The unit tests now build and run in CI — they had never been compiled. They caught
the emoji bug above on their first run.

## Downloads

Two builds, both below:

- **`WinCompose-Setup-0.9.17.exe`** — the installer; registers WinCompose to start
  with Windows. This is what PolyKybdHost's tray entry downloads.
- **`WinCompose-NoInstall-0.9.17.zip`** — portable; runs from any folder.

⚠️ These builds are **not code-signed yet**, so Windows SmartScreen will warn that
the publisher is unrecognised — choose **More info → Run anyway** if you are happy
to proceed. Signing is planned and waiting on a registered business entity, since
the certificate has to be issued to one.

Because they are unsigned, `SHA256SUMS.txt` ships alongside and lists the SHA-256 of
each asset, produced by the same CI run that built them:

```powershell
Get-FileHash .\WinCompose-NoInstall-0.9.17.zip -Algorithm SHA256
```

Compare against the matching line. That proves the file is byte-for-byte what our CI
produced — it does not prove who produced it, which is what a signature is for.

## Licence

WinCompose is © Sam Hocevar and contributors, released under the
[WTFPL](http://www.wtfpl.net/). This fork keeps that licence.
