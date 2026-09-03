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
`iscc` reads that version off the built exe, so the asset filenames follow it,
and `scripts/publish_release.py` derives the tag from it. `GitVersion.yml` sets
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

It reads the version from the default branch (not from whatever branch you have
checked out), finds the prepared notes, then creates and publishes the release.
Auth comes from `GH_TOKEN` / `GITHUB_TOKEN`, else `gh auth token`.

④ Update the updater
--------------------

`status.txt` in the repo root is what `src/wincompose/Updater.cs` reads, over
`https://raw.githubusercontent.com/thpoll83/wincompose/main/status.txt`. Set
`Latest` to the released version once the release exists — while it names a
version that has no release, the tray offers a download that is not there.

⑤ Update the README
-------------------

The download links name the version explicitly, so point them at the new tag.

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
