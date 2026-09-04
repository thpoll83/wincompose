WinCompose
==========

A compose key for Windows, free and open-source, created by Sam Hocevar.

A **compose key** allows to easily write special characters such as **é
ž à ō û ø ☺ ¤ ∅ « ♯ ⸘ Ⓚ ㊷ ♪ ♬** using short and often
very intuitive key combinations. For instance, **ö** is obtained using
<kbd>o</kbd> + <kbd>"</kbd>, and **♥** is obtained using <kbd>&lt;</kbd>
\+ <kbd>3</kbd>.

WinCompose also supports Emoji input for 😁 👻 👍 💩 🎁 🌹 🐊.

This is the **PolyKybd fork**. WinCompose is Sam Hocevar's project — essentially
all of the code, the compose-sequence data and the design are his work — and this
fork builds on [ell1010's fork](https://github.com/ell1010/wincompose), which
modernised the UI and retargeted to .NET Framework 4.8.1. It exists because
[PolyKybd](https://github.com/thpoll83/PolyKybd) uses WinCompose for unicode and
emoji output on Windows and needed a handful of fixes. It does not claim to
succeed either repository; both are still there, and these changes are meant to
go back upstream.

Download
--------

 * **[Latest release](https://github.com/thpoll83/wincompose/releases/latest)** —
   `WinCompose-Setup-*.exe` is the installer and registers WinCompose to start with
   Windows; `WinCompose-NoInstall-*.zip` is portable and runs from any folder.

 * [All releases of this fork](https://github.com/thpoll83/wincompose/releases/).

 * Older versions are in [ell1010's releases](https://github.com/ell1010/wincompose/releases/) and in [Sam Hocevar's original releases](https://github.com/samhocevar/wincompose/releases/).

**Note: these builds are not code-signed**, so Windows SmartScreen warns that the
publisher is unrecognised — choose **More info → Run anyway** if you are happy to
proceed. Signing is planned and waits on a registered business entity, since the
certificate has to be issued to one.

Because they are unsigned, every release ships a `SHA256SUMS.txt` listing the
SHA-256 of each asset, written by the same CI run that built them:

```powershell
Get-FileHash .\WinCompose-NoInstall-<version>.zip -Algorithm SHA256
```

Compare that against the matching line. It proves the file is byte-for-byte what
our CI produced — it does not prove who produced it, which is what a signature is
for.

Quick start
-----------

After installation, WinCompose should appear in the System Tray. Press and
release the <kbd>⎄ Compose</kbd> key to initiate a compose sequence (this key
defaults to <kbd>Right Alt</kbd>); the icon should change to indicate a compose
sequence is in progress.

Then type in the keys for a compose sequence, such as <kbd>A</kbd> then
<kbd>E</kbd> for **Æ**:

![Quick Launch](/web/shot1.png)

If <kbd>Right Alt</kbd> is not suitable for you, you can change it in the options.

Examples
--------

Compose rules are supposed to be intuitive. Here are some examples:

 - <kbd>⎄ Compose</kbd> <kbd>\`</kbd> <kbd>a</kbd> → **à**
 - <kbd>⎄ Compose</kbd> <kbd>'</kbd> <kbd>e</kbd> → **é**
 - <kbd>⎄ Compose</kbd> <kbd>^</kbd> <kbd>i</kbd> → **î**
 - <kbd>⎄ Compose</kbd> <kbd>~</kbd> <kbd>n</kbd> → **ñ**
 - <kbd>⎄ Compose</kbd> <kbd>/</kbd> <kbd>o</kbd> → **ø**
 - <kbd>⎄ Compose</kbd> <kbd>"</kbd> <kbd>u</kbd> → **ü**
 - <kbd>⎄ Compose</kbd> <kbd>o</kbd> <kbd>c</kbd> → **©**
 - <kbd>⎄ Compose</kbd> <kbd>+</kbd> <kbd>-</kbd> → **±**
 - <kbd>⎄ Compose</kbd> <kbd>:</kbd> <kbd>-</kbd> → **÷**
 - <kbd>⎄ Compose</kbd> <kbd>(</kbd> <kbd>7</kbd> <kbd>)</kbd> → **⑦**
 - <kbd>⎄ Compose</kbd> <kbd>C</kbd> <kbd>C</kbd> <kbd>C</kbd> <kbd>P</kbd> → **☭**
 - <kbd>⎄ Compose</kbd> <kbd>&lt;</kbd> <kbd>3</kbd> → **♥**

Emoji sequences typically start with two <kbd>⎄ Compose</kbd> hits:

 - <kbd>⎄ Compose</kbd> <kbd>⎄ Compose</kbd> <kbd>a</kbd> <kbd>n</kbd> <kbd>g</kbd> <kbd>r</kbd> <kbd>y</kbd> → 😠
 - <kbd>⎄ Compose</kbd> <kbd>⎄ Compose</kbd> <kbd>g</kbd> <kbd>r</kbd> <kbd>i</kbd> <kbd>n</kbd> <kbd>n</kbd> <kbd>i</kbd> <kbd>n</kbd> <kbd>g</kbd> → 😁
 - <kbd>⎄ Compose</kbd> <kbd>⎄ Compose</kbd> <kbd>s</kbd> <kbd>u</kbd> <kbd>s</kbd> <kbd>h</kbd> <kbd>i</kbd> → 🍣
 - <kbd>⎄ Compose</kbd> <kbd>⎄ Compose</kbd> <kbd>s</kbd> <kbd>n</kbd> <kbd>a</kbd> <kbd>k</kbd> <kbd>e</kbd> → 🐍

A special Unicode input mode can be activated in the options and lets
the user type in any Unicode character:

 - <kbd>⎄ Compose</kbd> <kbd>u</kbd> <kbd>5</kbd> <kbd>8</kbd> <kbd>d</kbd> <kbd>Enter</kbd> → ֍ (U+058D Right-Facing Armenian Eternity Sign)
 - <kbd>⎄ Compose</kbd> <kbd>u</kbd> <kbd>2</kbd> <kbd>3</kbd> <kbd>f</kbd> <kbd>0</kbd> <kbd>Enter</kbd> → ⏰ (U+23F0 Alarm Clock)

The full list of rules can be found by clicking on the WinCompose system tray
icon or using the “Show Sequences…” menu entry:

![Sequence List](/web/shot2.png)

The window allows you to filter the sequences being listed.

Features
--------

WinCompose supports the standard Compose file format. It provides more than
1700 compose rules from the [Xorg](http://www.x.org/wiki/) project and the
[dotXCompose](https://github.com/kragen/xcompose) project. You can add custom
rules by creating a file named `.XCompose` or `.XCompose.txt` in your
`%USERPROFILE%` folder. WinCompose must be restarted for changes to take
effect.

WinCompose stores its state in the `%APPDATA%\wincompose` folder: `settings.ini`
contains the settings, and `metadata.xml` contains all the metadata associated
with sequences.

WinCompose supports rules of more than 2 characters such as <kbd>⎄ Compose</kbd>
<kbd>(</kbd> <kbd>3</kbd> <kbd>)</kbd> for **③**.

WinCompose supports early exits. For instance, <kbd>⎄ Compose</kbd> <kbd>Q</kbd> will
immediately type **Q** because there is currently no rule starting with the capital
letter <kbd>Q</kbd>.

WinCompose carries translations for 47 languages, at varying completeness.
Translation happens upstream on the Weblate project, so work done there reaches
every fork; the badge is the live figure:

<a href="https://hosted.weblate.org/engage/wincompose/?utm_source=widget"><img src="https://hosted.weblate.org/widgets/wincompose/-/svg-badge.svg" alt="Translation status" /></a>

Development
-----------

Make sure that all Git submodules are fetched, then just open `src/wincompose.sln`
in Visual Studio in order to build WinCompose. You will also need to install
[Inno Setup](https://jrsoftware.org/isinfo.php) if you wish to build the installer.

`.github/workflows/build.yml` compiles the app on every push and pull request.
`release.yml` builds the installer, the portable zip and `SHA256SUMS.txt` and
attaches them when a release is published — see [`RELEASE.md`](RELEASE.md).

Bugs and Improvements
---------------------

Please report bugs or suggest improvements for **this fork** in its
[issue tracker](https://github.com/thpoll83/wincompose/issues).

For the upstream projects, use
[ell1010/wincompose](https://github.com/ell1010/wincompose/issues) or
[samhocevar/wincompose](https://github.com/samhocevar/wincompose/issues).
