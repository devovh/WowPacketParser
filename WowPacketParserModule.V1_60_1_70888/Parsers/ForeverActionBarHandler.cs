using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_UPDATE_ACTION_BUTTONS dla Forever 1.60.1.
    ///
    /// tc-forever (Server/Packets/SpellPackets.cpp:65 UpdateActionButtons::Write,
    /// SpellPackets.h:124 ClassicNumActionButtons = 360):
    ///   [360 x uint64 ActionButton][uint8 Reason]  =  360*8 + 1 = 2881 bajtow.
    ///
    /// Fallback (V4_4_0_54481) czyta 180 przyciskow - dla Forever to o polowe za malo,
    /// zostaje 1440 bajtow. Rozklad uint64 jest ten sam co w 4.4:
    ///   dolne 56 bitow = akcja (spell/macro/item), gorny bajt = typ.
    /// </summary>
    public static class ForeverActionBarHandler
    {
        private const int ButtonCount = 360;

        [Parser(Opcode.SMSG_UPDATE_ACTION_BUTTONS)]
        public static void HandleUpdateActionButtons(Packet packet)
        {
            for (var i = 0; i < ButtonCount; ++i)
            {
                var packed = packet.ReadUInt64();

                if (packed == 0)
                    continue;

                packet.AddValue("Action", packed & 0x00FFFFFFFFFFFFFF, i);
                packet.AddValue("Type", (byte)((packed >> 56) & 0xFF), i);
            }

            packet.ReadByte("Reason");
        }
    }
}
