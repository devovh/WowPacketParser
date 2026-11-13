using System;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Sesja świata Forever (1.60.1): wejście do świata, MOTD, czas, wylogowanie.
    /// Numeracja wire — patrz ForeverOpcodeHelper.
    /// </summary>
    public static class WorldSessionHandler
    {
        [Parser(Opcode.SMSG_LOGIN_VERIFY_WORLD)]
        public static void HandleLoginVerifyWorld(Packet packet)
        {
            packet.ReadInt32<MapId>("MapID");
            packet.ReadVector4("Position");
            packet.ReadSingle("Orientation");
        }

        [Parser(Opcode.SMSG_MOTD)]
        public static void HandleMessageOfTheDay(Packet packet)
        {
            var lineCount = packet.ReadInt32("LineCount");

            for (var i = 0; i < lineCount; i++)
                packet.ReadCString("Line", i);
        }

        [Parser(Opcode.SMSG_TIME_SYNC_REQUEST)]
        public static void HandleTimeSyncRequest(Packet packet)
        {
            packet.ReadUInt32("SequenceIndex");
        }

        [Parser(Opcode.SMSG_LOGOUT_RESPONSE)]
        public static void HandleLogoutResponse(Packet packet)
        {
            packet.ReadInt32("Reason");
            packet.ReadBit("Instant");
        }

        [Parser(Opcode.SMSG_LOGOUT_COMPLETE)]
        public static void HandleLogoutComplete(Packet packet)
        {
        }

        [Parser(Opcode.SMSG_LOGOUT_CANCEL_ACK)]
        public static void HandleLogoutCancelAck(Packet packet)
        {
        }

        [Parser(Opcode.SMSG_TUTORIAL_FLAGS)]
        public static void HandleTutorialFlags(Packet packet)
        {
            var count = packet.ReadInt32("TutorialCount");

            for (var i = 0; i < count; i++)
                packet.ReadUInt32("TutorialFlag", i);
        }

        [Parser(Opcode.SMSG_ACCOUNT_DATA_TIMES)]
        public static void HandleAccountDataTimes(Packet packet)
        {
            packet.ReadTime64("ServerTime");

            for (var i = 0; i < 32; i++)
                packet.ReadTime64("AccountDataType", i);
        }

        [Parser(Opcode.CMSG_LOGOUT_REQUEST)]
        public static void HandleLogoutRequest(Packet packet)
        {
            packet.ReadBit("IdleLogout");
        }
    }
}
