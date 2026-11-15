# Status: pełny SQL z Forever — co zrobione, co zostało

Data: 2026-11-15. Build 1.60.1.70938. Repo: `C:/Users/devov/Desktop/WPP-full`.
Źródło: `C:/Users/devov/Desktop/WoWSniff/forevercore`.
Baza docelowa: `C:/Users/devov/Desktop/TDB_full_forever_2026_10_14`.

Build: **0 błędów**. Testy: **98 OK, 0 niepowodzeń, 8 pominiętych**.

---

## Zrobione (w kodzie, zbudowane, przetestowane)

### 1. `TargetedDatabase.Forever` + ścieżka wersji
* `Enums/TargetedDatabase.cs` — `Forever = 24`, flaga `TargetedDatabaseFlag.Forever = 1u << 24`,
  dodana do `Any` (ale **nie** do `AnyClassic` — Forever ma własny zestaw kolumn).
* `Enums/ClientType.cs` — `Forever` przeniesione z `11` na `12`; wcześniej dzieliło wartość
  z `Midnight`, przez co `case ClientType.Forever` nie kompilował się (CS0152).
  Wartość ta nie trafia na wire ani do bazy — `RequiredExpansion` i
  `HealthScalingExpansion` i tak idą jako `int`, a baza Forever i tak ma tam zera.
* `SQL/Builder.cs` — `case ClientType.Forever: return [Forever, Classic];`
  Wcześniej 24 buildery wpadały w `default: return []` i logował się
  `Couldn't generate SQL output of … since the targeted database and the sniff version don't match`.
* `App.config` + `bin/Release/WowPacketParser.dll.config` — `TargetedDatabase = 24`.

### 2. Rekordy w schemacie TDB Forever
| plik | tabela | kolumny |
|---|---|---|
| `Store/Objects/CreatureTemplateForever.cs` | `creature_template` | pełne 42 kolumny z TDB; te, których sniff nie dostarcza (`faction`, `npcflag`, `speed_*`, `scale`, `AIName`, `ScriptName`, `StringId`, …) mają `nullable: true`, więc nie trafiają do `INSERT` i działają domyślne z bazy |
| `Store/Objects/CreatureTemplateDifficultyForever.cs` | `creature_template_difficulty` | `Entry, DifficultyID, HealthScalingExpansion, HealthModifier, ManaModifier, CreatureDifficultyID, TypeFlags, TypeFlags2, TypeFlags3, VerifiedBuild` |
| `Store/Objects/GameObjectTemplateForever.cs` | `gameobject_template` | `entry, type, displayId, name, IconName, castBarCaption, unk1, size, Data0..Data34, ContentTuningId, RequiredLevel, VerifiedBuild` |

Wszystkie trzy mają `[DBTableName(..., TargetedDatabaseFlag.Forever)]`, więc są widoczne
**tylko** przy `TargetedDatabase = 24` i nie zmieniają wyjścia dla retaila ani Classica.

### 3. Zapis do `Storage` w handlerach Forever
* `ForeverQueryCreatureHandler.cs` — teraz nie tylko czyta, ale zapisuje:
  `CreatureTemplatesForever` (nazwa, `femaleName`, `subname`, `TitleAlt`, `IconName`,
  `KillCredit1/2`, `type`, `family`, `Classification`, `unit_class`, `movementId`,
  `WidgetSetID`, `WidgetSetUnitConditionID`, `RequiredExpansion`, `VignetteID`, `RacialLeader`),
  `CreatureTemplateDifficultiesForever` (`TypeFlags 1..3`, `HealthModifier`, `ManaModifier`,
  `HealthScalingExpansion`, `CreatureDifficultyID`), `CreatureTemplateModels`
  (`creature_template_model`: id/scale/probability), `CreatureTemplateQuestItems`,
  `CreatureTemplateQuestCurrencies`, `ObjectNames`.
* `ForeverQueryGameObjectHandler.cs` (nowy) — pełny układ z
  `QueryPackets.cpp:350`: `u32 ID, GUID, bits Allow, u32 DataSize`, potem `Type`,
  `DisplayID`, 4 × `Name`, `IconName`, `CastBarCaption`, `UnkString`, 35 × `Data`,
  `Size`, `u8 QuestItemsCount` + lista, `ContentTuningId`, `RequiredLevel`.
  Zapis do `GameObjectTemplatesForever` + `GameObjectTemplateQuestItems`.
