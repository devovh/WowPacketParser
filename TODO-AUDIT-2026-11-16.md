# TODO-FULL-PARSER.md — audyt (2026-11-16)

Sprawdzone po commicie `159275037`, na sniffie `dump_1.60.1.70938_2026-11-14_12-41-07.pkt`
(119 594 pakiety) i na kodzie w `WowPacketParserModule.V1_60_1_70888/Parsers`.
Wynik: **`TODO-FULL-PARSER.md` jest nieaktualny w ponad połowie punktów** — część rzeczy,
które opisuje jako „do zrobienia”, jest już napisana (nie zawsze w module Forever — sporo
idzie przez fallback).

## 1. Co jest już w kodzie, a TODO twierdzi, że nie

| Punkt z TODO | Stan faktyczny | Dowód |
|---|---|---|
| 2.1 `CMSG_USE_ITEM` — „Needs a Forever SpellCastRequest reader” | **Działa.** Czytnik to `V3_4_0_45166/Parsers/ItemHandler.cs:499` → `SpellHandler.ReadSpellCastRequest`, wywoływany przez fallback modułów. W module Forever nie ma własnego `CMSG_USE_ITEM` i nie potrzebuje go. | 1/1 pakiet w sniffie (80 B, Number 113199) czyta się do końca: wszystkie pola z układu dewelopera, w tym Target, MissileTrajectory, CraftingNPC, `OptionalCurrenciesCount`, `CraftingFlags`, `HasCrafingOrderID` |
| 2.1 `SMSG_RECENT_ALLY_LIST` (0x470366, 396 B) | **Napisany**, nie „do zrobienia”. | `ForeverDeveloperLayouts3Handler.cs:13`; w sniffie 1 pakiet, 8 sojuszników, czyta się do końca |
| 2.1 `SMSG_RECENT_ALLY_DATA_RESPONSE` | **Napisany** (`159275037`). | `ForeverDeveloperLayouts3Handler.cs:44`; 1 pakiet, `Valid=7`, `Count=0`, `Unk2=0` |
| 2.1 `SMSG_GUILD_BANK_QUERY_RESULTS` (0x530025) | **Napisany** z pełnym układem (tablice + itemy). | `ForeverDeveloperLayouts4Handler.cs:35`; w sniffie 2 pakiety, oba puste (`TabCount=0`, `ItemCount=0`, `FullUpdate=True`) |
| 2.1 `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT` | **Napisany**; w sniffie tylko pusta lista (`ItemCount=0`, `DesiredDelay=300`, `HasMoreResults=False`). | `ForeverDeveloperLayoutsHandler.cs` |
| 2.1 `SMSG_AUCTION_FAVORITE_LIST` | **Napisany**; w sniffie tylko `Count=0`. | `ForeverDeveloperLayouts4Handler.cs:77` |
| 2.1 `SMSG_LOAD_EQUIPMENT_SET` | **Napisany**; w sniffie tylko `Count=0`. | `ForeverDeveloperLayouts4Handler.cs:93` |
| 2.2 cztery bezimienne (0x470377, 0x470228, 0x4C0005, 0x470059) | **Zamknięte** — deweloper też nic nie ma. `SMSG_BATTLEFIELD_LIST` zostaje na 0x4C0005. | `DEVELOPER-REQUEST.md` 2.1, commit `159275037` |
| 2.3 pięć nazwanych bez układu | **Zamknięte** — serwer ich nie wysyła, zostają jako hex. | `DEVELOPER-REQUEST.md` 2.2 |
| 3. 70862 | **Zamknięte jako cel** — serwer obsługuje tylko 70938. | `159275037` |

## 2. Co realnie zostało (i dlaczego TODO tego nie rozróżnia)

Wszystkie cztery pakiety poniżej **czytają się bez błędu**, ale w jedynym pakiecie ze sniffu
mają puste sekcje (`Count=0`, pusta tablica banku). Układ jest znany (ocena A/A- od twórcy
serwera), więc **nie da się go „ustalić” ze sniffu** — po prostu nie ma danych do sprawdzenia:

- `SMSG_GUILD_BANK_QUERY_RESULTS`: tablice i itemy,
- `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT`: `AuctionItem` (puste przy `ItemCount=0`),
- `SMSG_AUCTION_FAVORITE_LIST`: wpisy przy `Count>0`,
- `SMSG_LOAD_EQUIPMENT_SET`: zestawy przy `Count>0`.

**Żeby to domknąć, potrzebny jest sniff z danymi**: otwarty bank gildii z itemami w tablicy,
licytacje na AH (coś zalicytowanego), ulubione aukcje dodane do listy, zapisane zestawy
wyposażenia. To jest jedna sesja w grze — bez niej każdy czytnik zostanie „napisanym na
układzie z dokumentacji”, nie „potwierdzonym na bajtach”.

## 3. Reszta, niezależna od dewelopera

- Małe układy z oceną G (1–2 próbki): `SMSG_MOVE_INITIAL_OBJECT_UPDATE_COMPLETE`,
  `SMSG_REGIONWIDE_CHARACTER_RESTRICTIONS_DATA`/`..._MAIL_DATA`, `SMSG_LAST_CATALOG_FETCH_RESPONSE`,
  `SMSG_BATCH_PRESENCE_SUBSCRIPTION`, `CMSG_SERVER_VALIDATION_SIGNATURE_REQUEST`,
  `CMSG_GET_ACCOUNT_CHARACTER_LIST`, `CMSG_CLUB_FINDER_REQUEST_SUBSCRIBED_CLUB_POSTING_IDS`,
  `CMSG_MOVE_SET_TURN_RATE_CHEAT`, `CMSG_PERKS_PROGRAM_REQUEST_REFUND`. Nie zasilają żadnej tabeli SQL.
- `SMSG_REQUEST_PVP_REWARDS_RESPONSE`: ostatni bajt (0xF8) bez nazwy.
- `SMSG_RATED_PVP_INFO`: rozmiar 666 B pewny, kolejność pól z retailu — niesprawdzona.
- Nazwy nieudowodnione (ocena B): `0x440134`, `0x440136`, `0x4400E6`.
- Dane, których sniff nie ma (patrz `SNIFFING-CHECKLIST.md`): `npc_vendor.incrtime` = 0,
  `creature_loot_template` jako szacunek, `creature_classic_level_spawn` niezaimplementowane.

## 4. Wniosek

Nie ma tu nic, co da się „wyguglać” ani wyprowadzić z posiadanych pakietów: albo kod już jest,
albo brakuje próbki z gry. Najtańszy kolejny krok to **jeden nowy sniff** według
`SNIFFING-CHECKLIST.md` (bank gildii z itemami, licytacje AH, zestawy, vendory z restockiem,
50+ zabójstw) — wtedy wszystkie punkty z sekcji 2 i 3 zamykają się jednym przebiegiem.
