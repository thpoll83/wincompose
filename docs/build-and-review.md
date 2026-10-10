# Build, CI, review and the session environment

_Moved verbatim from `CLAUDE.md` on 2026-10-10. CLAUDE.md keeps a short pointer._

## Why this fork matters
The **wincompose** repo is the PolyKybd fork of WinCompose, a compose-key tray app
for Windows.

PolyKybd depends on it. `PolyKybdHost/polyhost/input/unicode_input.py` prefers
`InputMethod.WinCompose` over the far more limited native Windows path, so this
app is what gives the keyboard real Unicode and emoji output there — which is why
the host has an "Install WinCompose…" tray entry at all.

It is a **fork of a fork**: samhocevar → ell1010 → thpoll83. Both upstreams are
read-only from a session here; only `thpoll83/wincompose` can be pushed to.

The **code-review conventions** and **branching rules** in
[`../PolyKybdHost/CLAUDE.md`](../../PolyKybdHost/CLAUDE.md) apply here too — in
particular: start each piece of work on a fresh branch cut from the updated
default (**`main`**), never keep committing to a branch whose PR has merged, and
verify an AI reviewer's finding against the code before acting on it.


## Reviewers: expect none

All three bots are effectively unavailable on this repo, and each goes quiet in
its own way. Measured repeatedly (2026-09-04, 2026-09-07):

- **CodeRabbit does not auto-review here.** (First blamed on the 10-star threshold; since 2026-10-08 auto-review is off for all PolyKybd repos in the CodeRabbit org settings.) It renders a
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
