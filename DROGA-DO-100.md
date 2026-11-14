# Droga do 100% — Forever 1.60.1 (build 70938)

Stan na 2026-11-14, po sesji: `SMSG_HIGHEST_THREAT_UPDATE` i `SMSG_TRAINER_LIST`.

## Punkt startowy tej sesji

| Sniff | Pkty | OK | Błędy | Pominięte | Bez struktury |
|---|---|---|---|---|---|
| 70938 (13-18-57) | 29 428 | 28 427 (**96,598%**) | 227 (0,771%) | 201 | 573 |
| 70862 (312 210) | 312 210 | 296 829 (**95,074%**) | 4 330 (1,387%) | 6 853 | 4 198 |

## Po tej sesji

| Sniff | Pkty | OK | Błędy | Zmiana |
|---|---|---|---|---|
| 70938 (13-18-57) | 29 428 | 28 427 (**96,598%**) | 227 | bez zmian |
| 70862 (312 210) | 312 210 | 297 988 (**95,445%**) | 3 171 | **+1 159 pakietów, −1 159 błędów** |

Build: 0 błędów, 0 ostrzeżeń.

### Co zostało naprawione

`SMSG_HIGHEST_THREAT_UPDATE` (0x470187) — **538 błędów → 0**.

Układ zmierzony na wszystkich 538 pakietach (każdy ma dokładnie 33 B i domyka się bez reszty):

```
[0]  GUID  UnitGUID          maska 0xEF2F = 12 bitów -> 14 B
[14] u32   ThreatListCount   (zawsze 1)
[18] GUID  ThreatGUID        maska 0xE006 = 5 bitów  -> 7 B
[25] u32   Threat
[29] u32   Threat2           (serwer powtarza tę samą wartość)
```

`SMSG_TRAINER_LIST` (0x47018D) — **621 błędów → 0**.

Układ zmierzony na wszystkich 621 pakietach (każdy 48 B):

```
[0]  u32   TrainerID
[4]  u32   SpellID            (rośnie o 1 w kolejnych pakietach tego samego trenera)
[8]  u32   MoneyCost
[12] GUID  TrainerGUID        maska 0xE006 = 5 bitów -> 7 B
[19] u32   ReqSkillLine
[23] u32   ReqSkillRank
[27] u32   ReqAbility1
[31] u32   ReqAbility2        (serwer powtarza)
[35] u32   ReqAbility3        (zawsze 0)
[39] u32   ReqAbility2        (duplikat)
[43] u32   ReqAbility3        (duplikat, zawsze 0)
[47] u8    padding
```

**Wspólny mianownik obu:** parser 6.x czyta tam `ReadPackedGuid128` (maska 2-bajtowa
low/high). Forever używa **maski u16 LE + tyle bajtów, ile bitów zapalonych**.
Dlatego nowy `ForeverGuid.Read` rozłożył oba opcody bez reszty.

### Pliki

- `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverGuid.cs` — odczyt GUID-a Forever
- `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverThreatHandler.cs`
- `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverTrainerHandler.cs`

---

## Czego potrzeba, żeby dojść do 100%

„100%” to nie jedna liczba. Rozbite na cztery worki:

### Worek A — błędy parsowania: 227 (70938) / 3 171 (70862)

| Opcode | 70938 | 70862 | Co trzeba |
|---|---|---|---|
| `SMSG_UPDATE_OBJECT` | 131 | 1 756 | **zrzut z klienta** (patrz niżej) |
| `SMSG_QUERY_PLAYER_NAMES_RESPONSE` (`ReadPlayerGuidLookupData`) | 25 | 118 | zmierzyć układ maski z pakietów |
| `SMSG_QUERY_PLAYER_NAMES_RESPONSE` (`ReadNameCacheLookupResult`) | 3 | 9 | j.w. |
| `SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE` | 16 | 16 | układ Forever |
| `SMSG_QUERY_QUEST_INFO_RESPONSE` | 9 | 9 | j.w. |
| `SMSG_ENUM_CHARACTERS_RESULT` (`ReadClassUnlockData`) | 2 | 3 | j.w. |
| reszta (po 1–4) | ~41 | ~1 260 | drobnica |

