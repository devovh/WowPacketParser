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

---

## Sesja 2026-11-14 (cd.): GameObjectData WriteCreate — kolejność pól z tc-forever

### Co było źle

`ReadCreateGameObjectData` w `UpdateFieldsHandler.cs` (V1_60_1_70888) czytał pola w
kolejności retail 12.x, a Forever zapisuje je inaczej. Efekt: **166 pakietów** miało
`Updatefields not fully read ... Bytes remaining: 4` (gameobjecty: totemy, beczki,
stacje zadań) — parser zjadał 4 bajty za mało albo za dużo i gubił granicę obiektu.

### Co mówi `tc-forever` (`GameObjectData::WriteCreate`, UpdateFields.cpp:7515)

Klient Classic ma **jedno pole więcej** niż retail: `int32 Level` na `+0x74`, tuż po
`FactionTemplate` i **przed** `State`. Retail trzymał `Level` dopiero po `CustomParam`.
Serwer zapisuje je w obu miejscach (ta sama wartość) — dlatego liczba bajtów się
zgadzała, ale wszystko za `FactionTemplate` było przesunięte o 4.

Dodatkowo kolejność liczników list jest inna niż w 12.x:

```
FactionTemplate
Level            <- pole Classic, nie ma go w retailu na tej pozycji
State, TypeID, PercentHealth, ArtKit, CustomParam
EnableDoodadSetsCount
Level            <- powtórka (ta sama wartość)
AnimGroupInstance, UiWidgetItemID/Quality/Count
WorldEffectsCount
EnableDoodadSets[]
WorldEffects[]
[BIT] HasAssistActionData   <- bit reader, nie bajt
```

### Poprawka

`WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs`,
`ReadCreateGameObjectData`: `Level` czytany wcześnie (`LevelEarly`) i późno (`Level`),
licznik `EnableDoodadSets` przed `Level`, `WorldEffects` po `UiWidgetItemCount`,
flaga `HasAssistActionData` przez `ResetBitReader()`. Gdy oba `Level` się różnią,
parser wypisuje `GameObjectData: LevelEarly (...) != Level (...)` — to sygnał
przesunięcia pól, nie błąd pakietu.

### Wynik (sniff `dump_1.60.1.70888_2026-11-12_18-28-24.pkt`, 26 875 pkt)

| Metryka | Przed | Po |
|---|---|---|
| Pakiety OK | 26 081 (97,046%) | 26 081 (97,046%) |
| Błędy | 188 | 188 |
| Bez struktury | 413 | 413 |
| `Updatefields not fully read` | 2 379 | **2 213 (−166)** |

Liczba pakietów OK się nie zmieniła, bo te 166 przypadków nie kończyło się wyjątkiem —
kończyło się tylko komunikatem. Ale **zniknął cały worek 4-bajtowego ogona** w bloku
tworzenia gameobjectu, czyli dane tych obiektów są teraz czytane do końca.

### Co zostaje w „Updatefields not fully read” (2 213)

Rozbite po `IsOwned` / `HasFragmentUpdates`:

| Wariant | Ile | Co to jest |
|---|---|---|
| `IsOwned: false, HasFragmentUpdates: false` | ~2 200 | **pusta aktualizacja gracza** — blok ma 2 bajty flag i nic więcej, ale `Data size` w pakiecie jest większy (21, 17, 46, 49, 128 B). Reszta bloku to prawdopodobnie dane, których parser nie zna (nie ma ich w `BaseEntity::BuildValuesUpdateBlockForPlayer` dla `ContentsChangedMask == 0`). |
| `IsOwned: true, HasFragmentUpdates: true, TypeMask: 0xFF...` | ~94 wystąpienia | przedmioty w ekwipunku (`Item/0`), `TypeMask 4291298048 = 0xFFCE0FC0` — bity 6-9, 13-16, 18, 19, 21-23, 26-31. To nie jest maska typów `1<<TYPEID_*` z emulatora; potrzebne osobne ustalenie. |

