using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Proto;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_CAST_FAILED dla Forever 1.60.1.
    ///
    /// Układ z tc-forever (Server/Packets/SpellPackets.cpp:556 CastFailed::Write):
    ///   [CastID PackedGuid][int32 SpellID][Visual][int32 Reason]
    ///   [int32 FailedArg1][int32 FailedArg2][FailedBy PackedGuid]
    ///
    /// Handler 9.x czyta FailedBy tylko dla gałęzi Retail >= 12.0.0, a Forever leci przez
    /// fallback z gałęzią Classic, więc ostatnie pole zostawało nieprzeczytane (2 bajty:
    /// puste PackedGuid = 0x00 0x00).
    /// </summary>
    public static class SpellHandler
    {
        private static void ReadSpellCastVisual(Packet packet, params object[] idx)
        {
            packet.ReadInt32("SpellXSpellVisualID", idx);
            packet.ReadInt32("ScriptVisualID", idx);
        }

        [Parser(Opcode.SMSG_CAST_FAILED)]
        public static void HandleCastFailed(Packet packet)
        {
            var spellFail = packet.Holder.SpellCastFailed = new();
            spellFail.CastGuid = packet.ReadPackedGuid128("CastID");
            spellFail.Spell = (uint)packet.ReadInt32<SpellId>("SpellID");
            ReadSpellCastVisual(packet, "Visual");
            spellFail.Success = packet.ReadInt32("Reason") == 0;
            packet.ReadInt32("FailedArg1");
            packet.ReadInt32("FailedArg2");
            packet.ReadPackedGuid128("FailedBy");
        }
    }
}
