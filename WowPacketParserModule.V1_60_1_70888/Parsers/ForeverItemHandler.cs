using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// CMSG_BUY_ITEM dla Forever 1.60.1 — układ podany przez dewelopera 2026-11-17.
    ///
    ///   VendorGUID     guid Forever (u16 maska, potem popcount(mask) bajtow)
    ///   ContainerGUID  guid Forever (pusty = maska 0x0000, razem 2 bajty)
    ///   Item           ItemInstance (i32 ItemID, 7b ModCount + flush, ModCount x [u8 Type, i32 Value],
    ///                  1b HasItemBonus + flush, [u8 Context, u32 n, n x i32 BonusListID])
    ///   Quantity       i32
    ///   Muid           u32
    ///   Slot           u32
    ///   ItemType       i32
    ///
    /// Sprawdzone na trzech pakietach ze sniffu 17.11 11:53 (43, 41 i 37 bajtow):
    ///   43 B, maska 0xEF67 (12 bajtow GUID-a), ItemID 2901, Quantity 1, Muid 1, Slot 255
    ///   41 B, maska 0xE667 (10 bajtow GUID-a), ItemID 2493, Quantity 1, Muid 6, Slot 255
    ///   37 B, maska 0xE767 (11 bajtow GUID-a), ItemID rozny, bez modyfikatorow
    /// Wszystkie trzy koncza sie dokladnie na ostatnim bajcie.
    ///
    /// Stary czytnik (ItemHandler.cs) czytal zupelnie inny uklad, dlatego Quantity/Muid/Slot
    /// wychodzily jako 2320 / 65536 / 458752.
    /// </summary>
    public static class ForeverItemHandler
    {
        [Parser(Opcode.CMSG_BUY_ITEM)]
        public static void HandleBuyItem(Packet packet)
        {
            ForeverGuid.Read(packet, "VendorGUID");
            ForeverGuid.Read(packet, "ContainerGUID");
            ForeverDeveloperLayouts4Handler.ReadItemInstance(packet, "Item");
            packet.ReadInt32("Quantity");
            packet.ReadUInt32("Muid");
            packet.ReadUInt32("Slot");
            packet.ReadInt32("ItemType");
        }
    }
}
