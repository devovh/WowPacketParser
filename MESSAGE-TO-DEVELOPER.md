# WPP-full update: new sniff parses with 0 errors, a few questions

Hi,

I pushed an update to https://github.com/devovh/WowPacketParser (master, commit `0342e6993`).

## State of build 70938
- Sniff 14.11: 119 528 packets parsed, **0 errors**.
- Sniff 16.11 (271 433 packets): **0 errors**, down from 30. The SQL output has 13 006 rows in 72 tables, with 0 bad rows.
- Your trainer layout is in. On the real sniff it gives `trainer` 3, `trainer_spell` 24 and `creature_trainer` 3. Prices are written as sent, so the reputation discount is not removed.
- Also fixed, from the bytes and tc-forever:
  - `SMSG_LEVEL_UP_INFO` is `0x470195`. I removed the false `SMSG_CRITERIA_DELETED` that used that number.
  - Skill info and `PetNames` in the UPDATE_OBJECT update mask.
  - `CMSG_CREATE_CHARACTER`, `CHECK_CHARACTER_NAME_AVAILABILITY`, `PARTY_UNINVITE`, `DUEL_WINNER` and `REGIONWIDE_*`.
- The new sniff filled `npc_vendor` (83 rows), `creature` (792) and `creature_template` (473). It also filled `gameobject_template` (409) and the quest tables (offer reward, request items, questender).
- "Parsed OK" means each packet was read to the end. It does not mean every field is understood.

## Questions
1. Could you send your Stormwind/Westfall enUS `.pkt`, or the pet, stable and mail layouts you mentioned? Without them those packets stay unparsed in our sniffs. Then I can fix your remaining ~179 errors directly.
2. Two layouts are guesses (grade G) and I would like you to confirm them:
   - `SMSG_DUEL_WINNER`: 5 bytes sit between the two realm addresses and the names. I don't know what they are.
   - `CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE`: I read it as a single u32.
3. Four server opcodes have no name: `0x470377`, `0x470228`, `0x4C0005` and `0x470059`. Seven named opcodes have no layout. Samples are in `UNKNOWN-OPCODES-70938.md`.
4. For build 70862, the player create block with the Owner flag is shifted after `ModTimeRate`. This causes about 340 UPDATE_OBJECT errors in that sniff. If you have the 70862 UnitData/PlayerData layout, please send it. It is low priority.

For the next sniff, `SNIFFING-CHECKLIST.md` in the repo root lists what to do in game to fill the empty tables. Examples are waiting 10 minutes at vendors for `incrtime`, reading a book for `page_text`, and killing 50 or more of the same mob for loot chances.
