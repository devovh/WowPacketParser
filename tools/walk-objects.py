"""Przechodzi obiekty w SMSG_UPDATE_OBJECT korzystajac z ZMIERZONEGO ukladu bloku ruchu.

Dla kazdego obiektu: type, guid, ObjectType, naglowek 7B, guid powtorzony,
flags64, MoveTime, pozycja. Potem zostaje blok UpdateFields - dlugosc bierzemy
z u32 tuz za pozycja i sprawdzamy, czy nastepny obiekt wypada logicznie.
"""
import struct
import sys


def unpack_guid(d, off, count):
    lo_mask, hi_mask = d[off], d[off + 1]
    off += 2
    lo_bytes = [i for i in range(8) if lo_mask & (1 << i)]
    hi_bytes = [i for i in range(8) if hi_mask & (1 << i)]
    lo = 0
    for k, bit in enumerate(lo_bytes):
        lo |= d[off + k] << (bit * 8)
    off += len(lo_bytes)
    hi = 0
    for k, bit in enumerate(hi_bytes):
        hi |= d[off + k] << (bit * 8)
    off += len(hi_bytes)
    return (hi, lo), off


def main():
    from pkt_dump import read_pkt
    path, idx = sys.argv[1], int(sys.argv[2])
    _, pkts = read_pkt(path)
    d = pkts[idx][2]
    count = struct.unpack_from("<I", d, 2)[0]
    off = 11
    print(f"pakiet {idx}: len={len(d)} count={count}")

    for n in range(count):
        start = off
        utype = d[off]; off += 1
        g, off = unpack_guid(d, off, 0)
        head = f"[{n:2d}] @{start:6d} type={utype} guid=0x{g[1]:X}{g[0]:016X}"
        if utype == 0:
            size = struct.unpack_from("<I", d, off)[0]; off += 4
            print(f"{head} VALUES size={size}")
            off += size
            continue
        objtype = d[off]; off += 1
        blockmask = d[off]
        hdr = d[off:off + 7]; off += 7
        has_guid = bool(blockmask & 0x04)
        if has_guid:
            g2, off = unpack_guid(d, off, 0)
            flags = struct.unpack_from("<Q", d, off)[0]; off += 8
            movetime = struct.unpack_from("<I", d, off)[0]; off += 4
            x, y, z, o = struct.unpack_from("<ffff", d, off); off += 16
            print(f"{head} CREATE objType={objtype} mask=0x{blockmask:02X} "
                  f"flags={flags} mt={movetime} pos=({x:.1f},{y:.1f},{z:.1f}) o={o:.2f} "
                  f"pozycja_konczy_sie@{off}")
            print(f"          guid powtorzony zgadza sie: {g2 == g}")
        else:
            print(f"{head} CREATE objType={objtype} mask=0x{blockmask:02X} (bez guid)")
        if n >= 6:
            print("... (stop po 7 obiektach)")
            return


main()
