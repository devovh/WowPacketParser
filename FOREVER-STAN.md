# Forever 1.60.1 (70338–70938) — stan parsera WPP

Ostatnia aktualizacja: po weryfikacji z TDB (`TDB_full_forever_2026_10_14`) i naprawie
bloku wartości obiektów.

## Wyniki na sniffach

| Sniff | Pkty | OK | Błędy | Pominięte | Bez struktury |
|---|---|---|---|---|---|
| dump_1.60.1.70938_2026-11-13_14-02-23.pkt | 30 769 | **29 680 (96,46%)** | 315 (1,02%) | 289 | 485 |
| dump_1.60.1.70938_2026-11-13_13-18-57.pkt | 29 428 | **28 442 (96,65%)** | 212 (0,72%) | 201 | 573 |
| forevercore-70888.pkt | 26 875 | **26 019 (96,82%)** | 250 (0,93%) | 193 | 413 |
| dump_1.60.1.70938_2026-11-13_14-35-29.pkt | 4 807 | **4 459 (92,76%)** | 86 (1,79%) | 118 | 144 |
| dump_1.60.1.70862_2026-11-07_12-13-01.pkt | 312 210 | **290 759 (93,13%)** | 10 400 (3,33%) | 6 853 | 4 198 |

Punkt wyjścia przed pracą: 88,05% na sniffie 70862 (25 326 błędów `SMSG_UPDATE_OBJECT`)
i 88,1% na 70938.

## Co zostało ustalone i naprawione

### 1. Format bloku wartości: płaski, bez masek zmian

Forever **nie używa** masek zmian z 12.x. Blok wartości to te same pola co przy tworzeniu
obiektu, w tej samej kolejności, bez bitowego prefiksu:

```
[u32 licznik pol][u32 ogon][wartosci plasko]
```

Dla jednostki: `UnitFieldCount: 22`, `UnitFieldCountTail: 193`.

### 2. Ogon bloku (4 bajty) — rozstrzygnięte twardo, danymi z TDB

Weryfikacja `entry → DisplayID` z pakietów wobec TDB: **162/163 zgodnych (99,4%)**
(136 gameobjectów, 26 stworów, 1 niezgodny — pet 165189, bo TDB trzyma inny wariant modelu).
To dowód, że pola są czytane we właściwej kolejności.

Zostające 4 bajty na końcu bloku grywalnego obiektu to **prawdziwe pole**, nie śmieć:
dwa obiekty o tym samym `EntryID` i identycznych polach wartości mają tam różne wartości
(1766,30 / 44,48 = pozycja spawnu). Po dodaniu liczników `EnableDoodadSets`/`WorldEffects`
w dobrym miejscu zostaje dokładnie 4 bajty — pole jest w bloku, ale nie jest jeszcze nazwane.
W tabeli `gameobject_template` (TDB) nie ma odpowiednika, więc wymaga dalszego ustalenia.

### 3. Maska fragmentów encji — 768 → 172 błędów `EndOfStream`

Jednostki z niezmienionymi polami mają w bloku wartości pustą listę fragmentów
(0–3 bity) i **żadnej maski**. Odczyt maski zjadał bajty następnego obiektu, co psuło
całą resztę pakietu. W `WowPacketParserModule.V12_0_0_65390/Parsers/UpdateHandler.cs`,
`ReadUpdateEntityFragmentData`, dla buildów 70338–70938 maska nie jest czytana, gdy
lista fragmentów jest pusta. Efekt: 29 242 → 29 680 pakietów OK, 753 → 315 błędów.

### 4. Blok gracza

`AvatarMask` (u32) + `AvatarTail` (u32) + pola gracza + liczniki + sub-fragmenty.
Kolejność liczników: `[u32 liczniki][dane]`. Nazwa gracza i flagi są po wartościach,
bez bitowego prefiksu.

### 5. Blok gameobjectu w wartości

Płaski układ jak przy tworzeniu; liczniki `EnableDoodadSetsCount`/`WorldEffectsCount`
są przed swoimi listami, a flaga `HasAssistActionData` (bit) zamyka blok.

## Co zostaje

- **76 pakietów `ArgumentOutOfRangeException`** — jeden pakiet ze złym `Data size`
  (127 078 B) i 75 kolejnych z rzędu; wymaga osobnego podejścia.
- **172 pakiety `EndOfStream`** — blok wartości gracza (`IsOwned: true`) w dużych
  pakietach miejskich; układ bloku gracza nie jest jeszcze w pełni zgodny.
- **14 881 komunikatów „Updatefields not fully read”** — to nie są błędy pakietów,
  tylko ślady po 4-bajtowym ogonie opisanym w punkcie 2.
- Parsery poza `SMSG_UPDATE_OBJECT`: `SMSG_CHAT` (13/13 błędów), `SMSG_QUERY_PLAYER_NAMES_RESPONSE`
  (14/40), `SMSG_SPELL_CHANNEL_UPDATE` (11/11), `SMSG_QUERY_CREATURE_RESPONSE` (5/5).

