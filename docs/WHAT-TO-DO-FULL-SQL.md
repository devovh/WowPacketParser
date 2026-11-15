# What Must Be Done for Full SQL Generation from a WoW Forever Sniff

Status: 2026-11-14, client build **1.60.1.70938**.
Project: `C:/Users/devov/Desktop/WPP-full` (fork of WowPacketParser).
Reference source: `C:/Users/devov/Desktop/WoWSniff/forevercore` (TrinityCore, branch `forever-bots`).
Reference database: `C:/Users/devov/Desktop/TDB_full_forever_2026_10_14`.

This is the actionable checklist. Everything below is derived from the code in the
repository and from the two reference trees above — not from memory.

---

## 0. Where we are today

The config is already set to "extract everything" (see `CONFIG-FOREVER.md`):
`DumpFormat 4`, `TargetedDatabase 20`, 65 `SQLOutput.*` keys enabled, all hotfix
tables enabled, `ParseAllHotfixes true`, `SaveTempSpawns true`, plus a new
`KeepSQLFile false` that stops a second run from appending to the previous output.

Result on `dump_1.60.1.70938_2026-11-14_12-41-07.pkt`:

| | default config | current config |
|---|---|---|
| tables in the file | 4–33 | **55** |
| `INSERT` statements | 38 | **506** |
| rows | ~1,700 | **16,599** |
| size | 0.15–0.38 MB | **3.6 MB** |

So the config side is done. What is missing is **code**: the sniff contains the data
(132 `SMSG_QUERY_CREATURE_RESPONSE`, 74 of them with `Result: 0`; 74
`SMSG_QUERY_GAME_OBJECT_RESPONSE` with full payloads; 11 quest responses) but the
Forever handlers never write it into `Storage`, so the builders have nothing to emit.
Measured: **0 rows** of `creature_template`, `gameobject_template`, `quest_template`,
`npc_text`, `page_text`.

---

## 1. `TargetedDatabase.Forever` + `case ClientType.Forever` — small, do first

**Files:** `WowPacketParser/Enums/TargetedDatabase.cs`, `WowPacketParser/SQL/Builder.cs`.

`ClientType.Forever = 11` exists, but `GetExpectedTargetDatabasesForExpansion`
(`Builder.cs:198`) has no case for it and falls into `default: return [];`. Every
builder method marked `[BuilderMethod(..., CheckVersionMismatch = true)]` is then
skipped — 24 builders, 36 log lines of
`Couldn't generate SQL output of X since the targeted database and the sniff version don't match`.

**Steps:**

1. Add to `TargetedDatabase`:
   ```csharp
   Forever = 24   // next free slot after MoPClassic = 23
   ```
2. Add to `TargetedDatabaseFlag`:
   ```csharp
   Forever = 1u << TargetedDatabase.Forever,
   AnyClassic = Classic | WotlkClassic | CataClassic | MoPClassic,   // unchanged
   Any = AnyRetail | AnyClassic | Forever,                           // add Forever
   ```
   Do **not** add `Forever` to `AnyClassic`: `AnyClassic` is used by retail-style
   records to mean "a Classic-era column set", and Forever has its own column set
   (section 3).
3. In `GetExpectedTargetDatabasesForExpansion`:
   ```csharp
   case ClientType.Forever:
       return [TargetedDatabase.Forever, TargetedDatabase.Classic];
   ```
   Keeping `Classic` in the list lets you still build Classic-shaped output for
   comparison; drop it if you want Forever-only.
4. Set `TargetedDatabase = 24` in `App.config` (and in
   `bin/Release/WowPacketParser.dll.config`).

**Verify:** the log must no longer contain `Couldn't generate SQL output of …`.
Output will still be empty for the template tables — that is section 2.

---

## 2. Write the queried templates into `Storage` — the real work

