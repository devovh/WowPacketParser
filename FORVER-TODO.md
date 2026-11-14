# Forever 1.60.1 — co zostało do zrobienia

Stan na 2026-11-14. Szczegóły i pomiary: `FOREVER-STAN.md`, `RAPORT-FOREVER-UPDATE-OBJECT.md`.

## Zrobione (nie ruszać bez powodu)

- `SMSG_UPDATE_OBJECT` — nagłówek, GUID-y (u16 maska + popcount), fragmenty encji,
  maska typów, blok gracza (11 bloków), blok aktywnego gracza (13 bloków).
- `SMSG_CHAT` (0x4C0001) — 118/118 pakietów bez błędu.
- `SMSG_TUTORIAL_FLAGS` — 8 × u32 bez nagłówka `count`.

## Do zrobienia — w kolejności opłacalności

### 1. `SMSG_QUERY_PLAYER_NAMES_RESPONSE` / `HandleNameQueryResponse` — 23 błędy (70938)

`QueryHandler.HandleNameQueryResponse` rzuca `EndOfStreamException` / `ArgumentOutOfRange`.
Sprawdzić w sniffie, czy odpowiedź Forever ma `u32 count` + `packed GUID` per wpis,
czy inny układ (np. liczba jako `u16`). Analogicznie `SMSG_QUERY_CREATURE_RESPONSE`.

### 2. `CharacterHandler.ReadPlayerGuidLookupData` — 25 błędów (70938)

Czytane jest `GuidLookupMask`, potem warunkowe bloki. Forever prawdopodobnie
nie wysyła wszystkich pól, które zakłada maska 12.x. Zmierzyć na pakietach,
które bity maski faktycznie występują w sniffie.

### 3. `HandleEnumCharactersResult` / `ReadRaceUnlockData` / `ReadClassUnlockData` — po 2 błędy

`EnumCharactersResult` w Forever nie ma listy `RaceUnlock`/`ClassUnlock` w formacie 12.x.
Ustalić długość pakietu i porównać z odczytem.

### 4. Blok wartości `SMSG_UPDATE_OBJECT` — kolejność pól pod maską (ZABLOKOWANE)

Nie da się ustalić ze sniffów: 785 unikalnych ciał dla maski 0x17, 582 dla 0x03,
brak powtarzalnego wzorca. Potrzebne:

- 3–5 pakietów **tego samego obiektu** z różnymi maskami, albo
- zrzut z pamięci klienta (`.text` w `Wow.exe` 70938 jest zaszyfrowana —
  entropia 8,00, brak prologów i stringów; potrzebny dump z działającego procesu).

Do tego czasu `ReadForeverValuesBody` czyta maskę i `nBits` i odrzuca resztę bloku —
to jest celowe, żeby nie psuć kolejnych pakietów. Wynik 95–96% jest przez to zawyżony
o ~1,1 pkt (wariant „maska bez pól” dawał 96,68%, ale wyrzucał dane — odrzucony świadomie).

### 5. 5 najdłuższych wiadomości `SMSG_CHAT` (UTF-8)

Licznik długości to `(znaki × 4) & 0xFF`; przy > 63 znakach trzeba dobrać `+64·k`.
Dla 5 wiadomości > 250 znaków żadne `k` nie trafia w ogon — jednostka długości
dla długich tekstów UTF-8 nieustalona. Treść jest odzyskiwana poprawnie (do ogona),
więc to kosmetyka licznika, nie błąd parsowania.

### 6. 76 pakietów `ArgumentOutOfRangeException` w sniffie 70862

Jeden pakiet ze złym `Data size` (127 078 B) psuje 75 kolejnych.

## Jak sprawdzać

```
dotnet build WowPacketParser.slnx -c Release -v q --nologo
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config "<plik.pkt>"
```

Punkt odniesienia: **70938 = 96,598% (227 błędów)**, **70862 = 95,074% (4 330 błędów)**.
Każda zmiana, która tego nie poprawia, jest cofana.
