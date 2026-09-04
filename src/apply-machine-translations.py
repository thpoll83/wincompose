#!/usr/bin/env python3
"""Layer machine-translated strings under the human translations.

`update-data.sh` builds `language/*/X.<locale>.resx` from the Weblate-fed
`po/*.po` files, and a string with no translation there simply falls back to
English at runtime.  This script fills those gaps from `po-machine/<lang>.po`,
which is *not* part of the Weblate corpus: it is written here, in this fork.

The precedence is therefore Weblate, then machine, then English, and the middle
layer can only ever replace an English string in a non-English UI — it never
overrides a human translation.  Run it after `update-data.sh` step 2 (which it
calls itself), or standalone; it is idempotent.

    python3 apply-machine-translations.py            # fill the gaps
    python3 apply-machine-translations.py --check    # report, write nothing
"""

import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))

# Targets: (machine-file kind, English master, generated file template).
TARGETS = [
    ("Text", "language/i18n/Text.resx", "language/i18n/Text.{loc}.resx"),
    ("Category", "language/unicode/Category.resx", "language/unicode/Category.{loc}.resx"),
]

# .po language code -> .resx locale.  Mirrors po2res() in update-data.sh;
# check_po2res_matches_shell() below fails loudly if the two ever drift.
PO2RES = {
    "pt_BR": "pt-BR",
    "zh": "zh-CHS",
    "zh_Hant": "zh-CHT",
    "sc": "it-CH",
    "eo": "de-CH",
    "be@latin": "be-BY",
}


def po2res(lang):
    if lang in PO2RES:
        return PO2RES[lang]
    return "" if "@" in lang else lang


def check_po2res_matches_shell():
    """update-data.sh owns the same mapping; assert we agree with it."""
    path = os.path.join(HERE, "update-data.sh")
    if not os.path.exists(path):
        return
    body = open(path, encoding="utf-8").read()
    m = re.search(r"po2res\(\)\s*\{(.*?)\n\}", body, re.S)
    if not m:
        sys.exit("apply-machine-translations: cannot find po2res() in update-data.sh")
    shell = {}
    for case, action in re.findall(r"^\s*([^\s)|]+)\)\s*echo\s*(\S*)\s*;;", m.group(1), re.M):
        if case == "*":
            continue
        shell[case] = "" if action in ('""', "''") else action
    ours = dict(PO2RES)
    ours["*@*"] = ""
    if shell != ours:
        sys.exit("apply-machine-translations: po2res mapping has drifted from "
                 f"update-data.sh\n  update-data.sh: {shell}\n  this script:    {ours}")


def read_resx(path):
    """id -> value, ignoring the schema comment block at the top."""
    if not os.path.exists(path):
        return None
    text = open(path, encoding="utf-8-sig").read()
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    return dict(re.findall(r'<data name="([^"]+)"[^>]*>\s*<value>(.*?)</value>', text, re.S))


def read_machine_po(path):
    """(kind, id) -> msgstr, for entries that actually carry a translation."""
    out = {}
    kind = ident = None
    msgstr = None
    reading = False
    for line in open(path, encoding="utf-8"):
        line = line.rstrip("\n")
        m = re.match(r"^#:\s*(\w+)\s+ID:(\S+)\s*$", line)
        if m:
            kind, ident = m.group(1), m.group(2)
            continue
        m = re.match(r'^msgstr\s+"(.*)"$', line)
        if m:
            msgstr = m.group(1)
            reading = True
            continue
        m = re.match(r'^"(.*)"$', line.strip())
        if m and reading:
            msgstr += m.group(1)
            continue
        if reading:
            if kind and ident and msgstr:
                out[(kind, ident)] = msgstr
            reading = False
            msgstr = None
    if reading and kind and ident and msgstr:
        out[(kind, ident)] = msgstr
    return out


def unescape_po(s):
    return s.replace(r"\"", '"').replace(r"\\", "\\").replace(r"\n", "\n").replace(r"\t", "\t")