**Why:** `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverQueryCreatureHandler.cs`
reads every field and ends with `packet.AddSniffData(...)` — it never calls
`Storage.CreatureTemplates.Add(...)`. The other Forever handlers inherit 12.x behaviour
where templates arrive as hotfixes, not as query responses, so nothing is stored.

### 2.1 Wire layout (authoritative, from `forevercore`)

`src/server/game/Server/Packets/QueryPackets.cpp:75` — `QueryCreatureResponse::Write()`:

```
Bits<6>(1), FlushBits
u32  CreatureID
u8   result                     // 0 = data present, 1 = no data
if result == 0:
  Bits<11> Title, Bits<11> TitleAlt, Bits<6> CursorName, Bits<1> Leader
  4 x ( Bits<11> Name[i], Bits<11> NameAlt[i] )
  Name[0..3], NameAlt[0..3]                          // SizedCString::Data
  Stats.Flags.data()      std::array<uint32,3>  = 12 B   // TypeFlags, TypeFlags2, TypeFlags3
  u8   CreatureType                              =  1 B
  i32  CreatureFamily                            =  4 B
  i8   Classification                            =  1 B
  ProxyCreatureID.data()  std::array<uint32,2>   =  8 B   // KillCredit1, KillCredit2
  Size<u32> DisplayCount                         =  4 B
  f32  TotalProbability                          =  4 B
  per display: u32 CreatureDisplayID, f32 Scale, f32 Probability
  f32  HpMulti, f32 EnergyMulti
  Size<u32> QuestItems, Size<u32> QuestCurrencies
  8 x i32: CreatureMovementInfoID, HealthScalingExpansion, RequiredExpansion,
           VignetteID, Class, CreatureDifficultyID, WidgetSetID, WidgetSetUnitConditionID
  Title, TitleAlt, CursorName                    // SizedCString::Data
  QuestItems[], QuestCurrencies[]
```

This closes the "37 unknown bytes" problem recorded in `FOREVER-STAN.md`: they are
12 (Flags) + 1 (type) + 4 (family) + 1 (classification) + 8 (2x ProxyCreatureID)
+ 4 (DisplayCount) + 4 (TotalProbability) = 34 bytes plus alignment padding.
Nothing has to be guessed.

Field sources, `src/server/game/Entities/Creature/Creature.cpp:184`
(`CreatureTemplate::BuildQueryData`):
`Flags[0..2] = TypeFlags, TypeFlags2, TypeFlags3`; `ProxyCreatureID = KillCredit[0..1]`;
`Title = SubName`; `TitleAlt = TitleAlt`; `CursorName = IconName`;
`HpMulti = HealthModifier`; `EnergyMulti = ManaModifier`; `Class = unit_class`;
`CreatureMovementInfoID = movementId`.

`QueryPackets.cpp:350` — `QueryGameObjectResponse::Write()`:

```
u32 GameObjectID
GUID Guid
Bits<1> Allow, FlushBits
Size<u32> statsDataLength
statsData:
  i32 Type, i32 DisplayID
  4 x string Name[0..3]
  string IconName, string CastBarCaption, string UnkString
  35 x i32 Data[0..34]        // MAX_GAMEOBJECT_DATA = 35, SharedDefines.h:3234
  f32 Size
  Size<u8> QuestItems, then QuestItems[]
  i32 ContentTuningId, i32 RequiredLevel
```

Matches the sniff (`DataSize: 185`, `Type: 5 (Generic)`, `Display ID: 309`,
`[0..15] Data`, `[0..3] Name`).

`QueryPackets.cpp:305` (`QueryPageTextResponse`) and `:327` (`QueryNPCTextResponse`):
both start `u32 ID, Bits<1> Allow, FlushBits`; page text then
`Size<u32> Pages` with `u32 ID, u32 NextPageID, i32 PlayerConditionID, u8 Flags,
Bits<12> TextLen, FlushBits, Text`; npc text then
`i32 (MAX_NPC_TEXT_OPTIONS * 8)`, `Probabilities[]`, `BroadcastTextID[]`.

