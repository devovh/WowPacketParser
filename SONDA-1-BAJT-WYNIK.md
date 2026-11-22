# Bloki z 1 nieprzeczytanym bajtem — co to naprawdę jest

Data: 2026-11-21. Sniff: `dump_1.60.1.70959_2026-11-19_10-24-12.pkt`.

## Punkt wyjścia

129 229 z 173 747 ostrzeżeń to `Bytes remaining: 1`. Wyglądało jak jeden problem.
**Sonda pokazała, że to dwa różne, a większy nie jest tym, co myślałem.**

## Sonda

Dodałem sondę w `UpdateHandler.cs` (przed komunikatem o niedoczytaniu), wypisującą
przez `packet.AddValue`: długość bloku, pozycję, listę fragmentów, typ obiektu.

### Rozkład długości bloków z „zostaje 1 bajt"

| `Length` | ile |
|---|---|
| **3** | **128 833** |
| 7 | 327 |
| 8 | 56 |
| 14 | 4 |
| 34 | 3 |
| 46 | 2 |

**99,7% to bloki 3-bajtowe**, nie 7-bajtowe jak sądziłem z pierwszego przykładu.

### Co siedzi w blokach 3-bajtowych

```
128802 ×  pos=2  frags=CGObject  objType=null  hasFragUpd=False
    31 ×  pos=2  frags=CGObject,Tag_Unit  objType=Unit  hasFragUpd=False
```

Wszystkie: `CGObject`, **`HasFragmentUpdates = False`**, pozycja 2 z 3.

### Co siedzi w blokach 7-bajtowych

```
317 ×  pos=6  frags=CGObject,Tag_Item  objType=Item
  6 ×  pos=6  frags=CGObject,Tag_Unit  objType=null
  3 ×  pos=6  frags=CGObject,Tag_Unit  objType=Unit
```

To są bloki **itemowe** — 327 sztuk, nie 129 229.

## Diagnoza

**Blok 3-bajtowy:**

```
IsOwned            (1 B)
HasFragmentUpdates (1 B)
ObjectChangesMask  (1 B, 4 bity + FlushBits)
```

Parser czyta `IsOwned`, `HasFragmentUpdates`, a potem `ReadUpdateObjectData`, które
czyta **4 bity** maski i robi `ResetBitReader()`. **`ResetBitReader()` ustawia
`_bitpos = 8` i nic więcej** (`PacketReads.cs:616`) — **nie przesuwa pozycji**.
Więc 4 przeczytane bity nie zużywają bajtu, i zostaje 1 bajt.

**tc-forever** (`UpdateFields.cpp`, `ObjectData::WriteUpdate`):

```cpp
data.WriteBits(changesMask.GetBlock(0), 4);
data.FlushBits();
```

`FlushBits()` dopisuje resztę bajtu zerami — maska zajmuje **zawsze cały bajt**,
nawet gdy jest zerowa.

## Co zmierzyłem

**Próba: dosunięcie 1 bajtu po masce** (`if (unreadBits) SetPosition(+1)`):

| miara | baza | +dosunięcie |
|---|---|---|
| `Bytes remaining: 1` | 129 229 | **129 039** (−190) ✔ |
| `Updatefields` | 173 747 | **173 905** (+158) ✖ |
| `EndOfStream` | 969 | **1 014** (+45) ✖ |
| `creature` | 7 | 7 ✔ |

**Próba: dosunięcie tylko gdy maska == 0:**

| miara | baza | +maska zerowa |
|---|---|---|
| wszystko | — | **bez zmian** |

**Cofnięte.** Poprawka działa na 190 blokach, ale psuje 158 innych — bo gdy maska
**ma** bity, pola poniżej i tak zjadają te bajty, a ja dokładałem jeden za dużo.

## Co zostało w kodzie

**Jedna zmiana, zmierzona, bez regresji: `Tag_Item` w masce typów.**

`ReadForeverValuesTypeMask` nie mapował bitu 1 (`1u << 1`) na `Tag_Item`, więc bloki
itemowe w ogóle nie miały fragmentu `Tag_Item` i `ReadUpdateEntityFragmentDataForever`
nie miał czego czytać. Dodane:

```csharp
if ((typeMask & (1u << 1)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.Tag_Item));
...
case WowCSEntityFragments.Tag_Item:
    handler.ReadUpdateItemData(fieldsData, index);
    break;
```

**Wynik: bez zmian w metrykach** (173 747 / 187 / 969 / 7), ale bloki itemowe są
teraz czytane poprawnie zamiast być pomijane.

## Gdzie naprawdę jest te 128 833

To **nie jest błąd w bloku** — to `ObjectData` z **zerową maską zmian**. Serwer
wysyła bajt `0x00` na maskę, a parser go nie zjada.

Poprawne rozwiązanie: w `ReadUpdateObjectData` **zawsze** zużywać cały bajt maski
(4 bity + 4 bity dopełnienia), niezależnie od tego, czy maska jest zerowa.

Moja próba to robiła, ale **za szeroko** — dosuwała bajt także tam, gdzie pola
już go zjadły. Trzeba dosuwać **zawsze, bezwarunkowo**, a nie warunkowo:

```csharp
var changesMask = packet.ReadBits("ObjectChangesMask", 4);
packet.ResetBitReader();
// ZAWSZE: FlushBits po 4 bitach zajmuje caly bajt
```

Ale to wymaga sprawdzenia, czy `ReadBits(4)` nie zużywa bajtu sam z siebie —
`ResetBitReader` tylko ustawia `_bitpos = 8`, więc **bajt zostaje niezużyty
tylko wtedy, gdy `_bitpos` był < 8 przed resetem**. Dosunięcie musi być
bezwarunkowe, i to jest do zmierzenia osobno.

## Stan

| miara | wartość |
|---|---|
| `Updatefields` | 173 747 |
| `Packet not fully read` | 722 |
| `InvalidData` | 187 |
| `EndOfStream` | 969 |
| `creature` | 7 |

Kod: jedna zmiana (`Tag_Item`), zmierzona, bez regresji.
