# 70959 — SMSG_UPDATE_OBJECT / ActivePlayerData

Short version: build 70959 is now recognised, and every opcode parses. One
opcode is left — `SMSG_UPDATE_OBJECT` on the ActivePlayer — and I need one
piece of information to fix it properly rather than guess.

## What is done

Build 1.60.1.70959 added (`ClientVersionBuild`, `Opcodes`, `UpdateFields`,
`ClientVersion` dispatch to the 70888 module).

Wire opcodes are identical to 70938 — verified on a full 128404-record sniff
(`dump_1.60.1.70959_2026-11-18_04-12-11.pkt`): same `0x60000E`, `0x5E0000`,
`0x600002`, `0x690011`, same proportions, zero unknown opcodes. So the table
mapping to 70888 is right.

`ForeverQueryCreatureHandler`: 70959 sends 3 `TypeFlags` like 70938, not 4.
That alone removed 189 `EndOfStreamException` on `SMSG_QUERY_CREATURE_RESPONSE`.

Result: 125932 (98.075%) parsed, 2274 (1.771%) errors.
70938 unchanged: 119529 (99.946%), 0 errors.

## What is left

All 2274 errors are `SMSG_UPDATE_OBJECT`. 83% of them are the ActivePlayer
path, 300+ of them in `ReadUpdateActivePlayerData`.

The concrete case — a 63-byte Values update, guid
`0x086C8C000000000000000000053F4AA5`:

```
IsOwned: true
HasFragmentUpdates: true
ArchetypeSerializationType: 0
fragments: CGObject, PlayerHouseInfoComponent_C, PlayerInitiativeComponent_C,
           Tag_Unit, Tag_Player
UnitBlocksMask: 2
UnitBlock[1]: 16781312
Unknown70291: False
ActivePlayerBlocksMask: 0
ActivePlayerChangedBits: (empty)
InvSlots[0]: 0x0
InvSlots[1]: 0x0
InvSlots[2]: 0x0
InvSlots[3]: 0x08000000000000000000000000000000
--> EndOfStreamException
```

In 70938 the same field list reads 105 `InvSlots` from the changed-field
mask. Here it reads 4 and the buffer is gone. So either the changed-field
mask arrives at a different position, or `InvSlots` is no longer a
changed-field list in 70959.

## What I need

`ActivePlayerData::WriteUpdate` from
`src/server/game/Entities/Object/Updates/UpdateFields.cpp` in tc-forever
(the version that ships with 70959), plus the same function for
`Player::BuildValuesUpdate` if the values block layout changed too.

With that I can set the bit numbers from code, the way 70938 was brought to
99.946%. Without it the only option is trying bit numbers until the parse
percentage rises — which produces garbage values (`Health: 1026497183744`,
`UnitBlock: 1048577`) while looking like progress. I did that by accident
three times on this build and reverted all three.

## What I tried and reverted, so we do not repeat it

| attempt | result | why it is not a fix |
|---|---|---|
| `IsNewLayout` widened to 70959 | 4073 errors | EndOfStream 2964 -> 204 but unread-bytes 7474 -> 8164; shifts the problem |
| `is70938 = false` in `ForeverUnitDataReader` | 2274 errors, 98.075% | best score, but `UnitBlock: 16781312`, `Health: 1026497183744` — garbage |
| fragment enum 1127 instead of 1215 | identical | no effect |
| `is70938 = true` for 70959 | 2555 errors | `AuraState: 13631488`, `ActivePlayerBlocksMask: 12` — garbage |

Only the `TypeFlags` change survived, because it was confirmed by field values
rather than by the error count.

## Note on SQL

Not blocking SQL generation. The TEMP/template tables come from query opcodes
(`SMSG_QUERY_CREATURE_RESPONSE`, `SMSG_QUERY_GAME_OBJECT_RESPONSE`,
`SMSG_QUERY_QUEST_INFO_RESPONSE`, `SMSG_VENDOR_INVENTORY`) and from TDB, and
those parse cleanly now. `Storage.Objects` — which `Spawns.cs`,
`AreaTriggers.cs`, `Conversations.cs` and `Miscellaneous.cs` touch — is the
only SQL path affected, and spawns can be taken from the 70938 sniff until
this is fixed.
