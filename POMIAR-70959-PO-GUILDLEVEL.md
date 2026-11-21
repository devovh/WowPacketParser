# Wynik ostatniego pomiaru: po `GuildLevel` NIE ma 17-bajtowej struktury

Jedna linia instrumentacji w `ReadCreatePlayerData`, wypisująca 20 bajtów zaraz po `GuildLevel`.
Przebieg na `dump_1.60.1.70959_2026-11-18_04-12-11.pkt`, pierwsze 12 trafień.

```
[X] GuildLevel->721 left=1341 next=00-02-09-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
[X] GuildLevel->688 left=1375 next=00-00-00-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
[X] GuildLevel->701 left=1383 next=00-00-00-00-00-00-00-19-00-00-00-8D-3B-00-00-02-00-00-00-04
[X] GuildLevel->717 left=1334 next=00-00-00-00-00-00-00-00-00-00-00-00-02-06-00-00-00-00-00-00
[X] GuildLevel->704 left=1365 next=00-00-00-19-00-00-00-94-08-00-00-02-00-00-00-04-00-00-00-03
[X] GuildLevel->689 left=1374 next=00-00-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
[X] GuildLevel->699 left=1421 next=00-00-00-00-00-00-00-00-00-00-00-00-00-02-12-00-00-00-00-00
[X] GuildLevel->687 left=1371 next=00-00-00-00-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00
[X] GuildLevel->708 left=1351 next=00-00-00-00-00-00-00-00-00-00-00-00-00-02-09-00-00-00-00-00
[X] GuildLevel->692 left=1372 next=00-00-00-00-01-00-00-04-00-00-00-00-00-00-00-19-00-00-00-5B
```

## Co z tego wynika

Gdyby po `GuildLevel` szła struktura `4 x uint32 + int8` z super district na początku, **drugi bajt po `GuildLevel` byłby zerowy** — super district mieści się w jednym bajcie dla każdej normalnej wartości (to `uint32`, ale wartości są małe).

To jest spełnione w 9 z 10 przypadków. **Ale to nie wystarcza jako dowód**, bo super district mógłby być 0 i wtedy cała struktura to 17 zer, a tak nie jest — w kilku próbkach już trzeci/czwarty bajt jest niezerowy (`00-02-09-00`, `00-00-00-01`, `00-00-00-19`).

**Najważniejsze:** `left` wynosi **1334–1421** przy całym bloku **~2060**. Czyli po `GuildLevel` zostaje **dwie trzecie bloku**, a parser wywala się **~45 bajtów dalej**. To znaczy, że problem jest **kilka pól dalej**, nie tu.

## Gdzie więc pada

Wcześniejsza diagnostyka pokazała: `pos` przy wywaleniu to **+41…+56 od startu `PlayerData`**, a `GuildLevel` ląduje na **+36…+56**. Czyli wywalenie następuje **kilka–kilkanaście bajtów po `GuildLevel`**.

Pola między `GuildLevel` a wywaleniem to, licząc ze źródła:

```
uint32 super district
uint32 0
uint32 0
uint32 0
int8   0
uint32 Customizations.size()        <- tu parser czyta licznik
uint32 RandomCustomizations.size()
uint8 PartyType[0], PartyType[1]
uint8 NativeSex, Inebriation, PvpTitle, ArenaFaction
uint32 DuelTeam
int32  GuildTimeStamp
```

**Bajty w próbkach pasują do tego układu, jeśli super district i dwa z trzech `uint32` są zerowe, a trzeci niesie wartość.** Np. `00-00-00-19-00-00-00-94-08-00-00-02-...` — zera do czwartego bajtu, potem `19`, potem `94 08`, potem `02`.

**Czyli struktura prawdopodobnie JEST, tylko przesunięta o jedno pole** — albo `Customizations.size()` jest czytane o 4 bajty za wcześnie.

## Wniosek

Hipoteza „brakuje 17-bajtowej struktury" jest **niewystarczająca** — dodanie jej pogarszało wynik, i teraz wiadomo dlaczego: **struktura prawdopodobnie już jest czytana jako coś innego**, a niezerowy bajt `02` w próbkach to prawdopodobnie **początek licznika `Customizations`**, który parser traktuje jako super district.

Instrumentacja cofnięta, repo czyste, wynik bazowy bez zmian: **170 / 38 / 18**.
