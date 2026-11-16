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

## After the session
- Log out normally so the file is closed.
- The file is saved in `C:\Users\devov\Desktop\regedit_70790\dumps`. Send the file name (or the file) for processing.

## Short version (if time is short)
Do only sections 1 and 2 (trainers and vendors): they give the biggest gain.
