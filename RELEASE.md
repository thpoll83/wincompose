Release Howto for WinCompose (PolyKybd fork)
============================================

Releases are tagged `PK-<version>` and are created by **publishing** a GitHub
release — not by pushing a tag. Publishing fires `release: published`, and
`.github/workflows/release.yml` builds the installer, the portable zip and
`SHA256SUMS.txt`, applies the crafted notes, and attaches the assets. Do not
attach anything by hand.

① Set the version
-----------------

`<AssemblyVersion>` and `<FileVersion>` in `src/wincompose/wincompose.csproj`.
That one number is the shipped version: `iscc` reads it off the built exe, so the
asset filenames follow it, and so do the About tab (`Settings.Version`) and the
version `Updater.cs` compares against `status.txt`. It does **not** decide which
tag gets published — see ③. `GitVersion.yml` sets `tag-prefix: PK-` so GitVersion
recognises our tags instead of computing 0.1.0.

⚠️ **The bump has to be MERGED to `main` before you publish.** The release builds
from the tag, and the tag can only be pinned to a commit that already declares the
version, so a bump still sitting on a branch leaves nothing to pin to — publishing
is refused (it used to ship assets carrying the *previous* version instead).
0.9.19 did this (issue #21): published 50 minutes before its bump merged, it carries
`WinCompose-Setup-0.9.18.exe`, reads 0.9.18 in its About tab, is missing two
changes its own notes describe, and could not be announced at all — an install
reporting 0.9.18 would have been offered an endless update to itself.

Three things now stand between you and that, so it is a check you no longer have
to remember. `scripts/publish_release.py` pins the tag to the commit that declares
the version, so a csproj that has drifted forward since the notes were prepared is
harmless. It dies when **no** commit declares it, and only when publishing would
CREATE the release, since re-applying notes to one that already exists re-runs no
build. And `release.yml` fails before attaching anything if the built exe's
version is not the tag's.

⚠️ `--allow-version-mismatch` overrides the first of those and leaves you a
published release with **no assets**, because the second still refuses them. It
is for getting the notes up; attach the assets afterwards with a
`workflow_dispatch` once the bump has merged.

Run `src/update-data.sh` if the translations need refreshing.

② Prepare the release notes
---------------------------

Notes live one file per tag on the unprotected `release-notes` branch:
`PK-<version>.md`, first line `# <title>`, the rest the body. The workflow reads
that file; without it the release falls back to GitHub's auto-generated notes.

③ Publish
---------

    python scripts/publish_release.py             # publish the newest prepared tag
    python scripts/publish_release.py --dry-run   # show what it would do
    python scripts/publish_release.py --tag PK-0.9.16

Which version to publish comes from the `release-notes` branch, **not** from the
csproj: with no `--tag`, it publishes the newest prepared `PK-<X.Y.Z>.md` found
there. Pass `--tag` whenever more than one is prepared, and read the "newest
prepared tag" line it prints before letting it publish.

**Where the tag lands is decided by the csproj, though.** The script tags the
commit whose `wincompose.csproj` declares that version, not `main`'s tip — and
since the release workflow checks out the tag, that is what makes the assets
carry the right number. A later merge drifting the csproj forward is then
harmless, and it says so.

If **no** commit declares the version, the bump has not merged and there is
nothing to pin to, so creating the release is refused. Merge the bump and
re-run. `--allow-version-mismatch` overrides, and publishes notes with no
assets — the release workflow refuses to attach assets whose version does not
match the tag. In a shallow clone the search can miss the commit, so
`git fetch origin --unshallow` comes before reaching for that flag.

Auth comes from `GH_TOKEN` / `GITHUB_TOKEN`, else `gh auth token`.

④ The updater announces it — automatically
-----------------------------------------

`status.txt` in the repo root is what `src/wincompose/Updater.cs` reads, over
`https://raw.githubusercontent.com/thpoll83/wincompose/main/status.txt` — from the
default branch, not from the releases. The tray offers a download the moment
`Latest` exceeds the running version.

**The `status` job in `release.yml` sets it for you**, after the assets are
attached and from the version resource of the binary it just shipped — so it can
only ever name a release that exists, at the version that is actually in the
download. It never moves `Latest` backwards, so re-dispatching an old tag to
re-attach its assets cannot un-announce a newer release. Check the job went green;
if it could not push, its error says so and names the one-line edit.

⚠️ **Deleting a release does not un-set it.** The job asks whether the release
exists *before* it bumps, and nothing looks again afterwards. So a release deleted
after the job has run leaves `Latest` naming a version with no release — the
early-bump failure below, arriving by another route. Revert `status.txt` in the
same breath as the deletion, or re-create the release (see *If the release build
fails*). 0.9.20 was deleted and re-created on 7 October, and for the ~4 hours in
between every install below 0.9.20 was offered the 0.9.19 download.

It was a manual step until 0.9.20, and it went wrong in both directions. Early,
every install is pointed at a release that is not there — and the `Installer:` /
`Portable:` URLs resolve `releases/latest`, so nobody gets a 404, they are handed
the version they already have and read it as the updater being broken (0.9.18, a
20-minute window). Forgotten, no existing install ever learns the release
happened and nothing about the release itself looks wrong (0.9.19, two weeks,
reported from outside as "no notification").

If the tag is in the wrong place
--------------------------------

⚠️ **Publishing never MOVES a tag.** `target_commitish` is documented as "Unused
if the Git tag already exists", so when the tag is already there the build comes
from wherever it points and the commit the script pins is only a claim.
`publish_release.py` refuses to create a release in that case, naming both
commits.

That is reachable from an ordinary sequence: publish too early, delete the
release, merge the bump, try again. The tag from the first attempt is still on
the pre-bump commit. 0.9.20 did exactly this.

Two ways out, and the first is better:

1. **Move the tag to the commit that declares the version**, then publish
   normally. The release then builds from a tree that reports the right version,
   and `git checkout <tag>` gives you the source the release was made from:

       git fetch origin main
       git tag -f PK-<version> <commit declaring it>
       git push --force origin refs/tags/PK-<version>
       python scripts/publish_release.py

   ⚠️ **Take the commit from the refusal, which names both** — where the tag is
   and where it should be. That is the only place to read them: the tag check
   runs before the script prints its summary, so a `--dry-run` on a misplaced
   tag never reaches the `target:` line. Safe as long as no release exists on
   that tag yet; check before force-pushing.
2. **Dispatch the workflow** (below) to attach correct assets to the tag where it
   is. Quicker, but the tag keeps pointing at a tree declaring the *previous*
   version, so a later `git checkout <tag>` misleads — which is the kind of thing
   that sends a bisect wrong.

If the release build fails
--------------------------

Re-running the original run replays the workflow file from that tag's commit, bug
included. Fix the default branch, then start the workflow by hand
(`workflow_dispatch`) with `tag: PK-<version>` to attach the assets to the release
that already exists. Leaving `tag` empty builds the assets as artifacts and
releases nothing — that is the safe smoke test.

Two things about that dispatch are easy to get wrong:

- **It re-applies the notes from `release-notes`.** Whenever a `PK-<version>.md`
  is on that branch, the dispatch runs `gh release edit --title --notes-file`, so
  a title or body edited by hand on GitHub is thrown away. Fix the file on the
  branch *first*, then dispatch. (With no notes file, the existing notes are kept
  and only the assets are replaced.)
- **It creates the release when there is none** (`gh release create`), so the same
  dispatch recovers a release that was deleted, not only one whose build failed.
  It builds from the ref you dispatch on, normally `main`, never from the tag's
  own commit — which is what the version assertion compares the tag against.

Building locally
----------------

`make` in an MSYS2 shell builds the installer and the portable version; building
the Visual Studio solution is not enough, since it only builds the installer. It
needs GitVersion, Inno Setup 6 and gettext, and all Git submodules fetched.

## Background: why the gates exist

_Moved verbatim from `CLAUDE.md` on 2026-10-10. CLAUDE.md keeps a short pointer._


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
