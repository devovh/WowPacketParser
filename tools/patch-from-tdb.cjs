// Uzupelnia w wygenerowanym SQL kolumny DB2, ktorych klient Forever NIE wysyla,
// wartosciami z bazy hotfixes TDB (po ID). Kolumny sa wstawiane w prawdziwym
// miejscu wg schematu TDB (nie na koncu), zeby plik importowal sie bez edycji.
//
// Uruchomienie:
//   node tools/patch-from-tdb.cjs [plik.sql] [TDB_hotfixes.sql]
//
// Domyslnie:
//   out/forever_70938.sql
//   C:/Users/devov/Desktop/TDB_full_forever_2026_10_14/TDB_full_hotfixes_forever_2026_10_14.sql

const fs = require("fs");
const readline = require("readline");

const SQL = process.argv[2] || "out/forever_70938.sql";
const TDB = process.argv[3] || "C:/Users/devov/Desktop/TDB_full_forever_2026_10_14/TDB_full_hotfixes_forever_2026_10_14.sql";

// Tabele do uzupelnienia: brakujaca kolumna -> skad brac wartosc
// (kolumny sa dopisywane w miejscu wynikajacym ze schematu TDB)
const TABLES = {
    item: ["ItemPetFoodID"],
    area_table: ["ExplorationLevel"],
    map: ["OceanLiquidTypeID"],
    item_sparse: ["AmmunitionType"],
};

function splitTuple(tuple) {
    const out = [];
    let cur = "";
    let q = false;
    for (let i = 0; i < tuple.length; i++) {
        const ch = tuple[i];
        if (ch === "\\" && q) { cur += ch + tuple[i + 1]; i++; continue; }
        if (ch === "'") { q = !q; cur += ch; continue; }
        if (!q && ch === ",") { out.push(cur); cur = ""; continue; }
        cur += ch;
    }
    out.push(cur);
    return out;
}

// Wyciaga kolejne krotki "(...)" z jednej linii INSERT-a
function tuplesOf(body) {
    const out = [];
    let depth = 0, q = false, start = -1;
    for (let i = 0; i < body.length; i++) {
        const ch = body[i];
        if (ch === "\\" && q) { i++; continue; }
        if (ch === "'") { q = !q; continue; }
        if (q) continue;
        if (ch === "(") { depth++; if (depth === 1) start = i; }
        else if (ch === ")") {
            depth--;
            if (depth === 0 && start >= 0) { out.push(body.slice(start + 1, i)); start = -1; }
        }
    }
    return out;
}

