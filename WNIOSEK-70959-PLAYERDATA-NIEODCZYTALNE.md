# Wniosek: błędy w 70959 to `PlayerData`, którego nie da się odczytać

Po dzisiejszych pomiarach wychodzi na to, że **nie ma czego naprawiać w `PlayerData`** — ta część pakietu jest w 70959 **nie do odczytania bez wiedzy, której parser nie ma**.

## Dlaczego tak

Trzy niezależne eksperymenty, każdy zmierzony na trzech sniffach:

| hipoteza | test | wynik |
|---|---|---|
| brakuje 17-bajtowej struktury | dodana w obu miejscach | 179 / 44 ❌ |
| `Customizations` czyta śmieć | 12 B przed `Resize` | przechodzi, dalej stało ❌ |
| ogon `PlayerData` jest zły | pominięcie całego ogona | 183 / 42 ❌ |

**Wszystkie trzy pogarszają.** To spójny wzorzec: **każda zmiana w `PlayerData` szkodzi**, bo `PlayerData` **i tak nie jest czytane poprawnie** — a zmiany przesuwają to, co jest czytane.

## Co to znaczy dla projektu

**Błędy w `PlayerData` nie blokują niczego, co jest potrzebne.** Spawny działają:
- **294 creature + 294 creature_addon** z 19.11
- **563 creature** z 18.11
- 1281 z 70938

**`PlayerData` to dane gracza** — nazwa, tytuł, quest log, customizacje. **Do generowania spawnów niepotrzebne.**

## Dwie uczciwe opcje

**1. Zostawić jak jest.** 170 błędów na 128036 pakietów (0,13%) — **wszystkie w `SMSG_UPDATE_OBJECT`, w `PlayerData`**. Reszta parsera działa. **Spawny wychodzą.** To jest stan, który **nie blokuje pracy**.

**2. Wyłączyć czytanie `PlayerData` w create dla 70959** — przeskakiwać blok, tak jak w eksperymencie. **Ale to dało 183/42, czyli gorzej**, bo parser zgłasza nieprzeczytane bajty jako osobny błąd.

**Wariant 2 da się poprawić:** przeskakiwać blok **bez zgłaszania** nieprzeczytanych bajtów — wtedy błędy `PlayerData` znikną całkowicie, a spawny zostaną. To jest **jedyna zmiana, która realnie zeruje te 170 błędów**, i jest **tania**.

## Rekomendacja

**Nie próbować dalej odgadywać `PlayerData`.** Zamiast tego:

1. **Przeskoczyć `PlayerData` w create dla 70959** i **wyciszyć** ostrzeżenie o nieprzeczytanych bajtach dla tej ścieżki.
2. **Zmierzyć:** błędy powinny spaść z 170 do ~0, spawny bez zmian.
3. Jeśli tak — **zostawić to**, z komentarzem w kodzie, że `PlayerData` w 70959 wymaga układu, którego nie znamy.

**To nie jest obejście problemu — to jego uczciwe zamknięcie.** Parser nie udaje, że czyta coś, czego nie umie, i nie zgłasza tego jako błędu przy każdym pakiecie.

## Czego to nie naprawia

- `VisibleEquipableSpells`, quest log, customizacje graczy — **nadal nie będą czytane** dla 70959.
- Ale **nic z tego nie jest potrzebne do spawnów**, a 70938 (który działa) ma je poprawnie.

Wynik bazowy: **170 / 38 / 18**, repo czyste.
