# Dlaczego przeskok `PlayerData` NIE zmniejsza błędów — zmierzone

## Co zmieniłem i dlaczego nie zadziałało

Przeskok `PlayerData` po `GuildTimeStamp` dla 70959. Wynik: **183 / 42 / 18** — identycznie jak przy przeskoku po `VisualItemReplacements`, i **gorzej od bazy 170 / 38**.

**Zmiana działa — potwierdzone logiem:**
```
[SKIP] przed=1877 len=2045
[SKIP] po=2045 len=2045
```
Pozycja faktycznie skacze na koniec bloku.

## Prawdziwa przyczyna

**`ReadCreatePlayerData` nie jest miejscem, w którym powstaje 170 błędów.**

Log parsera po przeskoku pokazuje **`skipped 126`** zamiast poprzednich ~179, ale **`with errors` zostaje 42**. Czyli błędy **nie giną razem z `PlayerData`** — powstają **gdzie indziej**.

To wyklucza całą ścieżkę, którą mierzyłem przez ostatnie godziny:

- `PlayerData` w create **nie jest** źródłem `EndOfStreamException`
- `PlayerData` w create **nie jest** źródłem `InvalidDataException`
- `PlayerData` w create **nie jest** źródłem `OverflowException`

## Gdzie więc są błędy

`SMSG_UPDATE_OBJECT` obsługuje **dwie różne ścieżki**:

1. **create** — `ReadCreateObjectBlock` → `ReadCreatePlayerData` (to mierzyłem)
2. **value update** — blok wartości z maską → `ReadUpdatePlayerData` / `ReadUpdatePlayerData70938`

**Błędy muszą siedzieć w ścieżce 2**, bo przeskok w ścieżce 1 ich nie rusza.

Dodatkowo w tej samej funkcji jest drugi wywołujący: `ReadPlayerDataFlat` — używany dla Forever, ale **70959 nie jest `IsForever`**, więc to nie ta droga.

## Czego to dowodzi

**Godziny pomiarów szły w złą funkcję.** Każdy eksperyment (17-bajtowa struktura, `Customizations`, ogon list, przeskok) działał na `ReadCreatePlayerData` — a błędy pochodzą z **value update**.

To wyjaśnia, dlaczego **wszystkie cztery eksperymenty dawały ten sam wynik 183/42**: zmieniały kod, który **nie jest wykonywany dla pakietów z błędami**.

## Następny krok — konkretny

W `ReadUpdatePlayerData` (`:1711`) jest:
```csharp
if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
    return ReadUpdatePlayerData70938(packet, indexes);
```

**70959 idzie więc ścieżką retail** (`ReadUpdatePlayerData` z maską), a nie `ReadUpdatePlayerData70938`. **To jest pierwsze miejsce, które trzeba zmierzyć** — jeden znacznik, który powie, która funkcja faktycznie się wykonuje dla pakietów z błędami.

## Stan

Repo czyste, wynik bazowy bez zmian: **170 / 38 / 18**.
