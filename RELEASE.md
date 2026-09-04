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
`iscc` reads that version off the built exe, so the asset filenames follow it.
It does **not** decide which tag gets published — see ③. `GitVersion.yml` sets
`tag-prefix: PK-` so GitVersion recognises our tags instead of computing 0.1.0.

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

④ Update the updater
--------------------

`status.txt` in the repo root is what `src/wincompose/Updater.cs` reads, over
`https://raw.githubusercontent.com/thpoll83/wincompose/main/status.txt`. Set
`Latest` to the released version once the release exists — while it names a
version that has no release, the tray offers a download that is not there.

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
