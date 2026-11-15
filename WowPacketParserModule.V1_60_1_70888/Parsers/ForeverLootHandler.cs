using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;
using WowPacketParserModule.Substructures;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_LOOT_RESPONSE (0x4700BF) Forever 1.60.1: uklad jak w 12.x
    /// (Owner, LootObj, FailureReason, AcquireReason, LootMethod, Threshold, Coins, ItemCount, CurrencyCount, itemy, bity).
    /// Poza odczytem zapisuje okno do Storage.ForeverLootWindows, z czego builder liczy creature_loot_template.
    /// </summary>
    public static class ForeverLootHandler
    {
        [Parser(Opcode.SMSG_LOOT_RESPONSE)]
        public static void HandleLootResponse(Packet packet)
        {
            var owner = packet.ReadPackedGuid128("Owner");
            packet.ReadPackedGuid128("LootObj");
            packet.ReadByteE<LootError>("FailureReason");
            var reason = packet.ReadByte("AcquireReason");
            packet.ReadByteE<LootMethod>("LootMethod");
            packet.ReadByteE<ItemQuality>("Threshold");
            packet.ReadUInt32("Coins");
            var itemCount = packet.ReadUInt32("ItemCount");
            var currencyCount = packet.ReadUInt32("CurrencyCount");

            var window = new ForeverLootWindow { Owner = owner, Entry = owner.GetEntry(), AcquireReason = reason };

            for (var i = 0; i < itemCount; ++i)
            {
                packet.ResetBitReader();
                packet.ReadBits("ItemType", 2, "Items", i);
                packet.ReadBits("ItemUiType", 3, "Items", i);
                packet.ReadBit("CanTradeToTapList", "Items", i);
                var quantity = packet.ReadUInt32("Quantity", "Items", i);
                packet.ReadByte("LootItemType", "Items", i);
                packet.ReadByte("LootListID", "Items", i);
                var item = WowPacketParserModule.Substructures.ItemHandler.ReadItemInstance(packet, "Items", i, "Loot");
                window.Items.Add((item.ItemID, quantity));
            }

            for (var i = 0; i < currencyCount; ++i)
                WowPacketParserModule.V6_0_2_19033.Parsers.LootHandler.ReadCurrenciesData(packet, "Currencies", i);

            packet.ResetBitReader();
            packet.ReadBit("Acquired");
            packet.ReadBit("AELooting");
            packet.ReadBit("PersonalLooting");

            if (owner.GetHighType() == HighGuidType.Creature && window.Entry != 0)
                Storage.ForeverLootWindows.Add(window);
        }
    }
}
