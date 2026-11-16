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

        /// <summary>
        /// Forever 70938, deweloper: u32, u8 Valid (7 = lista poprawna), u32 Count + encje, potem 1 nieznany bajt.
        /// Encje nie wystapily jeszcze w zadnym snifie (Count zawsze 0), wiec czytamy je jak w SMSG_RECENT_ALLY_LIST.
        /// Ostatni bajt czytamy jako Unknown (a nie zostawiamy), zeby nie zglaszac "not fully read".
        /// </summary>
        [Parser(Opcode.SMSG_RECENT_ALLY_DATA_RESPONSE)]
        public static void HandleRecentAllyDataResponse(Packet packet)
        {
            packet.ReadUInt32("Unk1");
            packet.ReadByte("Valid");
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
            if (packet.CanRead())
                packet.ReadByte("Unk2");
        }

        /// <summary>
        /// Forever 70938, deweloper (potwierdzone na 76 pakietach): u64 Type, u64 ObjectId, u32 Token,
        /// u32 DataSize, potem DataSize bajtow protobufa. Protobufa nie parsujemy - leci jako hex.
        /// Uwaga: 0x440126 to numer sprzed 70755; nasza tabela miala tam CMSG_CLUB_PRESENCE_SUBSCRIBE.
        /// </summary>
        [Parser(Opcode.CMSG_BATTLENET_REQUEST)]
        public static void HandleBattlenetRequest(Packet packet)
        {
            packet.ReadUInt64("Type");
            packet.ReadUInt64("ObjectId");
            packet.ReadUInt32("Token");
            var size = packet.ReadUInt32("DataSize");
            packet.ReadBytes("Data", (int)size);
        }

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
            for (var i = 0u; i < count; i++)
            {
                packet.ReadByte("Unknown0", i);
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
            if (packet.CanRead())
                ForeverGuid.Read(packet, "Guid2");
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

        /// <summary>Jak kazdy MOVE_*_ACK: MovementInfo + i32 AckIndex (A- wg dewelopera).</summary>
        [Parser(Opcode.CMSG_MOVE_ENABLE_FULL_SPEED_PITCHING_ACK)]
        public static void HandleMoveEnableFullSpeedPitchingAck(Packet packet)
        {
            WowPacketParserModule.V6_0_2_19033.Parsers.MovementHandler.ReadMovementAck(packet, "MovementAck");
        }

        [Parser(Opcode.CMSG_MOVE_SET_TURN_RATE_CHEAT)]
        public static void HandleMoveSetTurnRateCheat(Packet packet) { packet.ReadSingle("TurnRate"); }

        [Parser(Opcode.CMSG_UPGRADE_RUNEFORGE_LEGENDARY)]
        public static void HandleUpgradeRuneforgeLegendary(Packet packet) { packet.ReadByte("Unknown"); }
    }
}
