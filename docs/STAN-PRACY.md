# Stan pracy — WPP-full / Forever 1.60.1 (build 70938)

Ostatnia aktualizacja: 2026-11-15. Repo: `C:/Users/devov/Desktop/WPP-full`, branch `master`, remote `devovh`.
Ostatni commit: `749431604` — *docs: update remaining work after fix3 and the second sniff*.

## Poprawki developera (fix3, nałożone 2026-11-15)

Cztery patche z `C:/Users/devov/Desktop/fix3` nałożone przez `git apply`, build 0 błędów, testy 98/0/8:

1. `0001` — hotfixy Forever kasują wiersze **po ID** (`WHERE VerifiedBuild>0 AND ID IN (...)`), a nie całą tabelę. W pliku 0 całotabelowych `DELETE ... WHERE VerifiedBuild>0;`.
2. `0002` — usunięte `ALTER TABLE ... AUTO_INCREMENT = (SELECT ...)` (błąd składni w MySQL) i dodany przebieg kasujący duplikaty spawnów (ten sam entry i map w promieniu 5 jardów od niższego guid) oraz osierocone `creature_addon` / `spawn_tracking`.
3. `0003` — `creature_template`: flagi `femaleName`/`subname`/`TitleAlt`/`IconName` wracają do upstreamu, Forever tylko w `femaleName` i `TitleAlt`. Plik z powrotem LF.
4. `0004` — nowy `ForeverQueryNpcTextHandler.cs` czyta `SMSG_QUERY_NPC_TEXT_RESPONSE` (0x4B0008) do `Storage.NpcTextsForever`; kolumny poprawione na `Probability0..7` / `BroadcastTextID0..7`.

Uwaga: moja wcześniejsza wersja `npc_text` (przez `AddBroadcastTextToGossip` z `SMSG_GOSSIP_MESSAGE`) została zastąpiona wersją developera.

---

## Co jest zrobione

### Parser / wyjście SQL
- `TargetedDatabase.Forever = 24`, `ClientType.Forever = 12`.
- Config „wszystko”: `SQLOutput.* = true`, `KeepSQLFile = false`, `DumpFormat = 1`, `ParsingLog = true`, `DBEnabled = false`, `SplitSQLFile = false`.
- Rekordy Forever: `CreatureTemplateForever` (42 kol.), `CreatureTemplateDifficultyForever`, `GameObjectTemplateForever` (51 kol.), `NpcTextForever` (18 kol.), buildery Forever w `WDBTemplates.cs`.
- `quest_template` dla Forever: `ForeverQueryQuestHandler.cs` (`SMSG_QUERY_QUEST_INFO_RESPONSE` 0x670016) — 321 wierszy.
- `quest_reward_display_spell`: handler Forever czyta `(SpellID, PlayerConditionID, Type)` i zapisuje do `Storage.QuestRewardDisplaySpells`; builder nie jest już wyłączony dla Forever.
- `npc_text` Forever: tylko `Probability0-7` + `BroadcastTextID0-7`; wypełniane z `SMSG_GOSSIP_MESSAGE` przez `AddBroadcastTextToGossip` (nowe w `V12_0_0_65390/Parsers/NpcHandler.cs`).
- `page_text`: `PlayerConditionID` i `Flags` mają dopisaną flagę `Forever`.
- Hotfixy Forever idą przez fallback `V12_0_0_65390` (wariant `1200`), bez osobnej gałęzi `Forever` w `HotfixBuilder`.
- `AllowableRace` w `ItemSparseHotfix1200` / `ItemSearchNameHotfix1200`: `int?[]` (2 kolumny) zamiast `long?`.

### Poprawki z review developera
1. Usunięty debug-zapis `%TEMP%\q\prod.txt` z `Builder.cs`.
2. `KeepSQLFile` — plik kasowany tylko przy pustym `SQLFileName`.
3. GUID-y spawnów: `SET @CGUID/@OGUID/@ATSPAWNID/@ATPROPERTIESID/@MOVID` liczone **przed każdym blokiem** + `ALTER TABLE ... AUTO_INCREMENT` na końcu. Drugi import = no-op.
4. Spawny nie trafiają do plików bez `@CGUID` (`SplitSQLFile`).
5. `femaleName`/`subname`/`TitleAlt`/`IconName`: `Forever | SinceCataclysm` (vanilla/TBC/WotLK bez zmian).