### Pliki

- `WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs` (zmieniony)
- punkt odniesienia bez zmian: **97,046% / 188 błędów** na sniffie 70888

### Sesja 2026-11-14 (cd.) — maska fragmentów w bloku wartości: **hipoteza obalona**

Próbowałem zamienić odczyt `u32 TypeMask` na układ z `tc-forever`
(`Object::BuildEntityFragmentsForValuesUpdateForPlayerWithMask`, Object.cpp:114):
`[u8 FieldFlags][u8 IdsChanged][u8 ContentsChangedMask]` + bit listy fragmentów.
Zmiana zbudowana i przetestowana, potem **cofnięta** — nie daje ani jednego pakietu więcej
(97,046% / 188 błędów przed i po).

Co pokazały pomiary na sniffie 70888:

| Wartość w miejscu, gdzie kod czyta `u32` | Ile razy | Interpretacja |
|---|---|---|
| `0xFFC80300` | 76 | bajty `00 03 C8 FF` |
| `0xFFCC0300` | 7 | bajty `00 03 CC FF` |
| `0xC9C80300` | 7 | bajty `00 03 C9 FF` |
| `0x26210300` | 4 | bajty `00 03 21 26` |

Czyli w tym miejscu **zawsze** stoi `00 03`, a potem bajt maski i bajt `FF`.
`00 03` to dokładnie ten sam nagłówek listy fragmentów, który `DROGA-DO-100.md` opisuje
dla pakietu 881 (`u16 0x0300 | (n-1)`). Wniosek: w bloku wartości Forever **jest lista
fragmentów**, a nie maska typów — ale parser już ją czyta (przez `ReadEntityFragments`
dla `ArchetypeSerializationType`) i to nie jest źródłem pozostałych błędów.

**Co to znaczy dla worka D:** `SMSG_UPDATE_OBJECT` na sniffie 70888 daje 97,046%
i **nie poprawi się przez zmianę układu maski**. Pozostałe 2 213 komunikatów
„Updatefields not fully read” to puste aktualizacje (`IsOwned: false`,
`HasFragmentUpdates: false`, blok 2-bajtowy przy `Data size` 8–128 B) — dane, których
`BaseEntity::BuildValuesUpdateBlockForPlayer` w tej ścieżce nie zapisuje. Żeby je ruszyć,
potrzebny jest zrzut z klienta albo pełny kod ścieżki `Values` z emulatora, nie kolejna
hipoteza o masce.

Plik `WowPacketParserModule.V12_0_0_65390/Parsers/UpdateHandler.cs` przywrócony do stanu
z commita (`git checkout`) — jedyna zostająca zmiana w repo to `GameObjectData WriteCreate`.

### Sesja 2026-11-14 (cd. 2) — `SMSG_QUERY_PLAYER_NAMES_RESPONSE` (worek A)

Handler Forever przeniesiony do własnego modułu (`WowPacketParserModule.V1_60_1_70888/Parsers/QueryHandler.cs`)
z układem z `tc-forever` (`Server/Packets/QueryPackets.cpp:200-280`). Fallback 12.x→11.x→…→4.4
łapał wersję 4.4, która czyta `HasNameCacheUnused920` zamiast `HasGuildGuidLookupData` /
`HasHouseGuidLookupData` — dlatego wynik `[1]` i dalej był śmieciem.

Trzy różnice, wszystkie potwierdzone na snifie:

1. **`PlayerGuidLookupData`**: po 6-bitowej długości imienia idą jeszcze **dwie 9-bitowe długości**
   (nazwisko i trzecie pole, zawsze puste) — `CharacterHandler` 8.x ich nie czytał.
2. **`GuildGuidLookupData`**: kolejność to `[VirtualRealmAddress u32][Guid][bity 7][FlushBits][tekst]`,
   a nie `[Guid][bity][tekst]`.
