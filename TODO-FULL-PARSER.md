# WPP-full: what is still needed for the parser to work fully correctly

Status date: 2026-11-15. Repo: https://github.com/devovh/WowPacketParser (module `WowPacketParserModule.V1_60_1_70888`).
Target build: **Forever 1.60.1.70938** (live). Reference: tc-forever packet sources and the developer's replies
(`docs/DEVELOPER-OPCODES-70938-REPLY.md`).

## 1. Current state (dump 70938)
- 119 368 of 119 594 packets parsed OK (**99.81%**), **0 packets with errors**.
- Skipped (unnamed opcode): 58 packets. No structure (named, hex only): 168 packets.
- SQL output: 9 137 rows, 63 tables, 0 bad rows (`tools/check-sql.cjs`). Tests: 98 passed, 0 failed, 12 skipped.
- "OK" means the packet was read to the end, not that every field is understood.

## 2. Still to do for 70938

### 2.1 Packets with a name but no structure (28 opcodes, 168 packets)
Full list with sample bytes: `UNKNOWN-OPCODES-70938.md` (regenerate with `node tools/list-unknown-opcodes.cjs <parsed.txt> <out.md>`).
Done from bytes since the last version: `SMSG_ACTIVE_SCHEDULED_WORLD_STATE_INFO` (always `u32 Count = 0`, 225 packets) and `SMSG_UPDATE_RECENT_PLAYER_GUIDS` (8 zero bytes, meaning unknown). Biggest groups, and what is missing:

| Opcode | Packets | Status |
|---|---|---|
| `SMSG_LATENCY_REPORT_PING` 0x470316 | 66 | no layout (B). Its sample bytes are identical to `CMSG_QUERY_PLAYER_NAMES_FOR_COMMUNITY`, so one of the two names is probably wrong |
| `CMSG_QUERY_PLAYER_NAMES_FOR_COMMUNITY` 0x46000F | 66 | no layout (B) |
| others (RAF, perks, delves, BattlePay, sync entitlements, recent ally list, weekly rewards, ...) | 1-3 each | developer confirmed names only (B), no layouts |

Effect: none of these feed a SQL table, so the database output is not affected. They only matter for a complete log.

### 2.2 Opcodes nobody knows (6 unnamed, 8 packets)
Developer also does not know: S 0x470377, S 0x470228, S 0x4C0005, S 0x470059, C 0x3E013D. Needs more captures (and a look at what the client does right before or after) to identify them.

### 2.3 Names not proven
`0x440134`/`0x440136` (CLIENT: SET_EXCLUDED_CHAT_CENSOR_SOURCES / BATTLE_PAY_REQUEST_PRICE_INFO), `0x4400E6` (GET_ACCOUNT_CHARACTER_LIST), `0x440153` (CLUB_FINDER...POSTING_IDS), `0x44019F` (RECENT_ALLY_REQUEST_DATA). Also `0x440126` is `CMSG_CLUB_PRESENCE_SUBSCRIBE` in our table, but the developer's list says `CMSG_BATTLENET_REQUEST` for the same number: still to be settled.

### 2.4 Layouts not yet done
- `CMSG_USE_ITEM` (0x3E0171): `u8 PackSlot, u8 Slot, guid CastItem, SpellCastRequest`; needs the Forever `SpellCastRequest` layout.
- `SMSG_GUILD_BANK_QUERY_RESULTS` (0x530025): only the empty case is in the capture; the full layout (tabs, items) is untested.
- `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT`, `SMSG_AUCTION_FAVORITE_LIST`, `SMSG_LOAD_EQUIPMENT_SET`: only the empty case is read, non-empty falls back to "read to end".
- `SMSG_REQUEST_PVP_REWARDS_RESPONSE`: last byte (0xF8) not understood. `SMSG_RATED_PVP_INFO`: not cross-checked with server code.
- Auction bucket extra block is now read as an ItemInstance as the developer described; checked only that parsing ends cleanly, not field by field.

### 2.5 Data missing from the sniff (not parser bugs, needs new captures)
- `npc_vendor.incrtime` = 0 (no `SMSG_VENDOR_INVENTORY` for restocking vendors).
- `creature_loot_template`: drop chance is only an estimate (drops / kills); 5 rows have no data.
- `creature_classic_level_spawn` is not implemented.
- 1 gossip row has `OptionBroadcastTextID` = 0.
- LootID mapping (`tools/fix-loot-id.cjs`) verified only where LootID equals the creature entry.
- Needed capture: restocking vendors, many kills with loot, several cities and instances, the auction house opened a few times.

## 3. Build 70862 (older, low priority)
574 packets still have errors; only the 70938 layout is known to the developer.
- `SMSG_UPDATE_OBJECT`: the owner's `PlayerData` is read wrongly after `GuildTimeStamp`. In 70938 the developer says there is a block only for the owner and party members (175 quest log entries, quest id to index map, quest session log size); 70862 differs from that.
- `SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE` (68): layout differs from the 7.x template and from 70938.
- `SMSG_QUERY_CREATURE_RESPONSE` (about 124 error markers): not investigated.
- A few small ones: `SMSG_AUCTION_LIST_ITEMS_RESULT`, `SMSG_CORPSE_LOCATION`, `SMSG_INSPECT_RESULT`, `CMSG_CAN_DUEL`, `SMSG_PARTY_INVITE`, `CMSG_AUCTION_SELL_ITEM`, `CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE`.
- The opcode table is shared with 70938, so some 70862 packets may now use 70938 names or layouts.

## 4. Process
- After every change: build, parse 70938 (must stay at 0 errors), `dotnet test WowPacketParser.Tests`, `node tools/check-sql.cjs out/forever_70938.sql`.
- If the Forever client is updated, packet layouts and opcode numbers may change and must be re-checked.
- Commits `c53066815` and `ca48076a4` may still need `git push devovh master`.
