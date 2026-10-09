# CLAUDE.md — wincompose

This file provides guidance to Claude Code (claude.ai/code) when working in the
**wincompose** repo: the PolyKybd fork of WinCompose, a compose-key tray app for
Windows.

PolyKybd depends on it. `PolyKybdHost/polyhost/input/unicode_input.py` prefers
`InputMethod.WinCompose` over the far more limited native Windows path, so this
app is what gives the keyboard real Unicode and emoji output there — which is why
the host has an "Install WinCompose…" tray entry at all.

It is a **fork of a fork**: samhocevar → ell1010 → thpoll83. Both upstreams are
read-only from a session here; only `thpoll83/wincompose` can be pushed to.

The **code-review conventions** and **branching rules** in
[`../PolyKybdHost/CLAUDE.md`](../PolyKybdHost/CLAUDE.md) apply here too — in
particular: start each piece of work on a fresh branch cut from the updated
default (**`main`**), never keep committing to a branch whose PR has merged, and
verify an AI reviewer's finding against the code before acting on it.

## Reviewers: expect none

All three bots are effectively unavailable on this repo, and each goes quiet in
its own way. Measured repeatedly (2026-09-04, 2026-09-07):

- **CodeRabbit does not auto-review a repo under 10 stars.** It renders a
  *"Review available on request"* box with a Trigger-review checkbox, and posts a
  commit status of `state: success` with the description *"Review skipped: manual
  review required"* — a **green tick over a review that did not happen**. Ask for
  one with `@coderabbitai review`.
- **Greptile has spent its 50-credit trial** on this account, and says so in a
  review object.
- **Sourcery is out of its 250,000-diff-character weekly budget** and refuses in a
  `COMMENTED` review. ⚠️ It still posts a full **Reviewer's Guide** with mermaid
  diagrams and a file table — description only, zero findings, and the most
  convincing-looking artifact of the three.

So a green board here means **it compiles**. Say so in the PR body rather than
letting quiet checks read as approval — every PR from this session does.

⚠️ **Sourcery's Reviewer's Guide gets paths wrong.** On #16 its file table listed
`src/wincompose/wincompose/res/menu/*.png`, doubling a directory. Check the tree
before chasing it.

## Build & CI

`.github/workflows/build.yml` builds, runs the tests, then packages the
**portable** layout and uploads two artifacts — `wincompose-portable` and
`wincompose-release-assets`. `release.yml` handles the installer.

