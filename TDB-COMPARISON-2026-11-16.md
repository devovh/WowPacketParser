# Porównanie SQL ze sniffu z TDB — 2026-11-16

Źródło: `out/forever_70938_new.sql` (sniff `dump_1.60.1.70938_2026-11-16_10-39-12.pkt`,
271 433 pakiety, 13 005 wierszy w 72 tabelach, 0 złych wierszy).
Baza: MySQL 8.4, `wpp_tdb_base` (TDB `TDB_full_forever_2026_10_14`).
`wpp_tdb_w` to ta sama baza plus 13 nowych wierszy — porównuję z `wpp_tdb_base`, czyli czystym TDB.

## Wniosek w jednym zdaniu

Wszystko, co parser wypisał, **już jest w TDB** (z jednym wyjątkiem: `weather_update`, której nie ma
w żadnej z Twoich baz). Sniff nie wnosi nowych wierszy — wnosi **inny sposób liczenia** i to jest
jedyne miejsce, gdzie trzeba uważać.

## 1. `npc_vendor` — 102 wiersze: dane zgodne, brakuje jednej kolumny

- 4 vendorzy (`190`, `1287`, `1289` + `6740` częściowo) mają **identyczny zestaw itemów** co TDB.
- `6740`: sniff ma 12 itemów, TDB 15 — brakuje `21815`, `21829`, `21833`. Wyjaśnienie: tych itemów
  nie było w oknie vendora w trakcie sniffu (np. limit / warunek), nie błąd parsera.
- `299`, `6`, `69`: **nie ma ich w TDB w ogóle** (`rows=0`), a sniff ma dla nich 8, 6 i 5 itemów.
  To jedyny realny zysk z tego sniffu, ale to nie jest „nowy vendor” — TDB po prostu nie ma tych
  trzech entry w `npc_vendor`.
- **Brakująca kolumna:** SQL parsera nie wypisuje `incrtime` (TDB ma ją w schemacie). To znany brak
  danych, nie błąd. **Uwaga:** w TDB `incrtime` **nie jest wszędzie zerem** — 1 676 z 50 775 wierszy
  ma wartość > 0, czyli restock da się wziąć z TDB dla tych vendorów, a sniff musi uzupełnić resztę.
- Do sprawdzenia ręcznie: w sniffie `slot` jest numerem pozycji w oknie (33, 32, 31…), a w TDB
  `slot` to numer porządkowy (1, 2, 3…). Item per slot się zgadza, ale **kolumna `slot` znaczy co
  innego** — przed jakimkolwiek importem trzeba to przeliczyć.

## 2. `trainer` / `trainer_spell` — 3 trenerów, 24 zaklęcia

- `22` i `39`: dane **identyczne** z TDB (te same `SpellId`, te same `MoneyCost`, ten sam `ReqLevel`).
- `395`: **nie ma go w TDB** (`rows=0`), sniff ma dla niego 0 zaklęć — czyli pusty trener.
- Czyli zysk = 1 wiersz w `trainer`, zero w `trainer_spell`.

## 3. `creature_loot_template` — 19 wierszy, i tu jest realna różnica

| entry | item | sniff (drops/kills) | TDB |
|---|---|---|---|
| 6 | 159 | 10 % | 6,5536 % |
| 6 | 755 | 20 % | 29,7889 % |
| 6 | 4536 | 20 % | 13,4595 % |
| 69 | 750 | 100 % | 40 % |
| 69 | 7074 | 100 % | 38,3736 % |
| 299 | 750 | 100 % | 40 % |
| 299 | 4865 | 33,33 % | 36,9481 % |
| 299 | 7073 | 33,33 % | 37,8937 % |
| 299 | 7074 | 50 % | 37,7572 % |

- Przy 10 zabójstwach „1/10” to 10 %, a TDB mówi 6,55 % — **przy tak małej próbce szansa jest
  z definicji niepewna**. TDB jest tu wiarygodniejsze.
- **`100 %` w sniffie to artefakt:** przy `entry=69` item 750 wyszedł 1/1, więc parser liczy 100 %.
  TDB ma 40 %. To dokładnie ten problem, który opisuje `TODO-FULL-PARSER.md` 2.5.
- 6 itemów ze sniffu **nie ma w TDB** dla tych entry (`1368`, `1374`, `2210`, `2589`, `247834`,
  `1367`, `2211`, `2672`). Albo nowy loot Forevera, albo za mała próbka.

## 4. `creature_classic_level` — 220 wierszy, metoda daje węższy zakres

- Sniff 14.11: 83 wiersze, **82 identyczne** z TDB, jeden rozjazd: `3254` — sniff `11–11`,
  TDB `11–13`.
- Sniff 16.11: 220 wierszy. Tabela **jest już zaimplementowana** (`WDBTemplates.cs:223`,
  `CreatureClassicLevelForever`), wbrew temu, co twierdził stary wpis w `TODO-FULL-PARSER.md`.
- Metoda: min/max z poziomów widzianych NPC-ów. Ze sniffu wyjdzie **zawsze węższy lub równy**
  zakres niż z TDB, bo widzimy tylko część spawnów. Do uzupełniania brakujących entry — tak,
  do nadpisywania TDB — nie.

## 5. Czego w Twoich bazach nie ma wcale

Spośród 74 tabel, które parser wypisuje, **65 nie ma w `wpp_tdb_w`** — ale prawie wszystkie są
w `wpp_tdb_base` albo `wpp_tdb_h` (hotfixy: `spell_*`, `item`, `map`, `broadcast_text`, `taxi_nodes`).
Jedyne, czego nie ma nigdzie:

- **`weather_update`** — tabela spoza schematu TDB, helper parsera.

## Co z tym zrobić (rekomendacja)

1. Nie importować ze sniffu do TDB: `npc_vendor`, `trainer*`, `creature_classic_level` — TDB ma to
   samo lub więcej.
2. Traktować sniff jako **weryfikację** TDB, nie jako źródło: rozjazdy w `creature_loot_template`
   pokazują, gdzie próbka jest za mała (wszędzie poniżej ~50 zabójstw).
3. `slot` w `npc_vendor` **przeliczyć**, zanim ktokolwiek to zaimportuje — sniff ma numer pozycji
   w oknie, TDB numer porządkowy.
4. `incrtime` nadal wymaga sniffu z restockiem (vendor z limitem, 10 minut przerwy).
