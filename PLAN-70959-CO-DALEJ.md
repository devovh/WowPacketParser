# 70959 — co robić dalej (stan po dwóch kolejnych pomiarach)

Data: 2026-11-20. Kod parsera **czysty**, stan wyjściowy zweryfikowany:
`Updatefields not fully read` **12483** · `Packet not fully read` **38** ·
`InvalidDataException` **22** · `EndOfStreamException` **56**

## Zmierzone i odrzucone (nie wracać do tego)

| hipoteza | wynik | wyrok |
|---|---|---|
| maska guidu `u16` zamiast `u8` | 0 różnicy (kontrola: branch wyłączony = to samo) | **odrzucone** — `pos=119` dowodzi, że maski są 1-bajtowe |
| nagłówek `[u16][u32][u16][u8][u32]` | czyta się poprawnie (`NumObjUpdates: 66`, `Data size: 121517`) | **odrzucone** — działa |
| 2 int32 `Power/MaxPower` zamiast 20 | `InvalidDataException` 22 → 64 | **odrzucone** — przesuwa błąd |
| `IsForever` obejmuje 70959 (bloki `Values`) | `EndOfStreamException` 56 → 382, dane gracza bez zmian | **odrzucone** |
| 17 B „super district” za `GuildLevel` w create | 180 bloków **naprawionych**, ale 690 bloków `Values` zepsutych | **odrzucone** — działa tylko w create |

## Co jest teraz twardo ustalone

1. **Sonda działa tylko przez `AddValue`** — `Console.WriteLine` nie trafia do wyjścia procesu.
2. **176 bajtów za `Health`** (CreateObject2, gracz) to: `Power[0]=0`, `MaxPower[0]=525`,
   `525` znowu, ~80 B zer, `9.5f`, ~76 B zer, `462`, `14`.
3. **`MaxHealth` jest czytany z niewłaściwego miejsca** — `1984274890752 = 462 << 32`,
   czyli z ItemID-u `462` i zer. Serwer nie wysyła złego `MaxHealth`; reader czyta nie tam.
4. **83% błędów to bloki `Values`** (10 395 z 12 483), nie `Create`.
5. **Bloki `Values` gracza mają 21 B i kończą się po 2 B** (`pos=2`, `Length=21`).
   To jest najczęstszy wzorzec (`rem=19`, 2 550 wystąpień) — i tam nikt jeszcze nie zajrzał.

## Rekomendacja: zacząć od punktu 5, nie od UnitData

Blok `Values` dla gracza ma 21 B, a czytnik zjada 2 i zostaje 19. To nie jest problem
`UnitData` — to problem **tego, co reader robi po `IsOwned`/`HasFragmentUpdates`
przy `HasFragmentUpdates: false`**.

Konkretnie: `UpdateHandler.cs:163` wysyła każdy blok przez `ReadUpdateEntityFragmentData`,
a `HasShortFragmentMask` dla 70959 przy `fragmentBitCount <= 3` **wychodzi z funkcji po
2 bajtach**. Czyli dla gracza bez fragmentów reader nie czyta **nic** i 19 bajtów zostaje.
To jest dokładnie ten `rem=19`.

**Pierwszy krok:** sprawdzić, co realnie stoi w tych 19 bajtach (sonda `AddValue`
na początku `ReadUpdateEntityFragmentData`, gdy `HasShortFragmentMask` wchodzi).
Jeśli to maska zmian pól gracza — to jest brakujący odczyt, a nie zepsuty UnitData.

## Drugi krok (po punkcie 5)

`ReadCreatePlayerData` dla 70959: 17-bajtowy struct za `GuildLevel` **działa w create**
(180 bloków domkniętych). Ale `ReadPlayerDataFlat` (używany dla `Values`) **nie ma go wcale**.
Po ustaleniu punktu 5 trzeba ten sam struct dodać do `ReadPlayerDataFlat` — i wtedy
`Values` gracza powinny się domykać.

## Czego nie robić

Nie ruszać `ReadCreateUnitData` ani `Power/MaxPower` — to jest **skutek**, nie przyczyna.
Objaw `Health == MaxHealth` zniknie sam, gdy bloki `Values` i struct district będą czytane
poprawnie.
