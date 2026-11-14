using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_QUERY_CREATURE_RESPONSE (0x4B0006) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (Server/Packets/QueryPackets.cpp:75 QueryCreatureResponse::Write):
    ///
    ///   [bits 6: liczba wpisow][FlushBits]
    ///   per wpis:
    ///     [u32 CreatureID][u8 result]     0 = dane sa, 1 = brak danych
    ///     jesli result == 0:
    ///       [bits 11 Title][bits 11 TitleAlt][bits 6 CursorName][bit Leader]
    ///       [4 x (bits 11 Name[i], bits 11 NameAlt[i])]
    ///       [teksty Name/NameAlt][u32 TypeFlags x N][u8 CreatureType][i32 Family]
    ///       [i8 Classification][2 x i32 ProxyCreatureID]
    ///       [u32 DisplayCount][f32 TotalProbability][per display: u32 id, f32 scale, f32 prob]
    ///       [f32 HpMulti][f32 EnergyMulti][u32 QuestItems][u32 QuestCurrencies]
    ///       [8 x i32: MovementInfoID .. WidgetSetUnitConditionID]
    ///       [Title][TitleAlt][CursorName][QuestItems][QuestCurrencies]
    ///
    /// Fallback (9.x) czyta od razu u32 jako "Entry" i bit HasData, przez co dla pakietu
    /// 6-bajtowego (77 sztuk w snifie) zostaje 1 bajt, a dla pakietow z danymi (120-163 B)
    /// zostaje 115-158 bajtow.
    ///
    /// 6 B: 04 | 1F 34 04 00 | 01  ->  count=1, CreatureID=275487, result=1 (brak danych)
    /// </summary>
    public static class ForeverQueryCreatureHandler
    {
        [Parser(Opcode.SMSG_QUERY_CREATURE_RESPONSE)]
        public static void HandleCreatureQueryResponse(Packet packet)
        {
            packet.ResetBitReader();
            var count = packet.ReadBits("Count", 6);
            packet.ResetBitReader();

            for (var e = 0; e < count; ++e)
            {
                var entry = packet.ReadInt32("Entry", e);
                var result = packet.ReadByte("Result", e);

                if (result != 0)
                    continue;

                ReadCreatureStats(packet, (uint)entry, e);
            }
        }

        private static void ReadCreatureStats(Packet packet, uint entry, int e)
        {
            packet.ResetBitReader();
            var titleLen = packet.ReadBits("TitleLength", 11, e);
            var titleAltLen = packet.ReadBits("TitleAltLength", 11, e);
            var cursorNameLen = packet.ReadBits("CursorNameLength", 6, e);
            packet.ReadBit("Leader", e);

            var nameLens = new int[4];
            var nameAltLens = new int[4];
            for (var i = 0; i < 4; ++i)
            {
                nameLens[i] = (int)packet.ReadBits("NameLength", 11, e, i);
                nameAltLens[i] = (int)packet.ReadBits("NameAltLength", 11, e, i);
            }

            var names = new string[4];
            var namesAlt = new string[4];
            for (var i = 0; i < 4; ++i)
            {
                if (nameLens[i] > 1)
                    names[i] = packet.ReadCString("Name", e, i);
                if (nameAltLens[i] > 1)
                    namesAlt[i] = packet.ReadCString("NameAlt", e, i);
            }

            // Forever: tyle samo flag co w retail (4 x u32).
            for (var i = 0; i < 4; ++i)
                packet.ReadUInt32("TypeFlags", e, i);

            packet.ReadByteE<CreatureType>("CreatureType", e);
            packet.ReadInt32E<CreatureFamily>("CreatureFamily", e);
            packet.ReadSByteE<CreatureRank>("Classification", e);

            for (var i = 0; i < 2; ++i)
                packet.ReadInt32("ProxyCreatureID", e, i);

            var displayCount = packet.ReadUInt32("DisplayIdCount", e);
            packet.ReadSingle("TotalProbability", e);

            for (var i = 0; i < displayCount; ++i)
            {
                packet.ReadUInt32("CreatureDisplayID", e, i);
                packet.ReadSingle("DisplayScale", e, i);
                packet.ReadSingle("Probability", e, i);
            }

            packet.ReadSingle("HpMulti", e);
            packet.ReadSingle("EnergyMulti", e);
            packet.ReadUInt32("QuestItems", e);
            packet.ReadUInt32("QuestCurrencies", e);

            for (var i = 0; i < 8; ++i)
                packet.ReadInt32("UnkInt32", e, i);

            if (titleLen > 1)
                packet.ReadCString("Title", e);
            if (titleAltLen > 1)
                packet.ReadCString("TitleAlt", e);
            if (cursorNameLen > 1)
                packet.ReadCString("CursorName", e);

            packet.AddSniffData(StoreNameType.Unit, (int)entry, "QUERY_RESPONSE");
        }
    }
}
