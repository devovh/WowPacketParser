# 70959 - uklad SMSG_UPDATE_OBJECT zmierzony na bajtach

Zmierzone na `dump_1.60.1.70959_2026-11-18_04-12-11.pkt`.
Metoda: szukanie w bajtach miejsca, gdzie leza 3 floaty w zakresie wspolrzednych
(400-6000) + orientacja (-7..7). Pozycji nie da sie pomylic z innym polem, wiec
sluzy jako kotwica ukladu. Nie zgadywane - kazda liczba ponizej sprawdzona.

## Naglowek pakietu (bez zmian wzgledem 12.x)

| Offset | Pole |
|---|---|
| 0 | u16 MapID |
| 2 | u32 NumObjUpdates |
| bity | IsEndOfBatch, HasRemovedObjects |
| - | (jesli HasRemovedObjects) destroyed/removed + guidy |
| 7 | u32 Data size |

Dla pkt 837: map=0, count=47, datasize=115236, len=115247 -> `datasize` zgadza sie
co do bajta (115247 - 11 = 115236).

## Naglowek obiektu (bez zmian)

| Offset | Pole |
|---|---|
| +0 | u8 UpdateType (0=Values, 1/2=Create) |
| +1 | GUID 12 B packed (lo_mask, hi_mask, bajty) |

## Blok tworzenia (Create) - TU JEST ROZNICA

Po GUID obiektu i po bajcie ObjectType:

```
@+0   u8  ObjectType           (5=Unit, 8=GameObject, 9=AreaTrigger)
@+1   7 B naglowek bloku       0x84 00 00 00 00 00 00   (Unit)
                               0x81 00 00 00 00 00 00   (GameObject/AreaTrigger)
```

Maska naglowka (pierwszy bajt):
- bit 7 (0x80) - blok ma pozycje (zawsze ustawiony w zebranych probkach)
- bit 2 (0x04) - blok ma powtorzony GUID (tylko Unit)

### Gdy maska ma bit 2 (Unit)

```
@+8   GUID 12 B packed        <- DOKLADNIE te same bajty co GUID w naglowku obiektu
@+20  u64 flags64             (w probkach 1, 40, 288)
@+24  u32 MoveTime            rosnie monotonicznie (patrz nizej)
@+28  float X, Y, Z           (3 x 4 B)
@+40  float Orientation
@+44  float Pitch
@+48  float StepUpStartElevation
@+52  i32 RemoveForcesIDsCount
@+56  i32 MoveIndex
@+60  float GravityModifier   (w probkach 1.0)
```

Razem 31 bajtow od ObjectType do pozycji - zgadza sie z pomiarem.

### Gdy maska nie ma bitu 2 (GameObject, AreaTrigger)

7 bajtow naglowka, potem od razu pozycja (bez GUID i flag).

## Dowody

### MoveTime rosnie monotonicznie

| pakiet | MoveTime | delta |
|---|---|---|
| 48758 | 3970547540 | 0 ms |
| 48837 | 3970552826 | +5,3 s |
| 55056 | 3970748990 | +201 s |
| 122228 | 3972783427 | +2236 s |

Przyrosty zgadzaja sie z odstepami numerow pakietow.

### Pozycje sa w zakresie stref

20 pakietow z pozycja na stalej odleglosci od ObjectType daje wspolrzedne
480-2900 - czyli strefy startowe. Przy zlym offsetcie wychodza wartosci rzedu
1e27 albo 5 mln.

### Floaty po GravityModifier

@96+: 2.5, 8.0, 4.5, 4.72, 2.5, 7.0, 4.5, 3.14, 3.14, 0, 1.0, 2.0, 65.0, 1.0,
3.0, 10.0, 100.0 - wszystkie sensowne, zadnych smieci.

## Dlaczego obecny kod sie wywala

`ReadMovementUpdateBlock` z 12.x czyta 21 bitow flag + `bits360` (21 bitow) +
`PauseTimesCount` (u32). W 70959 po ObjectType jest od razu 7-bajtowy naglowek,
wiec `PauseTimesCount` wychodzi 2147519495 i caly blok sie rozjezdza.

## Co jeszcze zostalo

- Nie sprawdzono wariantow z `hasStationaryPosition`, transportem i splajnem -
  te bloki dokladaja dane po pozycji i trzeba je zmierzyc osobno.
- Uklad dla pozostalych ObjectType (8, 9) jest tylko zgrubny (7 B + pozycja).

## Proba uzycia tego ukladu - NIEUDANA (2026-11-18)

Probowalem wpisac ten uklad jako galaz `if (build == 70959)` w
`ReadMovementUpdateBlock` (V12_0_0_65390). Wynik byl gorszy:

| | przed | po |
|---|---|---|
| pakiety | 127927 (99,629%) | 127279 (99,124%) |
| bledy | 279 | 927 |
| wiersze `creature` | 563 | 0 |

Zmiana zostala cofnieta (`git checkout`).

### Dlaczego nie wyszlo

1. Uklad zmierzony jest poprawny tylko dla pakietow `type=2` z JEDNYM obiektem
   (20 probek). Duzych pakietow (np. 837: 47 obiektow) nie zmierzono.
2. Galaz konczyla sie `return moveInfo` zaraz po odczycie ruchu, wiec **nie czytala
   UpdateFields** - bloku pol, z ktorego bierze sie EntryID. Dlatego `creature`
   spadlo do 0.
3. Nie sprawdzono, jak wyglada blok Create, gdy po ruchu ida jeszcze
   `hasStationaryPosition`, transport, splajn.

### Co trzeba zmierzyc przed kolejna proba

- Pelny blok Create dla `type=2` z wieloma obiektami (znacznik konca bloku).
- Co dokladnie idzie po polach ruchu (UpdateFields) i jak dlugi jest ten blok.
- Warianty z pozycja stacjonarna, transportem i splajnem.

## Format obiektu type=0 (VALUES) - ZMIERZONY I PEWNY

Potwierdzony na trzech pakietach, zgodnosc co do bajta:

| pakiet | u32 za GUID | reszta pakietu |
|---|---|---|
| 1206 | 21 | 21 |
| 890 | 38 | 38 |
| 916 | 30 | 30 |

Uklad: `u8 type(0)`, `GUID packed`, `u32 rozmiar`, `dane[rozmiar]`.

## OTWARTE PYTANIE - co jest po UpdateFields w obiekcie Create

W pakiecie 837 obiekt[0] konczy sie tak:

```
@66..86  pitch, stepUp, RemoveForcesCount, MoveIndex, GravityModifier (20 B)
@86      u32 = 32                  <- rozmiar bloku pol (analogicznie do type=0)
@90..122 UpdateFields (32 B)
```

Ale pod `@122` NIE ma poczatku obiektu[1]. Nastepny GUID-maska w calym pakiecie
jest dopiero pod `@98843`, czyli miedzy @122 a @98843 lezy 98721 bajtow
nieznanego bloku.

Podejrzenia (NIE potwierdzone): MovementUpdate ze splajnem, lista AuraUpdate,
bloki addonowe.

Nie udalo sie tego rozstrzygnac bez zrodla serwera albo bez sniffu, w ktorym
ten sam obiekt wystepuje w dwoch momentach (zeby porownac, co sie zmienia).

## Wniosek

Blok ruchu i format type=0 sa zmierzone i pewne. Blok UpdateFields w Create
jest zmierzony co do rozmiaru, ale nie wiadomo, co po nim idzie - i to jest
jedyne, co blokuje pelne przejscie pakietu 837.
