# Obsługa builda 1.60.1.71037 w parserze

Data: 2026-11-21. Zmiana: dodany build **71037** (WoW Forever, 20 XI 2026) jako
alias 70959. Test regresji na sniffie 70959 **przechodzi bez zmian**.

## Dlaczego 71037 = 70959

Z ForeverChanges (fanowska baza czytająca pliki klienta), o buildzie z 20 listopada:

> „It writes the server's hotfixes up to November 17 into the client itself, and
> **no talent, class spell, racial or item reads differently**."

Czyli 71037 to **build serwerowy** — przenosi hotfixy do klienta, ale nie zmienia
danych. Nie ma podstaw, żeby układ pakietów był inny. **Założenie: identyczny jak
70959.** To trzeba potwierdzić sniffem.

## Co zmieniłem

Sześć plików, 12 miejsc — `71037` dopisany obok `70959`:

| plik | co |
|---|---|
| `WowPacketParser/Enums/ClientVersionBuild.cs` | `V1_60_1_71037 = 71037` |
| `WowPacketParser/Enums/Version/Opcodes.cs` | mapowanie opkodów → 70888 |
| `WowPacketParser/Enums/Version/UpdateFields.cs` | handler pól → 70888 |
| `WowPacketParser/Misc/ClientVersion.cs` | moduł parserów → 70888, `IsForever` |
| `...V12_0_0_65390/Parsers/UpdateHandler.cs` | `HasShortFragmentMask` |
| `...V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs` | 6 miejsc (layout, obejścia) |

**Ważny szczegół:** w `UpdateFieldsHandler.cs:963` był warunek
`Build != 70959 && ForeverBuild.IsNewLayout`. Bez zmiany **71037 trafiłby tam
przypadkiem**, ale jako „nie-70959" — czyli **bez** obejść dla 70959 (punkty
treningowe na końcu bloku). Poprawione: teraz 71037 jest wykluczony tak samo jak 70959.

## Test regresji (sniff 70959, `dump_..._2026-11-19_10-24-12.pkt`)

| miara | baza | po zmianie |
|---|---|---|
| `Updatefields not fully read` | 171 994 | **171 994** ✔ |
| `Packet not fully read` | 719 | **719** ✔ |
| `InvalidDataException` | 346 | **346** ✔ |
| `EndOfStreamException` | 820 | **820** ✔ |
| **wierszy `creature` w SQL** | **7** | **7** ✔ |

**Zero regresji.** Zmiana jest neutralna dla 70959, jak powinna być.

## Czego to NIE dowodzi

**Nie mam sniffu z 71037**, więc nie sprawdziłem, czy układ bajtów jest faktycznie
ten sam. Alias jest **hipotezą opartą na notatce Blizzarda**, nie na pomiarze.

Jeśli 71037 zmienił układ — parser wpadnie na ścieżkę 70959 i zacznie się sypać
tak jak przy każdej innej zmianie. Pierwszy sniff to pokaże od razu.

## Co zrobić, gdy pojawi się sniff 71037

1. Spuścić parser na nowy plik:

```
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config "<nowy.pkt>"
```

2. Sprawdzić metryki w `<nowy>_parsed.txt` i wiersze `creature` w SQL.

3. **Jeśli metryki są podobne do 70959** (ok. 172k / 719 / 346 / 820, creature ≥ 7)
   — alias był poprawny, praca idzie dalej bez zmian.

4. **Jeśli się rozjadą** — 71037 ma inny układ i trzeba go rozebrać osobno.

5. **Baseline do porównania** to nadal sniff 70959 z 10-24. Nowy sniff 71037
   stanie się nowym baseline dopiero, gdy przejdzie.

## Stan repo

Zmienione pliki (niezacommitowane):

```
M WowPacketParser/Enums/ClientVersionBuild.cs
M WowPacketParser/Enums/Version/Opcodes.cs
M WowPacketParser/Enums/Version/UpdateFields.cs
M WowPacketParser/Misc/ClientVersion.cs
M WowPacketParserModule.V12_0_0_65390/Parsers/UpdateHandler.cs
M WowPacketParserModule.V1_60_1_70888/UpdateFields/UpdateFieldsHandler.cs
```

Plus wcześniejsza zmiana `tools/compare-with-tdb.cjs` (hasło z kodu do env).
