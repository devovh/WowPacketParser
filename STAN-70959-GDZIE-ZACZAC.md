# Gdzie stanąć — 70959, `PlayerData` w value update

Notatka dla kolejnej sesji. Wszystko poniżej jest zmierzone, nie wywnioskowane.

## Stan wyjściowy

| sniff | pakiety | błędy | creature |
|---|---|---|---|
| 70959 18.11 | 128036 | **170** | 563 |
| 70959 19.11 | 38799 | **38** | 294 + 294 addon |
| 70938 | 492567 | **18** | 1281 |

**Spawny działają.** Błędy to 0,13% pakietów, wszystkie w `SMSG_UPDATE_OBJECT`.

## Gdzie NIE szukać — wykluczone pomiarem

Wszystkie cztery zmiany dotyczyły `ReadCreatePlayerData` i **wszystkie dały ten sam wynik ~183/42**:

| zmiana | 70959 18.11 | 70959 19.11 |
|---|---|---|
| baza | 170 | 38 |
| 17-bajtowa struktura w obu miejscach | 179 | 44 |
| 17-bajtowa struktura po `GuildLevel` | 181 | 44 |
| `Owner`-only `int32` po `TaxiNodesID` | 170 | 38 (neutralne) |
| pominięcie `ArchetypeSerializationType` w create | 253 | 80 |
| przeskok `PlayerData` po `VisualItemReplacements` | 183 | 42 |
| przeskok `PlayerData` po `GuildTimeStamp` | 183 | 42 |

**Przeskok potwierdzony logiem:** `[SKIP] przed=1877 → po=2045`. Pozycja skacze, a **błędy zostają**.

**Wniosek: `ReadCreatePlayerData` nie jest źródłem błędów.** Zmienianie go nic nie da.

## Gdzie zacząć

`UpdateFieldsHandler.cs:1711` — `ReadUpdatePlayerData`:

```csharp
public override IPlayerData ReadUpdatePlayerData(Packet packet, params object[] indexes)
{
    if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
        return ReadUpdatePlayerData70938(packet, indexes);
    // ... ścieżka retail: 11 bitów maski bloków, potem 32 bity na zapisany blok
```

**70959 idzie ścieżką retail**, a nie `ReadUpdatePlayerData70938`. **To jest pierwsza rzecz do zmierzenia** — czy to prawda, jednym znacznikiem w obu funkcjach.

## Wzorzec

`ReadUpdatePlayerData70938` (`:7177`) **działa** — 70938 ma 18 błędów. Jeśli 70959 ma podobny układ, ta funkcja jest wzorcem do porównania.

## Co już wiadomo o `PlayerData` w create (dla kontekstu, nie do naprawy)

- `PlayerData` startuje stabilnie na **+641…+668** od początku bloku pól
- od `GuildTimeStamp` do `WorldLootObjectInventorySwapSlot` odległości **stałe w 107 pakietach** (+441, +498, +787, +848, +852)
- `flags` zawsze `UnitAll`, **ani razu `PartyMember`** → quest log 175 wpisów nie jest czytany
- ogon: ostatnia nazwa kończy się na +1354, po niej **48 bajtów** (40 zer)
- między imieniem a nazwiskiem **6 bajtów**, których źródło nie pisze

## Narzędzia

- `tools/pkt_dump.py` — `read_pkt()` zwraca `(direction, opcode, data)`
- `tools/walk-update-object.py`, `tools/dump_opcode.py`
- serwer: `C:/Users/devov/Documents/GitHub/TrinityCore`, branch `forever-bots`, HEAD `a66b6b80ef`
- `BaseEntity.cpp:96` — `BuildCreateUpdateBlockForPlayer` (kolejność: type, guid, objType, movement, u32 size, flags, fragmenty, dane)
- `UpdateFields.cpp:2674` — `PlayerData::WriteCreate` (bez maski, pola liniowo)
- `UpdateFields.cpp:2847` — `PlayerData::WriteUpdate` (z maską)

## Jak uruchomić

```
cd WowPacketParser/bin/Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config <pkt>
```

Sniffy: `C:/Users/devov/Desktop/regedit_70790/dumps/`
Build: `dotnet build WowPacketParser.slnx -c Release -v q --nologo`

## Czego nie robić

- **Nie zgadywać układu** — każda zgadywana zmiana pogorszyła wynik.
- **Nie dodawać pól do `ReadCreatePlayerData`** — wykluczone powyżej.
- **Nie rozszerzać warunków builda zakresem** (`<= 70959`) — wciąga 70938 i kosztuje 13872 błędy. Pisać per build.
- **Mierzyć każdą zmianę na trzech sniffach** i cofać, jeśli którykolwiek się pogorszy.
