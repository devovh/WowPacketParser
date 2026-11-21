# 70959 — surowe bajty 176 B za `OverrideDisplayPowerID` (sonda hex)

Data: 2026-11-20. Sonda wycofana (`git checkout`). Sniff: `10-24`, 3 972 bloki create.

## Blok `[0]` — pełne 176 B, podzielone na 22 oktety po 8 B

```
 1 | 00 00 00 00 B9 01 00 00    →  u32: 0, 441
 2 | 00 00 00 00 00 02 00 00    →  u32: 0, 512
 3 | 00 02 00 00 00 00 00 00    →  u32: 512, 0
 4 | 00 00 00 00 00 00 00 00
 5 | 00 00 00 00 00 00 00 00
 6 | 00 00 00 00 00 00 00 00
 7 | 00 00 00 00 00 00 00 00
 8 | 00 00 00 00 00 00 00 00
 9 | 00 00 00 00 00 00 00 00
10 | 00 00 00 00 00 00 00 00
11 | 00 00 00 00 00 00 00 00
12 | 00 00 00 00 00 00 00 18 41   →  float 9.5
13 | 00 00 00 00 00 00 00 00
...
20 | 00 00 00 00 B9 01 00 00    →  to samo co oktet 1
```

**Blok `[0]` jest symetryczny: oktet 1 == oktet 22.** Ten sam wzorzec występuje
w **3 079 z 3 972** bloków (78%).

## To wyjaśnia `MaxHealth = 1894080577536`

`1894080577536 = 0x01B9_00000000`. To **oktet 1** czytany jako `int64` LE:
bajty `00 00 00 00 B9 01 00 00` → dolne 32 bity = 0, górne = 0x01B9 = 441.
Czyli **441 << 32**.

Serwer pisze `441` jako `int32` na pozycji +4 oktetu 1, a parser czyta `int64`
od pozycji 0 — dostaje `441 << 32`.

## Co widzimy w bajtach

| pozycja | zawartość |
|---|---|
| +0..+3 | 0 |
| +4..+7 | **441** (int32) |
| +8..+11 | 0 |
| +12..+15 | **512** (int32) |
| +16..+19 | 512 (to samo, przesunięte) |
| +20..+83 | zera |
| +84..+91 | **9.5** (float) |
| +92..+159 | zera |
| +160..+163 | 0 |
| +164..+167 | **441** — znowu ta sama wartość |

## Wniosek: `Health` NIE jest na pozycji 0

Skoro `441` siedzi na +4, a nie na +0, to **`Health` nie zaczyna się tam, gdzie myśli parser**.
Przed `Health` jest jeszcze **4 bajty** (jakieś pole `u32`), które tc-forever pomija.

To wyjaśnia **wszystko naraz**:
- `Health` = `441 << 32` zamiast `441`,
- `MaxHealth` = to samo (bo na końcu też jest `441`),
- `Power` wychodzi dobrze, bo po 4-bajtowym przesunięciu trafia na zera,
- `EffectiveLevel` wychodzi dobrze **przez przypadek** (przesunięcie się kumuluje i wraca).

## Poprawka do sprawdzenia

W `ReadCreateUnitData` przed `Health` wstawić dla 70959 odczyt `u32` (roboczo `UnknownBeforeHealth`)
i zmierzyć, czy `Health`/`MaxHealth` wychodzą sensowne.

Oczekiwane: `Health` ≈ 441, `MaxHealth` ≈ 441, `Power` nadal poprawne.

To jest jedna zmiana, mierzalna, i pierwsza, która ma podstawę w bajtach.
