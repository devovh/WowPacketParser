using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Uklady od dewelopera, odpowiedz nr 2, sekcja 3 (ALFA-OPCODES-70938-REPLY-2.md).
    /// Pewnosc A: SMSG_RECENT_ALLY_LIST, CMSG_RECENT_ALLY_REQUEST_DATA. Reszta to jego domysly (G) z dlugosci probek.
    /// </summary>
    public static class ForeverDeveloperLayouts3Handler
    {
        [Parser(Opcode.SMSG_RECENT_ALLY_LIST)]
        public static void HandleRecentAllyList(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            for (var i = 0u; i < count; i++)
            {
                ForeverGuid.Read(packet, "Ally", i);
                packet.ReadUInt32("Unk1", i);
                packet.ReadUInt32("Unk2", i);
                packet.ReadUInt32("Unk3", i);
                packet.ReadUInt32("ZoneID", i);
                var inter = packet.ReadUInt32("InteractionCount", i);
                for (var j = 0u; j < inter; j++)
                {
                    packet.ReadByte("Type", i, j);
                    packet.ReadTime64("UnixTime", i, j);
                    packet.ReadInt32("Value", i, j);
                    packet.ReadInt32("Unk", i, j);
                }
                packet.ReadUInt32("OnlineFlags", i);
            }
        }

        [Parser(Opcode.CMSG_RECENT_ALLY_REQUEST_DATA)]
        public static void HandleRecentAllyRequestData(Packet packet) { packet.ReadUInt32("Unknown"); }

        [Parser(Opcode.SMSG_MOVE_INITIAL_OBJECT_UPDATE_COMPLETE)]
        public static void HandleMoveInitialObjectUpdateComplete(Packet packet)
        {
            ForeverGuid.Read(packet, "MoverGUID");
            packet.ReadUInt32("Unknown");
        }

        [Parser(Opcode.CMSG_REQUEST_INSTANCE_ENCOUNTER_EVENT_SYNC)]
        public static void HandleRequestInstanceEncounterEventSync(Packet packet) { ForeverGuid.Read(packet, "Guid"); }

        [Parser(Opcode.SMSG_DELVES_ACCOUNT_DATA_ELEMENT_CHANGED)]
        public static void HandleDelvesAccountDataElementChanged(Packet packet)
        {
            packet.ReadInt32("Element");
            packet.ReadByte("Unknown");
        }

        [Parser(Opcode.SMSG_REGIONWIDE_CHARACTER_RESTRICTIONS_DATA)]
        [Parser(Opcode.SMSG_REGIONWIDE_CHARACTER_MAIL_DATA)]
        public static void HandleRegionwideCharacterData(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            packet.ReadByte("Unknown");
            for (var i = 0u; i < count; i++)
            {
                ForeverGuid.Read(packet, "Character", i);
                packet.ReadUInt32("Unk1", i);
                packet.ReadUInt32("Unk2", i);
            }
        }

        [Parser(Opcode.SMSG_LAST_CATALOG_FETCH_RESPONSE)]
        public static void HandleLastCatalogFetchResponse(Packet packet) { packet.ReadTime64("FetchTime"); }

        [Parser(Opcode.SMSG_BATCH_PRESENCE_SUBSCRIPTION)]
        public static void HandleBatchPresenceSubscription(Packet packet)
        {
            packet.ReadBit("Unknown");
            packet.ResetBitReader();
            var count = packet.ReadUInt32("Count");
            for (var i = 0u; i < count; i++)
                packet.ReadUInt64("Id", i);
        }

        [Parser(Opcode.CMSG_SERVER_VALIDATION_SIGNATURE_REQUEST)]
        public static void HandleServerValidationSignatureRequest(Packet packet)
        {
            packet.ReadUInt32("Unknown");
            ForeverGuid.Read(packet, "Guid");
        }

        [Parser(Opcode.CMSG_GET_ACCOUNT_CHARACTER_LIST)]
        public static void HandleGetAccountCharacterList(Packet packet)
        {
            packet.ReadUInt32("Token");
            packet.ReadToEnd();
        }

        [Parser(Opcode.CMSG_CLUB_FINDER_REQUEST_SUBSCRIBED_CLUB_POSTING_IDS)]
        public static void HandleClubFinderRequestSubscribedPostingIds(Packet packet) { packet.ReadUInt32("Unknown"); }

        [Parser(Opcode.SMSG_RAF_ACTIVITY_STATE_CHANGED)]
        public static void HandleRafActivityStateChanged(Packet packet) { packet.ReadUInt32("Unknown"); }

        [Parser(Opcode.SMSG_PERKS_ANIM_TOGGLE_KILL_SWITCH)]
        public static void HandlePerksAnimToggleKillSwitch(Packet packet)
        {
            packet.ReadBit("Bit1");
            packet.ReadBit("Bit2");
            packet.ResetBitReader();
        }

        [Parser(Opcode.CMSG_PERKS_PROGRAM_REQUEST_REFUND)]
        public static void HandleRoleMaskAsRefund(Packet packet) { packet.ReadByte("RoleMask"); }

        [Parser(Opcode.CMSG_MOVE_SET_TURN_RATE_CHEAT)]
        public static void HandleMoveSetTurnRateCheat(Packet packet) { packet.ReadSingle("TurnRate"); }

        [Parser(Opcode.CMSG_UPGRADE_RUNEFORGE_LEGENDARY)]
        public static void HandleUpgradeRuneforgeLegendary(Packet packet) { packet.ReadByte("Unknown"); }
    }
}