### 2.2 What to add in the parser

1. `Store/Objects/CreatureTemplate.cs` — new record `CreatureTemplateForever` (section 3).
2. `ForeverQueryCreatureHandler.ReadCreatureStats` — build the record from the fields
   already read and call
   `Storage.CreatureTemplatesForever.Add(entry, template, packet.TimeSpan)`.
   Displays go to `creature_template_model` (`CreatureTemplateModel`), KillCredit to
   `KillCredit1/2`, `Flags[0..2]` to `type_flags`, `type_flags2`, `type_flags3`.
3. `ForeverQueryGameObjectHandler` — build `GameObjectTemplateForever` and add to
   `Storage.GameObjectTemplatesForever`; quest items to `gameobject_questitem`.
4. Same pattern for page text and npc text handlers.

**Verify:** `INSERT INTO creature_template` with ~74 entries in the output, same for
`gameobject_template`; `node tools/sql_tables.cjs <file.sql>` lists both tables.

---

## 3. Make the output match the TDB Forever schema — medium

The database is **not** retail-shaped. `TDB_full_world_forever_2026_10_14.sql`
(616 MB, 262 tables) is the target.

`creature_template` has **42 columns** (line 1711):

```
entry KillCredit1 KillCredit2 name femaleName subname TitleAlt IconName
RequiredExpansion VignetteID faction npcflag speed_walk speed_run scale
Classification dmgschool BaseAttackTime RangeAttackTime BaseVariance RangeVariance
unit_class unit_flags unit_flags2 unit_flags3 family trainer_class type VehicleId
AIName MovementType ExperienceModifier RacialLeader movementId WidgetSetID
WidgetSetUnitConditionID RegenHealth CreatureImmunitiesId flags_extra ScriptName
StringId VerifiedBuild
```

Compared with the existing `CreatureTemplate` record: missing are `faction`, `npcflag`,
`speed_walk/run`, `scale`, `dmgschool`, `BaseAttackTime`, `RangeAttackTime`,
`BaseVariance`, `RangeVariance`, `unit_flags/2/3`, `trainer_class`, `VehicleId`,
`AIName`, `MovementType`, `ExperienceModifier`, `RegenHealth`,
`CreatureImmunitiesId`, `flags_extra`, `ScriptName`, `StringId`; extra are
`HealthModifier`, `ManaModifier`, `PetSpellDataId`, `type_flags`, `type_flags2`, and
there is no field for `TypeFlags3`. `CreatureTemplateClassic` does not fit either.

**Steps:**

1. New record `CreatureTemplateForever` in `Store/Objects/CreatureTemplate.cs`, every
   field annotated `[DBFieldName("<tdb column>", TargetedDatabaseFlag.Forever)]`
   (plain, not `AnyClassic`), `entry` as primary key.
   Columns the sniff cannot supply (`faction`, `npcflag`, speeds, `AIName`, `ScriptName`,
   …) stay out of the `INSERT` — see the `NULL` note below.
2. `gameobject_template` has **51 columns** and already matches the existing
   `GameObjectTemplate` record (`Data0..Data34` = `Data` x 35); only
   `AIName`, `ScriptName`, `StringId` are sniff-less.
3. **`NULL` is a trap:** most TDB columns are `int NOT NULL DEFAULT 0`
   (`VerifiedBuild int NOT NULL DEFAULT 0`). An `INSERT` that writes `NULL` into them
   fails on import. Either emit `0`/`''` instead of `NULL` (mark the fields
   `[DBFieldName(..., nullable: false)]`), or keep only the columns the sniff really
   fills in the `INSERT` and let the defaults do the rest.
4. Note: in `DBFieldNameAttribute.IsVisible()` / `DBTableNameAttribute.IsVisible()` the
   flag test is `_validForVersion.HasFlag(targetFlag)`, so an attribute whose flag list
   does **not** include `Forever` is invisible when `TargetedDatabase = Forever`.
   Existing retail records therefore produce empty output under Forever — which is
   what we want, but it means every table you still need must get a `Forever` variant.

