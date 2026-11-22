# Remap bitów PlayerData — zmierzony, bez efektu

Data: 2026-11-21.

## Co zrobiłem

Napisałem `ReadPlayerDataChangesMaskRemapped` — ten sam mechanizm co
`ForeverUnitDataReader` (11 bloków MSB-first + `ToRetailBits` z
`ForeverClassicBitRemap.PlayerData` i `PlayerDataGroups`), i podłączyłem go
w `ReadUpdatePlayerData70938`.

## Wynik: ZERO zmian

| miara | przed | po remapie |
|---|---|---|
| `Updatefields not fully read` | 173 747 | **173 747** |
| `Packet not fully read` | 722 | **722** |
| `InvalidDataException` | 187 | **187** |
| `EndOfStreamException` | 969 | **969** |
| `creature` | 7 | **7** |

**Co do jednej liczby.** Kod się skompilował, więc ścieżka się wykonała — ale
**70959 tam nie wchodzi**.

## Dlaczego — i to jest ważne odkrycie

`ReadUpdatePlayerData` (linia 1712) rozgałęzia się tak:

```csharp
public override IPlayerData ReadUpdatePlayerData(Packet packet, params object[] indexes)
{
    if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
        return ReadUpdatePlayerData70938(packet, indexes);

    // Forever 1.60.1 (70338-70938) — PlayerData::WriteUpdate z tc-forever
    // ...
    const int BlockCount = 11;
    var blocksMask = packet.ReadBits("PlayerBlocksMask", BlockCount);
    ...
    bool Changed(int bit) => (blocks[bit / 32] & (1u << (bit % 32))) != 0;
    var classicGroup32 = (blocks[1] & 1) != 0;
```

**70959 i 71037 idą do DRUGIEJ gałęzi** — tej napisanej specjalnie pod Forever,
która **już czyta bloki jako `uint` i używa `bit % 32` wprost**. Czyli:

- czyta 11-bitową maskę bloków ✔
- czyta bloki jako `ReadUInt32` (LE, nie MSB-first) — **inaczej niż UnitData**
- `Changed(bit)` sprawdza `blocks[bit / 32] & (1u << (bit % 32))`

Ta ścieżka **nie ma remapu**, ale też **nie miała go dostać** — bo to inna
implementacja, pisana od zera pod Forever.

## Co to znaczy

**`ReadUpdatePlayerData70938` nie jest używane przez 70959.** Moja zmiana dotknęła
martwego kodu. Cofnięta.

Prawdziwa ścieżka to ta z linii 1720+. Żeby ją naprawić, trzeba zmienić **ją**,
a nie `70938`.

## Gdzie naprawdę jest problem — z dumpu

Rozkład ostrzeżeń pokazuje **dwa różne problemy**, nie jeden:

**Problem A — 129 229 × `Bytes remaining: 1`** (to jest 75% wszystkich ostrzeżeń):

```
[0] HasFragmentUpdates: true
[0] ArchetypeSerializationType: 0
[0] [0] NewEntityFragmentID: CGObject
[0] [1] NewEntityFragmentID: Tag_Item
Updatefields not fully read! Current position: 6 Length: 7 Bytes remaining: 1
```

To **nie jest `PlayerData`** — to obiekt z fragmentami (`HasFragmentUpdates: true`),
`CGObject` + `Tag_Item`, 7 bajtów długości, przeczytane 6. Zostaje **1 bajt**.

To jest ścieżka `ReadEntityFragments` w module V12, nie `PlayerData`.

**Problem B — 169 876 × `Current position: 2`** — blok `Values` dla gracza, który
czyta 2 bajty maski i staje. To drugi, osobny problem.

## Następny krok

**Problem A jest większy i łatwiejszy.** 129 229 z 173 747 ostrzeżeń to obiekt
z fragmentami, któremu zostaje **1 bajt**. To wygląda na pole, które parser czyta
jako 1 bajt za mało — albo bajt dopełnienia, którego nie czyta.

Warto najpierw obejrzeć ten przypadek: `CGObject` + `Tag_Item`, 7 bajtów.

## Stan kodu

**Wszystko cofnięte.** Parser w stanie po nagłówku nazwy:

| miara | wartość |
|---|---|
| `Updatefields` | 173 747 |
| `InvalidData` | 187 |
| `EndOfStream` | 969 |
| `creature` | 7 |
