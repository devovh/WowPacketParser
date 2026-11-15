# SQL ze sniffa (WPP-full, Forever 1.60.1 / build 70938)

Sprawdzone 2026-11-14 na `dump_1.60.1.70938_2026-11-14_12-41-07.pkt`
(119 594 pakiety, Orgrimmar). Zrzuty testowe: `C:\Users\devov\Desktop\sql-test\`.

## Odpowiedź krótko: tak, ale nie „bez problemów”

| | stan |
|---|---|
| składnia wygenerowanego SQL-a | OK — sprawdzone: nawiasy zrównoważone, 1 668 wierszy (1668 wierszy w cfg_20.sql), **0** wierszy z niezgodną liczbą wartości vs kolumny (`tools/sql_tables.cjs` + test arności) |
| nagłówkowe `DELETE FROM … WHERE \`VerifiedBuild\` < 70938` | **ryzykowne** — 34 sztuki doklejane zawsze, patrz §2a. Przed importem wyciąć albo `SkipOnlyVerifiedBuildUpdateRows = true` |
| `SET @CGUID := SET_VALUE_MANUALLY_HERE` | **trzeba wypełnić ręcznie**, inaczej spawny polecą na guid 0 (§4) |
| `creature_template`, `gameobject_template`, `item_template`, `quest_template` | **nie są generowane** — blokada `ClientType.Forever` w `Builder.cs` (§5). Kod buildera jest, ale nigdy nie zostaje wywołany |
| kolumny hotfixów (`broadcast_text` itd.) | wychodzą w schemacie retail/12.x niezależnie od `TargetedDatabase` (sprawdzone 2/9/20 → identyczne 18 kolumn) — trzeba porównać z tabelami bazy Forever |
| append do `SQLFileName` | ten sam plik przy drugim przebiegu **dopisał się** (103 072 → 206 144 B, 2 nagłówki, bez `DROP`/`CREATE`) — przed każdym przebiegiem usuń plik albo podaj nową nazwę |

Wniosek: SQL jest poprawny składniowo i logika jest sensowna, ale „wgrać i
zapomnieć” nie zadziała. Minimum przed importem: usunąć `DELETE`-y nagłówkowe,
ustawić `@CGUID`/`@OGUID`, sprawdzić, czy w bazie docelowej są wszystkie
kolumny z `INSERT`-ów, i zrobić to na **kopii** bazy.

Skrót: SQL nie powstaje z samych pakietów, tylko z **obiektów, które parser
zapamiętał w `Storage`** (spawny z `SMSG_UPDATE_OBJECT`, odpowiedzi
`SMSG_QUERY_*_RESPONSE`, `SMSG_DB_REPLY` = hotfixy, gossip, vendory…). Pod koniec
parsowania `Builder.DumpSQL()` (`WowPacketParser/SQL/Builder.cs`) odpala 83 metody
builderów; każda zwraca gotowe zapytania, całość trafia do jednego `.sql`.

---

## 1. Spawny + szablony z pakietów (droga „bez hotfixów”)

W `WowPacketParser/bin/Release/WowPacketParser.dll.config`:

```xml
<add key="DumpFormat"       value="1"/>  <!-- 1 = txt + sql, 4 = tylko sql (szybciej) -->
<add key="TargetedDatabase" value="20"/> <!-- 20 = Classic — patrz §5, dlaczego nie 2 -->
```

Tabele włącza się **osobno** (sekcja `SQLOutput` w configu, domyślnie `false`).
Nazwy kluczy = wartości `WowPacketParser.Enums.SQLOutput`
(`WowPacketParser/Enums/SQLOutput.cs`):

