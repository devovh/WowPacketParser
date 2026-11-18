# 1.60.1.70959 — creature spawns: state, measurements, open question

Date: 2026-11-18
Sniff: `dump_1.60.1.70959_2026-11-18_04-12-11.pkt` (128404 packets)
Reference build (working): `dump_1.60.1.70938_2026-11-14_12-41-07.pkt` (119594 packets)

---

## 1. Current state

| build  | parsed              | errors        | `creature` rows |
|--------|---------------------|---------------|-----------------|
| 70959  | 127898 (99.606%)    | 308 (0.240%)  | **0**           |
| 70938  | 119529 (99.946%)    | 0             | 715             |

70959 errors went from **566 → 308** with the one change described in section 3.
70959 creature spawns are **still 0**, and that is the open problem.

---

## 2. What is already proven (do not re-investigate)

### 2.1 Units are parsed and stored

```
STORDBG total=953 unit=587      # Storage.Objects after parsing
CREATURECALL units=587          # Spawns.Creature() receives them
SPAWNDBG units=587 entry0=0     # not one of them has EntryID == 0
CREATURECALL ... sqlflag=18446744073709551615   # all SQL output flags set
```

The `SQLOutput.creature` flag is on. `Filters.CheckFilter` rejects nothing.
`Settings.GenerateCreateObject2SpawnsOnly` is off.

**The parser has 587 valid Units with a correct `EntryID`. `Spawns.Creature()` is
called with 587 Units. It still emits 0 rows.** The loss is therefore *inside*
`Spawns.Creature()` between the first `foreach` and the `return`, and the three
early `continue` paths were instrumented and counted zero times each.

### 2.2 Create blocks reach the end

`ReadCreateObjectBlock` is entered 592 times with `Object Type: 5 (Unit)`
(raw byte 5 → `ObjectType.Unit`, verified by printing after the conversion).
`Storage.Objects.Add` runs; `ENDDBG` at the end of the function fires for none of
them only because the block is cut earlier in builds where `IsForever` covers
70959 — see 2.4. With the current code (`IsForever` up to 70938) the Units do
arrive in `Storage.Objects`, as the 587 above show.

### 2.3 The `0xFF000000` `InvalidDataException`

Root cause found by printing the stack in `DynamicUpdateField.Resize`:

```
IPassiveSpellHistory size=4278190080 at UpdateFieldsHandler.cs:line 1037
```

`ReadCreateUnitData` read three dynamic lists (`PassiveSpells`, `WorldEffects`,
`ChannelObjects`) as a bare `u32` count. In 70959 the first of those reads `0xFF`
— the tail of the previous field — and `Resize` throws, killing the whole create
block. Reading them as `ReadUpdateMask(packet, 32)` instead made things *worse*
(the next field then landed on another `0xFF`), so the current code reads three
plain `u32` (`UnitDataTail0..2`) and that is where the 308 errors come from today.

**This is not the spawn blocker** (587 Units still land in storage), but it is
where the remaining 308 errors live, and the field names there are guesses.

### 2.4 70959 must NOT be pushed onto the Forever path

`UpdateHandler.IsForever` is `70338..70938`. Widening it to 70959 was measured:

```
IsForever up to 70938 -> 127885 parsed / 321 errors / creature 1455   (NOT reproducible)
IsForever up to 70959 -> 127767 parsed / 439 errors / creature 0
```

The `1455` line was an early artefact of comparing SQL files by timestamp and
was **withdrawn** — every careful re-run of that configuration gave **0 spawns**
and `439` errors. Do not repeat it.

Consequence: 70959 has no `ReadCreateEntityFragmentDataForever`. It goes through
the retail create path, and that path is what produces the 587 stored Units.

---

## 3. The change currently in the tree

`WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs`,
inside `ReadCreateUnitData`:

```csharp
// Forever 1.60.1: trzy listy dynamiczne UnitData sa pisane inaczej niz w retail.
// Serwer (tc-forever, UnitData::WriteCreate) poprzedza kazda liste maska zmian
// (ReadBits(32)), a nie samym u32 licznikiem - czytanie samego u32 zjadalo maske
// i licznik trafial na 0xFF000000 (4278190080), co konczylo sie InvalidDataException
// i utrata calego bloku create, przez co creature spawns nie trafialy do SQL.
if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70959)
{
    data.PassiveSpells.Resize(packet.ReadUInt32());   // -> currently ReadUInt32 / tail guesses
    data.WorldEffects.Resize(packet.ReadUInt32());
    data.ChannelObjects.Resize(packet.ReadUInt32());
}
```

Effect measured: **566 → 308 errors**, 70938 unchanged at 0.

---

## 4. The question for you

I can get 70959 to `127898 / 308 / creature 0`, and I can prove the 587 Units
have a valid `EntryID` in `Storage.Objects`, but `Spawns.Creature()` still emits
no rows and the early-continue paths are all counted zero.

Two things would settle it much faster than more black-box probing:

1. **How does the real server serialise `UnitData`'s dynamic lists in
   `BuildCreateUpdateBlockForPlayer` for build 70959?** Specifically the three
   lists around `GuildGUID` in `UnitData` — are they written as a bare `u32`
   count, as `ReadBits(32)` + mask, or are they not written at all in the create
   block? A pointer to the function in `tc-forever` would be enough.

2. **Is there any additional condition in `Spawns.Creature()` for the Forever
   target** beyond `Filters.CheckFilter`, `GenerateCreateObject2SpawnsOnly` and
   `entry == 0`? The comment in `SQLFile.cs` mentions a pass that removes rows
   duplicating an existing database spawn — if that pass can remove *all* rows
   when `SkipRowsWithFallbackValues` / `SaveExistingSpawns` are at defaults, that
   would explain 587 in and 0 out.

---

## 5. Reproduction

```bash
dotnet build WowPacketParser.slnx -c Release
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll -f <path>/dump_1.60.1.70959_2026-11-18_04-12-11.pkt -o out_70959
dotnet WowPacketParser.dll -f <path>/dump_1.60.1.70938_2026-11-14_12-41-07.pkt -o out_70938
```

Check `creature` rows in the produced `.pkt.sql`; 70959 gives 0, 70938 gives 715.
