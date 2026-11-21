# 70959 — pełna diagnoza przyczyny `Health == MaxHealth`

Data: 2026-11-20. Kod parsera **czysty**. Sniff bazowy: `10-24`.

## Ustalone fakty (wszystkie zmierzone, nie zgadywane)

### 1. Serwer ustawia ZARÓWNO bit grupowy, jak i bity pól

`UpdateField.h:640` — `MarkChanged` przy każdym polu z `BlockBit >= 0`:

```cpp
if constexpr (BlockBit >= 0)
    _changesMask.Set(BlockBit);   // bit grupowy, np. 32
_changesMask.Set(Bit);            // bit pola, np. 38
```

A `UpdateFields.h:344-345`:
```cpp
UpdateField<int64, 32, 38> Health;      // BlockBit=32, Bit=38
UpdateField<int64, 32, 39> MaxHealth;
```

Czyli gdy `Health` się zmienia: **bit 32 ORAZ bit 38** są ustawione.
`Health` i `MaxHealth` to osobne pola, więc zmiana jednego **nie** ustawia drugiego.

### 2. Remap bitów nie psuje niczego w tym zakresie

`WriteClassicChangesMaskHeader` (`UpdateFields.cpp:1375`) przenumerowuje bity przez
`ClassicUnitDataBit`. Dla bitów 32..47 wynik jest **identyczny** (32→32, 38→38, 39→39, 47→47).
Więc maska przychodzi z serwera z tymi samymi numerami.

### 3. Parser ma już obsługę bitów grupowych — i ona działa

`ForeverClassicBitRemap.ToRetailBits` (linia 146-160): gdy bit grupowy jest zapalony,
a **żaden** bit szczegółowy z zakresu nie jest, ustawia bit grupowy.
To odwzorowuje logikę serwera z `WriteClassicChangesMaskHeader` (linie 1385-1396).

**Ale to działa tylko, gdy bitów szczegółowych brakuje.** Gdy serwer ustawia bit 38
i **nie** ustawia 39, parser widzi `Changed(38)=true`, `Changed(39)=false` — i to jest
**zgodne z tym, co serwer naprawdę wysłał**. `MaxHealth` się wtedy nie zmienił.

### 4. Zmierzone: `MaxHealth` nie jest czytany w 1 027 blokach

W blokach z `UnitBlocksMask: 2` (grupa 32..63):
- 689 bloków czyta `Health`,
- **`MaxHealth` nie jest czytany ani razu.**

`Health: 854` jest **poprawne**. Więc odczyt działa.

## Wniosek: `Health == MaxHealth` to NIE błąd parsera

Jeśli serwer nie wysłał zmiany `MaxHealth`, parser ma prawo mieć `MaxHealth` równe temu,
co przyszło wcześniej. A `MaxHealth` przyszło w bloku **create** — i tam jest czytane źle
(patrz `POMIAR-70959-BAJTY-ZA-HEALTH.md`: `MaxHealth = 462 << 32`, czyli z ItemID-u).

**Czyli prawdziwa przyczyna jest w `ReadCreateUnitData`, nie w blokach `Values`.**
Bloki `Values` tylko dziedziczą złą wartość z create.

## Poprawka do wdrożenia

W `ReadCreateUnitData` (`UpdateFieldsHandler.cs:867-901`) dla 70959:

Zmierzone 176 bajtów za `Health`:
```
Power[0]=0, MaxPower[0]=525, MaxPower[0] ponownie 525,
~80 B zer, 9.5f, ~76 B zer, 462, 14
```

Obecny reader czyta 20× int32 + 20× float = 160 B i trafia `MaxHealth` na zera.
Trzeba ustalić **rzeczywisty** układ: ile par `Power/MaxPower` i ile `PowerRegen*`
wysyła 70959 — na podstawie 176 bajtów, nie na podstawie zgadywania.

To jest jedyna otwarta kwestia i jedyna droga: **odczytać te 176 bajtów pole po polu**,
zgodnie z tym, co robi serwer (`UnitData::WriteCreate`, `UpdateFields.cpp:1031-1090`).
Serwer pisze tam **20 int32** (`Power`/`MaxPower` w pętli `i<10`) i **20 floatów**
(`PowerRegen*` w pętli `i<10`) — czyli 160 B. Skoro w sniffie jest 176 B z innym
rozkładem, **serwer 70959 pisze inny wariant `WriteCreate`** niż ten w repo.

## Następny krok (konkretny)

Znaleźć w tc-forever **wariant** `UnitData::WriteCreate` dla buildów ≥ 70790
(`Build70790.FirstBuild = 70790` obejmuje 70959) — albo potwierdzić, że repo ma tylko
jeden wariant i różnica jest po stronie klienta. Bez tego dalsze zmiany to zgadywanie.