3. **`HouseLookupData`**: `[Guid][bity 8][FlushBits][tekst]`.

**Dowód** — pakiet `Number: 1746` (229 B, 3 wyniki), surowe bajty:

```
03 00 00 00              Count = 3
00 07 E0 24 1E 68 64 6C  [0] Result=0, PackedGuid 0x086C64…681E24, bity: Player=1 Guild=0 House=0
08 80 0E 08 00 00 00 ... [0] PlayerGuidLookupData: IsDeleted=0, NameLen=9, SurnameLen=7, Third=0
                         → "Baldwin" + "Formosus" (dokładnie 16 bajtów tekstu)
00 07 E0 BE 5C 3D E4 6A  [1] Result=0, PackedGuid 0x086A…3D5CBE, bity: Player=1 …
                         → "Mxxl" + "Bongbringer"
00 07 E0 C2 2C AC 8C 6C  [2] Result=0, PackedGuid 0x086C…AC2CC2, bity: Player=1 …
                         → "Fistor" + "Morraine"
```

Nazwiska (`Formosus`, `Bongbringer`, `Morraine`) wychodzą teraz poprawnie, wynik `[0]` czyta się
w całości. **Procent pakietów się nie zmienił** (97,046%), bo `SMSG_QUERY_PLAYER_NAMES_RESPONSE`
i tak był liczony jako OK — poprawiła się treść, nie status.

Przy okazji sprawdzone: `SMSG_QUERY_QUEST_INFO_RESPONSE` (5 wystąpień na snifie) czyta się
w całości, `SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE` w tym snifie **nie występuje** (0 razy) — nie ma
czego mierzyć, worek odpada.

### Sesja 2026-11-14 (cd. 3) — `SMSG_CAST_FAILED` (worek A)

Handler Forever w `WowPacketParserModule.V1_60_1_70888/Parsers/SpellHandler.cs`.
Układ z `tc-forever` (`Server/Packets/SpellPackets.cpp:556 CastFailed::Write`):

```
[CastID PackedGuid][i32 SpellID][Visual 2x i32][i32 Reason]
[i32 FailedArg1][i32 FailedArg2][FailedBy PackedGuid]
```

Handler 9.x czyta `FailedBy` tylko dla `ClientBranch.Retail >= 12.0.0`, a Forever leci przez
fallback z gałęzią Classic — ostatnie pole zostawało nieprzeczytane. Na snifie to zawsze
**2 bajty**: `00 00`, czyli pusty PackedGuid.

```
... 00 20 00 00 00 FF FF FF FF FF FF FF 00 00
                  ^Reason  ^Arg1    ^Arg2    ^FailedBy = 0x0
```

**Wynik: 5/5 pakietów `SMSG_CAST_FAILED` czyta się w całości.**
`26081 → 26084 OK (97,046% → 97,057%)`, błędów `188 → 185`.

### `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` — nie da się naprawić po stronie parsera

Trzy wystąpienia, `Length: 829`, a serwer wysyła **98 bajtów** (`tc-forever`,
`Server/Packets/SystemPackets.cpp:247`): 6 bajtów flag + `u32 CommercePricePollTimeSeconds`,
`i64 RedeemForBalanceAmount`, `i32 MaxCharactersOnThisRealm`, `u32 LiveRegionCharacterCopySourceRegions`,
`i32 ActiveBoostType`, `i32 TrialBoostType`, `i32 Minimum/MaximumExpansionLevel`, `i32 ContentSetID`,
`u32 DisabledGameModes`, `u32 GameRules`, `u32 AvailableGameModeIDs`, `i32 ActiveTimerunningSeasonID`,
`i32 RemainingTimerunningSeasonSeconds`, `i32 TimerunningConversionMin/Max`, `i16 MaxPlayerGuidLookupsPerRequest`,
`i16 NameLookupTelemetryInterval`, `u32 NotFoundCacheTimeSeconds`, `u32 DebugTimeEvents`,
`i32 MostRecentTimeEventID`, `u32 EventRealmQueues`.

