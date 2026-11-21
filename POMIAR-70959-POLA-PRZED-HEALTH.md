# 70959 — `CreatureType` / `DisplayPower` / `OverrideDisplayPowerID`: co mówią dane

Data: 2026-11-20. Parser czysty.

## Szerokości pól — parser i tc-forever zgodne

```
parser:      ReadByte CreatureType, ReadByte DisplayPower, ReadUInt32 OverrideDisplayPowerID
tc-forever:  uint8   CreatureType, uint8   DisplayPower, uint32   OverrideDisplayPowerID
```

**Identycznie.** Więc hipoteza „`CreatureType` albo `DisplayPower` jest 4-bajtowe”
jest **odrzucona** — gdyby było, tc-forever też by się mylił, a bajty by się nie zgadzały
w 3 972 blokach tak regularnie.

## Rozkład wartości w sniffie

`CreatureType` (create, bloki Unit):

| wartość | ile |
|---|---|
| **0** | **2 949** |
| 5 | 344 |
| 2 | 203 |
| 8 | 166 |
| 255 | 108 |

`DisplayPower`:

| wartość | ile |
|---|---|
| **1** (Mana) | **2 274** |
| 2 (Rage) | 438 |
| 0 | 382 |
| 255 | 178 |

`OverrideDisplayPowerID`:

| wartość | hex | ile |
|---|---|---|
| 17 235 968 | `0x01070000` | 870 |
| 458 752 | `0x00070000` | 551 |
| 16 843 264 | `0x01010200` | 544 |
| 17 236 224 | `0x01070100` | 360 |
| 0 | — | 181 |

## Kluczowa obserwacja: `OverrideDisplayPowerID` ma zawsze 0 w dwóch dolnych bajtach

`0x01070000`, `0x00070000`, `0x01010200` — **dolne 16 bitów to prawie zawsze zero**
(poza `0x01010200`, gdzie dolne 8 bitów to 0, a bajt 1 to 0x02).

To znaczy, że `OverrideDisplayPowerID` **nie jest** zwykłym `u32` — albo jest
czytane z przesunięciem, albo jest w rzeczywistości polem o innym układzie
(np. `u16` + `u16`).

## Co to daje

`Health` jest na **+4** względem tego, co parser nazywa `OverrideDisplayPowerID`.
A `OverrideDisplayPowerID` ma **dolne 2 bajty zerowe** w większości bloków.

To razem znaczy: **`OverrideDisplayPowerID` jest o 2 bajty za wcześnie albo za późno**,
i to samo przesunięcie trafia `Health`.

## Następny pomiar (konkretny)

Wypisać **16 bajtów przed** `Health` (czyli od końca `Sex`) i zobaczyć, gdzie dokładnie
w tym oknie siedzą wartości `CreatureType` (0/5/2/8), `DisplayPower` (1/2/0)
i `OverrideDisplayPowerID`.

To jedyne miejsce, gdzie przesunięcie może się zaczynać, i jedyne, którego jeszcze
nie obejrzałem bajt po bajcie.
