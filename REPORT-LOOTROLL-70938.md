# 70938 — SMSG_LOOT_ROLL / SMSG_LOOT_ROLL_WON / SMSG_OPEN_CONTAINER: stan i pomiar

Data: 2026-11-18. Sniffy: `dump_1.60.1.70938_2026-11-17_15-32-39.pkt` (493 052 pkt),
`..._23-36-25.pkt` (177 925 pkt), `..._19-39-54.pkt` (103 276 pkt), `..._20-39-47.pkt` (331 849 pkt).
Kod: `WowPacketParserModule.V1_60_1_70888/Parsers/ForeverLootRollHandler.cs`.

## 1. Co jest zrobione

`SMSG_OPEN_CONTAINER` (0x6A0006) — wcześniej czytał go ogólny handler 12.x jako jeden
GUID128 (16 B) na pakiecie 10-bajtowym i kończył `ArgumentOutOfRangeException`
(HighGuid 0x0804). Nowy czytnik: `u16` + `u32` + `u32`.

Efekt (15-32-39): **22 błędy → 19**, (23-36-25): **5 → 4**.

## 2. Co jest zmierzone w SMSG_LOOT_ROLL

Pakiet ma zawsze 42 bajty (8 szt. w 15-32-39). Rozbiór bajtów:

```
67 E0 F2 6C 7C C5 AB F0 6A 3C 0F E0 A5 4A 3F 05 8C 6C 08 | 00 01 00 00 | 00 00 03 32 03 00 00 00 00 | F5 FF FF FF 01 00 00 00 00 00
```

- pierwsze 10 bajtów to **GUID LootObject** w zwykłym pakowaniu 12.x (`0x3C6AF00000000000_00ABC500007C6CF2`, LootObject/0);
- kolejne 9 bajtów to **GUID Winner** (`Player/0 R6947/S0`, Low 0x53F4AA5) — ten sam w trzech pakietach;
- dalej `i32 Roll = 256`, `u8 RollType = 0`;
- potem ItemID/Quantity/LootItemType/LootListID i blok bitowy — **nieustalone**.

## 3. Czego nie da się ustalić z tych pakietów

Trzy pakiety w 15-32-39 różnią się tylko dwoma fragmentami:

| # | różnica A | różnica B |
|---|---|---|
| 1 | `F5 FF FF FF` | `01 00 00 00 00 00` |
| 2 | `FF FF FF FF` | `00 00 00 00 00 00` |
| 3 | `46 00 00 00` | `01 00 00 00 00 00` |

Żadna z tych wartości nie jest poprawnym `ItemID` (F5FFFFFF = 4294967285, 0x46 = 70).
W `SMSG_LOOT_ROLL_WON` (2 szt.) w tym miejscu są **prawdziwe** ItemID: `0x332` (818)
i `0xB41` (2881), a po nich stany rolowania (`46` = 70, `57` = 87) — tam układ jest czytelny.

Wniosek: w ROLL dwie 4-bajtowe wartości po `RollType` to nie ItemID/Quantity, tylko
pola rolowania (np. liczniki głosów), a item leży gdzie indziej. Z 3 pakietów, które mają
**identyczny** ItemID (te same dwa przedmioty) i **identyczne** guidy, nie da się tego rozstrzygnąć —
potrzebny pakiet z **innym** itemem albo drugi wariant tego samego rolowania.

Dodatkowo: w 15-32-39 jest tylko **jedna** kombinacja itemu (`0x332` = 818) i w 23-36-25 jedna
(`0xB41` = 2881), więc materiału do porównania jest za mało.

## 4. Odrzucone hipotezy (żeby nie powtarzać)

| hipoteza | wynik |
|---|---|
| 2 GUID-y 128 czytane jak w 12.x (maska low, maska high, bajty) | przesuwa o 1 bajt, potem czytnik spada o 4 bajty za daleko |
| `ReadPackedGuid128` (WPP: maska low → 8 bajtów, maska high → 8 bajtów) | to samo, reszta pakietu nie układa się w `Roll = 256` |
| ten sam `i32` co w ROLL na początku WON | daje `Roll = 0`, potem ItemID 818 — ale potem Quantity/LootListID to śmieci |
| brak bloku bitowego w ROLL | zostaje 4 bajty |
| 1 bit, 2 bity, 4 bajty, 5 bajtów bloku bitowego | zawsze zostaje 3–4 bajty |

## 5. Co dalej

Najkrótsza droga: **jeden nowy sniff z dwoma rolowaniami tego samego przedmiotu**
(np. dwa razy ten sam drop w jednej sesji) albo z rolowaniem przedmiotu, którego nie ma
w obecnych sniffach. Wtedy różnica bajtów wskaże ItemID i licznik bez zgadywania.

Drugi wariant: potwierdzenie układu od dewelopera (`LootHandler::BuildLootRoll` /
`LootRoll::BuildPacket` w tc-forever) — jeden akapit zamiast kolejnych hipotez.
