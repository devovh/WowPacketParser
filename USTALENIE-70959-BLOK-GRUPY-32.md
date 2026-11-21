# 70959 — bloki z `UnitBlocksMask: 2` (grupa z `Health`): co się w nich dzieje

Data: 2026-11-20. Kod parsera nietknięty. Sniff bazowy: `10-24`.

## Rozkład `UnitBlocksMask` w sniffie 10-24

| maska | bloki | ile razy |
|---|---|---|
| 16 | blok 4 | 1 058 |
| **2** | **blok 1** (grupa `32..63` = `Health`/`MaxHealth`/`Level`) | **1 027** |
| 18 | bloki 1+4 | 587 |
| 1 | blok 0 | 400 |

Czyli bloki z grupą `Health` **są** — 1 027 + 587 = 1 614 razy.

## Co te bloki zawierają

`UnitBlock` w maskach `2` (bity wewnątrz bloku 1):

| wartość | bity | ile razy |
|---|---|---|
| 65 = `0x41` | 0, 6 | **645** |
| 65537 = `0x10001` | 0, 16 | 267 |
| 1048577 = `0x100001` | 0, 20 | 55 |
| 1048641 = `0x100041` | 0, 6, 20 | 34 |

**Bit 0 jest zapalony we wszystkich** — a w TC bit 0 bloku 1 to bit **32**, czyli **bit grupowy**
grupy `32..63`. To potwierdza, że `Health`/`MaxHealth` są w bloku, ale pod bitem grupowym,
nie pod bitami 38/39.

## Co parser z nich czyta

- blok z `UnitBlock: 65` → czyta **1 pole**: `Health: 854` (sensowna wartość!),
- blok z `UnitBlock: 65537` → czyta `CurrentAreaID: 85` i `Flags: 8`,
- 689 z 1 027 bloków czyta `Health`, **`MaxHealth` nie czyta ani razu**.

## Gdzie jest błąd (nowe, twarde ustalenie)

`MaxHealth` (bit 39) **nigdy** nie jest czytany, bo `Changed(39)` jest fałszywe —
a bit 39 w bloku 1 nie jest ustawiany. Ustawiony jest bit grupowy 32 (bit 0 bloku).

To znaczy: serwer **wysyła całą grupę** (bit grupowy + wszystkie pola), a reader sprawdza
bity poszczególnych pól, których w masce nie ma. Efekt: `Health` trafia przypadkiem
(bo bit 38 wypada inaczej), `MaxHealth` nie trafia nigdy.

## Drugi, osobny problem: bloki bez ani jednego fragmentu

548 bloków z maską `2` ma `pos=2, len=3, rem=1` — czyli **po `IsOwned`/`HasFragmentUpdates`
reader nie czyta nic**. To bloki bez `HasFragmentUpdates`, gdzie brakuje odczytu maski zmian
na poziomie obiektu (`ObjectChangesMask`).

## Co to znaczy dla naprawy

Poprawka nie polega na „poszerzeniu warunku” (to zmierzone i odrzucone). Polega na:

1. **Respektowaniu bitu grupowego**: gdy bit grupowy grupy `32..63` jest zapalony,
   czytać **wszystkie** pola tej grupy po kolei (`Health`, `MaxHealth`, `Level`,
   `EffectiveLevel`, `FactionTemplate`, …), a nie tylko te, których bity są ustawione.
2. **Osobno**: dodać odczyt `ObjectChangesMask` dla bloków z `HasFragmentUpdates: false`.

Punkt 1 wyjaśnia cały objaw `Health == MaxHealth` i `MaxHealth` nigdy nieczytany.
Punkt 2 wyjaśnia 548 bloków `rem=1`.
