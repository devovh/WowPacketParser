# Reply 2 to "Opcodes still unknown in WPP-full (build 70938)" and "TODO-FULL-PARSER"

Source: our core's Classic opcode numbering for 70938 (the 70790 numbering, see the first reply) and our packet readers/writers.
Every sample in your list was checked by hand against the layout given here.

How sure we are:
- **A** = our core reads or writes this message on 70938 clients and your sample decodes with it exactly. Safe.
- **A-** = our core reads or writes it, but either your sample is empty or the feature is rarely used, so it is only partly proven.
- **G** = a guess from your sample bytes and lengths, plus the neighbouring numbers. Not proven.
- **B** = name only, from our numbering. Our core has no reader or writer, so we have no layout.
- **C** = we don't know it either.

Notation is the same as before: `guid` = packed guid (low mask byte, high mask byte, then the bytes that are set). Bits are MSB
first and flushed to the next byte before any byte read.

## 0. Something we have to correct: the connection group (client 0x4600xx)

Your two samples show that the whole client connection group uses **retail's index**. Our table had a +1 guess for
0x45000A..0x45000F (70245 numbers) that is wrong. The right names for 70938:

| Opcode | Name | Sure | Why |
|---|---|---|---|
| 0x460009 | CMSG_ENABLE_NAGLE | G | retail index 9, no data |
| 0x46000A | CMSG_QUEUED_MESSAGES_END | A | u32 Timestamp (your 4-byte sample, first reply) |
| 0x46000B | CMSG_LOG_STREAMING_ERROR | G | retail index 0xB |
| 0x46000D | CMSG_QUERY_PLAYER_NAME_BY_COMMUNITY_ID | G | retail index 0xD |
| 0x46000E | CMSG_QUERY_PLAYER_NAMES_FOR_COMMUNITY | G | retail index 0xE |
| 0x46000F | **CMSG_LATENCY_REPORT** (not QUERY_PLAYER_NAMES_FOR_COMMUNITY) | G, strong | see below |
| 0x460010 | CMSG_QUERY_PLAYER_NAMES | A | (already right) |

## 1. LATENCY_REPORT_PING / "QUERY_PLAYER_NAMES_FOR_COMMUNITY": the duplicate

Settled by the bytes, **G (strong)**. The server sends a list of timed entries and the client sends the same list back with one
entry of its own added. That is a latency report, so:

- S 0x470316 = **SMSG_LATENCY_REPORT_PING** (name right)
- C 0x46000F = **CMSG_LATENCY_REPORT** (rename)

Layout (both directions):
```
u32  Unknown            (0 in every sample)
u32  Count
Count x {
    u32  A              (0x5D0400B6 in every entry: looks like an address or host id)
    u32  B              (differs per entry: 0x2C70, 0x09, ...)
    u8   C
    u64  TimeMs         (unix time in milliseconds: 0x1A1D9DCF395 = 14 Nov 2026)
    u32  D
}                       21 bytes per entry
```
Lengths check: server 50 = 8 + 2 x 21, 218 = 8 + 10 x 21. Client 71 = 8 + 3 x 21, 239 = 8 + 11 x 21. The client's extra entry has
A = B = C = 0 and its own TimeMs. Our core has no reader or writer, so the field names are ours.

## 2. The 6 unnamed opcodes

| Dir | Opcode | Our view | Sure | Layout / notes |
|---|---|---|---|---|
| S | 0x530037 | **SMSG_GUILD_EVENT_STATUS_CHANGE** | A- | guid, bit AFK, bit DND, flush. Your 8 bytes = guid 06 E0 9D AC 8C 6C 08 + 00 (not AFK, not DND). Our numbering puts it here (0x530036 PRESENCE_CHANGE is the one before it); our writer is the retail one |
| C | 0x3E013D | **CMSG_SET_EMPOWER_MIN_HOLD_STAGE_PERCENT** | G | float. Your 1.0 is the client's default. The block from CANCEL_CHANNELLING (0x3E013A, proven) is retail + 2, which puts this name here. Our own table has it one lower (also a guess) |
| S | 0x4C0005 | Classic-only chat message | C (name) | Classic chat has two messages at indices 4 and 5 that retail does not have (from index 4 on, Classic = retail + 2). This is the second one. Payload u8 (0) |
| S | 0x470377 | unknown | C | no name in our numbering. Neighbours: 0x470375 SET_CTR_OPTIONS, 0x470378 REFETCH_TACT_KEYS |
| S | 0x470228 | unknown | C | no name in our numbering. Neighbours: 0x470227 BATTLE_PAY_COLLECTION_ITEM_DELIVERED, 0x470229 INSTANCE_SAVE_CREATED |
| S | 0x470059 | unknown | C | no name in our numbering. Neighbours: 0x470058 ACCOUNT_HOUSE_TYPE_COLLECTION_UPDATE, 0x47005A RUNE_REGEN_DEBUG. Bytes look like: bit (1), flush, u32 3, u32 Count 3, 3 x u32 (1, 2, 3), u8 0. G |

