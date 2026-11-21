# Where does UnitData end and PlayerData begin in a player create?

Your ordering answer was right and I verified every byte of it. My `132` was not a size, it was `0x84`, the first byte of the movement block. The real `u32` sits after movement and covers everything to the end of the object — I measured `@7571 = 2051`, and `7575 + 2051 = 9626`, which is exactly the position my reader reports when it runs off the end. So the block boundary is settled and I am not asking about that any more.

**One thing is left, and it is inside the block.**

## What the block looks like

Player, packet 1372 of `dump_1.60.1.70959_2026-11-18`:

```
@7571  u32  = 2051
@7575  04 03 21 26 cc cd ff    field flags (UnitAll), CGObject, PHI, PII, Tag_Unit, Tag_Player, End
@7582  01                      IndirectFragmentActive for CGObject
@7583  ...                     data, 2043 bytes, ends exactly at 9626
```

That is your layout, byte for byte: fragment list, indirect flag, then `ObjectData, UnitData, PlayerData`. Tags carry no data, so there are three payloads in 2043 bytes.

**My reader overruns this block.** It reads past 9626 — that is the `EndOfStreamException`, 56 of them on the 19.11 capture. So one of the three payloads is being read with the wrong length, and the only one I have no independent check on is `UnitData`.

## The question

**How many bytes does `UnitData` take in a player create, and is it the same layout a creature writes?**

I read `UnitData` on creatures without trouble — that reader is measured and the creature spawns come out complete (294 rows, 294 addons, from the 19.11 capture). But a player may write `UnitData` fields a creature never sets, and if the two layouts differ by even one field I am off by that much for the rest of the block.

Specifically:

- Is `UnitData` for a player written by the same `UnitData::WriteCreate` as for a creature, just with more bits set — or is there a separate player path?
- Does `UnitData` for a player carry anything extra that a creature does not, the way `PlayerData` carries the two 17-byte structs?
- If it is the same writer, then my error is in `PlayerData` after all and I will go back to your earlier notes with the boundary I now have — but I would rather know which of the two it is than try both.

**A byte count, or "same writer, more bits set", is enough.** I do not need the field list again.

## What I measured in the source since writing the above

I did not wait idle. I read `UpdateFields.cpp` at `a66b6b80ef` and counted the writers myself, so you do not have to.

`ObjectData::WriteCreate` is 12 bytes — `int32 EntryID`, `uint32 DynamicFlags`, `float Scale`. Confirmed on the packet: `@7583 = 02 d2 00 00 | 00 00 00 00 | 00 00 80 3f`, i.e. entry 722, dynamic flags 0, scale 1.0.

`UnitData::WriteCreate` is 143 static writes = 526 bytes, minus the conditional blocks. With `fieldVisibilityFlags = Owner | UnitAll` — which is what a player gets — the static part comes to **391 bytes**, and with the `IsBuild70425OrLater() && Owner` block of three `int32` that follows `TaxiNodesID` it is **403 bytes**. So my estimate for a player's `UnitData` is 403 bytes.

**That number does not fit the packet.** The field block is 2043 bytes (`@7583..@9626`). Taking 12 for `ObjectData` and 403 for `UnitData` leaves 1628 for `PlayerData`, and at `@7583 + 12 + 403 = 7998` there is nothing that looks like an `PlayerData` mask — the bytes there are `00 00 08 00 00 00 00 08`, and the 11-bit mask that would follow is zero.

The reason is the three unbounded lists at the end of `UnitData`:

```cpp
data << uint32(PassiveSpells.size());
data << uint32(WorldEffects.size());
data << uint32(ChannelObjects.size());
...
for (uint32 i = 0; i < PassiveSpells.size(); ++i)
    PassiveSpells[i].WriteCreate(data, receiver, owner);
```

A player has passive spells, so `PassiveSpells` is not empty and `UnitData` is longer than 403 — but I cannot tell by how much without knowing what one `PassiveSpells[i]` entry costs, and I have no packet where the three counts are readable.

**So the question narrows to this:** what is `PassiveSpellData::WriteCreate` (or whatever the element type is), and how many entries does a typical player carry? Even "usually 0 or 1, and one entry is N bytes" would let me place the boundary.

## What I tried today and reverted

So you can see what is already ruled out — all three were measured on the same three captures and all three came back worse:

| change | 70959 18.11 | 70959 19.11 |
|---|---|---|
| baseline | 170 | 38 |
| 17-byte struct added to 70959 (both places) | 179 | 44 |
| 17-byte struct added to 70959 (after GuildLevel only) | 181 | 44 |
| create skips `ArchetypeSerializationType` for 70959 | 253 | 80 |

The last one is worth your attention: you said a create has no `ArchetypeSerializationType` byte. When I made 70959 skip it, errors went from 170 to 253. So either it *is* there in this build's creates, or the byte I skip is being read somewhere else that matters. Either way, my reader is not simply "missing a byte at the front" — adding bytes at the front makes it worse, which is why I now think the overrun is in the middle, at a payload boundary.

## Current state

| capture | packets | errors |
|---|---|---|
| 70959 18.11 | 128025 | 170 |
| 70959 19.11 | 38699 | 38 |
| 70938 | 492567 | 18 |

Creature spawns complete. Both of your fixes from earlier today are committed and measured: the fragment-mask condition (`197 -> 170`, 70938 untouched) and `CMSG_AUTH_SESSION`, where the ticket size sits at `@77` and not `@69`.

Thanks for the ordering answer — it turned a week of guessing into one measurement.
