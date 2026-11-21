# 70959 — rozstrzygnięcie: 176 bajtów jest POPRAWNE

Data: 2026-11-20. Sondy wycofane. Sniff: `10-24`, 3 972 bloków create `FieldFlags: 4 (UnitAll)`.

## Pomiar sondą pozycji (`packet.AddValue`)

| miara | wynik | ile bloków |
|---|---|---|
| `OverrideDisplayPowerID` → koniec `MaxHealth` | **176 B** | **3 972 (wszystkie)** |
| koniec `MaxHealth` → koniec `EffectiveLevel` | 8 B | 3 972 |
| koniec `EffectiveLevel` → koniec `FactionTemplate` | 21 B | 3 972 |

**176 B w każdym bloku** — układ stały, niezależny od obiektu.

## Rozkład

```
Health                    int64    8 B
Power[0..9]               int32   40 B
MaxPower[0..9]            int32   40 B
PowerRegenFlat[0..9]      float   40 B
PowerRegenInterr[0..9]    float   40 B
MaxHealth                 int64    8 B
                        ──────────────
                        razem:  176 B
```

8 + 80 + 80 + 8 = **176**. Dokładnie tyle pisze `UnitData::WriteCreate` w `forevercore`.

## `EnumFlag::HasFlag` — sprawdzone w źródle

`src/common/Utilities/EnumFlag.h:106`:

```cpp
constexpr bool HasFlag(T flag) const
{
    return static_cast<i>(_value & flag) != static_cast<i>(0);
}
```

To jest **„którakolwiek flaga”**, nie „wszystkie”. Czyli `HasFlag(Owner | UnitAll)` przy
samym `UnitAll` daje **prawdę** → serwer **pisze** `PowerRegen` przy `UnitAll`.

Więc warunek w `forevercore` jest poprawny, a parser (`!= UpdateFieldFlag.None`) też jest poprawny.

## Wniosek: długość i warunki się zgadzają

- liczba bajtów: **176 = 176** ✔
- `PowerRegen` przy `UnitAll`: **tak w obu** ✔
- kolejność pól: **ta sama** ✔

## Skąd więc `MaxHealth = 15616501088256`

`15616501088256 = 0x0E34_00000000`. Górne 32 bity niezerowe, dolne wyzerowane.

Dla `int64` czytanego z 8 kolejnych bajtów to znaczy: **trafiamy o 4 bajty obok** —
czytamy parę `[0, wartość]` zamiast `[wartość]`.

A `EffectiveLevel` wychodzi **poprawnie** (`50`), `Level` wychodzi `0`. Czyli przesunięcie
o 4 B **kumuluje się** w środku i **wraca** przed `EffectiveLevel`.

To jest sprzeczne z tym, że delta `MaxHealth → EffectiveLevel` wynosi dokładnie 8 B
(czyli tyle, ile `Level` + `EffectiveLevel`).

## Jedyna niesprawdzona rzecz: `Level`

`Level` = 0 w 28 095 przypadkach, niezerowy w 92 567. `EffectiveLevel` zawsze sensowny.
Delta 8 B na parę `Level`+`EffectiveLevel` jest **poprawna**.

Czyli przesunięcie nie jest przed `EffectiveLevel`. Jest **przed `MaxHealth`**.

## Co to znaczy konkretnie

`MaxHealth` czytany jest 8 B za wcześnie albo za późno **względem `Health`**.
Skoro `Health` też wychodzi źle (`Health == MaxHealth`), a `Power` wychodzi dobrze —
przesunięcie jest **między `Power`/`MaxPower` a `PowerRegen`**.

80 B `Power`/`MaxPower` + 80 B `PowerRegen` — jeśli serwer pisze **20 par `Power`**
(40 B) i **40 floatów `PowerRegen`** (160 B), to razem 200, nie 160.

Ale delta jest 176. Więc nie.

## Następny krok (jedyny sensowny)

Odczytać **rzeczywiste wartości** tych 176 B z jednego konkretnego bloku create —
surowe bajty, nie zinterpretowane. Wtedy zobaczymy, gdzie w środku jest granica
między `Power` a `PowerRegen`, i czy `Health`/`MaxHealth` mają sensowne wartości
pod innym offsetem.

Wymaga to sondy wypisującej bajty od pozycji `OverrideEnd` do `MaxHealthEnd`.
