using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_QUERY_QUEST_INFO_RESPONSE (0x670016) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (Server/Packets/QuestPackets.cpp:80 QueryQuestInfoResponse::Write):
    ///
    ///   [u32 QuestID][bit Allow][FlushBits]
    ///   jesli Allow:
    ///     [i32 Info.QuestID][i32 ClassicLevelType][i32 ClassicQuestLevel][i32 QuestPackageID]
    ///     [i32 ClassicMinLevel][i32 ContentTuningID][i32 QuestSortID][i32 QuestInfoID]
    ///     [i32 SuggestedGroupNum][i32 RewardNextQuest][i32 RewardXPDifficulty][f32 RewardXPMultiplier]
    ///     [i32 RewardMoney][i32 RewardMoneyDifficulty][f32 RewardMoneyMultiplier][i32 RewardBonusMoney]
    ///     [u32 RewardDisplaySpellCount][i32 RewardSpell][i32 RewardHonor][f32 RewardKillHonor]
    ///     [i32 RewardFavor][i32 RewardArtifactXPDifficulty][f32 RewardArtifactXPMultiplier]
    ///     [i32 RewardArtifactCategoryID][i32 StartItem][u32 Flags][u32 FlagsEx][u32 FlagsEx2][u32 FlagsEx3]
    ///     [4 x (i32 RewardItem, i32 RewardAmount, i32 ItemDrop, i32 ItemDropQuantity)]
    ///     [6 x (i32 ChoiceItemID, i32 ChoiceQuantity, i32 ChoiceDisplayID)]
    ///     [i32 POIContinent][f32 POIx][f32 POIy][i32 POIPriority]
    ///     [i32 RewardTitle][i32 RewardArenaPoints][i32 RewardSkillLineID][i32 RewardNumSkillUps]
    ///     [i32 PortraitGiver][i32 PortraitGiverMount][i32 PortraitGiverModelSceneID][i32 PortraitTurnIn]
    ///     [5 x (i32 RewardFactionID, RewardFactionValue, RewardFactionOverride, RewardFactionCapIn)]
    ///     [i32 RewardFactionFlags][4 x (i32 RewardCurrencyID, i32 RewardCurrencyQty)]
    ///     [i32 AcceptedSoundKitID][i32 CompleteSoundKitID][i32 AreaGroupID][i64 TimeAllowed]
    ///     [u32 ObjectiveCount][2 x i32 RaceMask][u32 TreasurePickerCount][u32 NonDisplayableTreasurePickerCount]
    ///     [i32 Expansion][i32 ManagedWorldStateID][i32 QuestSessionBonus][i32 QuestGiverCreatureID]
    ///     [u32 ConditionalQuestDescriptionCount][u32 ConditionalQuestCompletionLogCount]
    ///     [u32 RewardHouseRoomCount][u32 RewardHouseDecorCount]
    ///     [RewardDisplaySpellCount x (i32 SpellID, i32 PlayerConditionID, i32 Type)]
    ///     [ObjectiveCount x (u32 ID, i32 Type, i8 StorageIndex, i32 ObjectID, i32 Amount,
    ///                        i32 ConditionalAmount, u32 Flags, u32 Flags2, f32 ProgressBarWeight,
    ///                        u32 VisualEffectsCount, i32 ParentObjectiveID,
    ///                        bits 8 Description, bit Visible, teksty, u32 VisualEffects)]
    ///     [teksty warunkowe], [TreasurePickerID], [NonDisplayableTreasurePickerIDs],
    ///     [RewardHouseRoomIDs], [RewardHouseDecorIDs]
    ///     [bits 9 LogTitle][bits 12 LogDescription][bits 12 QuestDescription][bits 9 AreaDescription]
    ///     [bits 10 PortraitGiverText][bits 8 PortraitGiverName][bits 10 PortraitTurnInText]
    ///     [bits 8 PortraitTurnInName][bits 11 QuestCompletionLog][bit ResetByScheduler][bit ReadyForTranslation]
    ///     [FlushBits][teksty]
    ///
    /// Handler retailowy (QuestHandler.HandleQuestQueryResponse, wersja Zero..V5_0_5) czyta ten pakiet
    /// innym ukladem, wiec dla Forever wychodzilo 0 wierszy w `quest_template` (11 pakietow w snifie,
    /// z czego 2 z danymi: 26799 i 97246).
    /// </summary>
    public static class ForeverQueryQuestHandler
    {
        [Parser(Opcode.SMSG_QUERY_QUEST_INFO_RESPONSE)]
        public static void HandleQuestQueryResponse(Packet packet)
        {
            var id = packet.ReadUInt32("QuestID");
            var allow = packet.ReadBit("Allow");
            packet.ResetBitReader();

            if (!allow)
                return;

            QuestTemplate quest = new() { ID = id };

            packet.ReadInt32("QuestIDDuplicate");
            quest.QuestType = packet.ReadInt32E<QuestType>("QuestType");
            quest.QuestLevel = packet.ReadInt32("QuestLevel");
            quest.QuestPackageID = (uint)packet.ReadInt32("QuestPackageID");
            quest.MinLevel = packet.ReadInt32("QuestMinLevel");
            quest.ContentTuningID = packet.ReadInt32("ContentTuningID");
            quest.QuestSortID = packet.ReadInt32E<QuestSort>("QuestSortID");
            quest.QuestInfoID = packet.ReadInt32E<QuestInfo>("QuestInfoID");
            quest.SuggestedGroupNum = (uint)packet.ReadInt32("SuggestedGroupNum");
            quest.RewardNextQuest = (uint)packet.ReadInt32("RewardNextQuest");
            quest.RewardXPDifficulty = (uint)packet.ReadInt32("RewardXPDifficulty");
            quest.RewardXPMultiplier = packet.ReadSingle("RewardXPMultiplier");
            quest.RewardMoney = packet.ReadInt32("RewardMoney");
            quest.RewardMoneyDifficulty = (uint)packet.ReadInt32("RewardMoneyDifficulty");
            quest.RewardMoneyMultiplier = packet.ReadSingle("RewardMoneyMultiplier");
            quest.RewardBonusMoney = (uint)packet.ReadInt32("RewardBonusMoney");

            var rewardDisplaySpellCount = packet.ReadUInt32("RewardDisplaySpellCount");

            quest.RewardSpell = packet.ReadInt32<SpellId>("RewardSpell");
            quest.RewardHonor = packet.ReadInt32("RewardHonor");
            quest.RewardKillHonor = packet.ReadSingle("RewardKillHonor");
            quest.RewardFavor = packet.ReadInt32("RewardFavor");
            quest.RewardArtifactXPDifficulty = (uint)packet.ReadInt32("RewardArtifactXPDifficulty");
            quest.RewardArtifactXPMultiplier = packet.ReadSingle("RewardArtifactXPMultiplier");
            quest.RewardArtifactCategoryID = (uint)packet.ReadInt32("RewardArtifactCategoryID");
            quest.StartItem = (uint)packet.ReadInt32<ItemId>("StartItem");
            quest.Flags = packet.ReadInt32E<QuestFlags>("Flags");
            quest.FlagsEx = packet.ReadInt32E<QuestFlagsEx>("FlagsEx");
            quest.FlagsEx2 = packet.ReadInt32E<QuestFlagsEx2>("FlagsEx2");
            quest.FlagsEx3 = packet.ReadInt32E<QuestFlagsEx3>("FlagsEx3");

            quest.RewardItem = new uint?[4];
            quest.RewardAmount = new uint?[4];
            quest.ItemDrop = new uint?[4];
            quest.ItemDropQuantity = new uint?[4];
            for (var i = 0; i < 4; ++i)
            {
                quest.RewardItem[i] = (uint)packet.ReadInt32<ItemId>("RewardItems", i);
                quest.RewardAmount[i] = (uint)packet.ReadInt32("RewardAmount", i);
                quest.ItemDrop[i] = (uint)packet.ReadInt32<ItemId>("ItemDrop", i);
                quest.ItemDropQuantity[i] = (uint)packet.ReadInt32("ItemDropQuantity", i);
            }

            quest.RewardChoiceItemID = new uint?[6];
            quest.RewardChoiceItemQuantity = new uint?[6];
            quest.RewardChoiceItemDisplayID = new uint?[6];
            for (var i = 0; i < 6; ++i)
            {
                quest.RewardChoiceItemID[i] = (uint)packet.ReadInt32<ItemId>("RewardChoiceItemID", i);
                quest.RewardChoiceItemQuantity[i] = (uint)packet.ReadInt32("RewardChoiceItemQuantity", i);
                quest.RewardChoiceItemDisplayID[i] = (uint)packet.ReadInt32("RewardChoiceItemDisplayID", i);
            }

            quest.POIContinent = (uint)packet.ReadInt32("POIContinent");
            quest.POIx = packet.ReadSingle("POIx");
            quest.POIy = packet.ReadSingle("POIy");
            quest.POIPriority = (uint)packet.ReadInt32("POIPriority");
            quest.RewardTitle = (uint)packet.ReadInt32("RewardTitle");
            quest.RewardArenaPoints = (uint)packet.ReadInt32("RewardArenaPoints");
            quest.RewardSkillLineID = (uint)packet.ReadInt32("RewardSkillLineID");
            quest.RewardNumSkillUps = (uint)packet.ReadInt32("RewardNumSkillUps");
            quest.QuestGiverPortrait = (uint)packet.ReadInt32("PortraitGiver");
            quest.PortraitGiverMount = (uint)packet.ReadInt32("PortraitGiverMount");
            quest.PortraitGiverModelSceneID = packet.ReadInt32("PortraitGiverModelSceneID");
            quest.QuestTurnInPortrait = (uint)packet.ReadInt32("PortraitTurnIn");

            quest.RewardFactionID = new uint?[5];
            quest.RewardFactionValue = new int?[5];
            quest.RewardFactionOverride = new int?[5];
            quest.RewardFactionCapIn = new int?[5];
            for (var i = 0; i < 5; ++i)
            {
                quest.RewardFactionID[i] = (uint)packet.ReadInt32("RewardFactionID", i);
                quest.RewardFactionValue[i] = packet.ReadInt32("RewardFactionValue", i);
                quest.RewardFactionOverride[i] = packet.ReadInt32("RewardFactionOverride", i);
                quest.RewardFactionCapIn[i] = packet.ReadInt32("RewardFactionCapIn", i);
            }

            quest.RewardFactionFlags = (uint)packet.ReadInt32("RewardFactionFlags");

            quest.RewardCurrencyID = new uint?[4];
            quest.RewardCurrencyCount = new uint?[4];
            for (var i = 0; i < 4; ++i)
            {
                quest.RewardCurrencyID[i] = (uint)packet.ReadInt32("RewardCurrencyID", i);
                quest.RewardCurrencyCount[i] = (uint)packet.ReadInt32("RewardCurrencyQty", i);
            }

            quest.SoundAccept = (uint)packet.ReadInt32("AcceptedSoundKitID");
            quest.SoundTurnIn = (uint)packet.ReadInt32("CompleteSoundKitID");
            quest.AreaGroupID = (uint)packet.ReadInt32("AreaGroupID");
            quest.TimeAllowed = packet.ReadInt64("TimeAllowed");

            var objectiveCount = packet.ReadUInt32("ObjectiveCount");

            // AllowableRaces is a 64-bit mask written as two int32; the column is 64-bit here.
            var raceMask = (ulong)(uint)packet.ReadInt32("RaceMask", 0);
            raceMask |= (ulong)(uint)packet.ReadInt32("RaceMask", 1) << 32;
            quest.AllowableRacesWod = raceMask;

            var treasurePickerCount = packet.ReadUInt32("TreasurePickerCount");
            var nonDisplayableTreasurePickerCount = packet.ReadUInt32("NonDisplayableTreasurePickerCount");
            quest.Expansion = (int)packet.ReadInt32E<ClientType>("Expansion");
            quest.ManagedWorldStateID = packet.ReadInt32("ManagedWorldStateID");
            quest.QuestSessionBonus = packet.ReadInt32("QuestSessionBonus");
            packet.ReadInt32("QuestGiverCreatureID");

            var conditionalDescriptionCount = packet.ReadUInt32("ConditionalQuestDescriptionCount");
            var conditionalCompletionLogCount = packet.ReadUInt32("ConditionalQuestCompletionLogCount");
            var rewardHouseRoomCount = packet.ReadUInt32("RewardHouseRoomCount");
            var rewardHouseDecorCount = packet.ReadUInt32("RewardHouseDecorCount");

            for (var i = 0; i < rewardDisplaySpellCount; ++i)
            {
                QuestRewardDisplaySpell rewardDisplaySpell = new QuestRewardDisplaySpell
                {
                    QuestID = quest.ID,
                    Idx = (uint)i,
                    SpellID = (uint)packet.ReadInt32<SpellId>("RewardDisplaySpellSpellID", i),
                    PlayerConditionID = (uint)packet.ReadInt32("RewardDisplaySpellPlayerConditionID", i),
                    Type = packet.ReadInt32("RewardDisplaySpellType", i),
                };
                Storage.QuestRewardDisplaySpells.Add(rewardDisplaySpell, packet.TimeSpan);
            }

            quest.ObjectiveText = new string[4];
            for (var i = 0; i < objectiveCount; ++i)
            {
                packet.ReadUInt32("ObjectiveID", i);
                packet.ReadInt32("ObjectiveType", i);
                packet.ReadSByte("ObjectiveStorageIndex", i);
                packet.ReadInt32("ObjectiveObjectID", i);
                packet.ReadInt32("ObjectiveAmount", i);
                packet.ReadInt32("ObjectiveConditionalAmount", i);
                packet.ReadUInt32("ObjectiveFlags", i);
                packet.ReadUInt32("ObjectiveFlags2", i);
                packet.ReadSingle("ObjectiveProgressBarWeight", i);
                var visualEffectCount = packet.ReadUInt32("ObjectiveVisualEffectsCount", i);
                packet.ReadInt32("ObjectiveParentObjectiveID", i);

                for (var j = 0; j < visualEffectCount; ++j)
                    packet.ReadInt32("ObjectiveVisualEffect", i, j);

                packet.ResetBitReader();
                var descriptionLength = packet.ReadBits("ObjectiveDescriptionLength", 8, i);
                packet.ReadBit("ObjectiveVisible", i);
                packet.ResetBitReader();

                var description = packet.ReadWoWString("ObjectiveDescription", descriptionLength, i);
                if (i < quest.ObjectiveText.Length)
                    quest.ObjectiveText[i] = description;
            }

            for (var i = 0; i < treasurePickerCount; ++i)
                packet.ReadUInt32("TreasurePickerID", i);

            for (var i = 0; i < nonDisplayableTreasurePickerCount; ++i)
                packet.ReadUInt32("NonDisplayableTreasurePickerID", i);

            for (var i = 0; i < conditionalDescriptionCount; ++i)
            {
                packet.ReadUInt32("ConditionalQuestDescriptionId", i);
                packet.ReadUInt32("ConditionalQuestDescriptionPlayerConditionId", i);
            }

            for (var i = 0; i < conditionalCompletionLogCount; ++i)
            {
                packet.ReadUInt32("ConditionalQuestCompletionLogId", i);
                packet.ReadUInt32("ConditionalQuestCompletionLogPlayerConditionId", i);
            }

            for (var i = 0; i < rewardHouseRoomCount; ++i)
                packet.ReadUInt32("RewardHouseRoomID", i);

            for (var i = 0; i < rewardHouseDecorCount; ++i)
                packet.ReadUInt32("RewardHouseDecorID", i);

            packet.ResetBitReader();
            var logTitleLength = packet.ReadBits("LogTitleLength", 9);
            var logDescriptionLength = packet.ReadBits("LogDescriptionLength", 12);
            var questDescriptionLength = packet.ReadBits("QuestDescriptionLength", 12);
            var areaDescriptionLength = packet.ReadBits("AreaDescriptionLength", 9);
            var portraitGiverTextLength = packet.ReadBits("PortraitGiverTextLength", 10);
            var portraitGiverNameLength = packet.ReadBits("PortraitGiverNameLength", 8);
            var portraitTurnInTextLength = packet.ReadBits("PortraitTurnInTextLength", 10);
            var portraitTurnInNameLength = packet.ReadBits("PortraitTurnInNameLength", 8);
            var questCompletionLogLength = packet.ReadBits("QuestCompletionLogLength", 11);
            quest.ResetByScheduler = packet.ReadBit("ResetByScheduler");
            packet.ReadBit("ReadyForTranslation");
            packet.ResetBitReader();

            quest.LogTitle = packet.ReadWoWString("LogTitle", logTitleLength);
            quest.LogDescription = packet.ReadWoWString("LogDescription", logDescriptionLength);
            quest.QuestDescription = packet.ReadWoWString("QuestDescription", questDescriptionLength);
            quest.AreaDescription = packet.ReadWoWString("AreaDescription", areaDescriptionLength);
            quest.QuestGiverTextWindow = packet.ReadWoWString("PortraitGiverText", portraitGiverTextLength);
            quest.QuestGiverTargetName = packet.ReadWoWString("PortraitGiverName", portraitGiverNameLength);
            quest.QuestTurnTextWindow = packet.ReadWoWString("PortraitTurnInText", portraitTurnInTextLength);
            quest.QuestTurnTargetName = packet.ReadWoWString("PortraitTurnInName", portraitTurnInNameLength);
            quest.QuestCompletionLog = packet.ReadWoWString("QuestCompletionLog", questCompletionLogLength);

            packet.AddSniffData(StoreNameType.Quest, (int)id, "QUERY_RESPONSE");
            Storage.QuestTemplates.Add(quest, packet.TimeSpan);

            // The retail handler also reads this opcode; keep only this one row per quest.
            Storage.ObjectNames.Add(new ObjectName
            {
                ObjectType = StoreNameType.Quest,
                ID = (int)id,
                Name = quest.LogTitle
            }, packet.TimeSpan);
        }
    }
}
