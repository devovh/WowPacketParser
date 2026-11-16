using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_LEVEL_UP_INFO = 0x470195 (tc-forever 0x450194 + przesuniecie Classic 0x20001).
    /// Wczesniej bledne 0x470199 i falszywe SMSG_CRITERIA_DELETED na 0x470195.
    /// Uklad wg tc-forever LevelUpInfo::Write; pakiet ma 76 B = 19 x i32:
    /// Level, HealthDelta, PowerDelta[10], StatDelta[5], NumNewTalents, NumNewPvpTalentSlots.
    /// </summary>
    public static class ForeverLevelUpHandler
    {
        [Parser(Opcode.SMSG_LEVEL_UP_INFO)]
        public static void HandleLevelUpInfo(Packet packet)
        {
            packet.ReadInt32("Level");
            packet.ReadInt32("HealthDelta");
            for (var i = 0; i < 10; ++i)
                packet.ReadInt32("PowerDelta", i);
            for (var i = 0; i < 5; ++i)
                packet.ReadInt32("StatDelta", (StatType)i);
            packet.ReadInt32("NumNewTalents");
            packet.ReadInt32("NumNewPvpTalentSlots");
        }
    }
}
