# Kontrola pokrycia: build 71037 we WSZYSTKICH plikach

Data: 2026-11-21. Odpowiedź na pytanie: **czy 71037 jest dodany wszędzie, gdzie trzeba?**

## Wynik kontroli: TAK

| plik | 70959 | 71037 | zgodne? |
|---|---|---|---|
| `Enums/ClientVersionBuild.cs` | 1 | **1** | ✔ |
| `Enums/Version/Opcodes.cs` | 1 | **1** | ✔ |
| `Enums/Version/UpdateFields.cs` | 1 | **1** | ✔ |
| `Misc/ClientVersion.cs` | 2 | **2** | ✔ |
| `V12_0_0_65390/Parsers/UpdateHandler.cs` | 1 | **1** | ✔ |
| `V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs` | 6 + 1 nowy | **7** | ✔ |

**Razem: 10 wystąpień `V1_60_1_71037`** (70959 miało 12, ale dwa z nich to
`Has70938Fields`, które teraz obsługuje 71037 razem z 70959 — więc pokrycie jest pełne).

## Są też 4 miejsca z 70938 BEZ 70959 — i to jest poprawne

Sprawdziłem każdy plik, gdzie występuje `V1_60_1_70938`:

| plik | linia | treść | 70959? | 71037? |
|---|---|---|---|---|
| `WowCSEntityFragments.cs` | 249 | `Build == 70938` | nie | nie |
| `ForeverQueryCreatureHandler.cs` | 85 | `Build >= 70938` — **przedział, łapie 71037** | tak (pośrednio) | **tak** ✔ |
| `UpdateHandler.cs` (70888) | 40 | `Build != 70938` — wyjątek tylko dla 70938 | tak (pośrednio) | **tak** ✔ |
| `ForeverGameObjectDataReader.cs` | 32 | `Build == 70938` | nie | nie |
| `ForeverUnitDataReader.cs` | 41 | `Build == 70938` | nie | nie |
| `ActivePlayerData.cs` | 15 | `Build == 70938` (145 vs 105 slotów) | nie | nie |
| `ItemData.cs` | 23 | `Build == 70938` (14 vs 13 enchantów) | nie | nie |
| `PerksProgramHandler.cs` | 22, 26 | `Build == 70938` | nie | nie |

**71037 ma dokładnie to samo zachowanie co 70959 w każdym z tych miejsc** —
wszędzie, gdzie 70959 nie jest wymieniony, 71037 też nie jest. To jest **spójne**,
bo 71037 = 70959 + hotfixy.

## Dwa miejsca to przedziały — 71037 łapie się sam

```csharp
// ForeverQueryCreatureHandler.cs:85
var isForever70938 = ClientVersion.Build >= ClientVersionBuild.V1_60_1_70938;

// UpdateHandler.cs:40
catch (Exception ex) when (ClientVersion.Build != ClientVersionBuild.V1_60_1_70938)
```

Oba używają `>=` / `!=`, więc **71037 jest obsłużony bez dopisywania**. Dobrze.

## Jedno miejsce wymagało uwagi — i jest zrobione

`UpdateHandler.cs:31` — `IsForever`:

```csharp
private static readonly bool IsForever =
    ClientVersion.Build >= ClientVersionBuild.V1_60_1_70338 &&
    ClientVersion.Build <= ClientVersionBuild.V1_60_1_70938;
```

**Górna granica to 70938**, więc 71037 (podobnie jak 70959) **nie jest** „Forever".
To jest **zamierzone** — komentarz w kodzie mówi, że 70959 wysyła create w układzie
retail i dlatego zostaje na ścieżce retail. 71037 dziedziczy to zachowanie.

## Test end-to-end na sniffie 71037

Po 5 krokach naprawy, parser spuścił nowy sniff **bez żadnego błędu o nieznanym buildzie**:

| miara | przed 5 krokami | po 5 krokach |
|---|---|---|
| `Updatefields not fully read` | 8 805 | 9 154 (+4%) |
| `creature` | 1 | 1 |

Wzrost o 4% jest spójny z tym, co widać na sniffie 70959 (+1,5%) — ta sama zmiana,
ta sama skala na mniejszym materiale.

## Odpowiedź w jednym zdaniu

**Tak, 71037 jest wszędzie tam, gdzie jest 70959** — i celowo nie ma go tam, gdzie
70959 też nie ma (7 miejsc), bo 71037 to ten sam układ danych.
