# Maska `PlayerData` — brak remapu bitów (potwierdzone w kodzie)

Data: 2026-11-21. Sprawdzenie tropu z punktu 3 odpowiedzi dewelopera.

## Co powiedział deweloper

> „the mask uses the classic client's own bit numbers, not retail's: classic adds
> bits for its own fields (the two 17-byte structs and the surname, among others).
> So retail bit positions will drift after GuildLevel."

## Co znalazłem w kodzie

**Remap bitów istnieje, ale jest nieużywany dla `PlayerData`.**

`ForeverClassicBitRemap` ma **trzy** funkcje remapujące i **trzy** tablice grup:

| funkcja | tablica grup | używana? |
|---|---|---|
| `UnitData(uint bit)` | `UnitDataGroups70425` | ✔ tak (`ForeverUnitDataReader.cs:56`) |
| `PlayerData(uint bit)` | `PlayerDataGroups` | ✖ **NIGDZIE** |
| `ActivePlayerData(uint bit)` | `ActivePlayerDataGroups70425` | ? (do sprawdzenia) |

Sprawdzone:

```
grep -rn "ForeverClassicBitRemap.PlayerData" --include="*.cs" .
→ pusto
grep -rn "ForeverClassicBitRemap.PlayerDataGroups" --include="*.cs" .
→ pusto
```

**`PlayerDataGroups` i `PlayerData(uint bit)` leżą w pliku nieużywane.** Ktoś je
napisał — prawdopodobnie pod `UnitData` — ale nigdy nie podłączył do ścieżki gracza.

## Gdzie to boli

`ReadUpdatePlayerData70938` (linia 7179) czyta maskę **bez remapu**:

```csharp
var changesMask = Read70938ChangesMask(packet, 11);
```

A `Read70938ChangesMask` (linia 7034) to gołe czytanie bitów:

```csharp
var present = packet.ReadBits(blockCount);
for (var i = 0; i < blockCount; ++i)
    if ((present & (1UL << i)) != 0)
        blocks[i] = (int)packet.ReadBits(32);
return new BitArray(blocks);
```

**Zero konwersji z numeracji classic na retail.** Czyli `changesMask[3]` to bit 3
numeracji **classic**, a kod sprawdza go jako bit 3 numeracji **retail**.

To jest dokładnie ten „drift po `GuildLevel`", o którym mówi deweloper.

## Dlaczego to tłumaczy objawy

**Objaw 1 — 8 805 × „Current position: 2 Length: 109".**

Blok `Values` gracza: parser czyta 2 bajty nagłówka maski, potem sprawdza bity,
które **nie pasują do niczego**, więc żadne pole się nie odpala i zostaje 107
nieprzeczytanych bajtów. Nie wyjątek, tylko ciche zatrzymanie.

**Objaw 2 — `EndOfStreamException` rosnący przy naprawach.**

Skoro bity są przesunięte, część bloków **przypadkiem** odpala pola, których
serwer nie wysłał — i wtedy parser czyta poza koniec. Im „lepiej" ustawimy ogon
create, tym więcej bloków `Values` dojdzie do tego etapu.

## Co to znaczy dla 17-bajtowego structu

Moja poprzednia próba dodania 17 B structu **pogarszała** wynik, bo próbowałem
naprawić ogon create, podczas gdy **główny problem jest w masce `Values`**.
To wyjaśnia, dlaczego `EndOfStream` spadał, a `InvalidData` rósł — przesuwałem
bajty w złym miejscu.

## Następny krok (do zrobienia)

Podłączyć `ForeverClassicBitRemap.PlayerData` + `PlayerDataGroups` do
`ReadUpdatePlayerData70938`, tak jak zrobiono to dla `UnitData`:

```csharp
// zamiast:
var changesMask = Read70938ChangesMask(packet, 11);

// zrobić odczyt klasycznych bloków + remap, jak w ForeverUnitDataReader.cs:49-56
```

**To jest zmiana do zmierzenia osobno**, bo dotyka najczęstszej ścieżki w sniffie
(8 805 ostrzeżeń to największa pojedyncza liczba).

## Uwaga o `ActivePlayerData`

`ReadUpdateActivePlayerData70938` (linia 7632) ma **ten sam problem** — czyta maskę
bez remapu. Tam też jest `ActivePlayerDataGroups70425` nieużywana. Do sprawdzenia
razem z `PlayerData`, ale **osobnym pomiarem**.
