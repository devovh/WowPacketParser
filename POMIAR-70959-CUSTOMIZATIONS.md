# `Customizations.size()` czyta się jako `00 02 09 00` — czyli 135168

Ostatni pomiar: 12 bajtów przed `Customizations.Resize`, wypisane dla każdego create gracza w 70959.

```
[Y] preCust->721  len=2062  next=00-02-09-00-00-00-00-00-00-00-00-00
[Y] postCust->741 len=2062
[Y] preCust->688  len=2063  next=00-00-00-01-00-00-00-00-00-00-00-00
[Y] postCust->708 len=2063
[Y] preCust->717  len=2051  next=00-00-00-00-00-00-00-00-00-00-00-00
[Y] postCust->737 len=2051
[Y] preCust->704  len=2069  next=00-00-00-19-00-00-00-94-08-00-00-02
[Y] preCust->689  len=2063  next=00-00-01-00-00-00-00-00-00-00-00-00
[Y] postCust->709 len=2063
```

## Co to znaczy

`Resize()` zjada `uint32` i **zawsze przechodzi dokładnie 20 bajtów** (`721 -> 741`, `688 -> 708`, `717 -> 737`). Czyli **`Resize` czyta dwa `uint32` po 4 bajty + coś jeszcze**, a nie tylko dwa liczniki. To jest zachowanie biblioteki, nie serwera — ale pokazuje, że parser **nie utknął w pętli**, tylko przeszedł dalej i **wywalił się później**.

Pierwszy przypadek: `00-02-09-00` little-endian = **0x00090200 = 589824**. To dokładnie ten śmieć, który widziałem w `Dynamic update field count 590336`. **Zgadza się co do wartości.**

## Gdzie leży prawdziwy problem

Przy `preCust->717` bajty to **12 zer**, a `Resize` i tak przechodzi do 737. **Czyli `Resize` czyta 20 bajtów niezależnie od zawartości** — a to znaczy, że licznik `Customizations` **nie steruje długością w tym miejscu**, albo jest czytany z innego offsetu.

**Kluczowa liczba:** po `postCust` zostaje `2062 - 741 = 1321` bajtów do końca bloku. Cały blok ma 2062. **Czyli `PlayerData` od startu (641–668) do `postCust` zużywa tylko ~80–100 bajtów**, a **1321 bajtów zostaje na resztę `PlayerData`** — która zawiera `VisibleEquipableSpells`, `ForcedReactions[32]`, `NpcAsPlayerInfo` i ogon z nazwami.

**1321 bajtów to dużo za mało na całą resztę, jeśli `PlayerData` ma być czytane do końca bloku.** Ale też **wystarczająco dużo**, żeby reszta się zmieściła — i to jest zgodne z tym, że błąd jest **później**, nie tutaj.

## Wniosek dla następnego kroku

`Customizations` **nie jest** miejscem błędu — `Resize` przechodzi i idzie dalej. Prawdziwe wywalenie (`pos` = +41…+56 od startu) wypada **przed** `preCust` w niektórych przypadkach i **po** w innych, co znaczy, że mierzyłem różne wywołania.

**Zostaje jedna rzecz, którą warto zmierzyć i która jest tania:** pozycja przy **każdym** polu `PlayerData` od `GuildLevel` do końca, na **jednym** pakiecie (`pkt 1372`, `PlayerData` start 641, blok 2051). To da pełną mapę zużycia bajtów i pokaże, gdzie zaczyna się rozjazd — bez zgadywania.

Instrumentacja cofnięta. Wynik bazowy bez zmian: **170 / 38 / 18**.
