// Zrzut pakietow z pliku .pkt do JSON (bez Pythona, ktory na tej maszynie nie dziala).
// Uzycie: node tools/pkt2json.cjs <plik.pkt> <wyjscie.json> [opcode-hex]
const fs = require('fs');

function readPkt(path) {
  const d = fs.readFileSync(path);
  if (d.subarray(0, 3).toString('latin1') !== 'PKT') throw new Error('to nie jest plik PKT');
  let off = 3;
  const version = d.readUInt16LE(off); off += 2;
  if (version === 0x201) { off += 2 + 40; }
  else if (version === 0x202) { off += 1 + 2 + 4 + 20 + 64; }
  else if (version === 0x300) { off += 1 + 4 + 4 + 40; const n = d.readInt32LE(off); off += 4 + n; }
  else if (version === 0x301) { off += 1 + 4 + 4 + 40 + 4 + 4; const n = d.readInt32LE(off); off += 4 + n; }
  else throw new Error('nieznana wersja PKT: 0x' + version.toString(16));

  const pkts = [];
  while (off + 8 <= d.length) {
    const magic = d.readUInt32LE(off); off += 4;
    let direction, opcode, length;
    if (version === 0x201 || version === 0x202) {
      off += 4 + 4;
      length = d.readInt32LE(off); off += 4;
      if (magic === 0xff) { opcode = d.readInt16LE(off); off += 2; length -= 2; }
      else { opcode = d.readInt32LE(off); off += 4; length -= 4; }
      direction = magic === 0xff ? 'SMSG' : 'CMSG';
    } else {
      direction = { 0x47534d53: 'SMSG', 0x47534d43: 'CMSG' }[magic] || 'BNET';
      off += 4 + 4;
      const additional = d.readInt32LE(off); off += 4;
      length = d.readInt32LE(off); off += 4;
      off += additional;
      opcode = d.readInt32LE(off); off += 4;
      length -= 4;
    }
    pkts.push({ direction, opcode, data: d.subarray(off, off + length).toString('hex') });
    off += length;
  }
  return pkts;
}

const [input, output, opcodeHex] = process.argv.slice(2);
const want = opcodeHex ? parseInt(opcodeHex, 16) : null;
const pkts = readPkt(input).filter(p => want === null || p.opcode === want);
fs.writeFileSync(output, JSON.stringify(pkts));
console.log(`${pkts.length} pakietow -> ${output}`);
