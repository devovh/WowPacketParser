using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.SQL;

namespace WowPacketParser.Store.Objects
{
    /// <summary>
    /// gameobject_template as it exists in the WoW Forever world database
    /// (TDB_full_world_forever). Columns:
    /// entry type displayId name IconName castBarCaption unk1 size Data0..Data34
    /// ContentTuningId RequiredLevel AIName ScriptName StringId VerifiedBuild
    ///
    /// Data0..Data34 is MAX_GAMEOBJECT_DATA (35) in forevercore, SharedDefines.h:3234.
    /// AIName / ScriptName / StringId are not part of the query response and are left
    /// to the database defaults.
    /// </summary>
    [DBTableName("gameobject_template", TargetedDatabaseFlag.Forever)]
    public sealed record GameObjectTemplateForever : IDataModel
    {
        [DBFieldName("entry", true)]
        public uint? Entry;

        [DBFieldName("type")]
        public GameObjectType? Type;

        [DBFieldName("displayId")]
        public uint? DisplayID;

        [DBFieldName("name")]
        public string Name;

        [DBFieldName("IconName")]
        public string IconName;

        [DBFieldName("castBarCaption")]
        public string CastBarCaption;

        [DBFieldName("unk1")]
        public string Unk1;

        [DBFieldName("size")]
        public float? Size;

        [DBFieldName("Data", 35, true)]
        public int?[] Data;

        [DBFieldName("ContentTuningId")]
        public int? ContentTuningId;

        [DBFieldName("RequiredLevel")]
        public int? RequiredLevel;

        [DBFieldName("AIName", nullable: true)]
        public string AIName;

        [DBFieldName("ScriptName", nullable: true)]
        public string ScriptName;

        [DBFieldName("StringId", nullable: true)]
        public string StringId;

        [DBFieldName("VerifiedBuild")]
        public int? VerifiedBuild = ClientVersion.BuildInt;
    }
}
