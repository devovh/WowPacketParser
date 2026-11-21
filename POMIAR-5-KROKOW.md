# 5 kroków naprawy UnitData — zmierzone

Data: 2026-11-21. Sniff: `dump_1.60.1.70959_2026-11-19_10-24-12.pkt`.

## Co zmieniłem

Dokładnie to, co wynikało z odpowiedzi dewelopera, **jako podmiana, nie dodanie**:

| # | krok | efekt na długości |
|---|---|---|
| 1 | `StateWorldEffectIDs` czytane dla 70959/71037 (`Has70938Fields`) | **+4 B** na starcie UnitData |
| 2 | `ModSpellHaste` **tylko dla ownera** (usunięty warunek „nie-70938") | **−4 B** dla stworów i innych graczy |
| 3 | `ClassicTrainingPoints*` **po `EmoteState`** (jak w 70938), nie na końcu | 0 B netto, przełożone |
| 4 | **usunięte** obejście „float po `ModTimeRate`" | **−4 B** (to był ten sam bajt co krok 2) |
| 5 | usunięta kopia `ClassicTrainingPoints*` z końca bloku | 0 B netto |

Dodałem `ForeverBuild.Has70938Fields` — buildy, w których writer pisze to samo co 70938
(70938, 70959, 71037), zgodnie z tym co napisał deweloper: *„the 70959 create bytes are
exactly the 70938 ones, there is no 70959 branch anywhere"*.

## Wynik pomiaru

| miara | baza | po 5 krokach | różnica |
|---|---|---|---|
| `Updatefields not fully read` | 171 994 | **174 648** | +2 654 (gorzej 1,5%) |
| `Packet not fully read` | 719 | **724** | +5 (gorzej) |
| `InvalidDataException` | 346 | **223** | **−123 (lepiej 36%)** |
| `EndOfStreamException` | 820 | **938** | +118 (gorzej 14%) |
| **wierszy `creature` w SQL** | **7** | **7** | **bez zmian** ✔ |

## Jak to czytać

**`creature` wraca do 7** — czyli naprawa nie psuje spawnowania stworów. To był
główny warunek, żeby uznać zmianę za sensowną (poprzednie próby spadały do 1).

**`InvalidDataException` spadło o 36%** (346 → 223). To realna poprawa: mniej bloków
kończy się wyjątkiem i utratą całego create.

**`Updatefields not fully read` wzrosło o 1,5%** (2 654). To drobne pogorszenie, ale
licznik jest zdominowany przez bloki `Values` dla graczy (171k z 174k), a nie przez create.

**`EndOfStreamException` wzrosło o 118** — to najsłabszy punkt. Znaczy, że w 118
przypadkach parser czyta teraz **więcej** niż jest w pakiecie, czyli nasz UnitData
jest za długi w jakiejś ścieżce.

## Co to znaczy

Hipoteza dewelopera **częściowo się potwierdziła**:

✔ Układ `UnitData` dla stworów — naprawiony, `creature` bez zmian na 7
✔ `InvalidDataException` — wyraźnie lepiej
✖ `EndOfStreamException` — gorzej, gdzieś czytamy za dużo

## Gdzie prawdopodobnie jest reszta

`EndOfStreamException` +118 przy `Updatefields` niemal bez zmian sugeruje, że problem
jest **poza UnitData** — najpewniej w `PlayerData` albo w nagłówku nazwy
(3 bajty, MSB-first, jeden nagłówek dla imienia i nazwiska).

Deweloper napisał: *„lines 1551 and 1559: the surname bits and string are only read
for 70938"*. **Tej zmiany NIE zrobiłem** — świadomie, bo dotyczy ścieżki gracza,
a chciałem najpierw ustalić, czy UnitData jest ok. Jest.

## Następny krok

Włączyć nagłówek nazwy (linie 1551/1559) dla 70959/71037 — 6 bitów imienia, 9 bitów
nazwiska, jeden nagłówek, potem oba stringi. To powinno zaadresować `EndOfStreamException`.

## Stan

Zmiana w `WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs`.
Niezacommitowana. Backup: `$TMPDIR/UFH-71037.bak`.