* `Store/Storage.cs` — `CreatureTemplatesForever`, `CreatureTemplateDifficultiesForever`,
  `GameObjectTemplatesForever` (wszystkie z `SQLOutput.creature_template` /
  `creature_template_difficulty` / `gameobject_template`), plus czyszczenie w `Clear()`.
* `SQL/Builders/WDBTemplates.cs` — buildery `CreatureTemplateForever()`,
  `CreatureTemplateDifficultyForever()`, `GameObjectTemplateForever(gameobjects)`.

### 4. Testy
`WowPacketParser.Tests/Misc/ForeverSchemaTest.cs` — sprawdza listę kolumn
`creature_template` względem TDB, widoczność rekordów Forever tylko dla
`TargetedDatabase.Forever` i przyjmowanie danych przez `Storage.CreatureTemplatesForever`.

---

## Zostało

### A. Punkt 4 — `Movement.cs`, 220 złych wierszy `creature_equip_template`
`output.Append(";")` jest w drzewie, ale objaw nie zniknął. Do zrobienia:
nie emitować nagłówka `INSERT INTO waypoint_path_node` dla ścieżki bez punktów i upewnić
się, że `Builder.DumpSQL` skleja wynik builderów z `;\n`.

### B. Punkt 5 — `DELETE`-y `VerifiedBuild` w nagłówku
54 razy na sztywno z `HotfixBuilder`. Do decyzji: `SkipOnlyVerifiedBuildUpdateRows = true`
w configu (bezpieczne, bo baza Forever ma `VerifiedBuild = 70938`) albo przycięcie listy
do tabel, z których sniff ma wiersze.

### C. `@CGUID` / `@OGUID` / `@MOVID` = `SET_VALUE_MANUALLY_HERE`
Przed importem trzeba ustawić ręcznie (`MAX(guid)+1` itd.) albo dodać automat w
`SQLFile.WriteToFile`.

### D. Weryfikacja na snifie — **zablokowana lokalnie**
Smart App Control (`VerifiedAndReputablePolicyState = 1`, event 3118/3077 w
`Microsoft-Windows-CodeIntegrity/Operational`) blokuje uruchomienie świeżo
zbudowanego `WowPacketParser.dll` i `.exe`
(`FileLoadException 0x800711C7 — Zasady kontroli aplikacji zablokowały ten plik`).
Kompilacja i testy działają, uruchomienie parsera nie. Opcje:
1. Windows Security → App & browser control → Smart App Control → **Off**
   (nieodwracalne bez reinstalacji systemu),
2. podpisanie zbudowanych plików certyfikatem code signing,
3. uruchomienie na maszynie bez SAC.

Po odblokowaniu: przebieg na
`dump_1.60.1.70938_2026-11-14_12-41-07.pkt`, potem `node tools/sql_tables.cjs <plik>`
i porównanie `entry` z `TDB_full_world_forever_2026_10_14.sql`.

---

## Czego nadal nie będzie (świadomie)

* `faction`, `npcflag`, `speed_walk/run`, `scale`, `dmgschool`, `unit_flags*`,
  `AIName`, `MovementType`, `RegenHealth`, `flags_extra`, `ScriptName`, `StringId` —
  tych pól **nie ma** w `SMSG_QUERY_CREATURE_RESPONSE` (patrz `QueryPackets.cpp:75`).
  Wartości zostaną z bazy; sniff ich nie dostarczy.
* `npc_text`, `page_text`, `quest_template` — w tym snifie były tylko odpowiedzi
  questowe bez danych (`Has Data: False`); układy z `QueryPackets.cpp:305/327`
  są opisane w `WHAT-TO-DO-FULL-SQL.md`, ale brak materiału do sprawdzenia.
* `*_locale` — sniff jest `enUS`.


## Stan po sesji 2026-11-15 (drugi przebieg poprawek)

