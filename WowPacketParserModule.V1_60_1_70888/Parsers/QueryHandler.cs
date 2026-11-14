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
                var data = V8_0_1_27101.Parsers.CharacterHandler.ReadPlayerGuidLookupData(packet, idx, "PlayerGuidLookupData");
                response.Race = (uint)data.Race;
                response.Gender = (uint)data.Gender;
                response.Class = (uint)data.Class;
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
