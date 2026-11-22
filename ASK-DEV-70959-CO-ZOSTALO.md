# 70959/71037 — one remaining over-read, and three fields I need named

Short version: your three lines (822, 942, 1551/1559) were right, and fixing all
three **as a swap, not an addition**, worked. `creature` rows went 7 → 7 (no
regression), `InvalidDataException` went 346 → 187 (−46%). What is left is one
symptom: `EndOfStreamException` on `SMSG_UPDATE_OBJECT`, which **grew** from 820
to 969. I think I know which fields cause it, but I need you to confirm whether
the realm writes them.

## What is measured now

Sniff: `dump_1.60.1.70959_2026-11-19_10-24-12.pkt` (60 MB, 5532 create blocks,
3970 of them `UnitAll`, 1505 `PlayerData` blocks).

| metric | baseline | after the three fixes |
|---|---|---|
| `Updatefields not fully read` | 171 994 | 173 747 |
| `Packet not fully read` | 719 | 722 |
| `InvalidDataException` | 346 | **187** |
| `EndOfStreamException` | 820 | **969** |
| rows in `creature` | 7 | **7** |

Build 71037 is confirmed identical: an 8 MB sniff from 2026-11-21 gives
8805 / 37 / 29 / 62 on the same code, i.e. 5–9% of the 70959 numbers, exactly
the size ratio of the two files. So one answer covers both builds.

## The over-read, in the dump

The tail of a `PlayerData` create block prints this:

```
[0] [0]  PlunderstormItemDisplayID: 2148466688    <- 0x80100000
[0] [1]  PlunderstormItemDisplayID: 1194423354    <- 0x4732...
[0] [2]  PlunderstormItemDisplayID: 120
[0] [13] PlunderstormItemDisplayID: 4294967295    <- 0xFFFFFFFF
```

Those are not display IDs, they are the next field's bytes being read as 19 × u32.
The reads in question, in `ReadCreatePlayerData`:

```csharp
data.VisualItemReplacements.Resize(packet.ReadUInt32());
for (var i = 0; i < 19; ++i)                       // 76 bytes
    data.PlunderstormItemDisplayID[i] = packet.ReadUInt32(...);
data.PersonalTabard  = ReadCreateCustomTabardInfo(...);   // 5 x u32
data.NpcAsPlayerInfo = ReadCreateNPCAsPlayerInfo(...);    // 4 x u32 + 3 x f32
```

`DistrictValue`/`DistrictFlag` right below them are **already** gated on
`Build == 70938`, so someone hit this before me on one field only.

## My guess, which I would like you to confirm

`PlunderstormItemDisplayID` (19 × u32 = 76 B) is a retail-only field —
Plunderstorm is 10.2.6 content. If the Forever `PlayerData::WriteCreate` never
writes it, then every player create block is 76 bytes short on our side, and
that alone would explain a steady `EndOfStreamException` on player blocks.

## The questions

**1.** Does `PlayerData::WriteCreate` in the 70959/71037 branch write
`PlunderstormItemDisplayID` at all? And `PersonalTabard` and `NpcAsPlayerInfo`?

**2.** If not, is the tail of `PlayerData` after `VisualItemReplacements`
literally just:

```
u32 VisualItemReplacements count + count * item
u32 Customizations count + entries
u32 RandomCustomizations count + entries
... then the name header (6 + 9 bits), Name, Surname
... then PHI (01 + u32 count), PII (01 + 2B guid + 32B + 2 x u32)
```

or is there something between them that is not in the retail order?

**3.** You wrote that after the surname come PHI (48 B) and then the block ends.
In the sniff, a **value** update block (`UpdateType: Values`) for a player still
reads only 2 bytes out of 109 and stops with `Updatefields not fully read`. Is
that block supposed to be a plain field mask + the fields the mask selects, with
**no** create-style tail at all? That is what I currently assume, and it is
where most of my 173 747 warnings come from.

## What I am not asking

Not for a patch, and not for the writer code — just the field list for those
three, and a yes/no on question 3. Everything else I can measure myself.

## Notes

- I removed the hardcoded MySQL password from `tools/compare-with-tdb.cjs`
  (commit `1514fb923`); it now reads `WPP_MYSQL_PWD` or `tools/.env.local`,
  which is gitignored. The old value is still in history at `7d5d1c42a` and in
  `TDB-COMPARISON-2026-11-16.md`; it is a localhost root password, so I left
  history alone, but you may want to drop that file.
- `Has70938Fields` in my parser now covers 70938 / 70959 / 71037, based on your
  "the 70959 create bytes are exactly the 70938 ones".
