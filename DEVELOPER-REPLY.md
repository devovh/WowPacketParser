# WPP-full: status after your answers (70938)

Thanks, your answers closed most of the list. Everything below is pushed to https://github.com/devovh/WowPacketParser (master, commit `a605bde21`).

## Result on dump 70938 (live build)
| | before | now |
|---|---|---|
| Parsed OK | 117 421 (98.18%) | **119 054 (99.55%)** |
| With errors | 0 | **0** |
| Skipped (unnamed opcode) | 1 127 | **126** |
| No structure (hex only) | 1 046 | **414** |

Tests: 98 passed, 0 failed, 12 skipped. `check-sql`: 9 137 rows, 63 tables, 0 bad rows.

## Applied from your reply
- Opcode numbers and names: all 13 opcodes from your list (CMSG_MOVE_SET_FACING_HEARTBEAT, QUERY_PLAYER_NAMES, QUERY_PLAYER_NAMES_FOR_COMMUNITY, QUERY_CREATURE, QUERY_GAME_OBJECT, SET_SELECTION, PING, AUTH_SESSION, QUEST_POI_QUERY, SPAWN_TRACKING_UPDATE, UPDATE_ACCOUNT_DATA, SMSG_ENCHANTMENT_LOG, SMSG_GUILD_PERMISSIONS_QUERY_RESULTS).
- Renames: 0x4400B8 = CMSG_QUERY_GUILD_INFO, 0x53002C = SMSG_QUERY_GUILD_INFO_RESPONSE, 0x470373 = SMSG_MIRROR_VARS, 0x530035 = SMSG_GUILD_EVENT_MOTD.
- New handlers (per tc-forever): CMSG_QUERY_CREATURE (6-bit count + ids), SMSG_QUERY_GUILD_INFO_RESPONSE, CMSG_AUTH_SESSION (UseIPv6 bit before the ticket size).
- Auction extra block: now read as an ItemInstance as you described, with no length guessing. In the capture: 150 blocks with 0 mods, 11 with 1 mod, 17 with bonus ids. I only checked that the parser reads to the end without errors, not the values byte by byte.

## Still open
1. `0x440126`: our table has CMSG_CLUB_PRESENCE_SUBSCRIBE there, you say CMSG_BATTLENET_REQUEST. Not changed yet. Which is right for 70938?
2. 414 packets still have a name but no structure, and 126 opcodes have no name. Could you send names/layouts for the remaining ones, or your opcode table?
3. Build 70862 (older): 574 packets with errors. The main groups are SMSG_UPDATE_OBJECT (342, PlayerData of the owner) and SMSG_LFG_LIST_SEARCH_RESULTS_UPDATE (68). We only have your 70938 layout, so these need captures or layouts of 70862. Low priority if 70938 is the live build.
4. New capture: restocking vendors (npc_vendor.incrtime is still 0), more kills with loot, the auction house opened a few times. I will do it when I can play.
