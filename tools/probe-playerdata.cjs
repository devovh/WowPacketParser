// Sonda 70959: przechodzi SMSG_UPDATE_OBJECT i pokazuje, gdzie konczy sie strumien.
// Bit-reader jak w WPP (MSB-first, bajt wczytywany leniwie).
//
// Uwaga o tym srodowisku: na tej maszynie "node" i ABACUSAI_BOT_NODE to rozne buildy
// i Buffer.from(hex,'hex') potrafi zjesc pierwszy bajt, a Buffer.readUInt32LE(offset)
// zwraca dane z innego miejsca. Dlatego hex zamieniamy recznie na tablice liczb,
// a wszystkie odczyty robimy bajt po bajcie.
// Uzycie: node tools/probe-playerdata.cjs <json z pkt2json> [indeks pakietu]
const fs = require('fs');

const hex = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'))[Number(process.argv[3] || 0)].data;
const data = [];
for (let i = 4; i + 1 < hex.length; i += 2)          // od 4: pomijamy naglowek opcodu w snifie
    data.push(parseInt(hex.substr(i, 2), 16));

let pos = 0, curByte = 0, bitPos = 8;
const byte = () => data[pos++];
const bits = (n) => {
    let v = 0n;
    for (let i = 0; i < n; i++) {
        if (bitPos === 8) { curByte = data[pos++]; bitPos = 0; }
        v = (v << 1n) | BigInt((curByte >> (7 - bitPos)) & 1);
        bitPos++;
    }
    return v;
};
const resetBits = () => { bitPos = 8; };
const u16 = () => byte() | (byte() << 8);
const u32 = () => { let v = 0; for (let i = 0; i < 4; i++) v |= byte() << (8 * i); return v >>> 0; };
const guid = () => {
    const mask = byte();
    const parts = [];
    for (let i = 0; i < 8; i++) if (mask & (1 << i)) parts.push(byte());
    return mask.toString(16) + ':' + parts.map(x => x.toString(16).padStart(2, '0')).join('');
};

const len = data.length;
const count = u32();
resetBits();
const map = u16();
const hasRemoved = Number(bits(1));
resetBits();
if (hasRemoved) {
    const destroyed = u16();
    const removed = u32();
    for (let i = 0; i < removed; i++) guid();
}
const dataSize = u32();
console.log(`count=${count} map=${map} hasRemoved=${hasRemoved} dataSize=${dataSize} len=${len} naglowek=${pos}`);

for (let obj = 0; obj < count; obj++) {
    const start = pos;
    const utype = byte();
    const g = guid();
    let objType = null, size = 0;
    if (utype === 0) size = u32();
    else if (utype === 1 || utype === 2) { objType = byte(); size = u32(); }
    else { console.log(`[${obj}] nieznany typ ${utype} na ${start}`); break; }
    const fieldsStart = pos;
    const end = fieldsStart + size;
    console.log(`[${obj}] off=${start} typ=${utype} objType=${objType} guid=${g} blok=${fieldsStart} size=${size} flags=${data[fieldsStart]} end=${end}`);
    if (end > len) { console.log(`   ! blok wychodzi za dane o ${end - len} B`); break; }
    pos = end;
}
console.log('koniec pos=', pos, 'len=', len, 'zostalo=', len - pos);
