# WPP-full: what is still needed for a fully working parser (Forever 1.60.1)

Fork: https://github.com/devovh/WowPacketParser (module `WowPacketParserModule.V1_60_1_70888`)
Reference for layouts: tc-forever (`src/server/game/Server/Packets`). Where tc-forever has no layout, the layout was derived from the raw bytes.

This file lists what is **still missing** so the parser can read everything the official Forever server sends. The developer's working parser would help most with the items in section 2.

## 1. Current state

| Sniff | Packets | Parsed OK | With errors | Skipped (unnamed opcode) | No structure (named, hex only) |
|---|---|---|---|---|---|
| **70938**, 16.11 (new) | 271 433 | 271 072 (99.867%) | **0** | 329 | 32 |
| **70938**, 14.11 | 119 594 | 119 528 (99.945%) | **0** | 55 | 11 |
| 70862 (older build) | 312 210 | 311 315 (99.713%) | 571 (0.183%) | 257 | 67 |

- 70938 is the target. Work orders 1-5 and your replies 1-3 (layouts, trainers) are implemented. SQL output of the new sniff: 13 006 rows, 72 tables, 0 bad rows (`tools/check-sql.cjs`).
- 70862 is a side target (section 3).
- "OK" means each packet was read to the end, not that every field is understood. Skipped and no-structure packets are not read at all.

## 2. Needed for 70938 (priority)

### 2.1 Opcodes with no name
Only 4 server opcodes are still unnamed: `0x470377`, `0x470228`, `0x4C0005`, `0x470059`. Samples are in `UNKNOWN-OPCODES-70938.md` (regenerate with `node tools/list-unknown-opcodes.cjs`).

**Closed (2026-11-16):** the developer has nothing on those numbers on his side either, so they stay unnamed and go out as raw hex. `SMSG_BATTLEFIELD_LIST` deliberately stays on `0x4C0005`. Do not guess names for them; if a name ever arrives, add it together with a reader.

### 2.2 Named opcodes with no structure (7 opcodes)
Names confirmed by the developer on 2026-11-16 — they are not shift leftovers. Settled:
- `CMSG_MOVE_ENABLE_FULL_SPEED_PITCHING_ACK` 0x420083: MovementInfo + `i32 AckIndex`, read like every other `MOVE_*_ACK` (`ReadMovementAck`). Done.
- `SMSG_RECENT_ALLY_DATA_RESPONSE` 0x470364: `u32`, `u8` (7 = list valid), `u32 Count`, then the entries; live sends one extra trailing byte he cannot explain, read as `Unk2`. Done.
- `SMSG_SYNC_WOW_ENTITLEMENTS` 0x470300, `SMSG_RAF_ACCOUNT_INFO` 0x4702ED, `SMSG_PAST_TIME_EVENTS` 0x470066, `SMSG_WEEKLY_REWARDS_PROGRESS_RESULT` 0x47030B, `SMSG_RAF_DEBUG_FRIEND_MONTHS` 0x470347: names right, but the server never sends them, so no layout can be proven. They stay hex-only on purpose.

### 2.3 Layouts that are guessed or unverified
- ~~`SMSG_AUCTION_LIST_BUCKETS_RESULT` (0x4702F7): the 2/7/12-byte block after the ItemID~~ — CLOSED: it is an `ItemInstance` (the same one as in `CMSG_BUY_ITEM`), so the 2/7/12 bytes are 0/1/2 mods. **Already implemented** in `ForeverAuctionHandler.cs` (commit `a605bde21`). Verified on the 16.11 capture: 2 316 buckets with the block, `ModifierCount` 0 (1 719) or 1 (642), `HasBonus` set in 1 424 of them, and the whole capture ends with 0 bytes left over.
- `SMSG_REQUEST_PVP_REWARDS_RESPONSE`: the last byte (0xF8) is not understood.
- `SMSG_RATED_PVP_INFO`: not cross-checked with server code.
- `SMSG_FEATURE_SYSTEM_STATUS`: follows tc-forever `SystemPackets.cpp:123`; verified on one capture.

### 2.3a Two client opcodes we could not lay out — CLOSED 2026-11-17

The developer supplied both field lists on 2026-11-17 and both fit the bytes exactly.

- **`CMSG_BUY_ITEM` (0x3F003B)** — `Guid Forever VendorGUID`, `Guid Forever ContainerGUID`, `ItemInstance Item`, `i32 Quantity`, `u32 Muid`, `u32 Slot`, `i32 ItemType`. The three packets (43/41/37 bytes) come out at 12/10/11 guid bytes with the item mod list; all three end on the last byte. Read in `ForeverItemHandler.cs`.
- **`CMSG_CHAT_MESSAGE_PARTY` (0x2D0026)** — `i32 Language`, `11 bits TextLength`, `1 bit IsSecure`, flush, `TextLength` bytes of text. The mysterious 0xA0 was the low half of the 11-bit length plus IsSecure. The same layout (without IsSecure) covers SAY, RAID, RAID_WARNING and INSTANCE_CHAT. Read in `ForeverChatHandler.cs`.

