# 70959 — ROZSTRZYGNIĘCIE: gdzie naprawdę jest `Health`

Data: 2026-11-20. Wszystkie sondy wycofane. Sniff: `10-24`.

## Twarde dane z bajtów

Blok `[3]` (`EntryID 11835`), 22 oktety po 8 B za `OverrideDisplayPowerID`:

```
 1 | 00 00 00 00 34 0E 00 00   →  u32: 0 , 3636     ← 3636 = HP tego NPC
 2-4 | zera
 5 | 05 00 00 00 00 00 00 00   →  u32: 5
 6-7 | zera
 8 | 00 00 00 00 40 42 0F 00   →  u32: 0 , 1 000 000
 9 | 40 42 0F 00 00 00 00 00   →  u32: 1 000 000 , 0
10-21 | zera
22 | 00 00 00 00 34 0E 00 00   →  u32: 0 , 3636     ← ta sama wartość co oktet 1
```

Blok `[0]`: oktet 1 = `...B9010000` = 441, oktet 22 = to samo.

**Wartość z oktetu 1 powtarza się w oktecie 22.** To nie przypadek —
w 3 079 z 3 972 bloków (78%) pierwszy i ostatni oktet są identyczne.

## Co to znaczy

`3636` siedzi na pozycji **+4**, nie +0. `Health` jako `int64` czytany od +0 daje
`3636 << 32` = `15616501088256`. Dokładnie to widzimy w logu.

**Prawdziwe `Health` = 3636.** Wartość jest poprawna dla NPC 11835.

## Hipoteza, którą odrzucam po pomiarze

„Dodaj `u32` przed `Health`” — sprawdzone: `Health`/`MaxHealth` faktycznie się naprawiają
(174 → 1 860 sensownych), **ale creature spawns spadają 7 → 1**, a błędy rosną
(`EndOfStreamException` 820 → 1 376). Zmiana wycofana.

Powód: to pole nie jest stałe. W 325 z 3 332 bloków jest **niezerowe**, a wartości
(`458752`, `33882112`, `117440520`, `50462720`, …) to **niespójne śmieci** — nie pole.

## Właściwy wniosek

`OverrideDisplayPowerID` **nie kończy się tam, gdzie myśli parser**. Wartość `3636` jest
na +4 względem pozycji, od której parser startuje `Health`. Czyli **`OverrideDisplayPowerID`
jest czytane 4 bajty za wcześnie** — albo przed nim brakuje pola.

To by tłumaczyło wszystko:
- `Health` = `3636 << 32` (czytane od +0, wartość na +4),
- `MaxHealth` = to samo (na końcu też jest `3636`),
- `OverrideDisplayPowerID` w logu = `458752` = `0x00070000` — też podejrzane,
- `Power` wychodzi dobrze, bo po przesunięciu trafia na zera.

## Co jest warte sprawdzenia jako następne

Pola **przed** `OverrideDisplayPowerID`: `CreatureType`, `DisplayPower`.
Jeśli `DisplayPower` albo `CreatureType` jest w rzeczywistości 4-bajtowe (nie 1),
to przesunięcie startuje wcześniej i wszystko się zgadza **bez** dodawania nowego pola.

Blok `[0]` ma `CreatureType: 5`, `DisplayPower: 2`. Dla NPC 11835 typ powinien być
rozsądny — sprawdzenie tego w `creature_template` rozstrzygnie sprawę.

## Stan kodu

Parser **czysty** (`git checkout`), żadna zmiana nie jest wdrożona.
Baza: 7 creature spawns, `Updatefields not fully read` 171 994.
