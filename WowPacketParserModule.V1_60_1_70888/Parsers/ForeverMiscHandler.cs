using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Poprawki drobnych pakietów Forever 1.60.1, każdy z układem wziętym z tc-forever.
    /// </summary>
    public static class ForeverMiscHandler
    {
        /// <summary>
        /// SMSG_HEALTH_UPDATE (tc-forever, Server/Packets/CombatPackets.cpp:130 HealthUpdate::Write):
        /// [PackedGuid][int64 Health]. Fallback czytał u32 i zostawiał 1 bajt.
        ///
        /// Uwaga: GUID-em tego pakietu nie udalo sie dopasowac - maska z ReadPackedGuid128
        /// (57350 = 0xE006) daje 5 bajtow i wtedy zostaje 1 nieprzeczytany, a maska u8
        /// (0x06) daje 2 bajty i zostaje 5. Zostawiony odczyt 2-bajtowej maski, bo to on
        /// daje poprawna wartosc Health (203521) - sam GUID jest nadal do ustalenia.
        /// </summary>
        [Parser(Opcode.SMSG_HEALTH_UPDATE)]
        public static void HandleHealthUpdate(Packet packet)
        {
            packet.ReadPackedGuid128("Guid");
            packet.ReadInt64("Health");
        }

        /// <summary>
        /// SMSG_ACCOUNT_MOUNT_UPDATE (tc-forever, Server/Packets/MiscPackets.cpp:743 AccountMountUpdate::Write):
        ///   [bit IsFullUpdate][u32 count]
        ///   dla każdego: [i32 spellId][bity 4 flags]  - WSZYSTKIE flagi w jednym strumieniu bitowym,
        ///   a FlushBits jest dopiero PO całej pętli.
        /// Fallback robił ResetBitReader w każdej iteracji i czytał flagi po jednej, więc przesuwał się
        /// o bajt na każdy mount (przy 2 mountach: 16 nieprzeczytanych bajtów).
        /// </summary>
        [Parser(Opcode.SMSG_ACCOUNT_MOUNT_UPDATE)]
        public static void HandleAccountMountUpdate(Packet packet)
        {
            packet.ReadBit("IsFullUpdate");
            var mountSpellIDsCount = packet.ReadUInt32("MountSpellIDsCount");

            var flags = new uint[mountSpellIDsCount];
            for (var i = 0; i < mountSpellIDsCount; ++i)
                flags[i] = packet.ReadBits(4);

            packet.ResetBitReader();

            for (var i = 0; i < mountSpellIDsCount; ++i)
            {
                packet.ReadInt32("MountSpellIDs", i);
                packet.AddValue("Flags", flags[i], i);
            }
        }
    }
}
