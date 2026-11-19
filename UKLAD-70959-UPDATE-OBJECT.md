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

---

## AKTUALIZACJA po odpowiedzi deva (2026-11-18)

Dev potwierdzil uklad i wyjasnil moj blad. Kluczowa poprawka: **"7-bajtowy
naglowek" to naprawde 21 bitow flag (MSB first) + flush do bajtu (3 B)
+ u32 PauseTimesCount**. Nie 7 bajtow o nieznanym znaczeniu.

### Kolejnosc 21 bitow (MSB first)

HasEntityPosition, NoBirthAnim, EnablePortals, PlayHoverAnim, ThisIsYou,
MovementUpdate, MovementTransport, Stationary, CombatVictim, ServerTime, Vehicle,
AnimKit, Rotation, GameObject, SmoothPhasing, SceneObject, ActivePlayer,
Conversation, Room, Decor, MeshObject. Potem flush.

### Maski - potwierdzone na 267 pakietach create z jednym obiektem

| maska | bity (MSB first) | znaczenie | ile |
|---|---|---|---|
| `0x84` | 0, 5 | HasEntityPosition + MovementUpdate | 134 x Unit (objType 5) |
| `0x84` | 0, 5 | to samo | 87 x objType 6 |
| `0x81` | 0, 7 | HasEntityPosition + Stationary | 24 x objType 8 |
| `0x81` | 0, 7 | to samo | 5 x objType 9 |
| `0x8C` | 0, 4, 5 | + ThisIsYou | gracz (active player) |

Przy liczeniu LSB-first `0x84` wychodzi jako "bity 2 i 7" - taki blad popelnilem
w pierwszej wersji. Bity sa MSB first.

### "Powtorzony GUID" to MoverGUID

Nie jest to duplikat GUID-u obiektu - to `MoverGUID`, pierwsze pole bloku ruchu.
W probkach ma te sama wartosc co GUID obiektu, dlatego wygladal jak powtorka.

### `flags64` to MoveFlags

`u64` po MoverGUID to `MoveFlags`, nie osobne flagi bloku.

### Co jest po bloku pol

Dev: **nic**. `u32 Size` obejmuje wszystko do konca obiektu i nastepny obiekt
zaczyna sie zaraz za nim. Moje 98721 bajtow "nieznanego bloku" to nie osobny
blok - to reszta obiektu[0] (blok ruchu ze splajnem, sily ruchu, 9 predkosci,
17 floatow advFlying), ktorego jeszcze nie odkodowalem.

### Zastosowana zmiana w kodzie

`ReadMovementUpdateBlock` w V12_0_0_65390: pierwszy bit nazwany
`HasEntityPosition`, `ThisIsYou` z etykieta, `ResetBitReader()` przed
`PauseTimesCount` (21 bitow konczy sie w polowie bajtu).

Wynik: 70959 bez zmian (99,629%, 279 bledow, 563 wiersze creature),
70938 bez regresji (99,901%, 19 bledow, 1281 wierszy). Czyli kod jest teraz
zgodny z lista deva, ale liczby byly juz wczesniej dobre - naglowek bloku create
nie byl przyczyna pozostalych 279 bledow.

### Otwarte

Dev sam napisal, ze polozenia list w UnitData (PassiveSpells, WorldEffects,
ChannelObjects) to uklad ICH serwera, nie pomiar zywego realmu. To nie jest
zmierzone i nie zostalo uzyte.

### Rozklad 21 bitow w zywym snifie 70959 (pierwszy obiekt create)

| objType | bity (MSB first) | ile |
|---|---|---|
| 5 (Unit) | HasEntityPosition + MovementUpdate | 253 |
| 6 | HasEntityPosition + MovementUpdate | 168 |
| 8 (GameObject) | HasEntityPosition + Stationary + Rotation | 60 |
| 8 (GameObject) | + ServerTime + GameObject | 13 |
| 9 | HasEntityPosition + Stationary | 6 |
| 10 | HasEntityPosition + Stationary | 4 |
| 11 | HasEntityPosition + Stationary | 3 |
| 1 | (brak) | 19 |

Zero odchylek od listy deva.

### Pety i summony (hipoteza Karrza)

