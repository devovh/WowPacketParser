using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Czat dla Forever 1.60.1 — układ podany przez dewelopera 2026-11-17.
    ///
    /// Uwaga: deweloper pisze "bity pakowane MSB first", a WowPacketParser czyta bity
    /// od najmlodszego. Zmierzone na jego wlasnym pakiecie:
    ///   01 00 00 00 | 00 A0 | 68 65 6C 6C 6F
    /// 11 bitow dlugosci + 1 bit IsSecure daja razem dwa bajty 00 A0, a dlugosc = 5.
    /// Parser (LSB first) dostaje to samo, gdy czyta 12 bitow z tych dwoch bajtow:
    /// 0xA000 = 0b1010_0000_0000_0000 -> dolne 11 bitow = 5, bit 11 (IsSecure) = 0.
    /// Dlatego czytamy 11 bitow dlugosci, potem 1 bit IsSecure - tak jak mowi deweloper,
    /// tylko w kolejnosci LSB, w ktorej parser sklada bity z bajtow.
    ///
    /// Dziala dla PARTY, SAY, RAID, RAID_WARNING i INSTANCE_CHAT (bez IsSecure).
    /// </summary>
    public static class ForeverChatHandler
    {
        private static void ReadLanguageText(Packet packet, bool hasSecureBit)
        {
            packet.ReadInt32E<Language>("Language");
            var textLen = packet.ReadBits("TextLength", 11);
            if (hasSecureBit)
                packet.ReadBit("IsSecure");
            packet.ResetBitReader();
            packet.ReadWoWString("Text", textLen);
        }

        [Parser(Opcode.CMSG_CHAT_MESSAGE_PARTY)]
        public static void HandleChatMessageParty(Packet packet)
        {
            ReadLanguageText(packet, true);
        }

        [Parser(Opcode.CMSG_CHAT_MESSAGE_SAY)]
        public static void HandleChatMessageSay(Packet packet)
        {
            ReadLanguageText(packet, false);
        }

        [Parser(Opcode.CMSG_CHAT_MESSAGE_RAID)]
        public static void HandleChatMessageRaid(Packet packet)
        {
            ReadLanguageText(packet, false);
        }

        [Parser(Opcode.CMSG_CHAT_MESSAGE_RAID_WARNING)]
        public static void HandleChatMessageRaidWarning(Packet packet)
        {
            ReadLanguageText(packet, false);
        }

        [Parser(Opcode.CMSG_CHAT_MESSAGE_INSTANCE_CHAT)]
        public static void HandleChatMessageInstanceChat(Packet packet)
        {
            ReadLanguageText(packet, false);
        }
    }
}
