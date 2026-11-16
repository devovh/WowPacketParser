# Sniffing checklist for WPP (build 1.60.1.70938)

Goal: capture packets that the current sniff (2026-11-14) does not contain, so the parser can fill the missing tables and so the untested handlers can be verified on real bytes.

## Before you start
- Start the sniffer as usual so it writes a `.pkt`, then log in a character and **do not log out until the end**.
- Use a character of about level 10+ with some money. Start in Stormwind (most NPCs).

## 1. Trainers (highest priority)
- Visit 5-10 trainers of different kinds (class, riding, First Aid, Cooking, Fishing).
- Open the trainer window at each one and wait about a second so the list loads.
- Buy one spell at one trainer.
- Fills: `trainer`, `trainer_spell`, `creature_trainer` (handler for `SMSG_TRAINER_LIST` 0x47018B is only tested on a synthetic packet).

## 2. Vendors
- Open the window of 10+ vendors (weapons, armor, food, reagents).
- At 2-3 vendors with limited stock, buy an item, walk away for 10 minutes, come back.
- Fills: `npc_vendor` (`maxcount`, `incrtime`, `ExtendedCost`).

## 3. Quests
- Accept and turn in 5-10 quests. On turn-in, open the reward window and do not click Complete at once.
- Fills: `quest_offer_reward`, `quest_request_items`.

## 4. Loot
- Kill 50+ mobs of the same type and loot them all.
- Fills: `creature_loot_template` (drop chance = drops/kills).

## 5. Other windows
- Auction house: open it and search for a few items.
- Read 2-3 books or signs (`page_text`).
- Click POI markers on the map (`points_of_interest`).
- If you can: stable master and pet window (hunter), mailbox (send yourself a mail from an alt), guild bank.
- Fills / verifies: pet, mail, guild bank and auction handlers (currently only empty cases are tested).

## 6. Niepuste bank, aukcje i zestawy (potrzebne od 2026-11-16)
Cztery czytniki są napisane z układu dewelopera (ocena A/A-), ale w sniffie 14.11 każdy z nich
trafił tylko na **pusty** pakiet (`Count=0`, `TabCount=0`, `ItemCount=0`). Układu nie da się
„ustalić” ze sniffu — brakuje danych, nie wiedzy. Każda z tych czterech rzeczy domyka jeden punkt:

- **Bank gildii z itemami**: otwórz bank gildii, w której w tablicy leży kilka itemów (min. 2 tablice).
  Potrzebny niepusty `SMSG_GUILD_BANK_QUERY_RESULTS` (0x530025): tablice z nazwami i ikonami,
  itemy z `ItemInstance`. Sprawdza czytnik `ForeverDeveloperLayouts4Handler.cs:35`.
- **Licytacje**: wystaw coś na aukcji i zalicytuj na cudzą aukcję, potem otwórz „Aukcje: licytowane”.
  Potrzebny `SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT` (0x4702FB) z `ItemCount > 0` — sprawdza
  `AuctionItem` (`ForeverDeveloperLayoutsHandler.cs:122`).
- **Ulubione aukcje**: dodaj 2-3 aukcje do ulubionych i otwórz listę.
  Potrzebny `SMSG_AUCTION_FAVORITE_LIST` (0x470304) z `Count > 0` (`ForeverDeveloperLayouts4Handler.cs:77`).
- **Zestawy wyposażenia**: zapisz 2 zestawy w oknie postaci i przełącz się między nimi.
  Potrzebny `SMSG_LOAD_EQUIPMENT_SET` (0x4701BB) z `Count > 0` (`ForeverDeveloperLayouts4Handler.cs:93`).

Wystarczy jedno wejście do banku i AH w trakcie sesji — reszta checklisty (trenerzy, vendory,
questy, loot) zostaje bez zmian.

## Nowy build klienta (procedura, 2026-11-16)

