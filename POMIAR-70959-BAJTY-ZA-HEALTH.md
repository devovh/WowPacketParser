# 70959 — rozkład bajtów za `Health` (twardy pomiar)

Data: 2026-11-20. Kod parsera nietknięty (sonda przez `AddValue`, potem `git checkout`).

## Surowe 176 bajtów za `Health` (pozycja 127, CreateObject2, gracz `[0]`)

Rozbite na grupy po 4 B (little-endian, wartość w nawiasie):

| offset | bajty | wartość | interpretacja |
|---|---|---|---|
| +0 | `00 00 00 00` | 0 | `Power[0]` |
| +4 | `0D 02 00 00` | **525** | `MaxPower[0]` |
| +8 | `0D 02 00 00` | **525** | powtórzone `MaxPower` |
| +12 | `00 00 00 00` | 0 | |
| +16 … +76 | 17 × zer | 0 | |
| +80 | `00 00 18 41` | **9.5f** | `PowerRegenInterruptedFlatModifier[0]` |
| +84 … +160 | 19 × zer | 0 | |
| +164 | `CE 01 00 00` | **462** | ItemID / wygląd |
| +168 | `00 00 00 00` | 0 | |
| +172 | `0E 00 00 00` | **14** | `EffectiveLevel` |

Pełny hex (176 B):
```
000000000D0200000D020000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000001841000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
00CE010000000000000E000000
```

## Co z tego wynika

- Blok **nie jest** 160 B `Power/MaxPower` + 80 B `PowerRegen*`. To ~80 B zer,
  jeden float `9.5`, znowu ~76 B zer, `462`, `14`.
- To **nie pasuje** do żadnego z dwóch czytników w repo:
  - `ReadCreateUnitData` czyta 20× int32 + 20× float,
  - `ForeverUnitDataReader` czyta maski zmian i bity pól.
- Kontrola pozycji: po zmianie sondy `MaxHealth` wypada nadal na `pos=295`, a `EffectiveLevel: 14`
  (poprawna wartość dla tej postaci) wychodzi **przez przypadek** — reader czyta `14` jako
  `EffectiveLevel`, bo trafia w to samo miejsce z przesunięciem.

## Czego NIE wiadomo

Nie wiadomo, **gdzie w tym bloku jest `MaxHealth`**. Wartość `1984274890752` (`0x1CE00000000`)
to `462 << 32` — czyli `MaxHealth` czyta się z bajtów `CE 01 00 00 00 00 00 00`, które
w rzeczywistości są ItemID-em `462` i zerami. To dowód, że `MaxHealth` jest czytany
**z niewłaściwego miejsca**, a nie że serwer wysyła zły `MaxHealth`.

## Następny krok (właściwy, bez zgadywania)

Wziąć tc-forever `UnitData::WriteCreate` / `WriteUpdate` z `UpdateFields.cpp` i porównać
kolejność pól z tymi 176 bajtami — pole po polu, od `Health` w górę. To jedyny sposób,
żeby ustalić, ile pól i jakiej szerokości stoi między `Health` a `MaxHealth` w 70959.

Poprzednie podejścia (maska guidu, nagłówek, ścieżka Forever, liczba par Power) są
**zmierzone i odrzucone** — nie ma sensu ich powtarzać.
