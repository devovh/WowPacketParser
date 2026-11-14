using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_SPELL_CHANNEL_UPDATE / SMSG_SPELL_FAILURE / SMSG_SPELL_FAILED_OTHER
    /// dla Forever 1.60.1 — brakujace pole FailedBy.
    ///
    /// tc-forever, Server/Packets/SpellPackets.cpp:
    ///   918  SpellChannelUpdate::Write  -> [CasterGUID][int32 TimeRemaining][FailedBy]
    ///   532  SpellFailure::Write       -> [CasterUnit][CastID][int32 SpellID][Visual][u16 Reason][FailedBy]
    ///   544  SpellFailedOther::Write   -> [CasterUnit][CastID][u32 SpellID][Visual][u8 Reason][FailedBy]
    ///
    /// Fallback (9.x) czyta FailedBy tylko dla Retail >= 12.0.0, wiec dla Forever zostaja
    /// dokladnie 2 bajty — pusta maska GUID-a (0x0000). Widac to w kazdym wariancie
    /// dlugosci: 13/14/15/19/20/21 B dla CHANNEL_UPDATE i 37/38/39 B dla FAILED_OTHER
    /// zawsze maja "Bytes remaining: 2".
    /// </summary>
    public static class ForeverSpellFailHandler
    {
        [Parser(Opcode.SMSG_SPELL_CHANNEL_UPDATE)]
        public static void HandleSpellChannelUpdate(Packet packet)
        {
            packet.ReadPackedGuid128("CasterGUID");
            packet.ReadInt32("TimeRemaining");
            packet.ReadPackedGuid128("FailedBy");
        }

        [Parser(Opcode.SMSG_SPELL_FAILURE)]
        public static void HandleSpellFailure(Packet packet)
        {
            packet.ReadPackedGuid128("CasterUnit");
            packet.ReadPackedGuid128("CastID");
            packet.ReadInt32<SpellId>("SpellID");
            V9_0_1_36216.Parsers.SpellHandler.ReadSpellCastVisual(packet, "Visual");
            packet.ReadInt16("Reason");
            packet.ReadPackedGuid128("FailedBy");
        }

        [Parser(Opcode.SMSG_SPELL_FAILED_OTHER)]
        public static void HandleSpellFailedOther(Packet packet)
        {
            packet.ReadPackedGuid128("CasterUnit");
            packet.ReadPackedGuid128("CastID");
            packet.ReadUInt32<SpellId>("SpellID");
            V9_0_1_36216.Parsers.SpellHandler.ReadSpellCastVisual(packet, "Visual");
            packet.ReadByte("Reason");
            packet.ReadPackedGuid128("FailedBy");
        }
    }
}
