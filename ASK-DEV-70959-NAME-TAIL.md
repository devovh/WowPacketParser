# The name/surname tail of a player create has a 3-byte prefix the source does not write

I have stopped guessing and gone at this from the end of the block instead of the beginning. Your ordering answer gave me a block boundary I trust (`u32 @7571 = 2051`, data `@7583..@9626`, ends exactly where my reader reports the overrun). So I read the block backwards from its end, and the tail is very regular — regular enough that it must mean something, but not what `PlayerData::WriteCreate` writes.

## What the tail looks like

`PlayerData::WriteCreate` ends like this (`UpdateFields.cpp:2788`):

```cpp
data.WriteBits(Name->size(), 6);
data.WriteBits(surname.size(), 9);
if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag::PartyMember))
    data.WriteBit(HasQuestSession);
data.WriteBit(HasLevelLink);
data.WriteBit(DeclinedNames.has_value());
data.FlushBits();
data << WorldPackets::SizedString::Data(*Name);
data << WorldPackets::SizedString::Data(surname);
```

So: 17 bits of lengths and flags, flush, then the two strings back to back, with no length bytes between them — `SizedString::Data` is `data.WriteString(value)`, and the lengths already went out as bits.

**In the packet, every name I can find has a 3-byte prefix in front of it that is not the length.** Five independent players from one `SMSG_UPDATE_OBJECT`, all with the same shape:

```
@12694  ... 18 08 00 | 42 72 69 67 68 74 42 6f 6e 65 | 01 00 00 00 00 01 ...
                      "BrightBone"   (10)
@14986  ... 18 08 00 | 42 6c 61 7a 65 72 53 68 61 64 | 01 00 00 00 00 01 ...
                      "BlazerShad"   (10)
@17226  ... 20 10 00 | 41 6c 74 61 72 69 6f 6e 48 6f 6c 79 6d 6f 6f 6e | 01 ...
                      "AltarionHolymoon" (16)
@19517  ... 10 0e 00 | 44 65 61 64 53 68 61 64 77 6f 77 | 01 00 00 00 00 01 ...
                      "DeadShadwow"  (11)
@24151  ... 18 0a 00 | 44 65 73 73 79 6c 53 77 69 70 65 | 01 00 00 00 00 01 ...
                      "DessylSwipe"  (11)
```

Note the prefix tracks the name length but is not equal to it:

| name | length | prefix |
|---|---|---|
| BrightBone | 10 | `18 08 00` |
| BlazerShad | 10 | `18 08 00` |
| AltarionHolymoon | 16 | `20 10 00` |
| DeadShadwow | 11 | `10 0e 00` |
| DessylSwipe | 11 | `18 0a 00` |

`18 08 00` for two different 10-character names is a good sign it is the same field with the same value, not a coincidence of neighbouring data.

## The questions

1. **What are those three bytes?** They sit exactly where the flushed length bits should be, so either the bits are wider than 6+9+2 in this build, or there is a field between the flush and the name.
2. **Are `Name` and `surname` written back to back in this build?** The source says yes. In the packet, `Zepnam` at `@9559` and `lonMusk` at `@9571` have six bytes between them (`18 08 00 58 c3 ab`), which is the same prefix shape again — so possibly the surname has its own copy of the prefix rather than sharing one bit header.
3. If `Name` and `surname` each carry their own prefix, **how many bits, and in which order** — is it `6 + 9` once at the end of the block as the source says, or a separate header per string?

## Why this matters more than the earlier questions

If the tail is 3 bytes longer per name than I read, then the whole `PlayerData` is shifted and everything before the name — `VisibleEquipableSpells`, the quest log, the 17-byte struct — is being read at the wrong offset. That would explain why every field I have added so far has made things worse instead of better: I have been adding fields to a block whose tail does not have the shape I assumed.

## Current state

| capture | packets | errors |
|---|---|---|
| 70959 18.11 | 128025 | 170 |
| 70959 19.11 | 38699 | 38 |
| 70938 | 492567 | 18 |

Creature spawns are complete (294 rows + 294 addons from the 19.11 capture). `ObjectData::WriteCreate` I verified myself: 12 bytes, and the packet agrees — `@7583 = 02 d2 00 00 | 00 00 00 00 | 00 00 80 3f`, entry 722, dynamic flags 0, scale 1.0.

Everything I tried today and reverted is in `ASK-DEV-70959-UNITDATA-BOUNDARY.md` — the short version is that adding the 17-byte struct, adding the `Owner`-only `int32` triple after `TaxiNodesID`, and skipping `ArchetypeSerializationType` in create all came back neutral or worse.
