# Co mamy — źródło, baza, i co z tego wynika

Sprawdzone 2026-11-14. To jest **uzupełnienie** `PLAN-PELNY-SQL.md`: tam pisałem,
że potrzebujemy drugiego źródła dla 37 nieznanych bajtów i schematu. **Oba już są
na dysku.** Wnioski poniżej są z kodu, nie z pamięci.

---

## 1. Źródło emulatora — `C:/Users/devov/Desktop/WoWSniff/forevercore`

To **TrinityCore z gałęzi Forever** (`git log`: `e6399e4f`, `forever-bots`).
Plik z układem pakietów:
`src/server/game/Server/Packets/QueryPackets.cpp` + `QueryPackets.h`.

(Uwaga: `C:/Users/devov/Desktop/server_forever` to **tylko build**, źródła leżą
w `Documents/GitHub/TrinityCore` — ta sama gałąź. Kanoniczne jest `forevercore`.)

### `SMSG_QUERY_CREATURE_RESPONSE` — rozwiązanie zagadki 37 bajtów

`QueryPackets.cpp:75` `QueryCreatureResponse::Write()`, dokładny układ:

```
Bits<6>(1), FlushBits
u32  CreatureID
u8   result              (0 = dane są, 1 = brak)
jeśli result == 0:
  Bits<11> Title, Bits<11> TitleAlt, Bits<6> CursorName, Bits<1> Leader
  4 × ( Bits<11> Name[i], Bits<11> NameAlt[i] )
  Name[0..3], NameAlt[0..3]                      (SizedCString::Data)
  Stats.Flags.data()       ← std::array<uint32, 3>  = 12 B   (TypeFlags, TypeFlags2, TypeFlags3)
  u8   CreatureType
  i32  CreatureFamily
  i8   Classification
  ProxyCreatureID.data()   ← std::array<uint32, 2>  =  8 B   (KillCredit1, KillCredit2)
  Size<u32> DisplayCount
  f32  TotalProbability
  per display: u32 CreatureDisplayID, f32 Scale, f32 Probability
  f32  HpMulti
  f32  EnergyMulti
  Size<u32> QuestItems
  Size<u32> QuestCurrencies
  8 × i32: CreatureMovementInfoID, HealthScalingExpansion, RequiredExpansion,
           VignetteID, Class, CreatureDifficultyID, WidgetSetID, WidgetSetUnitConditionID
  Title, TitleAlt, CursorName                    (SizedCString::Data)
  QuestItems[], QuestCurrencies[]
```

**37 bajtów = 12 (3× Flags) + 1 (type) + 4 (family) + 1 (classification) + 8
(2× ProxyCreatureID) + 4 (DisplayCount) + 4 (TotalProbability) + 3 (padding
wyrównania)**. Parser w WPP czyta to poprawnie — tylko te 37 bajtów traktuje
jako „nieznane”, bo nie zapisuje ich do modelu. **Nic nie trzeba zgadywać.**

Skąd wartości: `Creature.cpp:184` `CreatureTemplate::BuildQueryData` —
`Flags[0..2] = TypeFlags, TypeFlags2, TypeFlags3`, `ProxyCreatureID = KillCredit[0..1]`,
`Title = SubName`, `TitleAlt = TitleAlt`, `CursorName = IconName`,
`HpMulti = HealthModifier`, `EnergyMulti = ManaModifier`,
`Class = unit_class`, `CreatureMovementInfoID = movementId`.

### `SMSG_QUERY_GAME_OBJECT_RESPONSE`

`QueryPackets.cpp:350`:

```
u32 GameObjectID
GUID Guid
Bits<1> Allow, FlushBits
Size<u32> statsDataLength
statsData:
  i32 Type, i32 DisplayID
  4 × string Name[0..3]
  string IconName, string CastBarCaption, string UnkString
  35 × i32 Data[0..34]          (MAX_GAMEOBJECT_DATA = 35, SharedDefines.h:3234)
  f32 Size
  Size<u8> QuestItems, potem QuestItems[]
  i32 ContentTuningId, i32 RequiredLevel
```

Zgadza się z tym, co widać w sniffie: `DataSize: 185`, `Type: 5`, `Display ID: 309`,
`[0..15] Data`, `[0..3] Name`.

### `SMSG_QUERY_PAGE_TEXT_RESPONSE` / `SMSG_QUERY_NPC_TEXT_RESPONSE`

`QueryPackets.cpp:305` i `:327` — oba zaczynają się od `u32 ID`, `Bits<1> Allow`,
`FlushBits`, a potem:
* page_text: `Size<u32> Pages`, per strona `u32 ID, u32 NextPageID, i32 PlayerConditionID,
  u8 Flags, Bits<12> TextLen, FlushBits, Text`,