Czytanie „wg emulatora” daje 94 bajty i zostawia **735** — czyli sniff niesie tu dużo więcej,
niż emulator w tym miejscu zapisuje (prawdopodobnie inna ścieżka/config retail, którego
`tc-forever` nie odtwarza). Bez drugiego źródła (zrzut z prawdziwego serwera) nie ma czego dopasowywać —
zostawione w spoczynku, tak jak worek `TypeMask`.

### Sesja 2026-11-14 (cd. 4) — drugi sniff (70862, 312 210 pakietów) i diagnoza 43 tys. pustych bloków

Większy sniff: `C:/Users/devov/Desktop/regedit_70790/dumps/dump_1.60.1.70862_2026-11-07_12-13-01.pkt`
(41 MB, **312 210** pakietów). Baseline po wszystkich dotychczasowych poprawkach:

| | |
|---|---|
| OK | 298 171 (**95,503%**) |
| błędy | 2 988 (0,957%) |
| pominięte | 6 853 (2,195%) |
| bez struktury | 4 198 (1,345%) |
| „Updatefields not fully read” | **43 223** |
| „Packet not fully read” (poza updatefields) | **2 799** |

Rozkład tych 43 223 pustych bloków (po liczbie nieprzeczytanych bajtów):

| Bytes remaining | ile | co to |
|---|---|---|
| 14 | 8 637 | `Player`, `FieldFlags: 0`, `ObjectChangesMask: 1` |
| 18 | 6 665 | to samo, inna długość |
| 19 | 5 425 | |
| 26 / 22 | 2 698 / 2 659 | |
| 125 | 1 529 | `GameObject`, `ObjectChangesMask: 0` |
| 5 / 23 / 11 / 15 / 28 | po ~1 000 | |

**Co udało się ustalić ponad wszelką wątpliwość:**

1. Pole na początku bloku to **`uint8 UpdateFieldFlag`** (`BaseEntity.cpp:159`, `buf << uint8(fieldFlags)`),
   a nie bit `IsOwned`. `IsOwned` to bit 0 tego bajtu. Testowane — daje identyczny wynik, bo dla
   obcych obiektów ten bajt jest zerem, więc nie zmienia to ani jednego pakietu.
2. `HasFragmentUpdates` czytane jako bool **zgadza się** z `IdsChanged` (uint8). To nie jest błąd —
   dla 8 625 bloków `Player` parser zatrzymuje się na pozycji 2, bo blok ma 30 bajtów, a 28 bajtów
   danych gracza nie ma struktury w `ReadUpdateEntityFragmentDataForever`.
3. Większość resztek to **`ObjectChangesMask: 1`** z zerem po nim, czyli `ReadUpdateObjectData`
   czyta maskę i nic więcej (puste `ObjectData`). Dla `GameObject` z `ObjectChangesMask: 0`
   zostaje 125 bajtów.

**Blokada:** ścieżka `Values` dla Forever nie ma w `WowPacketParser` struktury pól dla gracza
i gameobjectu w tej wersji bloku. Bez pełnej listy pól z klienta (albo zrzutu, na którym widać
niezerowe `ObjectChangesMask`) dalsze czytanie to zgadywanie, które rozjeżdża kolejne obiekty
w tym samym pakiecie — dlatego zmiany w `UpdateHandler.cs` z tej próby zostały cofnięte.

**Wniosek dla planu:** metryka „OK %” na tym snifie stoi na 95,5% i nie ruszy się od samych
handlerów pojedynczych opcodów (te są już naprawione: `CAST_FAILED`, `HEALTH_UPDATE`,
`ACCOUNT_MOUNT_UPDATE`, `QUERY_PLAYER_NAMES_RESPONSE`). Zostają dwa worki wymagające danych
z klienta, nie kolejnej hipotezy.

