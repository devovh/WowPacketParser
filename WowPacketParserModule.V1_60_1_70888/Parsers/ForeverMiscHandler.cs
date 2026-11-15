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
        /// <summary>
        /// CMSG_VOICE_CHAT_LOGIN (0x44013C). tc-forever ma ten opcode jako STATUS_UNHANDLED
        /// (Opcodes.cpp:1171), więc nie ma wzorca z emulatora. Z 8-bajtowego pakietu
        /// widac tylko dwa u32 (1 i 0x417070) - czytamy je jako nieznane pola,
        /// żeby pakiet nie zostawał nieprzeczytany.
        /// </summary>
        [Parser(Opcode.CMSG_VOICE_CHAT_LOGIN)]
        public static void HandleVoiceChatLogin(Packet packet)
        {
            packet.ReadUInt32("ClassicUnknown1");
            packet.ReadUInt32("ClassicUnknown2");
        }

        /// <summary>
        /// CMSG_BATTLEMASTER_JOIN_ARENA (tc-forever, BattlegroundPackets.cpp:170): w Classic 1.60
        /// to jeden bajt - TeamSizeIndex - bez Roles. Retailowy odczyt czytal jeszcze Roles
        /// i rzucal EndOfStream na kazdym pakiecie.
        /// </summary>
        [Parser(Opcode.CMSG_BATTLEMASTER_JOIN_ARENA)]
        public static void HandleBattlemasterJoinArena(Packet packet)
        {
            packet.ReadByte("TeamSizeIndex");
        }

        /// <summary>
        /// CMSG_CONSUMABLE_TOKEN_REDEEM_CONFIRMATION: klient Forever wysyla tylko jeden u32
        /// (pakiet ma 4 bajty); retailowy uklad (Count, TokenGuid, bit) nie istnieje.
        /// </summary>
        [Parser(Opcode.CMSG_CONSUMABLE_TOKEN_REDEEM_CONFIRMATION)]
        public static void HandleConsumableTokenRedeemConfirmation(Packet packet)
        {
            packet.ReadUInt32("UnkInt32");
        }

        /// <summary>
        /// SMSG_GUILD_EVENT_LOG_QUERY_RESULTS (tc-forever, GuildPackets.cpp:281): [u32 count] i
        /// per wpis [GUID Player][GUID Other][u8 TransactionType][u8 RankID][u32 TransactionDate].
        /// Gdy dziennik jest pusty serwer wysyla pakiet o dlugosci 0 (bez nawet licznika),
        /// co jest poprawnym, pustym wynikiem.
        /// </summary>
        [Parser(Opcode.SMSG_GUILD_EVENT_LOG_QUERY_RESULTS)]
        public static void HandleGuildEventLogQueryResults(Packet packet)
        {
            if (packet.Length == 0)
                return;

            var count = packet.ReadInt32("EventEntryCount");
            for (var i = 0; i < count; ++i)
            {
                ForeverGuid.Read(packet, "PlayerGUID", i);
                ForeverGuid.Read(packet, "OtherGUID", i);
                packet.ReadByte("TransactionType", i);
                packet.ReadByte("RankID", i);
                packet.ReadUInt32("TransactionDate", i);
            }
        }
    }
}
