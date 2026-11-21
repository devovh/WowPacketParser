# Nagłówek nazwy (1551/1559) — zmierzone

Data: 2026-11-21. Sniff: `dump_1.60.1.70959_2026-11-19_10-24-12.pkt`.

## Co zmieniłem

Dwa miejsca w `ReadCreatePlayerData` — tak jak pisał deweloper:

```csharp
// przed: nazwisko tylko dla 70938
var surnameLength = ClientVersion.Build == ClientVersionBuild.V1_60_1_70938
    ? (int)packet.ReadBits(9) : 0;
...
if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
    packet.ReadWoWString("Surname", surnameLength, indexes);

// po: dla 70938, 70959 i 71037 (ForeverBuild.Has70938Fields)
```

**Ważne:** nagłówek nazwy to **jeden nagłówek dla obu stringów** — 6 bitów imienia,
potem 9 bitów nazwiska, potem bity `HasLevelLink` / `HasDeclinedNames`, a **dopiero
wtedy** oba stringi jeden za drugim. Tak to już było w kodzie dla 70938; brakowało
tylko dopisania 70959/71037.

## Wynik pomiaru

| miara | baza | 5 kroków | **+ nagłówek nazwy** |
|---|---|---|---|
| `Updatefields not fully read` | 171 994 | 174 648 | **173 747** (−901) ✔ |
| `Packet not fully read` | 719 | 724 | **722** (−2) ✔ |
| `InvalidDataException` | 346 | 223 | **187** (−36) ✔ |
| `EndOfStreamException` | 820 | 938 | **969** (+31) ✖ |
| **wierszy `creature`** | **7** | **7** | **7** ✔ |

## Jak to czytać

**`InvalidDataException` spadło łącznie o 46%** (346 → 187). To jest duża, realna poprawa:
mniej bloków kończy się wyjątkiem i utratą całego create.

**`Updatefields not fully read` wreszcie spadło** (174 648 → 173 747). Czyli nagłówek
nazwy jest poprawny — przesunięcie o 9 bitów trafiło we właściwe miejsce.

**`creature` stoi na 7.** Zero regresji w spawnowaniu.

**`EndOfStreamException` dalej rośnie** (938 → 969, +31). To jedyny zły wskaźnik,
i rosnący od samego początku (820 → 938 → 969). Czyli **nasz blok `PlayerData` jest
za długi o jakieś bajty** — czytamy więcej, niż serwer napisał.

## Gdzie prawdopodobnie jest reszta

Widać to w samym dumpie. Blok create gracza:

```
[0] [0] PlunderstormItemDisplayID: 2148466688     <- 0x80100000, śmieć
[0] [1] PlunderstormItemDisplayID: 1194423354     <- 0x4732...  , śmieć
[0] [2] PlunderstormItemDisplayID: 120
[0] [13] PlunderstormItemDisplayID: 4294967295    <- 0xFFFFFFFF
```

`PlunderstormItemDisplayID` to pole **zupełnie nieobecne w 1.60.1** — to dodatek
z retail (Plunderstorm to tryb z 10.2.6). Parser czyta **19 × u32 = 76 bajtów**,
których serwer Forever nigdy nie pisze.

To najprawdopodobniej **właśnie źródło `EndOfStreamException`**: czytamy 76 bajtów
za dużo w każdym create gracza.

## Następny krok

Sprawdzić, czy `PlunderstormItemDisplayID` i sąsiednie pola retail-only
(`PersonalTabard`, `NpcAsPlayerInfo`, `DistrictValue`/`DistrictFlag`) są w 70959
zapisywane. Jeśli nie — usunąć je z odczytu dla 70959/71037.

Uwaga: `DistrictValue`/`DistrictFlag` **już są** warunkowane na `== 70938` w kodzie
(linia ~1515), czyli ktoś wcześniej zauważył ten sam problem dla jednego pola.
