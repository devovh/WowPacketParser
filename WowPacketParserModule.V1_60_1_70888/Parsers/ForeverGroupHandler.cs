using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// CMSG_MINIMAP_PING dla Forever 1.60.1.
    ///
    /// Fallback z V6_0_2_19033 czyta Vector2 (8 B) + bajt, co daje wyjatek EndOfStream
    /// na pakietach 1-bajtowych - w sniffie 17.11 11:53 dwa razy (Number 27266 i 388349).
    /// Forever wysyla ping bez pozycji: 1 bajt. Nie zgadujemy jego znaczenia, czytamy jako u8.
    ///
    /// Uwaga: nie mylic z CMSG_MINIMAP_PING z V3_4_4_59817 (bit HasPartyIndex + Vector2
    /// + [PartyIndex]) - to inny uklad. Jesli kiedys pojawi sie dluzszy pakiet 0x440077,
    /// trzeba ten czytnik rozszerzyc warunkowo po dlugosci, nie zmieniac go na slepo.
    /// </summary>
    public static class ForeverGroupHandler
    {
        [Parser(Opcode.CMSG_MINIMAP_PING)]
        public static void HandleClientMinimapPing(Packet packet)
        {
            packet.ReadByte("Unk");
        }
    }
}
