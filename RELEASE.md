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
from the tag, and the tag is created at `main`'s tip, so a bump still sitting on a
branch means the release ships assets carrying the *previous* version. 0.9.19 did
this (issue #21): published 50 minutes before its bump merged, it carries
`WinCompose-Setup-0.9.18.exe`, reads 0.9.18 in its About tab, is missing two
changes its own notes describe, and could not be announced at all — an install
reporting 0.9.18 would have been offered an endless update to itself.

Two things now refuse rather than let that through, so this is a check you no
longer have to remember: `scripts/publish_release.py` dies when `main` is behind
the tag it is about to publish (`--allow-behind` overrides), and `release.yml`
fails before attaching anything if the built exe's version is not the tag's.

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

The tag comes from the `release-notes` branch, **not** from the csproj version:
with no `--tag`, it publishes the newest prepared `PK-<X.Y.Z>.md` found there. The
version on the default branch is read only to print a note when the two disagree,
so a csproj bump that landed after the notes were prepared does not change what
gets published. Pass `--tag` whenever more than one is prepared, and read the
"newest prepared tag" line it prints before letting it publish.

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

It was a manual step until 0.9.20, and it went wrong in both directions. Early,
every install is pointed at a release that is not there — and the `Installer:` /
`Portable:` URLs resolve `releases/latest`, so nobody gets a 404, they are handed
the version they already have and read it as the updater being broken (0.9.18, a
20-minute window). Forgotten, no existing install ever learns the release
happened and nothing about the release itself looks wrong (0.9.19, two weeks,
reported from outside as "no notification").

If the release build fails
--------------------------

Re-running the original run replays the workflow file from that tag's commit, bug
included. Fix the default branch, then start the workflow by hand
(`workflow_dispatch`) with `tag: PK-<version>` to attach the assets to the release
that already exists. Leaving `tag` empty builds the assets as artifacts and
releases nothing — that is the safe smoke test.

Building locally
----------------

`make` in an MSYS2 shell builds the installer and the portable version; building
the Visual Studio solution is not enough, since it only builds the installer. It
needs GitVersion, Inno Setup 6 and gettext, and all Git submodules fetched.
