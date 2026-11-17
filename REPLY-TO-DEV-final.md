# WPP-full → dev: your last answer applied (70938)

Thanks, that closes the list. Here is what happens with each of your answers (the commits are going in next, this is the plan I am executing right now):

- **The four names** are already on the right numbers, so nothing from patch 3 had to be applied — only the misleading `gen-opcodes-70888.js` comment is gone (the generator was never committed, it was a one-session helper).
- **`0x440126` = `CMSG_BATTLENET_REQUEST`**: our table had `CMSG_CLUB_PRESENCE_SUBSCRIBE` there — that was the pre-70755 number, as you said. Swapped, and a reader added: `u64 Type`, `u64 ObjectId`, `u32 Token`, `u32 DataSize`, then `DataSize` bytes of protobuf (written as hex, no protobuf parsing).
- **`0x470377`, `0x470228`, `0x4C0005`, `0x470059`**: left unnamed, and `SMSG_BATTLEFIELD_LIST` stays on `0x4C0005`. They go out as raw hex, as you suggested.
- **`CMSG_MOVE_ENABLE_FULL_SPEED_PITCHING_ACK` `0x420083`**: MovementInfo + `i32 AckIndex`, through the same `ReadMovementAck` as our other `MOVE_*_ACK` readers.
- **`SMSG_RECENT_ALLY_DATA_RESPONSE` `0x470364`**: `u32`, `u8 Valid`, `u32 Count`, then the entries (Count is 0 in every capture so far), and the trailing byte is read and marked `Unknown` instead of being left to throw "not fully read".
- **`SYNC_WOW_ENTITLEMENTS`, `RAF_ACCOUNT_INFO`, `PAST_TIME_EVENTS`, `WEEKLY_REWARDS_PROGRESS_RESULT`, `RAF_DEBUG_FRIEND_MONTHS`**: names confirmed, no layout — left as hex-only, with a note in the file that the server never sends them, so nobody in this repo guesses a layout for them later.
- **70862**: dropped as a target, since your server only speaks 70938. The old numbers stay in the docs as history, not as a work item.

Nothing else is waiting on you. If a name or a layout for those four unnamed server opcodes ever turns up on your side, send it and we add it — the slots are obvious in `Opcodes.cs`, and until then nothing in the parser guesses.