## Narzędzia

- `tools/tdb_check.py` — porównuje `(entry, DisplayID)` z wyniku parsowania z dumpem SQL TDB
  (`python tools/tdb_check.py <parsed.txt> <TDB_full_world_*.sql>`). Uruchamiać po każdej
  zmianie układu bloku wartości: spadek zgodności poniżej ~99% oznacza przesunięcie pola.

## Jak uruchomić

```
dotnet build WowPacketParser.slnx -c Release -v q --nologo
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config <plik.pkt>
```


## SMSG_CHAT (0x4C0001) - ROZWIAZANE

Układ nagłówka z `tc-forever` (`ChatPackets.cpp`, `Chat::Write`):

```
u8  SlashCmd
u32 Language
5x  GUID128 (packed: bajt maski low, bajt maski high, potem niezerowe bajty)
u32 TargetVirtualAddress
u32 SenderVirtualAddress
i32 AchievementID
u32 ChatFlags
f32 DisplayTime
i32 SpellID
[40-bitowy blok bitowy]
[kanal][tresc]
[ogon]
```

Blok bitowy Forever ma **40 bitów**, nie 51 jak w TrinityCore — w tym buildzie
nie ma pól `targetName` (11 b) ani `prefix` (5 b):

| bity | pole |
|---|---|
| 0..10 | SenderNameLength (11 b) |
| 11..17 | **ChannelNameLength (7 b)** — dokładne |
| 18..29 | TextLength (12 b) |
| 30..33 | flagi czatu (4 b) |
| 34..39 | TargetNameLength (6 b) |

Ogon pakietu: `01 <tag> <u16> <u32> <u32> 04 8C 6C 68` — 8 bajtów dla kanałów
Trade, 7 dla General.

`TextLength` z bitów jest o 2–3 znaki za krótka dla kanałów General, więc treść
liczymy z reszty pakietu (do ogona). Nazwa kanału wychodzi dokładnie.

**Weryfikacja:** 13/13 pakietów snu 70938 (kanały `General - Orgrimmar`,
`General - Durotar`, `Trade - English 41`), plus sniffy 70888 i 13-18-57
z linkami do itemów i zaklęć (`|Hitem:...|h[...]|h`, `|Htrade:...|h[...]|h`).

Narzędzia analizy: `tools/chat_bits.py`, `tools/chat_layout.py`.

### Wyniki na sniffach

| Sniff | Pkty | OK | Błędy |
|---|---|---|---|
| 70938 (14-02-23) | 30 769 | **29 693 (96,50%)** | 302 |
| 70938 (13-18-57) | 29 428 | **28 445 (96,66%)** | 209 |
| 70938 (14-35-29) | 4 807 | **4 459 (92,76%)** | 86 |
| forevercore-70888 | 26 875 | **26 021 (96,82%)** | 248 |
| 70862 (312 210) | 312 210 | **290 824 (93,15%)** | 10 335 |

## Blok wartosci: fragmenty encji i blok gracza (sesja 2026-11-13)

Ustalone z hexow pakietow 881 (127 B) i 802 (127 078 B) sniffu 70938:

- Naglowek `SMSG_UPDATE_OBJECT`: `u16 MapID`, `u32 NumObjUpdates`, bity
  (`IsEndOfBatch`, `HasRemovedObjects`), `u32 Data size`, potem obiekty.
- Obiekt: `u8 UpdateType`, packed GUID128, `u32 UpdateFieldSize`, blok wartosci.
- Blok wartosci Forever: `u8` bitow (`IsOwned`, `HasFragmentUpdates`), a gdy
  `HasFragmentUpdates`:
  **`u16 EntityFragmentCount`** (maska 0x1FF) i tyle bajtow `u8 fragmentID`
  — **nie ma** `u8 ArchetypeSerializationType` ani terminatora `0xFF` z 12.x.
- Po prefiksie **pola ida plasko, bez maski fragmentow** (gałąź
  `ReadUpdateEntityFragmentDataForever` w UpdateHandler 12.x).
- 0xFF w liscie fragmentow jest zwyklym (nieznanym) id, nie terminatorem.

Efekt: `EndOfStream` w `SMSG_UPDATE_OBJECT` spadl ze **172 do 0** w sniffie
14-02-23 (97,02% pakietow OK, 143 z bledami zamiast 302).

### Co zostaje

- Blok gracza `IsOwned: true` — nadal `Updatefields not fully read`
  (9 z 100 bajtow przeczytanych); potrzebny uklad pol gracza 1.60.1.
- 76 pakietów `ArgumentOutOfRangeException` (jeden pakiet ze złym `Data size`).
- Fragmenty encji 1.60.1 (`HasFragmentUpdates`) — nieznany układ.
- 14 881 „Updatefields not fully read” — nierozpoznany ogon bloku wartości.
