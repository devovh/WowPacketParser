# Raport: SMSG_UPDATE_OBJECT pod WoW Forever 1.60.1

| plik | rozmiar | pakietów |
|---|---|---|
| `regedit_70790/dumps/dump_1.60.1.70862_2026-11-07_12-13-01.pkt` | 43 MB | 312 210 |
| `WoWSniff/ymir-src/dist/dumps/dump_1.60.1.70938_2026-11-13_13-18-57.pkt` | 5 MB | 29 428 |

---

## 1. Wynik końcowy

Kod wrócił do stanu wyjściowego (`git status`: tylko ten raport), build przechodzi,
oba sniffy parsują się jak wcześniej:

| Sniff | OK | Błędy |
|---|---|---|
| 70862 | 296 802 (95,065%) | 4 357 (1,396%) |
| 70938 (ymir) | 28 119 (95,552%) | 535 (1,818%) |

---

## 2. Rozwiązane: pełny układ kontenera bloku wartości

Zdekodowałem sniff 70938 własnym czytnikiem PKT 3.1 i rozłożyłem 860 pakietów
`SMSG_UPDATE_OBJECT` bajt po bajcie (15 581 bloków).

```
nagłówek: [map u16][blockCount u32][bity u8][dataSize u32]
blok:     [u8 UpdateType][guid: u16 maska LE + popcount bajtów][u32 rozmiar][rozmiar bajtów]
ciało:    [u8 IsOwned][u8 HasFragmentUpdates][u8 maskaTypu][u8 nBits]
          [ceil(nBits/8) bajtów maski zmian][pola]
```

**Układ domyka się co do bajta w 708 z 860 pakietów.**

### Kluczowe poprawki wcześniejszych błędnych hipotez

| hipoteza | pomiar |
|---|---|
| maska guidu to `u8` | **`u16` LE** — inaczej `size = 141323265` |
| po guidzie jest maska typów `u32` | **`u32` to rozmiar bloku** (np. `03 00 00 00` = 3 bajty) |
| `03 00` w pakiecie 47 B to nieznane pole | **`03` = maska, `00` = 1. bajt następnej struktury** |
| `nBits` to liczba elementów | **liczba bitów maski**: `0x20`=32, `0x60`=96, `0x80`=128, `0xc0`=192, `0x01`=1 |

### Rozkład masek (zmierzony)

| maskaTypu | nBits | bloków |
|---|---|---|
| `0x15` | — (blok 3-bajtowy `00 00 15`) | **13 264** |
| `0x17` | `0x20` | 1 222 |
| `0x03` | `0x20` | 593 |
| `0x17` | `0x60` | 103 |
| `0x05` | `0xc0` | 128 |

85% bloków to pusty `00 00 15` — zero pól.

---

## 3. Gdzie jest granica — i dlaczego nie da się iść dalej

**Kolejności pól pod maską nie da się ustalić z sniffów.** Próbowałem:
- 785 unikalnych ciał dla maski `0x17`, 582 dla `0x03`
- brak powtarzalnego wzorca bit → pole
- parser `ymir-src` też tego nie rozgryzł (1027 × `Updatefields not fully read`)

**Zmierzone próby czytania pól pogarszają wynik:**

| wariant | OK | błędy |
|---|---|---|
| baseline | 28 119 (95,552%) | 535 |
| maska + czytanie pól | 28 119 (95,552%) | 535 |
| maska, pola pominięte | 28 119 (95,552%) | 535 |
| maska, bez czytania pól (sam kontener) | **28 452 (96,683%)** | **202** |

Wszystkie warianty z odczytem maski dają identyczny wynik — bo **błąd siedzi wyżej**,
w tym samym miejscu, którego nie ruszałem. Po odrzuceniu maski zostaje 210 ×
`Updatefields not fully read` i 487 × `EndOfStreamException`, ale **żaden z tych
wyjątków nie pochodzi z `SMSG_UPDATE_OBJECT`** — to `QueryHandler.HandleNameQueryResponse`
(23×), `CharacterHandler.ReadPlayerGuidLookupData` (25×), `WorldSessionHandler.HandleTutorialFlags`.

---

## 4. Co jest gotowe do użycia

### 4.1 Opcody Forever — `ymir-src/data/opcodes.txt`
3298 opcodów z prawdziwymi numerami, w tym `S 5D0000 SMSG_UPDATE_OBJECT`
(w sniffie jako `0x5E0000` / core `0x5D0000`).

### 4.2 Sniffer Forever — `ymir-src/sniffer/` (C#)
Czyta klucz sesji z pamięci klienta, AES-256-GCM, deflate 32 KB, `SMSG_MULTIPLE_PACKETS`.
Działa na każdym buildzie bez patchowania.

### 4.3 `Wow.exe` z dysku jest zaszyfrowany
`C:/Program Files (x86)/World of Warcraft/_forever_/Wow.exe` (81,7 MB, 1.60.1.70938):
sekcja `.text` ma entropię **8,00**, zero prologów funkcji, zero stringów gry, jedyne
importy to `Wow_loader.dll`. Statyczna analiza (Ghidra/IDA) **nic nie da** — potrzebny
zrzut z uruchomionego procesu.

---

## 5. Co dalej

1. **Rozstrzygnąć, co czytać pod maską** — potrzebne 3–5 pakietów tego samego obiektu
   z różnymi maskami (0, 1, 2, 6) albo zrzut z pamięci klienta po odszyfrowaniu.
2. **Osobno, bez reverse engineeringu:** `SMSG_CHAT` w
   `WowPacketParserModule.V1_60_1_70888/Parsers/ChatHandler.cs` — brakuje
   `SenderGuildGUID` i `SenderWowAccount` między `SenderGUID` a `TargetGUID`.
   Kolejność znana z `tc-forever`.
3. `HandleTutorialFlags` (`V1_60_1_70888/Parsers/WorldSessionHandler.cs:60`) —
   czyta `u32` z pustego pakietu; w sniffie 2 wystąpienia, oba `EndOfStreamException`.
