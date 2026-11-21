# 70959 — POMIAR: `u32` przed `Health` w `ReadCreateUnitData`

Data: 2026-11-20. Zmiana **wycofana** (`git checkout`).

## Zmiana

`UpdateFieldsHandler.cs:866`, w `ReadCreateUnitData`:

```csharp
data.OverrideDisplayPowerID = packet.ReadUInt32("OverrideDisplayPowerID", indexes);
if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70959)
    packet.ReadUInt32("UnknownBeforeHealth", indexes);   // ← dodane
data.Health = packet.ReadInt64("Health", indexes);
```

## Wynik: `Health`/`MaxHealth` NAPRAWIONE

| miara | przed | po |
|---|---|---|
| `Health` sensowne (0 < v < 10 mln) | 174 | **1 860** |
| `Health` śmieci | 3 972 | **374** |
| `MaxHealth` sensowne | 174 | **1 731** |
| `MaxHealth` śmieci | 3 972 | **325** |

Przykłady z create:

```
EntryID: 11835   Health: 3636    MaxHealth: 3636
EntryID: 271613  Health: 1983    MaxHealth: 1983
EntryID: 11750   Health: 156     MaxHealth: 156
```

Wartości są **sensowne dla NPC** (setki–tysiące HP). Hipoteza z bajtów była trafna:
`441` siedzi na +4, więc przed `Health` jest dodatkowy `u32`.

## Ale: błędy wzrosły

| miara | baza | po zmianie |
|---|---|---|
| `Updatefields not fully read` | 171 994 | **183 337** (+6,6%) |
| `Packet not fully read` | 719 | **1 182** |
| `InvalidDataException` | 346 | **496** |
| `EndOfStreamException` | 820 | **1 376** |

## I: creature spawns SPADŁY

| miara | baza | po zmianie |
|---|---|---|
| `INSERT INTO creature` | **7** | **1** |

To jest metryka, która się liczy. Spadek 7 → 1 oznacza, że przesunięcie o 4 B
naprawia `Health`, ale **psuje wszystko za nim** — w tym pola potrzebne do spawnowania.

## Wniosek

Hipoteza jest **prawdziwa** (`Health`/`MaxHealth` faktycznie są o 4 B dalej),
ale **niepełna**: nie wystarczy dodać `u32`. Trzeba też ustalić, co to pole znaczy,
i czy nie ma go też w innych miejscach bloku (skoro psuje pola za sobą).

## Co dalej

1. Sprawdzić, czy `UnknownBeforeHealth` ma sensowną wartość (np. 0, 512, 441) —
   jeśli tak, to jest to prawdziwe pole do dodania **do `forevercore`**.
2. Sprawdzić, czy taki sam `u32` nie występuje przed innymi polami 64-bitowymi
   (`MaxHealth` też jest `int64` — może też ma przesunięcie).
3. Zmierzyć creature spawns po każdej zmianie — to jedyna metryka, która nie kłamie.