```xml
<add key="creature"                  value="true"/>
<add key="creature_template"         value="true"/>
<add key="creature_template_addon"   value="true"/>
<add key="creature_addon"            value="true"/>
<add key="creature_equip_template"   value="true"/>
<add key="creature_model_info"       value="true"/>
<add key="creature_movement"         value="true"/>
<add key="gameobject"                value="true"/>
<add key="gameobject_template"       value="true"/>
<add key="gameobject_addon"          value="true"/>
<add key="npc_vendor"                value="true"/>
<add key="npc_trainer"               value="true"/>
<add key="npc_spellclick_spells"     value="true"/>
<add key="gossip_menu_option"        value="true"/>
<add key="gossip_menu_addon"         value="true"/>
<add key="creature_queststarter"     value="true"/>  <!-- + creature_questender -->
<add key="gameobject_queststarter"   value="true"/>  <!-- + gameobject_questender -->
<add key="points_of_interest"        value="true"/>
<add key="creature_text"             value="true"/>
<add key="weather_updates"           value="true"/>
```

`DBEnabled` **nie jest potrzebne**, żeby dostać SQL — bez bazy WPP dostajesz
zwykłe `INSERT` (a nie `UPDATE` diff-ujące względem `world`).

## 2. Uruchomienie

Wszystko z linii komend, bez ruszania configu (`--klucz wartość`):

```bat
cd WowPacketParser\bin\Release

dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config ^
  --DumpFormat 4 ^
  --TargetedDatabase 20 ^
  --creature true --creature_template true --creature_template_addon true ^
  --gameobject true --gameobject_template true --npc_vendor true ^
  --SQLFileName "C:\out\orgrimmar_70938.sql" ^
  "C:\Users\devov\Desktop\regedit_70790\dumps\dump_1.60.1.70938_2026-11-14_12-41-07.pkt"
```

* `--SQLFileName` — jeden wspólny plik dla wszystkich sniffów.
  **Bez** tego parametru plik nazywa się `RRRR_MM_DD_GG_MM_SS_<sniff>.pkt.sql`
  w katalogu bieżącym.
* `--DumpFormat 4` = tylko SQL; `1` = `.txt` + `.sql` w jednym przebiegu.
* Kilka sniffów naraz → jeden plik, wiersze różnią się `VerifiedBuild`.

Efekt (nagłówek + pseudo-liczniki GUID):

```sql
# Detected build: V1_60_1_70938
# Detected locale: enUS
# Targeted database: Classic
SET @CGUID := SET_VALUE_MANUALLY_HERE;
SET @OGUID := SET_VALUE_MANUALLY_HERE;

DELETE FROM `creature` WHERE `guid` BETWEEN @CGUID+0 AND @CGUID+301;
INSERT INTO `creature` (`guid`, `id`, `map`, ... , `VerifiedBuild`) VALUES
(@CGUID+0, 3296, 1, 1637, ...),  -- 3296 (Area: 1637 - Difficulty: 0) CreateObject1 (Auras: )
-- (@CGUID+1, 1863, ...),          -- !!! might be temporary spawn !!!
```

## 2a. UWAGA: nagłówkowe `DELETE` (utrata danych!)

Po włączeniu hotfixów **każdy** przebieg (`--DumpFormat 4`) dorzuca na początek
`DELETE FROM …` — nie da się tego wyłączyć żadnym `--klucz`. Źródło to koniec
buildera `Hotfixes()` w `SQL/Builders/HotfixBuilder.cs` (linie ~18037–18120):

```csharp
else if (!Settings.SkipOnlyVerifiedBuildUpdateRows)
    sql += $"DELETE FROM `achievement` WHERE (`VerifiedBuild`>0 AND `VerifiedBuild`<{ClientVersion.BuildInt});" ...
    // ~240 takich linii
```

Dwa problemy realne przy wgrywaniu:

1. **Wycina `VerifiedBuild`:** baza Forever ma w tych tabelach `70938`, a
   warunek brzmi `< 70938` — więc `DELETE` *nic* nie usuwa (i świetnie).
   ALE jeśli w bazie wiersze mają `VerifiedBuild = 0`/`NULL` lub nowszy build,
   warunek je pominie — a `INSERT` z tym samym `ID` poleci
   `Duplicate entry`, jeśli w schemacie nie ma `ON DUPLICATE KEY`.
