using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>Zapytania i odpowiedzi Forever 70938 wg tc-forever (QueryPackets.cpp:64, GuildPackets.cpp:31).</summary>
    public static class ForeverQueryRequestHandler
    {
        /// <summary>6 bitow licznika (flush), potem lista identyfikatorow stworzen.</summary>
        [Parser(Opcode.CMSG_QUERY_CREATURE, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleQueryCreature(Packet packet)
        {
            var count = packet.ReadBits("Count", 6);
            packet.ResetBitReader();
            for (var i = 0; i < count; ++i)
                packet.ReadUInt32("CreatureID", i);
        }

        [Parser(Opcode.SMSG_QUERY_GUILD_INFO_RESPONSE, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleQueryGuildInfoResponse(Packet packet)
        {
            ForeverGuid.Read(packet, "GuildGuid");
            var hasInfo = packet.ReadBit("HasInfo");
            packet.ResetBitReader();
            if (!hasInfo)
                return;

            ForeverGuid.Read(packet, "GuildGUID");
            packet.ReadUInt32("VirtualRealmAddress");
            var ranks = packet.ReadUInt32("RankCount");
            packet.ReadInt32("EmblemStyle");
            packet.ReadInt32("EmblemColor");
            packet.ReadInt32("BorderStyle");
            packet.ReadInt32("BorderColor");
            packet.ReadInt32("BackgroundColor");
            packet.ReadInt32("Unknown1601");
            for (var i = 0; i < ranks; ++i)
            {
                packet.ReadUInt32("RankID", i);
                packet.ReadUInt32("RankOrder", i);
                var len = packet.ReadBits(7);
                packet.ResetBitReader();
                packet.ReadWoWString("RankName", len, i);
            }
            var nameLen = packet.ReadBits(7);
            packet.ResetBitReader();
            packet.ReadWoWString("GuildName", nameLen);
        }
    }
}
