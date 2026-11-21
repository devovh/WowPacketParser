# Ogon `PlayerData` pkt 1372 — nazwy i 48 bajtów po nich

`PlayerData` w pkt 1372 zaczyna się na `@8224` (`7583 + 641`), blok kończy się na `@9626`.
**Długość `PlayerData` = 1402 bajty.**

## Ogon, od +1280

```
+1280  00 00 19 00 00 00 c0 01 00 00 1a 00 00 00 e0 01
+1296  00 00 1b 00 00 00 f8 01 00 00 1c 00 00 00 fb 01
+1312  00 00 39 03 00 00 4c 24 00 00 17 25 00 00 60 34
+1328  01 00 a0 01 00 00 06 5a 65 70 6e 61 6d 18 08 00
+1344  58 c3 ab 6c 6f 6e 4d 75 73 6b 01 00 00 00 00 01
+1360  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
+1376  00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
+1392  00 00 00 00 00 00 00 00 00 00
```

## Co tu jest

| offset | bajty | co |
|---|---|---|
| +1334 | `06` | długość imienia = 6 |
| +1335 | `Zepnam` | imię (6) |
| +1341 | `18 08 00` | **nieznane 3 bajty** |
| +1344 | `58 c3 ab` | **nieznane 3 bajty** |
| +1347 | `lonMusk` | nazwisko (7) |
| +1354 | `01 00 00 00` | … |
| +1358 | `00 01 00 00` | … |
| +1362 | 40 bajtów zer | … |

**Po nazwisku zostaje 48 bajtów** — pierwsze osiem to `01 00 00 00 00 01 00 00`, reszta zera.

## Liczby, które nie pasują do źródła

Źródło pisze:
```cpp
data.WriteBits(Name->size(), 6);
data.WriteBits(surname.size(), 9);
if (PartyMember) data.WriteBit(HasQuestSession);
data.WriteBit(HasLevelLink);
data.WriteBit(DeclinedNames.has_value());
data.FlushBits();
data << SizedString::Data(*Name);      // tylko tekst
data << SizedString::Data(surname);    // tylko tekst
if (DeclinedNames) DeclinedNames->WriteCreate(...);
```

Czyli: **17 bitów nagłówka, flush, imię, nazwisko bezpośrednio po imieniu, potem opcjonalnie 5 stringów `DeclinedNames`.**

**A w pakiecie:**
- między imieniem a nazwiskiem jest **6 bajtów** (`18 08 00 58 c3 ab`)
- po nazwisku jest **48 bajtów**, z czego 40 to zera

**`DeclinedNames` to 5 stringów** — gdyby wszystkie były puste, dałyby **20 bajtów** (`uint32` długości = 0 ×5). **Zostaje 28 bajtów niewyjaśnionych.**

## Odległość od stałego punktu

Z mapy 107 pakietów: `WorldLootObjectInventorySwapSlot` leży **+852** od `GuildTimeStamp`.
W pkt 1372 `Name` leży na **+1335**. **Różnica: 483 bajty** — a z mapy `Name` wypadał na **+1154…+1159** od `GuildTimeStamp`.

**483 − 1154 = −671.** To znaczy, że w pkt 1372 `PlayerData` **nie zaczyna się na 641**, tylko gdzie indziej — albo `GuildTimeStamp` jest w innym miejscu.

**To jest sprzeczność, której nie rozstrzygnąłem** i nie będę udawał, że rozstrzygnąłem.

## Co jest pewne

1. `PlayerData` **kończy się dokładnie na `@9626`** — zgadza się z `u32 @7571 = 2051`.
2. Ostatnia nazwa kończy się na **+1354**, po niej **48 bajtów**, z czego 40 to zera.
3. Między imieniem a nazwiskiem jest **6 nieznanych bajtów** — źródło ich nie pisze.
4. Odcinek od `GuildTimeStamp` do `WorldLootObjectInventorySwapSlot` jest **stały w 107 pakietach** — czyli czyta się poprawnie.

## Wniosek

**Ogon `PlayerData` ma 48 bajtów więcej, niż przewiduje źródło**, i to jest jedyna konkretna, zmierzona rozbieżność. Jeśli te 48 bajtów to `DeclinedNames` + coś jeszcze, to parser czytający tylko 5 pustych stringów (20 bajtów) **zostawia 28 bajtów nieprzeczytanych** — a to dokładnie ten rodzaj różnicy, który przesuwa wszystko dalej.

Instrumentacja cofnięta. Wynik bazowy bez zmian: **170 / 38 / 18**.
