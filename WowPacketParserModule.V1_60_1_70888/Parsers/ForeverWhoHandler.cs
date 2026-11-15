using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_WHO (0x4C0002) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (WhoPackets.cpp:110-150):
    ///   [u32 Token][u32 Count]
    ///   per wpis: PlayerGuidLookupData (z nazwiskiem i dodatkowa 9-bitowa dlugoscia),
    ///             [Guid GuildGUID][u32 GuildVirtualRealmAddress][i32 AreaID][i32][i32]
    ///             [4 x u32][i8]
    ///             [7 bitow GuildName][bit IsGM][bit][FlushBits][GuildName]
    /// W 12.x licznik to 6 bitow bez dodatkowych pol, stad blad w starym parserze.
    /// </summary>
    public static class ForeverWhoHandler
    {
        [Parser(Opcode.SMSG_WHO)]
        public static void HandleWho(Packet packet)
        {
            packet.ReadUInt32("RequestID");
            var count = packet.ReadUInt32("Count");

            for (var i = 0; i < count; ++i)
            {
                WowPacketParserModule.V8_0_1_27101.Parsers.CharacterHandler.ReadPlayerGuidLookupData(packet, i, "PlayerData");

                packet.ResetBitReader();
                packet.ReadPackedGuid128("GuildGUID", i);
                packet.ReadUInt32("GuildVirtualRealmAddress", i);
                packet.ReadInt32("AreaID", i);
                packet.ReadInt32("Unknown1", i);
                packet.ReadInt32("Unknown2", i);
                for (var j = 0; j < 4; ++j)
                    packet.ReadUInt32("Unknown3", i, j);
                packet.ReadSByte("Unknown4", i);

                var guildNameLength = packet.ReadBits(7);
                packet.ReadBit("IsGM", i);
                packet.ReadBit("Unknown5", i);
                packet.ResetBitReader();

                packet.ReadWoWString("GuildName", guildNameLength, i);
            }
        }
    }
}
