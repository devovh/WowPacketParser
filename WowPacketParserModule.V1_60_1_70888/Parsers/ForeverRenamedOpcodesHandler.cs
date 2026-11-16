using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Readers for the opcodes renamed in the 70938 table (0x44008E, 0x530039).
    /// Layouts from the tc-forever writers / readers, checked on live 70938 captures (2026-11-15 and 2026-11-16).
    /// </summary>
    public static class ForeverRenamedOpcodesHandler
    {
        /// <summary>0x44008E, was CAN_DUEL. Sent after release, before the corpse arrow shows.</summary>
        [Parser(Opcode.CMSG_QUERY_CORPSE_LOCATION_FROM_CLIENT)]
        public static void HandleQueryCorpseLocationFromClient(Packet packet)
        {
            ForeverGuid.Read(packet, "Player");
        }

        /// <summary>0x530039, was RANKS_UPDATED (which has no data).</summary>
        [Parser(Opcode.SMSG_GUILD_EVENT_RANK_CHANGED)]
        public static void HandleGuildEventRankChanged(Packet packet)
        {
            packet.ReadInt32("RankID");
        }
    }
}