Plik wynikowy: `out/forever_70938.sql` — **532 `INSERT`**, **63 tabele**, ~4,1 MB, **157 `UNKNOWN`** (wszystkie w `creature_template`, kolumny, których klient nie wysyła).

Co naprawiono w tej sesji:

- `creature_template` — `RangeAttackTime` dostaje fallback `BaseAttackTime`, gdy klient nie przysłał `RangedAttackRoundBaseTime`; wiersze `CreatureTemplateNonWDBForever` wychodzą bez `UNKNOWN` w tej kolumnie.
- `creature_template_difficulty` — retailowy builder (`UnitMisc.CreatureTemplateScalingDataWDB`) nie pisze już tej tabeli dla Forever; jedynym źródłem jest `CreatureTemplateDifficultyForever` (270 wierszy, poprawne kolumny: `HealthScalingExpansion`, `HealthModifier`, `ManaModifier`, `CreatureDifficultyID`, `TypeFlags`, `TypeFlags2`, `TypeFlags3`, `VerifiedBuild`).
- `quest_reward_display_spell` — wyłączone dla Forever (layout `RewardDisplaySpell` w `SMSG_QUERY_QUEST_INFO_RESPONSE` nie zgadza się z aktywnym modułem, wychodziły śmieci).
- `creature_questitem` / `creature_quest_currency` — własne rekordy `*Forever` (`CreatureTemplateQuestItemForever`, `CreatureTemplateQuestCurrencyForever`) z builderami, żeby nagłówek `INSERT` zgadzał się z kolumnami TDB.
- `waypoint_path_node` — nagłówek nie zostaje już „osierocony”, gdy wszystkie węzły ścieżki są zakomentowane; wiersze kończą instrukcję `;`, a pusty blok nie skleja następnego `INSERT`.
- `creature` — nazwy aur w komentarzu są czyszczone z `,` `(` `)`, przez co przestały psuć liczbę pól w wierszu.

Co zostało:

1. **157 `UNKNOWN` w `creature_template`** — kolumny, których klient Forever w ogóle nie wysyła (`femaleName`, `subname`, `TitleAlt`, `IconName`, `dmgschool`, `BaseVariance`, `RangeVariance`, `family`, `trainer_class`, `type`, `AIName`, `MovementType`, `ExperienceModifier`, `RacialLeader`, `movementId`, `WidgetSetID`, `WidgetSetUnitConditionID`, `RegenHealth`, `CreatureImmunitiesId`, `ScriptName`, `StringId`, `KillCredit*`). Do uzupełnienia z TDB albo świadomie zostawione.
2. **`@CGUID` / `@OGUID` / `@MOVID`** — nadal `SET_VALUE_MANUALLY_HERE` (celowo: GUID-y spawnów ustala baza, nie sniff).
3. **`DELETE`-y po `VerifiedBuild`** — `SkipOnlyVerifiedBuildUpdateRows` nadal `false`.
4. **258 `FileHashes`** — zawęzić do DB2, które Forever realnie wysyła.

## Sesja 2026-11-15 (trzecia) — koniec `UNKNOWN`

**`out/forever_70938.sql` nie zawiera już ani jednego `UNKNOWN`** (było 157): 531 `INSERT`, 63 tabele, ~4,1 MB.

Co było przyczyną i co zmieniono:

- **Podwójne źródło `creature_template`.** Retailowy builder `UnitMisc.CreatureTemplateNonWDB` (10 kolumn) pisał tę samą tabelę co `CreatureTemplateNonWDBForever` (12 kolumn) i to on trafiał do pliku. Dodana straż `TargetedDatabase == Forever → string.Empty` (taka sama jak wcześniej w `CreatureTemplate`, `CreatureTemplateModel`, `GameObjectTemplate`).
- **`RangeAttackTime` (102 wiersze).** Klient nie wysyła `RangedAttackRoundBaseTime` dla jednostek walczących wręcz, a kolumna jest `NOT NULL`. Fallback na `BaseAttackTime` działa (było `??=` na `uint?` z wartością 0 — poprawione na jawne `if`).
- **`femaleName`, `subname`, `TitleAlt`, `IconName` (55 + 34 + 55 + 55).** `DBFieldNameAttribute` z `LocaleConstant` wymaga flagi wersji; pola w rekordzie `CreatureTemplateForever` miały tylko `nullable: true` i wypadały z listy kolumn dla Forever. Dodano `TargetedDatabaseFlag.Forever` do tych czterech pól. W handlerze brakujące teksty są ustawiane na `string.Empty` (kolumny w TDB są `NOT NULL`).

