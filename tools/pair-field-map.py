"""Mapa zmiennosci bajtow w bloku pol (UnitData) create tego samego GUID.

Dla kazdego GUID creature z co najmniej dwoma create porownuje blok pol bajt
po bajcie i liczy, na ktorych pozycjach sie rozni. Blok pol jest lokalizowany
przez warunek offset + 4 + size == dlugosc pakietu (szukanie u32 size).
"""
import collections
import struct
import sys


def rd_guid(d, off):
    lm, hm = d[off], d[off + 1]
    off += 2
    for i in range(8):
        if lm & (1 << i):
            off += 1
    for i in range(8):
        if hm & (1 << i):
            off += 1
    return off


def collect(path, objtype=5):
    from pkt_dump import read_pkt
    _, pkts = read_pkt(path)
    out = collections.defaultdict(list)
    for idx, (dr, op, x) in enumerate(pkts):
        if op != 0x5E0000 or len(x) < 40:
            continue
        n = struct.unpack_from("<I", x, 2)[0]
        off = 11
        for _ in range(n):
            if off + 3 > len(x):
                break
            t = x[off]
            off += 1
            try:
                off2 = rd_guid(x, off)
            except IndexError:
                break
            gb = x[off:off2]
            off = off2
            if t == 0:
                if off + 4 > len(x):
                    break
                off += 4 + struct.unpack_from("<I", x, off)[0]
                continue
            if off + 1 > len(x):
                break
            ot = x[off]
            off += 1
            if ot != objtype:
                break
            found = None
            for probe in range(off, min(off + 260, len(x) - 4)):
                sz = struct.unpack_from("<I", x, probe)[0]
                if probe + 4 + sz == len(x):
                    found = (probe, sz)
                    break
            if found:
                p, sz = found
                out[gb].append((idx, x[p + 4:p + 4 + sz]))
            break
    return out


def main():
    files = sys.argv[1:]
    allrec = collections.defaultdict(list)
    for f in files:
        for k, v in collect(f).items():
            allrec[k].extend(v)

    pairs = [(k, v) for k, v in allrec.items() if len(v) >= 2]
    print(f"GUID z co najmniej dwoma create: {len(pairs)}")

    dc = collections.Counter()
    tot = 0
    lens = collections.Counter()
    for k, v in pairs:
        a, b = v[0][1], v[1][1]
        lens[len(a)] += 1
        if len(a) != len(b):
            continue
        tot += 1
        for i in range(len(a)):
            if a[i] != b[i]:
                dc[i] += 1
    print(f"par o rownej dlugosci: {tot}")
    print("dlugosci blokow:", dict(lens.most_common(8)))
    print()
    print("pozycje zmienne (posortowane po liczbie par):")
    for i, c in sorted(dc.items(), key=lambda kv: -kv[1])[:50]:
        print(f"  @{i:4d}: {c:3d}/{tot}")
    if dc:
        regs = []
        for i in sorted(dc):
            if regs and i == regs[-1][1] + 1:
                regs[-1][1] = i
            else:
                regs.append([i, i])
        print("regiony zmienne:", regs[:25])


main()
