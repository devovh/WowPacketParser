# 5 bajtów w UnitData — co się okazało

Data: 2026-11-21. Sniff: `dump_1.60.1.70959_2026-11-19_10-24-12.pkt`.

## Założenie było błędne

Myślałem, że 8 288 bloków z 5 nieprzeczytanymi bajtami to `UnitData` w `WriteUpdate`
(`Length: 647`). **Sonda pokazała, że `UnitData` jest czytane tylko 28 razy** —
a bloki z `left=5` mają `HasFragmentUpdates: false` i `TypeMask` **nie mają wcale**.

## Co naprawdę jest w bloku bez flagi fragmentów

Blok z `HasFragmentUpdates: false` i 647 bajtami zawiera **maskę `ObjectData`
i zaraz po niej `UnitData`** — tylko bez maski typów:

```
IsOwned              (1 B)
HasFragmentUpdates   (1 B)  = false
ObjectChangesMask    (1 B)  ← 4 bity + FlushBits
UnitData             (reszta)   ← bez maski typow, bo typ wynika z obiektu
```

Mój pierwszy branch czytał tylko `ObjectChangesMask` i kończył — dlatego z 1 bajtu
zrobiło się 5 (bajt maski zjedzony, ale `UnitData` dalej nieprzeczytane).

## Poprawka

Po `ReadUpdateObjectData` w gałęzi `!hasFragmentUpdates`, jeśli obiekt jest `Unit`
i w bloku zostały bajty:

```csharp
if (fieldsData.Position < fieldsData.Length)
{
    if (obj is Unit unitObj)
    {
        var unitData = handler.ReadUpdateUnitData(fieldsData, i);
        if (unitObj.UnitData is IMutableUnitData mutUnit)
            mutUnit.UpdateData(unitData);
        else
            unitObj.UnitData = unitData;
        updateValues.Fields.UpdateData(unitData);
    }
}
```

## Pomiar — czy warto

| miara | bez `UnitData` | z `UnitData` | |
|---|---|---|---|
| `Updatefields` | 51 709 | **47 921** | −3 788 ✔ |
| `Bytes remaining: 5` | 8 288 | **5 679** | −2 609 ✔ |
| `EndOfStream` | 953 | 1 231 | +278 ✖ |
| `InvalidData` | 186 | 186 | = |
| `creature` | 7 | **7** | = ✔ |

**Netto na plus: −3 788 `Updatefields` za +278 `EndOfStream`.** Zostawione.

## Sonda w `ForeverUnitDataReader` — wynik

`UnitData` jest czytane **28 razy**, i za każdym razem zostaje 12–37 bajtów:

| zostaje | ile |
|---|---|
| 14 | 17 |
| 18 | 9 |
| 37 | 1 |
| 12 | 1 |

To osobny problem: `UnitData::WriteUpdate` ma na końcu pola owner-only, których
`ForeverUnitDataReader` nie czyta. Próba odblokowania `ClassicBit(142/146/147)`
dla 70959 dała **zero zmian** — te bity nie są ustawione. Sonda cofnięta.

## 71037

Ta sama poprawka działa: `Bytes remaining: 1` = 0, `Updatefields` **9 154 → 4 403**
(−52%).

## Stan końcowy

| miara | 70959 | 71037 |
|---|---|---|
| `Updatefields` | 47 921 | 4 403 |
| `Bytes remaining: 1` | **0** | **0** |
| `InvalidData` | 186 | 24 |
| `EndOfStream` | 1 231 | 98 |
| `creature` | **7** | 1 |

## Co zostaje (5 679 bloków z 5 bajtami)

To nadal `UnitData` po `ObjectData`, ale `ForeverUnitDataReader` czyta je nie do końca
— 5 bajtów to prawdopodobnie końcówka `UnitData::WriteUpdate` (`Field_314` bit,
`AssistActionData` bit, `FlushBits`) plus coś, czego parser nie zna.
Następny krok: sonda na surowych bajtach **w tych konkretnych blokach**
(`Position` przed `ReadUpdateUnitData` i po nim), nie w `ForeverUnitDataReader`.
