using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Uklady od dewelopera, odpowiedz nr 2, sekcja 5 (pewnosc A-: jego writer; w sniffie sa tylko puste przypadki,
    /// wiec niepuste galezie NIE sa sprawdzone na prawdziwych bajtach).
    /// </summary>
    public static class ForeverDeveloperLayouts4Handler
    {
        /// <summary>ItemInstance: i32 ItemID, 7 bitow licznika modyfikatorow + flush, po (u8 typ, i32 wartosc), bit HasBonus + flush, [u8 Context, u32 n, n x i32].</summary>
        public static void ReadItemInstance(Packet packet, params object[] idx)
        {
            packet.ReadInt32("ItemID", idx);
            var mods = packet.ReadBits("ModifierCount", 7, idx);
            packet.ResetBitReader();
            for (var m = 0; m < mods; ++m)
            {
                packet.ReadByte("ModifierType", idx, m);
                packet.ReadInt32("ModifierValue", idx, m);
            }
            var hasBonus = packet.ReadBit("HasBonus", idx);
            packet.ResetBitReader();
            if (hasBonus)
            {
                packet.ReadByte("Context", idx);
                var n = packet.ReadUInt32("BonusListCount", idx);
                for (var j = 0; j < n; ++j)
                    packet.ReadInt32("BonusListID", idx, j);
            }
        }

        [Parser(Opcode.SMSG_GUILD_BANK_QUERY_RESULTS)]
        public static void HandleGuildBankQueryResults(Packet packet)
        {
            packet.ReadUInt64("Money");
            packet.ReadInt32("Tab");
            packet.ReadInt32("WithdrawalsRemaining");
            var tabs = packet.ReadUInt32("TabCount");
            var items = packet.ReadUInt32("ItemCount");

            for (var i = 0u; i < tabs; ++i)
            {
                packet.ReadInt32("TabIndex", "Tabs", i);
                var nameLen = packet.ReadBits(7);
                var iconLen = packet.ReadBits(9);
                packet.ResetBitReader();
                packet.ReadWoWString("Name", nameLen, "Tabs", i);
                packet.ReadWoWString("Icon", iconLen, "Tabs", i);
            }

            for (var i = 0u; i < items; ++i)
            {
                packet.ReadInt32("Slot", "Items", i);
                ReadItemInstance(packet, "Items", i, "Item");
                packet.ReadInt32("Count", "Items", i);
                packet.ReadInt32("EnchantmentID", "Items", i);
                packet.ReadInt32("Charges", "Items", i);
                packet.ReadInt32("OnUseEnchantmentID", "Items", i);
                packet.ReadInt32("Flags", "Items", i);
                var gems = packet.ReadBits("GemCount", 2, "Items", i);
                packet.ReadBit("Locked", "Items", i);
                packet.ResetBitReader();
                for (var g = 0; g < gems; ++g)
                {
                    packet.ReadByte("GemSlot", "Items", i, g);
                    ReadItemInstance(packet, "Items", i, "Gems", g);
                }
            }

            packet.ReadBit("FullUpdate");
            packet.ResetBitReader();
        }

        [Parser(Opcode.SMSG_AUCTION_FAVORITE_LIST)]
        public static void HandleAuctionFavoriteList(Packet packet)
        {
            packet.ReadUInt32("DesiredDelay");
            var count = packet.ReadBits("Count", 7);
            packet.ResetBitReader();
            for (var i = 0; i < count; ++i)
            {
                packet.ReadUInt32("Order", i);
                packet.ReadUInt32("ItemID", i);
                packet.ReadUInt32("ItemLevel", i);
                packet.ReadUInt32("BattlePetSpeciesID", i);
                packet.ReadUInt32("SuffixItemNameDescriptionID", i);
            }
        }

        [Parser(Opcode.SMSG_LOAD_EQUIPMENT_SET)]
        public static void HandleLoadEquipmentSet(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            for (var i = 0u; i < count; ++i)
            {
                packet.ReadInt32("Type", i);
                packet.ReadUInt64("Guid", i);
                packet.ReadUInt32("SetID", i);
                packet.ReadUInt32("IgnoreMask", i);
                for (var p = 0; p < 19; ++p)
                {
                    ForeverGuid.Read(packet, "Piece", i, p);
                    packet.ReadInt32("AppearanceID", i, p);
                }
                for (var e = 0; e < 2; ++e)
                    packet.ReadInt32("Enchant", i, e);
                packet.ReadInt32("SecondaryShoulderAppearanceID", i);
                packet.ReadInt32("SecondaryShoulderSlot", i);
                packet.ReadInt32("SecondaryWeaponAppearanceID", i);
                packet.ReadInt32("SecondaryWeaponSlot", i);
                var hasSpec = packet.ReadBit("HasAssignedSpecIndex", i);
                var nameLen = packet.ReadBits(8);
                var iconLen = packet.ReadBits(9);
                packet.ResetBitReader();
                if (hasSpec)
                    packet.ReadInt32("AssignedSpecIndex", i);
                packet.ReadWoWString("Name", nameLen, i);
                packet.ReadWoWString("Icon", iconLen, i);
            }
        }
    }
}
