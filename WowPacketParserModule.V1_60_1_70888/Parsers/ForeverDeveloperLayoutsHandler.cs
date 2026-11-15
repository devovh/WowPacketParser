using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Uklady od dewelopera serwera (ALFA-OPCODES-70938-REPLY.md, pewnosc A: probka dekoduje sie dokladnie).
    /// Packed guid = ForeverGuid.Read (maska + bajty).
    /// </summary>
    public static class ForeverDeveloperLayoutsHandler
    {
        [Parser(Opcode.CMSG_SUSPEND_TOKEN_RESPONSE)]
        public static void HandleSuspendTokenResponse(Packet packet) { packet.ReadUInt32("SequenceIndex"); }

        [Parser(Opcode.CMSG_UI_MAP_QUEST_LINES_REQUEST)]
        public static void HandleUiMapQuestLinesRequest(Packet packet) { packet.ReadInt32("UiMapID"); }

        [Parser(Opcode.CMSG_QUEUED_MESSAGES_END)]
        public static void HandleQueuedMessagesEnd(Packet packet) { packet.ReadUInt32("Timestamp"); }

        [Parser(Opcode.CMSG_REQUEST_PLAYED_TIME)]
        public static void HandleRequestPlayedTime(Packet packet) { packet.ReadBit("TriggerScriptEvent"); }

        [Parser(Opcode.CMSG_AREA_TRIGGER)]
        public static void HandleAreaTrigger(Packet packet)
        {
            packet.ReadInt32("AreaTriggerID");
            packet.ReadBit("Entered");
            packet.ReadBit("FromClient");
        }

        [Parser(Opcode.SMSG_CHANNEL_NOTIFY_LEFT)]
        public static void HandleChannelNotifyLeft(Packet packet)
        {
            var len = packet.ReadBits(7);
            packet.ReadBit("Suspended");
            packet.ResetBitReader();
            packet.ReadInt32("ChatChannelID");
            packet.ReadWoWString("Channel", len);
        }

        [Parser(Opcode.CMSG_DF_GET_SYSTEM_INFO)]
        public static void HandleDfGetSystemInfo(Packet packet)
        {
            packet.ReadBit("Player");
            var hasParty = packet.ReadBit("HasPartyIndex");
            packet.ResetBitReader();
            if (hasParty)
                packet.ReadByte("PartyIndex");
        }

        [Parser(Opcode.SMSG_QUERY_TIME_RESPONSE)]
        public static void HandleQueryTimeResponse(Packet packet) { packet.ReadTime64("CurrentTime"); }

        [Parser(Opcode.CMSG_TAXI_NODE_STATUS_QUERY)]
        public static void HandleTaxiNodeStatusQuery(Packet packet) { ForeverGuid.Read(packet, "UnitGUID"); }

        [Parser(Opcode.CMSG_SET_SHEATHED)]
        public static void HandleSetSheathed(Packet packet)
        {
            packet.ReadInt32("CurrentSheathState");
            packet.ReadBit("Animate");
        }

        [Parser(Opcode.CMSG_VIOLENCE_LEVEL)]
        public static void HandleViolenceLevel(Packet packet) { packet.ReadSByte("ViolenceLevel"); }

        [Parser(Opcode.SMSG_GUILD_EVENT_PRESENCE_CHANGE)]
        public static void HandleGuildEventPresenceChange(Packet packet)
        {
            ForeverGuid.Read(packet, "Guid");
            packet.ReadUInt32("VirtualRealmAddress");
            var nameLen = packet.ReadBits(6);
            var surnameLen = packet.ReadBits(9);
            packet.ReadBit("LoggedOn");
            packet.ResetBitReader();
            packet.ReadWoWString("Name", nameLen);
            packet.ReadWoWString("Surname", surnameLen);
        }

        [Parser(Opcode.SMSG_LOAD_EQUIPMENT_SET)]
        public static void HandleLoadEquipmentSet(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            if (count > 0)
                packet.ReadToEnd();
        }

        [Parser(Opcode.SMSG_AUCTION_HELLO_RESPONSE)]
        public static void HandleAuctionHelloResponse(Packet packet)
        {
            ForeverGuid.Read(packet, "Auctioneer");
            packet.ReadUInt32("PurchaseDeliveryDelay");
            packet.ReadUInt32("CancelDeliveryDelay");
            packet.ReadInt32("AuctionHouseID");
            packet.ReadBit("OpenForBusiness");
            packet.ResetBitReader();
        }

        [Parser(Opcode.CMSG_GAME_OBJ_REPORT_USE)]
        public static void HandleGameObjReportUse(Packet packet) { ForeverGuid.Read(packet, "Guid"); }

        [Parser(Opcode.CMSG_ATTACK_SWING)]
        public static void HandleAttackSwing(Packet packet) { ForeverGuid.Read(packet, "Victim"); }

        [Parser(Opcode.SMSG_LOG_XP_GAIN)]
        public static void HandleLogXpGain(Packet packet)
        {
            ForeverGuid.Read(packet, "Victim");
            packet.ReadInt32("Original");
            packet.ReadByte("Reason");
            packet.ReadInt32("Amount");
            packet.ReadSingle("GroupBonus");
        }

        [Parser(Opcode.SMSG_GUILD_BANK_REMAINING_WITHDRAW_MONEY)]
        public static void HandleGuildBankRemainingWithdrawMoney(Packet packet) { packet.ReadInt64("RemainingWithdrawMoney"); }

        [Parser(Opcode.CMSG_MOVE_INIT_ACTIVE_MOVER_COMPLETE)]
        public static void HandleMoveInitActiveMoverComplete(Packet packet) { packet.ReadUInt32("Ticks"); }

        [Parser(Opcode.SMSG_SOCIAL_CONTRACT_REQUEST_RESPONSE)]
        public static void HandleSocialContractRequestResponse(Packet packet)
        {
            packet.ReadBit("ShowSocialContract");
            packet.ResetBitReader();
        }

        [Parser(Opcode.SMSG_AUCTION_FAVORITE_LIST)]
        public static void HandleAuctionFavoriteList(Packet packet)
        {
            packet.ReadUInt32("DesiredDelay");
            var count = packet.ReadBits(7);
            packet.ResetBitReader();
            if (count > 0)
                packet.ReadToEnd();
        }

        [Parser(Opcode.SMSG_AUCTION_LIST_BIDDED_ITEMS_RESULT)]
        public static void HandleAuctionListBiddedItemsResult(Packet packet)
        {
            var count = packet.ReadUInt32("ItemCount");
            packet.ReadUInt32("DesiredDelay");
            if (count > 0)
                packet.ReadToEnd();
            else
            {
                packet.ReadBit("HasMoreResults");
                packet.ResetBitReader();
            }
        }

        [Parser(Opcode.SMSG_GENERATE_SSO_TOKEN_RESPONSE)]
        public static void HandleGenerateSsoTokenResponse(Packet packet)
        {
            packet.ReadUInt32("RequestID");
            packet.ReadUInt32("Result");
            packet.ReadTime64("Issued");
            packet.ReadTime64("Expires");
            packet.ReadToEnd();
        }
    }
}