## 3. Named opcodes with no structure (28)

| Dir | Opcode | Name | Sure | Layout |
|---|---|---|---|---|
| S | 0x470316 | SMSG_LATENCY_REPORT_PING | G | section 1 |
| C | 0x46000F | **CMSG_LATENCY_REPORT** | G | section 1 |
| C | 0x440136 | CMSG_BATTLE_PAY_REQUEST_PRICE_INFO | B | u8 Type (retail layout; your 00 fits) |
| S | 0x47035B | SMSG_CLASSIC_SOCIAL_WINDOW_OPEN_RESPONSE | A | u32 (0). Answer to CMSG_CLASSIC_SOCIAL_WINDOW_OPEN (no data), sent when the Social window opens |
| S | 0x600075 | SMSG_MOVE_INITIAL_OBJECT_UPDATE_COMPLETE | G | guid MoverGUID, u32 (0) |
| C | 0x3E0194 | CMSG_REQUEST_INSTANCE_ENCOUNTER_EVENT_SYNC | G | guid (the player) |
| C | 0x420083 | CMSG_MOVE_ENABLE_FULL_SPEED_PITCHING_ACK | A- | the same as every other MOVE_*_ACK: MovementInfo, then i32 AckIndex. Our core reads it with the shared movement ack reader |
| S | 0x47035E | SMSG_DELVES_ACCOUNT_DATA_ELEMENT_CHANGED | G | i32 (22), u8 (0) |
| S | 0x470019 | SMSG_REGIONWIDE_CHARACTER_RESTRICTIONS_DATA | G | u32 Count, u8 (0), then Count x { guid Character, u32 (0), u32 (10) } |
| S | 0x47001A | SMSG_REGIONWIDE_CHARACTER_MAIL_DATA | G | same shape as the one above: u32 Count, u8, Count x { guid, u32, u32 } |
| S | 0x470384 | SMSG_LAST_CATALOG_FETCH_RESPONSE | G | i64 unix time (0x6AF71270 = Nov 2026) |
| S | 0x470364 | SMSG_RECENT_ALLY_DATA_RESPONSE | B | our core does not send it; we cannot read your 10 bytes |
| S | 0x4702DB | SMSG_BATCH_PRESENCE_SUBSCRIPTION | G | bit (1), flush, u32 Count (1), then Count x u64 |
| C | 0x44019A | CMSG_SERVER_VALIDATION_SIGNATURE_REQUEST | G | u32 (1), guid (the player) |
| S | 0x470300 | SMSG_SYNC_WOW_ENTITLEMENTS | B | starts with u32 15, u32 15. No layout from us |
| C | 0x4400E6 | CMSG_GET_ACCOUNT_CHARACTER_LIST | B | u32 Token, then one byte of bits (retail shape) |
| C | 0x440153 | CMSG_CLUB_FINDER_REQUEST_SUBSCRIBED_CLUB_POSTING_IDS | A- | u32 (1). Our Guild Finder answers it with SMSG_CLUB_FINDER_GET_CLUB_POSTING_IDS_RESPONSE: u32 Count, Count x { i64 ClubID, u32 PostingID, u32 Type } |
| S | 0x4702ED | SMSG_RAF_ACCOUNT_INFO | B | |
| S | 0x650007 | SMSG_PERKS_ANIM_TOGGLE_KILL_SWITCH | G | bits only (C0 = two bits set) |
| S | 0x470066 | SMSG_PAST_TIME_EVENTS | B | |
| S | 0x4702FE | SMSG_RAF_ACTIVITY_STATE_CHANGED | G | u32 (0) |
| S | 0x47030B | SMSG_WEEKLY_REWARDS_PROGRESS_RESULT | B | |
| C | 0x44019F | CMSG_RECENT_ALLY_REQUEST_DATA | A | u32 (0). Sent when the Allies tab opens; our core answers it with SMSG_RECENT_ALLY_LIST (below) and the tab fills |
| C | 0x3E02B3 | the TrinityCore name PERKS_PROGRAM_REQUEST_REFUND is wrong for Classic | A- | u8 RoleMask: the roles ticked in the Group Finder (2 tank, 4 healer, 8 damage). Your 08 = damage only. Our core reads it this way |
| S | 0x470347 | SMSG_RAF_DEBUG_FRIEND_MONTHS | B | |
| S | 0x470366 | SMSG_RECENT_ALLY_LIST | A | see below |
| C | 0x420024 | CMSG_MOVE_SET_TURN_RATE_CHEAT | G | float TurnRate (your DB 0F 49 40 = 3.14159) |
| C | 0x3E0291 | CMSG_UPGRADE_RUNEFORGE_LEGENDARY | B | u8 (0) |

