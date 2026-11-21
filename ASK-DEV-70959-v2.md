# 70959 — one question about the field layout the realm actually sends

Short version: build 70959 is recognised, every opcode parses, and I cut the
error count from 2274 to 566. The 566 that remain are all `SMSG_UPDATE_OBJECT`
on the ActivePlayer path, and I have run out of ways to deduce the layout from
the bytes alone. I need one answer from you to finish it.

## What is done and measured

Build 1.60.1.70959 added, wire opcodes identical to 70938 (verified on the full
128404-record sniff), `ForeverQueryCreatureHandler` fixed for 3 `TypeFlags`.

Then the real win: 70959 was falling into the unconditional
`ReadUpdateActivePlayerData`, which reads all 105 `InvSlots` plus every field
after them without looking at the changes mask. A value block is 48-52 bytes,
those reads need over 350, so most packets died in `EndOfStreamException`.
Pointing 70959 at the mask-checked `ReadUpdateActivePlayerData70938` instead:

| sniff | before | after |
|---|---|---|
| `dump_1.60.1.70959_2026-11-18_04-12-11.pkt` | 125932 (98.075%), 2274 errors | **127640 (99.405%), 566 errors** |
| `dump_1.60.1.70938_2026-11-14_12-41-07.pkt` | 119529 (99.946%), 0 errors | 119529 (99.946%), 0 errors |

Committed as `6c170c5a1`.

## The question

Your `sql/custom/auth/2026_11_17_70_auth_classic_70959.sql` says:

> Realm 70 stays on gamebuild 70707. Its message numbers are handled per
> connection: 70959 clients get the 70790 numbering.

That tells me what a 70959 client sends and receives at the **opcode** level.
What I cannot derive is the **field layout**, because in `UpdateFields.cpp` the
layout is chosen from the realm's build, not the client's:

```cpp
ClassicOpcodes::IsBuild70425OrLater() ? ClassicUnitDataGroups70425 :
ClassicOpcodes::IsBuild70291OrLater() ? ClassicUnitDataGroups70291 :
                                        ClassicUnitDataGroups70009
```

and both of those read `sRealmList->GetCurrentRealm()->Build`.

**So, concretely:**

1. For a 70959 client on realm 70, which of the three `ClassicUnitDataGroups*`
   tables does `UnitData::WriteUpdate` actually use — the one selected by the
   realm's 70707, or is the per-connection client build used instead?

2. Same question for the two `UnitData` remaps:
   `if (classicBit >= 140 && IsBuild70291OrLater()) ++classicBit;` and
   `if (classicBit >= 132 && IsBuild70425OrLater()) classicBit += 3;`

If the answer is "the realm's 70707", then a 70959 client is served the
70291-group layout with only the `+1` shift. My parser currently applies the
70425 tables and both shifts for 70959, which would explain every remaining
error being a small 3-byte misalignment.

## What I already ruled out (measured, all reverted)

So the next person does not repeat these:

- `IsIndirectFragment` / `IsIndirect` tables differ between server and parser
  (`FPlayerOwnershipLink`, `FVendor_C`) — but not for a player, whose fragments
  are `CGObject + Tag_Unit + Tag_Player + Tag_ActivePlayer`. Not the cause.
- Change-mask block counts 8 / 11 / 14 — the server's
  `WriteClassicChangesMaskHeader<8/11/14>` confirms them. Not the cause.
- `ReadUInt32` vs `ReadBits(32)` for the unit block masks — 566 errors became
  575. Worse, reverted.
- `AssistActionData.has_value()` placement — the server writes it at the end of
  `WriteUpdate` (`UpdateFields.cpp:2031`) and near the start of `WriteCreate`
  (`:1280`), exactly as the packets show. Not the cause.
- Dropping the `+= 3` (70425) shift for 70959 only — 70938 went from 0 to 2247
  errors, so that shift is needed for 70938. Reverted.

## What would settle it fastest

One packet logged on **both** sides: the same `SMSG_UPDATE_OBJECT` as sent, plus
the `classicBlocks` / `changesMask` the server had for it. A single
`SMSG_UPDATE_OBJECT` for a player, server-side, is enough — I can diff it
against the sniff byte for byte instead of deducing from 48 bytes.

Failing that, the answer to question 1 above is all I need.
