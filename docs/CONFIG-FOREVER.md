# Config „wyciągnij wszystko z Forever” — co zmienione i co jeszcze blokuje

Data: 2026-11-14. Dotyczy `WowPacketParser/App.config`
(publikowany jako `WowPacketParser/bin/Release/WowPacketParser.dll.config`).

## Zmiany w configu

| klucz | było | jest | po co |
|---|---|---|---|
| `DumpFormat` | `1` (txt + sql) | `4` (tylko sql) | mniejszy plik, szybszy przebieg |
| `TargetedDatabase` | `2` (WotLK) | `20` (Classic) | lista kolumn bliżej Forever (i tak wychodzi retail — patrz §3) |
| `SQLOutput.*` | wszystkie `false` | **65 kluczy `true`** | `creature`, `gameobject`, `*_template`, `*_addon`, `npc_vendor`, `gossip_menu*`, `quest_*`, `page_text`, `points_of_interest`, `creature_text`, `spell_target_position`, `playerchoice*`, `locales_*` itd. |
| `hotfixes`, `hotfix_data`, `hotfix_blob`, `hotfix_optional_data`, `broadcast_text`, `broadcast_text_locale` | `false` | `true` | wszystko z `SMSG_DB_REPLY` |
| `ParseAllHotfixes` | `false` | `true` | nie trzeba klikać 258 pozycji ręcznie |
| `<Hotfix><FileHashes>` | 258 × `Enabled="false"` | 258 × `Enabled="true"` | gadatliwość hotfixów (które `DB2Hash` logować) |
| `SaveTempSpawns` | `false` | `true` | tymczasowe spawny bez `--` przed `INSERT` |
| **`KeepSQLFile`** (nowy) | — | `false` | patrz niżej |

`KeepSQLFile` to nowa opcja w kodzie (`Misc/Settings.cs`, `SQL/SQLFile.cs`):
domyślnie `false` = plik z `SQLFileName` jest **czyszczony** przed każdym
przebiegiem. Wcześniej drugi przebieg **dopisywał** się do pierwszego
(zmierzone: 3 605 367 B → 7 210 734 B, podwójne nagłówki). Z `KeepSQLFile=true`
dostajesz dawne zachowanie (kilka kompletnych plików w jednym) — świadomie.

## Wynik na sniffie 70938 (119 594 pakiety)

| | przed (config domyślny) | po |
|---|---|---|
| tabel w pliku | 4–33 | **55** |
| `INSERT`-ów | 38 | **506** |
| wierszy | ~1 700 | **16 599** (plus ~33 000 zakomentowanych punktów ścieżek) |
| rozmiar | 0,15–0,38 MB | **3,6 MB** |
| `DELETE`-ów (tylko przestrogi) | 34 | 54 |
| czas | 0,5 s | 0,5 s |

Nowe/(wcześniej brakujące) grupy: `gameobject`, `gameobject_template`,
`gossip_menu`, `gossip_menu_option`, `creature_queststarter`, `quest_details`,
`quest_poi`, `quest_poi_points`, `npc_spellclick_spells`, `creature_text`,
`creature_equip_template`, `creature_template_gossip`, `creature_model_info`,
`object_names`, `sniff_data`, `playercreateinfo`, `areatrigger_template`,
`waypoint_path`, `waypoint_path_node` + 33 tabele hotfixów/DB2.

## Czego config NIE odblokuje

`Builder.cs` dalej pomija 24 buildery z `CheckVersionMismatch`, bo
`GetExpectedTargetDatabasesForExpansion(ClientType.Forever)` wpada w
`default: return []`. W logu widać 36 linii
`Couldn't generate SQL output of X since the targeted database and the sniff
version don't match` — m.in. `CreatureTemplate`, `GameObjectTemplate`,
`ItemTemplate`, `QuestTemplate`, `NpcText`, `PageText`, `ScenarioPOI`.
To **nie config, a kod** — trzeba dodać `case ClientType.Forever`.

## Uwaga o `Delete`-ach

Nadal 54 razy `DELETE FROM … WHERE \`VerifiedBuild\` < 70938` (nagłówek, na sztywno)
oraz `DELETE FROM creature WHERE guid BETWEEN @CGUID+0 AND …`. Przed importem:
wypełnić `@CGUID`/`@OGUID`/`@ATSPAWNID`/`@ATPROPERTIESID`/`@MOVID` i przemyśleć
`SkipOnlyVerifiedBuildUpdateRows = true`.

## Uwaga o arności (błąd w generowaniu, nienaprawiony)

220 wierszy `creature_equip_template` ma **6 kolumn i 5 wartości**:
`(@MOVID + 0, 0, 0, NULL, '1860')` — to nie `creature_equip_template`, to wiersz
`waypoint_path` wklejony pod zły nagłówek. Źródło jest w
`SQL/Builders/Movement.cs`: przy każdym kolejnym moverze kod dodaje pustą
linię przez `AppendLine()`, a nie `;`, więc `INSERT … waypoint_path_node`
poprzedniej ścieżki zostaje niedomknięty i parser builderów „zlepia” kolejne
nagłówki. Po zgłoszeniu dodałem brakujące `output.Append(";")` zamykające
poprzednią ścieżkę (fix jest w drzewie), ale samo zjawisko zostało — 220
fałszywych wierszy `creature_equip_template` wciąż wychodzi. Do dalszej analizy.
