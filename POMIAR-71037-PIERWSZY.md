# Sniff 71037 — pierwszy pomiar

Data: 2026-11-21. Plik: `dump_1.60.1.71037_2026-11-21_10-24-14.pkt` (8,1 MB, ~12 minut gry).

## Wynik: alias 71037 = 70959 DZIAŁA

Parser nie wysypał się na nieznanym buildzie. Sparsował plik, wygenerował SQL i
tekst. To potwierdza, że **numer builda jest obsłużony**.

## Metryki

| miara | 71037 (12 min) | 70959 (60 MB) | proporcja |
|---|---|---|---|
| `Updatefields not fully read` | 8 805 | 171 994 | 5% |
| `Packet not fully read` | 37 | 719 | 5% |
| `InvalidDataException` | 29 | 346 | 8% |
| `EndOfStreamException` | 62 | 820 | 7% |
| bloków `FieldFlags` | 477 | 5 532 | 9% |
| **wierszy `creature`** | **1** | **7** | 14% |

**Wszystkie proporcje mieszczą się w 5–14%** — czyli dokładnie tyle, ile wynika
z różnicy rozmiaru sniffu. **Układ bajtów 71037 jest taki sam jak 70959.**

To jest najważniejsze: notatka Blizzarda („build serwerowy, żadne dane się nie
zmieniają") **potwierdziła się pomiarem**.

## Co z tego nie wynika

**Nie da się porównać 1:1**, bo sniffy mają inną długość i inną zawartość:

- 71037: 8 MB, prawdopodobnie krótka sesja (logowanie, postać w mieście)
- 70959: 60 MB, dłuższa sesja z większą liczbą stworów

Wskaźnik `creature` 1 vs 7 przy 9% bloków create jest **spodziewany** — mniej
materiału, mniej stworów do zapisania.

## Co widać w błędach

Pierwszy `Updatefields not fully read`:

```
UpdateType: Values
Object Guid: ... Player/0
IsOwned: true
HasFragmentUpdates: false
Current position: 2  Length: 109  Bytes remaining: 107
```

To jest **blok `Values` dla gracza bez zmienionych pól** — pozycja 2 z 109 bajtów,
czyli parser czyta 2 bajty nagłówka i staje. Ten sam problem co w 70959, tylko
na mniejszym materiale.

## Wnioski

**1. Alias 71037 = 70959 jest poprawny.** Nie trzeba nic rozbierać osobno.

**2. Baseline dla 71037 trzeba zbudować od nowa** — 8 MB to za mało, żeby
cokolwiek porównywać. Potrzebny dłuższy sniff (30+ MB), z walką i stworami.

**3. Problemy są te same co w 70959** — czyli praca nad 5 krokami UnitData
(idąca w `UpdateFieldsHandler.cs`) ma sens także dla 71037.

## Następny krok

**Nagraj dłuższy sniff 71037** — najlepiej taki sam scenariusz jak 10-24
(latanie po strefie, walka, dużo stworów). Wtedy metryki będą porównywalne 1:1
i będzie można sprawdzić, czy 5 kroków naprawy daje te same efekty.
