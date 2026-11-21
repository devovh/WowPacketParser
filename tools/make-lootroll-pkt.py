"""Buduje maly sniff PKT 3.1 z jednym pakietem SMSG_LOOT_ROLL (0x4700C8) i wypisuje plik.

Uzycie: python tools/make-lootroll-pkt.py <wyjscie.pkt>
Format: naglowek PKT 3.1 + rekord: magic 'SMSG', 2x u32 (conn/time), u32 additional, u32 length, dane.
"""

import struct
import sys


def build_packet(direction, opcode, data):
    magic = 0x47534D53 if direction == "SMSG" else 0x47534D43
    out = struct.pack("<I", magic)
    out += struct.pack("<i", 0)     # connection index
    out += struct.pack("<i", 0)     # time (ms)
    out += struct.pack("<i", 0)     # additional size
    out += struct.pack("<i", 4 + len(data))
    out += struct.pack("<i", opcode)
    out += data
    return out


def packed128(low, high):
    lo_mask = 0
    lo_bytes = []
    for i in range(8):
        v = (low >> (i * 8)) & 0xFF
        if v:
            lo_mask |= 1 << i
            lo_bytes.append(v)
    hi_mask = 0
    hi_bytes = []
    for i in range(8):
        v = (high >> (i * 8)) & 0xFF
        if v:
            hi_mask |= 1 << i
            hi_bytes.append(v)
    return bytes([lo_mask]) + bytes(lo_bytes) + bytes([hi_mask]) + bytes(hi_bytes)


def main():
    out_path = sys.argv[1] if len(sys.argv) > 1 else "lootroll-test.pkt"

    loot = packed128(0xABC500007C6CF2, 0x3C6AF00000000000)
    winner = packed128(0x53F4AA5, 0x86C8C0000000000)
    body = (
        bytes.fromhex("00010000")          # i32 Roll = 256
        + b"\x00"                           # u8 RollType = 0 (Pass)
        + bytes.fromhex("03320300")         # u32 ItemID = 209411
        + bytes.fromhex("00000000")         # u32 Quantity = 0
        + bytes([0xFF])                     # u8 LootItemType
        + bytes([0xFF])                     # u8 LootListID
        + bytes.fromhex("01000000")         # blok bitowy: Autopassed=0, OffSpec=0, koncowy bajt
    )
    data = loot + winner + body
    print("dlugosc pakietu:", len(data))

    header = b"PKT" + struct.pack("<H", 0x301)
    header += bytes([0])                        # sniffer id
    header += struct.pack("<I", 70938)          # client build
    header += b"enUS"                           # client locale
    header += bytes(40)                         # session key
    header += struct.pack("<I", 0)              # start time (unix)
    header += struct.pack("<I", 0)              # start tick count
    header += struct.pack("<i", 0)              # additional info size

    with open(out_path, "wb") as f:
        f.write(header + build_packet("SMSG", 0x4700C8, data))
    print("zapisano", out_path)


if __name__ == "__main__":
    main()
