"""Proby odczytu dlugosci pol bitowych SMSG_CHAT Forever.

Blok bitowy lezy 5 bajtow po polach stalych (po 5 pakowanych GUID-ach i 24
bajtach pol stalych). Bajt 0x40 na koncu bloku to dopełnienie FlushBits().
"""
import struct
import sys

PATH = sys.argv[1] if len(sys.argv) > 1 else (
    "C:/Users/devov/Desktop/regedit_70862/dumps/"
    "dump_1.60.1.70938_2026-11-13_14-02-23.pkt"
)


def load_chats(path):
    d = open(path, "rb").read()
    off = 72
    chats = []
    while off + 40 <= len(d):
        magic = struct.unpack_from("<I", d, off)[0]
        if magic not in (0x47534D53, 0x47534D43, 0x4E425F53):
            break
        off += 12
        add = struct.unpack_from("<i", d, off)[0]
        off += 4
        ln = struct.unpack_from("<i", d, off)[0]
        off += 4
        off += add
        op = struct.unpack_from("<i", d, off)[0]
        off += 4
        data = d[off:off + ln - 4]
        off += ln - 4
        if op == 0x4C0001:
            chats.append(data)
    return chats


def bitstart(x):
    p = 5
    for _ in range(5):
        lm, hm = x[p], x[p + 1]
        p += 2
        p += bin(lm).count("1") + bin(hm).count("1")
    return p + 24


def msb_bits(raw):
    return "".join(format(c, "08b") for c in raw)


def lsb_bits(raw):
    return "".join(format(c, "08b")[::-1] for c in raw)


def read_msb(bits, pos, n):
    return int(bits[pos:pos + n], 2)


def read_lsb(bits, pos, n):
    return int(bits[pos:pos + n][::-1], 2)


if __name__ == "__main__":
    for i, x in enumerate(load_chats(PATH)):
        p = bitstart(x)
        raw = x[p:p + 5]
        print(f"--- pakiet {i}  len={len(x)}  bitstart={p}")
        print(f"    blok: {raw.hex(' ')}")

        for label, bits, rd in (("MSB", msb_bits(raw), read_msb),
                                ("LSB", lsb_bits(raw), read_lsb)):
            pos = 0
            vals = []
            for n in (11, 11, 5, 7, 12):
                vals.append(rd(bits, pos, n))
                pos += n
            print(f"    {label}: s={vals[0]} t={vals[1]} p={vals[2]} "
                  f"c={vals[3]} x={vals[4]}")
