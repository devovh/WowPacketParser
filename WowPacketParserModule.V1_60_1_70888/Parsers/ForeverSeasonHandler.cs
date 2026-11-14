using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_SEASON_INFO (0x470065). tc-forever (Server/Packets/BattlegroundPackets.cpp:23
    /// SeasonInfo::Write) pisze 7 x int32, a potem 3 bity. Forever dokłada dwa kolejne
    /// int32 PRZED bitami, więc parser 12.x czytał bity z 8. int32 i zostawiał 8 bajtów.
    ///
    /// Weryfikacja na pakiecie 37 B (Number 277):
    ///   7 x int32 = 28 B, dwa dodatkowe int32 = 36 i 1087 (offsety 28 i 32), ostatni bajt = 3 bity.
    /// </summary>
    public static class ForeverSeasonHandler
    {
        [Parser(Opcode.SMSG_SEASON_INFO)]
        public static void HandleSeasonInfo(Packet packet)
        {
            packet.ReadInt32("MythicPlusDisplaySeasonID");
            packet.ReadInt32("MythicPlusMilestoneSeasonID");
            packet.ReadInt32("CurrentArenaSeason");
            packet.ReadInt32("PreviousArenaSeason");
            packet.ReadInt32("ConquestWeeklyProgressCurrencyID");
            packet.ReadInt32("PvpSeasonID");
            packet.ReadInt32("Unknown1027_1");
            packet.ReadInt32("ClassicUnknown1");
            packet.ReadInt32("ClassicUnknown2");

            packet.ResetBitReader();
            packet.ReadBit("WeeklyRewardChestsEnabled");
            packet.ReadBit("CurrentArenaSeasonUsesTeams");
            packet.ReadBit("PreviousArenaSeasonUsesTeams");
        }
    }
}