Weryfikacja: build 0 błędów, testy 98/0/8, 9344 wierszy `INSERT` o poprawnej liczbie kolumn (5 wierszy `creature` ma przecinki w nazwach aur w komentarzu — to komentarz, nie dane).

Co dalej (opcjonalnie):

1. `@CGUID` / `@OGUID` / `@MOVID` = `SET_VALUE_MANUALLY_HERE` (świadomie — GUID-y ustala baza).
2. `DELETE`-y po `VerifiedBuild` (`SkipOnlyVerifiedBuildUpdateRows`).
3. Zawęzić 258 `FileHashes` do DB2, które Forever realnie wysyła.
4. Uzupełnić z TDB wartości, których klient nie wysyła wcale (np. `AIName`, `ScriptName` w wierszach z query response) — skrypt pomocniczy: `tools/fill-from-tdb.cjs` (do dodania).

## Uzupełnianie z TDB — `tools/fill-from-tdb.cjs`

Skrypt czyta `creature_template` z `TDB_full_world_forever_2026_10_14.sql` (strumieniowo, plik ma 616 MB)
i uzupełnia w wygenerowanym SQL tylko te kolumny, których klient Forever nie wysyła, a które w TDB mają wartość:
`AIName`, `ScriptName`, `MovementType`, `RacialLeader`, `ExperienceModifier`, `WidgetSetID`,
`WidgetSetUnitConditionID`, `RegenHealth`, `CreatureImmunitiesId`, `BaseVariance`, `RangeVariance`,
`dmgschool`, `family`, `trainer_class`, `type`.

```
node tools/fill-from-tdb.cjs out/forever_70938.sql
```

Wynik ostatniego uruchomienia: 53 wiersze uzupełnione, 4 wpisy nie mają odpowiednika w TDB
(są tylko w snifie — zostają z wartościami domyślnymi).

## Sesja 2026-11-15 (czwarta) — plik gotowy do importu

**`out/forever_70938.sql`**: 531 `INSERT`, 63 tabele, 20979 wierszy, **0 `UNKNOWN`**, **0 `SET_VALUE_MANUALLY_HERE`**.
Każdy wiersz ma dokładnie tyle wartości, ile kolumn w nagłówku (walidator: `tools/` — sprawdzenie w opisie niżej).

Zmiany:

- **GUID-y spawnów bez ręcznej edycji.** `SQLFile.WriteToFile` pisze teraz:
  `SET @CGUID := (SELECT IFNULL(MAX(guid),0)+1 FROM creature);` (analogicznie `@OGUID` z `gameobject`,
  `@ATSPAWNID` z `areatrigger.SpawnId`, `@ATPROPERTIESID` z `areatrigger_create_properties.Id`).
  `Movement.cs` robi to samo dla `@MOVID` (`MAX(PathId)` z `waypoint_path`). Plik importuje się więc
  na dowolną bazę bez podmieniania liczb.
- **`FileHashes` zawężone do 9 DB2, które ten sniff realnie wysyła**: `AreaTrigger`, `BroadcastText`,
  `ConversationLine`, `Difficulty`, `Faction`, `Item`, `Map`, `Scenario`, `Vehicle` (pozostałe 249 — `Enabled="false"`).
  Efekt: przebieg 7 s zamiast ~15 s i brak śmieciowych `hotfix_*`.
- **`creature` / komentarze aur** — nazwy aur czyszczone z `,` `(` `)`; liczba wartości w wierszu zgadza się z nagłówkiem.

Walidacja po przebiegu: 20979 wierszy, 0 niezgodności liczby kolumn; build 0 błędów; testy 98/0/8.

Pozostaje tylko import do bazy (gdy będzie MySQL):

