# 70959 — co jest naprawione, a co nie (stan 2026-11-20)

Kod parsera: **bez zmian** względem commita `4bb24f14c`. Build czysty.
Pomiar na `dump_1.60.1.70959_2026-11-19_07-58-01.pkt`:

| miara | wartość |
|---|---|
| `Updatefields not fully read` | **12483** |
| `Packet not fully read` | 38 |
| `SMSG_UPDATE_OBJECT: InvalidDataException` | 22 |
| `EndOfStreamException` | 56 |
| `OverflowException` | 2 |

## Co działa (potwierdzone w logu)

- Nagłówek `SMSG_UPDATE_OBJECT`: `NumObjUpdates: 66`, `MapID`, `IsEndOfBatch`, `Data size: 121517` — **poprawnie**.
- Blok create do `FieldFlags` i listy fragmentów:
  `CGObject, PlayerHouseInfoComponent_C, PlayerInitiativeComponent_C, Tag_Unit, Tag_Player` — **poprawnie**.
- `ObjectData`: `Scale: 1`, `DisplayID: 54041` — **poprawnie**.
- Kolejność `SpellEmpowerStage` → `SummonedByHomeRealm` dla 70959 — **poprawnie**.
- Progi wejściowe `Health`:
  - gracz `[0]`: `pos=119` (7 guidów przed nim = 119 B → **maski guidów są 1-bajtowe, zmiana na `u16` NIE jest potrzebna**),
  - jednostki: `pos=114` (562 wystąpienia).

## Co jest zepsute (i gdzie)

`Health == MaxHealth == 1984274890752` w bloku gracza. Identyczne wartości oznaczają,
że parser czyta dwa razy **te same 8 bajtów** — czyli pomija stały fragment między nimi.
Między `Health` a `MaxHealth` reader czyta 160 B (`pos 127 → 287`). Skoro wynik jest ten sam,
prawdziwy blok ma **160 − 8 = 152 B**, więc w 70959 brakuje 8 bajtów:
najpewniej **jedno `u32` mniej w `Power`/`MaxPower`** albo jedno pole skalarne
przed `PowerRegen*`.

Czyli: rozjazd zaczyna się **w UnitData**, po `Health`, a nie na guidach i nie w nagłówku.

## Czego NIE da się tu zmierzyć (ważne)

`System.Console.WriteLine` z sondy **nie trafia do przechwytywanego wyjścia** procesu
(pojawia się tylko 2660 linii, bez kontekstu pakietu). Żeby zobaczyć wartości pól
w miejscu, trzeba pisać do pliku albo przez `packet.AddValue`, nie do konsoli.
To jest powód, dla którego poprzednie sondy „nic nie pokazywały”.

## Następny krok (jedna zmiana, jeden pomiar)

W `ReadCreateUnitData` dla 70959: policzyć dokładny rozmiar bloku, który reader zjada
między `Health` a `MaxHealth` (dziś 160 B) i porównać z tym, co wysyła serwer.
Najprościej: sondą przez `packet.AddValue("DBG_…")` (nie przez konsolę),
wypisać `Power[0]`, `MaxPower[0]`, `Power[9]`, `MaxPower[9]` — jeśli `MaxPower[9]`
jest śmieciem, brakuje `Power[10]`; jeśli nie, brakuje `PowerRegen*`.
