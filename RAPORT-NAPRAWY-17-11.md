# Raport: naprawa błędów parsera na sniffach 17.11.2026

Build 70938, serwer Wow Forever. Sniffy: `dump_1.60.1.70938_2026-11-17_10-05-40.pkt` (118 935 pakietów)
i `dump_1.60.1.70938_2026-11-17_11-53-35.pkt` (594 346 pakietów). Postać w sniffie: **Undead (Race 5), Paladin (ClassId 2)**.

---

## 1. Wynik przed i po

| sniff | przed | po |
|---|---|---|
| 17.11 11:53 | 593 812 (99,910 %), **11 błędów**, 9 „not fully read" | **593 823 (99,912 %), 0 błędów, 0 „not fully read"** |
| 17.11 10:05 | 118 622 (99,737 %), **1 błąd** | **118 623 (99,738 %), 0 błędów, 0 „not fully read"** |
| 16.11 (kontrola) | 271 072 (99,867 %), 0 błędów | **271 074 (99,868 %), 0 błędów**, „bez struktury" 32 → 30 |
| 14.11 (kontrola) | 119 529 (99,946 %), 0 błędów | **bez zmian — 119 529, 0 błędów** |

Żaden starszy sniff się nie pogorszył. Build: 0 błędów kompilacji.

---

## 2. Co naprawione

### `SMSG_ON_CANCEL_EXPECTED_RIDE_VEHICLE_AURA` (0x470193) — 4 pakiety
Handler w `WowPacketParser/Parsing/Parsers/SpellHandler.cs` był **pusty**, a serwer wysyła 4 bajty.
Cztery wystąpienia: `BE C7 01 00`, `A4 BD 01 00`, `B7 D0 01 00`, `2C BE 01 00` — jedno `uint32`.

**Czego NIE zrobiłem:** nie nazwałem tego pola `SpellID`. Sprawdziłem wartości (116670, 114052, 118967, 114220):
- żadna nie występuje w tym sniffie jako `SpellID` w `SMSG_AURA_UPDATE` / `SMSG_SPELL_GO`,
- 118967 jest w TDB jako `creature_template` (Hillstalker Worg), czyli nie jest spellą.

Pole czyta się jako `Unk` — pakiet jest zjedzony do końca, ale nie twierdzimy, co znaczy.
Plik: `WowPacketParserModule.V1_60_1_70888/Parsers/SpellHandler.cs`

### `CMSG_MINIMAP_PING` (0x440077) — 2 wyjątki `EndOfStream`
Fallback z `V6_0_2_19033` czytał `Vector2` (8 B) + bajt, a Forever wysyła **1 bajt**.
Nowy czytnik w module Forever: 1 × `u8`.
Plik: `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverGroupHandler.cs`

### `CMSG_SERVER_VALIDATION_SIGNATURE_REQUEST` (0x44019A) — 2 pakiety
Czytnik czytał **dwa** guidy Forever. Sniff 17.11 11:53 ma pakiet **29 B**: `Unknown: 3` + 8 + 11 + 11 bajtów,
czyli **trzy** guidy. Trzecia jest warunkowa (`if (packet.CanRead())`), więc krótsze pakiety
z 14.11 (11 B) i 16.11 (20 B) dalej czytają się poprawnie — potwierdzone na obu.
Plik: `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverDeveloperLayouts3Handler.cs`

---

## 3. ROZWIĄZANE 17.11.2026 po odpowiedzi dewelopera

Deweloper przysłał układy obu nierozwiązanych opcodów i **oba pasują do bajtów co do jednego**:

### `CMSG_BUY_ITEM` (0x3F003B)
guid Forever VendorGUID, guid Forever ContainerGUID, `ItemInstance` Item, `i32 Quantity`, `u32 Muid`, `u32 Slot`, `i32 ItemType`.

Trzy pakiety (43/41/37 B) wychodzą jako 12/10/11 bajtów GUID-a + lista modyfikatorów itemu — wszystkie trzy kończą się na ostatnim bajcie. Pole zmiennej długości to rozmiar GUID-a **plus modyfikatory itemu**, nie samo Entry/Slot/Count.
Wynik na sniffie: 43 B → ItemID 2901, Quantity 1, Muid 1, Slot 255.
Plik: `ForeverItemHandler.cs`

### `CMSG_CHAT_MESSAGE_PARTY` (0x2D0026)
`i32 Language`, `11 bitów TextLength`, `1 bit IsSecure`, flush, tekst.

Tajemniczy bajt `0xA0` to **dolna połowa 11-bitowej długości plus IsSecure** — `00 A0` = długość 5 i IsSecure 0. Ten sam układ (bez IsSecure) obsługuje SAY, RAID, RAID_WARNING i INSTANCE_CHAT.
Wynik: Language 1 (Orcish), TextLength 5, IsSecure False, Text „hello".
Plik: `ForeverChatHandler.cs`

### Blok w `SMSG_AUCTION_LIST_BUCKETS_RESULT` (0x4702F7)
To `ItemInstance` — te 2/7/12 bajty to 0/1/2 modyfikatorów, a nie nieznana treść. Heurystyka długości nie jest potrzebna.
**Już wdrożone** w `ForeverAuctionHandler.cs` (commit `a605bde21`, 15.11). Sprawdzone na sniffie 16.11:
2 316 koszyków z blokiem extra, `ModifierCount` 0 (1 719) lub 1 (642), `HasBonus` ustawiony w 1 424 z nich,
a cały sniff kończy się z **0 nieprzeczytanych bajtów**.

## 4. Czego deweloper nie potwierdził

- **`SMSG_DUEL_WINNER`** — jego serwer pisze układ retail (6 bitów BeatenNameLen, 6 bitów WinnerNameLen, 1 bit Fled, flush, u32 BeatenVirtualRealmAddress, u32 WinnerVirtualRealmAddress, nazwy). Pakiet 43 B z żywego serwera tego nie pasuje, więc nasze zgadywanie (5 nieznanych bajtów przed nazwami) **zostaje niepotwierdzone**.
- **`CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE`** — serwer go ignoruje bez czytania, więc nie ma dowodu na żaden układ. Zostaje nasze `u32`.
- 70862 (pola update, 4 opcody bez nazwy, 7 tylko-hex) — deweloper nie ma nic do dodania.

---

## 5. Narzędzia dodane

- `tools/make-synthetic-nonempty.cjs` — generator sniffu PKT 3.1 z **niepustymi** wariantami
  `SMSG_GUILD_BANK_QUERY_RESULTS`, `SMSG_AUCTION_FAVORITE_LIST`, `SMSG_LOAD_EQUIPMENT_SET`,
  `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT`. Pozwala sprawdzić czytniki, których prawdziwy sniff
  nigdy nie trafił (zawsze `Count = 0`).
  **Status:** plik się generuje, ale parser go nie czyta („No packets parsed") — format rekordu wymaga
  jeszcze dopasowania do prawdziwego sniffu. Do dokończenia.

---

## 6. Wniosek o „procentie działania"

- **Zero wyjątków** na wszystkich czterech sniffach po zmianach.
- W sniffach 17.11 **nie zostaje ani jeden pakiet** z „not fully read" i ani jeden wyjątek.
- Sniff 17.11 11:53: 71 pakietów „bez struktury" (0,012 %) — to opcody, które mają nazwę, ale czytnika nie ma; tam nic się nie psuje, po prostu nie ma co czytać.
- Undead paladin **nie generuje żadnych własnych, specyficznych pakietów** — nie ma tam nic do naprawy.