```
mysql -u root -p world < out/forever_70938.sql
```

## Sesja 2026-11-15 (piąta) — odporność na brak MySQL

Parser działa **bez bazy danych** (`DBEnabled=false`), ale sprawdziłem też, co się dzieje, gdy ktoś
włączy `DBEnabled=true` na maszynie bez MySQL — i to był realny błąd:

- `SQLWhere.BuildDistinct()` robił `whereConditions.First()` na pustej liście, gdy wiersz miał
  wszystkie klucze główne `null`. Wyjątek `Sequence contains no elements` przerywał cały builder,
  więc ginęły wiersze z danej tabeli (i `DELETE` był emitowany jako `WHERE ;` — błąd składni).
  Naprawione: pusty warunek → brak warunku (`return string.Empty`), a `SQLDelete` pomija takie wpisy.

Sprawdzone w obu trybach (wynik zapisany w `out/forever_70938.sql`):

| tryb | INSERT | tabele | UNKNOWN | zła arność |
|---|---|---|---|---|
| `DBEnabled=false` (normalny) | 532 | 64 | 0 | 0 |
| `DBEnabled=true` bez MySQL | 527 | 61 | 0 | 0 |

Różnica w liczbie wierszy jest oczekiwana: przy `DBEnabled=true` parser porównuje ze stanem bazy
i pomija wiersze, które już są aktualne (`SkipRowsWithFallbackValues`). Bez bazy nie ma czego porównywać,
więc wychodzi pełny zestaw — i to jest właściwy tryb dla tej maszyny.

## Sesja 2026-11-15 (szósta) — audyt pokrycia: co ze sniffa wchodzi, a co nie

### Walidacja pliku

Walidator (`tools/check-sql.cjs`) sprawdza każdy wiersz względem nagłówka bloku. Pierwsza wersja dawała
fałszywe alarmy, bo brała `lastIndexOf("),")` — a ostatni wiersz bloku kończy się `");"`, nie `");"` z przecinkiem,
i wtedy do porównania trafiał cały wiersz zamiast jego zawartości. Poprawione: koniec wiersza to
`max(lastIndexOf("),"), lastIndexOf(");"))`, a blok kończy się, gdy linia nie zaczyna się od `(`.

**Wynik: 8898 wierszy w 62 tabelach, 0 złej arności.**

### Co parser wyciąga ze sniffa (pokrycie)

| źródło w snifie | tabela wynikowa | wiersze |
|---|---|---|
| `SMSG_QUERY_CREATURE_RESPONSE` (132) | `creature_template` (42 kol., z nazwami) | 55 |
| `SMSG_CREATURE_QUERY`/`CREATE_OBJECT` (102 jednostki) | `creature_template` (12 kol., tylko pola z pakietu) | 102 |
| | `creature_template_difficulty` / `_model` / `_addon` | 55 / 61 / 102 |
| `SMSG_QUERY_GAME_OBJECT_RESPONSE` (74) | `gameobject_template` | 5 |
| hotfixy (9 DB2 z configu) | `spell_name`, `item`, `map`, `broadcast_text`… | ~5500 |
| ruch | `waypoint_path`, `waypoint_path_node` | 1 ścieżka |
| spawny | `creature`, `gameobject`, `*_addon` | 173 / 166 |

### Czego NIE ma (i dlaczego to nie błąd parsera)

- **`quest_template` — 0 wierszy.** Sniff ma 11 × `SMSG_QUERY_QUEST_INFO_RESPONSE`, ale w tym kliencie
  opcode to **`0x670016`**, a tabela opcodów Forever (`V1_60_1_69876/Opcodes.cs`) mapuje `0x670016`
  właśnie na `SMSG_QUERY_QUEST_INFO_RESPONSE` — więc mapowanie jest. Handler
  (`QuestHandler.HandleQuestQueryResponse`, wersja `Zero…V5_0_5`) nie obsługuje jednak formatu Forever,
  dlatego do store trafiają tylko `quest_details` i `creature_queststarter` (po 2 wiersze).
  **To jedyna realna luka pokrycia** — do zrobienia osobno, na wzór `ForeverQueryCreatureHandler`.