2. **Brak „diff”-a:** te `DELETE`-y lecą z listy tabel na sztywno, niezależnie
   od tego, co sniff przyniósł. Najbezpieczniej jest je **wyciąć** z pliku przed
   importem (`grep -v '^DELETE FROM'`) albo włączyć
   `SkipOnlyVerifiedBuildUpdateRows = true`, które tę sekcję pomija.

Sprawdzone dla 5 wartości `TargetedDatabase` (1, 2, 6, 9, 10, 20): liczba
`DELETE` = **34 w każdym**, więc to nagłówek na sztywno, nie efekt danych.

Wyjątek: `DELETE FROM \`creature\` WHERE \`guid\` BETWEEN @CGUID+0 AND …`
(spawny) pochodzi z buildera spawnów, nie z hotfixów — nadal trzeba ustawić
`@CGUID`/`@OGUID` (§4).

## 3. Hotfixy (DB2) — czyli to, co przyszło `SMSG_DB_REPLY`

Hotfixy to osobna gałąź: `HotfixBuilder` (`SQL/Builder/HotfixBuilder.cs`, tu
`SQL/Builders/HotfixBuilder.cs`, ~18 200 linii — po jednym builderze na każdą
wersję DB2). Klucze w configu:

```xml
<add key="hotfixes"             value="true"/>  <!-- najważniejsze: wszystko z DB2 -->
<add key="hotfix_data"          value="true"/>  <!-- tabela hotfix_data -->
<add key="hotfix_blob"          value="true"/>  <!-- tabela hotfix_blob -->
<add key="hotfix_optional_data" value="true"/>  <!-- tabela hotfix_optional_data -->
<add key="broadcast_text"       value="true"/>
<add key="broadcast_text_locale" value="true"/>
<add key="conversation_actors" ... />           
<!-- + dowolna inna tabela z listy SQLOutput, np. page_text, npc_text, item_template,
     quest_template, scenario_poi, spell_target_position, page_text_locale … -->
```

Uruchomienie razem ze spawnami:

```bat
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config ^
  --DumpFormat 4 --TargetedDatabase 20 ^
  --creature true --gameobject true --creature_template true --gameobject_template true ^
  --hotfixes true --hotfix_data true --hotfix_blob true ^
  --broadcast_text true --broadcast_text_locale true ^
  --SQLFileName "C:\out\orgrimmar_70938_full.sql" ^
  "C:\Users\devov\Desktop\regedit_70790\dumps\dump_1.60.1.70938_2026-11-14_12-41-07.pkt"
```

Co realnie wyszło z tego sniffa (test `test_classic2.sql`, 1 526 wierszy):

| grupa | tabele |
|---|---|
| hotfixy / DB2 (33 tabele) | `broadcast_text` (52 wiersze), `anim_kit`, `area_table`, `creature_display_info`, `creature_label`, `creature_model_data`, `criteria_tree`, `gameobject_display_info`, `item`, `item_search_name`, `item_sparse`, `mail_template`, `map`, `quest_v2`, `spell_aura_options`, `spell_categories`, `spell_category`, `spell_class_options`, `spell_cooldowns`, `spell_effect`, `spell_focus_object`, `spell_interrupts`, `spell_label`, `spell_levels`, `spell_misc`, `spell_name`, `spell_power`, `spell_target_restrictions`, `spell_visual`, `spell_visual_effect_name`, `spell_visual_kit`, `spell_x_spell_visual`, `taxi_nodes` |
| spawny (te same, co bez hotfixów) | `creature`, `gameobject`, `creature_template_addon` |

Pomocnik: `tools/sql_tables.cjs`

```
node tools/sql_tables.cjs C:\out\orgrimmar_70938_full.sql
```

wypisuje, które tabele plik zawiera i ile mają `INSERT`-ów (szybka kontrola, czy
hotfixy w ogóle weszły).

