# Question for dev — UPDATE_OBJECT on build 1.60.1.70959

The text below is ready to paste as-is.

---

## Full version (forum / GitHub issue)

Hi,

I'm working on a parser-side layout for client build **1.60.1.70959** (the build
currently live on the realm). I've reverse-engineered most of the
`SMSG_UPDATE_OBJECT` object block from sniffed bytes, but I'm stuck on one thing
and I'd rather ask than guess.

### What I measured (byte-verified, not guessed)

The packet header is unchanged from 12.x: `u16 map`, `u32 count`, a bitfield
(`IsEndOfBatch`, `HasRemovedObjects`), `u32 dataSize`. For a 115247-byte packet
with 47 objects, `dataSize == 115236`, i.e. exactly `len - 11`.

Each object starts with `u8 updateType`, then a packed GUID.

For `updateType == 0` (values update) the layout is confirmed three times over:

```
u8 type, packed GUID, u32 size, data[size]
```

`size` equals the remaining packet length exactly in all three packets I checked
(21/21, 38/38, 30/30), so this part is solid.

For `updateType == 2` (create) there is a change compared to 12.x. After the
packed GUID comes `u8 objectType`, and then a **7-byte block header** instead of
the 21+21 flag bits plus `u32 pauseTimesCount`. The first byte is a mask:

| mask | objectType |
|---|---|
| `0x84` | 5 (Unit) |
| `0x8C` | 7 |
| `0x81` | 8, 9 (GameObject, AreaTrigger) |

Bit 7 (`0x80`) is set in every sample I have; bit 2 (`0x04`) means the GUID is
repeated inside the block. When that bit is set, the block continues:

```
packed GUID (identical to the object's own GUID)
u64 flags64
u32 moveTime
float x, y, z
float orientation
float pitch
float stepUpStartElevation
i32 removeForcesCount
i32 moveIndex
float gravityModifier
```

I verified that `moveTime` grows monotonically across the sniff:

| packet | moveTime | delta |
|---|---|---|
| 48758 | 3970547540 | — |
| 48837 | 3970552826 | +5.3 s |
| 55056 | 3970748990 | +201 s |
| 122228 | 3972783427 | +2236 s |

The deltas match the packet spacing. I also verified that the position triple
lands inside valid map coordinates in all 20 packets where I could locate it
(480–2900 range; a wrong offset produces values like 1e27). So the movement block
is measured, and I'm confident in it.

### Where I'm stuck

After `gravityModifier` there is a `u32` that looks like the size of the update
field block, mirroring the `type == 0` case. In a single-object packet
(1055 bytes) that value is 16; in the 47-object packet it is 32, and the field
block then occupies `size` bytes.

But immediately after that block the next object does **not** start. In the
47-object packet:

```
offset  11  object[0] begins (updateType = 2)
offset  21  objectType = 7
offset  22  block header, mask 0x8C
offset  29  repeated GUID
offset  38  flags64
offset  46  moveTime
offset  50  x, y, z, orientation
offset  66  pitch, stepUp, removeForces, moveIndex, gravityModifier
offset  86  u32 = 32
offset  90  update field block (32 bytes)
offset 122  end of object[0]'s field block
```

The next packed-GUID mask doesn't appear until offset 98843. That leaves roughly
98700 bytes of something I can't identify between the end of object[0]'s field
block and the next object.

So my question is narrow:

**What sits between the end of the update-field block and the next object in a
`create` block on b1.60.1.70959?**

My guesses are a movement update with a spline, an aura update list, or
per-object addon blocks — but I don't want to implement a guess. A wrong layout
here is worse than no layout, because it desyncs every following object in the
packet.

If it's easier for you than describing it, any of these would settle it:

1. The field order for `ObjectCreateBlock` on 70959 — what follows the update
   mask block, and whether its size is explicit or implied.
2. Two sniffed `create` packets of the **same** GUID at different moments.
   Comparing what changes tells me which bytes are time-dependent and which are
   static, and that pins the boundary.
3. Or just: is the `u32` after `gravityModifier` really a size, or is it
   something else entirely (a flag word, an object type)?

### Context

With the current 12.x layout the parser reads 99.629% of a ~128k-packet sniff of
this build and produces correct creature spawns. The remaining 279 errors are all
`SMSG_UPDATE_OBJECT`. I'm not asking you to fix the errors — I'm asking for the
one layout fact that lets me finish the block.

Thanks.

---

## Short version (Discord / GitHub issue title + body)

**Title:** `SMSG_UPDATE_OBJECT` create block on 1.60.1.70959 — what follows the update field block?

**Body:**

> Working on `SMSG_UPDATE_OBJECT` for b1.60.1.70959.
>
> I've byte-verified the create block up to and including the movement data:
> a 7-byte block header with a mask (`0x84` Unit, `0x8C` type 7, `0x81`
> GameObject/AreaTrigger) instead of the 12.x flag bits, then repeated GUID,
> `flags64`, `moveTime`, xyz, orientation, pitch, stepUp, removeForces,
> moveIndex, `gravityModifier`. The `type == 0` layout is also confirmed
> (`u32 size` + data, 3/3 packets).
>
> What I can't find: in a 47-object packet, object[0]'s update-field block ends
> at offset 122, but the next object doesn't start until offset ~98843.
>
> **What lies between the end of the field block and the next object in a
> `create` block on 70959?** Spline? Aura list? Addon blocks? And is the `u32`
> right after `gravityModifier` actually the field-block size, or something else?
>
> A wrong guess here desyncs every following object, so I'd rather ask.

---

## What not to write

- Don't state "probably a spline" as fact — it's a hypothesis, and the dev will
  spot it immediately.
- Don't ask for "the error to be fixed" — the question is about one thing: what
  lies between the field block and the next object.
- Don't attach the whole sniff in the first message. If they ask, then yes — but
  ideally just packet 837 (115 kB) on its own.