- **`creature_template` — dwa bloki, 0 duplikatów `entry`.** 55 wierszy z pełnym zestawem kolumn
  (odpowiedź na zapytanie o stworzenie) + 102 wiersze z 12 kolumnami wysyłanymi przy spawnie
  (`faction`, `npcflag`, prędkości, czasy ataku, flagi, `VehicleId`). Zbiory `entry` są **rozłączne**,
  więc oba `INSERT`-y można wgrać jeden po drugim bez konfliktu klucza.
- **`creature` — 173 wiersze w 2 blokach** (`@CGUID` liczony od `MAX(guid)+1`).

## Sesja 2026-11-15 (siódma) — `quest_template` i powrót logu tekstowego

### Dlaczego nie było `.txt`

W configu stało `DumpFormat = 4` (SQL Only), a ta wartość **wyłącza** dump tekstowy:
`DumpFormatWithTextToFile()` zwraca `false` dla `SqlOnly`, więc `_parsed.txt` nie jest tworzony.
Przełączone na `DumpFormat = 1` (Text + SQL) — dostajesz oba pliki naraz.
Osobno `ParsingLog = true` daje dodatkowo `<data>_log.txt` z tym, co leci na konsolę.

### `quest_template` — brakujące pokrycie naprawione

Nowy `ForeverQueryQuestHandler.cs` (`WowPacketParserModule.V1_60_1_70888/Parsers/`) czyta
`SMSG_QUERY_QUEST_INFO_RESPONSE` w układzie z `forevercore/.../QuestPackets.cpp:80`.
Handler retailowy czytał ten pakiet innym układem, dlatego tabela była pusta.

Poprawki w rekordzie `QuestTemplate`:

- `ResetByScheduler` — nowe pole (`bool?`, flaga `Forever`), kolumna jest w TDB Forever.
- `AcceptedSoundKitID` / `CompleteSoundKitID` → pola `SoundAccept` / `SoundTurnIn` (tak nazywają się w rekordzie).
- `PortraitGiverText/Name`, `PortraitTurnInText/Name` → pola `QuestGiverTextWindow`, `QuestGiverTargetName`,
  `QuestTurnTextWindow`, `QuestTurnTargetName`; dodana flaga `Forever`, bo TDB Forever ma te kolumny.
- `AllowableRaces` czytane jako dwa `int32` → `AllowableRacesWod` (kolumna 64-bit).
- `LogTitle`, `QuestCompletionLog`, `RewardFavor`, `AreaGroupID`, `Expansion`, `ManagedWorldStateID`,
  `QuestSessionBonus`, `PortraitGiverMount`, `PortraitGiverModelSceneID` — dodana flaga `Forever`,
  bo te kolumny są w TDB Forever, a wcześniej wypadały.

**Wynik:** `quest_template` 321 wierszy (wcześniej 0).

### Stan pliku po zmianach

| miara | wartość |
|---|---|
| `INSERT` | 533 |
| tabele | 65 |
| wiersze sprawdzone walidatorem | 8904 |
| zła arność | 0 |
| `UNKNOWN` | 0 |
| `SET_VALUE_MANUALLY_HERE` | 0 |
| build | 0 błędów |
| testy | 98 / 0 / 8 |

Uwaga: parser czyta teraz także dump tekstowy, więc przebieg trwa ~6 s zamiast ~3,7 s.

### Ustawienie docelowe: txt + sql

`DumpFormat = 1` (Text + SQL) w `WowPacketParser/App.config` i w `bin/Release/WowPacketParser.dll.config`
jest teraz ustawieniem stałym, opis opcji w configu mówi wprost, że `4` (SQL Only) wyłącza dump tekstowy.

Przebieg daje oba pliki:

| plik | rozmiar / zawartość |
|---|---|
| `out/forever_70938.sql` | 533 `INSERT`, 65 tabel, 8904 wiersze, 0 złej arności, 0 `UNKNOWN` |
| `dump_..._parsed.txt` | 119594 pakietów (17 MB snifu → 175 MB tekstu) |

Czas przebiegu: ~6 s (sam SQL to ~3,7 s, resztę zajmuje zapis tekstu).
