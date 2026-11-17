// Syntetyczny sniff PKT 3.1 z NIEPUSTYMI wariantami czterech czytnikow, ktorych prawdziwy sniff
// nigdy nie trafil (zawsze Count=0). Payloady zbudowane DOKLADNIE wedlug ukladu dewelopera
// (docs/DEVELOPER-OPCODES-70938-REPLY-2.md, sekcje o banku, aukcjach i zestawach).
//
// Cel: sprawdzic, ze czytnik czyta wszystkie pola i konczy sie na ostatnim bajcie.
// To nie jest dowod na to, ze serwer tak wysyla - to test, czy nasz kod zgadza sie z dokumentacja.
//
// Uruchomienie: "$ABACUSAI_BOT_NODE" tools/make-synthetic-nonempty.cjs [plik.pkt]
const fs = require('fs');
const path = require('path');

const outPath = process.argv[2] || 'out/tmp/synthetic_nonempty.pkt';
const u8 = (v) => Buffer.from([v & 0xFF]);
const u16 = (v) => { const b = Buffer.alloc(2); b.writeUInt16LE(v); return b; };
const u32 = (v) => { const b = Buffer.alloc(4); b.writeUInt32LE(v); return b; };
const i32 = (v) => { const b = Buffer.alloc(4); b.writeInt32LE(v); return b; };
const u64 = (v) => { const b = Buffer.alloc(8); b.writeBigUInt64LE(BigInt(v)); return b; };

// ---- pisanie bitow (WowPacketParser czyta bity od najmlodszego) -------------
class BitWriter {
    constructor() { this.bytes = []; this.cur = 0; this.used = 0; }
    bit(v) {
        if (v) this.cur |= 1 << this.used;
        this.used += 1;
        if (this.used === 8) { this.bytes.push(this.cur); this.cur = 0; this.used = 0; }
    }
    bits(value, count) { for (let i = 0; i < count; i += 1) this.bit((value >> i) & 1); }
    flush() {
        if (this.used > 0) { this.bytes.push(this.cur); this.cur = 0; this.used = 0; }
        return Buffer.from(this.bytes);
    }
}

// Forever GUID: u16 maska, potem tyle bajtow, ile bitow w masce (bajt i na pozycji i).
function foreverGuid(low) {
    const bytes = [];
    let mask = 0;
    for (let i = 0; i < 8; i += 1) {
        const b = Number((BigInt(low) >> BigInt(8 * i)) & 0xFFn);
        if (b !== 0) { mask |= 1 << i; bytes.push(b); }
    }
    return Buffer.concat([u16(mask), Buffer.from(bytes)]);
}

// ItemInstance: i32 ItemID, 7 bitow ModifierCount, flush, modyfikatory, bit HasBonus, flush,
// [u8 Context, u32 n, n x i32 BonusListID]
function itemInstance(itemId, mods = [], bonus = null) {
    const head = [i32(itemId)];
    const bw = new BitWriter();
    bw.bits(mods.length, 7);
    head.push(bw.flush());
    for (const m of mods) head.push(Buffer.concat([u8(m.type), i32(m.value)]));
    const bw2 = new BitWriter();
    bw2.bit(bonus !== null);
    head.push(bw2.flush());
    if (bonus !== null) {
        head.push(u8(bonus.context));
        head.push(u32(bonus.list.length));
        for (const b of bonus.list) head.push(i32(b));
    }
    return Buffer.concat(head);
}

const records = [];
function pkt(dir, opcode, data) {
    const n = records.length;
    records.push(Buffer.concat([
        Buffer.from(dir, 'latin1'),
        u32(0), u32(100 + n), u32(9),
        Buffer.from('000000000000000000', 'hex'),
        u32(4 + data.length),   // length = opcode(4) + dane
        u32(opcode), data,
    ]));
}

// ============ 1) SMSG_GUILD_BANK_QUERY_RESULTS 0x530025, niepusty ============
// u64 Money, i32 Tab, i32 WithdrawalsRemaining, u32 TabCount, u32 ItemCount
// TabCount x { i32 TabIndex, 7b NameLen, 9b IconLen, flush, Name, Icon }
// ItemCount x { i32 Slot, ItemInstance, i32 Count, i32 EnchantmentID, i32 Charges,
//               i32 OnUseEnchantmentID, i32 Flags, 2b GemCount, bit Locked, flush,
//               GemCount x { u8 Slot, ItemInstance } }
// bit FullUpdate, flush
{
    const parts = [];
    parts.push(u64(1234567));            // Money
    parts.push(i32(0));                  // Tab
    parts.push(i32(5));                  // WithdrawalsRemaining
    parts.push(u32(2));                  // TabCount
    parts.push(u32(1));                  // ItemCount

    // tablica 0: "Mats" / "INV_Misc_Bag_01"
    const t0 = new BitWriter(); t0.bits(4, 7); t0.bits(16, 9);
    parts.push(i32(0), t0.flush(), Buffer.from('Mats', 'latin1'), Buffer.from('INV_Misc_Bag_01', 'latin1'));
    // tablica 1: "Gear" / "INV_Chest"
    const t1 = new BitWriter(); t1.bits(4, 7); t1.bits(10, 9);
    parts.push(i32(1), t1.flush(), Buffer.from('Gear', 'latin1'), Buffer.from('INV_Chest', 'latin1'));

    // jeden item: slot 5, item 2589 (Linen Cloth) z jednym modyfikatorem i bonusem, 1 gem
    parts.push(i32(5));
    parts.push(itemInstance(2589, [{ type: 3, value: 25 }], { context: 14, list: [1234] }));
    parts.push(i32(20));   // Count
    parts.push(i32(0));    // EnchantmentID
    parts.push(i32(-1));   // Charges
    parts.push(i32(0));    // OnUseEnchantmentID
    parts.push(i32(0));    // Flags
    const ib = new BitWriter(); ib.bits(1, 2); ib.bit(false);
    parts.push(ib.flush());
    parts.push(u8(3));                                    // GemSlot
    parts.push(itemInstance(12345, [], null));            // Gem

    const fb = new BitWriter(); fb.bit(true);
    parts.push(fb.flush());

    pkt('SMSG', 0x530025, Buffer.concat(parts));
}

