#!/usr/bin/env node
// parse_fields.cjs - wyciaga z UpdateFields.cpp tabele changesMask[n] -> pole -> typ
// dla UnitData::WriteUpdate, PlayerData::WriteUpdate, ActivePlayerData::WriteUpdate.
//
// Wyjscie: JSON z lista pol w kolejnosci wystapienia w kodzie, z typem i nazwa.

const fs = require('fs');

const SRC = 'C:/Users/devov/Desktop/WoWSniff/forevercore/src/server/game/Entities/Object/Updates/UpdateFields.cpp';
const src = fs.readFileSync(SRC, 'utf8');
const lines = src.split('\n');

// Znajdz poczatek funkcji i jej koniec (bilans nawiasow klamrowych)
function findFunc(name) {
    const idx = src.indexOf(name);
    if (idx < 0) return null;
    const startLine = src.slice(0, idx).split('\n').length - 1;
    let depth = 0, started = false;
    for (let i = startLine; i < lines.length; i++) {
        for (const ch of lines[i]) {
            if (ch === '{') { depth++; started = true; }
            else if (ch === '}') { depth--; }
        }
        if (started && depth === 0) return { start: startLine, end: i };
    }
    return null;
}

// Z podanego zakresu wyciagnij pola: sciezka warunkow + operacja
function extractFields(r) {
    const out = [];
    const stack = [];       // {cond, depth}
    let depth = 0;

    for (let i = r.start; i <= r.end; i++) {
        const raw = lines[i];
        const t = raw.trim();
        if (!t || t.startsWith('//')) continue;

        // warunek
        const condM = t.match(/^if \((.+)\)$/);
        // wypisz pole
        const fieldM = t.match(/^data << (?:(\w+)\((.*?)\)|(.+?));\s*(?:\/\/.*)?$/);
        const writeBitM = t.match(/^data\.WriteBit\((.+)\);\s*(?:\/\/.*)?$/);
        const writeBitsM = t.match(/^data\.WriteBits\((.+?), (.+?)\);\s*(?:\/\/.*)?$/);

        if (condM) { stack.push({ cond: condM[1], openDepth: depth }); }
        else if (fieldM) {
            let type, expr;
            if (fieldM[1]) { type = fieldM[1]; expr = fieldM[2]; }
            else { type = 'raw'; expr = fieldM[3]; }
            out.push({ line: i + 1, kind: 'field', type, expr, conds: stack.map(s => s.cond) });
        }
        else if (writeBitM) {
            out.push({ line: i + 1, kind: 'bit', expr: writeBitM[1], conds: stack.map(s => s.cond) });
        }
        else if (writeBitsM) {
            out.push({ line: i + 1, kind: 'bits', expr: writeBitsM[1], count: writeBitsM[2], conds: stack.map(s => s.cond) });
        }

        // sledz glebokosc
        for (const ch of raw) {
            if (ch === '{') depth++;
            else if (ch === '}') {
                depth--;
                while (stack.length && stack[stack.length - 1].openDepth >= depth) stack.pop();
            }
        }
    }
    return out;
}

const result = {};
for (const [key, sig] of [
    ['UnitData', 'void UnitData::WriteUpdate(Mask const& changesMask'],
    ['PlayerData', 'void PlayerData::WriteUpdate(Mask const& changesMask'],
    ['ActivePlayerData', 'void ActivePlayerData::WriteUpdate(Mask const& changesMask'],
]) {
    const r = findFunc(sig);
    if (!r) { console.error('NIE ZNALEZIONO: ' + sig); continue; }
    const fields = extractFields(r);
    result[key] = { lines: [r.start + 1, r.end + 1], count: fields.length, fields };
    console.log(key.padEnd(18), 'linie', String(r.start + 1).padStart(5) + '-' + String(r.end + 1).padStart(5), 'pol:', fields.length);
}

fs.writeFileSync('C:/Users/devov/Desktop/WPP-full/forever-fields.json', JSON.stringify(result, null, 1));
console.log('\nzapisano forever-fields.json');
