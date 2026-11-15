# Co trzeba zrobić, żeby SQL z Forever działał w pełni

Stan na 2026-11-14 (build 70938). Ocena na podstawie kodu, logów przebiegu i
treści sniffa. **Nie jest to lista zmian już zrobionych** — dziś w drzewie są
tylko cztery zmiany: `App.config` (config „wszystko”), `Settings.KeepSQLFile`,
`SQLFile` (czyszczenie pliku) i `Movement.cs` (średnik). Reszta poniżej to plan.

Kolejność = kolejność robienia. Każdy punkt ma zakres (mały/średni/duży),
źródło danych i sposób sprawdzenia.

---

## 1. `ClientType.Forever` w kontroli wersji — MAŁY, zrób pierwszy

**Gdzie:** `WowPacketParser/SQL/Builder.cs`, metoda
`GetExpectedTargetDatabasesForExpansion`.

**Dziś:** brak `case ClientType.Forever` → `default: return []` → 24 buildery
z `CheckVersionMismatch` nigdy się nie wykonują (36 linii w logu:
`Couldn't generate SQL output of CreatureTemplate …`). Config tego nie obejdzie,
bo warunek jest w kodzie.

**Zrobić:**

```csharp
case ClientType.Forever:
    return [TargetedDatabase.Midnight, TargetedDatabase.TheWarWithin,
            TargetedDatabase.BattleForAzeroth, TargetedDatabase.Classic];
```

**Uwaga:** to tylko odblokowuje **wywołanie** builderów. Nie znaczy, że coś
wyjdzie — trzeba punktu 2, bo `Storage.CreatureTemplates` itd. jest puste.

**Sprawdzić:** w logu ma zniknąć `Couldn't generate SQL output of …`.

---

## 2. Właściwy problem: Forever nic nie zapisuje do `Storage` — DUŻY

**Objaw:** w snifie jest **132 pakiety `SMSG_QUERY_CREATURE_RESPONSE`**
(74 z `Result: 0`, czyli z danymi), **74 `SMSG_QUERY_GAME_OBJECT_RESPONSE`**
z pełnym zestawem pól, **11 `SMSG_QUERY_QUEST_INFO_RESPONSE`**, a w SQL-u po
„wyciągnij wszystko” jest **0 wierszy `creature_template`,
`gameobject_template`, `quest_template`, `npc_text`, `page_text`**.

**Przyczyna:** moduł Forever (`WowPacketParserModule.V1_60_1_70888`) czyta te
pakiety **tylko na poziomie tekstu** — `ForeverQueryCreatureHandler.cs` woła
`packet.ReadX(...)` i na końcu `AddSniffData`, ale nigdzie nie robi
`Storage.CreatureTemplates.Add(...)`. Ten sam wzorzec jest w handlery gameobjectów
i questów (dziedziczą po 12.x, gdzie szablony przychodzą hotfixami, nie query).

**Zrobić, tabela po tabeli** (najpierw creature, bo najwięcej danych):

1. `ForeverQueryCreatureHandler.ReadCreatureStats`: zamiast tylko czytać,
   zbudować `CreatureTemplate` (`Store/Objects/CreatureTemplate.cs` — rekord dla
   retail, pola `Name`, `SubName`, `TitleAlt`, `IconName`, `Type`, `Family`,
   `Rank`, `TypeFlags`, `MovementID`, `VerifiedBuild`) i
   `Storage.CreatureTemplates.Add(entry, template, packet.TimeSpan)`.
   Displayki → `Storage.CreatureTemplateModels` (`creature_template_model`).
2. To samo dla `SMSG_QUERY_GAME_OBJECT_RESPONSE` → `Storage.GameObjectTemplates`
   (`entry`, `type`, `displayId`, `name`, `IconName`, `castBarCaption`,
   `unk1`, 35 × `Data`, `VerifiedBuild`; z sniffa widać `DataSize: 185`,
   `Type: 5 (Generic)`, `Display ID: 309`, `[0..15] Data`).
