# Forever 1.60.1 (70338–70938) — stan parsera WPP

Ostatnia aktualizacja: po weryfikacji z TDB (`TDB_full_forever_2026_10_14`) i naprawie
bloku wartości obiektów.

## Wyniki na sniffach

| Sniff | Pkty | OK | Błędy | Pominięte | Bez struktury |
|---|---|---|---|---|---|
| dump_1.60.1.70938_2026-11-13_13-18-57.pkt | 29 428 | **28 454 (96,690%)** | 200 (0,680%) | 201 | 573 |
| dump_1.60.1.70862_2026-11-07_12-13-01.pkt | 312 210 | **298 068 (95,470%)** | 3 091 (0,990%) | 6 853 | 4 198 |

Punkt wyjścia przed tą sesją: **96,598% / 227 błędów** (70938) i **95,074% /
4 330 błędów** (70862). Po sesji: **−27 błędów** na 70938 i **−1 239 błędów**
na 70862.

> Uwaga: starsze wiersze w tej tabeli (96,46%, 93,13%) pochodziły z innego
> etapu i innych plików wejściowych — aktualne są tylko dwa powyższe.

## Co zostało ustalone i naprawione

### 1. Format bloku wartości: maski zmian 12.x z remapem bitów

**Aktualne (poprawione):** blok wartości Forever **używa masek zmian** — dokładnie tak
jak 12.x, ale w numeracji bitów klienta klasycznego. Serwer (`tc-forever`,
`src/server/game/Entities/Object/Updates/UpdateFields.cpp`) pisze przez
`WriteClassicChangesMaskHeader<N>`: liczba bloków zależy od typu danych
(`UnitData` 8, `PlayerData` 11, `ActivePlayerData` 14), potem tylko niezerowe bloki
32-bitowe, potem bity pól.

Numery bitów różnią się od retailu, dlatego powstał `ForeverClassicBitRemap`
(`UnitData(bit)`, `PlayerData(bit)`, `ActivePlayerData(bit)`), który tłumaczy
numerację retail → Classic na podstawie `ClassicUnitDataBit70009`,
`ClassicPlayerDataBit`, `ClassicActivePlayerDataBit` z emulatora.

Dla `UnitData` jest jeszcze jeden bit po nagłówku maski (`IsBuild70291OrLater()`),
którzy klient 70862/70938 zawsze wysyła jako `false`.

Czytniki: `ForeverUnitDataReader.cs` (UnitData), `ForeverGameObjectDataReader.cs`
(GameObjectData), `ReadUpdatePlayerData` / `ReadUpdateActivePlayerData`
(już wcześniej w stylu masek).

**Wcześniejszy (błędny) opis** — zostawiony dla historii: „blok wartości to te same
pola co przy tworzeniu obiektu, bez masek zmian”. To nieprawda; dawało 88% i
`Updatefields not fully read` na 45 tys. pakietów.

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

## Blok wartosci: maski zmian (z kodu tc-forever)

Zrodlo: `tc-forever/src/server/game/Entities/Object/Updates/UpdateFields.cpp`
oraz `BaseEntity.cpp` / `Object.cpp` — tam jest dokladny zapis bloku wartosci.

- `ObjectData::WriteUpdate` — **4 bity maski**, `FlushBits()`, potem
  `EntryID` / `DynamicFlags` / `Scale`. Potwierdzone na pakiecie 881
  (maska 0x7 -> `EntryID 0xCDCC2621`, `DynamicFlags 0x17FF`, `Scale 1.0`).
- `PlayerData::WriteUpdate` — **11 blokow**: 11 bitow "ktore bloki sa zapisane",
  potem po 32 bity kazdy zapisany blok (`WriteClassicChangesMaskHeader`).
- `ActivePlayerData::WriteUpdate` — to samo, ale **14 blokow**.
- `UnitData::WriteUpdate` — **8 blokow** (dopisac, gdy bedzie potrzebne).
- `ClassicPlayerDataBit` / `ClassicUnitDataBit` / `ClassicActivePlayerDataBit`
  to **mapy numerow bitow retail -> Classic** — daja dokladna kolejnosc pol.
- Create (`BaseEntity::BuildCreateUpdateBlockForPlayer`): `u32 rozmiar`,
  `u8 FieldFlags`, `BuildEntityFragments` = id + 1 dla nie-tagow + `0xFF`,
  potem dla kazdego indirect `u8 1`, potem dane fragmentu.