### Sesja 2026-11-14 (cd. 5) — co dokładnie sniffować, żeby ruszyć puste bloki

Analiza 312 210 pakietów z sniffu 70862 pokazała, że bloki `Values` **bez** `HasFragmentUpdates`
mają kilka wariantów długości, a parser czyta tylko trzy bajty:

| Blok (B) | ile razy | co parser robi |
|---|---|---|
| 8 | 1 270 | czyta `[flags][idsChanged][mask]` i staje |
| 14 | 1 093 | zostaje 11 |
| **17** | **9 336** | zostaje 14 |
| **21** | **11 769** | zostaje 18 |
| 25 / 26 / 29 / 30 | ~7 500 | zostaje 22–26 |
| 46 / 49 | 1 664 | zostaje 43 / 46 |
| 128 | 1 565 | `GameObject`, zostaje 125 |

Czyli problem nie jest w nagłówku (ten czyta się dobrze — `flags`, `IdsChanged`, `ObjectChangesMask`
wychodzą sensownie), tylko w **treści po nagłówku**, której `ReadUpdateEntityFragmentDataForever`
nie ma jak czytać, bo `obj.EntityFragments` jest puste dla obcego obiektu i żaden fragment
nie jest wybierany do odczytu.

**Czego brakuje:** jednego snifu, w którym ten sam obiekt dostaje aktualizację wartości **z danymi
pola**, a nie samą maskę. Wtedy widać długość bloku, maskę i da się odczytać, co jest w środku.

#### Co robić w grze (plan sniffowania)

Cel: **ruch i interakcja**, nie stanie w miejscu. Sniff 70862 to 88 minut głównie bezruchu
(`MapID: 1` przez cały czas, 27 353 pakiety `SMSG_UPDATE_OBJECT`, z tego większość to puste maski).

1. **Zaloguj się i pokręć się po mieście** (Stormwind/Orgrimmar) — 2–3 minuty chodzenia wokół
   banku i AH. To generuje aktualizacje `Values` dla setek graczy/NPC z prawdziwymi polami.
2. **Wejdź do instancji albo na pole walki** — bij moby 3–4 minuty. To daje `UnitData`
   (`Health`, `Power`, `Auras`, `Target`) w blokach, których teraz nie umiemy czytać.
3. **Otwórz ekwipunek, załóż/zdejmij item, przełóż item w banku** — `ItemData` i `ContainerData`.
4. **Otwórz mapę, dziennik zadań, zaakceptuj i oddaj zadanie** — `PlayerData` z quest logiem.
5. **Przeleć się na inną mapę** (taxi albo hearthstone) — zmiana `MapID`, nowe obiekty.
6. **Zrób `/who` i kliknij kilka osób** — `SMSG_QUERY_PLAYER_NAMES_RESPONSE` z wieloma wynikami.
7. **Wyjdź z gry normalnie** — dopiero wtedy bufor sniffera się domyka.

Łącznie **8–10 minut** wystarczy. Plik `.pkt` wrzuć do `dumps/`, ja przeparsuję i porównam
rozkład długości bloków z tym, co mam teraz.

**Ważne:** nie trzeba nic zmieniać w konfiguracji sniffera — `interface.cfg` wskazuje kartę
`\Device\NPF_{F4FCA1D8-...}` i to działa (sniff 70862 powstał z niego).

### Sesja 2026-11-14 (cd. 6) — nowy sniff 70938 z ruchem w grze

Nowy sniff: `dump_1.60.1.70938_2026-11-14_12-41-07.pkt` (17,8 MB, **119 594** pakiety,
8,5 minuty, `MapID: 1`, Orgrimmar — kanały `rade (Local) - Orgrimmar`, `rade (Services) - City`).

| | |
|---|---|
| OK | 116 692 (**97,573%**) |
| błędy | 675 (0,564%) |
| pominięte | 1 181 (0,988%) |
| bez struktury | 1 046 (0,875%) |
| „Updatefields not fully read” | **13 326** |

