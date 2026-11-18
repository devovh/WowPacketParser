"""Krok po kroku przechodzi SMSG_UPDATE_OBJECT (70959) i pokazuje, gdzie sie rozjezdza.

Uzycie:
    python tools/walk-update-object.py <plik.pkt> <indeks pakietu>
"""
import struct
import sys

from pkt_dump import read_pkt


class R:
    def __init__(self, data, off=0):
        self.d = data
        self.off = off

    def u8(self):
        v = self.d[self.off]; self.off += 1; return v

    def u16(self):
        v = struct.unpack_from("<H", self.d, self.off)[0]; self.off += 2; return v

    def u32(self):
        v = struct.unpack_from("<I", self.d, self.off)[0]; self.off += 4; return v

    def u64(self):
        v = struct.unpack_from("<Q", self.d, self.off)[0]; self.off += 8; return v

    def guid(self):
        lo_mask = self.u8()
        hi_mask = self.u8()

        def pu64(mask):
            if mask == 0:
                return 0
            res = 0
            for i in range(8):
                if mask & (1 << i):
                    res |= self.u8() << (i * 8)
            return res

        lo = pu64(lo_mask)
        hi = pu64(hi_mask)
        return (lo, hi)


def main():
    path, idx = sys.argv[1], int(sys.argv[2])
    _, pkts = read_pkt(path)
    d = pkts[idx][2]
    r = R(d)
    mapid = r.u16()
    count = r.u32()
    bitpos = r.off * 8
    def rbit():
        nonlocal bitpos
        b = (d[bitpos // 8] >> (bitpos % 8)) & 1
        bitpos += 1
        return b
    is_end = rbit()
    has_removed = rbit()
    r.off = (bitpos + 7) // 8
    print(f"map={mapid} count={count} isEnd={is_end} hasRemoved={has_removed}")
    if has_removed:
        destroyed = struct.unpack_from("<h", d, r.off)[0]; r.off += 2
        removed = r.u32()
        print(f"  destroyed={destroyed} removed={removed} (outOfRange={removed-destroyed})")
        for _ in range(destroyed):
            r.guid()
        for _ in range(removed - destroyed):
            r.guid()
    datasize = r.u32()
    print(f"DataSize={datasize} len={len(d)} (roznicа: {len(d) - r.off - datasize})")

    for i in range(count):
        start = r.off
        if r.off >= len(d):
            print(f"  [{i}] KONIEC DANYCH na offset {r.off}")
            return
        utype = r.u8()
        g = r.guid()
        print(f"  [{i}] off={start} type={utype} guid=0x{g[1]:016X}{g[0]:016X}", end="")
        if utype == 0:
            size = r.u32()
            print(f" VALUES size={size}")
            r.off += size
        elif utype in (1, 2):
            print(" CREATE")
            break
        else:
            print(" ???")
            return


main()
