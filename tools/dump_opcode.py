"""Hexdump wybranego opcodu z pliku .pkt (reuse read_pkt z pkt_dump.py).

Uzycie:
    python tools/dump_opcode.py <plik.pkt> <opcode hex> [--limit N] [--raw]
"""

import sys

from pkt_dump import read_pkt


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 2

    path = sys.argv[1]
    want = int(sys.argv[2], 16)
    limit = None
    raw = False
    args = sys.argv[3:]
    for i, a in enumerate(args):
        if a == "--limit":
            limit = int(args[i + 1])
        if a == "--raw":
            raw = True

    _, pkts = read_pkt(path)
    shown = 0
    for direction, opcode, data in pkts:
        if opcode != want:
            continue
        if raw:
            print(f"{direction} {opcode:06X} len {len(data)} {data.hex()}")
        else:
            print(f"== {direction} {opcode:06X} len {len(data)}")
            for off in range(0, len(data), 16):
                chunk = data[off:off + 16]
                hexpart = " ".join(f"{b:02X}" for b in chunk)
                txt = "".join(chr(b) if 32 <= b < 127 else "." for b in chunk)
                print(f"{off:4d}  {hexpart:<47}  {txt}")
        shown += 1
        if limit and shown >= limit:
            break
    print(f"pokazano {shown} pakietow")
    return 0


if __name__ == "__main__":
    sys.exit(main())
