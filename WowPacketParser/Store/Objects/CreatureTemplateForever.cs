using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.SQL;

namespace WowPacketParser.Store.Objects
{
    /// <summary>
    /// creature_template as it exists in the WoW Forever world database
    /// (TDB_full_world_forever). Column list taken verbatim from the TDB schema:
    /// entry KillCredit1 KillCredit2 name femaleName subname TitleAlt IconName
    /// RequiredExpansion VignetteID faction npcflag speed_walk speed_run scale
    /// Classification dmgschool BaseAttackTime RangeAttackTime BaseVariance RangeVariance
    /// unit_class unit_flags unit_flags2 unit_flags3 family trainer_class type VehicleId
    /// AIName MovementType ExperienceModifier RacialLeader movementId WidgetSetID
    /// WidgetSetUnitConditionID RegenHealth CreatureImmunitiesId flags_extra ScriptName
    /// StringId VerifiedBuild
    ///
    /// Fields the sniff cannot supply (faction, npcflag, speeds, AIName, ScriptName, ...)
    /// are declared with nullable: true so they are left out of the INSERT and the
    /// database defaults apply - most TDB columns are NOT NULL DEFAULT 0.
    /// </summary>
    [DBTableName("creature_template", TargetedDatabaseFlag.Forever)]
    public sealed record CreatureTemplateForever : IDataModel
    {
        [DBFieldName("entry", true)]
        public uint? Entry;

        [DBFieldName("KillCredit1")]
        public uint? KillCredit1;

        [DBFieldName("KillCredit2")]
        public uint? KillCredit2;

        [DBFieldName("name")]
        public string Name;

        [DBFieldName("femaleName")]
        public string FemaleName;

        [DBFieldName("subname")]
        public string SubName;

        [DBFieldName("TitleAlt")]
        public string TitleAlt;

        [DBFieldName("IconName")]
        public string IconName;

        [DBFieldName("RequiredExpansion")]
        public int? RequiredExpansion;

        [DBFieldName("VignetteID")]
        public uint? VignetteID;

        [DBFieldName("faction", nullable: true)]
        public uint? Faction;

        [DBFieldName("npcflag", nullable: true)]
        public NPCFlags? NpcFlag;

        [DBFieldName("speed_walk", nullable: true)]
        public float? SpeedWalk;

        [DBFieldName("speed_run", nullable: true)]
        public float? SpeedRun;

        [DBFieldName("scale", nullable: true)]
        public float? Scale;

        [DBFieldName("Classification")]
        public CreatureRank? Classification;

        [DBFieldName("dmgschool", nullable: true)]
        public int? DmgSchool;

        [DBFieldName("BaseAttackTime", nullable: true)]
        public uint? BaseAttackTime;

        [DBFieldName("RangeAttackTime", nullable: true)]
        public uint? RangeAttackTime;

        [DBFieldName("BaseVariance", nullable: true)]
        public float? BaseVariance;

        [DBFieldName("RangeVariance", nullable: true)]
        public float? RangeVariance;

        [DBFieldName("unit_class")]
        public uint? UnitClass;

        [DBFieldName("unit_flags", nullable: true)]
        public UnitFlags? UnitFlags;

        [DBFieldName("unit_flags2", nullable: true)]
        public UnitFlags2? UnitFlags2;

        [DBFieldName("unit_flags3", nullable: true)]
        public UnitFlags3? UnitFlags3;

        [DBFieldName("family")]
        public CreatureFamily? Family;

        [DBFieldName("trainer_class", nullable: true)]
        public uint? TrainerClass;

        [DBFieldName("type")]
        public CreatureType? Type;

        [DBFieldName("VehicleId", nullable: true)]
        public uint? VehicleId;

        [DBFieldName("AIName", nullable: true)]
        public string AIName;

        [DBFieldName("MovementType", nullable: true)]
        public byte? MovementType;

        [DBFieldName("ExperienceModifier", nullable: true)]
        public float? ExperienceModifier;

        [DBFieldName("RacialLeader")]
        public bool? RacialLeader;

        [DBFieldName("movementId")]
        public uint? MovementID;

        [DBFieldName("WidgetSetID")]
        public int? WidgetSetID;

        [DBFieldName("WidgetSetUnitConditionID")]
        public int? WidgetSetUnitConditionID;

        [DBFieldName("RegenHealth", nullable: true)]
        public bool? RegenHealth;

        [DBFieldName("CreatureImmunitiesId", nullable: true)]
        public int? CreatureImmunitiesId;

        [DBFieldName("flags_extra", nullable: true)]
        public uint? FlagsExtra;

        [DBFieldName("ScriptName", nullable: true)]
        public string ScriptName;

        [DBFieldName("StringId", nullable: true)]
        public string StringId;

        [DBFieldName("VerifiedBuild")]
        public int? VerifiedBuild = ClientVersion.BuildInt;

        // not a column of creature_template in TDB Forever - written to creature_template_difficulty
        public uint? TypeFlags;
        public uint? TypeFlags2;
        public uint? TypeFlags3;
    }
}
