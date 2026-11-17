# Co da się zrobić bez nowego sniffu (2026-11-16)

Pytanie: czy to, co zostało, da się domknąć z materiału, który już mamy?
Odpowiedź: **większość tak, i przy okazji znalazłem błąd w parserze.** Nowy sniff jest potrzebny
tylko do trzech rzeczy, które wymagają zdarzenia w grze (restock, niepuste AH, niepuste zestawy).

## 1. ZNALEZIONY BŁĄD: `npc_vendor` zapisuje złe `entry`

W sniffie 16.11 jest **6 pakietów `SMSG_VENDOR_INVENTORY`** i wszystkie mają `entry` z **GUID-a
spawnu**, a nie z GUID-a template'u. Efekt: w SQL znalazły się wiersze dla `6`, `69`, `299`
— a to **Kobold Vermin, Timber Wolf i Young Wolf** (`type 7` i `type 1`, `npcflag 0`).

```
299  Young Wolf    type=1 (bestia)  npcflag=0  -> nie może być vendorom
6    Kobold Vermin type=7 (kobold)  npcflag=0
69   Timber Wolf   type=1 (bestia)  npcflag=0
```

Te trzy „brakujące" wpisy w TDB, które w raporcie wyglądały jak zysk ze sniffu, to **śmieci**.

**Przyczyna jest w kodzie:**

```csharp
// WowPacketParser/Parsing/Parsers/NpcHandler.cs:472
uint entry = packet.WriteGuid("GUID", guidBytes).GetEntry();
```

`GetEntry()` bierze `Entry` z GUID-a. Dla GUID-a stworzenia Forever (`0x206B04...0000D605...`)
`Entry` to **indeks spawnu** (tu `0xD605 = 54789`), a prawdziwy `entry` NPC-a jest w polu `Low`
(tu `8019172`). Widać to w `_parsed.txt`:

```
VendorGUID: ... Creature/0 R6849/S54789 Map: 0 Entry: 190 Low: 8019172
```

Parser wypisał `Entry: 190`, ale zapisał do bazy `6` / `69` / `299` — czyli coś innego niż to,
co sam pokazał. Do sprawdzenia: czy `NpcVendor.Entry` nie jest nadpisywane przy `Entry == 0`.

**Naprawa:** dla Forever brać `entry` z `Low` (albo rozwiązać GUID przez `Storage.Objects`,
który po `SMSG_UPDATE_OBJECT` zna mapowanie). Wtedy zamiast 102 śmieciowych wierszy wyjdzie
102 poprawne, i wszystkie będą zgodne z TDB.

## 2. Co da się domknąć bez sniffu

### `npc_vendor` — po naprawie punktu 1
- `slot`: **potwierdzone na 3 vendorach** (1287, 1289, 190) — TDB trzyma `slot = pozycja w oknie − 1`.
  Np. sniff `1 → 851`, TDB `2 → 851`. Po tej regule wszystkie 71 itemów się zgadza.
- `maxcount`: z pakietu (`-1 → 0`). TDB ma 0 dla tych trzech vendorów — zgodne.
- `ExtendedCost`: z pakietu, 0 dla wszystkich — zgodne.
- `incrtime`: **nie da się** — w pakiecie nie ma limitu czasu restocku. To jedyne pole, które
  wymaga prawdziwego vendora z restockiem (albo TDB, gdzie 1 676 z 50 775 wierszy ma wartość > 0).
- `type`: parser ma `type` z `MuID`/`Type`, TDB ma 1 — do porównania po naprawie punktu 1.

### `trainer` / `trainer_spell`
- 22 i 39: **już zgodne z TDB** co do wartości. Nic do zrobienia.
- 395: pusty trener, w TDB go nie ma — jeden `INSERT` do `trainer`.
- Więcej trenerów nie będzie bez nowego sniffu (w 16.11 były tylko 3 okna).

### `creature_loot_template`
- Reguła liczenia już jest (`drops/kills`). Problem to **wielkość próbki**, nie kod.
- Bez nowego sniffu można tylko **nie nadpisywać TDB**: sniff ma iść jako `INSERT IGNORE`
  (tak już jest), żeby 18 zabójstw nie zastąpiło 228 000 wierszy TDB.

### bank / AH / zestawy
- Czytniki są napisane i kończą się czysto. Bez niepustej próbki **nie da się potwierdzić**,
  że `ItemInstance` w banku i `AuctionItem` w aukcjach czytają się poprawnie.
- Ale da się zrobić coś tańszego: **test syntetyczny** — zbudować pakiet z niepustą tablicą/np.
  3 itemami według układu dewelopera i sprawdzić, że parser czyta wszystkie pola i kończy na
  ostatnim bajcie. To nie jest dowód, ale wyłapuje błędy typu `u64` vs `u32`, jak dzisiaj.
  (Tak już zrobiono dla `SMSG_TRAINER_LIST` i `RECENT_ALLY_DATA_RESPONSE`.)

## 3. Czego naprawdę nie da się bez sniffu albo bazy

- `incrtime` — potrzebny vendor z restockiem.
- `npc_vendor.BonusListIDs` — kolumna jest w TDB, ale nie w pakiecie Forever.
- Prawdziwe szanse łupu — potrzebne 50+ zabójstw na entry.

## Kolejność pracy

1. Naprawić `entry` w `npc_vendor` (punkt 1) — to błąd, nie brak danych. Po naprawie: build,
   sniff 16.11 (musi zostać 0 błędów), SQL, porównanie z TDB na nowo.
2. Wtedy dopisać brakujące: 3 trenerów z 16.11 do `trainer` (2 już są), sprawdzić `type`.
3. Testy syntetyczne dla niepustego banku/AH/zestawów.
4. Tabela `npc_vendor`: ustalić, czy `slot` ma być liczony jako pozycja−1 w builderze,
   czy zostawić jak jest i przeliczać przy imporcie.
