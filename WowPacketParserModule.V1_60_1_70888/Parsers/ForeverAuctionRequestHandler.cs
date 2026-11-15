using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Zapytania do domu aukcyjnego (AuctionHousePackets.cpp:366-410). TaintedBy (AddOnInfo, AddonPackets.cpp:24) ma
    /// nazwe i wersje jako SizedCString: dlugosc (10 bitow) liczy sie z bajtem zerowym, ktory trzeba przeczytac.
    /// Wspolny ReadAddOnInfo z 8.x czyta o jeden bajt mniej na kazdy napis, przez co sortowanie czytalo smieci.
    /// </summary>
    public static class ForeverAuctionRequestHandler
    {
        private static void ReadTaintedBy(Packet packet, params object[] idx)
        {
            packet.ResetBitReader();
            var nameLength = (int)packet.ReadBits(10);
            var versionLength = (int)packet.ReadBits(10);
            packet.ReadBit("Loaded", idx);
            packet.ReadBit("Disabled", idx);
            packet.ResetBitReader();
            if (nameLength > 0)
                packet.ReadCString("Name", idx);
            if (versionLength > 0)
                packet.ReadCString("Version", idx);
        }

        private static void ReadSort(Packet packet, params object[] idx)
        {
            packet.ResetBitReader();
            packet.ReadByte("SortOrder", idx);
            packet.ReadBit("ReverseSort", idx);
            packet.ResetBitReader();
        }

        private static void ReadTail(Packet packet)
        {
            packet.ResetBitReader();
            var tainted = packet.ReadBit("HasTaintedBy");
            var sortCount = packet.ReadBits(2);
            packet.ResetBitReader();
            if (tainted)
                ReadTaintedBy(packet, "TaintedBy");
            for (var i = 0; i < sortCount; ++i)
                ReadSort(packet, i);
        }

        [Parser(Opcode.CMSG_AUCTION_LIST_ITEMS_BY_BUCKET_KEY, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleListItemsByBucketKey(Packet packet)
        {
            packet.ReadPackedGuid128("Auctioneer");
            ForeverAuctionHandler.ReadBucketKeyPublic(packet, "BucketKey");
            packet.ReadUInt32("Offset");
            packet.ReadByte("Unknown830");
            ReadTail(packet);
        }

        [Parser(Opcode.CMSG_AUCTION_LIST_ITEMS_BY_ITEM_ID, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleListItemsByItemId(Packet packet)
        {
            packet.ReadPackedGuid128("Auctioneer");
            packet.ReadInt32("ItemID");
            packet.ReadInt32("SuffixItemNameDescriptionID");
            packet.ReadUInt32("Offset");
            ReadTail(packet);
        }

        [Parser(Opcode.CMSG_AUCTION_LIST_OWNED_ITEMS, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleListOwnedItems(Packet packet)
        {
            packet.ReadPackedGuid128("Auctioneer");
            packet.ReadUInt32("Offset");
            ReadTail(packet);
        }
    }
}
