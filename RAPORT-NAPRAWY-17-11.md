# Raport: naprawa błędów parsera na sniffach 17.11.2026

Build 70938, serwer Wow Forever. Sniffy: `dump_1.60.1.70938_2026-11-17_10-05-40.pkt` (118 935 pakietów)
i `dump_1.60.1.70938_2026-11-17_11-53-35.pkt` (594 346 pakietów). Postać w sniffie: **Undead (Race 5), Paladin (ClassId 2)**.

---

## 1. Wynik przed i po

| sniff | przed | po |
|---|---|---|
| 17.11 11:53 | 593 812 (99,910 %), **11 błędów**, 9 „not fully read" | **593 820 (99,911 %), 3 błędy, 3 „not fully read"** |
| 17.11 10:05 | 118 622 (99,737 %), **1 błąd** | **118 623 (99,738 %), 0 błędów** |
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

## 3. Zostaje nierozwiązane (świadomie, bez zgadywania)

### `CMSG_BUY_ITEM` (0x3F003B) — 2 pakiety
Trzy pakiety: **43, 41 i 37 bajtów**. Próbowałem dwóch układów:

| hipoteza | wynik |
|---|---|
| guid Forever (u16 maska + bajty) + Entry/Slot/Count | maski `ef67`/`e667`/`e767` dają 12/10/11 bajtów GUID-a — nie zostawia miejsca na resztę |
| guid128 + Entry/Slot/Count + guid128 + bajt | wychodzi 41 B, a pakiety mają 37 i 43 B |

Żaden nie tłumaczy trzech różnych długości. **Handler zostaje stary**, a plik
`ForeverItemHandler.cs` to notatka przy kodzie (bez `[Parser]`), żeby nikt nie powtarzał tych prób.
Do dopisania w `DEVELOPER-REQUEST.md` jako prośba o układ od dewelopera.

### `CMSG_CHAT_MESSAGE_PARTY` (0x2D0026) — 1 pakiet
11 bajtów: `01 00 00 00 | 00 | A0 68 65 6C 6C 6F`, czyli Language=1 (Orcish) i tekst „hello".
Czytnik V4_3_4 czyta 9 bitów długości i wychodzi o 3 bajty za mało. Ale bajt przed tekstem (0xA0) nie jest
długością ani w bitach, ani w bajtach — nie odgaduję jego znaczenia. Zostaje stary czytnik.

---

## 4. Narzędzia dodane

- `tools/make-synthetic-nonempty.cjs` — generator sniffu PKT 3.1 z **niepustymi** wariantami
  `SMSG_GUILD_BANK_QUERY_RESULTS`, `SMSG_AUCTION_FAVORITE_LIST`, `SMSG_LOAD_EQUIPMENT_SET`,
  `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT`. Pozwala sprawdzić czytniki, których prawdziwy sniff
  nigdy nie trafił (zawsze `Count = 0`).
  **Status:** plik się generuje, ale parser go nie czyta („No packets parsed") — format rekordu wymaga
  jeszcze dopasowania do prawdziwego sniffu. Do dokończenia.

---

## 5. Wniosek o „procentie działania"

- **Zero wyjątków** na wszystkich czterech sniffach po zmianach.
- To, co zostaje, to 3 pakiety „not fully read" (2 × `CMSG_BUY_ITEM`, 1 × `CMSG_CHAT_MESSAGE_PARTY`) —
  oba układy nierozwiązane, nie zepsute.
- Undead paladin **nie generuje żadnych własnych, specyficznych pakietów** — nie ma tam nic do naprawy.