To **lepszy** wynik niż na starym snifie (95,503%) — ale nie z powodu poprawki, tylko dlatego,
że ten sniff jest krótszy i ma mniej pakietów zbiorczych `SMSG_UPDATE_OBJECT`.

#### Co udało się ustalić — i co jest zablokowane

**Ustalone bez wątpliwości:**

1. `ReadUpdateUnitData` i `ReadUpdatePlayerData` **nigdy nie są wywoływane** na tym snifie
   (`UnitBlocksMask` / `PlayerBlocksMask` — 0 wystąpień). Bloki wartości idą wyłącznie
   ścieżką `CGObject` → `ReadUpdateObjectData` (maska 4 bity).
2. Bloki z `HasFragmentUpdates: true` mają `TypeMask` i czytają się **w całości** — w pakietach
   z `TypeMask` (251 wystąpień) liczba „Updatefields not fully read” wynosi **0**.
3. Bloki z `HasFragmentUpdates: false` mają **3-bajtowy nagłówek** `[flags][idsChanged][mask]`,
   po którym następuje treść, której parser nie czyta. Rozkład resztek: 5 (468), 14 (2 446),
   18 (3 214), 19 (583), 24 (291), 26 (242), 46 (1 226), 125 (575).

**Hipoteza odrzucona:** `TypeMask` (`0x26210300`, `0xFFCC0300`, `0xC9C80300`) **nie jest** maską
typów, którą rozumie kod (`bit0=Object, bit5=Unit, bit6=Player, bit8=GameObject`). Dla gracza
wychodzi `bit8` (czyli `GameObject`), a dla itemu `0xFFCC0300` daje bity 8,9,22,23,24… —
kombinacja niemożliwa. To raczej suma skrótu `u16 0x0300 | (n-1)` z `DROGA-DO-100.md`
i jakiegoś licznika. Zmiana w `UpdateHandler.cs` (dodanie odczytu `TypeMask` w ścieżce
`ReadUpdateEntityFragmentDataForever`) **nie wywołała się ani razu** — `TypeMask` w tym
snifie jest czytany tylko przy `HasFragmentUpdates: true`, gdzie kod już go obsługuje.

**Zablokowane:** te 13 326 bloków to nadal ten sam problem co w poprzedniej sesji —
treść po 3-bajtowym nagłówku bez struktury pól. Potrzebna lista pól `Values` dla Forever,
której nie ma ani w `tc-forever`, ani w `WowPacketParser`.

### Sesja 2026-11-14 (cd. 7) — droga przez klienta: sprawdzona i zamknięta

Pomysł: odczytać układ pól `Values` z samego klienta (czytnik zmian w `Wow.exe`),
tak jak `tc-forever` podaje `rva 0x44F47A0` (UnitData), `0xACB180` (PlayerData),
`0xA91D90` (ActivePlayerData).

**Wynik: nie da się z tego, co jest na dysku.**

| co sprawdzone | wynik |
|---|---|
| `WoWSniff/ghidra/Wow.exe` (80,6 MB) | `1.60.1.70888`, ale **cały `.text` ma entropię 7,997** — to nie jest kod, tylko dane zaszyfrowane/upakowane. Bajty pod `0xACB0D0` to szum (`5916fac021f9c850…`) |
| `.pdata` w tym pliku | 236 928 wpisów — struktura nagłówka jest, ale `.text` nie jest prawdziwym kodem |
| `0x44F47A0` w tym pliku | wypada w `.rdata`, **nie w żadnej funkcji** z `.pdata` |
| `0xACB180` i `0xA91D90` | leżą w funkcjach `0xACB0D0–0xACB2D4` i `0xA916C0–0xA91ED0` — czyli adresy z `tc-forever` **są poprawne dla tego buildu**, tylko ciała funkcji są zaszyfrowane |
| `WoWSniff/dump_70938.img`, `dump_70938_live.img` | 116 MB zrzuty pamięci `Wow.exe` (robione przez `dumpwow.exe`) — ta sama sytuacja: obraz pamięci zaszyfrowanego `.text` |
| `classic_re/` | katalog, z którego `tc-forever` wziął `upd_bits.py` — **nie ma go na dysku** |

