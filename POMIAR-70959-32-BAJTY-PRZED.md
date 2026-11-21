# 70959 — 32 bajty przed `CreatureType`: przesunięcie POTWIERDZONE

Data: 2026-11-20. Sonda wycofana (`git checkout`). Sniff: `10-24`.

## Surowe bajty dla NPC `11835` (od pozycji `Sex` + 1)

```
+ 0: 00    + 1: 01    + 2: 00    + 3: 00
+ 4: 07    + 5: 01    + 6: 00    + 7: 00
+ 8: 00    + 9: 00    +10: 34    +11: 0E
+12..+23: same zera
```

Jako `u32`:

| offset | wartość | hex |
|---|---|---|
| +0 | 256 | `0x00000100` |
| +4 | 263 | `0x00000107` |
| +8 | 238 288 896 | `0x0E340000` |
| +12 | 0 | — |

## Co to znaczy

Parser czyta od +0:
- `CreatureType` = bajt +0 = **0**
- `DisplayPower` = bajt +1 = **1**
- `OverrideDisplayPowerID` = +2..+5 = `00 00 07 01` = **17 235 968** (`0x01070000`)
- `Health` = +6..+13 = `00 00 00 00 34 0E 00 00` = **`3636 << 32`**

A **prawdziwe wartości** leżą o **2 bajty dalej**:
- `CreatureType` = bajt +2 = **0** ✔ (zgadza się przypadkiem, bo oba to 0)
- `DisplayPower` = bajt +3 = **0**
- `OverrideDisplayPowerID` = +4..+7 = `07 01 00 00` = **263** — sensowna wartość!
- `Health` = +8..+15 = `00 00 34 0E 00 00 00 00` → **`3636 << 16`**? nie...

Stop. `Health` na +8 to `00 00 34 0E 00 00 00 00` = `0x0E340000` — to nadal nie 3636.

Ale `3636` = `0x0E34` siedzi na **+10..+11**. Czyli `Health` zaczyna się na **+10**.

## Wniosek: przesunięcie to 10 bajtów, nie 2 ani 4

```
Sex             1 B  (już przeczytane)
???             ? B
CreatureType    1 B  na +2
DisplayPower    1 B  na +3
OverridePowerID 4 B  na +4..+7   = 263  ✔ sensowne
???             2 B  na +8..+9
Health          8 B  na +10..+17  = 3636 ✔ sensowne
```

Przed `CreatureType` jest **2 bajty**, a między `OverrideDisplayPowerID` a `Health`
jest **2 bajty**. Razem 4 bajty, które parser zjada za mało — ale **rozłożone w dwóch miejscach**,
nie w jednym. Dlatego dodanie jednego `u32` przed `Health` psuło wszystko.

## Potwierdzenie z rozkładu wzorców

32 bajty przed `CreatureType` (pierwsze 4 bajty):

| wzorzec | ile |
|---|---|
| `00010002` | 1 022 |
| `00010000` | 799 |
| `00010001` | 336 |
| `00FF0000` | 177 |
| `00020001` | 154 |
| `FF000000` | 108 |

Dla `00010000` dalej idzie `07010000` + wartość (`1805`, `EF00`, `7A0F`, `D000`, `1001`).
Czyli **`0701` powtarza się zawsze** — to nie pole, to fragment stałej struktury.

## Poprawka do wdrożenia

W `ReadCreateUnitData` dla 70959:

```csharp
data.Sex = packet.ReadByte("Sex", indexes);
packet.ReadUInt16("UnknownAfterSex", indexes);        // 2 B przed CreatureType
data.CreatureType = packet.ReadByte("CreatureType", indexes);
data.DisplayPower = packet.ReadByte("DisplayPower", indexes);
data.OverrideDisplayPowerID = packet.ReadUInt32("OverrideDisplayPowerID", indexes);
packet.ReadUInt16("UnknownBeforeHealth", indexes);    // 2 B przed Health
data.Health = packet.ReadInt64("Health", indexes);
```

Oczekiwane: `Health` = 3636, `OverrideDisplayPowerID` = 263, `Power` nadal poprawne.

To jest pierwsza wersja z **podstawą w bajtach** i z poprawnym podziałem przesunięcia.
