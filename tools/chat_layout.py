"""Rozklad bloku bitowego SMSG_CHAT Forever (5 bajtow po polach stalych).

Ustalone na 13 pakietach snu 70938:
  blok = 00 04 XX YY 40
  b0 = 0x00, b1 = 0x04, b4 = 0x40 (dopelnienie FlushBits)
  b2b3 jako u16 BE:
    v>>12      = (channel*4) & 0x3f   -> 12/8/4 dla kanalow 19/18/17
    v&0xfff    = textLen*4 - (0 if ((channel*4)&8) else 8)
  Po kanale (7 bitow) zostaje 22 bity: rest>>10 = textLen - 2 (dla General),
  = textLen (dla Trade).
"""
import sys
sys.path.insert(0, "tools")
from chat_bits import load_chats, bitstart


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else (
        "C:/Users/devov/Desktop/regedit_70862/dumps/"
        "dump_1.60.1.70938_2026-11-13_14-02-23.pkt"
    )

    expected = [19, 19, 18, 19, 19, 18, 18, 18, 17, 17, 17, 17, 17]

    for i, x in enumerate(load_chats(path)):
        p = bitstart(x)
        b = x[p:p + 5]
        v = (b[2] << 8) | b[3]

        hi = v >> 12
        lo = v & 0xFFF

        # kanal: hi = (ch*4) & 0x3f  ->  ch = (hi | 64) / 4 dla hi < 16
        channel = (hi | 0x40) // 4 if hi in (4, 8, 12) else None

        # tekst: lo = tx*4 - korekcja
        correction = 0 if (channel * 4) & 8 else 8
        text = (lo + correction) // 4

        bits = "".join(format(c, "08b") for c in b)
        rest = int(bits[18:], 2)
        rest_text = rest >> 10

        ok = "OK" if channel == expected[i] else "NIE"
        print(f"{i:2d} blok={b.hex(' ')}  kanal={channel} (oczek {expected[i]}) {ok}"
              f"  tekst={text}  rest>>10={rest_text}")


if __name__ == "__main__":
    main()
