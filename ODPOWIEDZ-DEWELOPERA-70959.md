# Odpowiedź dewelopera tc-forever + co z tego zmierzyłem

Data: 2026-11-21. Plik źródłowy: `C:/Users/devov/Downloads/alfa-70959-reply-1120.txt`.

## Co pisze deweloper (skrót)

**1. UnitData — ten sam writer dla graczy i stworów.**
Nie ma osobnej ścieżki dla graczy, nic ekstra. **Create nie ma bitów — każde pole
jest zapisywane zawsze.** Rozmiar zmieniają tylko: pakowane guidy (gracz ma
GuildGUID, stwór nigdy), liczniki (`StateWorldEffectIDs`, `PassiveSpells`,
`WorldEffects`, `ChannelObjects`) i flagi pól. Inni gracze dostają te same flagi
co stwory (`UnitAll`, plus `PartyMember` jeśli w grupie). **Na ich stronie bajty
create dla 70959 są dokładnie takie same jak dla 70938 — nie ma żadnej gałęzi 70959
w writerze.**

**2. Trzy bajty to nagłówek nazwy, czytany MSB-first.**
6 bitów długość imienia, 9 bitów długość nazwiska, `HasLevelLink`,
`HasDeclinedNames`, flush. **Jeden nagłówek dla obu stringów**, potem oba stringi
jeden za drugim.

```
18 08 00 = 000110 | 000000100 | 0 0  → imię 6, nazwisko 4   (Bright / Bone)
20 10 00 = 8 + 8                                            (Altarion / Holymoon)
10 0e 00 = 4 + 7                                            (Dead / Shadwow)
18 0a 00 = 6 + 5                                            (Dessyl / Swipe)
```

Mój przypadek `Zepnam / IonMusk` to to samo: `18 08 00`, potem `Xëlon` (6 B, bo
`ë` jest dwubajtowe) + `Musk`.

**3. Po nazwisku są jeszcze dwie rzeczy.**
`01` (PlayerHouseInfo active) + u32 liczba domów, potem `01` (PlayerInitiative
active) + packed `NeighborhoodGUID` (2 B) + `InitiativeInfo` (int64 + 3×int32 +
3×float = 32 B) + dwa u32 liczniki. **Razem 48 bajtów — dokładnie tyle, ile jest
przerwy między nazwiskiem a końcem bloku.** Czyli blok to
**ObjectData, UnitData, PlayerData, PHI, PII**, a nie trzy payloady.