### Narzędzia
- `tools/check-sql.cjs` — walidator arności wierszy.
- `tools/patch-from-tdb.cjs` — uzupełnia kolumny spoza pakietu wartościami z TDB, wstawiane w pozycji wg schematu TDB: `item.ItemPetFoodID`, `item_sparse.AmmunitionType`, `area_table.ExplorationLevel`, `map.OceanLiquidTypeID`.
- `tools/sql_tables.cjs` — lista tabel w wyjściu.

---

## Wynik ostatniego przebiegu

```
out/forever_70938.sql
8904 wiersze | 63 tabele | 0 złych arności
117205/119594 pakietów (98.002%) OK, ~6 s
build: 0 błędów | testy: 98 passed / 0 failed / 8 skipped
```

Polecenia (cmd.exe):

```cmd
cd C:\Users\devov\Desktop\WPP-full
dotnet build WowPacketParser.slnx -c Release
cd WowPacketParser\bin\Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config --SQLFileName forever_70938.sql "C:\Users\devov\Desktop\regedit_70790\dumps\dump_1.60.1.70938_2026-11-14_12-41-07.pkt"
cd C:\Users\devov\Desktop\WPP-full
move WowPacketParser\bin\Release\forever_70938.sql out\forever_70938.sql
node tools\patch-from-tdb.cjs
node tools\check-sql.cjs out\forever_70938.sql
```

---

## NOWE ZAMÓWIENIE OD DEVELOPERA (2026-11-15) — to jest następna praca

Cytat: *„nice, cheers for taking them. if you fancy more, these would help us most, roughly in order”*. Pięć punktów, priorytet taki jak w liście.

### 1. faction, npcflag, level, unit_flags, scale → `creature_template` + `creature_template_difficulty`
Dev: w Ironforge widzieli Magister Falath, Turlethion Moonrage i Drake Ekans, ale z query dostali tylko nazwę i model — „so we couldn't make them”.

**Ustalone w kodzie (stan wyjściowy):**
- `CreatureTemplateNonWDBForever` (`WowPacketParser/Store/Objects/CreatureTemplate.cs:195`) już pisze `faction`, `npcflag`, `speed_walk`, `speed_run`, `BaseAttackTime`, `RangeAttackTime`, `unit_class`, `unit_flags`, `unit_flags2`, `unit_flags3`, `VehicleId` — **148 wierszy** w `out/forever_70938.sql` (drugi INSERT, linia 56132).
- Źródło: `WDBTemplates.cs:214` `CreatureTemplateNonWDBForever(Dictionary<WowGuid, Unit> units)`, czyta `npc.UnitData.FactionTemplate`, `NpcFlags[0..1]`, `Flags/Flags2/Flags3`, `Movement.RunSpeed/WalkSpeed`.
- **`scale` NIE jest zapisywany** — jest tylko `DisplayScale` w `creature_template_model` z query. Do dodania.
- **`level` NIE jest zapisywany** — `UnitData.Level` (`ForeverUnitDataReader.cs:153`) jest czytany, ale nie trafia do SQL.
- **Poziomu nie ma też w `creature_template`** (42 kolumny, sprawdzone w TDB). Prawdopodobnie idzie przez `ContentTuningID` w `creature_template_difficulty` — sniff ma **30581 wystąpień `ContentTuningID`**, wartości m.in. `0, 10, 11, 12, 1164800`. `UnitData.ContentTuningID` jest czytany (`ForeverUnitDataReader.cs:155`), ale `CreatureTemplateDifficultyForever` **nie ma tej kolumny** (ma ją tylko retailowy `CreatureTemplateDifficulty.cs:83`). **To pierwszy trop do sprawdzenia w tc-forever.**

**Do zrobienia:** dodać `scale` (z `UnitData.ScaleX` / display scale) i `ContentTuningID` (+ ewentualnie `LevelScalingDeltaMin/Max`) do rekordów Forever i do buildera.

### 2. `npc_vendor` — maxcount, incrtime, ExtendedCost, PlayerConditionID
- W 70938: **0 wierszy**. W 70862: jest `npc_vendor` — sprawdzić, czy pola są kompletne.
- Do zrobienia: `maxcount` i `incrtime` dla ograniczonego stocku + `ExtendedCost` + `PlayerConditionID`.