// ============ 2) SMSG_AUCTION_FAVORITE_LIST 0x470304, niepusty =============
// u32 DesiredDelay, 7 bitow Count, flush, Count x { u32 Order, ItemID, ItemLevel, BattlePetSpeciesID, SuffixItemNameDescriptionID }
{
    const parts = [u32(300)];
    const bw = new BitWriter(); bw.bits(2, 7);
    parts.push(bw.flush());
    for (const [order, item, ilvl, pet, suffix] of [[1, 2589, 10, 0, 0], [2, 12345, 60, 1234, 7]]) {
        parts.push(u32(order), u32(item), u32(ilvl), u32(pet), u32(suffix));
    }
    pkt('SMSG', 0x470304, Buffer.concat(parts));
}

// ============ 3) SMSG_LOAD_EQUIPMENT_SET 0x4701BB, niepusty ===============
// u32 Count, Count x { i32 Type, u64 Guid, u32 SetID, u32 IgnoreMask,
//   19 x { guid Piece, i32 AppearanceID }, 2 x i32 Enchant,
//   i32 SecondaryShoulderAppearanceID, i32 SecondaryShoulderSlot,
//   i32 SecondaryWeaponAppearanceID, i32 SecondaryWeaponSlot,
//   bit HasAssignedSpecIndex, 8b NameLen, 9b IconLen, flush, [i32 AssignedSpecIndex], Name, Icon }
{
    const parts = [u32(1)];
    parts.push(i32(0));                       // Type
    parts.push(u64(0x0800000000000ABCn));     // Guid
    parts.push(u32(7));                       // SetID
    parts.push(u32(0));                       // IgnoreMask
    for (let p = 0; p < 19; p += 1) {
        // co drugi slot ma item, zeby sprawdzic i puste, i niepuste
        parts.push(p % 2 === 0 ? foreverGuid(0x0800000000000F00n + BigInt(p)) : foreverGuid(0n));
        parts.push(i32(p % 2 === 0 ? 1000 + p : 0));
    }
    parts.push(i32(1234), i32(5678));         // 2 x Enchant
    parts.push(i32(0), i32(0), i32(0), i32(0));
    const bw = new BitWriter();
    bw.bit(true);                             // HasAssignedSpecIndex
    bw.bits(4, 8);                            // NameLen "PvE"
    bw.bits(9, 9);                            // IconLen "INV_Chest"
    parts.push(bw.flush());
    parts.push(i32(2));                       // AssignedSpecIndex
    parts.push(Buffer.from('PvE', 'latin1'));
    parts.push(Buffer.from('INV_Chest', 'latin1'));
    pkt('SMSG', 0x4701BB, Buffer.concat(parts));
}

// ============ 4) SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT 0x4702FB, niepusty ==
// UWAGA: obecny czytnik dla ItemCount > 0 robi ReadToEnd() - ten pakiet ma pokazac,
// ze da sie go rozpisac na pola. Na razie laduje w "read to end", wiec test sprawdza
// tylko, ze parser nie rzuca wyjatku.
{
    const parts = [];
    parts.push(u32(1));       // ItemCount
    parts.push(u32(300));     // DesiredDelay
    // reszta wg ukladu AuctionItem - na razie dowolne, sensowne bajty
    parts.push(Buffer.alloc(64, 0xAA));
    const bw = new BitWriter(); bw.bit(false);
    parts.push(bw.flush());
    pkt('SMSG', 0x4702FB, Buffer.concat(parts));
}

const header = Buffer.concat([
    Buffer.from('PKT', 'latin1'),
    u16(0x301),
    Buffer.from([0x15]),                                        // ymir
    u32(70938),
    Buffer.from('enUS', 'latin1'),
    Buffer.alloc(40, 0),
    u32(0), u32(0),
    u32(6), Buffer.from([0x03, 0x01, 0x10, 0x0E, 0x00, 0x00]),
]);

fs.mkdirSync(path.dirname(outPath), { recursive: true });
fs.writeFileSync(outPath, Buffer.concat([header, ...records]));

const info = [
    'plik: ' + outPath,
    'pakietow: ' + records.length,
    '0x530025 GUILD_BANK_QUERY_RESULTS: 2 tablice, 1 item z 1 gemem, FullUpdate=true',
    '0x470304 AUCTION_FAVORITE_LIST: Count=2',
    '0x4701BB LOAD_EQUIPMENT_SET: Count=1, 19 czesci (co druga pusta), HasSpec=true',
    '0x4702FB BIDDED_ITEMS_RESULT: ItemCount=1 (czytnik robi ReadToEnd)',
    '',
].join('\n');
fs.writeFileSync(path.join(path.dirname(outPath), 'synthetic-nonempty-info.txt'), info);
process.stdout.write(info);
