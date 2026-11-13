"""
Weryfikacja bloków wartosci SMSG_UPDATE_OBJECT Forever 1.60.1 wobec TDB.

Uruchamiany po parsowaniu sniffu: czyta <sniff>_parsed.txt i plik SQL TDB,
i dla kazdego CreateObject porownuje (entry, DisplayID) z tabela TDB.
Mowi tez, ile bajtow zostaje w bloku wartosci kazdego obiektu.

Uzycie:
    python tdb_check.py <parsed.txt> <TDB_full_world_*.sql> [--sample N]

Zgodnosc displayId = sygnal, ze blok wartosci czyta pola w dobrej kolejnosci.
"""

import csv
import io
import re
import sys
from collections import Counter, defaultdict


def read_tdb(path, tables):
    """Wyciaga z dumpu MySQL tabele {nazwa: (kolumny, wiersze)} - strumieniowo, bez bazy."""
    out = {t: (None, []) for t in tables}
    cur = None
    cols = None
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            m = re.match(r"^CREATE TABLE `(\w+)`", line)
            if m:
                cur = m.group(1)
                cols = [] if cur in tables else None
                continue
            if cols is not None:
                mc = re.match(r"^\s+`(\w+)`", line)
                if mc:
                    cols.append(mc.group(1))
                elif line.startswith(") ENGINE"):
                    out[cur] = (cols, [])
                    cols = None
                continue
            mi = re.match(r"^INSERT INTO `(\w+)` VALUES (.*);?\s*$", line)
            if mi and mi.group(1) in tables and out[mi.group(1)][0] is not None:
                rows = out[mi.group(1)][1]
                for vals in re.finditer(r"\(([^()]*)\)", mi.group(2)):
                    try:
                        rows.append(next(csv.reader(io.StringIO(vals.group(1)), quotechar="'", skipinitialspace=False)))
                    except Exception:
                        pass
    return out


def index_tdb(tables):
    """entry -> zbior displayId (stwory) oraz entry -> displayId (gameobjecty)."""
    creature = defaultdict(set)
    cols, rows = tables["creature_template_model"]
    ci, di = cols.index("CreatureID"), cols.index("CreatureDisplayID")
    for r in rows:
        if len(r) <= max(ci, di):
            continue
        try:
            creature[int(r[ci])].add(int(r[di]))
        except ValueError:
            pass

    gameobject = {}
    cols, rows = tables["gameobject_template"]
    ei, di = cols.index("entry"), cols.index("displayId")
    for r in rows:
        if len(r) <= max(ei, di):
            continue
        try:
            gameobject[int(r[ei])] = int(r[di])
        except ValueError:
            pass

    return creature, gameobject


def parse_blocks(path):
    """Dzieli wynik parsowania na pakiety."""
    blocks = []
    cur = None
    lines = []
    rx = re.compile(r"^(ServerToClient|ClientToServer): (\S+) \(0x([0-9A-Fa-f]+)\) Length: (\d+)")
    with open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            m = rx.match(line)
            if m:
                if cur:
                    blocks.append((cur, lines))
                cur = m.group(2)
                lines = [line.rstrip()]
                continue
            if cur:
                lines.append(line.rstrip())
    if cur:
        blocks.append((cur, lines))
    return blocks


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 2

    parsed, sql = sys.argv[1], sys.argv[2]
    tables = read_tdb(sql, {"creature_template_model", "gameobject_template", "creature_template"})
    creature, gameobject = index_tdb(tables)
    print(f"TDB: {len(creature)} stworow z modelami, {len(gameobject)} gameobjectow")

    stat = Counter()
    leftovers = Counter()
    bad = []

    for _op, lines in parse_blocks(parsed):
        body = "\n".join(lines)
        if "CreateObject" not in body:
            continue

        entry = {}
        for idx, typ, e in re.findall(
            r"\[(\d+)\] Object Guid: .*? (Creature|Pet|Vehicle|GameObject)/\d+.*?Entry: (\d+)", body
        ):
            entry[int(idx)] = (typ, int(e))

        display = {int(i): int(v) for i, v in re.findall(r"\[(\d+)\] DisplayID: (\d+)", body)}

        for idx, (typ, e) in entry.items():
            d = display.get(idx)
            if d is None:
                stat["bez DisplayID"] += 1
                continue
            if typ == "GameObject":
                if e not in gameobject:
                    stat["gameobject poza TDB"] += 1
                    continue
                if gameobject[e] == d:
                    stat["gameobject OK"] += 1
                else:
                    stat["gameobject zly display"] += 1
                    bad.append((typ, e, d, gameobject[e]))
            else:
                if e not in creature:
                    stat["stwor poza TDB"] += 1
                    continue
                if d in creature[e]:
                    stat["stwor OK"] += 1
                else:
                    stat["stwor zly display"] += 1
                    bad.append((typ, e, d, sorted(creature[e])[:3]))

        for left in re.findall(r"Bytes remaining: (\d+)", body):
            leftovers[int(left)] += 1

    print("wynik:", dict(stat))
    ok = stat["gameobject OK"] + stat["stwor OK"]
    total = ok + stat["gameobject zly display"] + stat["stwor zly display"]
    if total:
        print(f"zgodnosc displayId: {ok}/{total} ({100 * ok / total:.1f}%)")
    if leftovers:
        print("bajty zostajace w bloku wartosci:", leftovers.most_common(6))
    for row in bad[:10]:
        print("  niezgodne:", row)
    return 0


if __name__ == "__main__":
    sys.exit(main())
