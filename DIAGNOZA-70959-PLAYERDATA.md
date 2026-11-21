# Mapa diagnostyczna `PlayerData` w 70959 — z prawdziwego przebiegu parsera

Zmierzona instrumentacją w `ReadCreatePlayerData`, na `dump_1.60.1.70959_2026-11-18_04-12-11.pkt`.
Nie jest to wnioskowanie ze źródła — to pozycje, które parser faktycznie osiąga.

## Gdzie zaczyna się `PlayerData`

Z 318 zmierzonych wywołań: **start w przedziale 641–668** bajta wewnątrz bloku pól obiektu.

To dobra wiadomość: granica bloku jest stabilna i zgadza się z `u32 @7571 = 2051`.
Problem nie leży w nagłówku create — leży w środku `PlayerData`.

## Gdzie ląduje `GuildLevel`

Odległość `GuildLevel` od startu `PlayerData`, rozkład na 318 wywołaniach:

| offset | ile razy |
|---|---|
| +36 | 13 |
| +38 | 11 |
| +39 | 9 |
| +40 | 10 |
| +41 | 38 |
| +42 | 20 |
| +43 | 8 |
| +44 | 23 |
| +45 | 11 |
| +46 | 11 |
| +47 | 23 |
| +48 | 18 |
| +49 | 36 |
| +50 | 50 |
| +51 | 20 |
| +52 | 6 |
| +53 | 3 |
| +54 | 4 |
| +55 | 3 |
| +56 | 1 |

**To jest poprawne zachowanie, nie błąd.** Guidy w `PackedGuid128` mają zmienną długość, więc stałe pole ląduje pod zmiennym offsetem — i serwer pisze je tak samo. Rozrzut +36…+56 to różnica w długościach trzech guidów, `GuildClubMemberID` i `LootTargetGUID`.

## Gdzie parser się wywala

| wyjątek | `pos` przy wywaleniu | ile zostało |
|---|---|---|
| `InvalidDataException: Dynamic update field count …` | ~690–725 | ~1337–1432 |
| `EndOfStreamException` | = `len` bloku | 0 |
| `OverflowException` | 1854, 1870 | 266, 199 |

**Wszystkie `InvalidDataException` padają w odległości +41…+56 od startu — czyli zaraz po `GuildLevel`.**

To jest miejsce, w którym źródło pisze:

```cpp
data << int32(GuildLevel);
data << uint32(ClassicCharacterSuperDistrictID()) << uint32(0) << uint32(0) << uint32(0) << int8(0);
data << uint32(Customizations.size());
```

Parser czyta tam `Customizations.Resize(packet.ReadUInt32())` **bez 17-bajtowej struktury** dla 70959, więc jako licznik bierze pierwszy `uint32` super district — i dostaje śmieć (`590336`, `16777216`, `419430400`, …).

## Sprzeczność, która została do rozwiązania

Dodanie struktury dla 70959 **pogorszyło** wynik (170 → 179 przy obu kopiach, 170 → 181 przy jednej). A diagnostyka mówi, że parser pada dokładnie tam, gdzie tej struktury brakuje.

Obie rzeczy mogą być prawdziwe jednocześnie, jeśli **`ClassicCharacterSuperDistrictID()` nie jest jedyną różnicą** — czyli jeśli struktura ma inną długość niż 17 bajtów, albo jeśli przed nią jest jeszcze pole, którego nie czytam.

## Flaga, którą warto zauważyć

W 318 zmierzonych wywołaniach **`flags` było zawsze `UnitAll`** — ani razu `PartyMember`. Czyli 175-wpisowy quest log **nie jest czytany** w tych pakietach i nie może być przyczyną.

## Co to daje

1. Granica bloku jest potwierdzona i nie wymaga dalszych pomiarów.
2. Miejsce wywalenia jest znane co do bajtu: **zaraz po `GuildLevel`**.
3. Hipoteza „za mało pól na początku" jest **wykluczona** — guidy czytają się poprawnie, ich zmienna długość wyjaśnia rozrzut.
4. Zostaje **jedna** rzecz do zmierzenia: ile bajtów naprawdę ma struktura po `GuildLevel` w tym buildzie.

Instrumentacja jest tymczasowa i nie jest zacommitowana — to są pozycje z jednego przebiegu, nie zmiana parsera.
