# 70959 — pomiar Power/MaxPower w UnitData: wynik i wycofanie

Data: 2026-11-20. Kod parsera **przywrócony do stanu wyjściowego** (`git checkout`), build czysty.

## Co wykazała sonda (przez `AddValue`, nie przez konsolę)

`AddValue` **działa** — wartości trafiają do logu parsowania. To naprawia wcześniejszy problem,
w którym `Console.WriteLine` nie było widać.

### Blok `CreateObject2` gracza `[0]`, UnitData

| punkt | pozycja |
|---|---|
| przed `Health` | 127 |
| po `Power`/`MaxPower` (20× int32) | 207 |
| po `PowerRegen*` (20× float) | 287 |
| po `MaxHealth` | 295 |

### Surowe bajty za `Health` (pozycja 127)

```
00 00 00 00 | 0D 02 00 00 | 0D 02 00 00 | 00 00 00 00 ...
```

Czytelnie: `Power[0] = 0`, `MaxPower[0] = 525` (`0x20D`), a potem **zera**.
Czyli **serwer nie wysyła 20 int32-ów `Power`/`MaxPower`** — wysyła jeden komplet.

## Dlaczego to jednak NIE jest poprawka

Po zmianie na „czytaj 2 int32 zamiast 20”:

| miara | przed | po zmianie |
|---|---|---|
| `Updatefields not fully read` | 12483 | **11018** (spadek) |
| `Packet not fully read` | 38 | 32 (spadek) |
| `InvalidDataException` | **22** | **64** (wzrost ×3) |

Spadek dwóch pierwszych wskaźników jest **pozorny**: mniej bajtów czytanych = mniej okazji,
żeby zostały nieprzeczytane. Ale prawdziwa miara — `InvalidDataException` — **potroiła się**.
Po zmianie gracz dostał `MaxHealth: 0`, `Level: 0`, `BaseHealth: 169984`, `MinItemLevel: 6400`
— czyli nowe śmieci zamiast starych. Poprawka **przesunęła** błąd, nie usunęła go.

**Wycofane.** `git checkout WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs`.

## Co z tego zostaje jako wiedza

1. **`AddValue` jest jedynym działającym sposobem sondowania** — `Console.WriteLine` ginie.
2. **Bajty za `Health` w CreateObject2 to `00 00 00 00 | 0D 02 00 00 | 0D 02 00 00 | zera`** —
   to jest twardy punkt odniesienia dla dalszej pracy.
3. **Rozkład 12483 błędów:** `Values` 10395, `Create` 623. Czyli 83% to bloki **update wartości**,
   nie tworzenie obiektów. Poprzednia praca skupiała się na złej części.
4. **`ReadUpdateUnitData` dla 70959 nie używa `ReadCreateUnitData`** — idzie przez
   `ForeverUnitDataReader` (maska zmian + bity pól). To inna ścieżka i to tam jest 10395 błędów.
5. **`IsForever` nie obejmuje 70959** (`UpdateHandler.cs:30-32`, zakres 70338–70938), ale
   `HasShortFragmentMask` **obejmuje** (`UpdateHandler.cs:37-39`). To niespójne — i to jest
   najbardziej podejrzane miejsce: fragmenty czytane są jak retail, a maska pól jak Forever.

## Następny krok (na podstawie punktu 5)

Sprawdzić, czy 70959 nie powinien iść w `ReadUpdateEntityFragmentDataForever` (ścieżka Forever
dla bloków `Values`), skoro jego `ReadUpdateUnitData` już idzie przez `ForeverUnitDataReader`.
Zmiana jest jednolinijkowa (`IsForever` obejmuje 70959), ale wcześniejsze notatki mówią,
że próba dała 439 błędów i 0 spawnów creature — trzeba to zmierzyć ponownie, bo tamten pomiar
był robiony razem z innymi zmianami.

## Stan wyjściowy (zweryfikowany po cofnięciu)

`Updatefields not fully read` **12483** · `Packet not fully read` **38** ·
`InvalidDataException` **22** · `EndOfStreamException` **56**
