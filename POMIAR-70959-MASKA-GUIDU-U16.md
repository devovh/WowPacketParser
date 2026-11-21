# 70959 — maska guidu `u16`: zmierzone, brak efektu

Data pomiaru: 2026-11-20. Kod parsera: **bez zmian** (zmiana cofnięta).

## Co zostało zmierzone

Hipoteza: w 70959 serwer pisze maskę guidu jako `uint16` i dokłada tylko niezerowe bajty
(`ByteBuffer& operator<<(ByteBuffer&, ObjectGuid const&)`, tc-forever
`src/server/game/Entities/Object/ObjectGuid.cpp:1087-1110`), a `ReadPackedGuid128` czyta
1-bajtową maskę + bajt `0x8000` — czyli **1 bajt przesunięcia na każdym guidzie**.

Zmiana: w `WowPacketParser/Misc/PacketReads.cs`, `ReadPackedGuid128`, gałąź tylko dla
`ClientVersionBuild.V1_60_1_70959`: maska 16-bitowa, potem `popcount(mask)` bajtów.

## Wynik — brak różnicy

Sniff `dump_1.60.1.70959_2026-11-19_07-58-01.pkt`:

| miara | bez zmiany | ze zmianą |
|---|---|---|
| `Packet not fully read` | 38 | 38 |
| `SMSG_UPDATE_OBJECT: InvalidDataException` | 22 | 22 |
| blok `Current position: 98907` | jest | jest |

**Test kontrolny** (gałąź wyłączona przez `&& false`, przebudowa, ten sam sniff): dalej 38 / 22.
Czyli pomiar **nie reaguje** na zmianę `ReadPackedGuid128` dla 70959 — gałąź albo nie jest
wykonywana w tej ścieżce, albo `ClientVersion.Build` w niej nie jest `V1_60_1_70959`.
To trzeba sprawdzić sondą przed jakąkolwiek dalszą zmianą w guidach.

## Kontrola środowiska (ważne dla kolejnych pomiarów)

- `dotnet build` **nie odświeża** `WowPacketParser/bin/Release/WowPacketParser.dll` przy
  zmianie w `WowPacketParser` — timestamp DLL stoi. Trzeba `touch` pliku przed buildem
  i sprawdzić czas DLL, inaczej mierzy się stary binarny.
- Wcześniejsze pomiary w tej sesji robione `node` są **nieważne**: runtime liczy
  operatory 32-bitowe niekonsekwentnie (to samo wyrażenie `66` vs `2910846976`).
  Wszystkie liczby z tej sesji, które pochodziły z `node`, należy powtórzyć w C#.

## Co jest pewne z surowego sniffu (do powtórzenia w C#)

Payload pakietu 0: `00 00 | 42 00 00 00 | 80 ad | da | 01 00 02 0f e0 a5 4a 3f 05 8c 6c …`

- `[u16 0][u32 66][u16 0xad80=44416][u8 0xda][u32 121517]`
- `121517 = 121528 − 11` — długość się zgadza co do bajtu, więc to prawdziwy nagłówek.
- Bajty 11+ = pierwszy blok; dotychczasowe czytanie go jako `updateType`+guid daje śmieci
  (typ 224, objectType 0), więc kolejność pól w bloku create jest inna niż w retail.

## Statystyka błędów 70959 (baseline, 19.11)

- `Updatefields not fully read`: **12483** ← to jest główna masa, nie 38
- `SMSG_UPDATE_OBJECT: EndOfStreamException`: 56
- `SMSG_UPDATE_OBJECT: InvalidDataException`: 22
- `Packet not fully read`: 38
- `OverflowException`: 2

Wniosek: „38 błędów” z wcześniejszych notatek to tylko `Packet not fully read`.
Realny wskaźnik to ~12,5 tys. niedoczytanych bloków wartości.

## Następny krok (jedna zmiana, jeden pomiar)

1. Sonda w `ReadPackedGuid128`: wypisać `ClientVersion.Build` i pierwsze wywołanie dla 70959.
   Dopiero potem ruszać maskę guidu — inaczej nie wiadomo, czy zmiana w ogóle działa.
2. Zmierzyć, ile z 12483 `Updatefields not fully read` to obiekty typu Unit (bez PlayerData),
   a ile ActivePlayer — to rozdziela problem `UnitData` od `PlayerData`.
