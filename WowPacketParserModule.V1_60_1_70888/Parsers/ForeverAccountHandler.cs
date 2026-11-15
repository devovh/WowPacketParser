using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Male pakiety Forever 1.60.1 z ukladem z tc-forever.
    /// </summary>
    public static class ForeverAccountHandler
    {
        /// <summary>
        /// SMSG_GROUP_DECLINE (PartyPackets.cpp:151): [9 bitow dlugosc][FlushBits][Name]. Retailowy odczyt
        /// czytal C-string i nie domykal pakietu.
        /// </summary>
        [Parser(Opcode.SMSG_GROUP_DECLINE)]
        public static void HandleGroupDecline(Packet packet)
        {
            var len = packet.ReadBits("NameLength", 9);
            packet.ResetBitReader();
            packet.ReadWoWString("Name", len);
        }

        /// <summary>
        /// CMSG_AUTOBANK_ITEM (BankPackets.cpp:23): [InvUpdate: 2 bity liczby, pary (ContainerSlot, Slot)]
        /// [u8 BankType][u8 Bag][u8 Slot]. Retail nie ma pola BankType.
        /// </summary>
        [Parser(Opcode.CMSG_AUTOBANK_ITEM)]
        public static void HandleAutoBankItem(Packet packet)
        {
            var count = packet.ReadBits("InvItemCount", 2);
            packet.ResetBitReader();
            for (var i = 0; i < count; ++i)
            {
                packet.ReadByte("ContainerSlot", i);
                packet.ReadByte("Slot", i);
            }

            packet.ReadByte("BankType");
            packet.ReadByte("Bag");
            packet.ReadByte("Slot");
        }

        /// <summary>
        /// SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT: wpis konta ma tak jak lista postaci nazwisko.
        /// Bity: [6 dl. imienia][9 dl. realmu][3 nieznane][6 dl. nazwiska], potem Name, RealmName, Surname.
        /// Na koncu bit ConsoleCommand. Uklad zmierzony na pakiecie 59 B: "Imperator" + "Alfa".
        /// </summary>
        [Parser(Opcode.SMSG_GET_ACCOUNT_CHARACTER_LIST_RESULT)]
        public static void HandleGetAccountCharacterListResult(Packet packet)
        {
            packet.ReadUInt32("Token");
            var count = packet.ReadUInt32("CharactersCount");

            for (var i = 0u; i < count; ++i)
            {
                packet.ReadPackedGuid128("WowAccount", "Characters", i);
                packet.ReadPackedGuid128("Guid", "Characters", i);
                packet.ReadUInt32("VirtualRealmAddress", "Characters", i);
                packet.ReadByteE<Race>("RaceID", "Characters", i);
                packet.ReadByteE<Class>("ClassID", "Characters", i);
                packet.ReadByteE<Gender>("SexID", "Characters", i);
                packet.ReadByte("ExperienceLevel", "Characters", i);
                packet.ReadTime64("LastActiveTime", "Characters", i);
                packet.ReadInt32("ContentSetID", "Characters", i);

                packet.ResetBitReader();
                var nameLen = packet.ReadBits("NameLength", 6, "Characters", i);
                var realmLen = packet.ReadBits("RealmNameLength", 9, "Characters", i);
                packet.ReadBits("Unknown", 3, "Characters", i);
                var surnameLen = packet.ReadBits("SurnameLength", 6, "Characters", i);

                packet.ReadWoWString("Name", nameLen, "Characters", i);
                packet.ReadWoWString("RealmName", realmLen, "Characters", i);
                packet.ReadWoWString("Surname", surnameLen, "Characters", i);
            }

            packet.ResetBitReader();
            packet.ReadBit("ConsoleCommand");
        }
    }
}