Karrz: wlasne pety/guardiany/totemy przychodza jako `0x84` / objType 5 tak jak
zwykly creature, ale niosa pola owner-only (dlatego ze odbiorca jest ich wlascicielem).

Zmierzone na snifie: Unit create z `MovementUpdate` = 15 (pakiety 1-obiektowe),
wszystkie maja `MoverGUID == wlasny GUID obiektu`. `MoveFlags`: `0x1` x12,
`0x0` x3. Nie znalazlem jednostek, ktore jednoznacznie wygladaja na wlasne
pety/summony - wiec ta hypoteza nie tlumaczy pozostalych 279 bledow w tym snifie.

### Serwer nie aktualizuje klienta od czasu powstania

Karrz: "No we didnt update, havent since making it, not to say it wont ever".
Ich serwer wysyla 70938 i 70959 identyczne bajty pol. Zgadza sie z pomiarem:
70938 schodzi do 0 bledow na tabelach 70425 z oboma remapami, a 70959 zachowuje
sie identycznie po stronie pol.

### Dlaczego dev nie da sniffu

Surowy capture zawiera ID kont i postaci, nicki innych graczy, czat i adresy
polaczen. Odmowa jest uzasadniona, a nasz wlasny sniff 70959 jest lepszym zrodlem.

## Blok ruchu rozpracowany do konca (pomiar na pkt 5278/5785/6071)

Metoda: w pakietach z JEDNYM obiektem create szukalem takiego `u32 size`, ze
`offset + 4 + size == dlugosc pakietu`. Trzy pakiety, ta sama jednostka:

| pakiet | u32 size @ | size | dlugosc |
|---|---|---|---|
| 5278 | 209 | 647 | 860 |
| 5785 | 310 | 647 | 961 |
| 6071 | 322 | 647 | 973 |

Ta sama wartosc 647 w trzech roznych pakietach - to rozmiar bloku pol dla tego
zestawu pol (Unit, objType 5).

### Uklad po GravityModifier (pkt 5278)

```
@ 94  9 bitow: HasStandingOn, HasTransport, HasFall, HasSpline,
      HeightChangeFailed, RemoteTimeValid, HasInertia, HasAdvFlying,
      HasDriveStatus.  Potem flush.
@ 96  9 predkosci (36 B): 2.5 walk, 8.0 run, 4.5 runBack, 4.7222 swim,
      2.5 swimBack, 7.0 flight, 4.5 flightBack, 3.1416 turn, 3.1416 pitch
@132  u32 MovementForcesCount = 0
@136  float MovementForcesModMagnitude = 1.0
@140  17 floatow advFlying (68 B): 2.0, 65.0, 1.0, 3.0, 10.0, 100.0, 1.5708,
      2.4435, 3.1416, 6.2832, 1.5708, 4.7124, 30.0, 80.0, 2.75, 7.0, 0.4
@208  1 bajt: bit HasSpline + flush
@209  u32 Size bloku pol (647), potem Size bajtow pol
```

Wszystkie wartosci sa sensowne (pi/2, pi, 2pi, limity predkosci), zadnych smieci.

### Co to zmienilo w kodzie

`ReadMovementUpdateBlock`: `ResetBitReader()` po 9 bitach flag, przed predkosciami.
Zmiana poprawna, ale **wynik sie nie zmienil** - `ReadSingle()` sam sie wyrownuje,
wiec parser czytal ten blok dobrze juz wczesniej.

| sniff | pakiety | bledy | creature |
|---|---|---|---|
| 70959 07:58 (nowy) | 38664 (99,416%) | 79 | 294 |
| 70959 18.11 | 127927 (99,629%) | 279 | 563 |
| 70938 | 492566 (99,901%) | 19 | 1281 |

### Wniosek

Blok ruchu nie jest przyczyna pozostalych bledow. Zostaje `UpdateFields`,
a dokladniej `UnitData` - i to jest zgodne z zastrzezeniem Karrza, ze jego
pozycje pol od 13 wzwyz to uklad ich serwera, nie pomiar zywego realmu.

### Nowe narzedzie

`tools/pair-map.py` - bierze pary `create` tego samego GUID z dwoch momentow
i liczy, na ktorych bajtach sie roznia. Na snifie 07:58 znalazl 30 takich par;
poza blokiem ruchu roznice zaczynaja sie dopiero @183 (blok pol).