async function readTdbSchema(needed) {
    // schemat: tabela -> lista kolumn w kolejnosci; dane: tabela -> Map(id -> wartosci)
    const schema = {}, data = {};
    // UWAGA: dumpy TDB maja INSERT-y dlugosci setek kB w JEDNEJ linii.
    // readline dzieli je na kawalki, wiec czytamy caly plik i dzielimy po ";\n".
    const raw = fs.readFileSync(TDB, "utf8").replace(/\r\n/g, "\n");
    let cur = null;
    for (const line of raw.split("\n")) {
        const mc = line.match(/^CREATE TABLE `([^`]+)` \(/);
        if (mc) { cur = needed.includes(mc[1]) ? mc[1] : null; if (cur) schema[cur] = []; continue; }
        if (cur) {
            if (line.startsWith(")")) { cur = null; continue; }
            const m = line.match(/^\s+`([^`]+)`\s+(\S+)/);
            if (m) schema[cur].push({ col: m[1], type: m[2] });
            continue;
        }
        for (const t of needed) {
            const pre = "INSERT INTO `" + t + "`";
            if (!line.startsWith(pre)) continue;
            let header = null, bodyStart = -1;
            if (line.startsWith(pre + " (")) {              // INSERT INTO `t` (`col`,...) VALUES (...),(...);
                const p = line.indexOf("(");
                header = line.slice(p + 1, line.indexOf(")", p)).split(",").map(x => x.replace(/`/g, "").trim());
                bodyStart = line.indexOf(")", p) + 1;
            } else if (line.startsWith(pre + " VALUES ")) {  // INSERT INTO `t` VALUES (...),(...);
                header = schema[t] ? schema[t].map(c => c.col) : null;
                bodyStart = line.indexOf("VALUES") + 6;
            }
            if (!header) continue;
            const idIdx = header.indexOf("ID");
            if (idIdx < 0) continue;
            if (!data[t]) data[t] = new Map();
            for (const tup of tuplesOf(line.slice(bodyStart))) {
                const v = splitTuple(tup);
                if (v.length !== header.length) continue;
                data[t].set(v[idIdx].trim(), v);
            }
        }
    }
    return { schema, data };
}

(async () => {
    const needed = Object.keys(TABLES);
    const { schema, data } = await readTdbSchema(needed);

    // 1. wczytaj plik SQL i podziel na bloki INSERT per tabela
    const text = fs.readFileSync(SQL, "utf8");
    const crlf = text.includes("\r\n");
    const lines = text.replace(/\r\n/g, "\n").split("\n");
    const stats = {};

    let i = 0;
    while (i < lines.length) {
        const m = lines[i].match(/^INSERT INTO `([^`]+)` \(([^)]*)\) VALUES\s*$/);
        if (!m) { i++; continue; }
        const table = m[1];
        const header = m[2].split(",").map(x => x.replace(/`/g, "").trim());
        const fix = TABLES[table];
        const tdbSchema = schema[table];
        const tdbData = data[table];
        if (!fix || !tdbSchema || !tdbData) { i++; continue; }

        // granica bloku: ostatnia linia zaczynajaca sie od "("
        let end = i + 1;
        while (end < lines.length && lines[end].startsWith("(")) end++;

        const tdbCols = tdbSchema.map(c => c.col);
        const missing = tdbCols.filter(c => !header.includes(c) && c !== "ID" && c !== "VerifiedBuild");
        if (!missing.length) { i = end; continue; }

        // nowa lista kolumn w kolejnosci TDB (tylko kolumny znane + uzupelniane)
        const keep = new Set([...header, ...missing]);
        const newHeader = tdbCols.filter(c => keep.has(c));
        const idOld = header.indexOf("ID");

        let changed = 0, notInTdb = 0;
        const newRows = [];
        for (let r = i + 1; r < end; r++) {
            const line = lines[r];
            const endPos = Math.max(line.lastIndexOf("),"), line.lastIndexOf(");"));
            const tail = line.slice(endPos + 1);
            const vals = splitTuple(line.slice(1, endPos));
            if (vals.length !== header.length) { newRows.push(line); continue; }

            const id = vals[idOld].trim();
            const src = tdbData.get(id);
            if (!src) { notInTdb++; newRows.push(line); continue; }

            const byName = {};
            header.forEach((h, k) => byName[h] = vals[k]);
            let ch = false;
            for (const c of missing) {
                const t = tdbCols.indexOf(c);
                const sv = src[t];
                if (sv === undefined) continue;
                byName[c] = sv;
                if (sv !== "NULL") ch = true;
            }
            newRows.push("(" + newHeader.map(c => byName[c]).join(",") + ")" + tail);
            if (ch) changed++;
        }

        lines[i] = "INSERT INTO `" + table + "` (" + newHeader.map(c => "`" + c + "`").join(", ") + ") VALUES";
        for (let r = 0; r < newRows.length; r++) lines[i + 1 + r] = newRows[r];
        stats[table] = { rows: newRows.length, changed, notInTdb, added: missing };
        i = end;
    }

    fs.writeFileSync(SQL, crlf ? lines.join("\r\n") : lines.join("\n"), "utf8");
    for (const [t, s] of Object.entries(stats))
        console.log(`${t}: ${s.rows} wierszy, uzupelniono ${s.changed}, brak w TDB ${s.notInTdb} (dodane kolumny: ${s.added.join(", ")})`);
    if (!Object.keys(stats).length) console.log("nic do uzupelnienia");
})();