* npc_text: `i32 (MAX_NPC_TEXT_OPTIONS * 8)`, potem `Probabilities[]`, `BroadcastTextID[]`.

---

## 2. Baza — `C:/Users/devov/Desktop/TDB_full_forever_2026_10_14`

`TDB_full_world_forever_2026_10_14.sql` (616 MB, **262 tabele**) +
`TDB_full_hotfixes_forever_2026_10_14.sql` (341 MB).

**To jest schemat docelowy i on nie jest retailowy.** Przykłady:

### `creature_template` — 42 kolumny (TDB, linia 1711)

```
entry KillCredit1 KillCredit2 name femaleName subname TitleAlt IconName
RequiredExpansion VignetteID faction npcflag speed_walk speed_run scale
Classification dmgschool BaseAttackTime RangeAttackTime BaseVariance RangeVariance
unit_class unit_flags unit_flags2 unit_flags3 family trainer_class type VehicleId
AIName MovementType ExperienceModifier RacialLeader movementId WidgetSetID
WidgetSetUnitConditionID RegenHealth CreatureImmunitiesId flags_extra ScriptName
StringId VerifiedBuild
```

Czego **nie ma** w WPP: `faction`, `npcflag`, `speed_walk/run`, `scale`, `dmgschool`,
`BaseAttackTime`, `RangeAttackTime`, `BaseVariance`, `RangeVariance`, `unit_flags*`,
`trainer_class`, `VehicleId`, `AIName`, `MovementType`, `ExperienceModifier`,
`RegenHealth`, `CreatureImmunitiesId`, `flags_extra`, `ScriptName`, `StringId`.
Czego jest za dużo: `HealthModifier`, `ManaModifier`, `PetSpellDataId`, `type_flags`,
`type_flags2` (a `TypeFlags3` nie ma pola w rekordzie).

**Wniosek:** `CreatureTemplateClassic` z `Store/Objects/CreatureTemplate.cs` też nie
pasuje. Trzeba **własnego rekordu `CreatureTemplateForever`** z tymi 42 polami
(te, których sniff nie dostarcza, zostają `NULL`/domyślne — i to jest OK, bo TDB
ma tam dane).

### `gameobject_template` — 51 kolumn

```
entry type displayId name IconName castBarCaption unk1 size
Data0…Data34 ContentTuningId RequiredLevel AIName ScriptName StringId VerifiedBuild
```
To pokrywa się z rekordem `GameObjectTemplate` w WPP (Data × 35 = `MAX_GAMEOBJECT_DATA`),
z wyjątkiem `AIName/ScriptName/StringId` — sniff ich nie ma, zostają domyślne.

### Kolumny `data` w TDB mają typ `int`, `VerifiedBuild int NOT NULL DEFAULT 0`

Czyli `NULL` w `INSERT` na kolumnie `NOT NULL` wywali import. Każdy builder musi
wypisywać `0`/`''` zamiast `NULL` — albo kolumny muszą dostać `DEFAULT`.

---

## 3. Co to zmienia w planie (`PLAN-PELNY-SQL.md`)

| punkt planu | przed | po (mamy źródło + bazę) |
|---|---|---|
| 2 — zapis `Storage` | „1–2 dni, blokada: 37 nieznanych bajtów” | **brak blokady** — układ jest znany z `QueryPackets.cpp`; 0,5–1 dnia |
| 3 — schemat docelowy | „retail czy Forever, decyzja” | **decyzja podjęta: schemat = TDB Forever**, 262 tabele, rekordy `*Forever` w `Store/Objects` |
| nowe — walidacja | — | porównać wynik z TDB: `entry` z sniffa musi istnieć w `creature_template` (poza nowymi NPC) |

### Kolejność, teraz konkretna

1. `case ClientType.Forever` w `Builder.GetExpectedTargetDatabasesForExpansion`
   (+ `TargetedDatabase.Forever` w enumie) — bez tego buildery nie ruszają.
2. `CreatureTemplateForever` + `GameObjectTemplateForever` w `Store/Objects`
   z kolumnami **dokładnie** jak TDB (§2) i `TargetedDatabaseFlag.Forever`.
3. `ForeverQueryCreatureHandler`: dodać `Storage.CreatureTemplatesForever.Add(...)`
   wg układu z §1; `ForeverQueryGameObjectHandler` analogicznie (35 × Data).
4. `Movement.cs` — średnik/pusty nagłówek (punkt 4 starego planu, nadal aktualny).
5. Test: `tools/sql_tables.cjs` + sprawdzenie `entry` w TDB.

**Punkt 2 starego planu przestaje być ryzykowny**: nie trzeba zgadywać pola po polu,
wystarczy przepisać `QueryPackets.cpp` linia po linii i porównać wynik z TDB.
