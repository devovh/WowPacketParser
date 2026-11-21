# 70959 — pomiar ścieżki Forever dla bloków `Values`

Data: 2026-11-20. Zmiana: `IsForever` w `WowPacketParserModule.V12_0_0_65390/Parsers/UpdateHandler.cs:30`
objęła także `V1_60_1_70959`. Zmierzona, potem **wycofana**.

## Wynik pomiaru

| miara | stan wyjściowy | IsForever + 70959 | ocena |
|---|---|---|---|
| `Updatefields not fully read` | 12 483 | **7 441** (−40%) | lepiej |
| `Packet not fully read` | 38 | **234** (×6) | gorzej |
| `InvalidDataException` | 22 | **18** | lekko lepiej |
| `EndOfStreamException` | 56 | **382** (×7) | dużo gorzej |
| dane gracza (`Health`, `MaxHealth`, `Level`) | śmieci | **te same śmieci** | bez zmian |

## Interpretacja

Spadek `Updatefields not fully read` o 40% **nie jest poprawą**. Liczba `Values` wręcz wzrosła
(11 536 bloków), ale to nie znaczy, że czytają się poprawnie — czytają się **krócej**,
bo ścieżka Forever czyta mniej bajtów i zostawia resztę. Zapłaciliśmy za to:
`EndOfStreamException` ×7 i `Packet not fully read` ×6, czyli parser zaczyna **wybiegać poza
koniec pakietu** i psuć kolejne pakiety w strumieniu.

Najważniejsze: **dane gracza się nie poprawiły**. `Health == MaxHealth == 1984274890752`,
`Level: 0` — identycznie jak przed zmianą. Czyli przyczyna nie leży w wyborze ścieżki
fragmentów, tylko w samym czytniku pól.

## Wniosek

Ścieżka Forever dla `Values` **nie jest** rozwiązaniem. Zmiana wycofana,
stan wyjściowy przywrócony i zweryfikowany:

`Updatefields not fully read` **12 483** · `Packet not fully read` **38** ·
`InvalidDataException` **22** · `EndOfStreamException` **56**

## Co to zawęża

Skoro ani ścieżka retail, ani Forever nie poprawia `Health`/`MaxHealth`, a sonda bajtowa pokazała
`Power[0]=0, MaxPower[0]=525, potem zera` — problem jest **w środku czytnika pól**, nie w wyborze
ścieżki. Następny krok to czytnik `ReadCreateUnitData`/`ForeverUnitDataReader` sam w sobie,
porównany bajt po bajcie z tym, co realnie przychodzi za `Health`.
