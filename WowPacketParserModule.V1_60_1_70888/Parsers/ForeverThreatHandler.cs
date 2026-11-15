using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Pakiety zagrozenia, mocy i tytulow Forever 1.60.1. Numeracja opcodow: indeks Classic = retail - 1
    /// (ClassicOpcodes.cpp TranslateServerOpcode70245), a na drucie w 70938 sa jeszcze przesuniete.
    ///
    /// SMSG_THREAT_UPDATE (0x470187), tc-forever CombatPackets.cpp:46:
    ///   [GUID Unit][u32 count] per wpis [GUID][i64 Threat]
    /// SMSG_HIGHEST_THREAT_UPDATE (0x470186), CombatPackets.cpp:59:
    ///   [GUID Unit][GUID HighestThreat][u32 count] per wpis [GUID][i64 Threat]
    /// SMSG_THREAT_CLEAR (0x470189): [GUID Unit]
    /// SMSG_POWER_UPDATE (0x47017F), CombatPackets.cpp:97: [GUID][u32 count] per wpis [u8 PowerType][i32 Power]
    /// SMSG_TITLE_EARNED (0x470184) / SMSG_TITLE_LOST (0x470185): [u32 Index]
    /// SMSG_HEALTH_UPDATE nie istnieje w Classic (zdrowie idzie przez UnitData).
    /// </summary>
    public static class ForeverThreatHandler
    {
        [Parser(Opcode.SMSG_THREAT_UPDATE)]
        public static void HandleThreatUpdate(Packet packet)
        {
            ForeverGuid.Read(packet, "UnitGUID");
            var count = packet.ReadUInt32("ThreatListCount");
            for (var i = 0u; i < count; ++i)
            {
                ForeverGuid.Read(packet, "ThreatGUID", i);
                packet.ReadInt64("Threat", i);
            }
        }

        [Parser(Opcode.SMSG_HIGHEST_THREAT_UPDATE)]
        public static void HandleHighestThreatUpdate(Packet packet)
        {
            ForeverGuid.Read(packet, "UnitGUID");
            ForeverGuid.Read(packet, "HighestThreatGUID");
            var count = packet.ReadUInt32("ThreatListCount");
            for (var i = 0u; i < count; ++i)
            {
                ForeverGuid.Read(packet, "ThreatGUID", i);
                packet.ReadInt64("Threat", i);
            }
        }

        [Parser(Opcode.SMSG_THREAT_CLEAR)]
        public static void HandleThreatClear(Packet packet)
        {
            ForeverGuid.Read(packet, "UnitGUID");
        }

        [Parser(Opcode.SMSG_POWER_UPDATE)]
        public static void HandlePowerUpdate(Packet packet)
        {
            ForeverGuid.Read(packet, "Guid");
            var count = packet.ReadUInt32("Count");
            for (var i = 0u; i < count; ++i)
            {
                packet.ReadByteE<PowerType>("PowerType", i);
                packet.ReadInt32("Power", i);
            }
        }

        [Parser(Opcode.SMSG_TITLE_EARNED)]
        public static void HandleTitleEarned(Packet packet)
        {
            packet.ReadUInt32("Index");
        }

        [Parser(Opcode.SMSG_TITLE_LOST)]
        public static void HandleTitleLost(Packet packet)
        {
            packet.ReadUInt32("Index");
        }
    }
}
