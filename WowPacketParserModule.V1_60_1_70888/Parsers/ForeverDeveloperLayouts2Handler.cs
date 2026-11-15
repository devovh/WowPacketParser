using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>Uklady od dewelopera, odpowiedz nr 2 (ALFA-OPCODES-70938-REPLY-2.md).</summary>
    public static class ForeverDeveloperLayouts2Handler
    {
        /// <summary>Raport opoznien: ten sam uklad w obu kierunkach (G, mocna pewnosc). 21 bajtow na wpis.</summary>
        [Parser(Opcode.SMSG_LATENCY_REPORT_PING)]
        [Parser(Opcode.CMSG_LATENCY_REPORT)]
        public static void HandleLatencyReport(Packet packet)
        {
            packet.ReadUInt32("Unknown");
            var count = packet.ReadUInt32("Count");
            for (var i = 0u; i < count; i++)
            {
                packet.ReadUInt32("A", i);
                packet.ReadUInt32("B", i);
                packet.ReadByte("C", i);
                packet.ReadUInt64("TimeMs", i);
                packet.ReadUInt32("D", i);
            }
        }

        [Parser(Opcode.SMSG_GUILD_EVENT_STATUS_CHANGE)]
        public static void HandleGuildEventStatusChange(Packet packet)
        {
            ForeverGuid.Read(packet, "Guid");
            packet.ReadBit("AFK");
            packet.ReadBit("DND");
            packet.ResetBitReader();
        }

        [Parser(Opcode.SMSG_CLASSIC_SOCIAL_WINDOW_OPEN_RESPONSE)]
        public static void HandleClassicSocialWindowOpenResponse(Packet packet) { packet.ReadUInt32("Unknown"); }

        [Parser(Opcode.CMSG_SET_EMPOWER_MIN_HOLD_STAGE_PERCENT)]
        public static void HandleSetEmpowerMinHoldStagePercent(Packet packet) { packet.ReadSingle("Percent"); }

        [Parser(Opcode.CMSG_BATTLE_PAY_REQUEST_PRICE_INFO)]
        public static void HandleBattlePayRequestPriceInfo(Packet packet) { packet.ReadByte("Type"); }
    }
}
