using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Pakiety tworzenia postaci, sprawdzania nazwy, grupy i pojedynku w Classic 70938.
    /// Uklady wg tc-forever (CharacterPackets.cpp, PartyPackets.cpp), potwierdzone na bajtach sniffa 16.11
    /// (np. CHECK_CHARACTER_NAME: "10 0C" + "OresMentis" = nazwa 4 "Ores" + nazwisko 6 "Mentis").
    /// </summary>
    public static class ForeverCharacterHandler
    {
        [Parser(Opcode.CMSG_CHECK_CHARACTER_NAME_AVAILABILITY)]
        public static void HandleCheckCharacterName(Packet packet)
        {
            packet.ReadUInt32("SequenceIndex");
            var nameLen = packet.ReadBits(6);
            packet.ReadBits("Unknown", 3);
            var surnameLen = packet.ReadBits(6);
            packet.ResetBitReader();
            packet.ReadWoWString("Name", nameLen);
            packet.ReadWoWString("Surname", surnameLen);
        }

        [Parser(Opcode.CMSG_CREATE_CHARACTER)]
        public static void HandleCreateCharacter(Packet packet)
        {
            var nameLen = packet.ReadBits(6);
            var hasTemplateSet = packet.ReadBit("HasTemplateSet");
            packet.ReadBit("IsTrialBoost");
            packet.ReadBit("UseNPE");
            packet.ReadBit("HardcoreSelfFound");
            packet.ReadBits("Unknown", 2);
            var surnameLen = packet.ReadBits(6);
            packet.ResetBitReader();

            packet.ReadByteE<Race>("RaceID");
            packet.ReadByteE<Class>("ClassID");
            packet.ReadByteE<Gender>("SexID");
            var count = packet.ReadUInt32("CustomizationsCount");
            packet.ReadInt32("Unknown");
            packet.ReadInt32("TimerunningSeasonID");
            packet.ReadWoWString("Name", nameLen);
            packet.ReadWoWString("Surname", surnameLen);
            if (hasTemplateSet)
                packet.ReadInt32("TemplateSetID");
            for (var i = 0u; i < count; ++i)
            {
                packet.ReadUInt32("ChrCustomizationOptionID", "Customizations", i);
                packet.ReadUInt32("ChrCustomizationChoiceID", "Customizations", i);
            }
        }

        [Parser(Opcode.CMSG_PARTY_UNINVITE)]
        public static void HandlePartyUninvite(Packet packet)
        {
            var hasPartyIndex = packet.ReadBit("HasPartyIndex");
            var reasonLen = packet.ReadBits(9);
            packet.ResetBitReader();
            ForeverGuid.Read(packet, "TargetGUID");
            if (hasPartyIndex)
                packet.ReadByte("PartyIndex");
            packet.ReadWoWString("Reason", reasonLen);
        }

        [Parser(Opcode.SMSG_DUEL_WINNER)]
        public static void HandleDuelWinner(Packet packet)
        {
            // Wg tc-forever: bity 6+6+1, 2x u32 adres realmu, nazwy. W 70938 nazwy sa zakonczone zerem
            // i przed nimi stoi 5 bajtow o nieznanym znaczeniu (grade G).
            packet.ReadBits("BeatenNameLength", 6);
            packet.ReadBits("WinnerNameLength", 6);
            packet.ReadBit("Fled");
            packet.ResetBitReader();
            packet.ReadUInt32("BeatenVirtualRealmAddress");
            packet.ReadUInt32("WinnerVirtualRealmAddress");
            packet.ReadBytes(5);
            packet.ReadCString("BeatenName");
            packet.ReadCString("WinnerName");
        }

        [Parser(Opcode.CMSG_RESURRECT_RESPONSE)]
        public static void HandleResurrectResponse(Packet packet)
        {
            // W sniffie jest pakiet o dlugosci 0 (odmowa bez danych) - czytamy tylko to, co jest.
            if (!packet.CanRead())
                return;
            ForeverGuid.Read(packet, "Resurrecter");
            packet.ReadUInt32("Response");
        }

        [Parser(Opcode.CMSG_AUCTIONABLE_TOKEN_SELL_AT_MARKET_PRICE)]
        public static void HandleTokenSellAtMarketPrice(Packet packet)
        {
            packet.ReadUInt32("ClientToken");
        }
    }
}