- Values (`Object::BuildEntityFragmentsForValuesUpdateForPlayerWithMask`):
  `u8 FieldFlags`, `u8 0` (IdsChanged), `u8 contentsChangedMask`.

### Co zostaje

- Blok gracza `IsOwned: true` — parser czyta juz naglowek maski gracza
  (11 blokow) i wypisuje `PlayerChangedBits`; zostaje dopisanie pol wg
  kolejnosci z `ClassicPlayerDataBit` i `PlayerData::WriteUpdate`.
- `UnitData` — dopisac 8 blokow maski i kolejnosc pol (`ClassicUnitDataBit`).
- 76 pakietów `ArgumentOutOfRangeException` (jeden pakiet ze złym `Data size`).
- Fragmenty encji 1.60.1 (`HasFragmentUpdates`) — nieznany układ.
- 14 881 „Updatefields not fully read” — nierozpoznany ogon bloku wartości.

## Blok wartosci SMSG_UPDATE_OBJECT — stan po sesji (2026-11-13, wieczor)

### Co zostalo ustalone z tc-forever i potwierdzone na snifie 70938
1. **Fragmenty encji w Forever maja wlasna numeracje**: `BuildEntityFragments`
   (`src/server/game/Entities/Object/BaseEntity.cpp:196`) zapisuje `id + 1` dla kazdego
   nie-tagowego fragmentu (tagi 200+ bez zmian). Dlatego w pakiecie widzimy `33 26 CC CD`
   (retail 32/38/204/205). W `WowCSEntityFragments1215` doszly wartosci `*_Classic`
   (19, 21-25, 29, 32-36, 39) i `ToUniversal` tlumaczy je na wspolne.
2. **Licznik fragmentow**: `u16` = `0x0300 | (n - 1)` (typ serializacji 3 + liczba-1);
   `0xFF` konczy liste. W pakiecie 881: `00 03` → 4 fragmenty `33 26 CC CD FF`.
3. **Blok wartosci gracza/jednostki**: po liscie fragmentow idzie `u32 TypeMask`
   (bity `1 << TYPEID_*`: Object=0, Unit=5, Player=6, ActivePlayer=7, GameObject=8),
   a potem pola w kolejnosci `ObjectData`, `UnitData`, `PlayerData`, `ActivePlayerData`,
   `GameObjectData` — dokladnie jak `Player::BuildValuesUpdate` / `Creature::BuildValuesUpdate`.
   Wczesniej ten blok nie byl czytany wcale (tylko `CGObject`), co dawalo
   „Updatefields not fully read” na kazdym pakiecie z graczem.
4. **PlayerData::WriteUpdate** ma naglowek 11 blokow (`WriteClassicChangesMaskHeader<11>`),
   a pola ida w kolejnosci zapisu z emulatora (blok 32 trzyma m.in. TaxiMountAnimKitID,
   blok 36 nazwe, 47 DeclinedNames, 51-52 PartyType, 54-228 QuestLog, 230-248 VisibleItems,
   250-255 AvgItemLevel, 257-288 ForcedReactions, 290-305 VisibleEquipableSpells,
   307-325 PlunderstormItemDisplayID). Zaimplementowane w `ReadUpdatePlayerData`.

### Co jeszcze nie dziala
- `ReadUpdateObjectData` czyta 4-bitowa maske zmian, ale w pakiecie 881 na pozycji maski
  sa juz bajty UnitData — kolejnosc `ObjectData` w Forever wymaga jeszcze weryfikacji
  (emulator pisze `u32 maska typow` i od razu `ObjectData::WriteUpdate`; w pakiecie 881
  po masce widac `12 00 10 00 C1 00 00 48 ...`, co nie wyglada na 4 bity maski ObjectData).
- `ReadUpdateUnitData` (8 blokow) i `ReadUpdateActivePlayerData` (14 blokow) sa zaimplementowane
  dla 12.x i daja smieci dla Forever — trzeba je przepisac na `ClassicUnitDataBit` /
  `ClassicActivePlayerDataBit` z `UpdateFields.cpp`.
- Wynik sniffu 70938 bez zmian: 29 852 / 30 769 (97,02%) OK, 143 bledy.

---