def escape_xml(s):
    return (s.replace("&", "&amp;").replace("<", "&lt;")
             .replace(">", "&gt;").replace('"', "&quot;"))


def append_entries(path, entries):
    """Insert <data> elements before </root>, keeping CRLF and the BOM."""
    with open(path, encoding="utf-8-sig", newline="") as f:
        raw = f.read()          # newline="" so existing CRLFs survive the round trip
    block = ""
    for ident, value in entries:
        block += (f'  <data name="{ident}" xml:space="preserve">\r\n'
                  f"    <value>{escape_xml(value)}</value>\r\n"
                  f"    <comment>machine-translated fallback</comment>\r\n"
                  f"  </data>\r\n")
    if "</root>" not in raw:
        sys.exit(f"apply-machine-translations: no </root> in {path}")
    if "\r\n" not in raw:      # the repo declares *.resx eol=crlf, but do not assume
        block = block.replace("\r\n", "\n")
    raw = raw.replace("</root>", block + "</root>", 1)
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        f.write(raw)


def check_placeholders(english):
    """A {0} that survives into the English but not the translation is a defect.

    This looks at every locale, not only the ones we fill: `string.Format` is
    what renders these, so a dropped or mistyped placeholder either loses the
    value or throws, and nothing else in the build looks for it.
    """
    import glob
    holes = re.compile(r"\{\d+\}")
    problems = []
    for kind, master, template in TARGETS:
        prefix = template.split("{loc}")[0]
        suffix = template.split("{loc}")[1]
        for path in sorted(glob.glob(prefix + "*" + suffix)):
            loc = os.path.basename(path)[len(os.path.basename(prefix)):-len(suffix)]
            for ident, value in (read_resx(path) or {}).items():
                want = sorted(set(holes.findall(english[kind].get(ident, ""))))
                if want and sorted(set(holes.findall(value))) != want:
                    problems.append((loc, ident, want, value))
    for loc, ident, want, value in problems:
        print(f"  WARNING {loc}/{ident}: expected {' '.join(want)}, got {value!r}")
    return problems


def main():
    check = "--check" in sys.argv[1:]
    os.chdir(HERE)
    check_po2res_matches_shell()

    english = {kind: read_resx(master) for kind, master, _ in TARGETS}
    for kind, master, _ in TARGETS:
        if english[kind] is None:
            sys.exit(f"apply-machine-translations: missing English master {master}")

    check_placeholders(english)

    machine_dir = "po-machine"
    if not os.path.isdir(machine_dir):
        print("apply-machine-translations: no po-machine/ directory, nothing to do")
        return 0

    total_added = total_gap = 0
    for name in sorted(os.listdir(machine_dir)):
        if not name.endswith(".po"):
            continue
        lang = name[:-3]
        loc = po2res(lang)
        if not loc:
            continue
        entries = read_machine_po(os.path.join(machine_dir, name))
        added = gap = 0
        for kind, _, template in TARGETS:
            path = template.format(loc=loc)
            existing = read_resx(path)
            if existing is None:
                print(f"  {lang}: no {path}, skipped")
                continue
            todo = []
            for ident in english[kind]:
                if ident in existing:
                    continue
                value = entries.get((kind, ident))
                if value:
                    todo.append((ident, unescape_po(value)))
                else:
                    gap += 1
            if todo and not check:
                append_entries(path, todo)
            added += len(todo)
        if added or gap:
            verb = "would fill" if check else "filled"
            note = f", {gap} still English" if gap else ""
            print(f"  {lang:9} -> {loc:7} {verb} {added:3}{note}")
        total_added += added
        total_gap += gap

    print(f"apply-machine-translations: {total_added} string(s) "
          f"{'to fill' if check else 'filled'}, {total_gap} still falling back to English")
    return 0


if __name__ == "__main__":
    sys.exit(main())
