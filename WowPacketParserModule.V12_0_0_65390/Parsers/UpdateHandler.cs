using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.PacketStructures;
using WowPacketParser.Parsing;
using WowPacketParser.Proto;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;
using WowPacketParser.Store.Objects.UpdateFields;
using WowPacketParserModule.V6_0_2_19033.Enums;
using AreaTriggerHandler = WowPacketParserModule.V11_0_0_55666.Parsers.AreaTriggerHandler;
using CoreFields = WowPacketParser.Enums.Version;
using CoreParsers = WowPacketParser.Parsing.Parsers;
using MovementFlag = WowPacketParser.Enums.v12.MovementFlag;
using SplineFlag = WowPacketParserModule.V6_0_2_19033.Enums.SplineFlag;

namespace WowPacketParserModule.V12_0_0_65390.Parsers
{
    public static class UpdateHandler
    {
        // Forever 1.60.1 (70338-70938) ma wlasny uklad bloku wartosci i fragmentow encji.
        // UWAGA: 70959 NIE moze tu wejsc - zmierzone na dump_1.60.1.70959_2026-11-18:
        //   IsForever do 70938 -> 127885 (99.596%) / 321 bledow / 1455 spawnow creature
        //   IsForever do 70959 -> 127767 (99.504%) / 439 bledow /    0 spawnow creature
        // 70959 wysyla co innego (serwer: BaseEntity::BuildCreateUpdateBlockForPlayer w tc-forever
        // pisze blok tworzenia w ukladzie retail), wiec zostaje na sciezce retail.
        private static readonly bool IsForever =
            ClientVersion.Build >= ClientVersionBuild.V1_60_1_70338 &&
            ClientVersion.Build <= ClientVersionBuild.V1_60_1_70938;

        // Forever: jednostka bez zmienionych pol nie ma maski fragmentow (serwer jej nie pisze),
        // a mimo to dotychczasowy czytnik probowal ja odczytac i zjadal bajty nastepnego obiektu.
        // Zakres 70338-70938 (bez 70938) i osobno 70959 - 70938 MUSI byc poza, bo z ta galazia
        // daje 13890 bledow zamiast 18 (zmierzone). Samo 70959: 197 -> 170 bledow.
        private static readonly bool HasShortFragmentMask =
            (ClientVersion.Build >= ClientVersionBuild.V1_60_1_70338 && ClientVersion.Build < ClientVersionBuild.V1_60_1_70938) ||
            ClientVersion.Build == ClientVersionBuild.V1_60_1_70959 ||
            ClientVersion.Build == ClientVersionBuild.V1_60_1_71037;

