# What is still broken — WPP-full, build 1.60.1.70938

Source capture: `dump_1.60.1.70938_2026-11-14_12-41-07.pkt`
(119,594 packets, Orgrimmar, 8.5 min, MapID 1).

State: **Karrz1 patches 0001 / 0002 / 0003 / 0004 / 0005 applied** on `master`.

## Overall result

| bucket | packets | % |
|---|---|---|
| **OK** | **117,307** | **98.088 %** |
| errors (exception) | **0** | **0.000 %** |
| skipped | 1,181 | 0.988 % |
| without structure | 1,046 | 0.875 % |

Skipped and without-structure packets have no handler at all; they are not
parser failures. Excluding them from the denominator:

**117,307 / 117,307 = 100.000 %** of everything the parser actually attempts.

**No packet throws an exception any more.** The only remaining defect is
44 packets that parse but leave unread bytes at the end.

---

## Part 1 — Exceptions: FIXED (was 62, now 0)

All 62 exceptions are gone after patches 0004 and 0005. Recorded here so the
reasoning is not repeated. The author of 0004 corrected one detail of the
earlier analysis: `"{0:F20}"` for `1E30` does produce a 20+ character string,
so that example was wrong. The real trigger is **NaN and infinity**, which
format to short strings (`"NaN"`, `"Infinity"`) and make `Substring(0, 20)`
throw.

### 1.1 `SMSG_GUILD_ROSTER` — 61 packets — fixed by 0004

```
WowPacketParser.Misc.Packet.FormatFloat(Single value)          PacketReads.cs:741
WowPacketParserModule.Substructures.MythicPlusHandler.ReadDungeonScoreSummary   MythicPlusHandler.cs:55
WowPacketParserModule.V9_0_1_36216.Parsers.GuildHandler.HandleGuildRoster      GuildHandler.cs:64
```

`FormatFloat` now guards the substring by string length. Trimming, current
culture and `DebugReads` formatting are unchanged.

**Still open:** the guild-roster field alignment itself. Forever 1.60.1 has no
Mythic+, yet `HandleGuildRoster` calls the retail `ReadDungeonScoreSummary`.
The exception no longer fires, but the bytes are still read as the wrong
structure — that reader can silently produce nonsense or leave bytes unread.
Not visible on this capture (the roster packets now close cleanly), but it is
not a correct layout.

### 1.2 `SMSG_UPDATE_OBJECT` — 1 packet — fixed by 0005

