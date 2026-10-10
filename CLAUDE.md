# CLAUDE.md: wincompose (PolyKybd fork of WinCompose)

A compose-key tray app for Windows. PolyKybdHost prefers `InputMethod.WinCompose` over
the native Windows path, so this app is what gives the keyboard real Unicode and emoji
output there. Fork chain: samhocevar → ell1010 → thpoll83; only `thpoll83/wincompose` can
be pushed. Default branch `main`.

**Rules for all PolyKybd repos** (review, branching, releases, web session limits) are
in `../polykybd-claude/CLAUDE.md`, with the shared skills (`polykybd-github-release`,
`session-retro`, …). If that repo is not attached, ask the user to attach
`thpoll83/polykybd-claude`.

| topic | doc |
|---|---|
| build, CI artifacts, reviewers, the WPF-UI source trick | [`docs/build-and-review.md`](docs/build-and-review.md) |
| memory measurements, Emoji.Wpf cost | [`docs/memory.md`](docs/memory.md) |
| WPF-UI traps, themes, the animated tray icon | [`docs/wpf-ui.md`](docs/wpf-ui.md), [`art/README.md`](art/README.md) |
| translations and the machine fallback | [`docs/translations.md`](docs/translations.md) |
| releases, and the PK-0.9.19 post-mortem | [`RELEASE.md`](RELEASE.md) |

## Environment, build and review

- **No .NET toolchain in the container; CI is the only compiler.** A push that cannot
  build costs a full round, so read the diff adversarially first.
- ⚠️ **With nothing to compile or render, a claim about WPF-UI is only as good as the
  source you read.** Both wrong diagnoses in #19 came from reasoning about the library.
  Read the file: `curl -sS https://raw.githubusercontent.com/lepoco/wpfui/3.0.5/src/Wpf.Ui/<path>`
  works, while `api.github.com` returns 403 (no tree listing, so a path is a guess until
  it returns 200).
- **`build.yml` builds, runs the 9 MSTest tests and uploads `wincompose-portable` and
  `wincompose-release-assets`.** A PR is testable from that artifact without a release.
  It runs on `push` and `pull_request`, so `mergeable_state: unstable` while the second
  run is going means pending, not failing.
- ⚠️ **A portable build beside an installed copy gives two keyboard hooks.** There is no
  single-instance guard. Quit the installed one first.
- ⚠️ **An XML comment cannot contain `--`**, and the C# here uses `--` as a dash. A XAML
  comment in that style fails with `MC3000`. Check before pushing:
  `python3 -c "import xml.dom.minidom,io,sys; xml.dom.minidom.parseString(io.open(sys.argv[1],encoding='utf-8-sig').read().encode())" <file>.xaml`
- **Expect no bot review here.** CodeRabbit must be asked by hand (and posts a green
  "Review skipped" status otherwise); Greptile and Sourcery are out of credit, and
  Sourcery still posts a convincing Reviewer's Guide with zero findings and wrong paths.
  Say in the PR body that a green board means it compiles.

## Rules that bind code across the app

- **Memory: nothing leaks.** Only live managed heap is evidence. Private bytes vary
  165–233 MB between machines for the same build. `GC.GetTotalMemory(false)` counts
  garbage, so it cannot measure a release. Ask users for `-memprofile` and a quit from
  the tray.
- ⚠️ **Nothing on the startup path may touch an Emoji.Wpf type.** Its first use costs a
  fixed ~41 MB that is never released. That is why the tray menu icons are PNGs
  (`res/menu/`, from `src/render-menu-icons.py`).
- ⚠️ **A WPF implicit style matches the exact type.** `BaseWindow` gets its style through
  `SetResourceReference(StyleProperty, typeof(BaseWindow))`; keep that line. A window left
  unstyled gets white `SystemColors.WindowBrush` in both themes.
- ⚠️ **Changing a window's base class is a sweep over five windows, and the C# names only
  three.** `SettingsWindow` and `SequenceWindow` take theirs from the XAML root. Grep
  both the `.cs` and the `.xaml`. `RemoteControl` stays a plain `Window` on purpose.
- ⚠️ **`FluentWindow` takes over the caption whatever `ExtendsContentIntoTitleBar` says**,
  and `WindowBackgroundManager.UpdateBackground` clears the caption colour. With the native
  caption, use only `ApplyDarkThemeToWindow` / `RemoveDarkThemeFromWindow`, from
  `OnSourceInitialized`.
- **A hardcoded value in a WPF-UI template can only be changed by copying the template**
  (four copies in `StyleOverrides.xaml`). `BasedOn` does not bring the triggers along.
- ⚠️ **React to a theme change via `ApplicationThemeManager.Changed`, never the
  `theme_mode` setting**, which stays `System` when Windows flips. Empty `theme_mode`
  means System. Check `wincompose.log` for `Theme: System resolved to …` before
  theorising.
- ⚠️ **Grep the XAML for literal colours when the theme or icon changes.** A scan of the
  images cannot see them.
- **The tray animation**: `TaskbarIcon.Icon` is one cheap `NIM_MODIFY`. Cache every frame
  (`Icon.FromHandle` never frees the HICON) and drive frames from a `Stopwatch`.
- ⚠️ **Editing a `.resx` by hand does not survive** `src/update-data.sh`. A machine
  translation never overrides a human one, and a `CORRECTIONS` entry must name a defect
  the build can see.

## Releases

Tags are `PK-<version>` (`GitVersion.yml`); never `vX.Y.Z`. [`RELEASE.md`](RELEASE.md) is
the authority.

- ⚠️ **The shipped version is hardcoded in `src/wincompose/wincompose.csproj`**
  (`<AssemblyVersion>`/`<FileVersion>`), and the installer name, zip name, About tab and
  updater all read it. `publish_release.py` therefore tags the commit whose csproj
  declares the version, and refuses when none does, which means the bump has not merged.
- ⚠️ **An existing tag beats the pin.** A release never moves a tag, so a leftover tag on
  a pre-bump commit builds the wrong tree while the script reports the right commit. Move
  the tag; don't dispatch.
- **`status.txt` is written by CI** (the `status` job in `release.yml`) from the shipped
  binary. Never edit it by hand.
- **No session here can delete a remote branch or publish a release.** Stage the notes
  and hand over `python scripts/publish_release.py`.