        [HasSniffData] // in ReadCreateObjectBlock
        [Parser(Opcode.SMSG_UPDATE_OBJECT, ClientVersionBuild.V12_1_0_69214)]
        public static void HandleUpdateObject(Packet packet)
        {
            var updateObject = packet.Holder.UpdateObject = new();
            uint map = updateObject.MapId = packet.ReadUInt16<MapId>("MapID");
            var count = packet.ReadUInt32("NumObjUpdates");
            packet.ResetBitReader();
            packet.ReadBit("IsEndOfBatch");
            var hasRemovedObjects = packet.ReadBit("HasRemovedObjects");
            if (hasRemovedObjects)
            {
                var destroyedObjCount = packet.ReadInt16("DestroyedObjCount");
                var removedObjCount = packet.ReadUInt32("RemovedObjCount"); // destroyed + out of range
                var outOfRangeObjCount = removedObjCount - destroyedObjCount;

                for (var i = 0; i < destroyedObjCount; i++)
                {
                    var partWriter = new StringBuilderProtoPart(packet.Writer);
                    var guid = packet.ReadPackedGuid128("ObjectGUID", "Destroyed", i);
                    updateObject.Destroyed.Add(new DestroyedObject(){Guid=guid, TextStartOffset = partWriter.StartOffset, TextLength = partWriter.Length, Text = partWriter.Text});
                }

                for (var i = 0; i < outOfRangeObjCount; i++)
                {
                    var partWriter = new StringBuilderProtoPart(packet.Writer);
                    var guid = packet.ReadPackedGuid128("ObjectGUID", "OutOfRange", i);
                    updateObject.OutOfRange.Add(new DestroyedObject(){Guid=guid, TextStartOffset = partWriter.StartOffset, TextLength = partWriter.Length, Text = partWriter.Text});
                }
            }
            packet.ReadUInt32("Data size");

            for (var i = 0; i < count; i++)
            {
                var type = (UpdateTypeCataclysm)packet.ReadByte();

                var partWriter = new StringBuilderProtoPart(packet.Writer);
                packet.AddValue("UpdateType", type.ToString(), i);
                var guid = packet.ReadPackedGuid128("Object Guid", i);

                if (type == UpdateTypeCataclysm.CreateObject1)
                {
                    var packetTimestamp = Utilities.GetUnixTimeFromDateTime(packet.Time);

                    var spawnTimestamp = packetTimestamp & ~((1 << 23) - 1);
                    spawnTimestamp += guid.GetSpawnTimestamp() + Packet.UtcTimeOffset;

                    var timestampDiff = Math.Abs(spawnTimestamp - packetTimestamp);
                    if (timestampDiff <= Settings.TreatAsCreateObject2Tolerance && !Storage.Objects.ContainsKey(guid))
                        type = UpdateTypeCataclysm.CreateObject2;

                    packet.AddValue("TreatAsCreateObject2", (type == UpdateTypeCataclysm.CreateObject2), i);
                    packet.AddValue("TimestampDiff", timestampDiff, i);
                }

                switch (type)
                {
                    case UpdateTypeCataclysm.Values:
                    {
                        var updateValues = new UpdateValues();
                        var updatefieldSize = packet.ReadUInt32();
                        var handler = CoreFields.UpdateFields.GetHandler();
                        if (handler == null)
                        {
                            packet.WriteLine("UPDATE_OBJECT: no UpdateFields handler for this build - values block skipped");
                            break;
                        }
                        updateValues.Fields = new();
                        using (var fieldsData = new Packet(packet.ReadBytes((int)updatefieldSize), packet.Opcode, packet.Time, packet.Direction, packet.Number, packet.Writer, packet.FileName))
                        {
                            WoWObject obj;
                            Storage.Objects.TryGetValue(guid, out obj);

                            var fragments = obj?.EntityFragments ?? [new WowCSEntityFragment(WowCSEntityFragments1127.CGObject)];

                            fieldsData.ReadBool("IsOwned", i);
                            var hasFragmentUpdates = fieldsData.ReadBool("HasFragmentUpdates", i);
                            if (hasFragmentUpdates)
                            {
                                // Forever 1.60.1: zamiast listy identyfikatorow fragmentow idzie od razu
                                // u32 maska typow (1<<0 Object, 1<<5 Unit, 1<<6 Player, 1<<7 ActivePlayer,
                                // 1<<8 GameObject). Obecnosc bitu = dane tego typu sa w bloku.
                                // Patrz Player::BuildValuesUpdate / Creature::BuildValuesUpdate w tc-forever.
                                // Uwaga: maska jest tylko pod flaga HasFragmentUpdates - poza nia blok
                                // wartosci ma 3 bajty (IsOwned, HasFragmentUpdates, ObjectChangesMask).
                                if (IsForever && ClientVersion.Build != ClientVersionBuild.V1_60_1_70938)
                                {
                                    fragments = ReadForeverValuesTypeMask(fieldsData, i);
                                    if (obj != null)
                                        obj.EntityFragments = fragments;
                                    break;
                                }

                                // Build 70938 uses serialization type + fragment IDs + 0xFF,
                                // followed by the changed-fragment mask, as in this reader.
                                switch (fieldsData.ReadByte("ArchetypeSerializationType", i))
                                {
                                    case 0:
                                        fragments = ReadEntityFragments(fieldsData, "NewEntityFragmentID", i);
                                        if (obj != null)
                                            obj.EntityFragments = fragments;
                                        break;
                                    case 1:
                                        fragments.AddRange(ReadEntityFragments(fieldsData, "NewEntityFragmentID", i));
                                        foreach (var removedFragment in ReadEntityFragments(fieldsData, "RemovedEntityFragmentID", i))
                                            fragments.RemoveAll(f => f.UniversalValue == removedFragment.UniversalValue);
                                        fragments.Sort();
                                        break;
                                    default:
                                        if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
                                            throw new InvalidOperationException("Unknown entity fragment serialization type for build 70938.");
                                        break;
                                }
                            }

                            if (handler == null)
                            {
                                packet.WriteLine("UPDATE_OBJECT: no UpdateFields handler for this build - values block skipped");
                                break;
                            }

                            // 70959/71037 pisza maske typow (ReadForeverValuesTypeMask) zamiast retailowego
                            // u32 updateTypeFlag. Wolamy to tylko przy hasFragmentUpdates, bo bez flagi blok
                            // ma 3 bajty i zadnej maski typow (128833 blokow z 1 nieprzeczytanym bajtem).
                            if ((IsForever && ClientVersion.Build != ClientVersionBuild.V1_60_1_70938)
                                || ((ClientVersion.Build == ClientVersionBuild.V1_60_1_70959
                                     || ClientVersion.Build == ClientVersionBuild.V1_60_1_71037) && hasFragmentUpdates))
                                ReadUpdateEntityFragmentDataForever(obj, fragments, updateValues, handler, fieldsData, i);
                            else if ((ClientVersion.Build == ClientVersionBuild.V1_60_1_70959
                                      || ClientVersion.Build == ClientVersionBuild.V1_60_1_71037) && !hasFragmentUpdates)
                            {
                                // Bez flagi fragmentow blok to IsOwned, HasFragmentUpdates i maska zmian
                                // ObjectData (ObjectData::WriteUpdate: WriteBits(GetBlock(0), 4) + FlushBits,
                                // czyli zawsze caly bajt). Bez tego zostawal 1 nieprzeczytany bajt
                                // w 128833 blokach.
                                var objData = handler.ReadUpdateObjectData(fieldsData, i);
                                if (obj is { ObjectData: IMutableObjectData mutObj })
                                    mutObj.UpdateData(objData);
                                else if (obj != null)
                                    obj.ObjectData = objData;
                                updateValues.Fields.UpdateData(objData);

                                // Po masce ObjectData ida jeszcze dane typu (bez maski typow) - kolejnosc
                                // jak w galezi Forever: Unit, Player, ActivePlayer, GameObject.
                                if (fieldsData.Position < fieldsData.Length)
                                {
                                    if (obj is Unit unitObj)
                                    {
                                        var unitData = handler.ReadUpdateUnitData(fieldsData, i);
                                        if (unitObj.UnitData is IMutableUnitData mutUnit)
                                            mutUnit.UpdateData(unitData);
                                        else
                                            unitObj.UnitData = unitData;
                                        updateValues.Fields.UpdateData(unitData);
                                    }
                                }
                            }
                            else
                                ReadUpdateEntityFragmentData(obj, fragments, updateValues, handler, fieldsData, i);

                            if (fieldsData.Position != fieldsData.Length)
                            {
                                if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
                                    throw new InvalidOperationException($"70938 update fields have {fieldsData.Length - fieldsData.Position} unread bytes.");
                                packet.WriteLine($"Updatefields not fully read! Current position: {fieldsData.Position} Length: {fieldsData.Length} Bytes remaining: {fieldsData.Length - fieldsData.Position}");
                            }
                        }
                        updateObject.Updated.Add(new UpdateObject{Guid = guid, Values = updateValues, TextStartOffset = partWriter.StartOffset, TextLength = partWriter.Length, Text = partWriter.Text});
                        break;
                    }
                    case UpdateTypeCataclysm.CreateObject1:
                    case UpdateTypeCataclysm.CreateObject2:
                    {
                        var createType = type.ToCreateObjectType();
                        var createObject = new CreateObject() { Guid = guid, Values = new(){}, CreateType = createType };
                        ReadCreateObjectBlock(packet, createObject, guid, map, createType, i);
                        createObject.Text = partWriter.Text;
                        createObject.TextStartOffset = partWriter.StartOffset;
                        createObject.TextLength = partWriter.Length;
                        updateObject.Created.Add(createObject);
                        break;
                    }
                }
            }
        }

        private static WowCSEntityFragment TransformFragment1127(Packet packet, string name, byte fragmentId, int idx, int fragmentIdx) =>
            new (packet.AddValue(name, (WowCSEntityFragments1127)fragmentId, idx, fragmentIdx));

        private static WowCSEntityFragment TransformFragment1215(Packet packet, string name, byte fragmentId, int idx, int fragmentIdx) =>
            new (packet.AddValue(name, (WowCSEntityFragments1215)fragmentId, idx, fragmentIdx));

        private static List<WowCSEntityFragment> ReadEntityFragments(Packet packet, string name, int idx)
        {
            Func<Packet, string, byte, int, int, WowCSEntityFragment> fragmentTransformer;
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V12_1_5_70340))
                fragmentTransformer = TransformFragment1215;
            else
                fragmentTransformer = TransformFragment1127;

            var fragmentIds = new List<WowCSEntityFragment>();
            byte fragmentId;
            while ((fragmentId = packet.ReadByte()) != 255)
                fragmentIds.Add(fragmentTransformer(packet, name, fragmentId, idx, fragmentIds.Count));

