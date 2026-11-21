# 70959 — POMIAR: dwa `u16` w `ReadCreateUnitData` (wycofane)

Data: 2026-11-20. Zmiana **wycofana** (`git checkout`).

## Zmiana

```csharp
data.Sex = packet.ReadByte("Sex", indexes);
if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70959)
    packet.ReadUInt16("UnknownAfterSex", indexes);      // 2 B
data.CreatureType = packet.ReadByte("CreatureType", indexes);
data.DisplayPower = packet.ReadByte("DisplayPower", indexes);
data.OverrideDisplayPowerID = packet.ReadUInt32("OverrideDisplayPowerID", indexes);
if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70959)
    packet.ReadUInt16("UnknownBeforeHealth", indexes);  // 2 B
data.Health = packet.ReadInt64("Health", indexes);
```

## Wynik: pola create NAPRAWIONE

| pole | przed | po |
|---|---|---|
| `Health` sensowne | 174 | **1 860** |
| `MaxHealth` sensowne | 174 | **1 731** |
| `OverrideDisplayPowerID` (NPC 11835) | 17 235 968 | **263** ✔ |
| `Health` (NPC 11835) | 15 616 501 088 256 | **3 636** ✔ |

**Wszystkie trzy pola są teraz poprawne.** Bajty się zgadzają, wartości są sensowne.

## Ale creature spawns: 7 → 1

| miara | baza | po zmianie |
|---|---|---|
| `INSERT INTO creature` | **7** | **1** |
| `Updatefields not fully read` | 171 994 | 183 337 |
| `EndOfStreamException` | 820 | 1 376 |

Identycznie jak przy poprzedniej próbie (jeden `u32`).

## Co to znaczy

Poprawka **przesuwa cały blok o 4 bajty**, a `Health`/`MaxHealth`/`OverrideDisplayPowerID`
są teraz poprawne — czyli **te 4 bajty naprawdę tam są**.

Ale spawny spadają. Czyli **coś za `Health` czyta się teraz źle** — i to jest pole
potrzebne do spawnowania (pozycja, EntryID, DisplayID albo FactionTemplate).

To sugeruje, że przesunięcie **nie jest stałe**: te 4 bajty są w create
**tylko dla niektórych obiektów**, a nie dla wszystkich.

## Hipoteza do sprawdzenia

`UnknownAfterSex` i `UnknownBeforeHealth` mogą być zależne od `UpdateFieldFlag`:
- `UnitAll` (creature) — są,
- `Owner` (gracz) — nie ma,
- `None` — nie ma.

W sniffie: 3 970 bloków `UnitAll`, 72 `Owner`, 1 488 `None`.
Jeśli dodać te 4 bajty **tylko przy `UnitAll`**, to bloki `Owner`/`None` zostaną nietknięte
i spawny powinny wrócić.

## Stan kodu

Parser **czysty**. Baza: 7 creature spawns.
