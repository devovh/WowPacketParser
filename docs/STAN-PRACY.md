# Stan pracy — WPP-full / Forever 1.60.1 (build 70938)

Ostatnia aktualizacja: 2026-11-15. Repo: `C:/Users/devov/Desktop/WPP-full`, branch `master`, remote `devovh`.
Ostatni commit: `834fe0846` — *forever: npc_text through broadcast_text ids, page_text and quest_reward_display_spell for 1.60.1*.

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

## Co zostało (kolejność)

1. **`SkipOnlyVerifiedBuildUpdateRows`** — dev: nagłówkowe `DELETE ... WHERE VerifiedBuild > 0` są domyślnie włączone. Do decyzji: `true` przy imporcie na istniejącą bazę.
2. **`page_text`** — kod gotowy, ale ten sniff nie ma ani jednego pakietu z tekstem strony. Trzeba nagrać sniff z czytaniem książek / tablic / znaków.
3. **Import do bazy** — brak MySQL użytkownika (świadomie pominięte). Po postawieniu: `mysql -u root -p world < out\forever_70938.sql`.
4. **Drugi sniff** `dump_1.60.1.70862_2026-11-07_12-13-01.pkt` — sprawdzony (8607 wierszy, 0 złych), nieużyty jako wynik finalny.
5. **Hotfixy bez odpowiednika w TDB** — 20 z 68 `TableHash` (`Spell`, `ItemDisplayInfo`, `CreatureSoundData`, `QuestObjective`, `GlobalStrings`, …); 14 tabel z odpowiednikiem w TDB nie dostało `INSERT` (m.in. `ModifiedCraftingItem` 154 rek., `TransmogrHoliday` 154, `Gameobjects` 75).

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
