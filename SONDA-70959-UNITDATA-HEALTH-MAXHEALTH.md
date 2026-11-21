# 70959 — sonda: gdzie dokładnie parser gubi się w bloku wartości

Data: 2026-11-20. Cel: ustalić **pierwszą** pozycję, w której wartości przestają się zgadzać,
zamiast zgadywać pole. Kod parsera nietknięty do czasu zmierzenia.

## Punkt zaczepienia: Health gracza == MaxHealth

W bloku wartości gracza `[0]` (ActivePlayer, pkt 839):

| pole | wartość w logu | wartość prawdziwa |
|---|---|---|
| `Health` | `1984274890752` | ~kilkaset tysięcy |
| `MaxHealth` | `1984274890752` | ta sama co Health |
| `Level` | `0` | 1-60 |
| `EffectiveLevel` | `14` | — |

**Obie wartości są identyczne.** To znaczy, że parser czyta dwa razy te same 8 bajtów —
czyli od `Health` do `MaxHealth` zjada o **8 bajtów za mało**: przeskakuje nad
`Power[10] + MaxPower[10]` (80 B) i `PowerRegenFlatModifier` (80 B) w jednym ruchu.
To nie jest przesunięcie o 1 bajt (takie dałoby inną liczbę), tylko **pominięcie
całego stałego fragmentu** — czyli w 70959 brakuje w UnitData bloku pól
`Power`/`MaxPower` albo `PowerRegen*`.

Gdyby przesunięcie było wcześniej (na przykładzie guidów), `Health` i `MaxHealth`
nie byłyby identyczne — byłyby dwiema różnymi śmieciami.

## Hipoteza do sprawdzenia (jedna zmiana, jeden pomiar)

W `ReadCreateUnitData` (`UpdateFieldsHandler.cs:810`) blok
`for (i<10) Power/MaxPower` stoi **przed** `if (Owner|UnitAll) PowerRegen*`,
a `MaxHealth` po nim. W 70959 — skoro `Health == MaxHealth` — blok `Power/MaxPower`
najprawdopodobniej **nie jest wysyłany** (albo `PowerRegen*`). Do sprawdzenia sondą:
wypisać pozycję (`packet.Position`) na wejściu w `Health` i w `MaxHealth` i różnicę.

## Co jest już pewne (nie wymaga zgadywania)

- Nagłówek 70959 w sniffie: `[u32 count=66][u16 map][bit IsEndOfBatch][bit HasRemovedObjects][u32 DataSize]`
  — czytany poprawnie (log: `NumObjUpdates: 66`, `Data size: 121517`).
- Blok create czyta się poprawnie do `FieldFlags` i listy fragmentów
  (`CGObject, PlayerHouseInfoComponent_C, PlayerInitiativeComponent_C, Tag_Unit, Tag_Player`).
- `Scale: 1`, `DisplayID: 54041` w ObjectData są **sensowne** — czyli ObjectData czyta się dobrze.
- Pierwszy śmieć: `SummonedByHomeRealm: 4278190080` (0xFF000000), potem `Race: 0`,
  `Health: 1984274890752`. Czyli rozjazd zaczyna się **w UnitData, przed `Race`**.

## Kolejność pomiarów

1. Sonda: pozycje przed/po `Power/MaxPower`, `PowerRegen*`, `Health`, `MaxHealth` w `ReadCreateUnitData`.
2. Na tej podstawie: usunąć albo przenieść ten jeden blok dla 70959.
3. Zmierzyć: `Updatefields not fully read` (baseline **12483**), `Packet not fully read` (38),
   `InvalidDataException` (22). Sukces = spadek 12483.