- ⚠️ **A PR is testable without cutting a release.** Grab the artifact from the
  run: `https://github.com/thpoll83/wincompose/actions/runs/<run>/artifacts/<id>`,
  where the id comes from `actions_list list_workflow_run_artifacts`. This is
  worth knowing because the workflow's own header comment said the opposite for a
  while, and I repeated it twice before reading the steps (fixed in #15).
- ⚠️ **A portable build run beside an installed copy gives you two keyboard
  hooks.** The app has **no single-instance guard** — only the installer
  broadcasts `WM_WINCOMPOSE_EXIT` — so quit the installed one first.
- **Two runs per commit.** The workflow fires on both `push` and `pull_request`,
  so every commit produces two runs and the PR reads `mergeable_state: unstable`
  while the second is still going. That is *pending*, not failing.
- **There are 9 tests** (`tests/`, MSTest: `UpdaterTest`, `SettingsTest`,
  `sequences/Sequence`) and CI runs them. They had existed since 2016 without ever
  being compiled until 0.9.17, and caught a real bug on their first run.

⚠️ **An XML comment cannot contain `--`, and the C# in this repo uses `--` as an
ASCII em dash.** Writing a XAML comment in the house style is therefore a build
failure — `error MC3000` — and it cost a CI round on #16. One second to check:

```bash
python3 -c "import xml.dom.minidom,io,sys; xml.dom.minidom.parseString(
io.open(sys.argv[1],encoding='utf-8-sig').read().encode())" src/wincompose/ui/Foo.xaml
```

## Memory: measured, and the answer is "no leak"

"WinCompose uses 200 MB" was unanswerable until 0.9.18 — nothing reported its own
footprint. `MemoryReport` now does, and the picture is settled. **Measured on
three machines with figures agreeing to 0.1 MB:**

| | live managed heap |
|---|---|
| compose rules + Unicode metadata (ours) | **7.9 MB** |
| + WPF | ~12.8 MB |
| + Emoji.Wpf | **54.1 MB** |

- **Nothing leaks.** Live heap is flat from the first second and stays flat for
  hours. Twenty forced full collections over ten minutes reclaimed nothing, and
  private bytes moved 2.9 MB in 45 minutes.
- ⚠️ **Private bytes are not comparable between machines and mean very little.**
  The same build settled at 165, 226 and 233 MB on three machines while live heap
  was identical. Most of the spread is committed GC segments the collector has not
  been asked to give back — a forced collection drops it ~55 MB. **Only live heap
  is evidence.**
- ⚠️ **`GC.GetTotalMemory(false)` counts garbage, so it cannot measure a
  release.** Freeing an object graph moves nothing until something collects, so an
  uncollected reading after a release shows no drop and reads as *"the change did
  nothing"*. This cost two rounds. The periodic sample deliberately does not
  collect (it must not perturb the app); the startup and shutdown readings do.
- **`-memprofile`** takes twenty samples 30 s apart, each with a full collection,
  then falls back to the five-minute cadence. That is the switch to ask a user to
  run; ten minutes plus a quit from the tray answers almost any memory question.
- ⚠️ **Quit from the tray, not Task Manager** — the shutdown reading is the only
  one that collects at the end of a real session.

### Emoji.Wpf costs ~41 MB, once, forever

`Emoji.Wpf` 0.3.3 renders through `Typography.OpenFont`, which parses the emoji
font into managed structures and never releases them.

⚠️ **The cost is a fixed initialisation, not per-glyph.** Four 18px tray-menu
icons cost the same ~41 MB as opening a whole window of emoji — measured at +41.4
and +46.6 MB respectively. So the rule is **nothing on the startup path may touch
an emoji type**, not "use fewer emoji".

That is why `ui/NotificationIcon.xaml` draws its four menu icons from PNGs
(`res/menu/`, regenerated by `src/render-menu-icons.py`) while the sequence
window, the settings tabs and the debug window keep using Emoji.Wpf — there
rendering arbitrary emoji is the product, and the cost is only paid if the window
is opened. Startup live heap went 54.2 → 12.9 MB.

⚠️ **The saving is "has not opened a window this session", not "is not using one
now".** The window is cached and the font parse is static, so closing it returns
nothing.

⚠️ Which font Emoji.Wpf draws from is **unverified** — it is a NuGet package whose
source is not in this repo. "Segoe UI Emoji" is an inference from its defaults,
repeated three times in one session before anyone checked. `render-menu-icons.py`
takes a font path as its first argument, so `C:\Windows\Fonts\seguiemj.ttf`
reproduces whatever Windows draws if the question ever matters.

## WPF-UI: the traps that cost rounds

The UI is [WPF-UI](https://github.com/lepoco/wpfui) 3.0.5 (`WPF-UI` NuGet), merged
in `Application.xaml` as `ThemesDictionary` + `ControlsDictionary` + our own
`StyleOverrides.xaml`. Four things about it are not guessable from the API, and
each was found the slow way.

⚠️ **A WPF implicit style matches the EXACT type, and this bites TWICE over.**
`StyleOverrides.xaml` already records it for `emoji:TextBlock`, which is its own type and so missed
the 12px `TextBlock` setter. The window version is worse in both directions:

- WPF-UI's window styles are keyed `{x:Type FluentWindow}` / `{x:Type Window}`. A
  subclass gets one only because `FluentWindow` overrides
  `DefaultStyleKeyProperty` to `typeof(FluentWindow)` — that indirection, not the
  implicit lookup, is what carries the style down to `BaseWindow` and to
  `SettingsWindow` under it.
- A style **we** write keyed `{x:Type BaseWindow}` reaches nothing, because no
  window IS a `BaseWindow` — they are `SettingsWindow`, `SequenceWindow`,
  `AboutBox` and `KeySelector`. It fails silently: the window simply keeps the WPF
  default `SystemColors.WindowBrush`, which is white in BOTH themes, so in light
  it looks perfect and in dark it is white-on-white with only the colour emoji
  legible. `BaseWindow` asks for its style by name with
  `SetResourceReference(StyleProperty, typeof(BaseWindow))`; keep that line.

⚠️ **`FluentWindow` takes the caption over whatever `ExtendsContentIntoTitleBar`
says.** Its `OnSourceInitialized` calls
`OnExtendsContentIntoTitleBarChanged(default, ExtendsContentIntoTitleBar)`
unconditionally, and that handler ignores both values: it forces
`WindowStyle.SingleBorderWindow`, applies a `WindowChrome` with `CaptionHeight = 0`
and `GlassFrameThickness = -1`, then calls `RemoveWindowTitlebarContents`. So
clearing the property does **not** give you the caption Windows draws — deriving
from plain `Window` is what does, which is why `BaseWindow` does.

⚠️ **`WindowBackgroundManager.UpdateBackground` ends by setting
DWMWA_CAPTION_COLOR to "none".** That is right for a window drawing its own
`ui:TitleBar` and is exactly what stops Windows painting a caption for one that
is not. With the native caption, use `ApplyDarkThemeToWindow` /
`RemoveDarkThemeFromWindow` alone — DWMWA_USE_IMMERSIVE_DARK_MODE is then the only
attribute that is ours. Whichever you call, call it from `OnSourceInitialized`:
the caption-colour call needs an HWND and, unlike its neighbours, does not defer
itself to `Loaded` — it returns false and leaves the caption alone.

⚠️ **Changing a window's base class is a SWEEP, not an edit — there are five
windows and the C# names only three of them.** `SettingsWindow` and
`SequenceWindow` declare no base in their `.xaml.cs` (it comes from the XAML
root), so a grep of the C# finds `AboutBox`, `DebugWindow` and `KeySelector` and
misses the two biggest. That is how `DebugWindow` was left on plain `Window` when
the other four moved to `BaseWindow` (#19): WPF-UI's implicit `{x:Type Window}`
style still themed its body, so it was a dark window with a light caption, and
neither the build nor the tests can see that. Grep both:

```bash
grep -rn "class .*: *\(Window\|BaseWindow\)\b" --include=*.cs src/
grep -rn "^<[A-Za-z:]*\(BaseWindow\|Window\)\b" --include=*.xaml src/
```

⚠️ `RemoteControl` is the one deliberate exception: a message-only window hosting
the HWND hook, never shown, so it stays a plain `Window` and needs no theming.

**A hardcoded value inside a WPF-UI template can only be overridden by copying
that template.** `StyleOverrides.xaml` carries four such copies — `TabItem`
(MinWidth 180), `CheckBox` (a 22px glyph), `TabControl` (`Background="Transparent"`
on the header panel) and `MenuItem` (32px rows). Each is the theme's own template
verbatim with one value changed, and each says which one in a comment. ⚠️ A
template's **triggers do not come along with `BasedOn`**, so a copy that drops them
loses the hover highlight, the checked state and the rest.

## Themes

`theme_mode` in `settings.ini` is `System` (the default, and what an EMPTY value
means), `Light` or `Dark`. `Settings.SetTheme()` resolves it and **logs what it
resolved to** — before that the only trace was a `Debug.WriteLine`, so "it is
still bright everywhere" was answerable from no artifact at all. Read
`%LocalAppData%\WinCompose\wincompose.log` for `Theme: System resolved to Dark`
before theorising.

- ⚠️ **Empty used to fall through to Light**, silently, so WinCompose stayed white
  on a machine running Windows in dark mode — by design, with nothing saying so,
  and with the theme combo box showing blank because `""` matched neither entry.
  If a user says "I use the dark theme", ask whether they mean Windows or the app.
- A Windows theme switch arrives as **WM_SETTINGCHANGE with "ImmersiveColorSet"**
  in lParam. `RemoteControl` already owns an HWND hook for the life of the
  process, so that is where it is read; no watcher, no window to keep alive.
  ⚠️ lParam is a string pointer for only *some* values of that message, so it is
  read defensively — this drives the colours and must never be able to take the
  process down.
- ⚠️ **`theme_mode` does NOT move when Windows flips its theme, so nothing may
  key off the SETTING to react to a theme change.** `SystemThemeEvent` →
  `SetTheme()` → `ApplySystemTheme()` re-resolves and re-applies while
  `theme_mode` stays `System`, so `ThemeMode.ValueChanged` never fires. The auto
  mode shipped with `BaseWindow.ApplyThemeToFrame` bound to exactly that: an open
  window's content re-themed and its native caption did not, which is the dark
  content over a light title bar that whole change existed to remove. Subscribe
  to **`ApplicationThemeManager.Changed`** instead — WPF-UI raises it from inside
  `Apply()`, so the preference route and the Windows route both reach it, and it
  reports the theme that was applied rather than the one that was asked for.
  (Caught by CodeRabbit on #19, fixed in `33c8312`.)
- ⚠️ **Colours hardcoded in XAML are invisible to a scan of the images.** Two
  rounds went into "where is the old beige icon?" when the answer was that the
  sequence list draws its keycaps as XAML gradients, and six more literals
  (`Background="White"`, `Foreground="Black"`, `LightGray` borders) dated from
  when white was the only background a window had. When the icon or the theme
  changes, **grep the XAML for literal colours**, not just `res/`.
- **A stale taskbar icon is Windows, not us.** The exe carries no
  `<ApplicationIcon>`, no `.rc` and no `.res`: its icons come only from
  `InsertIcons.exe wincompose.exe res` after the build, and the tray icon is
  composited at runtime from `key_empty.png` plus a decal. `ie4uinit.exe -show`,
  or a reboot, is the fix.

## The tray icon animates while composing

Idle is the cap with the **dark** diamond; composing lights one quarter of that
diamond and walks it clockwise at 280 ms a frame (`SpinFrames` / `SpinFrameMs`
in `NotificationIcon.xaml.cs`). The full account — why a diamond cannot be seen
to rotate, and the measured still-pair table this replaced — is in
`art/README.md`. Three things that are easy to get wrong:

- ⚠️ **`TaskbarIcon.Icon` is ONE `Shell_NotifyIcon` NIM_MODIFY, not a delete
  and re-add.** wpf-notifyicon's setter writes `iconData.IconHandle` then calls
  `Util.WriteIconData(ref iconData, NotifyCommand.Modify, …)`
  (`src/NotifyIconWpf/TaskbarIcon.Declarations.cs`). So animating through it
  costs nothing, and the icon neither flickers nor moves. **Our own comment
  said the opposite**, which is exactly the kind of note that
  talks the next person out of a change that was always cheap — the *Visibility*
  setter is the one that really does delete and re-add, and that comment is
  correct where it sits, three lines below.
- ⚠️ **Cache every frame.** `Icon.FromHandle(bitmap.GetHicon())` never destroys
  the handle, so building a frame per tick leaks an HICON every 280 ms. The
  cache is indexed by the state bits plus the frame (`index >> 2`).
- ⚠️ **Drive the frame off a `Stopwatch`, not a tick count.**
  `CompositionTarget.Rendering` is the WPF render loop, not a metronome;
  counting ticks makes the animation run at whatever rate WPF happens to be
  drawing at, where reading a clock drops a frame instead.

`Animate` is off when Windows animations are off (`SystemParameters.
ClientAreaAnimation`) or the icon is hidden; the composing icon is then the
static frame 0, which is still a lit quarter and so still differs from idle.

## Translations

`src/update-data.sh` rebuilds `language/*/X.<locale>.resx` from the Weblate-fed
`po/*.po`. **⚠️ So editing a `.resx` by hand does not survive** — the next run
overwrites it.

Two layers sit under that, both in `src/apply-machine-translations.py` (step 4 of
`update-data.sh`):

- **The machine fallback** (`po-machine/*.po`, written in this fork, *not* part of
  the Weblate corpus) fills strings a locale is missing. Precedence is Weblate,
  then machine, then English, and it **never overrides a human translation** — so
  correcting a string upstream still replaces ours everywhere. This took the
  complete-interface count from **10 of 47 locales to 39 of 47** (137 base
  strings).
- **`CORRECTIONS`** is the one exception and is deliberately narrow: a human
  string `string.Format` provably cannot render. An entry must name a defect the
  build can **see** — today, a placeholder that does not survive into the
  translation — never a wording preference. When upstream renders correctly the
  entry reports **STALE** rather than silently going on overriding, which is the
  cue to delete it. Two entries today (`be` and `fi` `DelaySeconds`), both still
  wrong upstream on Weblate.

`check_placeholders()` warns for every locale, not only the ones we fill.

## Releases

**`RELEASE.md` is the authority** and is accurate; the
`polykybd-github-release` skill drives the flow. Tags are `PK-<version>` (set by
`GitVersion.yml` `tag-prefix`, so **never** tag `vX.Y.Z`), and a release is created
by **publishing**, not by pushing a tag.

⚠️ **The shipped version is HARDCODED in `src/wincompose/wincompose.csproj`, and
the tag is chosen independently — so publishing before the bump has merged ships
a release labelled with the PREVIOUS version, in four places at once.**
`<AssemblyVersion>`/`<FileVersion>` is the only source: `iscc` reads it off the
built exe for the installer filename, `release.yml` reads the same resource to
name the zip, `Settings.Version` is `Assembly.GetExecutingAssembly()` so the About
tab shows it, and `Updater.cs` compares *that* against `status.txt`. Which version
to publish comes from the `release-notes` branch instead
(`scripts/publish_release.py`), and the tag used to be created at `main`'s tip —
which is the whole mechanism below.

**Worked example, PK-0.9.19 (2026-10-02, issue #21).** Published 09:47 UTC; the
bump `cf7e91e` was committed 09:45 but sat on the PR #23 branch and merged at
10:35. So the tag landed on `3916815`, whose tree still said `0.9.18.0`:

- the assets are `WinCompose-Setup-0.9.18.exe` and
  `WinCompose-NoInstall-0.9.18.zip` — real 0.9.19 builds (installer 4,952,184 B
  against 0.9.18's 4,949,128), just mislabelled, which is why the reporter saw the
  new rotating tray icon in a file called 0.9.18;
- the About tab reads 0.9.18;
- `status.txt` could not be bumped **at all**: an install reporting 0.9.18 would
  be offered an endless update to itself, and `releases/latest` would hand back
  the same two files;
- and three commits its own notes describe (the Check-for-Updates button and the
  About-tab link regrouping) are not in the assets either, because they merged in
  the same window.

⚠️ **`publish_release.py` now tags the commit whose csproj DECLARES the version,
not `main`'s tip** — and because `release.yml` checks out the tag, that is what
makes the assets carry the right number. The sibling repos had this (it is
`commit_for_version()`, with four hard-won traps in its docstring: oldest match
not newest, no early exit, `--first-parent`, and a `:(top)` pathspec); this copy
was taken from them in `e894a3a` before the pin existed, which is why #21
happened here and not there. A later merge drifting the csproj forward is then
harmless, and the script says so rather than warning.

⚠️ **The pin cannot help when NO commit declares the version** — the bump has
not merged, so there is nothing to pin to — and that is exactly #21. Creating
the release is refused there. Only when creating: re-applying notes to an
existing release runs no build, so the tree's version is then irrelevant, and
`release_exists()` tells the two apart. ⚠️ **A tag existing is a different
question** — a deleted release, a hand-pushed tag, or a build that died before
`gh release create` each leave a tag with no release. Only a 404 counts as "no
release"; anything else is treated as creating, the stricter reading, because an
auth or network failure reading as success is how the check goes quiet.
⚠️ **A shallow clone reaches the same refusal for a different reason** — the
search could not see the commit — so unshallowing comes before
`--allow-version-mismatch`, which publishes the release the gate exists to
prevent. `release.yml` also asserts the built exe's version equals the tag
before attaching anything, because the script is only one of three ways a
release starts (a hand-pushed tag and a `workflow_dispatch` recovery do not go
through it).

⚠️ **An EXISTING tag beats the pin, and the pin's own report hides it.**
`target_commitish` is documented as "Unused if the Git tag already exists" — a
release never moves a tag — so the build comes from wherever the tag points while
the script reports the commit it *wanted*. Reached by an ordinary sequence:
publish early, delete the release, merge the bump, try again; the first attempt's
tag is still on the pre-bump commit. **PK-0.9.20 sat exactly there** (tag on
`6cb987cb1a` declaring 0.9.19, pin resolving `113093cfa2`), so the script would
have printed the right commit and published the wrong one. It refuses now, and
⚠️ **that check must fire even when the version gate passes** — there the pin
found a correct commit and the mismatch gate sees nothing wrong. The fix is to
**move the tag** and publish normally, not to dispatch: a dispatch attaches
correct assets while leaving `git checkout <tag>` on a tree that declares the
previous version. `RELEASE.md` → *If the tag is in the wrong place*.

⚠️ **Do NOT make the gate fire on the tree merely differing from the tag.** The
firmware and host tree is normally *ahead* of a prepared tag — every merge
auto-bumps — and the pin already makes that safe, so refusing there blocks
almost every legitimate publish. The condition is "no commit declares it", not
"the numbers differ".

**The GitVersion derivation is still the deeper answer, and still not done.**
Deriving the exe's version from the tag would make a mislabelled release
unrepresentable rather than merely refused. It is not done because there is no
.NET toolchain in the container, so CI is the only compiler (a wrong guess costs
a full round), and because GitVersion's output on an untagged commit would change
what every development build's About tab reads.

⚠️ **`scripts/publish_release.py` is byte-identical across `qmk_firmware`,
`PolyKybdHost` and `wincompose`, and nothing checks it.** It had already
diverged in both directions at once: the siblings held the pin and the
`make_latest` correctness, this copy held the create-time gate, and each was
the newer one for a different thing. `md5sum */scripts/publish_release.py` is
the check.

**`status.txt` is written by CI since 0.9.20** — the `status` job in `release.yml`,
after the assets are attached, from the version resource of the binary it just
shipped, never moving `Latest` backwards. It cannot name a release that does not
exist, nor a version other than the one in the download. It was manual before
that and went wrong in both directions, both quietly:

- **Bumped early**, every install is told about a release that does not exist.
  It does not 404: `Installer:`/`Portable:` resolve `releases/latest`, so the user
  is quietly handed the version they already have. Cost a 20-minute window on
  0.9.18 (2026-09-07) because the release skill said to bump it as prep.
- **Left stale**, no existing install ever learns the release happened, the
  release itself looks perfect, and testing it cannot reveal this — 0.9.19 sat
  unannounced for two weeks and was reported from outside as "no notification".

## Environment

- **Cannot delete a remote branch from a session here** — the git proxy returns
  403 on the delete, as it does on `refs/tags/*`. Leftover branches have to go in
  the GitHub UI; don't burn retries.
- **Cannot publish a release** — no `gh`, and no create-release MCP tool. Stage
  the notes and hand over `python scripts/publish_release.py`.
- **No .NET toolchain in the container.** CI is the only compiler, so a push that
  cannot build costs a full round — read the diff adversarially first.
  - ⚠️ **With nothing to compile and nothing to render, a claim about WPF-UI is
    worth only as much as the source you read it from.** Both wrong diagnoses in
    the window-styling work (#19) came from reasoning about the library instead:
    `1a91b3c` blamed the DWM caption colour and changed nothing, and `f8216e1`'s
    message asserted that `FluentWindow` left `BaseWindow` unstyled, which is
    false — `FluentWindow` overrides `DefaultStyleKeyProperty`. Every correct
    answer that session came from fetching the file and reading it.
  - **Raw file reads work, the API does not.** Measured 2026-09-07:
    ```bash
    curl -sS "https://raw.githubusercontent.com/lepoco/wpfui/3.0.5/src/Wpf.Ui/Appearance/WindowBackgroundManager.cs"
    ```
    returns 200 for any path, while `api.github.com/repos/lepoco/wpfui/…` returns
    **403** — *"GitHub access to this repository is not enabled for this session"*,
    which also names `add_repo` as the way in. So there is no tree listing, and a
    path is guesswork until it 200s: `FluentWindow.cs` is under
    `src/Wpf.Ui/Controls/FluentWindow/`, and the obvious
    `src/Wpf.Ui/Controls/Window/` 404s.
