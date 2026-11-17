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

        /// <summary>
        /// SMSG_ON_CANCEL_EXPECTED_RIDE_VEHICLE_AURA dla Forever 1.60.1.
        ///
        /// Handler w WowPacketParser/Parsing/Parsers/SpellHandler.cs (HandleSpellNull) jest PUSTY,
        /// a serwer wysyla 4 bajty - w sniffie 17.11 11:53 cztery razy, zawsze ten sam rozmiar:
        ///   BE C7 01 00 | A4 BD 01 00 | B7 D0 01 00 | 2C BE 01 00
        /// czyli jedno uint32. Wartosci (116670, 114052, 118967, 114220) nie sa jednak SpellID-em:
        /// zadna nie wystepuje w tym sniffie jako SpellID w SMSG_AURA_UPDATE/SPELL_GO, a 118967
        /// jest w TDB jako creature_template (Hillstalker Worg), nie spell. Dlatego pole zostaje
        /// bez nazwy - czytamy je, zeby pakiet byl zjedzony do konca, ale NIE twierdzimy, co znaczy.
        /// </summary>
        [Parser(Opcode.SMSG_ON_CANCEL_EXPECTED_RIDE_VEHICLE_AURA)]
        public static void HandleOnCancelExpectedRideVehicleAura(Packet packet)
        {
            packet.ReadUInt32("Unk");
        }
    }
}