**Wniosek:** nie ma jak odczytać układu pól bez odszyfrowania `.text` klienta
(albo bez `classic_re/`), a to osobny, duży projekt — nie część parsowania sniffa.

**Co to znaczy dla WPP:** te 13 326 bloków zostaje na 3-bajtowym nagłówku. Nagłówek
(`flags`, `IdsChanged`, `ObjectChangesMask`) jest odczytany poprawnie i potwierdzony
na 251 pakietach z `TypeMask` (0 błędów). Reszta to treść, której układu nie znamy
z żadnego dostępnego źródła.

### Sesja 2026-11-14 (cd. 8) — przegląd tc-forever, co jeszcze warto dodać

Przejrzałem `tc-forever/src/server/game/Server/Packets/` pod kątem opcodów, które w snifie 70938
nadal mają `Packet not fully read`. Wynik:

**Dodane i zmierzone:**

| opcode | było | jest | źródło |
|---|---|---|---|
| `SMSG_UPDATE_ACTION_BUTTONS` (0x470083) | 1440 B nieprzeczytanych | **0** | tc-forever `SpellPackets.cpp:65` + `SpellPackets.h:124` (`ClassicNumActionButtons = 360`) |

Pakiet ma dokładnie 2881 B = 360 × 8 + 1 (`Reason`). Fallback z 4.4 czyta 180 przycisków,
czyli połowę. Nowy handler: `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverActionBarHandler.cs`.
Rozkład `uint64` ten sam co w 4.4: dolne 56 bitów = akcja, górny bajt = typ.

Metryka całości bez zmian (97,575% / 673 błędy) — ten opcode nie był liczony jako błąd,
tylko jako „nie w pełni przeczytany".

**Sprawdzone i odrzucone (nie da się dopasować do pakietu):**

1. `SMSG_TRAINER_LIST` (0x47018D) — 28 pakietów, wszystkie 48 B. Uklad z tc-forever
   (`NPCPackets.cpp:166`: `[Guid][int8 type][u32 id][u32 count][spells][greeting]`) **nie pasuje**:
   pierwsze 12 B to `89 14 00 00 | 0E 00 00 00 | 00 00 00 00` = 5289 / 14 / 0, czyli
   `TrainerID / SpellID / MoneyCost`. Po nich maska GUID-a `0xE006` (5 bajtów) zostawia
   29 B — nie dzieli się na wpis 33-bajtowy. Z maską `u8 0x06` (2 bajty) zostaje 33 B, czyli
   dokładnie jeden wpis — ale wtedy kończy się `EndOfStream`. **Emulator i prawdziwy serwer
   mają tu inny układ**, więc bez drugiego źródła nie da się tego rozłożyć.
2. `SMSG_HEALTH_UPDATE` (0x47017F) — 16 B. `ReadPackedGuid128` (maska `0xE006`, 5 bajtów)
   zostawia 1 B; maska `u8 0x06` (2 bajty) daje poprawne `Health = 203521`, ale zostawia 5 B.
   Żaden z dwóch wariantów się nie domyka. Handler zostaje na oryginalnym `ReadPackedGuid128`,
   bo przynajmniej nie psuje wartości Health.
3. `SMSG_SEASON_INFO` (0x470065) — 37 B, parser czyta 29. tc-forever `BattlegroundPackets.cpp:23`
   pisze 7 × i32 + 3 bity = 29 B. **Sniff ma 8 bajtów więcej** — ten sam wzorzec co
   `FEATURE_SYSTEM_STATUS` i `UPDATE_ACCOUNT_DATA_COMPLETE`.
