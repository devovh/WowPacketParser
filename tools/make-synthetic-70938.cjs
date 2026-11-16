// Syntetyczny plik PKT 3.1 z DWOMA nowymi czytnikami 70938, na bajtach skopiowanych z prawdziwego snifa.
// Payloady (identyczne z dump_1.60.1.70938_2026-11-14_12-41-07.pkt):
//   SMSG_RECENT_ALLY_DATA_RESPONSE 0x470364: 08F568ED070000000000
//   CMSG_BATTLENET_REQUEST         0x440126: u64 Type, u64 ObjectId, u32 Token, u32 DataSize, protobuf
// Layout pliku (PKT 3.1): "PKT" u16 0x301 | snifferId u8 | build u32 | locale 4B | sessionKey 40B
//   | startTime u32 | startTick u32 | additionalLen u32 | additional
//   rekord: u32 length | "CMSG"/"SMSG" | u32 connIdx | u32 tick | u32 additionalSize | u32 opcode | dane
const fs = require('fs');
const path = require('path');

const outPath = process.argv[2] || 'out/tmp/synthetic_70938.pkt';
const u16 = (v) => { const b = Buffer.alloc(2); b.writeUInt16LE(v); return b; };
const u32 = (v) => { const b = Buffer.alloc(4); b.writeUInt32LE(v); return b; };

const records = [];
function pkt(dir, opcode, data) {
    const n = records.length;
    records.push(Buffer.concat([
        Buffer.from(dir, 'latin1'),             // "SMSG" / "CMSG"
        u32(0),                                 // connIdx
        u32(100 + n),                           // tickCount
        u32(9),                                 // additionalSize (ymir: 9 B)
        Buffer.from('000000000000000000', 'hex'), // 9 B: 8 x double time + dlugosc komentarza
        u32(12 + data.length),                  // length = 8 B (opcode u32 + reszta) + dane
        u32(opcode),
        data,
    ]));
}

// 1) SMSG_RECENT_ALLY_DATA_RESPONSE - prawdziwy pakiet 10 B z 14.11
pkt('SMSG', 0x470364, Buffer.from('08F568ED070000000000', 'hex'));

// 2) CMSG_BATTLENET_REQUEST #1 - Type 17059201880250384385, Token 1, protobuf 15 B
const pb1 = Buffer.from('0D4E42000015746670611D53556E65', 'hex');
const b1 = Buffer.alloc(24 + pb1.length);
b1.writeBigUInt64LE(17059201880250384385n, 0);
b1.writeBigUInt64LE(0n, 8);
b1.writeUInt32LE(1, 16);
b1.writeUInt32LE(pb1.length, 20);
pb1.copy(b1, 24);
pkt('CMSG', 0x440126, b1);

// 3) CMSG_BATTLENET_REQUEST #2 - Type 6370832657470717956, Token 3, protobuf 2 B
const pb2 = Buffer.from('0800', 'hex');
const b2 = Buffer.alloc(24 + pb2.length);
b2.writeBigUInt64LE(6370832657470717956n, 0);
b2.writeBigUInt64LE(0n, 8);
b2.writeUInt32LE(3, 16);
b2.writeUInt32LE(pb2.length, 20);
pb2.copy(b2, 24);
pkt('CMSG', 0x440126, b2);

const header = Buffer.concat([
    Buffer.from('PKT', 'latin1'),
    u16(0x301),
    Buffer.from([0x15]),                                        // sniffer id 'ymir'
    u32(70938),                                                 // build
    Buffer.from('enUS', 'latin1'),                              // locale
    Buffer.alloc(40, 0),                                        // session key
    u32(0), u32(0),                                             // startTime, startTick
    u32(6), Buffer.from([0x03, 0x01, 0x10, 0x0E, 0x00, 0x00]),  // snifferVersion 1.3 + 65 B komentarza (jak w snifie)
]);

fs.mkdirSync(path.dirname(outPath), { recursive: true });
fs.writeFileSync(outPath, Buffer.concat([header, ...records]));
const info = [
    'plik: ' + outPath,
    'pakietow: ' + records.length,
    '0x470364 payload: 08F568ED070000000000  (oczekiwane: Unk1=3983078664, Valid=7, Count=0, Unk2=0)',
    '0x440126 #1: DataSize=15, Data=0D4E42000015746670611D53556E65',
    '0x440126 #2: DataSize=2,  Data=0800',
    '',
].join('\n');
fs.writeFileSync(path.join(path.dirname(outPath), 'synthetic-info.txt'), info);
process.stdout.write(info);
