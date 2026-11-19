"""Mapa zmiennosci bajtow w create tego samego GUID.

Bierze pary create tego samego obiektu z dwoch momentow i liczy, na ktorych
pozycjach bajty sie roznia. Pozycja (blok ruchu) jest znana i sluzy jako punkt
odniesienia; reszta roznic wskazuje pola czasowe, a brak roznic - pola stale.
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


def collect(path, maxtype=(5, 6)):
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
            start = off
            t = x[off]
            off += 1
            try:
                g, off = None, rd_guid(x, off)
            except IndexError:
                break
            # guid liczony od nowa, bo rd_guid nie zwraca wartosci
            gg = guid_bytes = x[start + 1:off]
            if t == 0:
                if off + 4 > len(x):
                    break
                off += 4 + struct.unpack_from("<I", x, off)[0]
                continue
            if off + 1 > len(x):
                break
            ot = x[off]
            if ot in maxtype:
                out[guid_bytes].append(x[start:])
            off += 1
            break
    return out


def main():
    files = sys.argv[1:]
    allrec = collections.defaultdict(list)
    for f in files:
        for k, v in collect(f).items():
            allrec[k].extend(v)

    pairs = [(k, v) for k, v in allrec.items() if len(v) >= 2]
    print(f"GUID-ow z co najmniej dwoma create: {len(pairs)}")

    diffcount = collections.Counter()
    total = 0
    for k, v in pairs:
        a, b = v[0], v[1]
        n = min(len(a), len(b))
        total += 1
        for i in range(n):
            if a[i] != b[i]:
                diffcount[i] += 1

    print(f"par porownanych: {total}")
    print()
    print("pozycje zmienne (ile par sie rozni) - pierwsze 60:")
    for i in sorted(diffcount)[:60]:
        print(f"  @{i:3d}: {diffcount[i]:3d}/{total}")
    print()
    print("ostatnia zmienna pozycja:", max(diffcount) if diffcount else "-")
    # spojne regiony
    regs = []
    for i in sorted(diffcount):
        if regs and i == regs[-1][1] + 1:
            regs[-1][1] = i
        else:
            regs.append([i, i])
    print("regiony zmienne:", regs[:20])


main()
