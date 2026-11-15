# WPP-full: what is still needed for a fully working parser (Forever 1.60.1)

Fork: https://github.com/devovh/WowPacketParser (module `WowPacketParserModule.V1_60_1_70888`)
Reference for layouts: tc-forever (`src/server/game/Server/Packets`). Where tc-forever has no layout, the layout was derived from the raw bytes.

This file lists what is **still missing** so the parser can read everything the official Forever server sends. The developer's working parser would help most with the items in section 2.

## 1. Current state

| Build | Packets | Parsed OK | With errors | Skipped (unnamed opcode) | No structure (named, hex only) |
|---|---|---|---|---|---|
| **70938** (current live build) | 119 594 | 117 421 (98.18%) | **0** | 1 127 | 1 046 |
| 70862 (older build) | 312 210 | 301 301 (96.51%) | 573 (0.18%) | 6 138 | 4 198 |

- 70938 is the target. Work orders 1-5 (creature_template scale/ContentTuningID, npc_vendor, creature_loot_template, gossip, waypoint_path) are implemented. SQL output: 9 137 rows, 63 tables, 0 bad rows (`tools/check-sql.cjs`).
- 70862 is a side target. The numbers above are after the latest fixes, but its largest error group is still open (section 3).
- "98.18% OK" does not mean everything is understood. It means each packet was read to the end. Skipped and no-structure packets are not read at all.

## 2. Needed for 70938 (priority)

### 2.1 Opcodes with no name (1 127 packets, 61 opcodes)
The parser has no opcode name for these, so the packets are skipped. Direction C = client to server, S = server to client. Largest by packet count or size:

| Dir | Opcode | Packets | Bytes (total) |
|---|---|---|---|
| C | 0x42007F | 460 | 26 423 |
| C | 0x460010 | 120 | 4 465 |
| C | 0x46000F | 66 | 10 230 |
| C | 0x3E0143 | 132 | 660 |
| C | 0x3E0144 | 104 | 1 847 |
| C | 0x3F00CD | 41 | 326 |
| S | 0x4701C0 | 34 | 1 222 |
| C | 0x460006 | 32 | 256 |
| C | 0x4400E0 | 4 | 2 816 |
| C | 0x3E0169 | 4 | 568 |
| C | 0x4400C6 | 1 | 498 |
| C | 0x460001 | 1 | 928 (one-off, right after auth) |
| S | 0x530028 | 2 | 168 |

The remaining ~48 opcodes have 1-10 packets each. The full list can be regenerated from `dump_..._parsed.txt` (lines whose name is a bare number).

**Request:** names and layouts for these opcodes, or the opcode table of your parser (client and server).

### 2.2 Named opcodes with no structure (1 046 packets, 39 opcodes)
These have a name but no parser, so only hex is printed. Largest:

| Opcode | Packets | Note |
|---|---|---|
| `CMSG_REQUEST_CHARACTER_GUILD_FOLLOW_INFO` 0x4400B8 | 338 | |
| `SMSG_UNKNOWN_53002C` 0x53002C | 338 | 68 747 bytes in total; probably guild or club related |
| `SMSG_ACTIVE_SCHEDULED_WORLD_STATE_INFO` 0x4701F3 | 225 | |
| `SMSG_LATENCY_REPORT_PING` 0x470316 | 66 | 8 844 bytes |
| `CMSG_CLUB_PRESENCE_SUBSCRIBE` 0x440126 | 15 | |
| `SMSG_UPDATE_RECENT_PLAYER_GUIDS` 0x470099 | 12 | |
| `SMSG_UNSET_INSTANCE_LEAVER` 0x470373 | 6 | 22 341 bytes: the name is almost certainly wrong, the opcode is shifted |
| `SMSG_SYNC_WOW_ENTITLEMENTS` 0x470300 | 1 | 1 208 bytes |
| `SMSG_RECENT_ALLY_LIST` 0x470366 | 1 | 396 bytes |
| `SMSG_GUILD_EVENT_DISBANDED` 0x530035 | 1 | 264 bytes: the name is probably wrong |

