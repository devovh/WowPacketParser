# Eksperyment: odcięcie ogona `PlayerData` nie usuwa błędów

Hipoteza: błędy w 70959 biorą się z list zmiennej długości na końcu `PlayerData`.
Test: wymusić, żeby parser **przeskoczył cały ogon** i nie próbował go czytać.

## Wariant A — przeskok od razu po `WorldLootObjectInventorySwapSlot`

Po 852 bajtach potwierdzonych jako stabilne, parser ustawia pozycję na koniec bloku i wraca.

| sniff | błędy |
|---|---|
| 70959 18.11 | **183** (baza 170) |
| 70959 19.11 | **42** (baza 38) |
| 70938 | 18 |

## Wariant B — przeskok przed listami (po `WorldLootObjectInventorySwapSlot`, przed `Customizations`)

Ten sam wynik: **183 / 42 / 18**.

## Co to znaczy

**Błędy nie znikają — wręcz rosną o 13 i 4.** Czyli:

1. **Ogon `PlayerData` nie jest źródłem błędów.** Gdyby był, pominięcie go naprawiłoby wszystko.
2. **Błąd jest wcześniej** — w części, którą uznałem za stabilną, albo **poza `PlayerData`**.
3. Wzrost o 13 błędów to efekt uboczny: pominięte bajty zostają nieprzeczytane, a parser zgłasza to jako „Updatefields not fully read".

## Gdzie to zostawia diagnozę

Mapa 107 pakietów pokazała **stałe odległości** od `GuildTimeStamp` do `WorldLootObjectInventorySwapSlot`. Stała odległość dowodzi, że parser **przechodzi** ten odcinek — ale **nie dowodzi, że czyta go poprawnie**. Może czytać spójnie te same śmieci w każdym pakiecie.

To jest słabość, którą trzeba nazwać wprost: **stałość nie jest poprawnością**. Trzeba porównać wartości z tym, co być powinno (np. `PlayerTitle` musi być sensownym identyfikatorem tytułu, `LogoutTime` sensownym czasem), a tego jeszcze nie zrobiłem.

## Wniosek praktyczny

Trzy hipotezy odpadły dziś pomiarem:
- brak 17-bajtowej struktury → odcinek stały, dodanie pogarsza
- `Customizations` czyta śmieć → `Resize` przechodzi, dalej stało
- ogon `PlayerData` → pominięcie ogona nie zmniejsza błędów

**Zostaje sprawdzenie, czy wartości w „stabilnym" odcinku są sensowne** — to jest tanie i nie wymaga deva: wypisać `PlayerTitle`, `LogoutTime`, `CovenantID`, `CurrentSpecID`, `HonorLevel` z kilku pakietów i zobaczyć, czy to liczby z sensownego zakresu, czy śmieci.

Wynik bazowy bez zmian: **170 / 38 / 18**. Repo czyste.
