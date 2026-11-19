# Reply to dev — ready to paste (EN)

## Short reply (Discord / forum)

Thanks, that settles it. A few confirmations and one note back.

**You were right and I was wrong on the header.** What I called a "7-byte block
header with a mask" is exactly what you said: 21 flag bits, MSB first, flushed to
a byte, then `u32 PauseTimesCount`. I was reading the bits LSB-first, which is why
`0x84` looked like "bits 2 and 7" to me instead of `HasEntityPosition` +
`MovementUpdate`.

I checked your bit order across 267 single-object create packets from a live
70959 sniff. It holds:

| mask | bits set (MSB first) | count |
|---|---|---|
| `0x84` | 0, 5 — HasEntityPosition + MovementUpdate | 134x objectType 5, 87x objectType 6 |
| `0x81` | 0, 7 — HasEntityPosition + Stationary | 24x objectType 8, 5x objectType 9 |
| `0x8C` | 0, 4, 5 — the above + ThisIsYou | player create |

Two things I had mislabelled, now clear: the "repeated GUID" is `MoverGUID` (first
field of the movement block, which happens to equal the object's own GUID in all
my samples), and the `u64` after it is `MoveFlags`.

**On "nothing follows the field block":** confirmed, my ~98k bytes of "unknown
block" were just the rest of object[0] — the spline movement block, movement
forces, the 9 speeds and the 17 adv-flying floats, none of which I had decoded
yet. Not a separate block. That was a bad reading on my part.

**Correction on realm 70, which I also asked about by implication:** you said
every client on that realm gets the 70425 tables plus both remaps (+1 from bit
140, +3 from bit 132), regardless of client build. My measurement agrees — 70938
only drops to 0 errors with the 70425 tables and both shifts. So I'm treating
70959 as identical to 70938 on the field side.

**Now the part I can't verify, and you flagged it yourself:** you wrote that the
`UnitData` positions from item 13 onward (the owner-only fields, the
`PassiveSpells`/`WorldEffects`/`ChannelObjects` entry lists near the end of
UnitData) are what your server writes, not a measurement of the live realm. I'll
take them as the server layout, but I won't present them as measured.

To be clear about what I'm still seeing: with the header now matching your list
exactly, my parse numbers didn't move at all — 99.629% of the sniff, 279 errors,
all of them `SMSG_UPDATE_OBJECT`. Creature spawns come out correctly (563 rows).
So the create-block header was never the cause; whatever is left is inside the
field data. That's consistent with your own caveat about UnitData.

If you ever get a 70959 capture, or happen to know whether the owner-only fields
are also gated on `ThisIsYou` for other players' creates, that would be the next
thing I'd want. No rush — and thanks again, this saved me from implementing a
guess.

---

## Ultra-short version (one message, Discord)

> Thanks — that settles it. You were right: my "7-byte header" is 21 bits MSB
> first + flush + `u32 PauseTimesCount`. I was reading LSB-first. Verified your
> order on 267 single-object create packets in a live 70959 sniff: `0x84` =
> HasEntityPosition+MovementUpdate (Unit), `0x81` = HasEntityPosition+Stationary
> (GO/AT), `0x8C` = +ThisIsYou. Also: the "repeated GUID" is `MoverGUID`, the
> `u64` is `MoveFlags`, and "nothing after the field block" is confirmed — my 98k
> of "unknown" was just the rest of object[0] (spline, forces, speeds, adv-flying).
>
> One thing to flag: with the header now matching your list exactly, my numbers
> didn't move — still 99.629%, 279 `SMSG_UPDATE_OBJECT` errors, 563 correct
> creature rows. So the header was never the cause; what's left is inside the
> field data, which lines up with your own caveat about the UnitData positions
> from item 13 onward being your server's layout rather than a live measurement.
>
> If you ever get a 70959 capture, I'd take it. Thanks again.

---

## Notes on tone (not for sending)

- Open with the concession, not with your own findings. You got the bit order
  wrong and he was right — say it in the first line and move on.
- Don't re-explain the whole measurement back to him; the table is enough, and it
  shows you actually tested rather than just accepted.
- The one thing worth pushing back on gently is his own caveat: his item 13+
  layout is not a measurement. Say you'll treat it as server layout, not as
  confirmed live bytes. That's honest and it's also protecting you later.
- Don't ask another question about the same block. Ask about the *next* thing
  (a 70959 capture, or the `ThisIsYou` gating) so the thread moves forward
  instead of circling.
