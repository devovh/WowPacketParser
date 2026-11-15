using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.SQL;

namespace WowPacketParser.Store.Objects
{
    /// <summary>
    /// One SMSG_LOOT_RESPONSE window of a creature corpse (Forever 1.60.1), raw material for creature_loot_template.
    /// </summary>
    public sealed class ForeverLootWindow
    {
        public uint Entry;
        public WowGuid Owner;
        public byte AcquireReason;
        public System.Collections.Generic.List<(int ItemId, uint Quantity)> Items = new();
    }

    /// <summary>creature_loot_template of the Forever core (tc-forever world database).</summary>
    [DBTableName("creature_loot_template", TargetedDatabaseFlag.Forever)]
    public sealed record CreatureLootTemplateForever : IDataModel
    {
        [DBFieldName("Entry", true)]
        public uint? Entry;

        [DBFieldName("ItemType", true)]
        public sbyte? ItemType;

        [DBFieldName("Item", true)]
        public uint? Item;

        [DBFieldName("Chance")]
        public float? Chance;

        [DBFieldName("QuestRequired")]
        public bool? QuestRequired;

        [DBFieldName("LootMode")]
        public ushort? LootMode;

        [DBFieldName("GroupId")]
        public byte? GroupId;

        [DBFieldName("MinCount")]
        public byte? MinCount;

        [DBFieldName("MaxCount")]
        public byte? MaxCount;
    }
}