3. `SMSG_QUERY_NPC_TEXT_RESPONSE` / `SMSG_QUERY_PAGE_TEXT_RESPONSE` →
   `Storage.NpcTexts` / `PageTexts` (te same warunki co wyżej, ale na razie w
   sniffie były tylko odpowiedzi questowe).

**Blokada do rozwiązania po drodze:** w `SMSG_QUERY_CREATURE_RESPONSE`
z danymi zostaje **37 nieznanych bajtów** między `Name/NameAlt` a
`CreatureDisplayID` (opisane w `FOREVER-STAN.md`, sesja 2026-11-14 cd. 10).
Bez tego układ pól po nich jest zgadywany — `HpMulti`, `EnergyMulti`, `Title`
wychodzą poprawnie, ale `TypeFlags` (3 × u32) dają `0x00080000, 0, 0x01000000`.
Trzeba drugiego źródła: tc-forever `QueryPackets.cpp` z gałęzi `Classic70291`
albo porównanie z DB2 `CreatureDisplayInfo`.

**Sprawdzić:** plik SQL ma `INSERT INTO creature_template` z ~74 wpisami;
`tools/sql_tables.cjs` pokazuje te tabele.

---

## 3. Schemat docelowy: Forever ≠ retail — ŚREDNI

Nawet gdy punkt 2 zadziała, kolumny pójdą retailowe (`TargetedDatabase` 20/9
daje ten sam zestaw). Przykłady z tego repo:

* `creature_template`: dziś `Classification`, `unit_class`, `WidgetSetID`,
  brak `rank`/`type_flags` w wielu wariantach; w Forever `type_flags` jest
  3-elementowe (widać w snifie: 3 × u32, nie 4),
* `gameobject_template`: `Data` × 35 kolumn (SinceShadowlands), `ContentTuningId`
  — w Forever `DataSize: 185` sugeruje inny rozmiar,
* `broadcast_text`: 18 kolumn z `SoundKitID1/2`, `EmoteDelay1..3`.

**Zrobić:** jedna z dwóch dróg:

* **A (tania):** dodać `TargetedDatabase.Forever`, przepisać atrybuty
  `DBFieldName(..., TargetedDatabaseFlag.Forever)` w rekordach `Store/Objects`
  pod realny schemat bazy Forever — dużo małych zmian, ale bez ryzyka regresji
  w retailu.
* **B (szybsza, brudna):** zostawić retail i po imporcie dopasować nazwy kolumn
  ręcznie. Nie polecam przy 55 tabelach.

**Sprawdzić:** `SHOW COLUMNS FROM creature_template` na bazie Forever vs nagłówek
`INSERT INTO creature_template (...)` ze sniffa — musi być identyczny zbiór.

---

## 4. `Movement.cs` — zlepianie INSERT-ów, ŚREDNI

**Objaw:** 220 wierszy pod `INSERT INTO creature_equip_template` z 6 kolumnami i
5 wartościami (`(@MOVID + 0, 0, 0, NULL, '1860')`) — to wiersze
`waypoint_path`, wklejone pod następny nagłówek.

**Zrobione:** dodałem `output.Append(";")` zamykający `INSERT waypoint_path_node`
poprzedniej ścieżki. Zjawisko **nie zniknęło** — trzeba znaleźć, co dokłada
kolejny `INSERT` bez `;` (prawdopodobnie `BaseProtoQueryBuilder` składa wyjścia
wielu packetów bez separatora; teraz dochodzi pusty `INSERT waypoint_path_node`).

**Zrobić:** w `Movement.GenerateQuery()` nie emitować nagłówka
`INSERT INTO waypoint_path_node` dla ścieżki bez ani jednego punktu; oraz
upewnić się, że `Builder.DumpSQL` składa `sql` z buildera z `;\n` między
packetami.

**Sprawdzić:** `tools/sql_tables.cjs` → `niezgodna arność: {}`.