The rest are small (1-3 packets): RAF, perks, delves, BattlePay, guild name change, auction favorites/bidded items, etc.
Several names look misaligned by one position (Forever opcode numbers are retail minus 1, and some pairs are swapped), so the names in `Opcodes.cs` for 0x47xxxx and 0x53xxxx should be checked against your table.

### 2.3 Layouts that are guessed or unverified
- `SMSG_AUCTION_LIST_BUCKETS_RESULT` (0x4702F7): from bucket flags with bit 7 set, the client writes `u32 ItemID` plus a block of **2, 7 or 12 bytes of unknown content**. The length is chosen by a heuristic (the next bucket header must be sane). It was verified on one capture only. tc-forever does not write this block.
- `SMSG_REQUEST_PVP_REWARDS_RESPONSE`: the last byte (0xF8) is not understood.
- `SMSG_RATED_PVP_INFO`: not cross-checked with server code.
- `SMSG_FEATURE_SYSTEM_STATUS`: follows tc-forever `SystemPackets.cpp:123`; verified on one capture.

### 2.4 Data the sniff does not contain (not parser bugs, need new captures or server knowledge)
- `npc_vendor.incrtime` is always 0: the capture has no `SMSG_VENDOR_INVENTORY` for vendors that restock.
- `creature_loot_template`: 5 rows have no data; the drop chance is only an estimate (drops / kills).
- `creature_classic_level_spawn` is not implemented.
- 1 gossip row has `OptionBroadcastTextID` = 0.
- The LootID mapping (`tools/fix-loot-id.cjs`) was verified only where LootID == creature entry.

## 3. Needed for 70862 (secondary)
The parser status line reports 573 packets with errors. Counting error markers in the `_parsed.txt` log gives 896 (a packet can log an exception and nested warnings, and some exceptions are caught inside handlers), so treat the per-opcode numbers below as "where the problems are", not as an exact partition of the 573.

| Opcode | Error markers | Cause / state |
|---|---|---|
| `SMSG_UPDATE_OBJECT` | 665 | `PlayerData` of the active player (ThisIsYou) is read wrongly after `GuildTimeStamp` (the descriptor of 70862 has more data there than 70938). Creature `UnitData` is already correct. Other players (non-owner) also still drift in some packets. |
| `SMSG_QUERY_CREATURE_RESPONSE` | 124 | `EndOfStreamException` while reading the display array. Not investigated yet. |
| `SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE` | 84 | Layout differs from the 7.x template: the bytes show a count, then fixed-size entries with a ticket and `Party` members. Not in tc-forever and not in 70938. |
| `SMSG_AUCTION_LIST_ITEMS_RESULT` | 6 | not investigated |
| `SMSG_CORPSE_LOCATION` | 4 | not investigated |
| `CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE` | 3 | not investigated |
| `SMSG_INSPECT_RESULT` | 3 | not investigated |
| `CMSG_CAN_DUEL` | 2 | not investigated |
| 5 others, 1 each | 5 | `SMSG_PARTY_INVITE`, `CMSG_LFG_LIST_SEARCH`, `SMSG_LFG_LIST_SEARCH_RESULTS`, `CMSG_MOVE_SET_ADV_FLYING_AIR_FRICTION_ACK`, `CMSG_AUCTION_SELL_ITEM` |

Already fixed for 70862 (in git, commits `be171cfff` to `74a37e9c4`): creature UnitData layout, player-name lookup, contact list, action button, channel list, guild rewards list, logout, faction standing, auction list requests.

## 4. What I would ask the developer
1. The opcode table (client and server) of your working parser, to name the 61 unnamed and verify the 39 hex-only opcodes (2.1 and 2.2).
2. The update-field descriptor layout of your parser for ActivePlayer/Player in 1.60.1 (both 70862 and 70938), to close the `SMSG_UPDATE_OBJECT` errors in 70862.
3. A decision on the auction extra block (2.3): does the server know what these 2/7/12 bytes are?
4. Another capture with: vendors that restock, more kills with loot, several cities and instances, and the auction house opened more than once.

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