**Verify:** `SHOW COLUMNS FROM creature_template` on the target DB vs the column list
in the `INSERT` header must be the same set.

---

## 4. `Movement.cs` — malformed `INSERT`s — medium

**Symptom:** 220 rows under `INSERT INTO creature_equip_template` have 6 columns and
5 values, e.g. `(@MOVID + 0, 0, 0, NULL, '1860')` — those are `waypoint_path` rows
glued under the next header.

A `output.Append(";")` closing the previous `waypoint_path_node` INSERT was added, but
the symptom persists. **Remaining work:** in `Movement.GenerateQuery()` do not emit a
`waypoint_path_node` header for a path with zero points, and make sure
`Builder.DumpSQL` joins per-packet builder output with `;\n`.

**Verify:** `node tools/sql_tables.cjs` reports no arity mismatch.

---

## 5. Header `DELETE` statements — small, but decide

54 x `DELETE FROM x WHERE \`VerifiedBuild\` > 0 AND \`VerifiedBuild\` < 70938` are
appended unconditionally from `HotfixBuilder` (~lines 18037–18120). Against a database
that already has `VerifiedBuild = 70938` they delete nothing, but rows with `0`/`NULL`
would be removed, and without `ON DUPLICATE KEY UPDATE` you get `Duplicate entry`.

**Do:** either `--SkipOnlyVerifiedBuildUpdateRows true` in the config, or trim the list
to tables the sniff actually produced rows for.

---

## 6. Placeholder counters — small

`@CGUID`, `@OGUID`, `@ATSPAWNID`, `@ATPROPERTIESID`, `@MOVID` are emitted as
`SET_VALUE_MANUALLY_HERE`. Before import they must be set (`MAX(guid)+1` etc.).
Optional automation: an `AutoGenerateGuids` option in `SQLFile.WriteToFile` computed
from `Storage`, so the file imports without manual editing.

---

## 7. Hotfix noise and locales — small/medium

`ParseAllHotfixes = true` plus 258 `<Hotfix><FileHashes Enabled="true">` makes the log
verbose; the SQL is 3.6 MB for 16,599 rows, of which `broadcast_text` is 52 rows.
Narrow the `FileHashes` list to the DB2 hashes Forever actually sends.
`*_locale` tables need `ClientLocale != enUS`; the capture is `enUS`, so no locale rows
are produced — that is expected, not a bug.

---

## 8. Regression check after every change — small

Build + `dotnet test WowPacketParser.Tests` + one run on
`dump_1.60.1.70938_2026-11-14_12-41-07.pkt`, recording: table count, `INSERT` count,
row count, and the `tools/sql_tables.cjs` arity report. Without this it is very easy to
fix one table and shift the columns of twenty others.

Run command:

```
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config \
  --SQLFileName out.sql "<path>/dump_1.60.1.70938_2026-11-14_12-41-07.pkt"
```

---

## Effort summary

| # | task | size | effect |
|---|---|---|---|
| 1 | `TargetedDatabase.Forever` + `case ClientType.Forever` | 10 min | version-mismatch errors disappear, builders run |
| 2 | store queried templates in `Storage` | 0.5–1 day | real `creature_template`, `gameobject_template`, `page_text`, `npc_text` from the sniff |
| 3 | TDB Forever schema records | 1 day | output imports without manual column mapping |
| 4 | `Movement.cs` | 1–2 h | no malformed `creature_equip_template` rows |
| 5 | `DELETE` statements | 15 min | no risk of deleting data |
| 6 | placeholder counters | 2 h | import without editing the file |
| 7 | hotfix/locale noise | 2–3 h | smaller file, readable log |

Items 1, 4 and 5 are safe to do immediately — they do not move any column.
Item 2 is the real work and is no longer blocked: the wire layout is known from
`QueryPackets.cpp`. Item 3 only makes sense after 2.
