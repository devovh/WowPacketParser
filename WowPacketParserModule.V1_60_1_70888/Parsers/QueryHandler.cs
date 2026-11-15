using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Proto;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_QUERY_PLAYER_NAMES_RESPONSE dla Forever 1.60.1.
    ///
    /// Układ z tc-forever (Server/Packets/QueryPackets.cpp:200-280):
    ///   Size&lt;uint32&gt;(Players)
    ///   dla każdego: [u8 Result][PackedGuid Player][bit HasPlayerGuidLookupData]
    ///                [bit HasGuildGuidLookupData][bit HasHouseGuidLookupData][FlushBits]
    ///                + dane, każdy blok z własnym strumieniem bitowym.
    ///
    /// Różnice względem 12.x, przez które fallback czytał śmieci od pierwszego wyniku:
    ///  * PlayerGuidLookupData ma po długości imienia DWIE 9-bitowe długości (nazwisko i trzecie
    ///    pole, zawsze puste) - patrz CharacterHandler 8.x,
    ///  * GuildGuidLookupData pisze VirtualRealmAddress PRZED Guid, a nie odwrotnie.
    /// </summary>
    public static class QueryHandler
    {
        /// <summary>
        /// PlayerGuidLookupData wg tc-forever QueryPackets.cpp:200: IsDeleted, dlugosc imienia (6 bitow),
        /// dlugosc nazwiska (9), jeszcze jedna dlugosc (9, zawsze 0), 5 x dlugosc odmiany (7); potem odmiany,
        /// guidy, dane liczbowe i na koncu tekst imienia i nazwiska.
        /// </summary>
        public static (byte Race, byte Gender, byte Class, byte Level) ReadPlayerGuidLookupData(Packet packet, params object[] idx)
        {
            packet.ResetBitReader();
            packet.ReadBit("IsDeleted", idx);
            var nameLength = (int)packet.ReadBits(6);
            var surnameLength = (int)packet.ReadBits(9);
            var thirdLength = (int)packet.ReadBits(9);
            var declined = new int[5];
            for (var i = 0; i < 5; ++i)
                declined[i] = (int)packet.ReadBits(7);
            packet.ResetBitReader();

            for (var i = 0; i < 5; ++i)
                packet.ReadWoWString("Name Declined", declined[i], i, idx);

            packet.ReadPackedGuid128("AccountID", idx);
            packet.ReadPackedGuid128("BnetAccountID", idx);
            packet.ReadPackedGuid128("Player Guid", idx);
            packet.ReadUInt64("GuildClubMemberID", idx);
            packet.ReadUInt32("VirtualRealmAddress", idx);
            var race = packet.ReadByteE<Race>("Race", idx);
            var gender = packet.ReadByteE<Gender>("Gender", idx);
            var cls = packet.ReadByteE<Class>("Class", idx);
            var level = packet.ReadByte("Level", idx);
            packet.ReadByte("PvpFaction", idx);
            packet.ReadInt32("TimerunningSeasonID", idx);
            packet.ReadWoWString("Name", nameLength, idx);
            packet.ReadWoWString("Surname", surnameLength, idx);
            if (thirdLength > 0)
                packet.ReadWoWString("Unknown", thirdLength, idx);
            return ((byte)race, (byte)gender, (byte)cls, level);
        }

        public static PacketQueryPlayerNameResponse ReadNameCacheLookupResult(Packet packet, params object[] idx)
        {
            var response = new PacketQueryPlayerNameResponse();
            packet.ResetBitReader();
            packet.ReadByte("Result", idx);
            packet.ReadPackedGuid128("Player", idx);
            var hasPlayerGuidLookupData = packet.ReadBit("HasPlayerGuidLookupData", idx);
            var hasGuildGuidLookupData = packet.ReadBit("HasGuildGuidLookupData", idx);
            var hasHouseGuidLookupData = packet.ReadBit("HasHouseGuidLookupData", idx);
            packet.ResetBitReader();

            if (hasPlayerGuidLookupData)
            {
                var data = ReadPlayerGuidLookupData(packet, idx, "PlayerGuidLookupData");
                response.Race = data.Race;
                response.Gender = data.Gender;
                response.Class = data.Class;
                response.Level = data.Level;
                response.HasData = true;
            }

            if (hasGuildGuidLookupData)
            {
                // GuildGuidLookupData: [VirtualRealmAddress][Guid][bity dlugosci 7][FlushBits][tekst]
                packet.ResetBitReader();
                packet.ReadUInt32("VirtualRealmAddress", idx, "GuildGuidLookupData");
                packet.ReadPackedGuid128("Guid", idx, "GuildGuidLookupData");
                var length = packet.ReadBits(7);
                packet.ResetBitReader();
                packet.ReadWoWString("Name", length, idx, "GuildGuidLookupData");
            }

            if (hasHouseGuidLookupData)
            {
                // HouseLookupData: [Guid][bity dlugosci 8][FlushBits][tekst]
                packet.ResetBitReader();
                packet.ReadPackedGuid128("Guid", idx, "HouseGuidLookupData");
                var length = packet.ReadBits(8);
                packet.ResetBitReader();
                packet.ReadWoWString("Name", length, idx, "HouseGuidLookupData");
            }

            return response;
        }

        [Parser(Opcode.SMSG_QUERY_PLAYER_NAMES_RESPONSE)]
        public static void HandleNameQueryResponse(Packet packet)
        {
            var response = packet.Holder.QueryPlayerNameResponse = new();

            var count = packet.ReadUInt32("Count");
            for (var i = 0; i < count; ++i)
                response.Responses.Add(ReadNameCacheLookupResult(packet, i));
        }

        [Parser(Opcode.CMSG_QUERY_PLAYER_NAMES)]
        public static void HandleNameQuery(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            for (var i = 0; i < count; ++i)
                packet.ReadPackedGuid128("Players", i);
        }
    }
}
