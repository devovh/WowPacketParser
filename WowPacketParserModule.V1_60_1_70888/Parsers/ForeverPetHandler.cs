using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Hunter pet packets, 70938. Layouts from the tc-forever writers / readers, all packets of
    /// the 2026-11-15 live capture (hunter with a pet) decode to the last byte.
    /// </summary>
    public static class ForeverPetHandler
    {
        [Parser(Opcode.SMSG_PET_SPELLS_MESSAGE)]
        public static void HandlePetSpellsMessage(Packet packet)
        {
            ForeverGuid.Read(packet, "PetGUID");
            packet.ReadUInt16("CreatureFamily");
            packet.ReadUInt16("Specialization");
            packet.ReadUInt32("TimeLimit");
            packet.ReadByte("CommandState");
            packet.ReadByte("Flag");
            packet.ReadByte("ReactState");

            for (var i = 0; i < 18; ++i)
                packet.ReadUInt32("ActionButton", i);

            var actionCount = packet.ReadUInt32("ActionCount");
            var cooldownCount = packet.ReadUInt32("CooldownCount");
            var historyCount = packet.ReadUInt32("SpellHistoryCount");

            for (var i = 0; i < actionCount; ++i)
                packet.ReadUInt32("Action", i);

            for (var i = 0; i < cooldownCount; ++i)
            {
                packet.ReadInt32("SpellID", i, "Cooldown");
                packet.ReadInt32("Duration", i, "Cooldown");
                packet.ReadInt32("CategoryDuration", i, "Cooldown");
                packet.ReadSingle("ModRate", i, "Cooldown");
                packet.ReadUInt16("Category", i, "Cooldown");
            }

            for (var i = 0; i < historyCount; ++i)
            {
                packet.ReadInt32("CategoryID", i, "SpellHistory");
                packet.ReadInt32("RecoveryTime", i, "SpellHistory");
                packet.ReadSingle("ChargeModRate", i, "SpellHistory");
                packet.ReadSByte("ConsumedCharges", i, "SpellHistory");
            }
        }

        [Parser(Opcode.SMSG_PET_ACTION_FEEDBACK)]
        public static void HandlePetActionFeedback(Packet packet)
        {
            packet.ReadInt32("Response");
            packet.ReadInt32<SpellId>("SpellID");
        }

        [Parser(Opcode.CMSG_PET_ABANDON)]
        public static void HandlePetAbandon(Packet packet)
        {
            ForeverGuid.Read(packet, "Pet");
        }
    }
}
