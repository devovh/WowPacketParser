using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParserModule.Substructures;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Trzy czytniki, ktore na sniffach z 17.11.2026 (70938) konczyly sie bledem lub nieprzeczytanymi bajtami.
    /// Uklad kazdego jest zmierzony na bajtach pakietu, nie zgadniety.
    ///
    /// SMSG_OPEN_CONTAINER (0x6A0006) - 10 bajtow, 6 pakietow w sniffach 15-32-39 i 23-36-25:
    ///   8F E0 | 4F 93 EE 6F | 40 8C 6C 0C
    ///   Pierwsze dwa bajty sa stale (Low Item/0 R7151/S65359 - ta sama torba), potem 4 bajty rozne
    ///   (0x6FEE934F, 0x6FEE761D, 0x6FEE903D - rosnace, wiec licznik/czas, nie GUID), potem stala koncowka.
    ///   Ogolny czytnik 12.x czytal tu GUID128 (16 B) i wyrzucal ArgumentOutOfRangeException (HighGuid 0x0804).
    ///   10 bajtow jest zmierzone; znaczenie pol - nie.
    ///
    /// SMSG_LOOT_ROLL (0x4700C8) i SMSG_LOOT_ROLL_WON (0x4700CC) - 42 bajty, szkielet jak w 7.x/5.5.0, ale
    /// zamiast ItemInstance jest tylko ItemID (u32), bo 12 bajtow modyfikatorow nie wystepuje.
    /// Guidy w tych pakietach nie sa pakowane jak w 12.x (maska low, maska high, potem bajty):
    /// maska, po jednym bajcie, maska, po jednym bajcie - inaczej reszta pakietu sie nie uklada.
    ///   ROLL: [GUID LootObj][GUID Winner][i32 Roll][u8 RollType][u32 ItemID][u32 Quantity][u8 LootItemType][u8 LootListID]
    ///   WON:  [GUID LootObj][i32 Roll][u8 RollType][u32 ItemID][u32 Quantity][u8 LootItemType][u8 LootListID]
    ///   na koncu blok bitowy: ROLL 2 bity (Autopassed, OffSpec), WON 1 bit (MainSpec).
    /// Zmierzone wartosci: Roll 256, RollType 0 (Pass), ItemID 818 / 2881, LootItemType 165, LootListID 74 / 1.
    /// </summary>
    public static class ForeverLootRollHandler
    {
        [Parser(Opcode.SMSG_OPEN_CONTAINER)]
        public static void HandleOpenContainer(Packet packet)
        {
            packet.ReadUInt16("ItemGUIDLow");
            packet.ReadUInt32("UnkCounter");
            packet.ReadUInt32("UnkTail");
        }

        private static void ReadLootGuid(Packet packet, string name, params object[] indexes)
        {
            var lowMask = packet.ReadByte();
            var low = 0ul;
            for (var i = 0; i < 8; ++i)
                if ((lowMask & (1 << i)) != 0)
                    low |= (ulong)packet.ReadByte() << (i * 8);

            var highMask = packet.ReadByte();
            var high = 0ul;
            for (var i = 0; i < 8; ++i)
                if ((highMask & (1 << i)) != 0)
                    high |= (ulong)packet.ReadByte() << (i * 8);

            packet.AddValue(name, new WowGuid128(low, high), indexes);
        }

        [Parser(Opcode.SMSG_LOOT_ROLL)]
        public static void HandleLootRoll(Packet packet)
        {
            ReadLootGuid(packet, "LootObj");
            ReadLootGuid(packet, "Winner");
            packet.ReadInt32("Roll");
            packet.ReadByteE<LootRollType>("RollType");
            packet.ReadUInt32<ItemId>("ItemID");
            packet.ReadUInt32("Quantity");
            packet.ReadByte("LootItemType");
            packet.ReadByte("LootListID");
            packet.ResetBitReader();
            packet.ReadBit("Autopassed");
            packet.ReadBit("OffSpec");
        }

        [Parser(Opcode.SMSG_LOOT_ROLL_WON)]
        public static void HandleLootRollWon(Packet packet)
        {
            ReadLootGuid(packet, "LootObj");
            packet.ReadInt32("Roll");
            packet.ReadByteE<LootRollType>("RollType");
            packet.ReadUInt32<ItemId>("ItemID");
            packet.ReadUInt32("Quantity");
            packet.ReadByte("LootItemType");
            packet.ReadByte("LootListID");
            packet.ResetBitReader();
            packet.ReadBit("MainSpec");
        }
    }
}
