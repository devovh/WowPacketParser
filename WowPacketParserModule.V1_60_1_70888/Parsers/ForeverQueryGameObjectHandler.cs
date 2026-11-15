using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_QUERY_GAME_OBJECT_RESPONSE (0x4B0007) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (Server/Packets/QueryPackets.cpp:350 QueryGameObjectResponse::Write):
    ///
    ///   [u32 GameObjectID]
    ///   [GUID Guid]
    ///   [bits 1 Allow][FlushBits]
    ///   [u32 dlugosc statsData]
    ///   statsData:
    ///     [i32 Type][i32 DisplayID]
    ///     [4 x cstring Name]
    ///     [cstring IconName][cstring CastBarCaption][cstring UnkString]
    ///     [35 x i32 Data]            MAX_GAMEOBJECT_DATA = 35 (SharedDefines.h:3234)
    ///     [f32 Size]
    ///     [u8 QuestItemsCount][QuestItems[]]
    ///     [i32 ContentTuningId][i32 RequiredLevel]
    ///
    /// Wynik trafia do Storage.GameObjectTemplatesForever (schemat TDB Forever)
    /// oraz Storage.GameObjectTemplateQuestItems.
    /// </summary>
    public static class ForeverQueryGameObjectHandler
    {
        [HasSniffData]
        [Parser(Opcode.SMSG_QUERY_GAME_OBJECT_RESPONSE)]
        public static void HandleGameObjectQueryResponse(Packet packet)
        {
            var entry = packet.ReadInt32("Entry");
            ForeverGuid.Read(packet, "GUID");
            var allow = packet.ReadBit("Allow");

            if (!allow)
            {
                packet.ReadUInt32("DataSize");
                return;
            }

            packet.ReadUInt32("DataSize");

            var gameObject = new GameObjectTemplateForever
            {
                Entry = (uint)entry,
                Type = packet.ReadInt32E<GameObjectType>("Type"),
                DisplayID = packet.ReadUInt32("Display ID")
            };

            var name = new string[4];
            for (var i = 0; i < 4; i++)
                name[i] = packet.ReadCString("Name", i);
            gameObject.Name = name[0];

            gameObject.IconName = packet.ReadCString("Icon Name");
            gameObject.CastBarCaption = packet.ReadCString("Opening Text");
            gameObject.Unk1 = packet.ReadCString("Closing Text");

            gameObject.Data = new int?[35];
            for (var i = 0; i < gameObject.Data.Length; i++)
                gameObject.Data[i] = packet.ReadInt32("Data", i);

            gameObject.Size = packet.ReadSingle("Size");

            var questItemCount = packet.ReadByte("QuestItemsCount");
            for (var i = 0; i < questItemCount; i++)
            {
                Storage.GameObjectTemplateQuestItems.Add(new GameObjectTemplateQuestItem
                {
                    GameObjectEntry = (uint)entry,
                    Idx = (uint)i,
                    ItemId = (uint)packet.ReadInt32<ItemId>("Quest Item", i)
                }, packet.TimeSpan);
            }

            gameObject.ContentTuningId = packet.ReadInt32("ContentTuningId");
            gameObject.RequiredLevel = packet.ReadInt32("RequiredLevel");

            Storage.GameObjectTemplatesForever.Add(gameObject, packet.TimeSpan);

            Storage.ObjectNames.Add(new ObjectName
            {
                ObjectType = StoreNameType.GameObject,
                ID = (int)entry,
                Name = gameObject.Name
            }, packet.TimeSpan);

            packet.AddSniffData(StoreNameType.GameObject, (int)entry, "QUERY_RESPONSE");
        }
    }
}
