using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_HIGHEST_THREAT_UPDATE (0x470187) dla Forever 1.60.1.
    ///
    /// Uklad zmierzony bajt po bajcie na 538 pakietach ze sniffu 1.60.1.70862,
    /// wszystkie maja dokladnie 33 bajty i wszystkie domykaja sie bez reszty:
    ///
    ///   [0]  GUID  UnitGUID          (maska 0xEF2F = 12 bitow -> 14 B)
    ///   [14] u32   ThreatListCount   (zawsze 1 w tym snifie)
    ///   [18] GUID  ThreatGUID        (maska 0xE006 = 5 bitow -> 7 B)
    ///   [25] u32   Threat
    ///   [29] u32   Threat2 (to samo co Threat, powtorzone przez serwer)
    ///
    /// Parser 6.x czyta tu ReadPackedGuid128 (maska 2-bajtowa low/high), co dla Forever
    /// daje zle przesuniecie i EndOfStream. Roznica jest tylko w formacie GUID-a.
    /// </summary>
    public static class ForeverThreatHandler
    {
        [Parser(Opcode.SMSG_HIGHEST_THREAT_UPDATE)]
        public static void HandleHighestThreatlistUpdate(Packet packet)
        {
            ForeverGuid.Read(packet, "UnitGUID");

            var count = packet.ReadUInt32("ThreatListCount");

            for (var i = 0u; i < count; ++i)
            {
                ForeverGuid.Read(packet, "ThreatGUID", i);
                packet.ReadUInt32("Threat", i);
                packet.ReadUInt32("Threat2", i);
            }
        }
    }
}