## Sesja 2026-11-14: SMSG_CHAT (0x4C0001) i SMSG_TUTORIAL_FLAGS — zmierzone, poprawione

### SMSG_CHAT — poprzedni opis w tym dokumencie był BŁĘDNY

Wcześniejszy opis („13 pakietów, 40-bitowy blok bitowy, 5×GUID128, adresy wirtualne”)
nie przechodził weryfikacji na pełnym sniffie. Zmierzone od zera na **118 pakietach**
sniffu `dump_1.60.1.70862_2026-11-07_12-13-01.pkt`:

```
[0]   u8   typ wiadomości (ChatMessageTypeNew)
[1]   u8   język
[2]   u16  zero
[4]   4 × GUID128  (nadawca, gildia, konto, cel) — packed: maska low, maska high, niezerowe bajty
[36]  u32  ChatFlags
[40]  u8   chanBitsHi
[41]  u8   (chanBitsLo << 3) | flagi
[42]  u8   długość tekstu × 4   (dolne 8 bitów — przy > 63 znakach następuje przepełnienie)
[43]  u8   0x40
[44]  kanał + treść, UTF-8, sklejone bez separatora
[-]   ogon 8–10 B, kończy się na 04 8C 6C 68
```

Liczniki:

- **kanał**: `chanBits = pre[4] * 32 + (pre[5] >> 3)`, długość kanału w **znakach** = `chanBits / 8`.
  Walidacja 83/83 pakietów kanałowych (`General - The Barrens` = 21 znaków,
  `Trade - English 41` = 18, `General - Orgrimmar` = 19).
- **treść**: liczba **znaków** = `pre[6] / 4`. Bajt jest 8-bitowy, więc przy wiadomościach
  dłuższych niż 63 znaki trzeba dobrać `+ 64·k`; wybieramy to `k`, po którym zostaje
  prawidłowy ogon. Działa w 78/83; pozostałe 5 (najdłuższe, z UTF-8) bierzemy do początku
  ogona rozpoznanego od końca pakietu.
- **wpisy NPC i addonów** (`chanBits = 0`, np. `Barak Kodobane%s attempts to run away…`,
  `ATLASFRV_F_SLC#v:1030001`, `RXPGComms^1^T^S…`) nie mają nazwy kanału — treść idzie
  do końca pakietu (100% zgodności).

Efekt: `SMSG_CHAT` 118/118 bez błędu (było 13/13 błędów).

### SMSG_TUTORIAL_FLAGS — brak nagłówka z liczbą flag

Pakiet (0x47026A, Length **32**) to od razu 8 × `u32` flag, bez `count`.
Wcześniejszy parser czytał pierwsze `u32` jako licznik (`0x7BF691B3` = 2 079 756 723)
i wywalał się na `EndOfStream` (2 wystąpienia w sniffie 70862).
Po poprawce 8 flag czytanych bez błędu.

### Wyniki

| Sniff | Przed sesją | Po sesji |
|---|---|---|
| 70938 (13-18-57) | 28 119 (95,552%), 535 błędów | **28 427 (96,598%), 227 błędów** |
| 70862 (312 210 pkt) | 296 802 (95,065%), 4 357 błędów | **296 829 (95,074%), 4 330 błędów** |

Build: 0 błędów, 0 ostrzeżeń. Zmienione pliki:
`WowPacketParserModule.V1_60_1_70888/Parsers/ChatHandler.cs`,
`WowPacketParserModule.V1_60_1_70888/Parsers/WorldSessionHandler.cs`.

### Co nadal zostaje

- 5 najdłuższych wiadomości `SMSG_CHAT` (UTF-8, > 250 znaków) — licznik długości
  ma przepełnienie, którego jednostki nie udało się ustalić; treść odzyskana
  heurystycznie (do ogona), więc jest poprawna, ale nie wynika wprost z licznika.
- `SMSG_UPDATE_OBJECT`: kolejność pól pod maską w bloku wartości (zablokowane — patrz
  `RAPORT-FOREVER-UPDATE-OBJECT.md`).
- `HandleNameQueryResponse` (23×), `ReadPlayerGuidLookupData` (25×),
  `ReadRaceUnlockData` / `ReadClassUnlockData` / `HandleEnumCharactersResult` (po 2×)
  — to teraz główne źródła pozostałych błędów w sniffie 70938.
