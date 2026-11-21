# 70959 — pomiar grupy 32 w `ForeverUnitDataReader` (odrzucone) + baza na 10-24

Data: 2026-11-20. Kod parsera **czysty** (`git checkout`), build OK.

## Co zmieniłem

`ForeverUnitDataReader.cs:148` — warunek grupy 32:

```csharp
// przed
if (is70938 || Changed(32))
// po
if (is70938 || ClientVersion.Build == V1_60_1_70959 || Changed(32))
```

Powód: tc-forever `WriteUpdate` trzyma `Health` (38), `MaxHealth` (39), `Level` (40)
w grupie `32..63`, a bit grupowy bywa niezapalony.

## Wynik

| miara | baza | po zmianie |
|---|---|---|
| `Updatefields not fully read` | 171 994 | 171 994 (bez zmian) |
| `Packet not fully read` | 719 | **820** |
| `InvalidDataException` | 346 | **346** (bez zmian) |
| `EndOfStreamException` | 820 | **820** (bez zmian) |
| creature w SQL | **7** | **7** |

**Zmiana nie wpłynęła na nic** — poza tym, że przybyło 101 `Packet not fully read`.
To znaczy, że ten warunek **nie jest wykonywany** dla 70959 albo gałąź nie ma znaczenia
dla tego, co widzę w logu. Wycofane.

## Dlaczego nie zadziałało — nowe ustalenie

Zajrzałem w konkretny blok `Values` jednostki w logu:

```
[12] HasFragmentUpdates: false
ObjectChangesMask: 1
UnitBlocksMask: 16
[12] [4] UnitBlock: 50331648
[12] Unknown70291: False
[12] [0] Power: 1240
```

`UnitBlocksMask: 16` = blok 4 → grupa TC `128..151`. To **inna grupa** niż 32..63,
w której siedzą `Health`/`MaxHealth`. Czyli `ForeverUnitDataReader` w ogóle nie wchodzi
w grupę 32 dla tych bloków — i dlatego zmiana warunku nic nie dała.

Blok czyta się poprawnie: `Power: 1240` to sensowna wartość.

## Nowa baza pomiarowa: `10-24` (60 MB, 162 create, 2 426 creature)

| miara | wartość |
|---|---|
| `Updatefields not fully read` | 171 994 |
| `Packet not fully read` | 719 |
| `InvalidDataException` | 346 |
| `EndOfStreamException` | 820 |
| **creature w SQL** | **7** |

To jest właściwy punkt odniesienia — wcześniejsze pomiary na `07-58` (15 create)
były robione na zbyt małym materiale.

## Wniosek

Teza z `POROWNANIE-70959-WRITEUPDATE-TC.md` („błąd w warunku grupy 32”) jest
**obalona pomiarem**. Nagłówek maski i remap bitów są poprawne, ale problem nie leży
w warunkach grup — leży w tym, **które grupy blok faktycznie zawiera** i co reader
z nimi robi po odczycie.

Następny krok musi być inny: sprawdzić blok, w którym `UnitBlocksMask` ma **tylko blok 1**
(czyli grupę z `Health`), i zobaczyć, czy w logu w ogóle pojawiają się tam `Health`/`MaxHealth`.
