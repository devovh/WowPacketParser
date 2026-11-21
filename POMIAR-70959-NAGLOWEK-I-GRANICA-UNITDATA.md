# 70959 — co zmierzono w sesji 20.11 (surowe bajty w miejscu rozjazdu)

Ta notatka domyka trzy otwarte hipotezy z `STAN-70959-GDZIE-ZACZAC.md`.
Wszystko poniżej jest **odczytane z logów parsera i z bajtów sniffu**, nie wywnioskowane.
Stan błędów się **nie zmienił** (19.11: 38 błędów, 18.11: 170) — to są pomiary, nie poprawka.

## 1. Którą ścieżką idzie 70959 w `SMSG_UPDATE_OBJECT`

**Nie ścieżką retail** — wręcz przeciwnie, niż zakładała notatka wyjściowa.

`WowPacketParserModule.V1_60_1_70888/Parsers/UpdateHandler.cs:27` przechwytuje
`SMSG_UPDATE_OBJECT`, tłumaczy numer na core'owy (`0x5E0000` → `0x5D0000`)
i woła handler 12.x. Ten czyta nagłówek w układzie **retail 12.x**:

| pole | retail 12.x (`UpdateHandler.cs:47-72`) | co jest w 70959 (sniff 19.11, pakiet nr 839) |
|---|---|---|
| `MapID` | u16 | 80 ad = **mapa 44416** (bajt 0x80 = `IsEndOfBatch`) |
| `NumObjUpdates` | u32 | 66 ✔ (po przesunięciu) |
| `IsEndOfBatch` | bit | bit 0 bajtu 0 = 1 ✔ |
| `HasRemovedObjects` | bit | w 70959 jest to bit 1 tego samego bajtu = **1**, a parser czyta bit 0 **następnego** bajtu = 0 → **nię widzi usunięć** |

Wniosek: **w 70959 `IsEndOfBatch` i `HasRemovedObjects` siedzą w jednym bajcie**
(retail ma je w dwóch bajtach / po `u32`). Zgodne z tym, co robi reszta kodu Forever
(`HasShortFragmentMask`), ale **nagłówek `SMSG_UPDATE_OBJECT` tego nie uwzględnia**.

## 2. Gdzie dokładnie zaczyna się rozjazd `PlayerData` (build 70959)

Sonda na żywym sniffie (`dump_1.60.1.70959_2026-11-19_07-58-01.pkt`) wypisała
pierwsze 32 bajty w miejscu, gdzie `ReadCreatePlayerData` startuje, dla obiektu
`[0] ActivePlayer`, `FieldFlags: 7 (Owner, PartyMember, UnitAll)`, blok 941 B:

```
pos=941  bytes=80 3F 00 00 00 00 00 00 00 00 00 00 00 00 00 0F 80 60 F3 BB 77 74 0F 80 3A 74 31 47 78 A5 4A 3F
```

Co z tego wynika, pole po polu:

- `80 3F 00 00` = **1.0f** → to jest `Scale` z **ObjectData**, czyli strumień
  stoi **przed** `ObjectData`, a nie w `PlayerData`. Parser tymczasem jest już
  w `PlayerData` (czyli UnitData/ObjectData zostały zjedzone o ~kilka bajtów za dużo).
- dalej: `0F 80 60 F3 BB` / `77 74 0F 80` / `3A 74 31 47` / `78 A5 4A 3F` — to
  **packed guidy**: `0F` = maska (4 bajty), `80` = maska (1 bajt). Czyli po `Scale`
  idą guidy, zgodnie z `ObjectData` + `UnitData`.
- pełny guid `A5 4A 3F 05 8C 6C 08` pojawia się w bajtach dopiero dalej
  (`... 78 A5 4A 3F`) — to ten sam guid, który parser pokazuje jako `Object Guid`
  obiektu 0.

Czyli: **ogon `PlayerData` nie jest problemem — problem jest wcześniej**, w granicy
`UnitData` → `ObjectData`, i to ta granica przesuwa cały blok o kilka bajtów.

## 3. Dowód, że to nie „za mało pól w PlayerData”

Dla tego samego obiektu w logu widać rzeczy niemożliwe po stronie danych:

- `QuestLog [0] QuestID: 151126016` (0x0903_0000) — przy 175 wpisach quest logu to śmieć,
- `ForcedReactions [3] FactionID: 40037120`, `[7] FactionID: 2` — też śmieć,
- `VisibleItems [2] ItemID: 201326979` — też śmieć,
- `[15] PlunderstormItemDisplayID: 1949990927`, `[16] ...: 7882545` — też śmieć.

Śmieci są **zgrupowane w tablicach o stałym rozmiarze**, a nie rozsiane — to znaczy,
że parser czyta je o stałą liczbę bajtów obok, a nie że brakuje jednego pola.

## 4. `ReadUpdatePlayerData` w tej sesji w ogóle nie był wołany dla `CreateObject2`

Sonda w `ReadUpdatePlayerData` dała tylko kilka wywołań w całym snifie (pozycje 14–65,
to bloki `Values`), a `CreateObject2` ich nie wywołuje. Czyli zmiany w
`ReadUpdatePlayerData` **nie mają prawa zmienić liczby błędów przy tworzeniu obiektu** —
zgodne z tabelą pomiarów z poprzedniej notatki (wszystkie ~183/42).

## 5. Co z tym zrobić dalej (kolejność, jedna zmiana na raz)

1. **`HasRemovedObjects` w 70959 czytać jako bit 1 bajtu, w którym jest `IsEndOfBatch`**
   (albo równoważnie: `IsEndOfBatch` i `HasRemovedObjects` z jednego bajtu MSB-first).
   To jedyna zmiana w nagłówku — sprawdzić, czy 38 błędów spada, i czy `NumObjUpdates`
   nie zacznie się rozjeżdżać na innych pakietach.
2. Dopiero potem ruszać granicę `UnitData`/`ObjectData` — punkt 2 pokazuje, że tam
   leży pierwsze przesunięcie. Kandydaci: brak bajtu `HeirFlags` (usunięty dopiero
   w 8.1), albo inny zestaw bitów w nagłówku ruchu dla `ActivePlayer`.

## Narzędzia dodane w tej sesji

- `tools/pkt2json.cjs` — `.pkt` → JSON (bez Pythona; **na tej maszynie `python` nie działa**,
  a `ABACUSAI_BOT_NODE` czyta `Buffer` inaczej niż `node` z PATH — do hexów używać `node`).
- `tools/probe-playerdata.cjs` — przechodzi `SMSG_UPDATE_OBJECT` po nagłówku i blokach,
  pokazuje `off/typ/objType/guid/blok/size`. Uwaga: w tym środowisku `Buffer.from(hex,'hex')`
  na długim stringu gubi pierwszy bajt — czytać `parseInt` po dwa znaki.

## Czego NIE robić (potwierdzone tą sesją)

- Nie zmieniać `ReadCreatePlayerData` „w ciemno” — sonda pokazuje, że strumień jest
  już przesunięty **przed** wejściem w `PlayerData`.
- Nie zakładać, że 70959 idzie „ścieżką retail” w całości — nagłówek idzie, ale
  `IsEndOfBatch`/`HasRemovedObjects` są w innym miejscu niż w 12.x.