### 3. `creature_loot_template` — z okien lootu
- W 70938: **0 wierszy**.
- Wymagania deva: **pomijać okna skinningu (`AcquireReason 6`)**, dodać komentarz z `drops/kills`, żeby było widać, z ilu zabójstw wynika szansa.

### 4. `gossip_menu`, `gossip_menu_option`, `creature_template_gossip`
- Teraz: `gossip_menu` 1, `gossip_menu_option` 1, `creature_template_gossip` 1 — za mało.
- Do zrobienia: z pakietów gossip, z **broadcast text ids**.

### 5. `waypoint_path` — dla NPC-ów, które chodzą
- Teraz: **0 wierszy**. `creature_addon` jest (2 INSERT-y).
- Do zrobienia: `waypoint_path` połączony z `creature_addon.PathId`, żeby miejskie NPC (Myra, łapacze złodziei, kurier) wyszły z trasami.

---

## Co zostało (kolejność)

0. **Punkty 1–5 developera powyżej** — to jest teraz główna praca.
1. **Import do bazy** — brak MySQL (świadomie pominięte). To jedyna rzecz, która sprawdzi plik naprawdę: składnię, klucze obce, `@CGUID`/`@NPCTEXTID`, drugi import bez duplikatów. Po postawieniu: `mysql -u root -p world < out\forever_70938.sql`.
2. **`page_text`** — kod gotowy, ale żaden z dwóch sniffów nie ma pakietu z tekstem strony. Trzeba nagrać sniff z czytaniem książek / tablic / znaków.
3. **`SkipOnlyVerifiedBuildUpdateRows`** — po patchu `0001` hotfixy kasują po `ID`, więc ten problem jest już rozwiązany dla Forever. Flaga zostaje `false`; zmienić tylko jeśli dev poprosi.
4. **Hotfixy bez odpowiednika w TDB** — 20 z 68 `TableHash` (`Spell`, `ItemDisplayInfo`, `CreatureSoundData`, `QuestObjective`, `GlobalStrings`, …); 14 tabel z odpowiednikiem w TDB nie dostało `INSERT` (m.in. `ModifiedCraftingItem` 154 rek., `TransmogrHoliday` 154, `Gameobjects` 75). Do decyzji, czy warto to dłubać.
5. **Drugi sniff 70862** — przepuszczony przez kod po patchach: 8607 wierszy, 60 tabel, 0 złych (`out/forever_70862.sql`). Uzupełnia 70938 o `creature_text`, `npc_vendor`, `quest_offer_reward`, `quest_request_items`, `points_of_interest`, `chat_channels`.

---

## Kluczowe ścieżki

| Co | Gdzie |
|---|---|
| Projekt | `C:/Users/devov/Desktop/WPP-full` |
| Sniff główny | `C:/Users/devov/Desktop/regedit_70790/dumps/dump_1.60.1.70938_2026-11-14_12-41-07.pkt` |
| Sniff 2 | `C:/Users/devov/Desktop/regedit_70790/dumps/dump_1.60.1.70862_2026-11-07_12-13-01.pkt` |
| TDB world | `C:/Users/devov/Desktop/TDB_full_forever_2026_10_14/TDB_full_world_forever_2026_10_14.sql` |
| TDB hotfixes | `.../TDB_full_hotfixes_forever_2026_10_14.sql` |
| Źródło emulatora | `C:/Users/devov/Desktop/WoWSniff/forevercore` (branch `forever-bots`) |
| Dokument zadaniowy (EN) | `docs/WHAT-TO-DO-FULL-SQL.md` |
| Uwagi dev | `docs/TODO-70938.md`, `docs/BLEDY-70938.md` |

## Pułapki (żeby nie tracić czasu)

- Pliki `.cs` mają końcówki **CRLF** — skrypty node sprawdzają `s.includes("\r\n")`.
- Duże pliki TDB czytać `readFileSync` + `split("\n")`, **nie `readline`** (INSERT-y po setki kB w jednej linii). `readFileSync` całości wywala się na limicie 512 MB — czytać streamem.
- Shell użytkownika to **cmd.exe** — `head`/`grep` nie działają, podawać polecenia cmd.
- Plik wyjściowy parsera ląduje w `bin/Release/`, trzeba go przenieść do `out/`.
- `out/` jest w `.gitignore`.