**4. Diagnoza przewerowania (jego zdanie: „strong guess, not a measured fix").**
Około 36 miejsc w module 70888 sprawdza `Build == V1_60_1_70938`, więc 70959
wpada na starą ścieżkę:

| linia | co się dzieje | skutek |
|---|---|---|
| **822** | `StateWorldEffectIDs` czytane tylko dla 70938 | UnitData **4 B za krótki już na starcie** |
| **942** | `ModSpellHaste` czytane dla każdego nie-70938 → na 70959 czytane też dla innych graczy (a jest tylko dla ownera) | **+4 B** |
| **1551/1559** | bity i string nazwiska tylko dla 70938 | **−3 B** |

Moje obejścia dla 70959 (float po `ModTimeRate`, punkty treningowe na końcu)
prawdopodobnie **kasują** te błędy dla stworów. Dlatego stwory wychodzą dobrze,
a gracze nie — ich guidy i liczniki są inne, więc przesunięte odczyty trafiają
na pakowany guid albo licznik i uciekają za koniec bloku. Z tego samego powodu
pominięcie `ArchetypeSerializationType` wyglądało gorzej — to już była przesunięta ścieżka.

**5. Uwaga bezpieczeństwa (niezwiązana).**
`tools/compare-with-tdb.cjs` linia 7 miała hasło roota do lokalnego MySQL,
a repo jest publiczne. Zmienić hasło i przenieść je do zmiennej środowiskowej.

## Co zmierzyłem

### Baselines (sniff `dump_1.60.1.70959_2026-11-19_10-24-12.pkt`)

| miara | baza (parser czysty) |
|---|---|
| `Updatefields not fully read` | **171 994** |
| `Packet not fully read` | 719 |
| `InvalidDataException` | 346 |
| `EndOfStreamException` | 820 |
| **wierszy `creature` w SQL** | **7** |

### Weryfikacja uwag dewelopera w kodzie

**Linia 822 — potwierdzone.** Kod:

```csharp
data.StateWorldEffectIDs = new System.Nullable<uint>[ClientVersion.Build == ClientVersionBuild.V1_60_1_70938 ? packet.ReadUInt32() : 0];
```

**Linia 942 — potwierdzone, i to jest błąd logiczny.** Kod:

```csharp
if (ClientVersion.Build != ClientVersionBuild.V1_60_1_70938 || (flags & UpdateFieldFlag.Owner) != UpdateFieldFlag.None)
    data.ModSpellHaste = packet.ReadSingle("ModSpellHaste", indexes);
```

Na 70959 warunek `Build != 70938` jest **zawsze prawdziwy**, więc `ModSpellHaste`
jest czytane dla każdego bloku — także dla stworów i innych graczy. Powinno być
odwrotnie: „70938 **lub** 70959, i tylko dla ownera".

**Linie 1551/1559 — potwierdzone.**

### Pomiar: rozszerzenie warunków na „70938 lub 70959"

Zrobiłem dokładnie to, co sugeruje deweloper — trzy warunki (822, 942, 1551/1559)
zamienione na wspólny `ForeverBuild.HasNewFields`.

| miara | baza | po zmianie | różnica |
|---|---|---|---|
| `Updatefields not fully read` | 171 994 | **183 059** | **+11 065 (gorzej)** |
| `Packet not fully read` | 719 | 1 189 | +470 (gorzej) |
| `InvalidDataException` | 346 | 551 | +205 (gorzej) |
| `EndOfStreamException` | 820 | 1 347 | +527 (gorzej) |
| **wierszy `creature` w SQL** | **7** | **1** | **−6 (gorzej)** |

**Hipoteza dewelopera się NIE potwierdziła** — przy samych trzech warunkach jest
gorzej na każdej mierze. Zmiana przywrócona do stanu bazowego.

## Dlaczego hipoteza nie zadziałała

Deweloper **nie uwzględnił moich obejść dla 70959**, które już są w kodzie:

1. **Float po `ModTimeRate`** — mój kod czyta dodatkowy float dla 70959, bo
   `CreatedBySpell` wychodził `0x3F800000` (czyli ten float). To obejście
   kompensuje **+4 B** z linii 942.
2. **Punkty treningowe przeniesione na koniec** — kolejne obejście dla 70959.

Czyli **bajty się już zgadzały** (moje obejścia = jego „cancel out"), a włączenie
dodatkowych pól **dodało je zamiast zamienić** — i suma wyszła za duża.

Deweloper trafnie to przewidział („their guids and counts differ, so the shifted
reads land on a packed guid") — tylko w drugą stronę: nie chodzi o to, że obejścia
nie działają, ale że **już działają** i trzeba je **usunąć razem** z włączeniem
nowych pól, a nie włączać nowe na wierzchu.

## Co to znaczy dla dalszej pracy

**Następny pomiar musi podmieniać, nie dodawać.** Konkretnie:

1. Włączyć `StateWorldEffectIDs` dla 70959 (+4 B na starcie UnitData).
2. Włączyć `ModSpellHaste` tylko dla ownera na 70959 (**−4 B** dla stworów).
3. Włączyć bity nazwiska dla 70959 (−3 B, ale to ścieżka gracza).
4. **Usunąć obejście „float po ModTimeRate"** dla 70959 — bo linia 942 już go nie dodaje.
5. Zmierzyć. Jeśli stwory wrócą do 7+, iść dalej.

Kolejność ma znaczenie: **krok 4 jest warunkiem, żeby 1–3 cokolwiek dały.**

## Uwaga bezpieczeństwa — załatwiona

`tools/compare-with-tdb.cjs`: hasło wyjęte z kodu, czytane teraz z `WPP_MYSQL_PWD`
albo z `tools/.env.local` (wpisany do `.gitignore`). **Hasło nadal jest w historii
gita (commit `7d5d1c42a`) — trzeba je zmienić w MySQL, samo usunięcie linii nie wystarczy.**
