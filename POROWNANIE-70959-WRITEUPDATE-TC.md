# 70959 — porównanie tc-forever `UnitData::WriteUpdate` z tym, co robi parser

Data: 2026-11-20. Kod parsera nietknięty. Źródło: `TrinityCore` branch `forever-bots`,
`src/server/game/Entities/Object/Updates/UpdateFields.cpp`.

## Jak serwer pisze blok wartości (tc-forever, `WriteUpdate`, linia 1405)

```cpp
std::array<uint32, 8> classicBlocks = WriteClassicChangesMaskHeader<8>(data, changesMask,
    &ClassicUnitDataBit, ClassicUnitDataGroups70425);
bool classicGroup128 = (classicBlocks[4] & 1) != 0;
if (IsBuild70291OrLater()) data.WriteBit(false);   // jeden bit po nagłówku maski
```

Nagłówek maski (`WriteClassicChangesMaskHeader`, linia 1375):
- `WriteBits(classicBlocksMask, 8)` — 8 bitów: które z 8 bloków są niezerowe,
- potem **tylko dla niezerowych** bloków: `WriteBits(block, 32)`.

To jest dokładnie to, co robi `ForeverUnitDataReader` (`BlockCount = 8`, `ReadBits("UnitBlocksMask", 8)`,
potem `ReadBits("UnitBlock", 32)`). **Zgadza się co do bitu.**

## Kluczowe: jak numerowane są bity

`ClassicUnitDataBit` (linia 1339) przenumerowuje bity retail→Classic przez
`ClassicUnitDataBit70009` + przesunięcia `70291` (+1 od 140) i `70425` (+3 od 132).
Parser ma **tę samą funkcję** w `ForeverClassicBitRemap.UnitData` (linia 47) —
przepisaną 1:1, z tymi samymi warunkami.

Czyli **remap bitów jest poprawny** i nie jest przyczyną rozjazdu.

## Gdzie stoją `Health` / `MaxHealth` (TC `changesMask`, linia 1659)

| TC bit | pole | typ |
|---|---|---|
| 38 | `Health` | int64 |
| 39 | `MaxHealth` | int64 |
| 40 | `Level` | int32 |
| 41 | `EffectiveLevel` | int32 |
| 47 | `FactionTemplate` | int32 |

Wszystkie w grupie TC `32..63` = blok 1 maski.

## Obserwacja z logu, która to potwierdza

Blok `Values` gracza (sniff 10-24) czyta się tak:

```
UnitBlocksMask: 2          <- tylko blok 1 (bit 1 zapalony)
[0] [1] UnitBlock: 1048577  <- 0x100001: bity 0 i 20
Unknown70291: False
AuraState: 13631488
LocalFlags: 266248
```

`UnitBlocksMask: 2` = tylko blok 1 → grupa `32..63`, czyli **`Health`/`MaxHealth`/`Level`
są w tym bloku**. Sam odczyt maski działa.

## Gdzie jest więc błąd

Skoro maska i remap są poprawne, a `Health`/`MaxHealth` wychodzą źle — błąd jest
w **odczycie pól po masce**, nie w masce. Konkretnie w `ForeverUnitDataReader`:

```csharp
if (is70938 || Changed(32))
{
    ...
    if (Changed(38)) data.Health = packet.ReadInt64("Health", indexes);
    if (Changed(39)) data.MaxHealth = packet.ReadInt64("MaxHealth", indexes);
```

Warunek `is70938 || Changed(32)` — dla 70959 `is70938` jest **false**, więc wchodzi tylko
przy `Changed(32)`. A bit 32 to **bit grupowy** grupy `32..63`, nie pole. Czyli:
- gdy bit grupowy 32 jest zapalony → wchodzi i czyta pola według bitów 33..63,
- gdy nie jest → **cała grupa jest pominięta**, nawet jeśli w bloku 1 są zapalone bity pól.

To wyjaśnia wzorzec: `Level` wychodzi poprawnie dla 92 567 creature (bit grupowy bywał
zapalony), a `Health`/`MaxHealth` nie — bo przy bitach 38/39 zachowanie zależy od tego,
czy bit 32 był ustawiony.

## Wniosek

To jest **naprawialne i precyzyjnie zlokalizowane**: `is70938` w `ForeverUnitDataReader`
traktuje 70959 jak 70938 w kwestii „zawsze czytaj grupę 32”, ale 70959 wysyła bloki
z bitem grupowym, którego ten warunek nie honoruje.

Następny krok: zamienić `is70938` na `Is70938OrLater` (czyli objąć 70959) w tej jednej
gałęzi i zmierzyć na `10-24`, licząc **creature w SQL** (dziś 7), nie tylko wyjątki.

Uwaga: `is70938` występuje w tym pliku w wielu miejscach — zmiana musi objąć **tylko**
tę gałąź grupy 32, żeby nie powtórzyć błędu z `IsForever` (tam poszerzenie zakresu
zepsuło `EndOfStreamException`).
