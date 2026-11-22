# Przełom: 129 229 → 0 nieprzeczytanych bajtów

Data: 2026-11-21. Sniff: `dump_1.60.1.70959_2026-11-19_10-24-12.pkt`.

## Co było naprawdę nie tak

Nie chodziło o `Tag_Item` ani o itemy. Chodziło o **blok `Values` bez flagi
`HasFragmentUpdates`**.

### Struktura bloku (tc-forever)

```
IsOwned              (1 B)
HasFragmentUpdates   (1 B)
─── gdy HasFragmentUpdates == False ───
ObjectChangesMask    (1 B)   ← ObjectData::WriteUpdate: WriteBits(GetBlock(0), 4) + FlushBits
─── gdy HasFragmentUpdates == True ───
TypeMask             (4 B)   ← ReadForeverValuesTypeMask
+ dane typów
```

Parser w ogóle nie obsługiwał gałęzi `HasFragmentUpdates == False` — zostawiał
ten ostatni bajt maski `ObjectData`. **128 833 bloki, wszystkie 3-bajtowe.**

## Poprawka

W `WowPacketParserModule.V12_0_0_65390/Parsers/UpdateHandler.cs`, po gałęzi
`if (hasFragmentUpdates)`, dodana gałąź:

```csharp
else if ((ClientVersion.Build == ClientVersionBuild.V1_60_1_70959
          || ClientVersion.Build == ClientVersionBuild.V1_60_1_71037) && !hasFragmentUpdates)
{
    // Bez flagi fragmentow blok to IsOwned, HasFragmentUpdates i maska zmian
    // ObjectData (ObjectData::WriteUpdate: WriteBits(GetBlock(0), 4) + FlushBits,
    // czyli zawsze caly bajt).
    var objData = handler.ReadUpdateObjectData(fieldsData, i);
    if (obj is { ObjectData: IMutableObjectData mutObj })
        mutObj.UpdateData(objData);
    else if (obj != null)
        obj.ObjectData = objData;
    updateValues.Fields.UpdateData(objData);
}
else
    ReadUpdateEntityFragmentData(obj, fragments, updateValues, handler, fieldsData, i);
```

Do tego `Tag_Item` dopisany do `ReadForeverValuesTypeMask` (bit `1u << 1`) i jako
`case` w `ReadUpdateEntityFragmentDataForever` — 327 bloków itemowych.

## Pomiar

| miara | przed | po | zmiana |
|---|---|---|---|
| `Bytes remaining: 1` | 129 229 | **0** | **−100%** ✔ |
| `Updatefields not fully read` | 173 747 | **51 709** | **−70%** ✔ |
| `InvalidData` | 187 | 186 | −1 ✔ |
| `EndOfStream` | 969 | 953 | −16 ✔ |
| `creature` | 7 | **7** | bez zmian ✔ |

**Żadna metryka się nie pogorszyła.** Pierwszy raz w całej sesji zmiana obniżyła
`Updatefields` o trzycyfrowy procent bez ruszania `creature`.

## Co jeszcze zostało (51 709)

| zostaje bajtów | ile bloków |
|---|---|
| **5** | **8 288** |
| 12 | 1 045 |
| 11 | 478 |
| 10 | 253 |
| 9 | 519 |
| 4 | 18 |

**Największa grupa: 8 288 bloków z 5 nieprzeczytanymi bajtami** — to `UnitData`
w `WriteUpdate` (`Length: 647`). `ForeverUnitDataReader` czyta `UnitData::WriteUpdate`
i gubi 5 bajtów na końcu. To następny krok, warty osobnego pomiaru.

## Czego NIE udało się naprawić po drodze

**Gałąź `Forever` dla 70959 w całości** (`ReadUpdateEntityFragmentDataForever`):

| miara | wynik |
|---|---|
| `Bytes remaining: 1` | 3 ✔ |
| `Updatefields` | 29 362 ✔ |
| `EndOfStream` | **9 484** ✖ |
| `creature` | **6** ✖ |

Powód: gałąź `Forever` czyta `TypeMask` (u32) także wtedy, gdy `hasFragmentUpdates`
jest `False` — a wtedy blok ma tylko 1 bajt maski i zjada bajty następnego obiektu.
**Cofnięte**; `Forever` zostaje tylko przy `hasFragmentUpdates == True`.
