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

⚠️ **`status.txt` is bumped AFTER publishing, and the value is wrong in both
directions.** `Updater.cs` reads it from `main` — not from the releases — and
offers a download the moment `Latest` exceeds the running version:

- **Bumped early**, every install is told about a release that does not exist.
  It does not 404: `Installer:`/`Portable:` resolve `releases/latest`, so the user
  is quietly handed the version they already have. Cost a 20-minute window on
  0.9.18 (2026-09-07) because the release skill said to bump it as prep.
- **Left stale**, no existing install ever learns the release happened, the
  release itself looks perfect, and testing it cannot reveal this.

## Environment

- **Cannot delete a remote branch from a session here** — the git proxy returns
  403 on the delete, as it does on `refs/tags/*`. Leftover branches have to go in
  the GitHub UI; don't burn retries.
- **Cannot publish a release** — no `gh`, and no create-release MCP tool. Stage
  the notes and hand over `python scripts/publish_release.py`.
- **No .NET toolchain in the container.** CI is the only compiler, so a push that
  cannot build costs a full round — read the diff adversarially first.
