# 70959 — reply to Karrz, and the one measurement that does not fit

**Status:** 99.629% / 278 packet errors / 563 creature rows on a live 70959 capture.
Your explanation of block 4 is right, and it led me to a second thing I had been
getting wrong all along. One number still does not add up.

---

## 1. Your block-4 list matches what I see

I re-read `UnitData` with the classic 70425+ numbering you gave — 128 group,
129 LookAtControllerID, 130 PerksVendorItemID, 131 TaxiNodesID, 132-134 the three
70425 int32, 135 GuildGUID, 136 FlightCapabilityID, 137 GlideEventSpeedDivisor,
138 DriveCapabilityID, 139/140 MaxHealthModifierFlat, 141 SilencedSchoolMask,
142 float, 143 list count, 144 CurrentAreaID, 145 combo target, 146/147, 148
NameplateDistanceMod, 149 AutoAttackRangeMod, 150 NameplateAttachToGUID,
151 AssistActionData — and with the order you gave (unknown bit + flush, then
GuildGUID, then the HasAssistActionData bit + flush, payload only on 151).

Two objects from `dump_1.60.1.70959_2026-11-18_04-12-11.pkt`, packet 1759:

```
[0]  UnitBlock 17301633   -> 0x01080081: bits 0, 7, 23 -> classic 128, 135, 151
[24] UnitBlock 3          -> bits 0, 1          -> classic 128, 129 (LookAtControllerID)
```

Both decode, both match your ordering. On `[24]` the parser reads
`LookAtControllerID: 338` and `HasAssistActionData: False` and the values look
plausible, so I am confident the numbering change is the right fix.

## 2. What I had wrong before your answer

Not the bit numbers — the **block boundary**. I was taking the byte after the
object GUID for the start of the field block. It is not: the field block is
preceded by a `u32` size, and I was reading that `u32` as an object type.

```
[0]  guid @12..24   u32 size @25 = 46   field block @29..74    (log: Length 46)  ok
[1]  guid @76..88   u32 size @89 = 17   field block @93..109   (log: Length 17)  ok
```

Both sizes now match the parser's own `Length` line, which is how I know the
offsets are right and not another guess. This is also why two earlier attempts of
mine were reading one byte off and made things worse.

## 3. The number that does not fit

Object `[1]`, a Pet, entry 165189, field block `@93..109`, all 17 bytes:

```
@93  ..96   00 00 03 20    bits + fragment CGObject + Tag_Unit
@97  ..100  00 00 00 10    UnitBlocksMask = 16          (log: 16)  ok
@101 ..104  81 00 00 00    UnitBlock = 129              (log: 129) ok
@105 ..109  00 90 1f 0f 00 the rest
```

`UnitBlock 129` = classic 128 and 135. Per your ordering that means:

- bit `Unknown70291` + flush — 1 bit, fine
- **GuildGUID** — a packed guid, and `@105` starts `00 90 1f 0f 00`
- bit `HasAssistActionData` + flush
- payload only if classic 151 is set — it is not

**Only 5 bytes are left for GuildGUID.** A packed guid needs 2 mask bytes plus one
byte per set bit. `90` as the second mask byte sets 3 bits, so `90 1f 0f 00 00` is
a mask byte plus two data bytes and then the block ends — `07 e0 c4 8e 66 6c`
sits *past* the block, at `@110`, which is the start of object `[2]`.

So either

- **the block really does end mid-guid** and `u32 size` does not cover the whole
  unit block (which would explain why the parser stops with 3 bytes left, and why
  `[0]` also stops short), or
- **GuildGUID is not written here at all** for a non-owned pet, and those 5 bytes
  are something else in block 4.

## 4. The question

**Does `updatefieldSize` include everything the unit block writes, or is the
block bit-padded and the guid can run past it?** Put another way: on this realm,
for a pet whose receiver is not the owner, is classic bit 135 (GuildGUID) ever
set — and if it is, does its packed guid count against the same `u32` size?

If the answer is "the size is authoritative and the guid cannot cross it", then
bit 135 is not what `[1]` carries and I should look at bits 148-151 instead. That
is a one-line answer and it closes the last open block.

## 5. Numbers, so you can see what is left

| capture | packets | errors | creature rows |
|---|---|---|---|
| 70959 18.11 | 127928 (99.629%) | 278 | 563 |
| 70959 19.11 | 38664 (99.416%) | 79 | 294 |
| 70938 15-32-39 | 492566 (99.901%) | 19 | 1281 |

"Updatefields not fully read" on 18.11: 12710, of which 2416 stop with exactly
3 bytes left — the case above.

## 6. What I tried, so you do not repeat it

| change | result |
|---|---|
| block 4 in your classic numbering, GuildGUID before the assist bit | **274 errors** (from 278), but 19.11 got worse: 108 (from 79) — reverted |
| treat the three `u32` before FlightCapabilityID as list counts | worse: 300 errors, 533 creature — reverted |
| `IsNewLayout` for 70959 | neutral: 278 vs 279 — kept |
| GameObject update bits 22-26 in server order | 941 fewer "not fully read" — kept (`ae95ac36d`) |

The first row is the interesting one: your numbering **does** improve the 18.11
capture, so it is closer to right than what I had. It regresses 19.11, which is
why I backed it out rather than ship half of it. Once the size question above is
answered I will finish it properly.

## 7. Method

Bytes, not guesses: `tools/find-packet.py`, `tools/walk-update-object.py`,
`tools/pair-field-map.py`. The `u32 size` is confirmed by the constraint
`offset_of_block + size == offset_of_next_object_guid`, which now holds on `[0]`
and `[1]` independently. `pair-field-map.py` over 11 create pairs found only 4
varying bytes in the whole 647-byte UnitData block, so the block is structurally
constant — the error is in field order, not content.
