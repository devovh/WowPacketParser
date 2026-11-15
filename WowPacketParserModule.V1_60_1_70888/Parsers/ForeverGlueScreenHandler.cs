using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN (0x470064) dla Forever 1.60.1.70938 (829 B).
    ///
    /// Uklad wyprowadzony z bajtow sniffa (kolejnosc pol jak w tc-forever, ktory pisze krotszy pakiet):
    ///   [6 B flagi][u32 CommercePricePollTimeSeconds][i64 RedeemForBalanceAmount][i32 MaxCharactersOnThisRealm]
    ///   [u32 LiveRegionCharacterCopySourceRegions][i32 ActiveBoostType][i32 TrialBoostType][i32 MinimumExpansionLevel]
    ///   [i32 MaximumExpansionLevel][i32 ContentSetID][u32 DisabledGameModes][u32 GameRules][u32 AvailableGameModeIDs]
    ///   [i32 ActiveTimerunningSeasonID][i32 RemainingTimerunningSeasonSeconds][u32 TimerunningConversionMinCharacterAge]
    ///   [i32 TimerunningConversionMaxSeasonID][i16 MaxPlayerGuidLookupsPerRequest][i16 NameLookupTelemetryInterval]
    ///   [u32 NotFoundCacheTimeSeconds][u32 DebugTimeEvents][i32 MostRecentTimeEventID][u32 EventRealmQueues]
    ///   [33 B jeszcze nieopisane]
    ///   tablice: LiveRegion (4 B), DisabledGameModes (9 B), GameRules (12 B: Rule, Value, ValueF), AvailableGameModeIDs (4 B).
    /// </summary>
    public static class ForeverGlueScreenHandler
    {
        [Parser(Opcode.SMSG_FEATURE_SYSTEM_STATUS_GLUE_SCREEN)]
        public static void HandleGlue(Packet packet)
        {
            packet.ReadBytes(6);
            packet.ReadUInt32("CommercePricePollTimeSeconds");
            packet.ReadInt64("RedeemForBalanceAmount");
            packet.ReadInt32("MaxCharactersOnThisRealm");
            var regions = packet.ReadUInt32("LiveRegionCharacterCopySourceRegionsCount");
            packet.ReadInt32("ActiveBoostType");
            packet.ReadInt32("TrialBoostType");
            packet.ReadInt32("MinimumExpansionLevel");
            packet.ReadInt32("MaximumExpansionLevel");
            packet.ReadInt32("ContentSetID");
            var disabled = packet.ReadUInt32("DisabledGameModesCount");
            var rules = packet.ReadUInt32("GameRulesCount");
            var available = packet.ReadUInt32("AvailableGameModeIDCount");
            packet.ReadInt32("ActiveTimerunningSeasonID");
            packet.ReadInt32("RemainingTimerunningSeasonSeconds");
            packet.ReadUInt32("TimerunningConversionMinCharacterAge");
            packet.ReadInt32("TimerunningConversionMaxSeasonID");
            packet.ReadInt16("MaxPlayerGuidLookupsPerRequest");
            packet.ReadInt16("NameLookupTelemetryInterval");
            packet.ReadUInt32("NotFoundCacheTimeSeconds");
            packet.ReadUInt32("DebugTimeEventCount");
            packet.ReadInt32("MostRecentTimeEventID");
            packet.ReadUInt32("EventRealmQueues");
            packet.ReadBytes(33);

            for (var i = 0; i < regions; ++i)
                packet.ReadUInt32("LiveRegionCharacterCopySourceRegion", i);

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

            for (var i = 0; i < available; ++i)
                packet.ReadInt32("GameModeID", "AvailableGameModeIDs", i);
        }
    }
}
