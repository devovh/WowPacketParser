using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_ENUM_CHARACTERS_RESULT (0x470018) dla Forever 1.60.1.
    /// Uklad z tc-forever (CharacterPackets.cpp:208-470):
    ///  - VisualItem: [i32 ItemID][i32 Transmog][u8 Subclass][u8 InvType][u32 DisplayID]
    ///    [u32 DisplayEnchantID][i32 SecondaryIMA][u8 SheatheCategory] (19 sztuk)
    ///  - po imieniu jest nazwisko (6 bitow dlugosci), przed bitami FirstLogin/RealmInfoFound/IsRealmOffline
    ///  - po RealmQueue idzie jeszcze i32 SuperDistrictID
    ///  - ClassUnlock ma 3 bity (HasExpansion, HasUnlockedAchievement, HasEntitlement)
    ///  - RaceUnlock: [i8 RaceID][u32 classCount][ClassUnlock...][7 bitow]
    ///  - RaceLimitDisable: [i8 RaceID][i8 Reason]
    /// </summary>
    public static class ForeverEnumCharactersHandler
    {
        private static void ReadVisualItem(Packet packet, params object[] idx)
        {
            packet.ReadInt32("ItemID", idx);
            packet.ReadInt32("TransmogrifiedItemID", idx);
            packet.ReadByte("Subclass", idx);
            packet.ReadByteE<InventoryType>("InvType", idx);
            packet.ReadUInt32("DisplayID", idx);
            packet.ReadUInt32("DisplayEnchantID", idx);
            packet.ReadInt32("SecondaryItemModifiedAppearanceID", idx);
            packet.ReadByte("SheatheCategory", idx);
        }

        private static void ReadBasic(Packet packet, params object[] idx)
        {
            packet.ReadPackedGuid128("Guid", idx);
            packet.ReadUInt32("VirtualRealmAddress", idx);
            packet.ReadUInt16("ListPosition", idx);
            packet.ReadByteE<Race>("RaceID", idx);
            packet.ReadByteE<Gender>("SexID", idx);
            packet.ReadByteE<Class>("ClassID", idx);
            packet.ReadInt16("SpecID", idx);
            var customizationCount = packet.ReadUInt32("CustomizationCount", idx);
            packet.ReadByte("ExperienceLevel", idx);
            packet.ReadInt32<MapId>("MapID", idx);
            packet.ReadInt32<ZoneId>("ZoneID", idx);
            packet.ReadVector3("PreloadPos", idx);
            packet.ReadUInt64("GuildClubMemberID", idx);
            packet.ReadPackedGuid128("GuildGUID", idx);
            packet.ReadUInt32("Flags", idx);
            packet.ReadUInt32("Flags2", idx);
            packet.ReadUInt32("Flags3", idx);
            packet.ReadUInt32("Flags4", idx);
            packet.ReadByte("CantLoginReason", idx);
            packet.ReadUInt32("PetCreatureDisplayID", idx);
            packet.ReadUInt32("PetExperienceLevel", idx);
            packet.ReadUInt32("PetCreatureFamilyID", idx);

            for (uint j = 0; j < 19; ++j)
                ReadVisualItem(packet, idx, "VisualItems", j);

            packet.ReadInt32("SaveVersion", idx);
            packet.ReadTime64("CreateTime", idx);
            packet.ReadTime64("LastActiveTime", idx);
            packet.ReadInt32("LastLoginVersion", idx);
            V9_0_1_36216.Parsers.CharacterHandler.ReadCustomTabardInfo(packet, idx, "PersonalTabard");

            for (uint j = 0; j < 2; ++j)
                packet.ReadInt32("ProfessionIDs", idx, j);

            packet.ReadInt32("TimerunningSeasonID", idx);
            packet.ReadUInt32("OverrideSelectScreenFileDataID", idx);
            packet.ReadUInt32("RealmQueue", idx);
            packet.ReadInt32("SuperDistrictID", idx);

            for (var j = 0u; j < customizationCount; ++j)
                V9_0_1_36216.Parsers.CharacterHandler.ReadChrCustomizationChoice(packet, idx, "Customizations", j);

            packet.ResetBitReader();
            var nameLength = packet.ReadBits("NameLength", 6, idx);
            var surnameLength = packet.ReadBits("SurnameLength", 6, idx);
            packet.ReadBit("FirstLogin", idx);
            packet.ReadBit("RealmInfoFound", idx);
            packet.ReadBit("IsRealmOffline", idx);

            packet.ReadWoWString("Name", nameLength, idx);
            packet.ReadWoWString("Surname", surnameLength, idx);
        }

        private static void ReadRestrictionAndMail(Packet packet, params object[] idx)
        {
            packet.ResetBitReader();
            packet.ReadBit("BoostInProgress", idx);
            packet.ReadBit("RpeAvailable", idx);

            packet.ReadUInt32("RestrictionFlags", idx);
            var senders = new uint[packet.ReadUInt32("MailSenderCount", idx)];
            var types = packet.ReadUInt32("MailSenderTypeCount", idx);
            packet.ReadUInt32("NoRpeReason", idx);

            for (var j = 0; j < types; ++j)
                packet.ReadByte("MailSenderType", idx, j);

            packet.ResetBitReader();
            for (var j = 0; j < senders.Length; ++j)
                senders[j] = packet.ReadBits(6);

            for (var j = 0; j < senders.Length; ++j)
                packet.ReadDynamicString("MailSender", senders[j], idx);
        }

        [Parser(Opcode.SMSG_ENUM_CHARACTERS_RESULT)]
        public static void HandleEnumCharactersResult(Packet packet)
        {
            packet.ReadBit("Success");
            packet.ReadBit("Realmless");
            packet.ReadBit("IsDeletedCharacters");
            packet.ReadBit("IgnoreNewPlayerRestrictions");
            packet.ReadBit("IsRestrictedNewPlayer");
            packet.ReadBit("IsNewcomerChatCompleted");
            packet.ReadBit("IsRestrictedTrial");
            packet.ReadBit("IsAccountLapsedPlayer");
            var hasClassDisableMask = packet.ReadBit("HasClassDisableMask");
            packet.ReadBit("ForceCharacterListSort");

            var chars = packet.ReadUInt32("CharactersCount");
            var regionwide = packet.ReadUInt32("RegionwideCharactersCount");
            packet.ReadInt32("MaxCharacterLevel");
            var raceUnlocks = packet.ReadUInt32("RaceUnlockCount");
            var conditional = packet.ReadUInt32("UnlockedConditionalAppearanceCount");
            var raceLimits = packet.ReadUInt32("RaceLimitDisablesCount");
            var warbands = packet.ReadUInt32("WarbandGroupsCount");

            if (hasClassDisableMask)
                packet.ReadUInt32("ClassDisableMask");

            for (var i = 0u; i < chars; ++i)
            {
                ReadBasic(packet, i, "Characters", "Basic");
                ReadRestrictionAndMail(packet, i, "Characters", "RestrictionsAndMails");
            }

            for (var i = 0u; i < regionwide; ++i)
            {
                ReadBasic(packet, i, "RegionwideCharacters", "Basic");
                packet.ReadUInt64("Money", i);
                packet.ReadSingle("AvgEquippedItemLevel", i);
                packet.ReadSingle("CurrentSeasonMythicPlusOverallScore", i);
                packet.ReadInt32("CurrentSeasonBestPvpRating", i);
                packet.ReadSByte("PvpRatingBracket", i);
                packet.ReadInt16("PvpRatingAssociatedSpecID", i);
            }

            for (var i = 0u; i < raceUnlocks; ++i)
            {
                packet.ReadSByteE<Race>("RaceID", i, "RaceUnlockData");
                var classes = packet.ReadUInt32("ClassUnlockCount", i, "RaceUnlockData");
                for (var j = 0u; j < classes; ++j)
                {
                    packet.ReadSByteE<Class>("ClassID", i, "RaceUnlockData", "ClassUnlocks", j);
                    packet.ReadUInt32("AchievementID", i, "RaceUnlockData", "ClassUnlocks", j);
                    packet.ResetBitReader();
                    packet.ReadBit("HasExpansion", i, "RaceUnlockData", "ClassUnlocks", j);
                    packet.ReadBit("HasUnlockedAchievement", i, "RaceUnlockData", "ClassUnlocks", j);
                    packet.ReadBit("HasEntitlement", i, "RaceUnlockData", "ClassUnlocks", j);
                }

                packet.ResetBitReader();
                packet.ReadBit("HasUnlockedLicense", i, "RaceUnlockData");
                packet.ReadBit("HasUnlockedAchievement", i, "RaceUnlockData");
                packet.ReadBit("HasHeritageArmorUnlockAchievement", i, "RaceUnlockData");
                packet.ReadBit("HasEntitlement", i, "RaceUnlockData");
                packet.ReadBit("HideRaceOnClient", i, "RaceUnlockData");
                packet.ReadBit("FactionBalanceDisabled", i, "RaceUnlockData");
                packet.ReadBit("DoesNotHaveAvailableClasses", i, "RaceUnlockData");
            }

            for (var i = 0u; i < conditional; ++i)
            {
                packet.ReadInt32("AchievementID", i, "UnlockedConditionalAppearances");
                packet.ReadInt32("ConditionalType", i, "UnlockedConditionalAppearances");
            }

            for (var i = 0u; i < raceLimits; ++i)
            {
                packet.ReadSByteE<Race>("RaceID", i, "RaceLimitDisables");
                packet.ReadSByte("Reason", i, "RaceLimitDisables");
            }

            for (var i = 0u; i < warbands; ++i)
            {
                packet.ReadUInt64("GroupID", i, "WarbandGroups");
                packet.ReadByte("OrderIndex", i, "WarbandGroups");
                packet.ReadUInt32("WarbandSceneID", i, "WarbandGroups");
                packet.ReadUInt32("Flags", i, "WarbandGroups");
                packet.ReadInt32("ContentSetID", i, "WarbandGroups");
                var members = packet.ReadUInt32("MemberCount", i, "WarbandGroups");
                for (var j = 0u; j < members; ++j)
                {
                    packet.ReadUInt32("WarbandScenePlacementID", i, "WarbandGroups", "Members", j);
                    var type = packet.ReadInt32("Type", i, "WarbandGroups", "Members", j);
                    packet.ReadInt32("ContentSetID", i, "WarbandGroups", "Members", j);
                    if (type == 0)
                        packet.ReadPackedGuid128("Guid", i, "WarbandGroups", "Members", j);
                }

                packet.ResetBitReader();
                var nameLen = packet.ReadBits("NameLength", 9, i, "WarbandGroups");
                packet.ReadWoWString("Name", nameLen, i, "WarbandGroups");
            }
        }
    }
}