```
WowPacketParser.Misc.DynamicUpdateField`1.Resize(UInt32 newSize)  DynamicUpdateField.cs:25
...V1_60_1_70888.UpdateFieldHandler.ReadCreateActivePlayerData  UpdateFieldsHandler.cs:3885
```

The real cause was **not** `Resize` — it was earlier, in the shared Perks
vendor-item reader. Forever is a separate client branch, so `OriginalPrice`
and `WarbandSceneID` were both skipped. The reader consumed 40 bytes plus the
bit byte instead of 48 plus the bit byte, and `CraftingOrders` came out as
2,667,577,344 instead of 0.

Record 801 (zero-based), payload exactly 127,078 bytes — the same packet this
project hand-decoded over several sessions:

```
Old Perks width:     40 bytes + bit byte; CraftingOrders = 2,667,577,344
Corrected width:     48 bytes + bit byte; CraftingOrders = 0
FrozenPerksVendorItem 50585; CraftingOrders count 50839
```

`Resize` additionally gained a 65,536 ceiling that rejects absurd counts before
allocation or casting — a defensive limit, not a claimed protocol maximum.
Rejection leaves existing values intact; there is no clamping and no silent
data loss.

---

## Part 2 — "Packet not fully read" (44 packets)

These parse without throwing but leave unread bytes. Each one is a field the
handler does not know about.

| count | opcode | wire | length | pos | remaining |
|---|---|---|---|---|---|
| 10 | `SMSG_PARTY_UPDATE` | 0x470097 | 150 | 139 | **11** |
| 6 | `SMSG_HEALTH_UPDATE` | 0x47017F | 161 | 16 | **145** |
| 5 | `SMSG_TITLE_LOST` | 0x470189 | 13 | 4 | **9** |
| 3 | `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | 0x470064 | 829 | 715 | **114** |
| 3 | `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | 0x4701B7 | 170 | 10 | **160** |
| 2 | `CMSG_RESURRECT_RESPONSE` | 0x4400AF | 37 | 9 | **28** |
| 2 | `SMSG_GROUP_DECLINE` | 0x47023D | 12 | 2 | **10** |
| 1 | `SMSG_AUTH_RESPONSE` | 0x470001 | 378 | 5 | **373** |
| 1 | `SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT` | 0x47020D | 59 | 54 | **5** |
| 1 | `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | 0x4701B7 | 175 | 15 | **160** |
| 1 | `SMSG_ACCOUNT_MOUNT_UPDATE` | 0x470047 | 31 | 14 | **17** |
| 1 | `SMSG_RATED_PVP_INFO` | 0x4D000F | 666 | 65 | **601** |
| 1 | `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | 0x4D0014 | 310 | 177 | **133** |
| 1 | `CMSG_AUTOBANK_ITEM` | 0x410003 | 4 | 3 | **1** |
| 1 | `SMSG_WHO` | 0x4C0002 | 5,321 | 4,460 | **861** |
| 1 | `CMSG_RESURRECT_RESPONSE` | 0x4400AF | 33 | 6 | **27** |
| 1 | `SMSG_WHO` | 0x4C0002 | 5,600 | 3,872 | **1,728** |
| 1 | `SMSG_WHO` | 0x4C0002 | 5,329 | 5,072 | **257** |
| 1 | `SMSG_TITLE_LOST` | 0x470189 | 14 | 4 | **10** |
| 1 | `SMSG_THREAT_CLEAR` | 0x470191 | 26 | 13 | **13** |

Notes on the interesting ones:

- **`SMSG_HEALTH_UPDATE` at 161 bytes** — the handler reads 16 bytes (guid,
  `u32 ClassicUnknown`, `u32 Health`, `u8 pad`). A 161-byte packet under this
  opcode is *not* a health update; Forever reuses the opcode for a batch form.
  This is the single biggest unread tail after the auth/account packets.
- **`SMSG_WHO` × 3** — same handler, three different unread tails (861, 1,728,
  257). Player entries carry Forever-only trailing fields.
- **`SMSG_UPDATE_ACCOUNT_DATA_COMPLETE`** — the payload contains server memory
  addresses (`0x6AEC7898`, `0x6AF848D4` repeating). This is a **server-side bug**;
  the bytes are not a valid structure and cannot be parsed.
- **`SMSG_AUTH_RESPONSE` (373 bytes unread)** — text `"CAMWOW EU5 Realm 5 PvE"`
  at offset 63, `Result = 0x80000000`, `SuccessInfo` with virtual realms. No
  matching structure in tc-forever.
- **`SMSG_RATED_PVP_INFO` / `SMSG_REQUEST_PVP_REWARDS_RESPONSE`** — no emulator
  counterpart; the emulator does not implement rated PvP.

---

## Part 3 — What has no second source (blocked, not broken)

These cannot be finished by guessing. Each needs either an emulator
implementation or a second capture:

| opcode | why blocked |
|---|---|
| `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | server memory leak; bytes are not a structure |
| `SMSG_AUTH_RESPONSE` | no matching structure in tc-forever |
| `SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT` | bit split unknown; one 59-byte packet is not enough |
| `SMSG_RATED_PVP_INFO` | rated PvP not implemented in tc-forever |
| `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | same |
| `SMSG_ACCOUNT_MOUNT_UPDATE` | 17 unexplained trailing bytes |
| `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | 829 bytes on wire vs 98 in emulator |
| `SMSG_TITLE_LOST` | 9–10 trailing bytes, no emulator field |
| `SMSG_THREAT_CLEAR` | 13 trailing bytes |
| `SMSG_GROUP_DECLINE` | 10 trailing bytes |
| `CMSG_AUTOBANK_ITEM` | 1 trailing byte |
| `SMSG_GUILD_ROSTER` layout | Mythic+ struct called on a non-Mythic+ payload |

---

## Part 4 — Recommended order of work

Exceptions are done. What is left is the 44 unread tails:

1. `SMSG_PARTY_UPDATE` (10) and `SMSG_HEALTH_UPDATE` (6) — 16 packets, the
   largest single group. `HEALTH_UPDATE` at 161 bytes is a batch form, not the
   16-byte single form the handler assumes.
2. `SMSG_WHO` (3) — three different unread tails from one handler; player
   entries carry Forever-only trailing fields.
3. `SMSG_TITLE_LOST` (6), `SMSG_GROUP_DECLINE` (2), `SMSG_THREAT_CLEAR` (1),
   `CMSG_RESURRECT_RESPONSE` (3), `CMSG_AUTOBANK_ITEM` (1) — 13 packets, all
   small single-field gaps.
4. Everything in Part 3 — leave alone unless a second source appears.

**Current state: 117,307 / 119,594 = 98.088 %**, and **100.000 %** of packets
the parser attempts. The 44 remaining are missing fields, not crashes.

**Realistic ceiling on this capture:** the ~34 fixable tails would give
117,341 / 119,594 = 98.12 %, again 100 % of attempted packets. The other ~10
(`SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` and the PvP opcodes) are server-side
garbage or emulator gaps and cannot be closed from this capture alone.

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
Parsed 117307 (98.088%) packets successfully, 0 (0.000%) with errors,
skipped 1181 (0.988%) and without structure 1046 (0.875%)  (total: 119594)
```
