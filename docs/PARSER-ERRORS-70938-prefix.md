# WPP-full parser errors — sniff 1.60.1.70938

Source: `dump_1.60.1.70938_2026-11-14_12-41-07_parsed.txt` (119,594 packets, Orgrimmar, 8.5 min).
Generated automatically from the parser output file. **Re-generated after all session fixes.**

**Overall result: 116,907 OK (97.753%), 460 errors (0.385%), 1,181 skipped, 1,046 without structure.**

## Summary table

| opcode | wire | total | exceptions | not-fully-read | module / method |
|---|---|---|---|---|---|
| `SMSG_UPDATE_OBJECT` | 0x5E0000 | 583 | 354 | 229 | — |
| `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 0x660027 | 116 | 71 | 45 | `QueryHandler.ReadNameCacheLookupResult`<br>`CharacterHandler.ReadPlayerGuidLookupData`<br>`Packet.ReadValue` |
| `SMSG_QUERY_CREATURE_RESPONSE` | 0x4B0006 | 55 | 53 | 2 | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| `SMSG_PARTY_UPDATE` | 0x470097 | 10 | 0 | 10 | — |
| `SMSG_HEALTH_UPDATE` | 0x47017F | 6 | 0 | 6 | — |
| `SMSG_TITLE_LOST` | 0x470189 | 6 | 0 | 6 | — |
| `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | 0x4701B7 | 4 | 0 | 4 | — |
| `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | 0x470064 | 3 | 0 | 3 | — |
| `SMSG_WORLD_SERVER_INFO` | 0x470046 | 3 | 3 | 0 | `MiscellaneousHandler.HandleWorldServerInfo` |
| `SMSG_GUILD_ROSTER` | 0x530003 | 3 | 3 | 0 | `MythicPlusHandler.ReadDungeonScoreSummary`<br>`MythicPlusHandler.ReadDungeonScoreMapSummary` |
| `CMSG_RESURRECT_RESPONSE` | 0x4400AF | 3 | 0 | 3 | — |
| `SMSG_WHO` | 0x4C0002 | 3 | 0 | 3 | — |
| `SMSG_GUILD_EVENT_PRESENCE_CHANGE` | 0x530037 | 2 | 2 | 0 | `GuildHandler.HandleGuildEventPresenceChange` |
| `SMSG_QUERY_QUEST_INFO_RESPONSE` | 0x670016 | 2 | 2 | 0 | `Packet.ReadValue` |
| `SMSG_GROUP_DECLINE` | 0x47023D | 2 | 0 | 2 | — |
| `SMSG_AUTH_RESPONSE` | 0x470001 | 1 | 0 | 1 | — |
| `SMSG_ENUM_CHARACTERS_RESULT` | 0x470018 | 1 | 1 | 0 | `CharacterHandler.ReadClassUnlockData` |
| `CMSG_CONSUMABLE_TOKEN_REDEEM_CONFIRMATION` | 0x44011D | 1 | 1 | 0 | `TokenHandler.HandleConsumableTokenRedeemConfirmation` |
| `SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT` | 0x47020D | 1 | 0 | 1 | — |
| `CMSG_BATTLEMASTER_JOIN_ARENA` | 0x3F00BE | 1 | 1 | 0 | `Packet.ReadValue` |
| `SMSG_FEATURE_SYSTEM_STATUS` | 0x470063 | 1 | 1 | 0 | `Packet.ReadValue` |
| `SMSG_ACCOUNT_MOUNT_UPDATE` | 0x470047 | 1 | 0 | 1 | — |
| `SMSG_GUILD_EVENT_LOG_QUERY_RESULTS` | 0x53002B | 1 | 1 | 0 | `GuildHandler.HandleGuildEventLogQueryResults` |
| `SMSG_RATED_PVP_INFO` | 0x4D000F | 1 | 0 | 1 | — |
| `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | 0x4D0014 | 1 | 0 | 1 | — |
| `SMSG_AUCTION_LIST_BUCKETS_RESULT` | 0x4702F7 | 1 | 1 | 0 | `AuctionHouseHandler.ReadBucketInfo` |
| `CMSG_AUTOBANK_ITEM` | 0x410003 | 1 | 0 | 1 | — |
| `SMSG_THREAT_CLEAR` | 0x470191 | 1 | 0 | 1 | — |

## Details — exceptions (packets counted as errors)

