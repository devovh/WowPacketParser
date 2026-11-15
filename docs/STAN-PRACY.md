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

### 1. [ZROBIONE] faction, npcflag, level, unit_flags, scale → `creature_template` + `creature_template_difficulty`
Dev: w Ironforge widzieli Magister Falath, Turlethion Moonrage i Drake Ekans, ale z query dostali tylko nazwę i model — „so we couldn't make them”.

**Ustalone w kodzie (stan wyjściowy):**
- `CreatureTemplateNonWDBForever` (`WowPacketParser/Store/Objects/CreatureTemplate.cs:195`) już pisze `faction`, `npcflag`, `speed_walk`, `speed_run`, `BaseAttackTime`, `RangeAttackTime`, `unit_class`, `unit_flags`, `unit_flags2`, `unit_flags3`, `VehicleId` — **148 wierszy** w `out/forever_70938.sql` (drugi INSERT, linia 56132).
- Źródło: `WDBTemplates.cs:214` `CreatureTemplateNonWDBForever(Dictionary<WowGuid, Unit> units)`, czyta `npc.UnitData.FactionTemplate`, `NpcFlags[0..1]`, `Flags/Flags2/Flags3`, `Movement.RunSpeed/WalkSpeed`.
- **`scale` NIE jest zapisywany** — jest tylko `DisplayScale` w `creature_template_model` z query. Do dodania.
- **`level` NIE jest zapisywany** — `UnitData.Level` (`ForeverUnitDataReader.cs:153`) jest czytany, ale nie trafia do SQL.
- **Poziomu nie ma też w `creature_template`** (42 kolumny, sprawdzone w TDB). Prawdopodobnie idzie przez `ContentTuningID` w `creature_template_difficulty` — sniff ma **30581 wystąpień `ContentTuningID`**, wartości m.in. `0, 10, 11, 12, 1164800`. `UnitData.ContentTuningID` jest czytany (`ForeverUnitDataReader.cs:155`), ale `CreatureTemplateDifficultyForever` **nie ma tej kolumny** (ma ją tylko retailowy `CreatureTemplateDifficulty.cs:83`). **To pierwszy trop do sprawdzenia w tc-forever.**

**ZROBIONE (2026-11-15):** `scale` trafia do drugiego INSERT-a `creature_template`; `ContentTuningID` jest kolumną `creature_template_difficulty` (sniff 70938 ma je zawsze 0, więc wpisywane 0). **Poziom NIE idzie przez ContentTuningID** — tc-forever (`Creature::ApplyLevelScaling`) czyta `creature_classic_level (entry, level_min, level_max)`; parser pisze ją teraz z `UnitData.Level` (83 wpisy, bez petów/summonów/charmów). Usunięty też debugowy zapis do `Temp\q\built.txt`.

**Było do zrobienia:** dodać `scale` (z `UnitData.ScaleX` / display scale) i `ContentTuningID` (+ ewentualnie `LevelScalingDeltaMin/Max`) do rekordów Forever i do buildera.

### 2. `npc_vendor` — [ZROBIONE]
- W 70938: 0 wierszy, bo w tym sniffie nie ma ani jednego SMSG_VENDOR_INVENTORY (sprawdzone grepem po 0x47005C). To brak danych, nie błąd parsera.
- W 70862 jest 1 okno kupca (entry 5816, 8 pozycji), parsowane bez błędów. Z tego pakietu dostajemy slot, item, maxcount (-1 -> 0), ExtendedCost, type.
- Dodane do SQL (flaga Forever): `PlayerConditionID`, `IgnoreFiltering`. `incrtime` zostaje 0 w bazie, klient go nie wysyła. Więcej kupców wymaga nowego sniffa z otwartymi oknami u kupców.

### 3. `creature_loot_template` — [ZROBIONE]
- Parser `ForeverLootHandler` (SMSG_LOOT_RESPONSE) zbiera okna lootu stworów do `Storage.ForeverLootWindows`; builder `UnitMisc.CreatureLootTemplateForever` pisze tabelę.
- Pomija skinning (`AcquireReason 6`) i pickpocketing (2). `Chance` = drops/kills*100, gdzie kills = liczba okien lootu danego entry; komentarz wiersza: `<item> - drops/kills 1/4`.
- 70938: 5 wierszy (entry 3100 i 3244). To wszystko, co jest w sniffie (5 okien). AcquireReason 14 i 9 to wartości Forever spoza enuma LootType; żadnego okna skinningu w sniffach nie ma.
- Ograniczenie: item, który nie wypadł w żadnym oknie, nie istnieje w danych. Szansa jest tylko oszacowaniem z małej próby.

### 4. `gossip_menu`, `gossip_menu_option`, `creature_template_gossip` — [ZROBIONE w granicach sniffa]
- Sniff 70938 zawiera **jeden** SMSG_GOSSIP_MESSAGE (bankier 3318, menu 1623), więc po 1 wierszu to wszystkie dostępne dane, nie błąd parsera.
- `gossip_menu_option` ma teraz wszystkie kolumny schematu tc-forever (`GossipOptionID`, `OptionNpc`, `Language`, `Flags`, `GossipNpcOptionID`, `SpellID`, `OverrideIconID`).
- `OptionBroadcastTextID` = 0: dopasowanie idzie po tekście z bazy TDB (`SQLDatabase.BroadcastTexts`), a tekstu opcji nie ma w broadcast_text ze sniffa. Wymaga bazy TDB do uzupełnienia.

