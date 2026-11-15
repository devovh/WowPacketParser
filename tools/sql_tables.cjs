#!/usr/bin/env node
// Sprawdza, które tabele hotfixów (DB2) sniff 70938 realnie przynosi,
// mapując "INSERT INTO `x`" z SQL-a na punkty wejścia parsera hotfixów.
// Szybki skrót: sprawdzenie czy moduł 12.0 ma handler dla danego DB2Hash
// jest tu tylko przybliżeniem — dokładniej: w logu -v liczba "Write Hotfixes"
// i zawartość pliku SQL.
const fs = require('fs');

const sqlFile = process.argv[2];
const hotspotFile = process.argv[3] ?? 'WowPacketParserModule.V12_0_0_65390/Parsers/HotfixHandler.cs';

const sql = fs.readFileSync(sqlFile, 'utf8');
const ins = [...sql.matchAll(/INSERT INTO `([a-z0-9_]+)`/g)].map(m => m[1]);
const counts = {};
for (const t of ins) counts[t] = (counts[t] ?? 0) + 1;

const src = fs.readFileSync(hotspotFile, 'utf8');
const handlers = [...src.matchAll(/case DB2Hash\.(\w+):/g)].map(m => m[1]);

const hi = Object.keys(counts).filter(t => handlers.some(h => h.replace(/([a-z])([A-Z])/g, '$1_$2').toLowerCase() === t));

console.log(`tabel w SQL: ${Object.keys(counts).length}, INSERT-ów: ${ins.length}`);
console.log(`handlerów DB2Hash w 12.0: ${handlers.length}`);
console.log(`tabel wyglądających na hotfix/DB2: ${hi.length}`);
console.log(hi.map(t => `  ${t} x${counts[t]}`).join('\n'));
