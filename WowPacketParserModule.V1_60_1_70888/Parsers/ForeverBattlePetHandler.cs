using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_BATTLE_PET_JOURNAL (0x470092) dla Forever 1.60.1.
    ///
    /// tc-forever, Server/Packets/BattlePetPackets.cpp:72 (BattlePetJournal::Write) oraz :43
    /// (operator&lt;&lt; BattlePet). Roznica wzgledem retail: po polu Quality jest
    /// **int64 ClassicUnknown**, ktorego parser 7.x/12.x nie czyta. Przez to kazdy kolejny
    /// pet przesuwa sie o 8 bajtow i na koncu zostaje 11 nieprzeczytanych.
    ///
    /// Uklad peta:
    ///   [PackedGuid128 Guid][u32 Species][u32 CreatureID][u32 DisplayID]
    ///   [u16 Breed][u16 Level][u16 Exp][u16 Flags]
    ///   [u32 Power][u32 Health][u32 MaxHealth][u32 Speed][u8 Quality]
    ///   [int64 ClassicUnknown]  &lt;- TEGO BRAKOWALO
    ///   [bits 7 nameLen][bit HasOwnerInfo][bit NoRename][FlushBits][name]
    ///   [OwnerInfo, jesli jest]
    ///
    /// Sniff 70938, pakiet 880 (250 B): 239 B przeczytane, 11 B zostawalo.
    /// </summary>
    public static class ForeverBattlePetHandler
    {
        private static void ReadClientBattlePet(Packet packet, params object[] idx)
        {
            packet.ReadPackedGuid128("BattlePetGUID", idx);

            packet.ReadInt32("SpeciesID", idx);
            packet.ReadInt32("CreatureID", idx);
            packet.ReadInt32("DisplayID", idx);

            packet.ReadInt16("BreedID", idx);
            packet.ReadInt16("Level", idx);
            packet.ReadInt16("Xp", idx);
            packet.ReadInt16("BattlePetDBFlags", idx);

            packet.ReadInt32("Power", idx);
            packet.ReadInt32("Health", idx);
            packet.ReadInt32("MaxHealth", idx);
            packet.ReadInt32("Speed", idx);

            packet.ReadByte("BreedQuality", idx);

            // Forever doklada tu 8 bajtow (tc-forever: pet.ClassicUnknown).
            packet.ReadInt64("ClassicUnknown", idx);

            packet.ResetBitReader();

            var customNameLength = packet.ReadBits(7);
            var hasOwnerInfo = packet.ReadBit("HasOwnerInfo", idx);
            packet.ReadBit("NoRename", idx);
            packet.ResetBitReader();

            packet.ReadWoWString("CustomName", customNameLength, idx);

            if (hasOwnerInfo)
                V6_0_2_19033.Parsers.BattlePetHandler.ReadClientBattlePetOwnerInfo(packet, "OwnerInfo", idx);
        }

        [Parser(Opcode.SMSG_BATTLE_PET_JOURNAL)]
        public static void HandleBattlePetJournal(Packet packet)
        {
            packet.ReadInt16("Trap");

            var slotsCount = packet.ReadInt32("SlotsCount");
            var petsCount = packet.ReadInt32("PetsCount");

            for (var i = 0; i < slotsCount; i++)
                V6_0_2_19033.Parsers.BattlePetHandler.ReadClientPetBattleSlot(packet, "Slots", i);

            for (var i = 0; i < petsCount; i++)
                ReadClientBattlePet(packet, "Pets", i);

            packet.ResetBitReader();
            packet.ReadBit("HasJournalLock");
        }
    }
}
