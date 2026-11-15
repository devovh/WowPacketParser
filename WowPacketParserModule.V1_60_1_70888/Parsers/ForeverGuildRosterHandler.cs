using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParserModule.Substructures;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_GUILD_ROSTER (0x530003) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (GuildPackets.cpp:76 GuildRosterMemberData, :119 GuildRoster::Write):
    ///   [i32 NumAccounts][packed time CreateDate][i32 GuildFlags][u32 MemberCount]
    ///   per czlonek:
    ///     [GUID][i32 RankID][i32 AreaID][i32 PersonalAchievementPoints][i32 GuildReputation]
    ///     [f32 LastSave][2 x (i32 DbID, i32 Rank, i32 Step)][u32 VirtualRealmAddress]
    ///     [u8 Status][u8 Level][u8 ClassID][u8 Gender]
    ///     [DungeonScoreSummary: f32, f32, u32 runs, runs x 17 B]
    ///     [u64 GuildClubMemberID][u8 RaceID][u64 nieznane (build >= 70291)][i32 TimerunningSeasonID]
    ///     [bity: 6 dl. imienia, 9 dl. nazwiska, 8 dl. notatki, 8 dl. notatki oficera, 1 Authenticated]
    ///     [Name][Surname][Note][OfficerNote]
    ///   [bity 11 WelcomeText, 11 InfoText][WelcomeText][InfoText]
    ///
    /// Retailowy parser czytal tu 6 + 8 + 8 bitow + SorEligible, bez nazwiska, przez co imie
    /// i notatki wychodzily puste, a wszystko po pierwszym czlonku bylo przesuniete.
    /// </summary>
    public static class ForeverGuildRosterHandler
    {
        [Parser(Opcode.SMSG_GUILD_ROSTER)]
        public static void HandleGuildRoster(Packet packet)
        {
            packet.ReadUInt32("NumAccounts");
            packet.ReadPackedTime("CreateDate");
            packet.ReadUInt32("GuildFlags");
            var memberCount = packet.ReadUInt32("MemberDataCount");

            for (var i = 0; i < memberCount; ++i)
            {
                ForeverGuid.Read(packet, "Guid", i);

                packet.ReadUInt32("RankID", i);
                packet.ReadUInt32<AreaId>("AreaID", i);
                packet.ReadUInt32("PersonalAchievementPoints", i);
                packet.ReadUInt32("GuildReputation", i);
                packet.ReadSingle("LastSave", i);

                for (var j = 0; j < 2; ++j)
                {
                    packet.ReadUInt32("DbID", i, "Profession", j);
                    packet.ReadUInt32("Rank", i, "Profession", j);
                    packet.ReadUInt32("Step", i, "Profession", j);
                }

                packet.ReadUInt32("VirtualRealmAddress", i);
                packet.ReadByteE<GuildMemberFlag>("Status", i);
                packet.ReadByte("Level", i);
                packet.ReadByteE<Class>("ClassID", i);
                packet.ReadByteE<Gender>("Gender", i);

                packet.ResetBitReader();
                ReadDungeonScoreSummary(packet, i);

                packet.ReadUInt64("GuildClubMemberID", i);
                packet.ReadByteE<Race>("RaceID", i);
                packet.ReadUInt64("Unknown70291", i);
                packet.ReadInt32("TimerunningSeasonID", i);

                packet.ResetBitReader();
                var nameLen = packet.ReadBits("NameLength", 6, i);
                var surnameLen = packet.ReadBits("SurnameLength", 9, i);
                var noteLen = packet.ReadBits("NoteLength", 8, i);
                var officerNoteLen = packet.ReadBits("OfficerNoteLength", 8, i);
                packet.ReadBit("Authenticated", i);

                packet.ReadWoWString("Name", nameLen, i);
                packet.ReadWoWString("Surname", surnameLen, i);
                packet.ReadWoWString("Note", noteLen, i);
                packet.ReadWoWString("OfficerNote", officerNoteLen, i);
            }

            packet.ResetBitReader();
            var welcomeLen = packet.ReadBits("WelcomeTextLength", 11);
            var infoLen = packet.ReadBits("InfoTextLength", 11);

            packet.ReadWoWString("WelcomeText", welcomeLen);
            packet.ReadWoWString("InfoText", infoLen);
        }

        // MythicPlus::DungeonScoreSummary in tc-forever: two floats, a run count and runs of
        // (i32 ChallengeModeID, f32 MapScore, i32 BestRunLevel, i32 BestRunDurationMS, u8, bit).
        private static void ReadDungeonScoreSummary(Packet packet, int member)
        {
            packet.ReadSingle("OverallScoreCurrentSeason", member, "DungeonScoreSummary");
            packet.ReadSingle("LadderScoreCurrentSeason", member, "DungeonScoreSummary");

            var runs = packet.ReadUInt32("RunCount", member, "DungeonScoreSummary");
            for (var r = 0u; r < runs; ++r)
                MythicPlusHandler.ReadDungeonScoreMapSummary550(packet, member, "DungeonScoreSummary", r);
        }
    }
}
