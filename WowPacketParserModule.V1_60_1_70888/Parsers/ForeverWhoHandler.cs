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

        /// <summary>
        /// CMSG_WHO (0x4400AF): w sniffie 70938 ten opcode niesie zapytanie /who, nie RESURRECT_RESPONSE.
        /// Uklad z tc-forever WhoPackets.cpp:61 ReadWhoRequest (+ Token, Origin, bit IsAddon).
        /// </summary>
        [Parser(Opcode.CMSG_WHO)]
        public static void HandleWhoRequest(Packet packet)
        {
            packet.ReadInt32("MinLevel");
            packet.ReadInt32("MaxLevel");
            packet.ReadInt32("RaceFilter", 0);
            packet.ReadInt32("RaceFilter", 1);
            packet.ReadInt32("ClassFilter");

            packet.ResetBitReader();
            var nameLen = packet.ReadBits("NameLength", 6);
            var realmLen = packet.ReadBits("VirtualRealmNameLength", 9);
            var surnameLen = packet.ReadBits("SurnameLength", 9);
            var guildLen = packet.ReadBits("GuildLength", 7);
            var guildRealmLen = packet.ReadBits("GuildVirtualRealmNameLength", 9);
            var wordCount = packet.ReadBits("WordCount", 3);
            packet.ReadBit("ShowEnemies");
            packet.ReadBit("ShowArenaPlayers");
            packet.ReadBit("ExactName");
            packet.ReadBit("Unknown");
            var areaCount = packet.ReadBits("AreaCount", 6);
            var hasServerInfo = packet.ReadBit("HasServerInfo");
            packet.ResetBitReader();

            packet.ReadWoWString("Name", nameLen);
            packet.ReadWoWString("VirtualRealmName", realmLen);
            packet.ReadWoWString("Surname", surnameLen);
            packet.ReadWoWString("Guild", guildLen);
            packet.ReadWoWString("GuildVirtualRealmName", guildRealmLen);

            for (var i = 0; i < wordCount; ++i)
            {
                packet.ResetBitReader();
                var len = packet.ReadBits("WordLength", 7, i);
                packet.ResetBitReader();
                packet.ReadWoWString("Word", len, i);
            }

            for (var i = 0; i < areaCount; ++i)
                packet.ReadInt32("AreaID", i);

            if (hasServerInfo)
            {
                packet.ReadByte("FactionGroup", "ServerInfo");
                packet.ReadInt32("Locale", "ServerInfo");
                packet.ReadUInt32("RequesterVirtualRealmAddress", "ServerInfo");
                for (var i = 0; i < 5; ++i)
                    packet.ReadUInt32("Skipped", "ServerInfo", i);
                packet.ReadByte("Skipped", "ServerInfo");
            }

            packet.ReadUInt32("Token");
            packet.ReadByte("Origin");
            packet.ResetBitReader();
            packet.ReadBit("IsAddon");
        }
    }
}
