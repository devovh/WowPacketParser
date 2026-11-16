using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Guild, mail, party, corpse and inventory packets, 70938.
    /// Layouts from the tc-forever writers / readers; every packet of these types in the
    /// 2026-11-15 and 2026-11-16 captures decodes to the last byte.
    /// </summary>
    public static class ForeverServerLayoutsHandler
    {
        [Parser(Opcode.SMSG_GUILD_MEMBER_UPDATE_NOTE)]
        public static void HandleGuildMemberUpdateNote(Packet packet)
        {
            ForeverGuid.Read(packet, "Member");
            var noteLength = packet.ReadBits("NoteLength", 8);
            packet.ReadBit("IsPublic");
            packet.ResetBitReader();
            packet.ReadWoWString("Note", noteLength);
        }

        [Parser(Opcode.CMSG_ACCEPT_GUILD_INVITE)]
        public static void HandleAcceptGuildInvite(Packet packet)
        {
            ForeverGuid.Read(packet, "GuildGuid");
        }

        [Parser(Opcode.SMSG_CORPSE_LOCATION)]
        public static void HandleCorpseLocation(Packet packet)
        {
            packet.ReadBit("Valid");
            packet.ResetBitReader();
            ForeverGuid.Read(packet, "Player");
            packet.ReadInt32<MapId>("ActualMapID");
            packet.ReadInt32<MapId>("MapID");
            ForeverGuid.Read(packet, "Transport");
            packet.ReadVector3("Position");
        }

        [Parser(Opcode.SMSG_INVENTORY_CHANGE_FAILURE)]
        public static void HandleInventoryChangeFailure(Packet packet)
        {
            var result = packet.ReadInt32("BagResult");
            ForeverGuid.Read(packet, "Item", 0);
            ForeverGuid.Read(packet, "Item", 1);
            packet.ReadByte("ContainerBSlot");

            switch (result)
            {
                case 1:  // CANT_EQUIP_LEVEL_I
                case 88: // PURCHASE_LEVEL_TOO_LOW
                    packet.ReadInt32("Level");
                    break;
                case 82: // EVENT_AUTOEQUIP_BIND_CONFIRM
                    ForeverGuid.Read(packet, "SrcContainer");
                    packet.ReadInt32("SrcSlot");
                    ForeverGuid.Read(packet, "DstContainer");
                    break;
                case 85: // ITEM_MAX_LIMIT_CATEGORY_COUNT_EXCEEDED_IS
                case 86: // ITEM_MAX_LIMIT_CATEGORY_SOCKETED_EXCEEDED_IS
                case 90: // ITEM_MAX_LIMIT_CATEGORY_EQUIPPED_EXCEEDED_IS
                    packet.ReadInt32("LimitCategory");
                    break;
                default:
                    break;
            }
        }

        [Parser(Opcode.SMSG_MAIL_LIST_RESULT)]
        public static void HandleMailListResult(Packet packet)
        {
            var count = packet.ReadUInt32("MailsCount");
            packet.ReadInt32("TotalNumRecords");

            for (var i = 0; i < count; ++i)
            {
                packet.ReadUInt64("MailID", i);
                var senderType = packet.ReadUInt32("SenderType", i);
                packet.ReadUInt64("Cod", i);
                packet.ReadInt32("StationeryID", i);
                packet.ReadUInt64("SentMoney", i);
                packet.ReadInt32("Flags", i);
                packet.ReadSingle("DaysLeft", i);
                packet.ReadInt32("MailTemplateID", i);
                var attachmentCount = packet.ReadUInt32("AttachmentsCount", i);

                if (senderType == 0)
                    ForeverGuid.Read(packet, "SenderCharacter", i);
                else if (senderType >= 2 && senderType <= 9)
                    packet.ReadInt32("AltSenderID", i);

                for (var j = 0; j < attachmentCount; ++j)
                {
                    packet.ReadByte("Position", i, j);
                    packet.ReadUInt64("AttachID", i, j);
                    ForeverDeveloperLayouts4Handler.ReadItemInstance(packet, i, j);
                    packet.ReadInt32("Count", i, j);
                    packet.ReadInt32("Charges", i, j);
                    packet.ReadUInt32("MaxDurability", i, j);
                    packet.ReadInt32("Durability", i, j);
                    var enchantCount = packet.ReadBits("EnchantsCount", 4, i, j);
                    var gemCount = packet.ReadBits("GemsCount", 2, i, j);
                    packet.ReadBit("Unlocked", i, j);
                    packet.ResetBitReader();

                    for (var k = 0; k < enchantCount; ++k)
                    {
                        packet.ReadInt32("ID", i, j, "Enchant", k);
                        packet.ReadUInt32("Expiration", i, j, "Enchant", k);
                        packet.ReadInt32("Charges", i, j, "Enchant", k);
                        packet.ReadByte("Slot", i, j, "Enchant", k);
                    }

                    for (var k = 0; k < gemCount; ++k)
                    {
                        packet.ReadByte("Slot", i, j, "Gem", k);
                        ForeverDeveloperLayouts4Handler.ReadItemInstance(packet, i, j, "Gem", k);
                    }
                }

                var subjectLength = packet.ReadBits("SubjectLength", 8, i);
                var bodyLength = packet.ReadBits("BodyLength", 13, i);
                packet.ResetBitReader();
                packet.ReadWoWString("Subject", subjectLength, i);
                packet.ReadWoWString("Body", bodyLength, i);
            }
        }

        [Parser(Opcode.SMSG_PARTY_INVITE)]
        public static void HandlePartyInvite(Packet packet)
        {
            packet.ReadBit("CanAccept");
            packet.ReadBit("IsXRealm");
            packet.ReadBit("IsXNativeRealm");
            packet.ReadBit("ShouldSquelch");
            packet.ReadBit("AllowMultipleRoles");
            packet.ReadBit("QuestSessionActive");
            var nameLength = packet.ReadBits("InviterNameLength", 6);
            var surnameLength = packet.ReadBits("InviterSurnameLength", 9);
            packet.ReadBit("IsCrossFaction");
            packet.ResetBitReader();

            ForeverGuid.Read(packet, "InviterGUID");
            ForeverGuid.Read(packet, "InviterBNetAccountId");
            packet.ReadUInt16("InviterCfgRealmID");

            // VirtualRealmInfo
            packet.ReadUInt32("RealmAddress");
            packet.ReadBit("IsLocal");
            packet.ReadBit("IsInternalRealm");
            var actualLength = packet.ReadBits("RealmNameActualLength", 8);
            var normalizedLength = packet.ReadBits("RealmNameNormalizedLength", 8);
            packet.ResetBitReader();
            packet.ReadWoWString("RealmNameActual", actualLength);
            packet.ReadWoWString("RealmNameNormalized", normalizedLength);

            packet.ReadByte("ProposedRoles");
            var lfgSlots = packet.ReadUInt32("LfgSlotsCount");
            packet.ReadUInt32("LfgCompletedMask");
            packet.ReadWoWString("InviterName", nameLength);
            packet.ReadWoWString("InviterSurname", surnameLength);
            for (var i = 0; i < lfgSlots; ++i)
                packet.ReadUInt32("LfgSlot", i);
        }
    }
}
