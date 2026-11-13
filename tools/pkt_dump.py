"""Zrzut pakietow z pliku .pkt (format PKT 2.1/2.2/3.0/3.1, jak w WowPacketParser).

Uzycie:
    python pkt_dump.py <plik.pkt> <opcode hex>   # hexdump danych wybranego opcodu
    python pkt_dump.py <plik.pkt> --list         # lista opcodow z licznikami
"""

import struct
import sys
from collections import Counter


def read_pkt(path):
    d = open(path, "rb").read()
    assert d[:3] == b"PKT", "to nie jest plik PKT"
    off = 3
    version = struct.unpack_from("<H", d, off)[0]
    off += 2
    sniffer = 0
    if version == 0x201:
        off += 2 + 40
    elif version == 0x202:
        sniffer = d[off]
        off += 1 + 2 + 4 + 20 + 64
    elif version == 0x300:
        sniffer = d[off]
        off += 1 + 4 + 4 + 40
        n = struct.unpack_from("<i", d, off)[0]
        off += 4 + n
    elif version == 0x301:
        sniffer = d[off]
        off += 1 + 4 + 4 + 40 + 4 + 4
        n = struct.unpack_from("<i", d, off)[0]
        off += 4 + n
    else:
        raise ValueError(f"nieznana wersja PKT: 0x{version:04X}")

    pkts = []
    while off + 8 <= len(d):
        magic = struct.unpack_from("<I", d, off)[0]
        off += 4
        if version in (0x201, 0x202):
            time = struct.unpack_from("<i", d, off)[0]
            off += 4 + 4
            length = struct.unpack_from("<i", d, off)[0]
            off += 4
            if magic == 0xFF:
                opcode = struct.unpack_from("<h", d, off)[0]
                off += 2
                length -= 2
            else:
                opcode = struct.unpack_from("<i", d, off)[0]
                off += 4
                length -= 4
            direction = "SMSG" if magic == 0xFF else "CMSG"
        else:
            direction = {0x47534D53: "SMSG", 0x47534D43: "CMSG"}.get(magic, "BNET")
            if version == 0x300:
                off += 4 + 4
            else:
                off += 4 + 4
            additional = struct.unpack_from("<i", d, off)[0]
            off += 4
            length = struct.unpack_from("<i", d, off)[0]
            off += 4
            off += additional
            opcode = struct.unpack_from("<i", d, off)[0]
            off += 4
            length -= 4
        data = d[off:off + length]
        off += length
        pkts.append((direction, opcode, data))
    return {"version": version, "sniffer": sniffer}, pkts


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 2

    hdr, pkts = read_pkt(sys.argv[1])
    print("naglowek:", hdr, "pakietow:", len(pkts))

    if sys.argv[2] == "--list":
        c = Counter(f"{op:06X}" for _, op, _ in pkts)
        for op, n in c.most_common():
            print(op, n)
        return 0

    want = int(sys.argv[2], 16)
    for direction, opcode, data in pkts:
        if opcode != want:
            continue
        print(direction, f"{opcode:06X}", "len", len(data))
        print(data.hex())
    return 0


if __name__ == "__main__":
    sys.exit(main())
