using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// CMSG_QUERY_NEIGHBORHOOD_INFO (0x4400B7). Fallback (11.x HousingHandler.cs:135) czyta
    /// PackedGuid128, który w Forever zajmuje 3 bajty (2-bajtowa maska + 1), a pakiet ma 4.
    /// W Forever pole to po prostu u32 bez maski.
    /// </summary>
    public static class ForeverHousingHandler
    {
        [Parser(Opcode.CMSG_QUERY_NEIGHBORHOOD_INFO)]
        public static void HandleQueryNeighborhoodInfo(Packet packet)
        {
            packet.ReadUInt32("NeighborhoodGUID");
        }
    }
}
