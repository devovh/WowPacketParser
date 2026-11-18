"""Znajduje pakiet po opcodzie i numerze (Number z _errors.txt) i robi jego hexdump.

Uzycie:
    python tools/find-packet.py <plik.pkt> <opcode hex> <numer> [--context N]
"""
import sys
from pkt_dump import read_pkt


def main():
    if len(sys.argv) < 4:
        print(__doc__)
        return 2
    path = sys.argv[1]
    want = int(sys.argv[2], 16)
    number = int(sys.argv[3])
    _, pkts = read_pkt(path)
    idx = 0
    for direction, opcode, data in pkts:
        idx += 1
        if opcode != want:
            continue
        if idx != number:
            continue
        print(f"# pakiet nr {idx}, len {len(data)}")
        print(data.hex())
        return 0
    print(f"nie znaleziono pakietu nr {number} z opcodem {want:06X}")
    return 1


sys.exit(main())