After both: 17.11 11:53 has **0 errors and 0 "not fully read"**, and no capture regressed.

### 2.4 Data the sniff does not contain (not parser bugs, need new captures or server knowledge)
- `npc_vendor.incrtime` is always 0: the capture has no `SMSG_VENDOR_INVENTORY` for vendors that restock.
- `creature_loot_template`: 5 rows have no data; the drop chance is only an estimate (drops / kills).
- `creature_classic_level_spawn` is not implemented.
- 1 gossip row has `OptionBroadcastTextID` = 0.
- The LootID mapping (`tools/fix-loot-id.cjs`) was verified only where LootID == creature entry.

## 3. Needed for 70862 (secondary — closed 2026-11-16)

The developer's server only targets 70938 and he has no 70862 layouts for us, so this section is history, not a work item. Nothing here is being pursued.
The parser status line reports 571 packets with errors. Counting error markers in the `_parsed.txt` log gives about 890 (a packet can log an exception and nested warnings, and some exceptions are caught inside handlers), so treat the per-opcode numbers below as "where the problems are", not as an exact partition of the 571.

| Opcode | Error markers | Cause / state |
|---|---|---|
| `SMSG_UPDATE_OBJECT` | 665 | `PlayerData` of the active player (ThisIsYou) is read wrongly after `GuildTimeStamp` (the descriptor of 70862 has more data there than 70938). Two experiments did not help: using the 70938 fragment path, and always reading `ModSpellHaste` for the Owner. After `ModTimeRate` the values are shifted (e.g. `BaseMana` = 83886080). Creature `UnitData` is already correct. Other players (non-owner) also still drift in some packets. |
| `SMSG_QUERY_CREATURE_RESPONSE` | 124 | `EndOfStreamException` while reading the display array. Not investigated yet. |
| `SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE` | 84 | Layout differs from the 7.x template: the bytes show a count, then fixed-size entries with a ticket and `Party` members. Not in tc-forever and not in 70938. |
| `SMSG_AUCTION_LIST_ITEMS_RESULT` | 6 | not investigated |
| `SMSG_CORPSE_LOCATION` | 4 | not investigated |
| `CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE` | 3 | read as a single u32 in 70938 (guess) |
| `SMSG_INSPECT_RESULT` | 3 | not investigated |
| `CMSG_CAN_DUEL` | 2 | not investigated |
| 5 others, 1 each | 5 | `SMSG_PARTY_INVITE`, `CMSG_LFG_LIST_SEARCH`, `SMSG_LFG_LIST_SEARCH_RESULTS`, `CMSG_MOVE_SET_ADV_FLYING_AIR_FRICTION_ACK`, `CMSG_AUCTION_SELL_ITEM` |

Already fixed for 70862 (in git, commits `be171cfff` to `74a37e9c4`): creature UnitData layout, player-name lookup, contact list, action button, channel list, guild rewards list, logout, faction standing, auction list requests.

## 4. What I would ask the developer
1. Your Stormwind/Westfall enUS `.pkt`, or the pet, stable and mail layouts, so we can fix your remaining ~179 errors.
2. Names and layouts for the 4 unnamed opcodes (2.1) and the 7 hex-only opcodes (2.2).
3. Confirmation of two guesses: `SMSG_DUEL_WINNER` (5 unknown bytes before the names) and `CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE` (single u32).
4. The update-field layout of ActivePlayer/Player for 70862 (low priority), to close the `SMSG_UPDATE_OBJECT` errors there.
5. A decision on the auction extra block (2.3): does the server know what these 2/7/12 bytes are?
6. ~~The field lists for `CMSG_BUY_ITEM` and `CMSG_CHAT_MESSAGE_PARTY`~~ — answered 2026-11-17, both implemented, see 2.3a.
7. Another capture, see `SNIFFING-CHECKLIST.md`: restocking vendors (wait 10 minutes), 50+ kills of the same mob, a book for `page_text`, pets/stable/mail, auction house opened more than once.

## 5. How to reproduce
```
dotnet build WowPacketParser.slnx -c Release
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config --DumpFormat 1 --SQLFileName forever_70938.sql "<dump>.pkt"
node tools/patch-from-tdb.cjs && node tools/fix-loot-id.cjs && node tools/check-sql.cjs out/forever_70938.sql
dotnet test WowPacketParser.Tests
```
`--DumpFormat 1` writes `<dump>_parsed.txt` next to the capture; grep it for `Packet not fully read!` (errors) or for a bare number instead of an opcode name (unnamed).
Config: `TargetedProject=TrinityCore`, `TargetedDatabase=24` (Forever).
