using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_FEATURE_SYSTEM_STATUS (0x470063), tc-forever SystemPackets.cpp:123 FeatureSystemStatus::Write.
    /// Classic 1.60.1: bez KioskSessionDurationMinutes, QuickJoinConfig zaraz po CommercePricePollTimeSeconds,
    /// SocialRestriction przed ContentSetID, 10 bitow ClassicFlagBits10 tam gdzie retail ma dlugosc napisu Unknown1027.
    /// </summary>
    public static class ForeverFeatureStatusHandler
    {
        [Parser(Opcode.SMSG_FEATURE_SYSTEM_STATUS)]
        public static void HandleFeatureSystemStatus(Packet packet)
        {
            packet.ReadByte("ComplaintStatus");
            packet.ReadUInt32("CfgRealmID");
            packet.ReadInt32("CfgRealmRecID");

            packet.ReadUInt32("MaxRecruits", "RAFSystem");
            packet.ReadUInt32("MaxRecruitMonths", "RAFSystem");
            packet.ReadUInt32("MaxRecruitmentUses", "RAFSystem");
            packet.ReadUInt32("DaysInCycle", "RAFSystem");
            packet.ReadUInt32("RewardsVersion", "RAFSystem");

            packet.ReadUInt32("CommercePricePollTimeSeconds");

            foreach (var name in new[]
            {
                "ToastDuration", "DelayDuration", "QueueMultiplier", "PlayerMultiplier", "PlayerFriendValue",
                "PlayerGuildValue", "ThrottleInitialThreshold", "ThrottleDecayTime", "ThrottlePrioritySpike",
                "ThrottleMinThreshold", "ThrottlePvPPriorityNormal", "ThrottlePvPPriorityLow", "ThrottlePvPHonorThreshold",
                "ThrottleLfgListPriorityDefault", "ThrottleLfgListPriorityAbove", "ThrottleLfgListPriorityBelow",
                "ThrottleLfgListIlvlScalingAbove", "ThrottleLfgListIlvlScalingBelow", "ThrottleRfPriorityAbove",
                "ThrottleRfIlvlScalingAbove", "ThrottleDfMaxItemLevel", "ThrottleDfBestPriority"
            })
                packet.ReadSingle(name, "QuickJoinConfig");
            packet.ResetBitReader();
            packet.ReadBit("ToastsDisabled", "QuickJoinConfig");
            packet.ResetBitReader();

            packet.ReadInt64("RedeemForBalanceAmount");
            packet.ReadUInt32("ClubsPresenceDelay");
            packet.ReadUInt32("ClubPresenceUnsubscribeDelay");

            packet.ResetBitReader();
            packet.ReadBit("IsSquelched", "Squelch");
            packet.ResetBitReader();
            ForeverGuid.Read(packet, "BnetAccountGUID", "Squelch");
            ForeverGuid.Read(packet, "GuildGUID", "Squelch");

            packet.ReadInt32("SocialRestriction");
            packet.ReadInt32("ContentSetID");
            var disabled = packet.ReadUInt32("DisabledGameModesCount");
            var rules = packet.ReadUInt32("GameRulesCount");
            packet.ReadInt32("ActiveTimerunningSeasonID");
            packet.ReadInt32("RemainingTimerunningSeasonSeconds");
            packet.ReadInt16("MaxPlayerGuidLookupsPerRequest");
            packet.ReadInt16("NameLookupTelemetryInterval");
            packet.ReadUInt32("NotFoundCacheTimeSeconds");
            packet.ReadUInt32("RealmPvpTypeOverride");

            packet.ReadInt32("MaxTries", "AddonChatThrottle");
            packet.ReadInt32("TriesRestoredPerSecond", "AddonChatThrottle");
            packet.ReadInt32("UsedTriesPerMessage", "AddonChatThrottle");
            packet.ReadInt32("UsedTriesPerMessage", "GuildChatThrottle");
            packet.ReadInt32("TriesRestoredPerSecond", "GuildChatThrottle");
            packet.ReadInt32("UsedTriesPerMessage", "GroupChatThrottle");
            packet.ReadInt32("TriesRestoredPerSecond", "GroupChatThrottle");

            packet.ReadSingle("AddonPerformanceMsgWarning");
            packet.ReadSingle("AddonPerformanceMsgError");
            packet.ReadSingle("AddonPerformanceMsgOverall");

            for (var i = 0; i < disabled; ++i)
            {
                packet.ReadByte("GameMode", "DisabledGameModes", i);
                packet.ReadInt32("ContentSetID", "DisabledGameModes", i);
                packet.ReadInt32("GameModeRecordID", "DisabledGameModes", i);
            }

            for (var i = 0; i < rules; ++i)
            {
                packet.ReadInt32("Rule", "GameRules", i);
                packet.ReadInt32("Value", "GameRules", i);
                packet.ReadSingle("ValueF", "GameRules", i);
            }

            packet.ResetBitReader();
            packet.ReadBit("VoiceEnabled");
            var hasEuropa = packet.ReadBit("HasEuropaTicketSystemStatus");
            packet.ReadBit("BpayStoreAvailable");
            packet.ReadBit("ItemRestorationButtonEnabled");
            var hasSessionAlert = packet.ReadBit("HasSessionAlert");
            packet.ReadBit("Enabled", "RAFSystem");
            packet.ReadBit("RecruitingEnabled", "RAFSystem");
            packet.ReadBit("CharUndeleteEnabled");

            foreach (var name in new[]
            {
                "RestrictedAccount", "CommerceServerEnabled", "TutorialEnabled", "VeteranTokenRedeemWillKick",
                "WorldTokenRedeemWillKick", "KioskModeEnabled", "CompetitiveModeEnabled", "RedeemForBalanceAvailable",
                "WarModeEnabled", "CommunitiesEnabled", "BnetGroupsEnabled", "CharacterCommunitiesEnabled",
                "ClubPresenceAllowSubscribeAll", "VoiceChatParentalDisabled", "VoiceChatParentalMuted", "QuestSessionEnabled",
                "IsChatMuted", "ClubFinderEnabled", "CommunityFinderEnabled", "BrowserCrashReporterEnabled",
                "SpeakForMeAllowed", "DoesAccountNeedAADCPrompt", "IsAccountOptedInToAADC", "LfgRequireAuthenticatorEnabled",
                "ScriptsDisallowedForBeta", "TimerunningEnabled", "PlayerIdentityOptionsEnabled",
                "IsPlayerContentTrackingEnabled", "LfdEnabled", "LfrEnabled", "PetHappinessEnabled",
                "GuildEventsEditsEnabled", "GuildTradeSkillsEnabled"
            })
                packet.ReadBit(name);

            packet.ReadBits("ClassicFlagBits10", 10);

            foreach (var name in new[]
            {
                "IsAccountCurrencyTransferEnabled", "NetEaseChatTelemetryEnabled", "LobbyMatchmakerQueueFromMainlineEnabled",
                "CanSendLobbyMatchmakerPartyCustomizations", "AddonProfilingEnabled", "GlobalUserGeneratedContentMuteEnabled",
                "AccountUserGeneratedContentIsRisky", "FriendsDisabled"
            })
                packet.ReadBit(name);
            packet.ResetBitReader();

            if (hasEuropa)
            {
                packet.ResetBitReader();
                packet.ReadBit("TicketsEnabled", "EuropaTicketSystemStatus");
                packet.ReadBit("BugsEnabled", "EuropaTicketSystemStatus");
                packet.ReadBit("ComplaintsEnabled", "EuropaTicketSystemStatus");
                packet.ReadBit("SuggestionsEnabled", "EuropaTicketSystemStatus");
                packet.ResetBitReader();
                for (var i = 0; i < 2; ++i)
                {
                    var t = i == 0 ? "ThrottleState" : "ExpensiveThrottleState";
                    packet.ReadUInt32("MaxTries", "EuropaTicketSystemStatus", t);
                    packet.ReadUInt32("PerMilliseconds", "EuropaTicketSystemStatus", t);
                    packet.ReadUInt32("TryCount", "EuropaTicketSystemStatus", t);
                    packet.ReadUInt32("LastResetTimeBeforeNow", "EuropaTicketSystemStatus", t);
                }
            }

            if (hasSessionAlert)
            {
                packet.ReadInt32("Delay", "SessionAlert");
                packet.ReadInt32("Period", "SessionAlert");
                packet.ReadInt32("DisplayTime", "SessionAlert");
            }
        }
    }
}