---

## 5. `DELETE`-y w nagłówku — MAŁY, ale trzeba zdecydować

54 × `DELETE FROM x WHERE VerifiedBuild > 0 AND VerifiedBuild < 70938`
doklejane na sztywno z `HotfixBuilder.Hotfixes()` (linie ~18037–18120).
W bazie Forever z `VerifiedBuild = 70938` nic nie usuwają, ale przy `0`/`NULL`
mogą wyciąć dane albo wywalić `Duplicate entry`, jeśli nie ma `ON DUPLICATE KEY`.

**Zrobić:** `--SkipOnlyVerifiedBuildUpdateRows true` (config) **albo** przyciąć
listę w kodzie, żeby leciała tylko dla tabel, z których sniff rzeczywiście ma
wiersze.

---

## 6. Pseudo-liczniki — MAŁY

`@CGUID`, `@OGUID`, `@ATSPAWNID`, `@ATPROPERTIESID`, `@MOVID` są wypisywane jako
`SET_VALUE_MANUALLY_HERE`. Przed importem trzeba je ustawić (`MAX(guid)+1` itd.).
Automatyzacja: opcja `AutoGenerateGuids` w `SQLFile.WriteToFile`, liczona
z `Storage`, żeby `INSERT`-y dały się wgrać bez ręcznej edycji.

---

## 7. Hotfixy: gadatliwość i locale — MAŁY/ŚREDNI

`ParseAllHotfixes = true` + 258 × `Enabled="true"` w `<Hotfix>` sprawia, że log
jest pełen `HotfixStore`; plik SQL ma 3,6 MB przy 16 599 wierszach, z czego
`broadcast_text` to 52 wiersze. To działa, ale:
* przy każdym sniffie leci cała lista tabel — warto zawęzić `FileHashes` do
  tych, które w Forever w ogóle przychodzą (`DB2Hash` z `HotfixHandler` 12.x),
* locale (`*_locale`) wymagają `ClientLocale` ≠ `enUS`; Forever w snifie ma
  `enUS`, więc wiersze `_locale` nie powstają — jeśli potrzebne, trzeba
  `--ClientLocale` ustawić inaczej **i** mieć sniff z innym locale.

---

## 8. Test regresji dla każdej zmiany — MAŁY

Dla punktów 1–5: po każdej zmianie uruchomić
`dotnet build` + `dotnet test WowPacketParser.Tests` + przebieg na
`dump_1.60.1.70938` i zapisać `tabel / INSERT / wierszy` oraz wynik
`tools/sql_tables.cjs`. Bez tego łatwo „naprawić” jedną tabelę i zepsuć 20
przez przesunięcie kolumn.

---

## Podsumowanie wysiłku

| # | zadanie | zakres | efekt po zrobieniu |
|---|---|---|---|
| 1 | `case ClientType.Forever` | 10 min | znikają błędy „version mismatch”, buildery się wywołują |
| 2 | zapis `Storage` z query-handlerów Forever | 1–2 dni | **prawdziwe** `creature_template`, `gameobject_template`, `quest_*`, `npc_text` ze sniffa |
| 3 | schemat Forever vs retail | 1 dzień | SQL wgrywa się bez ręcznego mapowania kolumn |
| 4 | `Movement.cs` | 1–2 h | 0 fałszywych wierszy `creature_equip_template` |
| 5 | `DELETE`-y | 15 min | brak ryzyka wycięcia danych |
| 6 | pseudo-liczniki | 2 h | import bez edycji pliku |
| 7 | hotfixy/locale | 2–3 h | mniejszy plik, sensowniejszy log |

Punkty 1, 4, 5 można zrobić od razu — nie zmieniają układu pól. Punkt 2 to
właściwa praca i wymaga rozwiązania 37 nieznanych bajtów w
`SMSG_QUERY_CREATURE_RESPONSE`. Punkt 3 ma sens dopiero po 2.
