using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_PARTY_UPDATE (0x470097) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (Server/Packets/PartyPackets.cpp:584 PartyUpdate::Write),
    /// z wlaczona galazia Classic70291 (1.60.1 jest nowszy niz build 70291):
    ///
    ///   [u16 PartyFlags][u8 PartyIndex][u8 PartyType][i32 MyIndex]
    ///   [Guid PartyGUID][u32 SequenceNum]
    ///   [i8 Classic70291_BeforeLeader]        <- brak w 12.x
    ///   [Guid LeaderGUID][u8 LeaderFactionGroup][i32 PingRestriction]
    ///   [u64 Classic70291_BeforeMemberCount]  <- brak w 12.x
    ///   [u32 PlayerListCount]
    ///   per gracz: WritePlayerInfo (VoiceStateID czytane jako 9 bitow, nie 6)
    ///   [4 bity: ChallengeMode / LfgInfos / LootSettings / DifficultySettings]
    ///
    /// Weryfikacja na pakiecie 43 B (Number 91120):
    ///   parser 12.x czytal 34 B i zostawial 9 — dokladnie i8 + u64.
    ///
    /// UWAGA: czesc z graczami (150 B) nadal nie domyka sie w calosci — patrz FOREVER-STAN.md.
    /// </summary>
    public static class ForeverPartyHandler
    {
        [Parser(Opcode.SMSG_PARTY_UPDATE)]
        public static void HandlePartyUpdate(Packet packet)
        {
            packet.ReadUInt16("PartyFlags");
            packet.ReadByte("PartyIndex");
            packet.ReadByte("PartyType");

            packet.ReadInt32("MyIndex");
            packet.ReadPackedGuid128("PartyGUID");
            packet.ReadInt32("SequenceNum");
            packet.ReadSByte("Classic70291BeforeLeader");
            packet.ReadPackedGuid128("LeaderGUID");
            packet.ReadByte("LeaderFactionGroup");
            packet.ReadInt32("PingRestriction");
            packet.ReadUInt64("Classic70291BeforeMemberCount");
            var playerCount = packet.ReadUInt32("PlayerListCount");

            for (var i = 0; i < playerCount; i++)
            {
                packet.ResetBitReader();
                var playerNameLength = packet.ReadBits("NameLength", 6, i);
                var voiceStateLength = packet.ReadBits("VoiceStateIDLength", 9, i);
                packet.ReadBit("Connected", i);
                packet.ReadBit("FromSocialQueue", i);
                packet.ReadBit("VoiceChatSilenced", i);
                packet.ResetBitReader();

                packet.ReadPackedGuid128("Guid", i);
                packet.ReadByte("Subgroup", i);
                packet.ReadByte("Flags", i);
                packet.ReadByte("RolesAssigned", i);
                packet.ReadByte("RolesDesired", i);
                packet.ReadByteE<Class>("Class", i);
                packet.ReadByte("FactionGroup", i);

                packet.ReadPackedGuid128("LeaverGUID", i, "LeaverInfo");
                packet.ReadSingle("LeaveScore", i, "LeaverInfo");
                packet.ReadUInt32("SeasonID", i, "LeaverInfo");
                packet.ReadUInt32("TotalLeaves", i, "LeaverInfo");
                packet.ReadUInt32("TotalSuccesses", i, "LeaverInfo");
                packet.ReadInt32("ConsecutiveSuccesses", i, "LeaverInfo");
                packet.ReadTime64("LastPenaltyTime", i, "LeaverInfo");
                packet.ReadTime64("LeaverExpirationTime", i, "LeaverInfo");
                packet.ReadInt32("Flags", i, "LeaverInfo");
                packet.ResetBitReader();
                packet.ReadBit("LeaverStatus", i, "LeaverInfo");
                packet.ResetBitReader();

                packet.ReadWoWString("Name", playerNameLength, i);
                packet.ReadDynamicString("VoiceStateID", voiceStateLength, i);
            }

            packet.ResetBitReader();
            var hasChallengeMode = packet.ReadBit("HasChallengeMode");
            var hasLfgInfo = packet.ReadBit("HasLfgInfo");
            var hasLootSettings = packet.ReadBit("HasLootSettings");
            var hasDifficultySettings = packet.ReadBit("HasDifficultySettings");
            packet.ResetBitReader();

            if (hasChallengeMode)
            {
                packet.ReadInt32("MapID", "ChallengeMode");
                packet.ReadInt32("InitialPlayerCount", "ChallengeMode");
                packet.ReadUInt64("InstanceID", "ChallengeMode");
                packet.ReadTime64("StartTime", "ChallengeMode");
                packet.ReadPackedGuid128("KeystoneOwnerGUID", "ChallengeMode");
                packet.ReadPackedGuid128("LeaverGUID", "ChallengeMode");
                packet.ReadUInt32("InstanceAbandonVoteCooldown", "ChallengeMode");
                packet.ReadBit("IsActive", "ChallengeMode");
                packet.ReadBit("HasRestrictions", "ChallengeMode");
                packet.ReadBit("CanVoteAbandon", "ChallengeMode");
                packet.ResetBitReader();
            }

            if (hasLfgInfo)
            {
                packet.ReadUInt32("Slot", "LfgInfo");
                packet.ReadByte("MyFlags", "LfgInfo");
                packet.ReadUInt32("MyRandomSlot", "LfgInfo");
                packet.ReadByte("MyPartialClear", "LfgInfo");
                packet.ReadSingle("MyGearDiff", "LfgInfo");
                packet.ReadByte("MyStrangerCount", "LfgInfo");
                packet.ReadByte("MyKickVoteCount", "LfgInfo");
                packet.ReadByte("BootCount", "LfgInfo");
                packet.ReadBit("Aborted", "LfgInfo");
                packet.ReadBit("MyFirstReward", "LfgInfo");
                packet.ResetBitReader();
            }

            if (hasLootSettings)
            {
                packet.ReadByte("Method", "LootSettings");
                packet.ReadPackedGuid128("LootMaster", "LootSettings");
                packet.ReadByte("Threshold", "LootSettings");
            }

            if (hasDifficultySettings)
            {
                packet.ReadInt16("DungeonDifficultyID", "DifficultySettings");
                packet.ReadInt16("RaidDifficultyID", "DifficultySettings");
                packet.ReadInt16("LegacyRaidDifficultyID", "DifficultySettings");
            }
        }
    }
}