Numer opcodu i uklad pakietu to dwie osobne rzeczy. Nowy build **moze dzialac bez zadnych zmian**,
jesli serwer nie ruszyl ani numerow, ani zawartosci pakietow. Nie zakladaj z gory, ze jest zepsuty —
sprawdz po kolei:

**1. Uruchom parser na nowym sniffie i zapisz trzy liczby** (sa w logu i w naglowku `_parsed.txt`):
`parsed` (procent), `skipped`, `without structure`.

**2. Porownaj z punktem odniesienia** (sniff 14.11, build 70938):
`119 529 (99,946%)`, `skipped 55`, `without structure 10`.
- **Wszystko podobne** → numery opcodow prawdopodobnie sie nie przesunely. Idz do punktu 4.
- **`skipped` / `without structure` skoczylo o setki** → numery sie przesunely albo pojawily sie nowe
  opcody. To jest sygnal, ze tabela `WowPacketParser/Enums/Version/V1_60_1_69876/Opcodes.cs`
  jest nieaktualna. Idz do punktu 3.

**3. Ustal, co sie przesunelo.** W `_parsed.txt` numery nieznane ida jako hex, np.
`ClientToServer: 4456684 (0x4400EC) Length: 0`. Zbierz je:
`grep -oE "\((0x[0-9A-F]+)\) Length" _parsed.txt | sort | uniq -c | sort -rn | head -20`
i porownaj z tabela. Pakiety, ktore **wczesniej mialy nazwe, a teraz ida jako hex**, to przesuniete
numery. Nowe nazwy w tabeli na stare numery to najczestszy powod „cichego” zepsucia.

**4. UWAGA: brak bledow nic nie znaczy.** Przy przesunietych numerach parser czyta cudzy pakiet
i konczy „czysto”, bo bajty przypadkiem sie ukladaja. Dlatego nie patrz na `0 errors`, tylko na
**sens wartosci** w `_parsed.txt`: GUID-y (nie same zera, prefiks `Player/`/`Creature/`),
`SpellID` w sensownym zakresie (setki/tysiace, nie 4 miliardy), `Health` <= `MaxHealth`,
daty w `Time:` z tego samego dnia. Jesli pola wygladaja na smieci — uklad albo numer jest zly,
mimo ze nie ma bledu.

**5. Sprawdz pakiety, ktore na pewno powinny przyjsc** (te sa w kazdej sesji):
`SMSG_UPDATE_OBJECT` (0x5E0000), `SMSG_MOVE_UPDATE` (0x60000E), `SMSG_AURA_UPDATE` (0x690011),
`SMSG_CHAT` (0x4C0001), `SMSG_TIME_SYNC_REQUEST` (0x600000), `CMSG_PLAYER_LOGIN` (0x440016).
W sniffie 14.11 sa wszystkie (kolejno 3831, 77 861, 17 784, 16, 95 i 1 pakiet), wiec latwo je znalezc.
Jeśli któregoś brak w nazwach, numer się przesunął.

**6. Jesli numery sie przesunely** — nie zgaduj. Numery biora sie z `tc-forever` (`Opcodes.h`,
`ClassicOpcodes.cpp`) albo od dewelopera serwera (tak powstal commit `159275037`).
Kolejność pracy: popraw tabela → build → przelicz sniff → commit (`TODO-FULL-PARSER.md`, sekcja 4).

**7. Jesli numery sa dobre, ale jakis pakiet ma `Packet not fully read`** — to blad ukladu, nie numeru.
Weź payload z hexa w `_parsed.txt` (albo z `out/tmp/dump-opcodes2.cjs`) i porównaj z układem w kodzie,
tak jak przy `SMSG_BATCH_PRESENCE_SUBSCRIPTION` (bylo `u64 Id`, jest `u32 Id` + `u32 Unk`).

## After the session
- Log out normally so the file is closed.
- The file is saved in `C:\Users\devov\Desktop\regedit_70790\dumps`. Send the file name (or the file) for processing.


## Short version (if time is short)
Do only sections 1 and 2 (trainers and vendors): they give the biggest gain.