            return fragmentIds;
        }

        private static void ReadCreateEntityFragmentData(WoWObject obj, CreateObject createObject, CoreParsers.UpdateFieldsHandlerBase handler, Packet fieldsData, UpdateFieldFlag flags, int index)
        {
            foreach (var fragment in obj.EntityFragments)
            {
                if (WowCSUtilities.IsIndirect(fragment.UniversalValue) && !fieldsData.ReadBool("IndirectFragmentActive [" + fragment.UniversalValue + "]", index))
                    continue;

                switch (fragment.UniversalValue)
                {
                    case WowCSEntityFragments.CGObject:
                        obj.ObjectData = handler.ReadCreateObjectData(fieldsData, flags, index);
                        createObject.Values.Fields.UpdateData(obj.ObjectData);
                        switch (obj.Type)
                        {
                            case ObjectType.Item:
                                handler.ReadCreateItemData(fieldsData, flags, index);
                                break;
                            case ObjectType.Container:
                                handler.ReadCreateItemData(fieldsData, flags, index);
                                handler.ReadCreateContainerData(fieldsData, flags, index);
                                break;
                            case ObjectType.AzeriteEmpoweredItem:
                                handler.ReadCreateItemData(fieldsData, flags, index);
                                handler.ReadCreateAzeriteEmpoweredItemData(fieldsData, flags, index);
                                break;
                            case ObjectType.AzeriteItem:
                                handler.ReadCreateItemData(fieldsData, flags, index);
                                handler.ReadCreateAzeriteItemData(fieldsData, flags, index);
                                break;
                            case ObjectType.Unit:
                            {
                                var data = (obj as Unit).UnitData = handler.ReadCreateUnitData(fieldsData, flags, index);
                                createObject.Values.Fields.UpdateData(data);
                                break;
                            }
                            case ObjectType.Player:
                                handler.ReadCreateUnitData(fieldsData, flags, index);
                                handler.ReadCreatePlayerData(fieldsData, flags, index);
                                break;
                            case ObjectType.ActivePlayer:
                                handler.ReadCreateUnitData(fieldsData, flags, index);
                                handler.ReadCreatePlayerData(fieldsData, flags, index);
                                handler.ReadCreateActivePlayerData(fieldsData, flags, index);
                                break;
                            case ObjectType.GameObject:
                            {
                                var data = (obj as GameObject).GameObjectData = handler.ReadCreateGameObjectData(fieldsData, flags, index);
                                createObject.Values.Fields.UpdateData(data);
                                break;
                            }
                            case ObjectType.DynamicObject:
                                handler.ReadCreateDynamicObjectData(fieldsData, flags, index);
                                break;
                            case ObjectType.Corpse:
                                handler.ReadCreateCorpseData(fieldsData, flags, index);
                                break;
                            case ObjectType.AreaTrigger:
                                (obj as AreaTriggerCreateProperties).AreaTriggerData = handler.ReadCreateAreaTriggerData(fieldsData, flags, index);
                                break;
                            case ObjectType.SceneObject:
                                (obj as SceneObject).SceneObjectData = handler.ReadCreateSceneObjectData(fieldsData, flags, index);
                                break;
                            case ObjectType.Conversation:
                                (obj as ConversationTemplate).ConversationData = handler.ReadCreateConversationData(fieldsData, flags, index);
                                break;
                        }
                        break;
                    case WowCSEntityFragments.FVendor_C: handler.ReadCreateVendorData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FMeshObjectData_C: handler.ReadCreateMeshObjectData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingDecor_C: handler.ReadCreateHousingDecorData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingRoom_C: handler.ReadCreateHousingRoomData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingRoomComponentMesh_C: handler.ReadCreateHousingRoomComponentMeshData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingPlayerHouse_C: handler.ReadCreateHousingPlayerHouseData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FJamHousingCornerstone_C: handler.ReadCreateHousingCornerstoneData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingPlotAreaTrigger_C: handler.ReadCreateHousingPlotAreaTriggerData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FNeighborhoodMirrorData_C: handler.ReadCreateNeighborhoodMirrorData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FMirroredPositionData_C: handler.ReadCreateMirroredPositionData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.PlayerHouseInfoComponent_C: handler.ReadCreatePlayerHouseInfoComponentData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingStorage_C: handler.ReadCreateHousingStorageData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.FHousingFixture_C: handler.ReadCreateHousingFixtureData(fieldsData, flags, index); break;
                    case WowCSEntityFragments.PlayerInitiativeComponent_C: handler.ReadCreatePlayerInitiativeComponentData(fieldsData, flags, index); break;
                }
            }
        }

        /// <summary>
        /// Forever (70338-70938): dane fragmentow w bloku wartosci. Po liscie fragmentow idzie
        /// u32 rozmiar, u32 maska typow (TYPEID_OBJECT/UNIT/PLAYER/ACTIVEPLAYER/GAMEOBJECT)
        /// i pola tych typow - patrz Player::BuildValuesUpdate / Creature::BuildValuesUpdate
        /// w tc-forever. Wczesniej ten blok nie byl czytany wcale, wiec kazdy pakiet z
        /// graczem/jednostka konczyl sie na "Updatefields not fully read".
        /// </summary>
        /// <summary>
        /// Forever 1.60.1: zamiast listy identyfikatorow fragmentow blok wartosci niesie
        /// u32 maske typow - bit 1&lt;&lt;TYPEID_* oznacza, ze dane tego typu ida w bloku.
        /// Kolejnosc czytania (Object, Unit, Player, ActivePlayer, GameObject) jest ta sama
        /// co kolejnosc zapisu w BaseEntity::BuildValuesUpdateBlockForPlayer w tc-forever.
        /// </summary>
        private static List<WowCSEntityFragment> ReadForeverValuesTypeMask(Packet packet, int index)
        {
            var typeMask = packet.ReadUInt32("TypeMask", index);

            var fragments = new List<WowCSEntityFragment>();
            if ((typeMask & (1u << 0)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.CGObject));
            // Forever: bit 1..4 = typy itemowe (Item, Container, AzeriteEmpoweredItem, AzeriteItem).
            // Bez tego blok Item konczyl sie z 1 nieprzeczytanym bajtem (129229 razy w sniffie).
            if ((typeMask & (1u << 1)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.Tag_Item));
            if ((typeMask & (1u << 5)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.Tag_Unit));
            if ((typeMask & (1u << 6)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.Tag_Player));
            if ((typeMask & (1u << 7)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.Tag_ActivePlayer));
            if ((typeMask & (1u << 8)) != 0) fragments.Add(new WowCSEntityFragment(WowCSEntityFragments1100.Tag_GameObject));
            return fragments;
        }

        private static void ReadUpdateEntityFragmentDataForever(WoWObject obj, List<WowCSEntityFragment> fragments, UpdateValues updateValues, CoreParsers.UpdateFieldsHandlerBase handler, Packet fieldsData, int index)
        {
            foreach (var existingFragment in fragments)
            {
                if (!WowCSUtilities.IsUpdateable(existingFragment.UniversalValue))
                    continue;

                switch (existingFragment.UniversalValue)
                {
                    case WowCSEntityFragments.CGObject:
                    {
                        var data = handler.ReadUpdateObjectData(fieldsData, index);
                        if (obj is { ObjectData: IMutableObjectData mut })
                            mut.UpdateData(data);
                        else if (obj != null)
                            obj.ObjectData = data;

                        updateValues.Fields.UpdateData(data);
                        break;
                    }
                    case WowCSEntityFragments.Tag_Item:
                    {
                        handler.ReadUpdateItemData(fieldsData, index);
                        break;
                    }
                    case WowCSEntityFragments.PlayerHouseInfoComponent_C:
                    case WowCSEntityFragments.PlayerInitiativeComponent_C:
                    case WowCSEntityFragments.Tag_Unit:
                    case WowCSEntityFragments.Tag_Player:
                    case WowCSEntityFragments.Tag_GameObject:
                    {
                        // Forever: dane typu to u32 rozmiar (tylko przy pierwszym typie), potem pola
                        // (ObjectData, UnitData, PlayerData, ActivePlayerData, GameObjectData).
                        // Uwaga: przy hasFragmentUpdates == False blok ma 3 bajty (IsOwned,
                        // HasFragmentUpdates, ObjectChangesMask) i ZADNEJ maski typow - czytanie
                        // u32 zjadaloby bajty nastepnego obiektu (9484 EndOfStream).
                        var typeMask = fieldsData.ReadUInt32("TypeMask", index);
                        const uint TypeMaskObject = 1u << 0;
                        const uint TypeMaskUnit = 1u << 5;
                        const uint TypeMaskPlayer = 1u << 6;
                        const uint TypeMaskActivePlayer = 1u << 7;
                        const uint TypeMaskGameObject = 1u << 8;

                        if ((typeMask & TypeMaskObject) != 0)
                        {
                            var data = handler.ReadUpdateObjectData(fieldsData, index);
                            if (obj is { ObjectData: IMutableObjectData mutObject })
                                mutObject.UpdateData(data);
                            else if (obj != null)
                                obj.ObjectData = data;
                            updateValues.Fields.UpdateData(data);
                        }
                        if ((typeMask & TypeMaskUnit) != 0)
                        {
                            var data = handler.ReadUpdateUnitData(fieldsData, index);
                            if (obj is Unit unit)
                            {
                                if (unit.UnitData is IMutableUnitData mutUnit)
                                    mutUnit.UpdateData(data);
                                else
                                    unit.UnitData = data;
                            }
                            updateValues.Fields.UpdateData(data);
                        }
                        if ((typeMask & TypeMaskPlayer) != 0)
                            handler.ReadUpdatePlayerData(fieldsData, index);
                        if ((typeMask & TypeMaskActivePlayer) != 0)
                            handler.ReadUpdateActivePlayerData(fieldsData, index);
                        if ((typeMask & TypeMaskGameObject) != 0)
                        {
                            var data = handler.ReadUpdateGameObjectData(fieldsData, index);
                            if (obj is GameObject go)
                            {
                                if (go.GameObjectData is IMutableGameObjectData mutGo)
                                    mutGo.UpdateData(data);
                                else
                                    go.GameObjectData = data;
                            }
                            updateValues.Fields.UpdateData(data);
                        }
                        break;
                    }
                }
            }
        }

        private static void ReadUpdateEntityFragmentData(WoWObject obj, List<WowCSEntityFragment> fragments, UpdateValues updateValues, CoreParsers.UpdateFieldsHandlerBase handler, Packet fieldsData, int index)
        {
            var fragmentBitCount = 0;
            foreach (var existingFragment in fragments)
            {
                if (!WowCSUtilities.IsUpdateable(existingFragment.UniversalValue))
                    continue;

                ++fragmentBitCount;
                if (WowCSUtilities.IsIndirect(existingFragment.UniversalValue))
                    ++fragmentBitCount;
            }

            // Forever (build 70338-70938): jednostka bez zmienionych pol ma blok wartosci
            // bez maski fragmentow. Lista fragmentow jest wtedy pusta (bitow 0-3), a maska
            // (1-2 bajty) i tak nie zawiera nic, wiec jej odczyt zjadalby bajty sasiedniego
            // obiektu. Dla wyzszych buildow zostaje dotychczasowe zachowanie.
            if (HasShortFragmentMask && fragmentBitCount <= 3)
                return;

            var changedFragments = new BitArray(fieldsData.ReadBytes((fragmentBitCount + 7) / 8));

            foreach (var existingFragment in fragments)
            {
                if (!WowCSUtilities.IsUpdateable(existingFragment.UniversalValue))
                    continue;

                var fragmentBitIndex = WowCSUtilities.GetUpdateBitIndex(fragments, existingFragment.UniversalValue);
                if (fragmentBitIndex < 0 || !changedFragments[fragmentBitIndex])
                    continue;

                if (WowCSUtilities.IsIndirect(existingFragment.UniversalValue) && !changedFragments[fragmentBitIndex + 1])
                    continue;

                switch (existingFragment.UniversalValue)
                {
                    case WowCSEntityFragments.CGObject:
                    {
                        var updateTypeFlag = fieldsData.ReadUInt32();
                        if ((updateTypeFlag & 0x0001) != 0)
                        {
                            var data = handler.ReadUpdateObjectData(fieldsData, index);
                            if (obj is { ObjectData: IMutableObjectData mut })
                                mut.UpdateData(data);
                            else if (obj != null)
                                obj.ObjectData = data;

                            updateValues.Fields.UpdateData(data);
                        }
                        if ((updateTypeFlag & 0x0002) != 0)
                            handler.ReadUpdateItemData(fieldsData, index);
                        if ((updateTypeFlag & 0x0004) != 0)
                            handler.ReadUpdateContainerData(fieldsData, index);
                        if ((updateTypeFlag & 0x0008) != 0)
                            handler.ReadUpdateAzeriteEmpoweredItemData(fieldsData, index);
                        if ((updateTypeFlag & 0x0010) != 0)
                            handler.ReadUpdateAzeriteItemData(fieldsData, index);
                        if ((updateTypeFlag & 0x0020) != 0)
                        {
                            var unit = obj as Unit;
                            var data = handler.ReadUpdateUnitData(fieldsData, index);
                            if (unit is { UnitData: IMutableUnitData mut })
                                mut.UpdateData(data);
                            else if (unit != null)
                                unit.UnitData = data;

                            updateValues.Fields.UpdateData(data);
                        }
                        if ((updateTypeFlag & 0x0040) != 0)
                            handler.ReadUpdatePlayerData(fieldsData, index);
                        if ((updateTypeFlag & 0x0080) != 0)
                            handler.ReadUpdateActivePlayerData(fieldsData, index);
                        if ((updateTypeFlag & 0x0100) != 0)
                        {
                            var go = obj as GameObject;
                            var data = handler.ReadUpdateGameObjectData(fieldsData, index);
                            if (go is { GameObjectData: IMutableGameObjectData mut })
                                mut.UpdateData(data);
                            else if (go != null)
                                go.GameObjectData = data;

                            updateValues.Fields.UpdateData(data);
                        }
                        if ((updateTypeFlag & 0x0200) != 0)
                            handler.ReadUpdateDynamicObjectData(fieldsData, index);
                        if ((updateTypeFlag & 0x0400) != 0)
                            handler.ReadUpdateCorpseData(fieldsData, index);
                        if ((updateTypeFlag & 0x0800) != 0)
                        {
                            var at = obj as AreaTriggerCreateProperties;
                            var data = handler.ReadUpdateAreaTriggerData(fieldsData, index);

                            if (data.Spline != null)
                                AreaTriggerHandler.ProcessAreaTriggerSpline(at, data, fieldsData, index);
                            else if (data.Orbit != null)
                                AreaTriggerHandler.ProcessAreaTriggerOrbit(at, data, fieldsData, index);
                        }
                        if ((updateTypeFlag & 0x1000) != 0)
                            handler.ReadUpdateSceneObjectData(fieldsData, index);
                        if ((updateTypeFlag & 0x2000) != 0)
                        {
                            var conversation = obj as ConversationTemplate;
                            var data = handler.ReadUpdateConversationData(fieldsData, index);
                            if (conversation is { ConversationData: IMutableConversationData mut })
                                mut.UpdateData(data);
                            else if (conversation != null)
                                conversation.ConversationData = data;
                        }
                        break;
                    }
                    case WowCSEntityFragments.FVendor_C: handler.ReadUpdateVendorData(fieldsData, index); break;
                    case WowCSEntityFragments.FMeshObjectData_C: handler.ReadUpdateMeshObjectData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingDecor_C: handler.ReadUpdateHousingDecorData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingRoom_C: handler.ReadUpdateHousingRoomData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingRoomComponentMesh_C: handler.ReadUpdateHousingRoomComponentMeshData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingPlayerHouse_C: handler.ReadUpdateHousingPlayerHouseData(fieldsData, index); break;
                    case WowCSEntityFragments.FJamHousingCornerstone_C: handler.ReadUpdateHousingCornerstoneData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingPlotAreaTrigger_C: handler.ReadUpdateHousingPlotAreaTriggerData(fieldsData, index); break;
                    case WowCSEntityFragments.FNeighborhoodMirrorData_C: handler.ReadUpdateNeighborhoodMirrorData(fieldsData, index); break;
                    case WowCSEntityFragments.FMirroredPositionData_C: handler.ReadUpdateMirroredPositionData(fieldsData, index); break;
                    case WowCSEntityFragments.PlayerHouseInfoComponent_C: handler.ReadUpdatePlayerHouseInfoComponentData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingStorage_C: handler.ReadUpdateHousingStorageData(fieldsData, index); break;
                    case WowCSEntityFragments.FHousingFixture_C: handler.ReadUpdateHousingFixtureData(fieldsData, index); break;
                    case WowCSEntityFragments.PlayerInitiativeComponent_C: handler.ReadUpdatePlayerInitiativeComponentData(fieldsData, index); break;
                }
            }
        }

        private static void ReadCreateObjectBlock(Packet packet, CreateObject createObject, WowGuid guid, uint map, CreateObjectType createType, int index)
        {
            ObjectType objType = ObjectTypeConverter.Convert(packet.ReadByteE<ObjectType801>("Object Type", index));

            WoWObject obj = CoreParsers.UpdateHandler.CreateObject(objType, guid, map);

            obj.CreateType = createType;
            obj.Movement = ReadMovementUpdateBlock(packet, createObject, guid, obj, index);

            createObject.Values.Fields = new();
            var updatefieldSize = packet.ReadUInt32();
            using (var fieldsData = new Packet(packet.ReadBytes((int)updatefieldSize), packet.Opcode, packet.Time, packet.Direction, packet.Number, packet.Writer, packet.FileName))
            {
                var flags = fieldsData.ReadByteE<UpdateFieldFlag>("FieldFlags", index);
                obj.EntityFragments = ReadEntityFragments(fieldsData, "EntityFragmentID", index);
                var handler = CoreFields.UpdateFields.GetHandler();

                if (handler == null)
                {
                    packet.WriteLine("UPDATE_OBJECT: no UpdateFields handler for this build - create block skipped");
                    return;
                }

                ReadCreateEntityFragmentData(obj, createObject, handler, fieldsData, flags, index);

                if (fieldsData.Position != fieldsData.Length)
                {
                    if (ClientVersion.Build == ClientVersionBuild.V1_60_1_70938)
                        throw new InvalidOperationException($"70938 update fields have {fieldsData.Length - fieldsData.Position} unread bytes.");
                    packet.WriteLine($"Updatefields not fully read! Current position: {fieldsData.Position} Length: {fieldsData.Length} Bytes remaining: {fieldsData.Length - fieldsData.Position}");
                }
            }

            if (obj is AreaTriggerCreateProperties createProperties)
            {
                AreaTriggerTemplate areaTriggerTemplate = new AreaTriggerTemplate
                {
                    Id = guid.GetEntry(),
                    IsCustom = 0
                };

                createProperties.AreaTriggerId = guid.GetEntry();
                createProperties.IsAreatriggerCustom = areaTriggerTemplate.IsCustom;
                createProperties.Flags = 0;

                if (createProperties.AreaTriggerData.Polygon != null)
                {
                    var verticesList = new List<AreaTriggerCreatePropertiesPolygonVertex>(createProperties.AreaTriggerData.Polygon.Vertices.Count);

                    for (var i = 0; i < createProperties.AreaTriggerData.Polygon.Vertices.Count; ++i)
                    {
                        var vertex = createProperties.AreaTriggerData.Polygon.Vertices[i];
                        if (!vertex.HasValue)
                            continue;

                        verticesList.Add(new AreaTriggerCreatePropertiesPolygonVertex
                        {
                            areatriggerGuid = guid,
                            Idx = (uint)i,
                            VerticeX = vertex.Value.X,
                            VerticeY = vertex.Value.Y
                        });
                    }

                    for (var i = 0; i < createProperties.AreaTriggerData.Polygon.VerticesTarget.Count; ++i)
                    {
                        var vertexTarget = createProperties.AreaTriggerData.Polygon.VerticesTarget[i];
                        if (!vertexTarget.HasValue)
                            continue;

                        verticesList[i].VerticeTargetX = vertexTarget.Value.X;
                        verticesList[i].VerticeTargetY = vertexTarget.Value.Y;
                    }

                    foreach (var vertice in verticesList)
                        Storage.AreaTriggerCreatePropertiesPolygonVertices.Add(vertice);
                }

                if (createProperties.AreaTriggerData.Spline != null)
                    AreaTriggerHandler.ProcessAreaTriggerSpline(createProperties, createProperties.AreaTriggerData, packet, index);
                else if (createProperties.AreaTriggerData.Orbit != null)
                    AreaTriggerHandler.ProcessAreaTriggerOrbit(createProperties, createProperties.AreaTriggerData, packet, index);

                Storage.AreaTriggerTemplates.Add(areaTriggerTemplate);
            }

            // If this is the second time we see the same object (same guid,
            // same position) update its phasemask
            if (Storage.Objects.ContainsKey(guid))
            {
                var existObj = Storage.Objects[guid].Item1;
                CoreParsers.UpdateHandler.ProcessExistingObject(ref existObj, obj, guid); // can't do "ref Storage.Objects[guid].Item1 directly
            }
            else
                Storage.Objects.Add(guid, obj, packet.TimeSpan);

            if (guid.HasEntry() && (objType == ObjectType.Unit || objType == ObjectType.GameObject))
                packet.AddSniffData(Utilities.ObjectTypeToStore(objType), (int)guid.GetEntry(), "SPAWN");
        }

        public static MovementUpdateTransport ReadTransportData(MovementInfo moveInfo, WowGuid guid, Packet packet, object index)
        {
            moveInfo.Transport = new MovementInfo.TransportInfo();
            MovementUpdateTransport transport = new();
            packet.ResetBitReader();
            transport.TransportGuid = moveInfo.Transport.Guid = packet.ReadPackedGuid128("TransportGUID", index);
            transport.Position = moveInfo.Transport.Offset = packet.ReadVector4("TransportPosition", index);
            var seat = packet.ReadByte("VehicleSeatIndex", index);
            transport.Seat = seat;
            transport.MoveTime = packet.ReadUInt32("MoveTime", index);

            var hasPrevMoveTime = packet.ReadBit("HasPrevMoveTime", index);
            var hasVehicleRecID = packet.ReadBit("HasVehicleRecID", index);

            if (hasPrevMoveTime)
                transport.PrevMoveTime = packet.ReadUInt32("PrevMoveTime", index);

            if (hasVehicleRecID)
                transport.VehicleId = packet.ReadInt32("VehicleRecID", index);

            if (moveInfo.Transport.Guid.HasEntry() && moveInfo.Transport.Guid.GetHighType() == HighGuidType.Vehicle &&
                guid.HasEntry() && guid.GetHighType() == HighGuidType.Creature)
            {
                VehicleTemplateAccessory vehicleAccessory = new VehicleTemplateAccessory
                {
                    Entry = moveInfo.Transport.Guid.GetEntry(),
                    AccessoryEntry = guid.GetEntry(),
                    SeatId = seat
                };
                Storage.VehicleTemplateAccessories.Add(vehicleAccessory, packet.TimeSpan);
            }

            return transport;
        }

        private static MovementInfo ReadMovementUpdateBlock(Packet packet, CreateObject createObject, WowGuid guid, WoWObject obj, object index)
        {
            var moveInfo = new MovementInfo();

            packet.ResetBitReader();

            // 70959: kolejnosc i nazwy bitow potwierdzone na 267 pakietach create.
            // Maski z zywego sniffu: 0x84 = HasEntityPosition+MovementUpdate (Unit),
            // 0x8C = to samo + ThisIsYou (gracz), 0x81 = HasEntityPosition+Stationary
            // (GameObject/AreaTrigger). Bity MSB first, 21 bitow, potem flush do bajtu.
            var hasEntityPosition = packet.ReadBit("HasEntityPosition", index);
            moveInfo.NoBirthAnim = packet.ReadBit("NoBirthAnim", index);
            packet.ReadBit("EnablePortals", index);
            moveInfo.PlayHoverAnim = packet.ReadBit("PlayHoverAnim", index);
            packet.ReadBit("ThisIsYou", index); // MoveInfo nie ma tego pola - tylko w bitach bloku

            var hasMovementUpdate = packet.ReadBit("MovementUpdate", index);
            var hasMovementTransport = packet.ReadBit("MovementTransport", index);
            var hasStationaryPosition = packet.ReadBit("Stationary", index);
            var hasCombatVictim = packet.ReadBit("HasCombatVictim", index);
            var hasServerTime = packet.ReadBit("HasServerTime", index);
            var hasVehicleCreate = packet.ReadBit("HasVehicleCreate", index);
            var hasAnimKitCreate = packet.ReadBit("HasAnimKitCreate", index);
            var hasRotation = packet.ReadBit("HasRotation", index);
            var hasGameObject = packet.ReadBit("HasGameObject", index);
            var hasSmoothPhasing = packet.ReadBit("HasSmoothPhasing", index);
            var sceneObjCreate = packet.ReadBit("SceneObjCreate", index);
            var playerCreateData = packet.ReadBit("HasPlayerCreateData", index);
            var hasConversation = packet.ReadBit("HasConversation", index);
            var hasRoom = packet.ReadBit("HasRoom", index);
            var hasDecor = packet.ReadBit("HasDecor", index);
            var hasMeshObject = packet.ReadBit("HasMeshObject", index);

            // 21 bitow konczy sie w polowie bajtu - reszta bloku jest bajtowa,
            // wiec trzeba doskoczyc do nastepnego pelnego bajtu.
            packet.ResetBitReader();

            var pauseTimesCount = packet.ReadUInt32("PauseTimesCount", index);

            if (hasMovementUpdate)
            {
                var movementUpdate = createObject.Movement = new();
                packet.ResetBitReader();
                movementUpdate.Mover = moveInfo.MoverGuid = packet.ReadPackedGuid128("MoverGUID", index);

                moveInfo.Flags64 = (ulong)packet.ReadUInt64E<MovementFlag>("MovementFlags", index);

                movementUpdate.MoveTime = packet.ReadUInt32("MoveTime", index);
                movementUpdate.Position = moveInfo.Position = packet.ReadVector3("Position", index);
                movementUpdate.Orientation = moveInfo.Orientation = packet.ReadSingle("Orientation", index);

                movementUpdate.Pitch = packet.ReadSingle("Pitch", index);
                movementUpdate.StepUpStartElevation = packet.ReadSingle("StepUpStartElevation", index);

                var removeForcesIDsCount = packet.ReadInt32();
                movementUpdate.MoveIndex = packet.ReadInt32("MoveIndex", index);

                packet.ReadSingle("GravityModifier", index);

                for (var i = 0; i < removeForcesIDsCount; i++)
                    packet.ReadPackedGuid128("RemoveForcesIDs", index, i);

                var hasStandingOnGameObjectGUID = packet.ReadBit("HasStandingOnGameObjectGUID", index);
                var hasTransport = packet.ReadBit("Has Transport Data", index);
                var hasFall = packet.ReadBit("Has Fall Data", index);
                var hasSplineInBits = packet.ReadBit("HasSpline", index);
                packet.ReadBit("HeightChangeFailed", index);
                packet.ReadBit("RemoteTimeValid", index);
                var hasInertia = packet.ReadBit("HasInertia", index);
                var hasAdvFlying = packet.ReadBit("HasAdvFlying", index);
                var hasDriveStatus = packet.ReadBit("HasDriveStatus", index);

                // 70959: 9 bitow konczy sie w polowie bajtu, a dalej ida floaty,
                // wiec trzeba doskoczyc do pelnego bajtu. Zmierzone na pkt 5278/5785/6071.
                packet.ResetBitReader();

                if (hasStandingOnGameObjectGUID)
                    packet.ReadPackedGuid128("StandingOnGameObjectGUID", index);

                if (hasTransport)
                    movementUpdate.Transport = ReadTransportData(moveInfo, guid, packet, index);

                if (hasFall)
                {
                    packet.ResetBitReader();
                    movementUpdate.FallTime = packet.ReadUInt32("Fall Time", index);
                    movementUpdate.JumpVelocity = packet.ReadSingle("JumpVelocity", index);

                    var hasFallDirection = packet.ReadBit("Has Fall Direction", index);
                    if (hasFallDirection)
                    {
                        packet.ReadVector2("Fall", index);
                        packet.ReadSingle("Horizontal Speed", index);
                    }
                }

                if (hasInertia)
                {
                    packet.ReadInt32("ID", "Inertia");
                    packet.ReadVector3("Force", index, "Inertia");
                    packet.ReadUInt32("Lifetime", index, "Inertia");
                }

                if (hasAdvFlying)
                {
                    packet.ReadSingle("ForwardVelocity", index, "AdvFlying");
                    packet.ReadSingle("UpVelocity", index, "AdvFlying");
                }

                if (hasDriveStatus)
                {
                    packet.ResetBitReader();
                    packet.ReadSingle("Speed", index, "DriveStatus");
                    packet.ReadSingle("MovementAngle", index, "DriveStatus");
                    packet.ReadBit("Accelerating", index, "DriveStatus");
                    packet.ReadBit("Drifting", index, "DriveStatus");
                }

                movementUpdate.WalkSpeed = moveInfo.WalkSpeed = packet.ReadSingle("WalkSpeed", index) / 2.5f;
                movementUpdate.RunSpeed = moveInfo.RunSpeed = packet.ReadSingle("RunSpeed", index) / 7.0f;
                packet.ReadSingle("RunBackSpeed", index);
                packet.ReadSingle("SwimSpeed", index);
                packet.ReadSingle("SwimBackSpeed", index);
                packet.ReadSingle("FlightSpeed", index);
                packet.ReadSingle("FlightBackSpeed", index);
                packet.ReadSingle("TurnRate", index);
                packet.ReadSingle("PitchRate", index);

                var movementForceCount = packet.ReadUInt32("MovementForceCount", index);
                packet.ReadSingle("MovementForcesModMagnitude", index);

                packet.ReadSingle("AdvFlyingAirFriction", index);
                packet.ReadSingle("AdvFlyingMaxVel", index);
                packet.ReadSingle("AdvFlyingLiftCoefficient", index);
                packet.ReadSingle("AdvFlyingDoubleJumpVelMod", index);
                packet.ReadSingle("AdvFlyingGlideStartMinHeight", index);
                packet.ReadSingle("AdvFlyingAddImpulseMaxSpeed", index);
                packet.ReadSingle("AdvFlyingMinBankingRate", index);
                packet.ReadSingle("AdvFlyingMaxBankingRate", index);
                packet.ReadSingle("AdvFlyingMinPitchingRateDown", index);
                packet.ReadSingle("AdvFlyingMaxPitchingRateDown", index);
                packet.ReadSingle("AdvFlyingMinPitchingRateUp", index);
                packet.ReadSingle("AdvFlyingMaxPitchingRateUp", index);
                packet.ReadSingle("AdvFlyingMinTurnVelocityThreshold", index);
                packet.ReadSingle("AdvFlyingMaxTurnVelocityThreshold", index);
                packet.ReadSingle("AdvFlyingSurfaceFriction", index);
                packet.ReadSingle("AdvFlyingOverMaxDeceleration", index);
                packet.ReadSingle("AdvFlyingLaunchSpeedCoefficient", index);

                packet.ResetBitReader();
                moveInfo.HasSplineData = packet.ReadBit("HasMovementSpline", index);

                for (var i = 0; i < movementForceCount; ++i)
                    V6_0_2_19033.Parsers.MovementHandler.ReadMovementForce(packet, "MovementForces", i);

                if (moveInfo.HasSplineData)
                {
                    var splineData = movementUpdate.SplineData = new();
                    packet.ResetBitReader();
                    splineData.Id = packet.ReadInt32("ID", index);
                    splineData.Destination = packet.ReadVector3("Destination", index);

                    var hasMovementSplineMove = packet.ReadBit("MovementSplineMove", index);
                    if (hasMovementSplineMove)
                    {
                        var moveData = splineData.MoveData = new();
                        packet.ResetBitReader();

                        moveData.Flags = packet.ReadUInt32E<SplineFlag>("SplineFlags", index).ToUniversal();
                        var face = packet.ReadByte("Face", index);
                        moveData.Elapsed = packet.ReadInt32("Elapsed", index);
                        moveData.Duration = packet.ReadUInt32("Duration", index);
                        moveData.DurationModifier = packet.ReadSingle("DurationModifier", index);
                        moveData.NextDurationModifier = packet.ReadSingle("NextDurationModifier", index);

                        switch (face)
                        {
                            case 1:
                                moveData.LookPosition = packet.ReadVector3("FaceSpot", index);
                                break;
                            case 2:
                                moveData.LookTarget = new() { Target = packet.ReadPackedGuid128("FaceGUID", index) };
                                break;
                            case 3:
                                moveData.LookOrientation = packet.ReadSingle("FaceDirection", index);
                                break;
                            default:
                                break;
                        }

                        var hasSpecialTime = packet.ReadBit("HasSpecialTime", index);

                        var pointsCount = packet.ReadBits("PointsCount", 16, index);

                        var hasSplineFilterKey = packet.ReadBit("HasSplineFilterKey", index);
                        var hasSpellEffectExtraData = packet.ReadBit("HasSpellEffectExtraData", index);
                        var hasJumpExtraData = packet.ReadBit("HasJumpExtraData", index);
                        var hasTurnData = packet.ReadBit("HasTurnData", index);
                        var hasAnimationTierTransition = packet.ReadBit("HasAnimationTierTransition", index);
                        var hasSpellVisualData = packet.ReadBit("HasSpellVisualData", index);

                        if (hasSpecialTime)
                            packet.ReadUInt32("SpecialTime", index);

                        if (hasSplineFilterKey)
                        {
                            packet.ResetBitReader();
                            var filterKeysCount = packet.ReadUInt32("FilterKeysCount", index);
                            for (var i = 0; i < filterKeysCount; ++i)
                            {
                                packet.ReadSingle("In", index, i);
                                packet.ReadSingle("Out", index, i);
                            }

                            packet.ReadBits("FilterFlags", 2, index);
                        }

                        for (var i = 0; i < pointsCount; ++i)
                            moveData.Points.Add(packet.ReadVector3("Points", index, i));

                        if (hasSpellEffectExtraData)
                            V8_0_1_27101.Parsers.MovementHandler.ReadMonsterSplineSpellEffectExtraData(packet, index);

                        if (hasJumpExtraData)
                            moveData.Jump = V8_0_1_27101.Parsers.MovementHandler.ReadMonsterSplineJumpExtraData(packet, index);

                        if (hasTurnData)
                            V8_0_1_27101.Parsers.MovementHandler.ReadMonsterSplineTurnData(packet, index, "MonsterSplineTurnData");

                        if (hasAnimationTierTransition)
                        {
                            packet.ReadInt32("TierTransitionID", index);
                            packet.ReadByte("AnimTier", index);
                            packet.ReadInt32("StartTime", index);
                            packet.ReadInt32("EndTime", index);
                        }

                        if (hasSpellVisualData)
                        {
                            for (var i = 0; i < 16; ++i)
                            {
                                packet.ReadInt32("SpellID", index, "SpellVisualData", i);
                                V9_0_1_36216.Parsers.SpellHandler.ReadSpellCastVisual(packet, index, "SpellVisualData", i, "Visual");
                                packet.ReadInt32("StartNodeIndex", index, "SpellVisualData", i);
                            }
                        }
                    }
                }
            }

            if (hasMovementTransport)
                createObject.Transport = ReadTransportData(moveInfo, guid, packet, index);

            if (hasStationaryPosition)
            {
                moveInfo.Position = packet.ReadVector3();
                moveInfo.Orientation = packet.ReadSingle();

                packet.AddValue("Stationary Position", moveInfo.Position, index);
                packet.AddValue("Stationary Orientation", moveInfo.Orientation, index);
                createObject.Stationary = new() { Position = moveInfo.Position, Orientation = moveInfo.Orientation };
            }

            if (hasCombatVictim)
                packet.ReadPackedGuid128("CombatVictim Guid", index);

            if (hasServerTime)
                packet.ReadUInt32("ServerTime", index);

            if (hasVehicleCreate)
            {
                var vehicle = createObject.Vehicle = new();
                moveInfo.VehicleId = (uint)packet.ReadInt32("RecID", index);
                vehicle.VehicleId = (int)moveInfo.VehicleId;
                vehicle.InitialRawFacing = packet.ReadSingle("InitialRawFacing", index);
            }

            if (hasAnimKitCreate)
            {
                var aiId = packet.ReadUInt16("AiID", index);
                var movementId = packet.ReadUInt16("MovementID", index);
                var meleeId = packet.ReadUInt16("MeleeID", index);
                if (obj is Unit unit)
                {
                    unit.AIAnimKit = aiId;
                    unit.MovementAnimKit = movementId;
                    unit.MeleeAnimKit = meleeId;
                }
                else if (obj is GameObject gob)
                {
                    gob.AIAnimKitID = aiId;
                }
            }

            if (hasRotation)
                createObject.Rotation = moveInfo.Rotation = packet.ReadPackedQuaternion("GameObject Rotation", index);

            if (hasGameObject)
            {
                packet.ResetBitReader();
                var worldEffectId = packet.ReadUInt32("WorldEffectID", index);
                if (worldEffectId != 0 && obj is GameObject gob)
                    gob.WorldEffectID = worldEffectId;

                var hasInt1 = packet.ReadBit("bit8", index);
                var hasShipPath = packet.ReadBit("HasShipPath", index);
                var hasTransportStatePercent = packet.ReadBit("HasTransportStatePercent", index);
                if (hasShipPath)
                {
                    packet.ResetBitReader();
                    packet.ReadUInt32("Period", index, "ShipPath");
                    packet.ReadUInt32("Progress", index, "ShipPath");
                    packet.ReadBit("StopRequested", index, "ShipPath");
                    packet.ReadBit("Stopped", index, "ShipPath");
                    packet.ReadBit("Field_16", index, "ShipPath");
                }

                if (hasInt1)
                    packet.ReadUInt32("Int1", index);

                if (hasTransportStatePercent)
                    packet.ReadSingle("TransportStatePercent", index);
            }

            if (hasSmoothPhasing)
            {
                packet.ResetBitReader();
                packet.ReadBit("ReplaceActive", index);
                packet.ReadBit("StopAnimKits", index);

                var replaceObject = packet.ReadBit();
                if (replaceObject)
                    packet.ReadPackedGuid128("ReplaceObject", index);
            }

            if (sceneObjCreate)
            {
                packet.ResetBitReader();

                var hasSceneLocalScriptData = packet.ReadBit("HasSceneLocalScriptData", index);
                var petBattleFullUpdate = packet.ReadBit("HasPetBattleFullUpdate", index);

                if (hasSceneLocalScriptData)
                {
                    packet.ResetBitReader();
                    var dataLength = packet.ReadBits(7);
                    packet.ReadWoWString("Data", dataLength, index);
                }

                if (petBattleFullUpdate)
                    V6_0_2_19033.Parsers.BattlePetHandler.ReadPetBattleFullUpdate(packet, index);
            }

            if (playerCreateData)
            {
                packet.ResetBitReader();
                var hasSceneInstanceIDs = packet.ReadBit("ScenePendingInstances", index);
                var hasRuneState = packet.ReadBit("Runes", index);

                if (hasSceneInstanceIDs)
                {
                    var sceneInstanceIDs = packet.ReadUInt32("SceneInstanceIDsCount");
                    for (var i = 0; i < sceneInstanceIDs; ++i)
                        packet.ReadInt32("SceneInstanceIDs", index, i);
                }

                if (hasRuneState)
                {
                    packet.ReadByte("RechargingRuneMask", index);
                    packet.ReadByte("UsableRuneMask", index);
                    var runeCount = packet.ReadUInt32();
                    for (var i = 0; i < runeCount; ++i)
                        packet.ReadByte("RuneCooldown", index, i);
                }
            }

            if (hasConversation)
            {
                packet.ResetBitReader();
                if (packet.ReadBit("HasTextureKitID", index))
                    (obj as ConversationTemplate).TextureKitId = packet.ReadUInt32("TextureKitID", index);
            }

            if (hasRoom)
                packet.ReadPackedGuid128("HouseGUID", index);

            if (hasDecor)
                packet.ReadPackedGuid128("RoomGUID", index);

            if (hasMeshObject)
            {
                packet.ReadPackedGuid128("AttachParentGUID", index);
                packet.ReadVector3("PositionLocalSpace", index);
                packet.ReadQuaternion("RotationLocalSpace", index);
                packet.ReadSingle("ScaleLocalSpace", index);
                packet.ReadByte("AttachmentFlags", index);
            }

            for (var i = 0; i < pauseTimesCount; ++i)
                packet.ReadUInt32("PauseTimes", index, i);

            return moveInfo;
        }
    }
}
