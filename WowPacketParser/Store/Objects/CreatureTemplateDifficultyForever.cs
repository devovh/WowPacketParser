using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.SQL;

namespace WowPacketParser.Store.Objects
{
    /// <summary>
    /// creature_template_difficulty as it exists in the WoW Forever world database
    /// (TDB_full_world_forever). Columns:
    /// Entry DifficultyID LevelScalingDeltaMin LevelScalingDeltaMax ContentTuningID
    /// HealthScalingExpansion HealthModifier ManaModifier ArmorModifier DamageModifier
    /// CreatureDifficultyID TypeFlags TypeFlags2 TypeFlags3 LootID PickPocketLootID
    /// SkinLootID GoldMin GoldMax StaticFlags1..8 VerifiedBuild
    ///
    /// The query response only carries HealthScalingExpansion, HealthModifier,
    /// ManaModifier, CreatureDifficultyID and the three TypeFlags; the rest is left to
    /// the database defaults.
    /// </summary>
    [DBTableName("creature_template_difficulty", TargetedDatabaseFlag.Forever)]
    public sealed record CreatureTemplateDifficultyForever : IDataModel
    {
        [DBFieldName("Entry", true)]
        public uint? Entry;

        [DBFieldName("DifficultyID", true)]
        public uint? DifficultyID;

        [DBFieldName("ContentTuningID")]
        public int? ContentTuningID = 0;

        [DBFieldName("HealthScalingExpansion")]
        public ClientType? HealthScalingExpansion;

        [DBFieldName("HealthModifier")]
        public float? HealthModifier;

        [DBFieldName("ManaModifier")]
        public float? ManaModifier;

        [DBFieldName("CreatureDifficultyID")]
        public int? CreatureDifficultyID;

        [DBFieldName("TypeFlags")]
        public uint? TypeFlags;

        [DBFieldName("TypeFlags2")]
        public uint? TypeFlags2;

        [DBFieldName("TypeFlags3")]
        public uint? TypeFlags3;

        [DBFieldName("VerifiedBuild")]
        public int? VerifiedBuild = ClientVersion.BuildInt;
    }
}

namespace WowPacketParser.Store.Objects
{
    /// <summary>
    /// creature_classic_level of the Forever core (tc-forever): the level range of an NPC entry.
    /// Forever does not scale vanilla creatures through ContentTuningID; Creature::ApplyLevelScaling
    /// reads this table first. Filled from UnitData.Level of the spawned units (min/max per entry).
    /// </summary>
    [DBTableName("creature_classic_level", TargetedDatabaseFlag.Forever)]
    public sealed record CreatureClassicLevelForever : IDataModel
    {
        [DBFieldName("entry", true)]
        public uint? Entry;

        [DBFieldName("level_min")]
        public byte? LevelMin;

        [DBFieldName("level_max")]
        public byte? LevelMax;
    }
}