### Dwa tryby hotfixów — wybierz świadomie

* **`DBEnabled = false`** (domyślnie): `SQLDatabase.Get` zwraca `null`,
  więc `SQLUtil.Compare` robi **czysty `INSERT`** z tego, co przyszło w
  sniffie. Nic nie jest porównywane z serwerową bazą. To jest tryb do
  „wypluj mi SQL ze sniffa”.
* **`DBEnabled = true`** + baza `hotfixes`/`world`: dostajesz `DELETE` + `UPDATE`
  tylko dla różniących się pól (diff). W tym trybie `SkipRowsWithFallbackValues
  = true` dodatkowo wycina wiersze równe wartościom domyślnym. Wymaga
  `SQL\create_wpp.sql` wgranego do bazy `wpp` i działającego MySQL.

## 4. Co poprawić ręcznie przed importem

1. `SET @CGUID := SET_VALUE_MANUALLY_HERE;` → `SELECT MAX(guid)+1 FROM creature;`
   (analogicznie `@OGUID`, `@ATSPAWNID`, `@ATPROPERTIESID`).
2. Wiersze `-- (@CGUID+n, …)` są **zakomentowane**: to potencjalnie tymczasowe
   spawny (`!!! might be temporary spawn !!!`). Odkomentuj tylko stałe spawny,
   albo uruchom z `--SaveTempSpawns true`.
3. `NULL` w `creature` (`npcflag`, `unit_flags`, `unit_flags2`, `unit_flags3`,
   `dynamicflags`) — pakiety tych pól nie niosą, wypełnij `0`.
4. Wiersze hotfixów mają kolumny retailowe (`Text`, `Text1`, `LanguageID`,
   `VerifiedBuild`, `quest_v2`) — przed importem do bazy Forever porównaj
   nagłówek `INSERT`-a ze schematem docelowej tabeli. Zestaw `Classic` (20)
   daje schemat retail, `TheWarWithin` (9) identyczny; różni się tylko lista
   tabel (`broadcast_text`, `quest_*`, `item_template`, `npc_text`, `page_text`).

## 5. Pułapka: blokada wersji (`CheckVersionMismatch`)

`Builder.cs` ma ~83 metody builderów; 24 z nich dostają `CheckVersionMismatch =
true` przez `[BuilderMethod(true, …)]`. Wtedy wykonywany jest warunek:

```csharp
if (!GetExpectedTargetDatabasesForExpansion(ClientVersion.Expansion).Contains(Settings.TargetedDatabase))
    continue; // "Couldn't generate SQL output of X since the targeted database and the sniff version don't match"
```

Forever ma własne `ClientType.Forever = 11` (`Enums/ClientType.cs`,
`ClientVersion.GetExpansion` → `IsForeverClientVersionBuild`), ale
`GetExpectedTargetDatabasesForExpansion` **nie ma case'a `ClientType.Forever`**
i wpada w `default: return [];`. Efekt zmierzony (log + SQL):

* `creature_template`, `gameobject_template`, `item_template`,
  `quest_template` (`npc_text`, `page_text`, `scenario_poi` …) **nie są
  generowane**, silnik: `Couldn't generate SQL output of CreatureTemplate
  since the targeted database and the sniff version don't match` — dla
  każdej z 5 sprawdzonych wartości (`2`, `20`, …).
* To są dokładnie te "szablony, których brakowało" w poprzednim podejściu.
  Kod jest gotowy, ale kontrola wersji go blokuje.

Naturalna poprawka (`Builder.cs`):

```csharp
case ClientType.Forever:
    return [TargetedDatabase.Midnight, TargetedDatabase.TheWarWithin,
            TargetedDatabase.Classic, TargetedDatabase.CataClassic];
```

