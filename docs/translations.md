# Translations

_Moved verbatim from `CLAUDE.md` on 2026-10-10. CLAUDE.md keeps a short pointer._

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
