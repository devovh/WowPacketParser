# WPP-full: what is still needed for the parser to work fully correctly

Status date: 2026-11-15. Repo: https://github.com/devovh/WowPacketParser (module `WowPacketParserModule.V1_60_1_70888`).
Target build: **Forever 1.60.1.70938**. Reference: tc-forever sources and the developer's replies
(`docs/DEVELOPER-OPCODES-70938-REPLY.md`, `docs/DEVELOPER-OPCODES-70938-REPLY-2.md`).

## 1. Current state (dump 70938)
- 119 507 of 119 594 packets parsed OK (**99.93%**), **0 packets with errors**.
- Skipped (unnamed opcode): 55 packets. No structure (named, hex only): 32 packets.
- SQL output: 9 137 rows, 63 tables, 0 bad rows (`tools/check-sql.cjs`). Tests: 98 passed, 0 failed, 12 skipped.
- "OK" means read to the end without error, not that every field is understood. Several layouts are the developer's guesses (grade G).

## 2. Still to do for 70938

### 2.1 Layouts given by the developer but not yet written into the parser
**2026-11-16: everything in this section is already in the parser - see `TODO-AUDIT-2026-11-16.md`.**
Kept as the record of what the developer supplied. `CMSG_USE_ITEM` goes through the
V3_4_0_45166 fallback (`ItemHandler.cs:499` -> `ReadSpellCastRequest`), `SMSG_RECENT_ALLY_LIST`
and both auction/bank/equipment handlers live in `ForeverDeveloperLayouts3/4Handler.cs`.
The small grade-G layouts were checked on the capture's bytes (`out/tmp/dump-opcodes2.txt`);
`SMSG_BATCH_PRESENCE_SUBSCRIPTION` was wrong there (u64 Id) and is fixed. What is still missing
for the guild bank, auction lists and equipment sets is *non-empty* sample data, not code.

Full text in `docs/DEVELOPER-OPCODES-70938-REPLY-2.md`, sections 3 and 5.
- `CMSG_USE_ITEM` (0x3E0171): `u8 PackSlot, u8 Slot, guid CastItem, SpellCastRequest` (full SpellCastRequest layout given, grade A). Needs a Forever `SpellCastRequest` reader.
- `SMSG_RECENT_ALLY_LIST` (0x470366, 396 bytes, grade A) and `SMSG_RECENT_ALLY_DATA_RESPONSE`.
- `SMSG_GUILD_BANK_QUERY_RESULTS` (0x530025): full layout with tabs and items (only the empty case is in the capture).
- Non-empty `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT`, `SMSG_AUCTION_FAVORITE_LIST`, `SMSG_LOAD_EQUIPMENT_SET` (now only "read to end").
- Small guessed layouts (G) for about 20 packets with 1-2 samples each: `SMSG_MOVE_INITIAL_OBJECT_UPDATE_COMPLETE` (guid + u32), `SMSG_REGIONWIDE_CHARACTER_RESTRICTIONS_DATA` / `..._MAIL_DATA`, `SMSG_LAST_CATALOG_FETCH_RESPONSE` (i64 time), `SMSG_BATCH_PRESENCE_SUBSCRIPTION`, `CMSG_SERVER_VALIDATION_SIGNATURE_REQUEST`, `CMSG_GET_ACCOUNT_CHARACTER_LIST`, `CMSG_CLUB_FINDER_REQUEST_SUBSCRIBED_CLUB_POSTING_IDS` (u32), `CMSG_MOVE_SET_TURN_RATE_CHEAT` (float), `CMSG_MOVE_ENABLE_FULL_SPEED_PITCHING_ACK` (MovementInfo + i32), `CMSG_PERKS_PROGRAM_REQUEST_REFUND` (u8 role mask; the name is wrong for Classic), and others. These do not feed any SQL table.

### 2.2 Unknown opcodes (4 unnamed, 5 packets)
**Closed 2026-11-16** - the developer has no names for them either, so they go out as raw hex.
S 0x470377, S 0x470228, S 0x4C0005 (Classic-only chat message, payload u8; `SMSG_BATTLEFIELD_LIST`
is deliberately kept on this number), S 0x470059. Not a parser problem - the name does not exist yet.

### 2.3 Named, but no layout from either side (grade B)
**Closed 2026-11-16** - the server never sends them, so they stay as hex:
`SMSG_SYNC_WOW_ENTITLEMENTS` (1 208 bytes), `SMSG_RAF_ACCOUNT_INFO`, `SMSG_PAST_TIME_EVENTS`,
`SMSG_WEEKLY_REWARDS_PROGRESS_RESULT`, `SMSG_RAF_DEBUG_FRIEND_MONTHS`, `CMSG_UPGRADE_RUNEFORGE_LEGENDARY`.

### 2.4 Other open items
- `SMSG_REQUEST_PVP_REWARDS_RESPONSE`: last byte 0xF8 unexplained (the server does not send it).
- `SMSG_RATED_PVP_INFO`: size 666 proven, field order is from retail and unchecked.
- Auction bucket extra block: read as ItemInstance (layout confirmed by the developer); only checked that parsing ends cleanly.
- Names not proven (grade B): `0x440134`, `0x440136`, `0x4400E6`.

### 2.5 Data missing from the sniff (needs a new capture)
- `npc_vendor.incrtime` = 0 (no restocking vendors captured).
- `creature_loot_template`: chance is only drops / kills; 5 rows without data. LootID mapping verified only where LootID equals the creature entry.
- `creature_classic_level_spawn` not implemented; 1 gossip row with `OptionBroadcastTextID` = 0.
- Wanted capture: restocking vendors, many kills with loot, several cities and instances, the auction house opened a few times.
- **Non-empty guild bank, auction lists and equipment sets**: see section 6 of `SNIFFING-CHECKLIST.md`.
  Without them those four handlers stay "written from the documented layout", never "confirmed on bytes".

## 3. Build 70862 (older, low priority)
574 packets still have errors. The developer only has the 70938 layouts.
- `SMSG_UPDATE_OBJECT` (342): the owner's `PlayerData` is read wrongly after `GuildTimeStamp`.
- `SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE` (68), `SMSG_QUERY_CREATURE_RESPONSE` (about 124 markers), and a few small ones (`SMSG_AUCTION_LIST_ITEMS_RESULT`, `SMSG_CORPSE_LOCATION`, `SMSG_INSPECT_RESULT`, `CMSG_CAN_DUEL`, ...).
- The opcode table is shared with 70938, so some 70862 packets now use 70938 names.

## 4. Process
- After every change: build, parse 70938 (must stay at 0 errors), `dotnet test WowPacketParser.Tests`, `node tools/check-sql.cjs out/forever_70938.sql`.
- If the Forever client is updated, opcode numbers and layouts must be re-checked.
- Push local commits: `git push devovh master`.