…albo czasowo `if (ClientVersion.Expansion == ClientType.Forever) return
Enum.GetValues<TargetedDatabase>().ToList();`, żeby zobaczyć, co te buildery
w ogóle wyplują, a potem przyciąć listę pod realny schemat Forever. Kod w forku
**nie jest zmieniony** — to plan, nie fakt.

### Co z tego wynika dla schematu

Wybór `TargetedDatabase` decyduje **tylko o liście kolumn**, bo
`DBFieldNameAttribute.IsVisible()` (`SQL/DBFieldNameAttribute.cs:162`):

```csharp
TargetedDatabaseFlag targetFlag = (TargetedDatabaseFlag)(1 << (int)Settings.TargetedDatabase);
return _validForVersion.HasFlag(targetFlag);
```

Hotfix builder (`Hotfixes()`) nie sprawdza wersji sniffu, więc hotfixy wychodzą
w każdym trybie. Sprawdzone: `broadcast_text` ma **identyczne 18 kolumn** dla
`TargetedDatabase` = 2, 9 i 20 (`Text`, `Text1`, `ID`, …, `SoundKitID1/2`,
`EmoteID1..3`, `EmoteDelay1..3`, `VerifiedBuild`). Czyli dla tego sniffa
`TargetedDatabase` **nie wpływa w praktyce** na to, co wychodzi.

## 6. Inne przydatne opcje

```xml
<add key="TargetedProject" value="0"/>      <!-- 0 TrinityCore, 1 Cmangos -->
<add key="SplitSQLFile"    value="true"/>   <!-- osobne *_world.sql / *_hotfixes.sql / *_wpp.sql -->
<add key="SQLOrderByKey"   value="true"/>   <!-- sortowanie wierszy po kluczu -->
<add key="SaveTempSpawns"  value="true"/>   <!-- zamiast komentowania temp. spawnów -->
<add key="ForceInsertQueries" value="true"/> <!-- ignoruj diff z bazy, zawsze INSERT -->
```

`SplitSQLFile = true` przydaje się, gdy chcesz wgrać do bazy tylko `world`,
a hotfixy puścić osobno (albo pominąć).

## 7. Cykl pracy

```
1. --DumpFormat 1 … txt, dopieszczanie parsera (docs/TODO-70938.md)
2. --DumpFormat 4 + tylko potrzebne tabele → *.sql
3. node tools/sql_tables.cjs *.sql  → czy hotfixy weszły
4. grep -c "might be temporary spawn" *.sql; przegląd wierszy z NULL
5. ręcznie: @CGUID/@OGUID, temp spawny, nagłówki tabel vs schemat Forever
6. import na KOPIĘ bazy, sprawdzenie w grze
```

---

## 8. Stan po zmianie configu (2026-11-14)

Config jest przestawiony na „wyciągnij wszystko” — szczegóły w
[`CONFIG-FOREVER.md`](CONFIG-FOREVER.md). Skrót:

* `DumpFormat 4`, `TargetedDatabase 20`, 65 × `SQLOutput… = true`,
  `hotfixes`/`hotfix_data`/`hotfix_blob`/`broadcast_text` = `true`,
  `ParseAllHotfixes = true`, `SaveTempSpawns = true`.
* Wynik na sniffie 70938: **55 tabel, 506 `INSERT`-ów, 16 599 wierszy, 3,6 MB**
  (wcześniej 38 `INSERT`-ów, 0,15 MB).
* `KeepSQLFile = false` — naprawia dopisywanie drugiego przebiegu do pliku
  `--SQLFileName` (zmierzone: było 3,6 MB → 7,2 MB).
* Nadal blokowane przez kod (nie config): `creature_template',
  `gameobject_template`, `item_template`, `quest_template`, `npc_text`,
  `page_text`, `scenario_poi` — `ClientType.Forever` nie ma case'a w
  `GetExpectedTargetDatabasesForExpansion`.
* Nadal do naprawy: 220 wierszy `creature_equip_template` z 5 wartościami na
  6 kolumn (efekt zlepiania w `SQL/Builders/Movement.cs`).