SMSG_RECENT_ALLY_LIST (A, our writer; your 396-byte sample decodes with it):
```
u32 Count
Count x {
    guid  Ally
    u32, u32, u32           (0)
    u32   ZoneID            (the ally's zone when online, else 0)
    u32   InteractionCount
    InteractionCount x { u8 Type, i64 UnixTime, i32 Value, i32 (0) }
                            Type = the client's Enum.RolodexType: 1 party, 2 raid, 3 trade, 4 whisper, 11 creature kill
                            ("fought together"), 12 dungeon; Value = the zone it happened in (your first entry: 11, zone 17)
    u32   (0xC0000000 when online, else 0)
}
```

## 4. Names not proven (your 2.3)

| Opcode | Name | Sure | Why |
|---|---|---|---|
| C 0x440126 | **CMSG_BATTLENET_REQUEST** | A | In 70245 numbers this is 0x440124, and the 70755 client added two messages at 0x440071/0x440072, so it is +2 now. Our server answers in-game Battle.net calls on this number for 70938 clients. CLUB_PRESENCE_SUBSCRIBE is the old 0x440126 from before 70755 (now 0x440128). Layout: u64 Type, u64 ObjectID, u32 Token, u32 Size, then Size bytes of protobuf. If your packets fit that, it is settled |
| C 0x440134 | CMSG_SET_EXCLUDED_CHAT_CENSOR_SOURCES | B | our numbering. No handler, not proven |
| C 0x440136 | CMSG_BATTLE_PAY_REQUEST_PRICE_INFO | B | our numbering. No handler, not proven |
| C 0x4400E6 | CMSG_GET_ACCOUNT_CHARACTER_LIST | B | our numbering. No handler, not proven |
| C 0x440153 | CMSG_CLUB_FINDER_REQUEST_SUBSCRIBED_CLUB_POSTING_IDS | A- | our Guild Finder reads it and sends the posting ids back (layout in section 3); not checked field by field in game |
| C 0x44019F | CMSG_RECENT_ALLY_REQUEST_DATA | A | the Allies tab works with it on our server (section 3) |

## 5. Layouts (your 2.4)

### CMSG_USE_ITEM (0x3E0171): u8 PackSlot, u8 Slot, guid CastItem, SpellCastRequest
SpellCastRequest is the same as in CMSG_CAST_SPELL. **A**: our server reads every spell cast and item use with it.
```
guid  CastID
u8    SendCastFlags
i32   Misc[3]
i32   SpellID
i32   SpellXSpellVisualID
i32   ScriptVisualID
SpellTargetData:
    u32   Flags
    guid  Unit
    guid  Item
    guid  HousingGUID
    bit   HousingIsResident
    bit   HasSrcLocation
    bit   HasDstLocation
    bit   HasOrientation
    bit   HasMapID
    7 bits NameLength
    flush
    [SrcLocation: guid Transport, f32 X, f32 Y, f32 Z]
    [DstLocation: same]
    [f32 Orientation]
    [i32 MapID]
    Name
f32   MissileTrajectory.Pitch
f32   MissileTrajectory.Speed
guid  CraftingNPC
u32   ExtraCurrencyCount
u32   CraftingReagentCount
u32   RemovedReagentCount
u8    CraftingCastFlags
ExtraCurrencyCount x { i32 CurrencyID, i32 Count }
CraftingReagentCount x Reagent, then RemovedReagentCount x Reagent, where Reagent =
    { i32 Slot, i32 Quantity, flush, bit HasItemID, bit HasCurrencyID, [i32 ItemID], [i32 CurrencyID], bit HasSource, flush, [u8 Source] }
    (HasSource is the third bit of the same byte when neither ID is present; any i32 read starts a new byte)
flush
bit   HasReceiveTime
bit   HasMoveUpdate
2 bits WeightCount
bit   HasCraftingOrderID
flush
[u32  ReceiveTime (ms)]
[MovementInfo, the same as in CMSG_MOVE_*]
WeightCount x { flush, 2 bits Type, i32 ID, u32 Quantity }
[u64  CraftingOrderID]
```

