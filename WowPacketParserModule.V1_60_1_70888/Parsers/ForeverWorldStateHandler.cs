using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>Male pakiety Forever 70938 wyprowadzone z bajtow sniffa (225 + 12 pakietow).</summary>
    public static class ForeverWorldStateHandler
    {
        /// <summary>We wszystkich 225 pakietach sniffa: 4 bajty zer = u32 licznik 0 (lista pusta).</summary>
        [Parser(Opcode.SMSG_ACTIVE_SCHEDULED_WORLD_STATE_INFO)]
        public static void HandleActiveScheduledWorldStateInfo(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            if (count > 0)
                packet.ReadToEnd();
        }

        /// <summary>12 pakietow po 8 bajtow, same zera; znaczenie pol nieznane.</summary>
        [Parser(Opcode.SMSG_UPDATE_RECENT_PLAYER_GUIDS)]
        public static void HandleUpdateRecentPlayerGuids(Packet packet)
        {
            packet.ReadUInt32("Unknown1");
            packet.ReadUInt32("Unknown2");
            packet.ReadToEnd();
        }
    }
}