| # | opcode | length | Number | exception | first module frame |
|---|---|---|---|---|---|
| 1 | `SMSG_ENUM_CHARACTERS_RESULT` | 1129 B | 55 | System.IO.EndOfStreamException | `CharacterHandler.ReadClassUnlockData` |
| 2 | `CMSG_CONSUMABLE_TOKEN_REDEEM_CONFIRMATION` | 4 B | 244 | System.IO.EndOfStreamException | `TokenHandler.HandleConsumableTokenRedeemConfirmation` |
| 3 | `CMSG_BATTLEMASTER_JOIN_ARENA` | 1 B | 263 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 4 | `SMSG_FEATURE_SYSTEM_STATUS` | 949 B | 273 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 5 | `SMSG_WORLD_SERVER_INFO` | 11 B | 320 | System.IO.EndOfStreamException | `MiscellaneousHandler.HandleWorldServerInfo` |
| 6 | `SMSG_UPDATE_OBJECT` | 127078 B | 801 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 7 | `SMSG_GUILD_EVENT_LOG_QUERY_RESULTS` | 0 B | 865 | System.IO.EndOfStreamException | `GuildHandler.HandleGuildEventLogQueryResults` |
| 8 | `SMSG_GUILD_ROSTER` | 1504 B | 872 | System.ArgumentOutOfRangeException | `MythicPlusHandler.ReadDungeonScoreSummary` |
| 9 | `SMSG_GUILD_ROSTER` | 1504 B | 924 | System.ArgumentOutOfRangeException | `MythicPlusHandler.ReadDungeonScoreSummary` |
| 10 | `SMSG_WORLD_SERVER_INFO` | 11 B | 946 | System.IO.EndOfStreamException | `MiscellaneousHandler.HandleWorldServerInfo` |
| 11 | `SMSG_GUILD_EVENT_PRESENCE_CHANGE` | 8 B | 953 | System.IO.EndOfStreamException | `GuildHandler.HandleGuildEventPresenceChange` |
| 12 | `SMSG_UPDATE_OBJECT` | 30460 B | 960 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 13 | `SMSG_UPDATE_OBJECT` | 24437 B | 988 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 14 | `SMSG_UPDATE_OBJECT` | 33757 B | 1044 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 15 | `SMSG_UPDATE_OBJECT` | 40811 B | 1083 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 16 | `SMSG_UPDATE_OBJECT` | 41392 B | 1115 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 17 | `SMSG_UPDATE_OBJECT` | 42814 B | 1149 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 18 | `SMSG_UPDATE_OBJECT` | 32171 B | 1188 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 19 | `SMSG_UPDATE_OBJECT` | 38125 B | 1221 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 20 | `SMSG_UPDATE_OBJECT` | 30931 B | 1251 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 21 | `SMSG_UPDATE_OBJECT` | 50738 B | 1287 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 22 | `SMSG_UPDATE_OBJECT` | 43917 B | 1337 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 23 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 2125 B | 1489 | System.ArgumentOutOfRangeException | `QueryHandler.ReadNameCacheLookupResult` |
| 24 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 2356 B | 1549 | System.IO.EndOfStreamException | `QueryHandler.ReadNameCacheLookupResult` |
| 25 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 2359 B | 1556 | System.ArgumentOutOfRangeException | `QueryHandler.ReadNameCacheLookupResult` |
| 26 | `SMSG_UPDATE_OBJECT` | 43573 B | 1644 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 27 | `SMSG_QUERY_CREATURE_RESPONSE` | 140 B | 1739 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 28 | `SMSG_UPDATE_OBJECT` | 44320 B | 1742 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 29 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 2451 B | 1820 | System.IO.EndOfStreamException | `QueryHandler.ReadNameCacheLookupResult` |
| 30 | `SMSG_UPDATE_OBJECT` | 43609 B | 1856 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 31 | `SMSG_UPDATE_OBJECT` | 46344 B | 1927 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 32 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 2344 B | 1961 | System.ArgumentOutOfRangeException | `QueryHandler.ReadNameCacheLookupResult` |
| 33 | `SMSG_UPDATE_OBJECT` | 29078 B | 2022 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 34 | `SMSG_UPDATE_OBJECT` | 19323 B | 2114 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 35 | `SMSG_UPDATE_OBJECT` | 2262 B | 2291 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 36 | `SMSG_UPDATE_OBJECT` | 2283 B | 2497 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 37 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 2391 B | 2522 | System.ArgumentOutOfRangeException | `QueryHandler.ReadNameCacheLookupResult` |
| 38 | `SMSG_UPDATE_OBJECT` | 2311 B | 2587 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 39 | `SMSG_UPDATE_OBJECT` | 2248 B | 2833 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 40 | `SMSG_UPDATE_OBJECT` | 925 B | 2955 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 41 | `SMSG_QUERY_CREATURE_RESPONSE` | 130 B | 2997 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 42 | `SMSG_UPDATE_OBJECT` | 8564 B | 3098 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 43 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 156 B | 3204 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 44 | `SMSG_UPDATE_OBJECT` | 5587 B | 3530 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 45 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 76 B | 3614 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 46 | `SMSG_UPDATE_OBJECT` | 2257 B | 3780 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 47 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 72 B | 3909 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 48 | `SMSG_UPDATE_OBJECT` | 2364 B | 3978 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 49 | `SMSG_UPDATE_OBJECT` | 8779 B | 4008 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 50 | `SMSG_UPDATE_OBJECT` | 8767 B | 4535 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 51 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 81 B | 4819 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 52 | `SMSG_UPDATE_OBJECT` | 14670 B | 5054 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 53 | `SMSG_UPDATE_OBJECT` | 17309 B | 5092 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 54 | `SMSG_UPDATE_OBJECT` | 20704 B | 5616 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 55 | `SMSG_UPDATE_OBJECT` | 5704 B | 6189 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 56 | `SMSG_UPDATE_OBJECT` | 2318 B | 6504 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 57 | `SMSG_UPDATE_OBJECT` | 12298 B | 6715 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 58 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 386 B | 6875 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 59 | `SMSG_UPDATE_OBJECT` | 5875 B | 7322 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 60 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 151 B | 7369 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 61 | `SMSG_UPDATE_OBJECT` | 4655 B | 9128 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 62 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 79 B | 9224 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 63 | `SMSG_UPDATE_OBJECT` | 16920 B | 10231 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 64 | `SMSG_UPDATE_OBJECT` | 927 B | 10982 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 65 | `SMSG_QUERY_CREATURE_RESPONSE` | 133 B | 11042 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 66 | `SMSG_UPDATE_OBJECT` | 4101 B | 11172 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 67 | `SMSG_UPDATE_OBJECT` | 2315 B | 12228 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 68 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 82 B | 12358 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 69 | `SMSG_UPDATE_OBJECT` | 8874 B | 12660 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 70 | `SMSG_UPDATE_OBJECT` | 12104 B | 13122 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 71 | `SMSG_UPDATE_OBJECT` | 7184 B | 13651 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 72 | `SMSG_UPDATE_OBJECT` | 10343 B | 14247 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 73 | `SMSG_UPDATE_OBJECT` | 4593 B | 14748 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 74 | `SMSG_UPDATE_OBJECT` | 5029 B | 15731 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 75 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 219 B | 15832 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 76 | `SMSG_UPDATE_OBJECT` | 5475 B | 16383 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 77 | `SMSG_UPDATE_OBJECT` | 2238 B | 16551 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 78 | `SMSG_UPDATE_OBJECT` | 8204 B | 16846 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 79 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 153 B | 16963 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 80 | `SMSG_UPDATE_OBJECT` | 2377 B | 17119 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 81 | `SMSG_UPDATE_OBJECT` | 25393 B | 17342 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 82 | `SMSG_UPDATE_OBJECT` | 5211 B | 17387 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 83 | `SMSG_UPDATE_OBJECT` | 10434 B | 17907 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 84 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 73 B | 18023 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 85 | `SMSG_UPDATE_OBJECT` | 5599 B | 18473 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 86 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 232 B | 18579 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 87 | `SMSG_UPDATE_OBJECT` | 3188 B | 19092 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 88 | `SMSG_UPDATE_OBJECT` | 3162 B | 19112 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 89 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 154 B | 19268 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 90 | `SMSG_UPDATE_OBJECT` | 2443 B | 19469 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 91 | `SMSG_UPDATE_OBJECT` | 3625 B | 19793 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 92 | `SMSG_UPDATE_OBJECT` | 2990 B | 19904 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 93 | `SMSG_UPDATE_OBJECT` | 21207 B | 20296 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 94 | `SMSG_UPDATE_OBJECT` | 7906 B | 20326 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 95 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 365 B | 20489 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 96 | `SMSG_UPDATE_OBJECT` | 7984 B | 20921 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 97 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 295 B | 21140 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 98 | `SMSG_UPDATE_OBJECT` | 18523 B | 21584 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 99 | `SMSG_UPDATE_OBJECT` | 2376 B | 21831 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 100 | `SMSG_UPDATE_OBJECT` | 8112 B | 22068 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 101 | `SMSG_UPDATE_OBJECT` | 2266 B | 22185 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 102 | `SMSG_UPDATE_OBJECT` | 5441 B | 22565 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 103 | `SMSG_UPDATE_OBJECT` | 6693 B | 22620 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 104 | `SMSG_UPDATE_OBJECT` | 3289 B | 22829 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 105 | `SMSG_UPDATE_OBJECT` | 4540 B | 23175 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 106 | `SMSG_UPDATE_OBJECT` | 10556 B | 23192 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 107 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 146 B | 23321 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 108 | `SMSG_UPDATE_OBJECT` | 2309 B | 24103 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 109 | `SMSG_UPDATE_OBJECT` | 3173 B | 24249 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 110 | `SMSG_UPDATE_OBJECT` | 2259 B | 24275 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 111 | `SMSG_UPDATE_OBJECT` | 6094 B | 24457 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 112 | `SMSG_UPDATE_OBJECT` | 2434 B | 24594 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 113 | `SMSG_AUCTION_LIST_BUCKETS_RESULT` | 14123 B | 24759 | System.IO.EndOfStreamException | `AuctionHouseHandler.ReadBucketInfo` |
| 114 | `SMSG_UPDATE_OBJECT` | 2247 B | 24822 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 115 | `SMSG_UPDATE_OBJECT` | 2229 B | 24843 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 116 | `SMSG_UPDATE_OBJECT` | 4194 B | 25059 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 117 | `SMSG_UPDATE_OBJECT` | 3978 B | 25442 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 118 | `SMSG_UPDATE_OBJECT` | 2269 B | 25537 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 119 | `SMSG_UPDATE_OBJECT` | 12068 B | 25553 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 120 | `SMSG_UPDATE_OBJECT` | 2242 B | 25603 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 121 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 133 B | 25652 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 122 | `SMSG_UPDATE_OBJECT` | 2272 B | 25832 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 123 | `SMSG_UPDATE_OBJECT` | 3225 B | 25952 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 124 | `SMSG_UPDATE_OBJECT` | 2262 B | 26223 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 125 | `SMSG_UPDATE_OBJECT` | 19045 B | 26259 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 126 | `SMSG_UPDATE_OBJECT` | 7395 B | 26598 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 127 | `SMSG_UPDATE_OBJECT` | 9278 B | 26969 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 128 | `SMSG_UPDATE_OBJECT` | 3184 B | 27094 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 129 | `SMSG_UPDATE_OBJECT` | 2264 B | 27153 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 130 | `SMSG_UPDATE_OBJECT` | 2272 B | 27350 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 131 | `SMSG_UPDATE_OBJECT` | 2356 B | 27889 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 132 | `SMSG_UPDATE_OBJECT` | 5132 B | 28051 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 133 | `SMSG_UPDATE_OBJECT` | 3410 B | 28188 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 134 | `SMSG_UPDATE_OBJECT` | 2283 B | 28267 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 135 | `SMSG_UPDATE_OBJECT` | 1011 B | 28689 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 136 | `SMSG_UPDATE_OBJECT` | 2255 B | 29408 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 137 | `SMSG_UPDATE_OBJECT` | 2310 B | 29487 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 138 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 75 B | 29646 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 139 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 149 B | 29648 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 140 | `SMSG_UPDATE_OBJECT` | 3229 B | 29811 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 141 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 29989 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 142 | `SMSG_UPDATE_OBJECT` | 4309 B | 30005 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 143 | `SMSG_UPDATE_OBJECT` | 2793 B | 30151 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 144 | `SMSG_UPDATE_OBJECT` | 5084 B | 30717 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 145 | `SMSG_UPDATE_OBJECT` | 15542 B | 31276 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 146 | `SMSG_UPDATE_OBJECT` | 1355 B | 31932 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 147 | `SMSG_UPDATE_OBJECT` | 2301 B | 32340 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 148 | `SMSG_UPDATE_OBJECT` | 4620 B | 32493 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 149 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 87 B | 32608 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 150 | `SMSG_UPDATE_OBJECT` | 3347 B | 32991 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 151 | `SMSG_UPDATE_OBJECT` | 3380 B | 33480 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 152 | `SMSG_UPDATE_OBJECT` | 4539 B | 34097 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 153 | `SMSG_UPDATE_OBJECT` | 3515 B | 34641 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 154 | `SMSG_QUERY_CREATURE_RESPONSE` | 139 B | 34701 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 155 | `SMSG_UPDATE_OBJECT` | 7164 B | 35110 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 156 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 143 B | 35210 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 157 | `SMSG_UPDATE_OBJECT` | 5539 B | 35554 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 158 | `SMSG_UPDATE_OBJECT` | 4673 B | 36053 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 159 | `SMSG_UPDATE_OBJECT` | 4602 B | 36635 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 160 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 73 B | 36707 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 161 | `SMSG_UPDATE_OBJECT` | 1048 B | 37016 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 162 | `SMSG_UPDATE_OBJECT` | 2603 B | 37054 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 163 | `SMSG_UPDATE_OBJECT` | 2269 B | 37393 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 164 | `SMSG_UPDATE_OBJECT` | 2338 B | 37490 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 165 | `SMSG_QUERY_CREATURE_RESPONSE` | 135 B | 37687 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 166 | `SMSG_UPDATE_OBJECT` | 7873 B | 37934 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 167 | `SMSG_UPDATE_OBJECT` | 9675 B | 38547 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 168 | `SMSG_UPDATE_OBJECT` | 2304 B | 39078 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 169 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 83 B | 39173 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 170 | `SMSG_UPDATE_OBJECT` | 2298 B | 39950 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 171 | `SMSG_UPDATE_OBJECT` | 2301 B | 40490 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 172 | `SMSG_UPDATE_OBJECT` | 3238 B | 41034 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 173 | `SMSG_UPDATE_OBJECT` | 1123 B | 41543 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 174 | `SMSG_UPDATE_OBJECT` | 2284 B | 42040 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 175 | `SMSG_UPDATE_OBJECT` | 3246 B | 42591 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 176 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 81 B | 42664 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 177 | `SMSG_UPDATE_OBJECT` | 3182 B | 43012 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 178 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 69 B | 43093 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 179 | `SMSG_UPDATE_OBJECT` | 3613 B | 43167 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 180 | `SMSG_UPDATE_OBJECT` | 6374 B | 44718 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 181 | `SMSG_UPDATE_OBJECT` | 3198 B | 45229 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 182 | `SMSG_QUERY_CREATURE_RESPONSE` | 134 B | 45422 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 183 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 228 B | 45463 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 184 | `SMSG_UPDATE_OBJECT` | 2370 B | 46363 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 185 | `SMSG_UPDATE_OBJECT` | 3506 B | 46432 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 186 | `SMSG_UPDATE_OBJECT` | 3168 B | 46538 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 187 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 74 B | 46819 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 188 | `SMSG_UPDATE_OBJECT` | 2289 B | 46889 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 189 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 76 B | 46999 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 190 | `SMSG_UPDATE_OBJECT` | 2290 B | 47112 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 191 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 47317 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 192 | `SMSG_UPDATE_OBJECT` | 2280 B | 47339 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 193 | `SMSG_UPDATE_OBJECT` | 893 B | 47413 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 194 | `SMSG_UPDATE_OBJECT` | 2318 B | 47551 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 195 | `SMSG_UPDATE_OBJECT` | 3244 B | 47677 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 196 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 224 B | 47724 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 197 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 153 B | 47921 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 198 | `SMSG_UPDATE_OBJECT` | 8822 B | 48130 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 199 | `SMSG_UPDATE_OBJECT` | 2251 B | 48169 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 200 | `SMSG_UPDATE_OBJECT` | 2280 B | 48573 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 201 | `SMSG_UPDATE_OBJECT` | 2354 B | 48665 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 202 | `SMSG_UPDATE_OBJECT` | 3321 B | 48714 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 203 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 222 B | 48831 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 204 | `SMSG_UPDATE_OBJECT` | 2251 B | 49002 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 205 | `SMSG_UPDATE_OBJECT` | 2258 B | 49327 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 206 | `SMSG_UPDATE_OBJECT` | 4967 B | 50736 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 207 | `SMSG_UPDATE_OBJECT` | 3324 B | 51212 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 208 | `SMSG_UPDATE_OBJECT` | 4714 B | 51679 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 209 | `SMSG_UPDATE_OBJECT` | 1530 B | 52743 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 210 | `SMSG_UPDATE_OBJECT` | 2780 B | 53512 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 211 | `SMSG_UPDATE_OBJECT` | 2263 B | 54104 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 212 | `SMSG_UPDATE_OBJECT` | 2315 B | 54398 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 213 | `SMSG_UPDATE_OBJECT` | 3808 B | 54795 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 214 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 80 B | 54882 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 215 | `SMSG_UPDATE_OBJECT` | 3870 B | 55253 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 216 | `SMSG_UPDATE_OBJECT` | 5600 B | 55772 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 217 | `SMSG_UPDATE_OBJECT` | 14942 B | 56303 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 218 | `SMSG_UPDATE_OBJECT` | 18662 B | 57479 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 219 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 78 B | 57643 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 220 | `SMSG_UPDATE_OBJECT` | 2410 B | 57923 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 221 | `SMSG_UPDATE_OBJECT` | 11292 B | 58465 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 222 | `SMSG_UPDATE_OBJECT` | 10354 B | 59014 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 223 | `SMSG_UPDATE_OBJECT` | 9763 B | 59477 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 224 | `SMSG_UPDATE_OBJECT` | 2633 B | 60785 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 225 | `SMSG_UPDATE_OBJECT` | 3327 B | 61365 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 226 | `SMSG_UPDATE_OBJECT` | 2280 B | 61880 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 227 | `SMSG_UPDATE_OBJECT` | 5738 B | 62336 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 228 | `SMSG_UPDATE_OBJECT` | 2325 B | 62799 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 229 | `SMSG_UPDATE_OBJECT` | 3137 B | 63307 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 230 | `SMSG_QUERY_QUEST_INFO_RESPONSE` | 1107 B | 63641 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 231 | `SMSG_UPDATE_OBJECT` | 3141 B | 63796 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 232 | `SMSG_UPDATE_OBJECT` | 880 B | 65376 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 233 | `SMSG_UPDATE_OBJECT` | 2304 B | 65899 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 234 | `SMSG_UPDATE_OBJECT` | 2335 B | 66405 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 235 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 81 B | 66567 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 236 | `SMSG_UPDATE_OBJECT` | 2245 B | 66919 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 237 | `SMSG_UPDATE_OBJECT` | 2292 B | 67029 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 238 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 67143 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 239 | `SMSG_UPDATE_OBJECT` | 2268 B | 67218 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 240 | `SMSG_UPDATE_OBJECT` | 2258 B | 67380 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 241 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 75 B | 67913 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 242 | `SMSG_UPDATE_OBJECT` | 2243 B | 68212 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 243 | `SMSG_UPDATE_OBJECT` | 2244 B | 68353 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 244 | `SMSG_UPDATE_OBJECT` | 3641 B | 68533 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 245 | `SMSG_UPDATE_OBJECT` | 13562 B | 68958 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 246 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 156 B | 69065 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 247 | `SMSG_UPDATE_OBJECT` | 2251 B | 69329 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 248 | `SMSG_UPDATE_OBJECT` | 3182 B | 69558 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 249 | `SMSG_UPDATE_OBJECT` | 2275 B | 69976 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 250 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 73 B | 70063 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 251 | `SMSG_UPDATE_OBJECT` | 2364 B | 70110 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 252 | `SMSG_UPDATE_OBJECT` | 3858 B | 71118 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 253 | `SMSG_UPDATE_OBJECT` | 2264 B | 72286 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 254 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 147 B | 72426 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 255 | `SMSG_UPDATE_OBJECT` | 3313 B | 72943 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 256 | `SMSG_UPDATE_OBJECT` | 2357 B | 73425 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 257 | `SMSG_UPDATE_OBJECT` | 3299 B | 75136 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 258 | `SMSG_UPDATE_OBJECT` | 2243 B | 76429 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 259 | `SMSG_UPDATE_OBJECT` | 7234 B | 76647 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 260 | `SMSG_UPDATE_OBJECT` | 10103 B | 77745 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 261 | `SMSG_UPDATE_OBJECT` | 5506 B | 78330 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 262 | `SMSG_UPDATE_OBJECT` | 2278 B | 78743 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 263 | `SMSG_UPDATE_OBJECT` | 2335 B | 78888 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 264 | `SMSG_UPDATE_OBJECT` | 3231 B | 79300 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 265 | `SMSG_UPDATE_OBJECT` | 2248 B | 80422 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 266 | `SMSG_UPDATE_OBJECT` | 4535 B | 81308 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 267 | `SMSG_UPDATE_OBJECT` | 2483 B | 82221 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 268 | `SMSG_UPDATE_OBJECT` | 2393 B | 82779 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 269 | `SMSG_UPDATE_OBJECT` | 4150 B | 83202 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 270 | `SMSG_UPDATE_OBJECT` | 13841 B | 84391 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 271 | `SMSG_UPDATE_OBJECT` | 2264 B | 84649 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 272 | `SMSG_UPDATE_OBJECT` | 6032 B | 84909 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 273 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 78 B | 85051 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 274 | `SMSG_UPDATE_OBJECT` | 7679 B | 85427 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 275 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 148 B | 85559 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 276 | `SMSG_UPDATE_OBJECT` | 2844 B | 86383 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 277 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 78 B | 86462 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 278 | `SMSG_UPDATE_OBJECT` | 15765 B | 86926 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 279 | `SMSG_UPDATE_OBJECT` | 6570 B | 87854 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 280 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 75 B | 87916 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 281 | `SMSG_UPDATE_OBJECT` | 4492 B | 88302 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 282 | `SMSG_UPDATE_OBJECT` | 2267 B | 90123 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 283 | `SMSG_UPDATE_OBJECT` | 4415 B | 90668 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 284 | `SMSG_UPDATE_OBJECT` | 2696 B | 91217 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 285 | `SMSG_UPDATE_OBJECT` | 3635 B | 91837 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 286 | `SMSG_UPDATE_OBJECT` | 3727 B | 92477 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 287 | `SMSG_UPDATE_OBJECT` | 2311 B | 93031 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 288 | `SMSG_UPDATE_OBJECT` | 7865 B | 96677 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 289 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 96749 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 290 | `SMSG_UPDATE_OBJECT` | 3341 B | 97487 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 291 | `SMSG_UPDATE_OBJECT` | 998 B | 97872 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 292 | `SMSG_UPDATE_OBJECT` | 947 B | 98024 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 293 | `SMSG_QUERY_CREATURE_RESPONSE` | 127 B | 98063 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 294 | `SMSG_UPDATE_OBJECT` | 5167 B | 98397 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 295 | `SMSG_UPDATE_OBJECT` | 2731 B | 98907 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 296 | `SMSG_UPDATE_OBJECT` | 2332 B | 99392 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 297 | `SMSG_UPDATE_OBJECT` | 7739 B | 99825 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 298 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 74 B | 99939 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 299 | `SMSG_UPDATE_OBJECT` | 3727 B | 100402 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 300 | `SMSG_UPDATE_OBJECT` | 3038 B | 100798 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 301 | `SMSG_UPDATE_OBJECT` | 1865 B | 101378 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 302 | `SMSG_UPDATE_OBJECT` | 4441 B | 101690 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 303 | `SMSG_QUERY_CREATURE_RESPONSE` | 131 B | 101728 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 304 | `SMSG_UPDATE_OBJECT` | 2546 B | 102058 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 305 | `SMSG_UPDATE_OBJECT` | 2312 B | 102240 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 306 | `SMSG_UPDATE_OBJECT` | 2299 B | 102470 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 307 | `SMSG_UPDATE_OBJECT` | 3153 B | 102629 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 308 | `SMSG_UPDATE_OBJECT` | 2861 B | 102711 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 309 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 102775 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 310 | `SMSG_UPDATE_OBJECT` | 5618 B | 102950 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 311 | `SMSG_UPDATE_OBJECT` | 2271 B | 102958 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 312 | `SMSG_UPDATE_OBJECT` | 3161 B | 103080 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 313 | `SMSG_UPDATE_OBJECT` | 2270 B | 103176 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 314 | `SMSG_UPDATE_OBJECT` | 7546 B | 103207 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 315 | `SMSG_QUERY_CREATURE_RESPONSE` | 131 B | 103237 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 316 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 152 B | 103251 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 317 | `SMSG_UPDATE_OBJECT` | 2232 B | 103298 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 318 | `SMSG_UPDATE_OBJECT` | 5443 B | 103410 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 319 | `SMSG_UPDATE_OBJECT` | 3174 B | 103419 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 320 | `SMSG_UPDATE_OBJECT` | 3155 B | 103445 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 321 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 80 B | 103475 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 322 | `SMSG_UPDATE_OBJECT` | 2274 B | 103489 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 323 | `SMSG_UPDATE_OBJECT` | 2979 B | 103523 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 324 | `SMSG_UPDATE_OBJECT` | 2801 B | 103649 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 325 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 103897 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 326 | `SMSG_UPDATE_OBJECT` | 2328 B | 104032 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 327 | `SMSG_UPDATE_OBJECT` | 2278 B | 104068 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 328 | `SMSG_UPDATE_OBJECT` | 2350 B | 104196 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 329 | `SMSG_UPDATE_OBJECT` | 6328 B | 104359 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 330 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 104392 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 331 | `SMSG_UPDATE_OBJECT` | 2509 B | 104490 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 332 | `SMSG_UPDATE_OBJECT` | 2701 B | 104632 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 333 | `SMSG_UPDATE_OBJECT` | 5760 B | 104747 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 334 | `SMSG_UPDATE_OBJECT` | 3278 B | 104890 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 335 | `SMSG_UPDATE_OBJECT` | 2393 B | 105031 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 336 | `SMSG_UPDATE_OBJECT` | 4308 B | 105244 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 337 | `SMSG_UPDATE_OBJECT` | 4200 B | 105367 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 338 | `SMSG_UPDATE_OBJECT` | 977 B | 105750 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 339 | `SMSG_QUERY_CREATURE_RESPONSE` | 120 B | 105769 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 340 | `SMSG_UPDATE_OBJECT` | 1129 B | 105857 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 341 | `SMSG_UPDATE_OBJECT` | 3406 B | 105985 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 342 | `SMSG_UPDATE_OBJECT` | 989 B | 106105 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 343 | `SMSG_UPDATE_OBJECT` | 4071 B | 106242 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 344 | `SMSG_UPDATE_OBJECT` | 2027 B | 106497 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 345 | `SMSG_UPDATE_OBJECT` | 899 B | 106709 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 346 | `SMSG_UPDATE_OBJECT` | 1085 B | 106846 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 347 | `SMSG_QUERY_CREATURE_RESPONSE` | 136 B | 106864 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 348 | `SMSG_UPDATE_OBJECT` | 3193 B | 107091 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 349 | `SMSG_UPDATE_OBJECT` | 2802 B | 107197 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 350 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 83 B | 107246 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 351 | `SMSG_UPDATE_OBJECT` | 2447 B | 107441 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 352 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 77 B | 107455 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 353 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 74 B | 107517 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 354 | `SMSG_UPDATE_OBJECT` | 1022 B | 107693 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 355 | `SMSG_UPDATE_OBJECT` | 2941 B | 107976 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 356 | `SMSG_UPDATE_OBJECT` | 2438 B | 108103 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 357 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 78 B | 108121 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 358 | `SMSG_UPDATE_OBJECT` | 2278 B | 108262 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 359 | `SMSG_UPDATE_OBJECT` | 3307 B | 108341 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 360 | `SMSG_UPDATE_OBJECT` | 2289 B | 108430 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 361 | `SMSG_UPDATE_OBJECT` | 1016 B | 108535 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 362 | `SMSG_UPDATE_OBJECT` | 3278 B | 108720 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 363 | `SMSG_UPDATE_OBJECT` | 975 B | 108969 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 364 | `SMSG_UPDATE_OBJECT` | 2283 B | 109121 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 365 | `SMSG_UPDATE_OBJECT` | 1229 B | 109301 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 366 | `SMSG_UPDATE_OBJECT` | 2304 B | 109373 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 367 | `SMSG_QUERY_CREATURE_RESPONSE` | 142 B | 109385 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 368 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 79 B | 109392 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 369 | `SMSG_QUERY_CREATURE_RESPONSE` | 133 B | 109460 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 370 | `SMSG_UPDATE_OBJECT` | 1315 B | 109496 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 371 | `SMSG_UPDATE_OBJECT` | 6389 B | 109548 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 372 | `SMSG_UPDATE_OBJECT` | 1001 B | 109623 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 373 | `SMSG_UPDATE_OBJECT` | 881 B | 109717 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 374 | `SMSG_UPDATE_OBJECT` | 4549 B | 109786 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 375 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 82 B | 109795 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 376 | `SMSG_UPDATE_OBJECT` | 3243 B | 109821 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 377 | `SMSG_UPDATE_OBJECT` | 5735 B | 109903 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 378 | `SMSG_UPDATE_OBJECT` | 1013 B | 109953 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 379 | `SMSG_UPDATE_OBJECT` | 1035 B | 109981 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 380 | `SMSG_UPDATE_OBJECT` | 1872 B | 110016 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 381 | `SMSG_UPDATE_OBJECT` | 907 B | 110139 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 382 | `SMSG_UPDATE_OBJECT` | 2271 B | 110180 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 383 | `SMSG_UPDATE_OBJECT` | 2276 B | 110367 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 384 | `SMSG_UPDATE_OBJECT` | 951 B | 110449 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 385 | `SMSG_UPDATE_OBJECT` | 3229 B | 110495 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 386 | `SMSG_UPDATE_OBJECT` | 4522 B | 110574 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 387 | `SMSG_UPDATE_OBJECT` | 2246 B | 110680 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 388 | `SMSG_UPDATE_OBJECT` | 2235 B | 110757 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 389 | `SMSG_UPDATE_OBJECT` | 2239 B | 110853 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 390 | `SMSG_UPDATE_OBJECT` | 5243 B | 111171 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 391 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 79 B | 111182 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 392 | `SMSG_UPDATE_OBJECT` | 2268 B | 111544 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 393 | `SMSG_UPDATE_OBJECT` | 2252 B | 111613 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 394 | `SMSG_UPDATE_OBJECT` | 1113 B | 111642 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 395 | `SMSG_UPDATE_OBJECT` | 2285 B | 111685 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 396 | `SMSG_UPDATE_OBJECT` | 16509 B | 111704 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 397 | `SMSG_UPDATE_OBJECT` | 1143 B | 111728 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 398 | `SMSG_UPDATE_OBJECT` | 5466 B | 111817 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 399 | `SMSG_UPDATE_OBJECT` | 6529 B | 111908 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 400 | `SMSG_UPDATE_OBJECT` | 3169 B | 111990 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 401 | `SMSG_UPDATE_OBJECT` | 4691 B | 112039 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 402 | `SMSG_UPDATE_OBJECT` | 3734 B | 112146 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 403 | `SMSG_UPDATE_OBJECT` | 1245 B | 112295 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 404 | `SMSG_UPDATE_OBJECT` | 911 B | 112345 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 405 | `SMSG_UPDATE_OBJECT` | 2258 B | 112435 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 406 | `SMSG_UPDATE_OBJECT` | 3279 B | 112487 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 407 | `SMSG_UPDATE_OBJECT` | 874 B | 112612 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 408 | `SMSG_UPDATE_OBJECT` | 3191 B | 112660 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 409 | `SMSG_UPDATE_OBJECT` | 2274 B | 112748 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 410 | `SMSG_UPDATE_OBJECT` | 2312 B | 112786 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 411 | `SMSG_UPDATE_OBJECT` | 2275 B | 112954 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 412 | `SMSG_UPDATE_OBJECT` | 5537 B | 113027 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 413 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 158 B | 113108 | System.IO.EndOfStreamException | `QueryHandler.ReadNameCacheLookupResult` |
| 414 | `SMSG_UPDATE_OBJECT` | 7892 B | 113166 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 415 | `SMSG_UPDATE_OBJECT` | 1129 B | 113464 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 416 | `SMSG_UPDATE_OBJECT` | 4596 B | 113526 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 417 | `SMSG_GUILD_EVENT_PRESENCE_CHANGE` | 8 B | 113724 | System.IO.EndOfStreamException | `GuildHandler.HandleGuildEventPresenceChange` |
| 418 | `SMSG_UPDATE_OBJECT` | 128456 B | 113817 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 419 | `SMSG_WORLD_SERVER_INFO` | 11 B | 114306 | System.IO.EndOfStreamException | `MiscellaneousHandler.HandleWorldServerInfo` |
| 420 | `SMSG_UPDATE_OBJECT` | 21889 B | 114329 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 421 | `SMSG_UPDATE_OBJECT` | 26851 B | 114341 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 422 | `SMSG_UPDATE_OBJECT` | 29440 B | 114385 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 423 | `SMSG_UPDATE_OBJECT` | 27763 B | 114403 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 424 | `SMSG_GUILD_ROSTER` | 1504 B | 114432 | System.ArgumentOutOfRangeException | `MythicPlusHandler.ReadDungeonScoreMapSummary` |
| 425 | `SMSG_QUERY_CREATURE_RESPONSE` | 122 B | 114576 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 426 | `SMSG_QUERY_CREATURE_RESPONSE` | 135 B | 114577 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 427 | `SMSG_QUERY_CREATURE_RESPONSE` | 140 B | 114578 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 428 | `SMSG_QUERY_CREATURE_RESPONSE` | 138 B | 114579 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 429 | `SMSG_QUERY_CREATURE_RESPONSE` | 154 B | 114580 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 430 | `SMSG_QUERY_CREATURE_RESPONSE` | 137 B | 114581 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 431 | `SMSG_QUERY_CREATURE_RESPONSE` | 163 B | 114582 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 432 | `SMSG_QUERY_CREATURE_RESPONSE` | 134 B | 114583 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 433 | `SMSG_QUERY_CREATURE_RESPONSE` | 124 B | 114584 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 434 | `SMSG_QUERY_CREATURE_RESPONSE` | 163 B | 114585 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 435 | `SMSG_QUERY_CREATURE_RESPONSE` | 141 B | 114586 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 436 | `SMSG_QUERY_CREATURE_RESPONSE` | 140 B | 114587 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 437 | `SMSG_QUERY_CREATURE_RESPONSE` | 134 B | 114588 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 438 | `SMSG_QUERY_CREATURE_RESPONSE` | 122 B | 114589 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 439 | `SMSG_QUERY_CREATURE_RESPONSE` | 121 B | 114590 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 440 | `SMSG_QUERY_CREATURE_RESPONSE` | 144 B | 114591 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 441 | `SMSG_QUERY_CREATURE_RESPONSE` | 129 B | 114592 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 442 | `SMSG_QUERY_CREATURE_RESPONSE` | 143 B | 114593 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 443 | `SMSG_QUERY_CREATURE_RESPONSE` | 122 B | 114594 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 444 | `SMSG_QUERY_CREATURE_RESPONSE` | 132 B | 114595 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 445 | `SMSG_QUERY_CREATURE_RESPONSE` | 132 B | 114596 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 446 | `SMSG_QUERY_CREATURE_RESPONSE` | 131 B | 114597 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 447 | `SMSG_QUERY_CREATURE_RESPONSE` | 160 B | 114598 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 448 | `SMSG_QUERY_CREATURE_RESPONSE` | 140 B | 114599 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 449 | `SMSG_QUERY_CREATURE_RESPONSE` | 142 B | 114600 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 450 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 820 B | 114745 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 451 | `SMSG_QUERY_CREATURE_RESPONSE` | 144 B | 114748 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 452 | `SMSG_UPDATE_OBJECT` | 2248 B | 114798 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 453 | `SMSG_UPDATE_OBJECT` | 2234 B | 115068 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 454 | `SMSG_UPDATE_OBJECT` | 6534 B | 115144 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 455 | `SMSG_QUERY_CREATURE_RESPONSE` | 123 B | 115166 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 456 | `SMSG_QUERY_CREATURE_RESPONSE` | 142 B | 115167 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 457 | `SMSG_QUERY_CREATURE_RESPONSE` | 144 B | 115168 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 458 | `SMSG_QUERY_CREATURE_RESPONSE` | 140 B | 115169 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 459 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 74 B | 115178 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 460 | `SMSG_UPDATE_OBJECT` | 1841 B | 115232 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 461 | `SMSG_QUERY_CREATURE_RESPONSE` | 148 B | 115241 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 462 | `SMSG_UPDATE_OBJECT` | 997 B | 115458 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 463 | `SMSG_UPDATE_OBJECT` | 2647 B | 115953 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 464 | `SMSG_QUERY_CREATURE_RESPONSE` | 138 B | 115964 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 465 | `SMSG_UPDATE_OBJECT` | 7099 B | 116032 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 466 | `SMSG_QUERY_CREATURE_RESPONSE` | 144 B | 116056 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 467 | `SMSG_QUERY_CREATURE_RESPONSE` | 141 B | 116057 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 468 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 83 B | 116061 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 469 | `SMSG_QUERY_CREATURE_RESPONSE` | 137 B | 116085 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 470 | `SMSG_UPDATE_OBJECT` | 2827 B | 116156 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 471 | `SMSG_QUERY_CREATURE_RESPONSE` | 143 B | 116169 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 472 | `SMSG_QUERY_CREATURE_RESPONSE` | 140 B | 116170 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 473 | `SMSG_UPDATE_OBJECT` | 3150 B | 116270 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 474 | `SMSG_QUERY_CREATURE_RESPONSE` | 144 B | 116293 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 475 | `SMSG_UPDATE_OBJECT` | 1864 B | 116386 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 476 | `SMSG_QUERY_CREATURE_RESPONSE` | 142 B | 116407 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |
| 477 | `SMSG_QUERY_QUEST_INFO_RESPONSE` | 1156 B | 116435 | System.IO.EndOfStreamException | `Packet.ReadValue` |
| 478 | `SMSG_UPDATE_OBJECT` | 2272 B | 117055 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 479 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 83 B | 117078 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 480 | `SMSG_UPDATE_OBJECT` | 2288 B | 117386 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 481 | `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 86 B | 117407 | System.IO.EndOfStreamException | `CharacterHandler.ReadPlayerGuidLookupData` |
| 482 | `SMSG_UPDATE_OBJECT` | 1750 B | 117901 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 483 | `SMSG_UPDATE_OBJECT` | 1798 B | 117986 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 484 | `SMSG_UPDATE_OBJECT` | 5083 B | 118030 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 485 | `SMSG_UPDATE_OBJECT` | 2298 B | 118267 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 486 | `SMSG_UPDATE_OBJECT` | 4059 B | 118383 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 487 | `SMSG_UPDATE_OBJECT` | 1035 B | 118441 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 488 | `SMSG_UPDATE_OBJECT` | 3157 B | 118456 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 489 | `SMSG_UPDATE_OBJECT` | 1023 B | 118613 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 490 | `SMSG_UPDATE_OBJECT` | 2500 B | 118685 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 491 | `SMSG_UPDATE_OBJECT` | 895 B | 118752 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 492 | `SMSG_UPDATE_OBJECT` | 2338 B | 119091 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Non-negative number required. (Parameter 'index') | — |
| 493 | `SMSG_UPDATE_OBJECT` | 4053 B | 119163 | SMSG_UPDATE_OBJECT: ArgumentOutOfRangeException: Index and length must refer to a location within the string. (Parameter 'length') | — |
| 494 | `SMSG_QUERY_CREATURE_RESPONSE` | 127 B | 119171 | System.IO.EndOfStreamException | `ForeverQueryCreatureHandler.ReadCreatureStats` |

## Details — "Packet not fully read" (packets OK, but with leftover bytes)

| opcode | wire | count | position / length / remaining (first 3) |
|---|---|---|---|
| `SMSG_UPDATE_OBJECT` | 0x5E0000 | 229 | 98980 / 127078 / **28098**<br>2241 / 30460 / **28219**<br>2750 / 24437 / **21687** |
| `SMSG_QUERY_PLAYER_NAMES_RESPONSE` | 0x660027 | 45 | 392 / 594 / **202**<br>2286 / 2369 / **83**<br>867 / 1271 / **404** |
| `SMSG_QUERY_CREATURE_RESPONSE` | 0x4B0006 | 2 | 129 / 163 / **34**<br>113 / 121 / **8** |
| `SMSG_PARTY_UPDATE` | 0x470097 | 10 | 139 / 150 / **11**<br>139 / 150 / **11**<br>139 / 150 / **11** |
| `SMSG_HEALTH_UPDATE` | 0x47017F | 6 | 16 / 161 / **145**<br>16 / 161 / **145**<br>16 / 161 / **145** |
| `SMSG_TITLE_LOST` | 0x470189 | 6 | 4 / 13 / **9**<br>4 / 13 / **9**<br>4 / 13 / **9** |
| `SMSG_UPDATE_ACCOUNT_DATA_COMPLETE` | 0x4701B7 | 4 | 10 / 170 / **160**<br>10 / 170 / **160**<br>10 / 170 / **160** |
| `SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN` | 0x470064 | 3 | 715 / 829 / **114**<br>715 / 829 / **114**<br>715 / 829 / **114** |
| `CMSG_RESURRECT_RESPONSE` | 0x4400AF | 3 | 9 / 37 / **28**<br>6 / 33 / **27**<br>9 / 37 / **28** |
| `SMSG_WHO` | 0x4C0002 | 3 | 2752 / 5321 / **2569**<br>2879 / 5600 / **2721**<br>2831 / 5329 / **2498** |
| `SMSG_GROUP_DECLINE` | 0x47023D | 2 | 2 / 12 / **10**<br>2 / 12 / **10** |
| `SMSG_AUTH_RESPONSE` | 0x470001 | 1 | 5 / 378 / **373** |
| `SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT` | 0x47020D | 1 | 54 / 59 / **5** |
| `SMSG_ACCOUNT_MOUNT_UPDATE` | 0x470047 | 1 | 14 / 31 / **17** |
| `SMSG_RATED_PVP_INFO` | 0x4D000F | 1 | 65 / 666 / **601** |
| `SMSG_REQUEST_PVP_REWARDS_RESPONSE` | 0x4D0014 | 1 | 177 / 310 / **133** |
| `CMSG_AUTOBANK_ITEM` | 0x410003 | 1 | 3 / 4 / **1** |
| `SMSG_THREAT_CLEAR` | 0x470191 | 1 | 13 / 26 / **13** |

## What these mean

- **Exception** = the packet is counted as an error; the parser stopped mid-way.
- **not-fully-read** = the packet parsed, but unread bytes were left at the end (a field the handler does not know about).
- `SMSG_UPDATE_OBJECT` is a single opcode but accounts for the bulk of both buckets — dominated by large batches of tens of kB.