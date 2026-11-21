# 70959 — three questions left, all in `PlayerData`

**Status after your MSB-first answer:** 197 errors (from 278), 99.692% of packets
parsed. That fix was worth more than everything else I had tried combined — thank
you. The block you explained now decodes to the byte with zero remainder.

What is left is three things, and all of them sit in `PlayerData`. I measured each
one before asking, so you know what I have already ruled out.

---

## 1. `ItemInstance` inside `VisibleEquipableSpells` — 306 errors

This is the whole remaining error count, effectively. Every one is an
`EndOfStreamException` on a `Player` `CreateObject1`, always in this array.

**What I have:** `PlayerData.VisibleEquipableSpells` is 16 entries, read
unconditionally (no mask). The parser routes them through
`Substructures.ItemHandler.ReadItemInstance`, which has no Forever branch — for
70959 it falls into `ReadItemInstance602`, the 2006 layout. That is clearly wrong.

**What I tried:** I have a working Forever layout for a normal item —
`ForeverDeveloperLayouts4Handler.ReadItemInstance`:

```csharp
int32  ItemID
bits   ModifierCount (7)
       ModifierType (u8) + ModifierValue (int32), repeated ModifierCount times
bit    HasBonus
       u8 Context, uint32 BonusListCount, int32 BonusListID * BonusListCount
```

Swapping `VisibleEquipableSpells` onto it made things **worse**: 197 -> 210 errors,
EndOfStream 306 -> 331. So `VisibleEquipableSpells` is **not** that layout either.

**Measured on** `dump_1.60.1.70959_2026-11-18_04-12-11.pkt`, packet 967, object
`[17]` (Player, low 5999139, field block 132 bytes at 18450). The parser walks past
the end of the stream with 14584 bytes still unread in the packet, so it is
over-reading by a lot, not by a byte or two.

**The question:** what does `PlayerData.VisibleEquipableSpells` actually write on
this realm? Specifically:

- Are the 16 entries still `ItemInstance`, or something simpler?
- If `ItemInstance`, which of your `ItemInstance` write variants is it — the same
  one a normal item update uses, or a reduced one?
- Is the count always 16, or does it depend on the changes mask?

Even "it is always 16 empty entries of N bytes" would close this, because the log
shows `ItemID: 0` for most of them and one garbage value where it desyncs.

## 2. `Dynamic update field count` — 107 errors

```
SMSG_UPDATE_OBJECT: InvalidDataException:
  Dynamic update field count 590336 exceeds the supported limit 65536.
```

Same packet shape: `Player`, and the values right before it are the plain
`PlayerData` fields (`DuelArbiter`, `WowAccount`, `BnetAccount`,
`GuildClubMemberID`, `LootTargetGUID`, `PlayerFlags`, `PlayerFlagsEx`,
`GuildRankID`, `GuildDeleteDate`, `GuildLevel`), so the desync starts just past
`GuildLevel`.

My reader expects `WriteMapFieldUpdate` there: `u8 ignoreChangesMask`, then
`u16 count`, then `count` pairs of `int32 key, int32 value`. The count I get is
590336 = 0x90000, which is 8 bytes' worth of misalignment rather than a small
off-by-one.

**The question:** is `WriteMapFieldUpdate` used in `PlayerData` at all on 70959, or
only in `ActivePlayerData`? And if it is, is the count still `u16`?

## 3. `OverflowException` — 4 errors

Only four, so this is the lowest priority, but I would rather close it than leave
it. If you know offhand which field can overflow on a player update, that is
enough; I have not isolated it yet.

---

## What I am not asking about

**Spawn rows.** I get 563 `creature` rows from the 18.11 capture and 294 from the
19.11 one, with matching `creature_addon` rows, and the set of distinct entries in
the SQL is 188 against 189 in the capture. So spawning works; if anything is
missing it is a deduplication question on my side (`SkipDuplicateSpawns`), not a
layout question. I mention it only so you do not spend time on it.

**The GuildGUID/pet thing from your last answer.** Confirmed and closed — bit 135
is never set on a pet from your server, and with MSB-first reading `[1]` no longer
claims bit 135 at all.

---

## Where the parser stands

| capture | packets | errors |
|---|---|---|
| 70959 18.11 | 128009 | 197 |
| 70959 19.11 | 38695 | 48 |
| 70938 15-32-39 | 492566 | 19 |

Committed: `a6128d862` (MSB-first block reading), `ae95ac36d` (GameObject bits
22-26 in server order, 941 fewer unread blocks), `c21bb4484` (the earlier write-up).

If any of the three above is a "no, we don't write that" answer, that is just as
useful to me as a layout — it tells me where to look instead.
