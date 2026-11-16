// Przepisuje klucz `Entry` w creature_loot_template z entry stworzenia na LootID (creature_template_difficulty.LootID, DifficultyID 0)
// wg TDB world. Wiersze, dla ktorych TDB nie zna LootID (0 lub brak), zostaja z entry i dostaja komentarz "LootID?".
// Gdy dwa stworzenia dziela LootID, wiersze (LootID, Item) sa laczone: bierzemy wyzsza szanse.
//
// Uzycie: node tools/fix-loot-id.cjs [plik.sql] [TDB_world.sql]
const fs = require("fs");
const { execFileSync } = require("child_process");

const SQL = process.argv[2] || "out/forever_70938.sql";
const TDB = process.argv[3] || "C:/Users/devov/Desktop/TDB_full_forever_2026_10_14/TDB_full_world_forever_2026_10_14.sql";

let text = fs.readFileSync(SQL, "utf8");
const crlf = text.includes("\r\n");
const lines = text.split(/\r?\n/);

const start = lines.findIndex(l => /^INSERT (IGNORE )?INTO `creature_loot_template`/.test(l));
if (start < 0) { console.log("brak creature_loot_template"); process.exit(0); }
let end = start + 1;
while (end < lines.length && lines[end].startsWith("(")) end++;

const rowRe = /^\((\d+), (\d+), (\d+), ([\d.]+), (\d+), (\d+), (\d+), (\d+), (\d+)\)[;,]( -- .*)?$/;
const rows = [];
for (let i = start + 1; i < end; i++) {
    const m = rowRe.exec(lines[i]);
    if (!m) throw new Error("nie rozumiem wiersza: " + lines[i]);
    rows.push({ entry: +m[1], type: +m[2], item: +m[3], chance: +m[4], qr: +m[5], mode: +m[6], grp: +m[7], min: +m[8], max: +m[9], comment: m[10] || "" });
}
const need = new Set(rows.map(r => r.entry));

// tylko linie INSERT dla creature_template_difficulty (reszta pliku 600 MB jest zbedna)
const raw = execFileSync("grep", ["-a", "^INSERT INTO `creature_template_difficulty`", TDB], { maxBuffer: 1 << 30 }).toString("latin1");
const lootOf = new Map();
for (const line of raw.split("\n")) {
    const body = line.slice(line.indexOf("VALUES") + 6);
    const re = /\((\d+),(-?\d+),(?:-?[\d.]+,){12}(\d+),/g;   // Entry, DifficultyID, 12 pol (Min..TypeFlags3), LootID
    let m;
    while ((m = re.exec(body))) {
        if (m[2] === "0" && need.has(+m[1])) lootOf.set(+m[1], +m[3]);
    }
}

const merged = new Map();
const unknown = [];
for (const r of rows) {
    const loot = lootOf.get(r.entry);
    let key = loot;
    let comment = r.comment;
    if (!loot) { key = r.entry; comment += " (LootID? brak w TDB, zostaje entry)"; unknown.push(r.entry); }
    else if (loot !== r.entry) comment += ` (entry ${r.entry})`;
    const k = key + ":" + r.type + ":" + r.item;
    const old = merged.get(k);
    if (!old || r.chance > old.chance) merged.set(k, { ...r, entry: key, comment });
}

const out = [...merged.values()].sort((a, b) => a.entry - b.entry || a.item - b.item);
// creature_loot_template nie ma klucza glownego (tylko KEY), wiec INSERT IGNORE nie chroni przed duplikatami przy
// ponownym imporcie. Kazdy wiersz idzie jako INSERT ... SELECT ... WHERE NOT EXISTS po (Entry, ItemType, Item):
// istniejace dropy (w tym questowe) zostaja, a drugi import niczego nie dodaje.
const cols = "`Entry`, `ItemType`, `Item`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`";
const outLines = out.map(r =>
    `INSERT INTO \`creature_loot_template\` (${cols}) SELECT ${r.entry}, ${r.type}, ${r.item}, ${r.chance}, ${r.qr}, ${r.mode}, ${r.grp}, ${r.min}, ${r.max} FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM \`creature_loot_template\` WHERE \`Entry\` = ${r.entry} AND \`ItemType\` = ${r.type} AND \`Item\` = ${r.item});${r.comment}`);

let from = start;
if (lines[start - 1] && lines[start - 1].startsWith("DELETE FROM `creature_loot_template`")) from = start - 1;
lines.splice(from, end - from, ...outLines);
fs.writeFileSync(SQL, lines.join(crlf ? "\r\n" : "\n"));
console.log(`creature_loot_template: ${rows.length} wierszy -> ${out.length}, LootID znane dla ${lootOf.size}/${need.size} stworzen` + (unknown.length ? `, bez LootID: ${[...new Set(unknown)].join(",")}` : ""));
