# Reply to Karrz — round 2 (EN, ready to paste)

## Short reply (Discord)

Thanks — both answers are useful, and the gate explanation is clearer than what I
was assuming.

**On the owner-only gate:** that makes sense, and it's a better rule than "gated on
ThisIsYou". I tested it on my live 70959 capture and I don't think it's the cause
of what I'm seeing, though. In the capture I have 15 unit creates that carry
`MovementUpdate`, all with `MoverGUID == the object's own GUID`, and `MoveFlags` of
`0x1` (12x) or `0x0` (3x). Nothing in there stands out as an owned pet, guardian or
totem — the capture is mostly creature spawns in a starting zone, so it's probably
just too early to contain many. So I can't confirm or refute the gate from this
capture; I'll keep the rule in mind for when I do see one.

**What I can confirm is the bit layout.** Across the capture, the 21 bits decode
exactly as you listed them, every time:

| objectType | bits set | count |
|---|---|---|
| 5 (unit) | HasEntityPosition + MovementUpdate | 253 |
| 6 | HasEntityPosition + MovementUpdate | 168 |
| 8 (gameobject) | HasEntityPosition + Stationary + Rotation | 60 |
| 8 (gameobject) | the above + ServerTime + GameObject | 13 |
| 9, 10, 11 | HasEntityPosition + Stationary | 6, 4, 3 |

Zero deviations. Before your answer I was reading these LSB-first and had
concluded `0x84` meant "bits 2 and 7" — so thanks for the correction, it was
entirely mine.

**On "no we didn't update, haven't since making it":** that's the answer I was
actually after, and it fits my measurement — 70938 only reaches zero errors with
the 70425 tables and both remaps, and 70959 behaves identically on the field side.
So I'm treating the two builds as the same layout.

**On the capture:** no problem, and the reason is a good one — I hadn't thought
about account and character IDs and other players' chat being in there. My own
sniff is enough; I just needed to know what to read it against, which your answers
gave me.

Thanks again — I'd have implemented a wrong layout without this.

---

## Ultra-short version (one message)

> Both useful, thanks. **Owner-only gate:** makes sense, and it's a better rule than
> "gated on ThisIsYou". Can't confirm it from my capture though — 15 unit creates
> with `MovementUpdate`, all `MoverGUID == own GUID`, `MoveFlags` 0x1/0x0, none
> looking like an owned pet or summon (mostly starting-zone creature spawns). So
> it's not what's behind my remaining errors, but I'll keep it for when I see one.
>
> **Bit layout:** confirmed on the whole capture, zero deviations — objType 5/6 =
> HasEntityPosition+MovementUpdate (253+168), objType 8 = +Stationary+Rotation (60)
> or +ServerTime+GameObject (13), objType 9/10/11 = +Stationary. I was reading
> these LSB-first before, so `0x84` looked like "bits 2 and 7". My mistake, fixed.
>
> **"Haven't updated since making it"** was what I needed — matches my
> measurement, so I'm treating 70938 and 70959 as the same field layout.
>
> **Capture:** fair enough, and a good reason — I hadn't thought about account IDs
> and other players' chat. My own sniff is enough. Thanks again, I'd have
> implemented a wrong layout without this.

---

## Notes on tone (not for sending)

- He gave you something concrete (the gate rule) and something honest (no capture,
  with a real reason). Acknowledge both before moving on.
- Report the negative result plainly: your capture doesn't contain enough owned
  pets to test the gate. Don't dress "I couldn't check" up as "it's wrong".
- The bit table is the strongest thing you have — it's evidence you verified rather
  than just accepted, and it closes that thread for good.
- Don't ask a new question this round. You've got what you needed; the next move is
  yours (decoding UnitData), not his.
