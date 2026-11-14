#!/usr/bin/env node
// parse_fields2.cjs - ekstraktor z prawdziwym drzewem zagniezdzen.
//
// Poprzednia wersja trzymala plaska liste warunkow i gubila:
//  - wiazanie && mocniej niz || (WorldEffects.HasChanged(i) || ignoreNestedChangesMask)
//  - petle for z licznikiem
//  - wywolania ReadUpdateMask/WriteUpdateMask (dynamiczne listy)
//  - bity grupowe klasyczne (classicGroup128 itd.)
//
// Ta wersja buduje drzewo: kazdy wezel to if/for/block, lisc to operacja na data.

const fs = require('fs');

const SRC = 'C:/Users/devov/Desktop/WoWSniff/forevercore/src/server/game/Entities/Object/Updates/UpdateFields.cpp';
const src = fs.readFileSync(SRC, 'utf8');

// --- tokenizacja po liniach, ale z liczeniem nawiasow klamrowych ---
const lines = src.split('\n');

function findFunc(sig) {
    const idx = src.indexOf(sig);
    if (idx < 0) return null;
    let line = src.slice(0, idx).split('\n').length - 1;
    // cofnij sie do linii z 'void'
    while (line > 0 && !lines[line].includes('::WriteUpdate') && !lines[line].includes(sig.slice(0, 20))) line--;
    let depth = 0, started = false;
    for (let i = line; i < lines.length; i++) {
        for (const ch of lines[i]) {
            if (ch === '{') { depth++; started = true; }
            else if (ch === '}') depth--;
        }
        if (started && depth === 0) return { start: line, end: i };
    }
    return null;
}

// Buduje drzewo z zakresu linii
function buildTree(start, end) {
    const root = { type: 'block', children: [] };
    const stack = [root];
    let pendingHeader = null;   // zebrany naglowek if/for przed '{'

    const cur = () => stack[stack.length - 1];

    for (let i = start; i <= end; i++) {
        let raw = lines[i];
        const t = raw.trim();
        if (!t || t.startsWith('//')) continue;

        // Rozbijamy linie na segmenty po '{' i '}' zeby obsluzyc jednolinijkowe bloki
        // Najpierw: czy linia konczy sie na '{' (naglowek bloku)
        const headerM = t.match(/^(if|for|while|else if)\s*\((.*)\)\s*$/) ||
                        t.match(/^(if|for|while|else if)\s*\((.*)\)\s*\{/);

        if (headerM && (t.endsWith('{') || t.endsWith(')'))) {
            const node = {
                type: headerM[1] === 'for' ? 'for' : (headerM[1] === 'else if' ? 'if' : headerM[1]),
                cond: headerM[2].trim(),
                line: i + 1,
                children: []
            };
            // Jesli linia konczy sie na '{' -> otworz blok
            if (t.endsWith('{')) {
                cur().children.push(node);
                stack.push(node);
            } else {
                // naglowek bez '{' w tej linii - '{' w nastepnej
                pendingHeader = node;
            }
            continue;
        }

        // '{' na poczatku linii po naglowku
        if (t === '{' && pendingHeader) {
            cur().children.push(pendingHeader);
            stack.push(pendingHeader);
            pendingHeader = null;
            continue;
        }

        // zamkniecie bloku
        if (t.startsWith('}')) {
            if (stack.length > 1) stack.pop();
            // '} else {' - obsluga
            if (t.includes('else') && t.includes('{')) {
                // traktuj jako osobny if bez warunku
                continue;
            }
            continue;
        }

        // Operacje na data
        let m;
        if ((m = t.match(/^data << (?:(\w+)\((.*?)\)|(.+?));/))) {
            cur().children.push({
                type: 'field', line: i + 1,
                ctype: m[1] || 'raw', expr: (m[2] !== undefined ? m[2] : m[3])
            });
        } else if ((m = t.match(/^data\.WriteBit\((.+?)\);/))) {
            cur().children.push({ type: 'bit', line: i + 1, expr: m[1] });
        } else if ((m = t.match(/^data\.WriteBits\((.+?),\s*(.+?)\);/))) {
            cur().children.push({ type: 'bits', line: i + 1, expr: m[1], count: m[2] });
        } else if ((m = t.match(/^(\w+)\.WriteUpdateMask\(data(?:,\s*(.+?))?\);/))) {
            cur().children.push({ type: 'dynmask', line: i + 1, name: m[1], bits: m[2] || '32' });
        } else if ((m = t.match(/^WriteCompleteDynamicFieldUpdateMask\((\w+)\.size\(\),\s*data(?:,\s*(.+?))?\);/))) {
            cur().children.push({ type: 'dynmask_full', line: i + 1, name: m[1], bits: m[2] || '32' });
        } else if ((m = t.match(/^(\w+)\[i\]\.WriteUpdate\((.+)\);/))) {
            cur().children.push({ type: 'dynitem', line: i + 1, name: m[1], args: m[2] });
        } else if ((m = t.match(/^(\w+)\->WriteUpdate\((.+)\);/))) {
            cur().children.push({ type: 'optitem', line: i + 1, name: m[1], args: m[2] });
        } else if ((m = t.match(/^data\.FlushBits\(\);/))) {
            cur().children.push({ type: 'flush', line: i + 1 });
        }
    }
    return root;
}

function countLeaves(n) {
    if (!n.children) return n.type === 'block' ? 0 : 1;
    return n.children.reduce((a, c) => a + countLeaves(c), 0);
}

const result = {};
for (const [key, sig] of [
    ['UnitData', 'void UnitData::WriteUpdate(Mask const& changesMask'],
    ['PlayerData', 'void PlayerData::WriteUpdate(Mask const& changesMask'],
    ['ActivePlayerData', 'void ActivePlayerData::WriteUpdate(Mask const& changesMask'],
]) {
    const r = findFunc(sig);
    if (!r) { console.error('NIE ZNALEZIONO: ' + sig); continue; }
    const tree = buildTree(r.start + 1, r.end - 1);
    result[key] = { lines: [r.start + 1, r.end + 1], nodes: countLeaves(tree), tree };
    console.log(key.padEnd(18), 'linie', String(r.start + 1).padStart(5) + '-' + String(r.end + 1).padStart(5), 'wezlow:', countLeaves(tree));
}

fs.writeFileSync('C:/Users/devov/Desktop/WPP-full/forever-fields.json', JSON.stringify(result, null, 1));
console.log('\nzapisano forever-fields.json');
