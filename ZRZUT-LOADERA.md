# ZRZUT-LOADERA.md — odszyfrowany kod klienta Forever 70938

Data: 2026-11-14. Cel: dostać funkcje `WriteUpdate`/`BuildValuesUpdate` z klienta,
żeby rozgryźć blok wartości `SMSG_UPDATE_OBJECT` (worek D — 1 756 błędów w sniffie 70862).

## Co poszło nie tak wcześniej

`dumpwow.exe` czytał cały obraz modułu **jednym** `ReadProcessMemory`. Trafiał na
stronę `PAGE_NOACCESS` (nagłówek `.text` w `Wow.exe`) i dostawał `ERROR_PARTIAL_COPY`
(err 299). Kod obsługi błędu zerował wtedy **cały pozostały obraz**. Dlatego
poprzednie zrzuty `dump_70938.img` miały `.text` same zera.

## Co zrobiłem

1. **`probe_mem.cpp` / `probe_mem.exe`** — chodzi region po regionie przez
   `VirtualQueryEx`, sprawdza `State`/`Protect` i raportuje, co naprawdę da się
   odczytać. Wynik: 4 308 regionów z danymi, 1,71 GB niezerowych bajtów.

2. **`dumploader2.cpp` / `dumploader2.exe`** — czyta moduł region po regionie
   (zachowując RVA), poprawia `PointerToRawData`/`SizeOfRawData` w nagłówkach sekcji
   i zapisuje prawidłowy plik PE.

## Wynik — `out/loader_full.img` (41,4 MB)

```
PID 13888, baza 0x7FFE84080000, SizeOfImage 0x2870000, sekcje 11, EntryPoint 0x1FD70C0
odczytano razem 42008576 z 42008576 bajtow obrazu   <- 100%, nic nie wyzerowane

sekcja    RVA          VirtualSize  rawOffset    niezerowe
.text     0x10000      33541286     0x400        30688860
.rdata    0x2010000    528804       0x1FFD200    351944
.data     0x20A0000    1135922      0x207E400    776381
.pdata    0x21C0000    72132        0x2193A00    65895
.eidsig   0x21E0000    32           0x21A5400    32
.gxfg     0x21F0000    10160        0x21A5600    5933
.tls      0x2200000    81           0x21A7E00    1
_RDATA    0x2210000    500          0x21A8000    353
.rsrc     0x2220000    664          0x21A8200    580
.reloc    0x2230000    6304         0x21A8600    6091
.eid      0x2240000    6094848      0x21AA000    5725903
```

Nagłówek: `MZ` / `PE\0\0`, machine `0x8664` (x64), `ImageBase 0x7FFE84080000`,
`EntryPoint 0x1FD70C0`. Plik otwiera się w Ghidrze/IDA jak zwykły DLL.

## Dowód, że to prawdziwy kod, nie szyfrogram

| Test | Wynik | Co znaczy |
|---|---|---|
| Prologi `48 83 EC` (sub rsp,x) | 3 378 | typowe prologi x64 |
| `48 89 5C 24` / `48 89 54 24` | 998 / 2 179 | zapis rbx/rdx na stos |
| `40 55` (push rbp) | 235 | klasyczny prolog |
| `4C 8B DC` (mov r11,rsp) | 16 | prolog z EH |
| `E8` call rel32 | 423 599, z tego **70,8%** celuje w `.text` | prawdziwe wywołania |
| `MOV reg,[rip+x]` | 21 246, z tego **100,0%** celuje w obraz | prawdziwe odwołania do danych |
| `LEA rip-rel` | 4 662, z tego **99,5%** celuje w obraz | j.w. |
| Entropia `.text` | 7,168 | szyfrogram miałby ~7,99 |

Entropia 7,168 jest wyższa niż typowa dla kodu (~6,2), ale to normalne dla
skompilowanego kodu z dużą ilością danych wbudowanych i wstawek SIMD.
Rozstrzygające są **100% poprawnych odwołań rip-relative** — szyfrogram tego nie daje.

## Czego ten zrzut jeszcze nie daje

`.rdata` loadera ma tylko **845 stringów ASCII** i **66 UTF-16** — i żaden nie zawiera
`Update`, `Values`, `Mask`, `Field`, `Opcode`. Loader to warstwa ładująca/odszyfrowująca,
**nie ma w nim logiki pakietów**. Ta siedzi w odszyfrowywanych modułach w pamięci
(`.eid`, 5,7 MB niezerowych) albo w `Wow.exe`.

Wniosek: **zrzut jest dobry i użyteczny, ale trzeba w nim szukać po kodzie, nie po
stringach.** Naturalny trop: `EntryPoint 0x1FD70C0` — stamtąd idzie odszyfrowanie.
Drugi trop: najczęściej czytane stałe w `.data` przez `MOV reg,[rip+]`:
`0x20A2930` (1 344 odwołań), `0x20A01C0` (796) — pierwsza z nich trzyma wskaźnik
`0x7FFE8609D4D8`, czyli adres w `.rdata` loadera.

## Następne kroki

1. Ghidra na `out/loader_full.img` → od `0x1FD70C0` w dół: znajdź pętlę odszyfrowującą
   i adres, pod który zapisuje odszyfrowany moduł.
2. Zrzucić **ten** moduł (region po regionie, jak wyżej) — tam będą stringi
   `SMSG_UPDATE_OBJECT` i funkcje `WriteUpdate`.
3. Z `WriteUpdate` odczytać kolejność pól pod maską → worek D → 100%.

Alternatywa bez Ghidry: **kod `tc-forever`** (`UpdateFields.cpp`, `PlayerData::WriteUpdate`)
daje kolejność pól wprost.

## Pliki

- `C:/Users/devov/Desktop/WoWSniff/probe_mem.cpp` + `.exe`
- `C:/Users/devov/Desktop/WoWSniff/dumploader2.cpp` + `.exe`
- `C:/Users/devov/Desktop/WoWSniff/out/loader_full.img` (41,4 MB, PE x64)

Uruchomienie:
```
cd C:/Users/devov/Desktop/WoWSniff
./probe_mem.exe
./dumploader2.exe out/loader_full.img
```
Wow.exe musi działać **i być zalogowany** — inaczej `.text` loadera jest `PAGE_NOACCESS`.
