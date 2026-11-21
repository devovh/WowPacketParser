# Mapa `PlayerData` — 107 kompletnych create, jeden przebieg

Instrumentacja po każdym `packet.Read*` w `ReadCreatePlayerData`, odległości liczone od `GuildTimeStamp`.
Przebieg: `dump_1.60.1.70959_2026-11-18_04-12-11.pkt`.

## Co się zgadza idealnie — 107 na 107 wywołań

| pole | odległość od `GuildTimeStamp` |
|---|---|
| `GuildLevel` | −22 |
| `NativeSex` | −11 |
| `DuelTeam` | −4 |
| `GuildTimeStamp` | 0 |
| `PlayerTitle` | **+441** |
| `LogoutTime` | **+498** |
| `CovenantID` | **+787** |
| `SpectateTarget` | **+848** |
| `WorldLootObjectInventorySwapSlot` | **+852** |

**Zero rozrzutu.** Każde z tych pól ląduje w tym samym miejscu we wszystkich 107 pakietach. To znaczy, że parser od `GuildTimeStamp` do `WorldLootObjectInventorySwapSlot` **czyta spójnie i poprawnie** — 852 bajty bez ani jednego wahnięcia.

## Jedno odstępstwo

`PlayerTitle`: 106 razy +441, **1 raz +12699**. Ten jeden to najpewniej pakiet, w którym `PlayerData` zaczyna się w innym miejscu (inny obiekt), a nie błąd odczytu — reszta jego pól też jest przesunięta o tę samą wartość (`LogoutTime` +12756, `CovenantID` +13045 — różnice te same).

## Gdzie jest rozrzut — i to jest odpowiedź

**`Name`:**

| odległość | ile razy |
|---|---|
| +1154 | 19 |
| +1157 | 15 |
| +1159 | 14 |
| … | reszta |

**Tu jest zmienność.** Wszystko **przed** `Name` jest stałe, `Name` skacze. To znaczy, że **zmienność wprowadzają pola na końcu** — te z listami zmiennej długości:

```cpp
data << uint32(ArenaCooldowns.size());
for (uint32 i = 0; i < 32; ++i) ForcedReactions[i].WriteCreate(...);
data << uint32(PetNames.size());
CtrOptions->WriteCreate(...);
...
data << uint32(VisualItemReplacements.size());
for (uint32 i = 0; i < 19; ++i) data << uint32(PlunderstormItemDisplayID[i]);
PersonalTabard->WriteCreate(...);
NpcAsPlayerInfo->WriteCreate(...);
```

**`PetNames.size()` i `VisualItemReplacements.size()` zmieniają długość — i to one odpowiadają za skok `Name` między +1154 a +1159.**

## Wniosek

**`PlayerData` czyta się poprawnie przez pierwsze 852 bajty.** Błąd nie leży w nagłówku ani w polach stałych.

**Leży w listach zmiennej długości na końcu** — i tam trzeba patrzeć dalej. To zawęża obszar z ~2000 bajtów do **~300 bajtów końcówki**.

## Co to zmienia w dotychczasowych hipotezach

- „Brakuje 17-bajtowej struktury" — **wykluczone**, struktura leży między `GuildLevel` a `GuildTimeStamp`, a ten odcinek jest stały co do bajtu we wszystkich 107 pakietach.
- „`Customizations` czyta śmieć" — **wykluczone**, `Resize` przechodzi i odległości dalej są stałe.
- **Zostaje:** `PetNames`, `VisualItemReplacements`, `ArenaCooldowns` — listy bez maski, długość zależy od zawartości.

Instrumentacja cofnięta, repo czyste. Wynik bazowy bez zmian: **170 / 38 / 18**.
