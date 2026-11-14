#!/usr/bin/env node
// parse_masks.cjs - wyciaga z UpdateFields.cpp tabele remapu bitow retail->Classic
// i grupy klasyczne dla Unit/Player/ActivePlayer.

const fs = require('fs');
const SRC = 'C:/Users/devov/Desktop/WoWSniff/forevercore/src/server/game/Entities/Object/Updates/UpdateFields.cpp';
const src = fs.readFileSync(SRC, 'utf8');

function grabArray(name) {
    // szukamy deklaracji "= \r\n{ {" po nazwie, zeby nie trafic w uzycie zmiennej
    const declRe = new RegExp(name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*=\\s*\\r?\\n\\s*\\{');
    const m0 = declRe.exec(src);
    if (!m0) return null;
    const startIdx = src.indexOf('{', m0.index + m0[0].length - 1);
    let depth = 0, end = -1, started = false;
    for (let k = startIdx; k < src.length; k++) {
        if (src[k] === '{') { depth++; started = true; }
        else if (src[k] === '}') { depth--; if (started && depth === 0) { end = k; break; } }
    }
    const body = src.slice(startIdx, end + 1);
    const pairs = [];
    const re = /\{\s*(\d+)\s*,\s*(\d+)\s*\}/g;
    let m;
    while ((m = re.exec(body))) pairs.push([+m[1], +m[2]]);
    return pairs;
}

function grabFunc(name) {
    const i = src.indexOf(name + '(uint32 bit)');
    if (i < 0) return null;
    // cofnij do 'static uint32'
    let start = src.lastIndexOf('static uint32', i);
    if (start < 0) start = i;
    let depth = 0, started = false;
    for (let k = start; k < src.length; k++) {
        if (src[k] === '{') { depth++; started = true; }
        else if (src[k] === '}') { depth--; if (started && depth === 0) { return src.slice(start, k + 1); } }
    }
    return null;
}

const out = {
    groups: {
        PlayerData: grabArray('ClassicPlayerDataGroups'),
        UnitData70425: grabArray('ClassicUnitDataGroups70425'),
        UnitData70291: grabArray('ClassicUnitDataGroups70291'),
        UnitData70009: grabArray('ClassicUnitDataGroups70009'),
        ActivePlayerData70425: grabArray('ClassicActivePlayerDataGroups70425'),
        ActivePlayerData: grabArray('ClassicActivePlayerDataGroups'),
    },
    remaps: {
        PlayerData: grabFunc('ClassicPlayerDataBit'),
        UnitData70009: grabFunc('ClassicUnitDataBit70009'),
        UnitData: grabFunc('ClassicUnitDataBit'),
        ActivePlayerData: grabFunc('ClassicActivePlayerDataBit'),
    }
};

fs.writeFileSync('C:/Users/devov/Desktop/WPP-full/forever-masks.json', JSON.stringify(out, null, 1));

for (const [k, v] of Object.entries(out.groups))
    console.log(k.padEnd(24), v ? v.length + ' grup: ' + JSON.stringify(v) : 'BRAK');
console.log('');
for (const [k, v] of Object.entries(out.remaps))
    console.log(k.padEnd(24), v ? v.split('\n').length + ' linii' : 'BRAK');
