# 70959 - dlaczego `creature` mial 0 wierszy (i co jest teraz)

Data: 2026-11-18. Sniff: `dump_1.60.1.70959_2026-11-18_04-12-11.pkt` (128404 pakiety).

## Przyczyna glowna: DivideByZeroException w Spawns.Creature()

`WowPacketParser/SQL/Builders/Spawns.cs`, wiersz z liczeniem procentu zdrowia:

```csharp
if (creature.UnitData.Health > 1 && (creature.UnitData.Flags & IsInCombat) == 0)
    row.Data.CurHealthPct = (uint)(creature.UnitData.Health / creature.UnitData.MaxHealth * 100);
```

W 70959 pole `MaxHealth` wychodzilo **0** dla 28 jednostek (pole przesuniete w bloku
UnitData - patrz nizej), wiec dzielenie przez 0 rzucalo `DivideByZeroException`
w calym `Creature()`. Efekt: **caly builder przepadal** - nie tylko te 28 jednostek,
ale wszystkie 587. W logu parsera to bylo widac jako:

```
52/103 - Error: Failed writing Creature
System.DivideByZeroException: Attempted to divide by zero.
```

Naprawa: przy `MaxHealth <= 0` zostaje domyslne 100%.

## Przyczyna druga: zle czytany blok UnitData w 70959

Po `ModTimeRate` jest jeszcze jeden float (w sniffie 1.0), a `ClassicTrainingPoints*`
sa na koncu bloku, nie po `EmoteState`. Przez przesuniecie `CreatedBySpell` wychodzil
`0x3F800000` (czyli ten float 1.0), a `Unit.IsTemporarySpawn()` oznaczalo 545 z 587
jednostek jako tymczasowe.

Naprawa (WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs):
- 70959 czyta dodatkowy `float UnknownAfterModTimeRate` przed `CreatedBySpell`,
- 70959 nie czyta `ClassicTrainingPoints*` po `EmoteState` (sa czytane na koncu bloku).

## Przyczyna trzecia: NaN w ModelData

`ModelData()` dzieli przez `scale` i `displayScale`; przy zerach wychodzilo NaN/Infinity,
a `SQLUtil` formatowal float przez `string.Format("{0:F20}", v).Substring(0, 20)` -
dla NaN tekst jest krotszy niz 20 znakow i `Substring` rzucal
`ArgumentOutOfRangeException`, wiec przepadal caly builder `ModelData`.

Naprawa (WowPacketParser/SQL/SQLUtil.cs): NaN/Infinity -> `NULL`, `Substring` tylko gdy tekst dluzszy.

## Wynik

| | przed | po |
|---|---|---|
| pakiety sparsowane | 127898 (99.606%) | 127927 (99.629%) |
| bledy parsowania | 308 | 279 |
| wiersze `creature` w SQL | **0** | **563** |
| `creature_addon` | 0 | 3 bloki |

## Co dalej (do dewelopera)

1. `CreatedBySpell` nadal wychodzi `0x8000003F` dla 522 jednostek - to nie wyglada na
   prawdziwy spell ID. Pytanie do dewelopera: co 70959 zapisuje w `UNIT_CREATED_BY_SPELL`.
2. `MaxHealth` = 0 dla 28 jednostek, `Level` = 0 dla wszystkich - kolejne pola przesuniete.
3. 8 wierszy `creature` wciaz zakomentowanych jako "might be temporary spawn" (pety).
