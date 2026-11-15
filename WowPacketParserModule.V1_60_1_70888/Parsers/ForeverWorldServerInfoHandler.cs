using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using CoreParsers = WowPacketParser.Parsing.Parsers;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_WORLD_SERVER_INFO (0x470046) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (MiscPackets.cpp:224 WorldServerInfo::Write):
    ///   [i16 DifficultyID][GUID HouseGUID][GUID HouseOwnerAccountGUID]
    ///   [GUID HouseCosmeticOwnerGUID][GUID NeighborhoodGUID]
    ///   [bits: IsTournamentRealm, XRealmPvpAlert, BlockExitingLoadingScreen,
    ///          HasRestrictedAccountMaxLevel, HasRestrictedAccountMaxMoney, HasInstanceGroupSize]
    ///   [u32 RestrictedAccountMaxLevel][u64 RestrictedAccountMaxMoney][u32 InstanceGroupSize]
    ///
    /// GUID-y maja forme Forever (maska u16 + bajty), nie ReadPackedGuid128 - dlatego pakiet
    /// ma 11 bajtow: 2 + 4 x 2 (puste GUID-y) + 1 bajt bitow.
    /// </summary>
    public static class ForeverWorldServerInfoHandler
    {
        [Parser(Opcode.SMSG_WORLD_SERVER_INFO)]
        public static void HandleWorldServerInfo(Packet packet)
        {
            CoreParsers.MovementHandler.CurrentDifficultyID = (uint)packet.ReadInt16<DifficultyId>("DifficultyID");

            ForeverGuid.Read(packet, "HouseGUID");
            ForeverGuid.Read(packet, "HouseOwnerAccountGUID");
            ForeverGuid.Read(packet, "HouseCosmeticOwnerGUID");
            ForeverGuid.Read(packet, "NeighborhoodGUID");

            packet.ResetBitReader();
            packet.ReadBit("IsTournamentRealm");
            packet.ReadBit("XRealmPvpAlert");
            packet.ReadBit("BlockExitingLoadingScreen");
            var hasMaxLevel = packet.ReadBit("HasRestrictedAccountMaxLevel");
            var hasMaxMoney = packet.ReadBit("HasRestrictedAccountMaxMoney");
            var hasGroupSize = packet.ReadBit("HasInstanceGroupSize");

            if (hasMaxLevel)
                packet.ReadUInt32("RestrictedAccountMaxLevel");

            if (hasMaxMoney)
                packet.ReadUInt64("RestrictedAccountMaxMoney");

            if (hasGroupSize)
                packet.ReadUInt32("InstanceGroupSize");
        }
    }
}

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_GUILD_EVENT_PRESENCE_CHANGE (0x530037) dla Forever 1.60.1.
    ///
    /// Pakiety maja 8 bajtow: GUID Forever (maska u16 + 5 bajtow = 7 B) i jeden bajt bitow
    /// [6 bitow dlugosc nazwy][LoggedOn][Mobile]. W tym buildzie nie ma VirtualRealmAddress
    /// ani drugiego (9-bitowego) pola, ktore opisuje zrodlo tc-forever dla starszego buildu.
    /// Nazwa (jesli dlugosc > 0) idzie na koncu.
    /// </summary>
    public static class ForeverGuildPresenceHandler
    {
        [Parser(Opcode.SMSG_GUILD_EVENT_PRESENCE_CHANGE)]
        public static void HandleGuildEventPresenceChange(Packet packet)
        {
            ForeverGuid.Read(packet, "Guid");

            packet.ResetBitReader();
            var nameLength = packet.ReadBits("NameLength", 6);
            packet.ReadBit("LoggedOn");
            packet.ReadBit("Mobile");

            if (nameLength > 0)
                packet.ReadWoWString("Name", nameLength);
        }
    }
}
