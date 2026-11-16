using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// 0x47018D to SMSG_CRITERIA_UPDATE, a nie SMSG_TRAINER_LIST (wczesniej bledna nazwa).
    /// Uklad wg tc-forever CriteriaUpdate::Write, potwierdzony na 28 pakietach po 48 bajtow:
    /// u32 CriteriaID, u64 Quantity, GUID gracza (Forever), u32 Flags, u32 StateFlags, u32 CurrentTime (packed),
    /// u64 ElapsedTime, u64 CreationTime, bit HasDynamicID (+ u64 gdy ustawiony).
    /// </summary>
    public static class ForeverCriteriaHandler
    {
        [Parser(Opcode.SMSG_CRITERIA_UPDATE)]
        public static void HandleCriteriaUpdate(Packet packet)
        {
            packet.ReadUInt32("CriteriaID");
            packet.ReadUInt64("Quantity");
            ForeverGuid.Read(packet, "PlayerGUID");
            packet.ReadUInt32("Flags");
            packet.ReadUInt32("StateFlags");
            packet.ReadPackedTime("CurrentTime");
            packet.ReadUInt64("ElapsedTime");
            packet.ReadUInt64("CreationTime");
            var hasDynamic = packet.ReadBit("HasDynamicID");
            packet.ResetBitReader();
            if (hasDynamic)
                packet.ReadUInt64("DynamicID");
        }
    }
}