4. `CMSG_VOICE_CHAT_LOGIN` — tc-forever nie ma w ogóle obsługi voice chat.

### Sesja 2026-11-14 (cd. 9) — FailedBy w pakietach czarów (+124 pakiety)

tc-forever `SpellPackets.cpp` ma we wszystkich trzech pakietach czarów pole `FailedBy`
(pusty `ObjectGuid` = 2 bajty maski `0x0000`):

| linia | pakiet | układ |
|---|---|---|
| 918 | `SMSG_SPELL_CHANNEL_UPDATE` | `[CasterGUID][int32 TimeRemaining][FailedBy]` |
| 532 | `SMSG_SPELL_FAILURE` | `[CasterUnit][CastID][i32 SpellID][Visual][u16 Reason][FailedBy]` |
| 544 | `SMSG_SPELL_FAILED_OTHER` | `[CasterUnit][CastID][u32 SpellID][Visual][u8 Reason][FailedBy]` |

Fallback (9.x `SpellHandler.cs:358`) czyta `FailedBy` **tylko** dla `ClientBranch.Retail >= V12_0_0_65390`,
więc dla Forever zostawały dokładnie 2 bajty — niezależnie od długości pakietu
(13/14/15/19/20/21 B dla CHANNEL_UPDATE, 37/38/39 B dla FAILED_OTHER, 38/39/40 B dla FAILURE).
To był najlepszy dowód: reszta zawsze równa 2.

Nowy plik: `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverSpellFailHandler.cs`.

**Wynik: 116 694 → 116 819 OK (97,575% → 97,680%), błędy 673 → 548, not-fully-read 586 → 29.**

Zostało tylko 29 niepełnych odczytów w 12 opcodach:

| opcode | ile |
|---|---|
| `SMSG_UPDATE_OBJECT` | 12 |
| `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | 4 |
| `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | 3 |
| `CMSG_QUERY_NEIGHBORHOOD_INFO` | 2 |
| pozostałe 8 opcodów | po 1 |

### Sesja 2026-11-14 (cd. 10) — QUERY_CREATURE_RESPONSE (+77 pakietów)

tc-forever `QueryPackets.cpp:75` ma w komentarzu wprost opisany układ Forever:

    [bits 6: liczba wpisów][FlushBits]
    per wpis: [u32 CreatureID][u8 result]   (0 = dane są, 1 = brak danych)

Fallback (9.x `CreatureHandler.cs:13`) czyta od razu `u32` jako `Entry` i bit `HasData`,
więc 77 pakietów po **6 bajtów** (`04 | 1F 34 04 00 | 01`) zostawiało 1 bajt, a pakiety
z danymi (120–163 B) — 115–158 bajtów.

Nowy plik: `ForeverQueryCreatureHandler.cs`. Efekt:

| | przed | po |
|---|---|---|
| OK | 116 819 (97,680%) | **116 896 (97,744%)** |
| błędy | 548 | **471** |
| `QUERY_CREATURE_RESPONSE` niepełnych | 132 | **0** |

**Co dalej zablokowane:** w pakietach z danymi (120–163 B) po tekstach Name/NameAlt idzie
37 bajtów, których nie da się dopasować do żadnego układu retail (4× `u32` TypeFlags,
`u8` type, `i32` family…). Wartości to `0x00080000, 0, 0x01000000, 0` — nie są to flagi
w żadnej znanej numeracji. Dalej widać poprawnie `CreatureDisplayID 146918`, `Scale 1.5`,
`HpMulti 1.35`, `EnergyMulti 1.0`, `Title "Great Windborne Cat Spirit"` — czyli od displaya
w górę układ się zgadza. Brakuje jedynie tych 37 bajtów przed nim. Wymaga drugiego źródła
(klient albo inny sniff z innym zestawem flag).
