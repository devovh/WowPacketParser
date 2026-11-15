# Parser status — WPP-full, build 1.60.1.70938

Source capture: `dump_1.60.1.70938_2026-11-14_12-41-07.pkt`
(119,594 packets, Orgrimmar, 8.5 min, MapID 1).

## Overall result

| bucket | packets | % |
|---|---|---|
| **OK** | **117,421** | **98.183 %** |
| errors (exception or unread bytes) | **0** | **0.000 %** |
| skipped | 1,127 | 0.942 % |
| without structure | 1,046 | 0.875 % |

Skipped and without-structure packets have no handler at all; they are not
parser failures. **No packet throws and no packet leaves unread bytes.**

History of this capture: 162 packets with errors -> 46 -> 18 -> 2 -> **0**.

---

## What was wrong and how it was fixed

Reference for layouts: tc-forever (`forevercore/src/server/game/Server/Packets`).
Where tc-forever has no layout (or differs), the layout was derived from the
packet bytes and lengths.

### Opcode numbering (wrong opcode -> wrong parser, looks like "garbage bytes")

The sniff differs from the tc-forever numbering for some opcodes. These were
found by packet size and content, not by the tables:

| wire | was | is | evidence |
|---|---|---|---|
| 0x47017F–0x47018B | retail numbering | Classic index = retail − 1 | `ClassicOpcodes.cpp TranslateServerOpcode70245`; `SMSG_HEALTH_UPDATE` removed, `ForeverThreatHandler.cs` added |
| 0x4400AF | `CMSG_RESURRECT_RESPONSE` | `CMSG_WHO` | MinLevel 13, MaxLevel 19, AreaID 1637, Token 3 |
| 0x4400AD | `CMSG_WHO` | `CMSG_RESURRECT_RESPONSE` | swapped pair |
| 0x4701B7 | `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | `SMSG_ACCOUNT_DATA_TIMES` | 170 B = Guid + ServerTime + 20 x Timestamp |
| 0x4701B6 | `SMSG_ACCOUNT_DATA_TIMES` | `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | 15 B packet |

An earlier version of this document called the 160 unread bytes of
`SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` a "server memory leak". That was wrong:
they were the 20 timestamps of `SMSG_ACCOUNT_DATA_TIMES` read through the
wrong opcode.

### Layout fixes (module `WowPacketParserModule.V1_60_1_70888/Parsers`)

| packet | handler | what changed |
|---|---|---|
| `SMSG_GUILD_ROSTER`, `SMSG_UPDATE_OBJECT` | patches 0004 / 0005 | NaN / infinity in `FormatFloat`; Mythic+ struct on a non-Mythic+ payload |
| `SMSG_PARTY_UPDATE` | `ForeverPartyHandler.cs` | LeaverStatus bit, optional blocks |
| `SMSG_GROUP_DECLINE`, `CMSG_AUTOBANK_ITEM`, `SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT` | `ForeverAccountHandler.cs` | tc-forever layouts |
| `SMSG_ACCOUNT_MOUNT_UPDATE` | `ForeverMiscHandler.cs` | trailing fields |
| `SMSG_THREAT_UPDATE`, `SMSG_THREAT_CLEAR`, `SMSG_TITLE_LOST` | `ForeverThreatHandler.cs` | shifted opcodes, tc-forever `CombatPackets.cpp` |
| `SMSG_WHO`, `CMSG_WHO` | `ForeverWhoHandler.cs` | `WhoPackets.cpp:61,110` |
| `SMSG_ACCOUNT_DATA_TIMES` | `WorldSessionHandler.cs` | Guid, ServerTime, 20 timestamps (was 32) |
| `SMSG_AUTH_RESPONSE` | `SessionHandler.cs` | `u32 Result` comes BEFORE the two bits |
| `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | `ForeverGlueScreenHandler.cs` | derived from bytes (829 B) |
| `SMSG_FEATURE_SYSTEM_STATUS` | `ForeverFeatureStatusHandler.cs` | `SystemPackets.cpp:123`: no `KioskSessionDurationMinutes`, SocialRestriction before ContentSetID, 10 `ClassicFlagBits10` bits |
| `SMSG_RATED_PVP_INFO` | `ForeverPvpHandler.cs` | 9 brackets x 74 B = 666 B (`BattlegroundPackets.cpp:350`) |
| `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | `ForeverPvpHandler.cs` | rewards with a u8 mask; no tc-forever layout |
| `SMSG_AUCTION_LIST_BUCKETS_RESULT` | `ForeverAuctionHandler.cs` | see "Open items" |

---

## Open items (parse completely, meaning not fully known)

These packets no longer fail, but part of what they carry is a guess:

| packet | what is uncertain |
|---|---|
| `SMSG_AUCTION_LIST_BUCKETS_RESULT` | The 70938 client appends, per bucket, a 7th flag bit (`HasExtraBlock`), then a u32 (= the bucket's ItemID) and a block of 2, 7 or 12 bytes whose content is unknown. tc-forever does not write it. The block length is chosen so that the next bucket header is sane (MinPrice not below the previous one, 0 appearances); verified on all 500 buckets of the one packet in this capture. A second capture would confirm it. |
| `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | The last byte (0xF8 in the capture) has no name yet. The packet is all zeros otherwise. |
| `SMSG_RATED_PVP_INFO` | tc-forever has the layout, but the emulator does not implement rated PvP, so the values were not cross-checked against server code. |

## Data the capture does not contain (not parser bugs)

- `npc_vendor.incrtime` stays 0; this capture has no `SMSG_VENDOR_INVENTORY`.
- Loot: only 5 `creature_loot_template` rows (few loot windows). Chance = drops / kills x 100, an estimate from a small sample.
- Gossip: a single `SMSG_GOSSIP_MESSAGE`; `OptionBroadcastTextID` stays 0.
- `creature_classic_level_spawn` is not implemented.

---

## How to reproduce

```
cd C:\Users\devov\Desktop\WPP-full
git checkout master
dotnet build WowPacketParser.slnx -c Release -v q --nologo
cd WowPacketParser\bin\Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config ^
  "C:\Users\devov\Desktop\regedit_70790\dumps\dump_1.60.1.70938_2026-11-14_12-41-07.pkt"
```

Expected output line:

```
Parsed 117421 (98.183%) packets successfully, 0 (0.000%) with errors,
skipped 1127 (0.942%) and without structure 1046 (0.875%)  (total: 119594)
```

Post-processing of the generated SQL:

```
node tools/patch-from-tdb.cjs        # DB2 columns the client does not send, from the TDB hotfixes
node tools/fix-loot-id.cjs           # creature_loot_template.Entry -> LootID (TDB world)
node tools/check-sql.cjs out/forever_70938.sql
```