### 5. `waypoint_path` — [ZROBIONE]
- `waypoint_path` + `waypoint_path_node` pisze builder `Movement` (220 ścieżek w 70938).
- Nowe: builder zapamiętuje `@MOVID+n` każdego stwora (`Movement.PathIdByGuid`), a `Spawns` wpisuje to w `creature_addon.PathId` (kolumna `PathId` dla Forever) i ustawia `creature.MovementType = 2`. W 70938: 50 stworów z trasą. Wiersze tymczasowych spawnów zostają zakomentowane razem ze spawnem.

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

---

## Poprawki parsera (sesja po punktach 1-5) — stan: 70938 = 98,18% OK, 2 pakiety z błędem

Z 18 błędnych pakietów zostały **2**, oba znane i opisane niżej.

| Pakiet | Co było źle | Poprawka |
|---|---|---|
| `SMSG_WHO` | brak handlera | nowy `ForeverWhoHandler.cs`, układ z tc-forever `WhoPackets.cpp` |
| `CMSG_WHO` / `CMSG_RESURRECT_RESPONSE` | opcody zamienione (0x4400AF niesie zapytanie /who) | zamiana w `V1_60_1_69876/Opcodes.cs` + handler zapytania |
| `SMSG_ACCOUNT_DATA_TIMES` / `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | opcody zamienione (0x4701B7 = 170 B = czasy kont) | zamiana w `Opcodes.cs`, układ GUID + czas + 20 x i64 |
| `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | czytany układ retailowy | nowy `ForeverGlueScreenHandler.cs`, układ wyprowadzony z bajtów (829 B) |
| `SMSG_AUTH_RESPONSE` | bity czytane przed `Result` | `Result` (u32) idzie PRZED bitami (tc-forever `AuthResponse::Write`) |
| `SMSG_RATED_PVP_INFO` | brak układu | 9 bracketów x 74 B = 666 B (`ForeverPvpHandler.cs`) |
| `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | brak układu | nagrody z maską u8; ostatni bajt (0xF8) jeszcze bez nazwy |

### Zostało (2 pakiety)

- `SMSG_FEATURE_SYSTEM_STATUS` (949 B): tc-forever pisze `QuickJoinConfig` zaraz po `CommercePricePollTimeSeconds`, ale w sniffie po
  `RedeemForBalanceAmount` idą 4 bajty `00 00 00 00` przed `ClubsPresenceDelay` (60000 = `60 EA 00 00`), a Squelch ma inny rozmiar niż
  zakłada kod. Szkic handlera nie jest w repo; trzeba go dokończyć na bajtach.
- `SMSG_AUCTION_LIST_BUCKETS_RESULT` (14123 B): wyjątek końca strumienia, nie analizowany.

### Zasady, które się sprawdziły
- Gdy tc-forever nie ma układu, liczyć z długości: `9 x 74 = 666`, `127 + 2x9 + 55x12 + 6x4 = 829`.
- Zrzut surowych bajtów: tymczasowy handler z `packet.WriteLine("HEX: " + BitConverter.ToString(bytes))`, potem go usunąć.

---

## Uwagi developera po punktach 1-5 (loot, ścieżki)

- **Loot kluczowany po `LootID`, nie po entry stworzenia**: `node tools/fix-loot-id.cjs` (uruchamiać po `patch-from-tdb.cjs`) podmienia `Entry` na `creature_template_difficulty.LootID` (DifficultyID 0) z TDB world. Gdy TDB nie zna LootID, zostaje entry z komentarzem. Dwa stworzenia z tym samym LootID są łączone (wyższa szansa).
- **Bez `DELETE`**: builder pisze `INSERT IGNORE`, nie kasuje istniejących wierszy, więc dropy z questów zostają.
- **Zwłoki liczone raz**: ten sam GUID zwłok otwarty drugi raz nie zwiększa `kills` (chance nie spada).
- **Czyszczenie duplikatów spawnów gubi ścieżki (`creature_addon.PathId`)** dla NPC, które już są w bazie. Developer podlinkuje je ręcznie, po naszej stronie bez zmian.

---

## Sniff 70938: 0 błędów parsera (119 594 pakietów)

- `SMSG_FEATURE_SYSTEM_STATUS`: `ForeverFeatureStatusHandler.cs`, układ z tc-forever `SystemPackets.cpp:123` (bez `KioskSessionDurationMinutes`, SocialRestriction przed ContentSetID, 10 bitów ClassicFlagBits10).
- `SMSG_AUCTION_LIST_BUCKETS_RESULT`: `ForeverAuctionHandler.cs`. Klient 70938 dopisuje do koszyka 7. bit (`HasExtraBlock`), a po nim u32 (= ItemID) i blok 2/7/12 bajtów o nieznanej treści (tc-forever go nie zapisuje). Długość bloku dobierana heurystyką (nagłówek następnego koszyka musi być sensowny); sprawdzone na 500 koszykach, pakiet czyta się do końca. Treść bloku nadal do ustalenia.
- 70862 (starszy build) nie jest celem tego modułu i ma błędy parsera; patrz niżej.