### SMSG_GUILD_BANK_QUERY_RESULTS (0x530025), full (A-: our writer, which the guild bank uses; your capture only has the empty bank)
```
u64 Money, i32 Tab, i32 WithdrawalsRemaining, u32 TabCount, u32 ItemCount
TabCount x { i32 TabIndex, 7 bits NameLength, 9 bits IconLength, flush, Name, Icon }
ItemCount x {
    i32 Slot
    ItemInstance
    i32 Count, i32 EnchantmentID, i32 Charges, i32 OnUseEnchantmentID, i32 Flags
    2 bits GemCount, bit Locked, flush
    GemCount x { u8 Slot, ItemInstance }
}
bit FullUpdate, flush
```
ItemInstance (used everywhere, including your auction bucket extra block):
```
i32 ItemID
7 bits ModifierCount, flush, ModifierCount x { u8 Type, i32 Value }
bit HasBonus, flush
[u8 Context, u32 BonusListCount, BonusListCount x i32 BonusListID]
```
That gives the bucket block lengths you saw: u32 ItemID + 1 + 5 x Modifiers + 1 = 2, 7 or 12 bytes after the ItemID.

### SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT, non-empty (A-: our writer; your capture has only the empty list)
`u32 ItemCount, u32 DesiredDelay, ItemCount x AuctionItem, bit HasMoreResults, flush`
AuctionItem:
```
bit HasItem, 4 bits EnchantCount, 2 bits GemCount, bit HasMinBid, bit HasMinIncrement, bit HasBuyout, bit HasUnitPrice,
bit CensorServerSideInfo, bit CensorBidInfo, bit HasBucketKey, bit HasCreator,
if not CensorBidInfo: bit HasBidder, bit HasBidAmount
flush
if not CensorBidInfo: [guid Bidder], [u64 BidAmount]
i32 Count, i32 Charges, i32 Flags, i32 AuctionID, guid Owner, i32 DurationLeft, u8 DeleteReason, u32 (0)
[ItemInstance]
EnchantCount x { i32 ID, u32 Expiration, i32 Charges, u8 Slot }
GemCount x { u8 Slot, ItemInstance }
[u64 MinBid] [u64 MinIncrement] [u64 Buyout] [u64 UnitPrice]
if not CensorServerSideInfo: guid ItemGUID, guid OwnerAccountID, i32 EndTime
[BucketKey: 20 bits ItemID, bit HasPetSpecies, 11 bits ItemLevel, bit HasSuffix, flush, [u16 PetSpecies], [u16 Suffix]]
[guid Creator]
```

### SMSG_AUCTION_FAVORITE_LIST, non-empty (A-: our writer)
`u32 DesiredDelay, 7 bits Count, flush, Count x { u32 Order, u32 ItemID, u32 ItemLevel, u32 BattlePetSpeciesID, u32 SuffixItemNameDescriptionID }`

### SMSG_LOAD_EQUIPMENT_SET, non-empty (A-: our writer; not checked in game with saved sets)
```
u32 Count
Count x {
    i32 Type, u64 Guid, u32 SetID, u32 IgnoreMask
    19 x { guid Piece, i32 AppearanceID }
    2 x i32 Enchant
    i32 SecondaryShoulderAppearanceID, i32 SecondaryShoulderSlot, i32 SecondaryWeaponAppearanceID, i32 SecondaryWeaponSlot
    bit HasAssignedSpecIndex, 8 bits NameLength, 9 bits IconLength, flush
    [i32 AssignedSpecIndex]
    Name, Icon
}
```

### PvP
- SMSG_RATED_PVP_INFO: our writer is the one you have. 9 brackets of 18 x i32 (PersonalRating, Ranking, SeasonPlayed, SeasonWon,
  SeasonFactionPlayed, SeasonFactionWon, WeeklyPlayed, WeeklyWon, RoundsSeasonPlayed, RoundsSeasonWon, RoundsWeeklyPlayed,
  RoundsWeeklyWon, BestWeeklyRating, LastWeeksBestRating, BestSeasonRating, PvpTierID, SeasonPvpTier, BestWeeklyPvpTier), u8
  BestSeasonPvpTierEnum, bit Disqualified, flush = 74 bytes. The live length (666) proves the size, but not the field order: we
  send it all zeros because there is no rated PvP, so the order comes from retail and is unchecked. **A- for the size, G for the
  order.**
- SMSG_REQUEST_PVP_REWARDS_RESPONSE: **B**. Our core does not send it, so we can't name the last byte (0xF8).

## Summary
- Rename: C 0x46000F = CMSG_LATENCY_REPORT (G strong). The whole client connection group 0x4600xx has retail's index.
- Settled: C 0x440126 = CMSG_BATTLENET_REQUEST (A).
- New names: S 0x530037 GUILD_EVENT_STATUS_CHANGE (A-), C 0x3E013D SET_EMPOWER_MIN_HOLD_STAGE_PERCENT (G).
  S 0x4C0005 is a Classic-only chat message (no retail name).
- Still unknown: S 0x470377, 0x470228, 0x470059.
- Layouts given: LATENCY_REPORT(_PING), RECENT_ALLY_LIST, SpellCastRequest, ItemInstance (= your bucket block), full guild bank,
  bidded items, favorites, equipment sets, plus small byte guesses for the rest.