**Największy pojedynczy zysk:** `SMSG_UPDATE_OBJECT`. To 1 756 z 3 171 błędów w 70862
i 131 z 227 w 70938. Bez niego nie ma 100%.

### Worek B — pakiety bez struktury: 573 (70938) / 4 198 (70862)

Parser nie zna struktury opcodu, więc wypisuje surowy hexdump. To nie są błędy.
Najczęstsze:

| Opcode | Ile | Co to jest |
|---|---|---|
| `0x4400B8` | 195 | `CMSG_REQUEST_CHARACTER_GUILD_FOLLOW_INFO` |
| `0x53002C` | 195 | nieznany (`SMSG_UNKNOWN_53002C`) |
| `0x4701F3` | 125 | `SMSG_ACTIVE_SCHEDULED_WORLD_STATE_INFO` |
| `0x460010` | 52 | nieznany |
| `0x42007F` | 39 | nieznany |

Wystarczy napisać dla nich handlery — to najtańszy worek do zamknięcia.

### Worek C — pakiety pominięte: 201 / 6 853

`skipped` — parser ich nie dotyka (filtry, brak handlera). Do sprawdzenia osobno.

### Worek D — blok wartości `SMSG_UPDATE_OBJECT` (ZABLOKOWANE)

**To jest jedyna rzecz, której nie da się zrobić ze sniffów.**

Co już wiadomo na pewno:

- nagłówek: `[u16 map][u32 blockCount][u8 bity][u32 dataSize]`
- blok: `[u8 UpdateType][GUID Forever][u32 rozmiar][rozmiar bajtów]`
- ciało: `[u8 IsOwned][u8 HasFragmentUpdates][u8 maskaTypu][u8 nBits][maski][pola]`
- maski: `0x15` (blok 3-bajtowy) × 13 264, `0x17`/nBits=32 × 1 222, `0x03`/nBits=32 × 593

Czego **brakuje**: kolejności pól pod maską. Zmierzone: 785 unikalnych ciał dla maski
`0x17`, 582 dla `0x03` — brak powtarzalnego wzorca. Ze sniffów się tego nie wyciągnie.

**Potrzebne (jedno z dwóch):**

1. **Zrzut pamięci z działającego klienta** — `wow_loader.dll` ma odszyfrowaną `.text`
   (31,99 MB, entropia 7,159), więc funkcje `WriteUpdate`/`BuildValuesUpdate` są tam
   czytelne. Narzędzie `dumpwow.exe` jest już skompilowane
   (`C:/Users/devov/Desktop/WoWSniff/dumpwow.exe`), ale **Wow.exe musi być uruchomiony**.
   Wtedy: `./dumpwow.exe out/wow_70938_full.img` → Ghidra → kolejność pól.

2. **Emulator `tc-forever`** — plik `UpdateFields.cpp` z `PlayerData::WriteUpdate`
   i `ClassicPlayerDataBit` daje kolejność pól wprost, bez reverse engineeringu.

**Bez jednego z tych dwóch worek D zostaje zamknięty.** Obecny parser czyta maskę
i `nBits`, a resztę bloku odrzuca — celowo, żeby nie psuć kolejnych pakietów.

### Ile realnie da się osiągnąć

| Scenariusz | 70938 | 70862 |
|---|---|---|
| teraz | 96,598% | 95,445% |
| + worki B i C (bez D) | ~98,2% | ~98,0% |
| + worek A bez `UPDATE_OBJECT` | ~99,0% | ~98,6% |
| + worek D (zrzut lub emulator) | **~100%** | **~100%** |

Czyli: **worki A, B, C zamkniemy samymi sniffami. Do 100% potrzebny jest zrzut
pamięci z uruchomionego klienta albo kod `tc-forever`.**

---

## Jak sprawdzać po każdej zmianie

```
dotnet build WowPacketParser.slnx -c Release -v q --nologo
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config "<plik.pkt>"
```

Punkt odniesienia: **70938 = 96,598% (227 błędów)**, **70862 = 95,445% (3 171 błędów)**.
Każda zmiana, która tego nie poprawia, jest cofana.
