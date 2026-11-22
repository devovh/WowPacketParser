# Odpowiedź dewelopera (2. runda) — zmierzona

Data: 2026-11-21. Źródło: wiadomość dewelopera tc-forever.

## Co powiedział

**1. Wszystkie trzy pola są pisane** w każdym create gracza:
`PlunderstormItemDisplayID` (19 × u32), `PersonalTabard`, `NpcAsPlayerInfo`.
**Czyli 76 bajtów naprawdę tam jest — przewerowanie jest z czegoś innego.**

**2. Ogon `PlayerData` NIE jest w kolejności retail.** Po `VisibleEquipableSpells` (16):

```
VisualItemReplacements count (u32)
PlunderstormItemDisplayID[19]
PersonalTabard
NpcAsPlayerInfo
17-bajtowy struct classic-only: 4 × u32 + 1 × i8   <- pierwszy u32 to super district
Customizations entries
RandomCustomizations entries
QuestSessionQuestLog entries (tylko PartyMember)
ArenaCooldowns entries
PetNames entries
VisualItemReplacements entries (i32 każde)
bity: imię (6), nazwisko (9), HasQuestSession (tylko PartyMember), HasLevelLink,
      HasDeclinedNames, flush
Name, Surname, potem DeclinedNames jeśli jest
```

To jest koniec `PlayerData`. **Liczniki `Customizations` i `RandomCustomizations`
nie są w ogonie** — siedzą wyżej, zaraz po pierwszej kopii tego samego 17-bajtowego
structu, która jest **po `GuildLevel`**.

**3. Blok `Values` to sam mask + pola z maski**, bez ogona create. **ALE maska
używa numerów bitów klienta classic, nie retail** — classic dodaje bity na własne
pola (dwa 17-bajtowe structy i nazwisko). **Pozycje bitów retail przesuwają się
po `GuildLevel`.**

## Co sprawdziłem w kodzie — i co się zgadza

**Ogon jest już poprawny.** Porównałem kod linia po linii z jego listą:

| dev | kod | zgadza się? |
|---|---|---|
| `VisibleEquipableSpells` (16) | linia 1503-1506 | ✔ |
| `VisualItemReplacements` count | 1507 | ✔ |
| `PlunderstormItemDisplayID[19]` | 1508-1511 | ✔ |
| `PersonalTabard` | 1512 | ✔ |
| `NpcAsPlayerInfo` | 1513 | ✔ |
| 17 B struct | **tylko dla 70938** | ✖ **brak dla 70959** |
| `Customizations` | 1519-1522 | ✔ |
| `RandomCustomizations` | 1523-1526 | ✔ |
| `QuestSessionQuestLog` (PartyMember) | 1527-1534 | ✔ |
| `ArenaCooldowns` | 1535-1538 | ✔ |
| `PetNames` | 1539-1542 | ✔ |
| `VisualItemReplacements` entries | 1543-1546 | ✔ |
| bity + Name + Surname | 1547-1565 | ✔ |

**Liczniki `Customizations`/`RandomCustomizations` są poprawnie przy `GuildLevel`**
(linia 1442-1443), ale **pierwsza kopia 17-bajtowego structu jest tylko dla 70938**
(linia 1433) — a dev mówi, że jest dla wszystkich.

## Co zmierzyłem

### Próba 1: 17 B struct w OBU miejscach (przy `GuildLevel` i w ogonie)

| miara | stan po nagłówku nazwy | +2 × 17 B |
|---|---|---|
| `Updatefields` | 173 747 | **183 290** ✖ |
| `InvalidData` | 187 | **488** ✖ |
| `EndOfStream` | 969 | **709** ✔ |
| **`creature`** | **7** | **6** ✖ |

**Gorzej na trzech z czterech.** Cofnięte.

### Próba 2: 17 B struct TYLKO w ogonie (zgodnie z jego listą)

| miara | stan po nagłówku nazwy | +17 B w ogonie |
|---|---|---|
| `Updatefields` | 173 747 | **176 254** ✖ |
| `InvalidData` | 187 | **219** ✖ |
| `EndOfStream` | 969 | **957** (ledwo lepiej) |
| **`creature`** | **7** | **7** ✔ |

**Też gorzej.** Cofnięte.

## Wniosek: jego opis ogona się zgadza, ale 17 B nie pasuje

Kod **już** czyta ogon dokładnie w tej kolejności, którą podał. Jedyne, czego brakuje,
to 17-bajtowy struct — a **dodanie go w każdym wariancie pogarsza wynik**.

To znaczy jedno z dwóch:

**A.** 17 B structu **nie ma** w 70959 (dev opisał writer 70938, nie 70959).

**B.** 17 B jest, ale **coś innego** w bloku jest krótsze o te 17 B — więc dodanie
przesuwa w drugą stronę. `EndOfStream` spada o 12–260, ale `InvalidData` rośnie —
klasyczny objaw przesunięcia, nie naprawy.

## Najmocniejszy trop: punkt 3 jego odpowiedzi

**„maska używa numerów bitów klienta classic, nie retail — po `GuildLevel` pozycje
retail przesuwają się."**

Mamy w repo `ForeverClassicBitRemap.ToRetailBits` i `PlayerDataGroups` — używane
dla `UnitData`, ale **nie sprawdziłem, czy `PlayerData` (ścieżka `Values`) też je ma**.
To jest dokładnie to, o czym mówi.

**Następny krok:** sprawdzić, czy blok `Values` dla `PlayerData` przechodzi przez
`ToRetailBits`. Jeśli nie — to jest źródło 8 805 ostrzeżeń „Current position: 2
Length: 109" i prawdopodobnie części `EndOfStream`.

## Stan kodu

**Wszystkie próby cofnięte.** Parser jest w stanie po nagłówku nazwy:

| miara | wartość |
|---|---|
| `Updatefields` | 173 747 |
| `Packet not fully read` | 722 |
| `InvalidData` | **187** |
| `EndOfStream` | 969 |
| **`creature`** | **7** |
