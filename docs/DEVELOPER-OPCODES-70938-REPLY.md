# Reply to "Opcodes still unknown in WPP-full (build 70938)"

Source: our core's Classic opcode numbering for 70938 (same numbering as 70790) and our packet readers/writers.
Every layout below was checked byte by byte against the sample in your list.

How sure we are:
- **A** = our core has a reader/writer for it AND your sample decodes with it exactly (length and values fit). Safe.
- **B** = name only, from our numbering rules. Our core has no handler for it, so no layout from us. Probably right, not proven.
- **C** = we don't know it either.

Notation: `guid` = packed guid (low mask byte, high mask byte, then the set bytes). `bit` values are MSB first, flushed to the next byte before any byte read/write.

## 1. Your unnamed opcodes (30)

| Dir | Opcode | Name | Sure | Layout |
|---|---|---|---|---|
| C | 0x460008 | CMSG_SUSPEND_TOKEN_RESPONSE | A | u32 SequenceIndex |
| C | 0x3E01E7 | CMSG_OPEN_TRADESKILL_NPC | B | no handler (your sample: 2 zero bytes) |
| C | 0x460007 | CMSG_LOG_DISCONNECT | B | no handler in our core; retail: u32 Reason (your 0x0B fits) |
| C | 0x3E0266 | CMSG_UI_MAP_QUEST_LINES_REQUEST | A | i32 UiMapID (0x5AE = 1454) |
| C | 0x460003 | CMSG_AUTH_CONTINUED_SESSION | B | no reader in our core; retail: u64 DosResponse, u64 Key, 64-byte LocalChallenge+Digest (= your 80 bytes) |
| C | 0x46000A | CMSG_QUEUED_MESSAGES_END | A | u32 Timestamp. Note: our own table has ENABLE_NAGLE here, but ENABLE_NAGLE has no payload and your 4-byte timestamp fits QUEUED_MESSAGES_END, so we go with that |
| C | 0x3E014E | CMSG_REQUEST_PLAYED_TIME | A | bit TriggerScriptEvent |
| C | 0x3E008A | CMSG_AREA_TRIGGER | A | i32 AreaTriggerID, bit Entered, bit FromClient (sample: 4101, 1, 1) |
| C | 0x460000 | CMSG_SUSPEND_COMMS_ACK | B | no reader in our core; retail: u32 Serial, u32 Timestamp (fits) |
| S | 0x4C001C | SMSG_CHANNEL_NOTIFY_LEFT | A | 7 bits name length, bit Suspended, flush, i32 ChatChannelID, name (sample: 18, 1, 2, "Trade - English 41") |
| C | 0x440136 | CMSG_BATTLE_PAY_REQUEST_PRICE_INFO | B | no handler. See 0x440134 below |
| S | 0x470377 | unknown | C | |
| C | 0x440041 | CMSG_DF_GET_SYSTEM_INFO | A | bit Player, bit HasPartyIndex, flush, [u8 PartyIndex] |
| S | 0x470190 | SMSG_QUERY_TIME_RESPONSE | A | i64 CurrentTime (unix seconds) |
| C | 0x3E0194 | CMSG_REQUEST_INSTANCE_ENCOUNTER_EVENT_SYNC | B | no handler; your sample is one guid |
| C | 0x3F0040 | CMSG_TAXI_NODE_STATUS_QUERY | A | guid UnitGUID |
| C | 0x3F001F | CMSG_SET_SHEATHED | A | i32 CurrentSheathState, bit Animate |
| S | 0x470228 | unknown | C | |
| C | 0x44019A | CMSG_SERVER_VALIDATION_SIGNATURE_REQUEST | B | no handler (sent once at login, matches our captures) |
| C | 0x3E013D | unknown | C | (your sample is float 1.0) |
| C | 0x3E0031 | CMSG_VIOLENCE_LEVEL | A | i8 ViolenceLevel |
| S | 0x4C0005 | unknown | C | |
| S | 0x530036 | SMSG_GUILD_EVENT_PRESENCE_CHANGE | A | guid, u32 VirtualRealmAddress, 6 bits name length, 9 bits surname length, bit LoggedOn, flush, name, surname. Classic has the surname and no Mobile bit (sample: name 9 chars, surname 4, LoggedOn 1) |
| S | 0x4701BB | SMSG_LOAD_EQUIPMENT_SET | A | u32 count, then per set (empty in your sample) |
| S | 0x470059 | unknown | C | |
| S | 0x47019A | SMSG_AUCTION_HELLO_RESPONSE | A | guid Auctioneer, u32 PurchaseDeliveryDelay, u32 CancelDeliveryDelay, i32 AuctionHouseID, bit OpenForBusiness, flush |
| C | 0x3F008C | CMSG_GAME_OBJ_REPORT_USE | A | guid |
| C | 0x3E011E | CMSG_ATTACK_SWING | A | guid Victim |
| C | 0x3E0171 | CMSG_USE_ITEM | A | u8 PackSlot, u8 Slot, guid CastItem, then SpellCastRequest (same as CMSG_CAST_SPELL's) |
| S | 0x470191 | SMSG_LOG_XP_GAIN | A | guid Victim, i32 Original, u8 Reason, i32 Amount, float GroupBonus (sample: 88, 0, 44, 1.0) |

## 2. Your named opcodes with no structure (35)

Names you have right unless the row says otherwise.

| Dir | Opcode | Your name | Our view | Sure | Layout |
|---|---|---|---|---|---|
| S | 0x4701F3 | SMSG_ACTIVE_SCHEDULED_WORLD_STATE_INFO | same | B | no writer in our core |
| S | 0x470316 | SMSG_LATENCY_REPORT_PING | same | B | no writer in our core |
| C | 0x46000F | CMSG_QUERY_PLAYER_NAMES_FOR_COMMUNITY | same | B | no reader in our core |
| S | 0x470099 | SMSG_UPDATE_RECENT_PLAYER_GUIDS | same | B | |
| C | 0x440134 | CMSG_BATTLE_PAY_REQUEST_PRICE_INFO | **CMSG_SET_EXCLUDED_CHAT_CENSOR_SOURCES** | B | our numbering puts BATTLE_PAY_REQUEST_PRICE_INFO at 0x440136 (your unnamed one). Neither has a handler, so this one is not proven |
| S | 0x530027 | SMSG_GUILD_FLAGGED_FOR_RENAME | **SMSG_GUILD_BANK_REMAINING_WITHDRAW_MONEY** | A | i64 RemainingWithdrawMoney. FLAGGED_FOR_RENAME is 1 byte, your packets are 8; they follow CMSG_GUILD_BANK_REMAINING_WITHDRAW_MONEY_QUERY |
| C | 0x420064 | CMSG_MOVE_INIT_ACTIVE_MOVER_COMPLETE | same | A | u32 Ticks |
| S | 0x47035B | SMSG_CLASSIC_SOCIAL_WINDOW_OPEN_RESPONSE | same | B | |
| S | 0x600075 | SMSG_MOVE_INITIAL_OBJECT_UPDATE_COMPLETE | same | B | |
| C | 0x420083 | CMSG_MOVE_ENABLE_FULL_SPEED_PITCHING_ACK | same | B | |
| S | 0x530025 | SMSG_GUILD_CHANGE_NAME_RESULT | **SMSG_GUILD_BANK_QUERY_RESULTS** | A | u64 Money, i32 Tab, i32 WithdrawalsRemaining, u32 TabCount, u32 ItemCount, then tabs (i32 TabIndex, 7 bits name len, 9 bits icon len, flush, name, icon) and items, then bit FullUpdate. Your 25 bytes = empty bank + FullUpdate 1 |
| S | 0x47035E | SMSG_DELVES_ACCOUNT_DATA_ELEMENT_CHANGED | same | B | |
| S | 0x470019 | SMSG_REGIONWIDE_CHARACTER_RESTRICTIONS_DATA | same | B | |
| S | 0x470384 | SMSG_LAST_CATALOG_FETCH_RESPONSE | probably same | B | our derived table has a different name here, but your sample (one 8-byte unix time) fits LAST_CATALOG_FETCH_RESPONSE better, so keep yours |
| S | 0x470327 | SMSG_SOCIAL_CONTRACT_REQUEST_RESPONSE | same | A | bit ShowSocialContract, flush |
| S | 0x470364 | SMSG_RECENT_ALLY_DATA_RESPONSE | same | B | |
| S | 0x4702DB | SMSG_BATCH_PRESENCE_SUBSCRIPTION | same | B | |
| S | 0x47001A | SMSG_REGIONWIDE_CHARACTER_MAIL_DATA | same | B | |
| S | 0x4702C9 | SMSG_GENERATE_SSO_TOKEN_RESPONSE | same | A | u32 RequestID, u32 Result, i64 Issued, i64 Expires, then the token bytes to the end of the packet (no length field) |
| S | 0x470300 | SMSG_SYNC_WOW_ENTITLEMENTS | same | B | |
| C | 0x4400E6 | CMSG_LIVE_REGION_CHARACTER_COPY | **CMSG_GET_ACCOUNT_CHARACTER_LIST** | B | no handler, not proven |
| C | 0x440153 | CMSG_RAF_UPDATE_RECRUITMENT_INFO | **CMSG_CLUB_FINDER_REQUEST_SUBSCRIBED_CLUB_POSTING_IDS** | B | no handler, not proven |
| S | 0x4702ED | SMSG_RAF_ACCOUNT_INFO | same | B | |
| S | 0x650007 | SMSG_PERKS_ANIM_TOGGLE_KILL_SWITCH | same | B | |
| S | 0x470066 | SMSG_PAST_TIME_EVENTS | same | B | |
| S | 0x470304 | SMSG_AUCTION_FAVORITE_LIST | same | A | u32 DesiredDelay, 7 bits count, flush, then count x favorite (empty in your sample) |
| S | 0x4702FE | SMSG_RAF_ACTIVITY_STATE_CHANGED | same | B | |
| S | 0x47030B | SMSG_WEEKLY_REWARDS_PROGRESS_RESULT | same | B | |
| C | 0x44019F | CMSG_BLEEP_PONG | **CMSG_RECENT_ALLY_REQUEST_DATA** | B | in our captures it comes right before SMSG_RECENT_ALLY_DATA_RESPONSE, once per login. Not proven |
| C | 0x3E02B3 | CMSG_PERKS_PROGRAM_REQUEST_REFUND | same | B | |
| S | 0x470347 | SMSG_RAF_DEBUG_FRIEND_MONTHS | same | B | |
| S | 0x470366 | SMSG_RECENT_ALLY_LIST | same | B | |
| C | 0x420024 | CMSG_MOVE_SET_TURN_RATE_CHEAT | same | B | |
| C | 0x3E0291 | CMSG_UPGRADE_RUNEFORGE_LEGENDARY | same | B | |
| S | 0x4702FB | SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT | same | A | u32 ItemCount, u32 DesiredDelay, items, bit HasMoreResults, flush (sample: 0, 300, 0) |

## Summary
- 25 now have a proven layout (A): 18 of your unnamed ones and 7 of the named ones.
- Renames we're sure of: 0x530027 = GUILD_BANK_REMAINING_WITHDRAW_MONEY, 0x530025 = GUILD_BANK_QUERY_RESULTS.
- Renames we think are right but can't prove: 0x440134/0x440136, 0x4400E6, 0x440153, 0x44019F.
- No idea: S 0x470377, 0x470228, 0x4C0005, 0x470059 and C 0x3E013D.
