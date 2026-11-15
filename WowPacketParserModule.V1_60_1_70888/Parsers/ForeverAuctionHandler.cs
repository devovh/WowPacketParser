using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_AUCTION_LIST_BUCKETS_RESULT (0x4702F7), tc-forever AuctionHousePackets.cpp:573 (AuctionListBucketsResult::Write):
    ///   [u32 count][u32 DesiredDelay][i32 Filters][i32 TotalCount] count x BucketInfo [bit BrowseMode][bit HasMoreResults]
    /// BucketInfo (AuctionHousePackets.cpp:147):
    ///   Key [bits 20 ItemID, bit hasPet, bits 11 ItemLevel, bit hasSuffix, flush, u16 pet, u16 suffix]
    ///   [i32 TotalQuantity][i32 RequiredLevel][u64 MinPrice][u32 n][n x i32 ItemModifiedAppearanceID]
    ///   [4 bity optional: MaxBattlePetQuality, MaxBattlePetLevel, BattlePetBreedID, BattlePetLevelMask][bit ContainsOwnerItem]
    ///   [bit ContainsOnlyCollectedAppearances][flush] i dane optional (u8, u8, u8, u32)
    /// </summary>
    public static class ForeverAuctionHandler
    {
        public static void ReadBucketKeyPublic(Packet packet, params object[] idx) => ReadBucketKey(packet, idx);

        private static void ReadBucketKey(Packet packet, params object[] idx)
        {
            packet.ResetBitReader();
            packet.ReadBits("ItemID", 20, idx);
            var hasPet = packet.ReadBit("HasBattlePetSpeciesID", idx);
            packet.ReadBits("ItemLevel", 11, idx);
            var hasSuffix = packet.ReadBit("HasItemSuffix", idx);
            packet.ResetBitReader();

            if (hasPet)
                packet.ReadUInt16("BattlePetSpeciesID", idx);
            if (hasSuffix)
                packet.ReadUInt16("ItemSuffix", idx);
        }

        /// <summary>
        /// Klient Forever 70938 dopisuje po flagach koszyka 7. bit (HasExtraBlock) i wtedy: u32 (= ItemID koszyka) oraz blok
        /// o zmiennej dlugosci (2, 7 lub 12 bajtow, tresc niezidentyfikowana, tc-forever go nie zapisuje). Dlugosc dobieramy tak,
        /// aby nastepny koszyk mial sensowny naglowek (MinPrice nie mniejsze od poprzedniego, 0 wygladow); dla ostatniego koszyka
        /// blok konczy sie przed ostatnim bajtem z bitami BrowseMode i HasMoreResults.
        /// </summary>
        private static void ReadExtraBlock(Packet packet, bool last, ulong minPrice, params object[] idx)
        {
            // ItemInstance (od dewelopera): i32 ItemID, lista modyfikatorow (7 bitow licznika + flush, po u8 typ + i32 wartosc),
            // 1 bit HasBonus + flush, a gdy ustawiony: u8 Context + u32 liczba + liczba x u32 BonusListID.
            packet.ReadInt32("ExtraItemID", idx);
            packet.ResetBitReader();
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
                var bonuses = packet.ReadUInt32("BonusCount", idx);
                for (var j = 0; j < bonuses; ++j)
                    packet.ReadUInt32("BonusListID", idx, j);
            }
        }

        [Parser(Opcode.SMSG_AUCTION_LIST_BUCKETS_RESULT)]
        public static void HandleAuctionListBucketsResult(Packet packet)
        {
            var count = packet.ReadUInt32("BucketsCount");
            packet.ReadUInt32("DesiredDelay");
            packet.ReadInt32("Filters");
            packet.ReadInt32("TotalCount");

            ulong prevMinPrice = 0;
            for (var i = 0; i < count; ++i)
            {
                ReadBucketKey(packet, "Buckets", i, "Key");
                packet.ReadInt32("TotalQuantity", "Buckets", i);
                packet.ReadInt32("RequiredLevel", "Buckets", i);
                var minPrice = packet.ReadUInt64("MinPrice", "Buckets", i);
                var appearances = packet.ReadUInt32("ItemModifiedAppearanceCount", "Buckets", i);
                for (var j = 0; j < appearances; ++j)
                    packet.ReadInt32("ItemModifiedAppearanceID", "Buckets", i, j);

                packet.ResetBitReader();
                var hasQuality = packet.ReadBit("HasMaxBattlePetQuality", "Buckets", i);
                var hasLevel = packet.ReadBit("HasMaxBattlePetLevel", "Buckets", i);
                var hasBreed = packet.ReadBit("HasBattlePetBreedID", "Buckets", i);
                var hasMask = packet.ReadBit("HasBattlePetLevelMask", "Buckets", i);
                packet.ReadBit("ContainsOwnerItem", "Buckets", i);
                packet.ReadBit("ContainsOnlyCollectedAppearances", "Buckets", i);
                var hasExtra = packet.ReadBit("HasExtraBlock", "Buckets", i);
                packet.ResetBitReader();

                if (hasQuality)
                    packet.ReadByte("MaxBattlePetQuality", "Buckets", i);
                if (hasLevel)
                    packet.ReadByte("MaxBattlePetLevel", "Buckets", i);
                if (hasBreed)
                    packet.ReadByte("BattlePetBreedID", "Buckets", i);
                if (hasMask)
                    packet.ReadUInt32("BattlePetLevelMask", "Buckets", i);

                if (hasExtra)
                    ReadExtraBlock(packet, i == count - 1, minPrice, "Buckets", i);
                prevMinPrice = minPrice;
            }

            packet.ResetBitReader();
            packet.ReadBit("BrowseMode");
            packet.ReadBit("HasMoreResults");
        }
    }
}
