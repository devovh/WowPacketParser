using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_QUERY_CREATURE_RESPONSE (0x4B0006) dla Forever 1.60.1.
    ///
    /// Uklad z tc-forever (Server/Packets/QueryPackets.cpp:75 QueryCreatureResponse::Write):
    ///
    ///   [bits 6: liczba wpisow][FlushBits]
    ///   per wpis:
    ///     [u32 CreatureID][u8 result]     0 = dane sa, 1 = brak danych
    ///     jesli result == 0:
    ///       [bits 11 Title][bits 11 TitleAlt][bits 6 CursorName][bit Leader]
    ///       [4 x (bits 11 Name[i], bits 11 NameAlt[i])]
    ///       [teksty Name/NameAlt][u32 TypeFlags x 3][u8 CreatureType][i32 Family]
    ///       [i8 Classification][2 x i32 ProxyCreatureID]
    ///       [u32 DisplayCount][f32 TotalProbability][per display: u32 id, f32 scale, f32 prob]
    ///       [f32 HpMulti][f32 EnergyMulti][u32 QuestItems][u32 QuestCurrencies]
    ///       [8 x i32: MovementInfoID, HealthScalingExpansion, RequiredExpansion, VignetteID,
    ///                 Class, CreatureDifficultyID, WidgetSetID, WidgetSetUnitConditionID]
    ///       [Title][TitleAlt][CursorName][QuestItems][QuestCurrencies]
    ///
    /// Fallback (9.x) czyta od razu u32 jako "Entry" i bit HasData, przez co dla pakietu
    /// 6-bajtowego (77 sztuk w snifie) zostaje 1 bajt, a dla pakietow z danymi (120-163 B)
    /// zostaje 115-158 bajtow.
    ///
    /// 6 B: 04 | 1F 34 04 00 | 01  ->  count=1, CreatureID=275487, result=1 (brak danych)
    ///
    /// Wynik zapisywany jest do Storage.CreatureTemplatesForever (schemat TDB Forever),
    /// Storage.CreatureTemplateDifficultiesForever (TypeFlags/HealthModifier/ManaModifier/
    /// CreatureDifficultyID/HealthScalingExpansion) oraz Storage.CreatureTemplateModels.
    /// </summary>
    public static class ForeverQueryCreatureHandler
    {
        [Parser(Opcode.SMSG_QUERY_CREATURE_RESPONSE)]
        public static void HandleCreatureQueryResponse(Packet packet)
        {
            packet.ResetBitReader();
            var count = packet.ReadBits("Count", 6);
            packet.ResetBitReader();

            for (var e = 0; e < count; ++e)
            {
                var entry = packet.ReadInt32("Entry", e);
                var result = packet.ReadByte("Result", e);

                if (result != 0)
                    continue;

                ReadCreatureStats(packet, (uint)entry, e);
            }
        }

        private static void ReadCreatureStats(Packet packet, uint entry, int e)
        {
            packet.ResetBitReader();
            var titleLen = packet.ReadBits("TitleLength", 11, e);
            var titleAltLen = packet.ReadBits("TitleAltLength", 11, e);
            var cursorNameLen = packet.ReadBits("CursorNameLength", 6, e);
            var leader = packet.ReadBit("Leader", e);

            var nameLens = new int[4];
            var nameAltLens = new int[4];
            for (var i = 0; i < 4; ++i)
            {
                nameLens[i] = (int)packet.ReadBits("NameLength", 11, e, i);
                nameAltLens[i] = (int)packet.ReadBits("NameAltLength", 11, e, i);
            }

            var names = new string[4];
            var namesAlt = new string[4];
            for (var i = 0; i < 4; ++i)
            {
                if (nameLens[i] > 1)
                    names[i] = packet.ReadCString("Name", e, i);
                if (nameAltLens[i] > 1)
                    namesAlt[i] = packet.ReadCString("NameAlt", e, i);
            }

            var isForever70938 = ClientVersion.Build >= ClientVersionBuild.V1_60_1_70938;
            var typeFlagsCount = isForever70938 ? 3 : 4;
            var typeFlags = new uint[typeFlagsCount];
            for (var i = 0; i < typeFlagsCount; ++i)
                typeFlags[i] = packet.ReadUInt32("TypeFlags", e, i);

            var creatureType = packet.ReadByteE<CreatureType>("CreatureType", e);
            var creatureFamily = packet.ReadInt32E<CreatureFamily>("CreatureFamily", e);
            var classification = packet.ReadSByteE<CreatureRank>("Classification", e);

            var killCredits = new uint?[2];
            for (var i = 0; i < 2; ++i)
                killCredits[i] = (uint)packet.ReadInt32("ProxyCreatureID", e, i);

            var displayCount = packet.ReadUInt32("DisplayIdCount", e);
            packet.ReadSingle("TotalProbability", e);

            for (var i = 0; i < displayCount; ++i)
            {
                var displayId = packet.ReadUInt32("CreatureDisplayID", e, i);
                var scale = packet.ReadSingle("DisplayScale", e, i);
                var probability = packet.ReadSingle("Probability", e, i);

                Storage.CreatureTemplateModels.Add(new CreatureTemplateModel
                {
                    CreatureID = entry,
                    Idx = (uint)i,
                    CreatureDisplayID = displayId,
                    DisplayScale = scale,
                    Probability = probability
                }, packet.TimeSpan);
            }

            var hpMulti = packet.ReadSingle("HpMulti", e);
            var energyMulti = packet.ReadSingle("EnergyMulti", e);
            var questItemsCount = packet.ReadUInt32("QuestItems", e);
            var questCurrenciesCount = packet.ReadUInt32("QuestCurrencies", e);

            var movementInfoId = (uint)packet.ReadInt32("CreatureMovementInfoID", e);
            var healthScalingExpansion = packet.ReadInt32E<ClientType>("HealthScalingExpansion", e);
            var requiredExpansion = packet.ReadInt32E<ClientType>("RequiredExpansion", e);
            var vignetteId = (uint)packet.ReadInt32("VignetteID", e);
            var unitClass = (uint)packet.ReadInt32E<Class>("UnitClass", e);
            var creatureDifficultyId = packet.ReadInt32("CreatureDifficultyID", e);
            var widgetSetId = packet.ReadInt32("WidgetSetID", e);
            var widgetSetUnitConditionId = packet.ReadInt32("WidgetSetUnitConditionID", e);

            var title = titleLen > 1 ? packet.ReadCString("Title", e) : null;
            var titleAlt = titleAltLen > 1 ? packet.ReadCString("TitleAlt", e) : null;
            var cursorName = cursorNameLen > 1 ? packet.ReadCString("CursorName", e) : null;

            for (var i = 0; i < questItemsCount; ++i)
            {
                Storage.CreatureTemplateQuestItemsForever.Add(new CreatureTemplateQuestItemForever
                {
                    CreatureEntry = entry,
                    Idx = (uint)i,
                    ItemId = (uint)packet.ReadInt32<ItemId>("QuestItemID", e, i)
                }, packet.TimeSpan);
            }

            for (var i = 0; i < questCurrenciesCount; ++i)
            {
                Storage.CreatureTemplateQuestCurrenciesForever.Add(new CreatureTemplateQuestCurrencyForever
                {
                    CreatureId = entry,
                    CurrencyId = packet.ReadInt32("QuestCurrencyID", e, i)
                }, packet.TimeSpan);
            }

            // The world database requires these text columns, and the client may send an empty
            // string for them (the lengths are read as "> 1", so an empty one becomes null).
            // An empty string is the database default, so keep the row importable.
            title ??= string.Empty;
            titleAlt ??= string.Empty;
            cursorName ??= string.Empty;

            // The client sends no female name at all; the database column is NOT NULL there.
            var femaleName = namesAlt[0] ?? string.Empty;

            Storage.CreatureTemplatesForever.Add(entry, new CreatureTemplateForever
            {
                Entry = entry,
                KillCredit1 = killCredits[0],
                KillCredit2 = killCredits[1],
                Name = names[0],
                FemaleName = femaleName,
                SubName = title,
                TitleAlt = titleAlt,
                IconName = cursorName,
                RequiredExpansion = (int?)requiredExpansion,
                VignetteID = vignetteId,
                Classification = classification,
                UnitClass = unitClass,
                Family = creatureFamily,
                Type = creatureType,
                RacialLeader = leader,
                MovementID = movementInfoId,
                WidgetSetID = widgetSetId,
                WidgetSetUnitConditionID = widgetSetUnitConditionId,
                TypeFlags = typeFlagsCount > 0 ? typeFlags[0] : null,
                TypeFlags2 = typeFlagsCount > 1 ? typeFlags[1] : null,
                TypeFlags3 = typeFlagsCount > 2 ? typeFlags[2] : null
            }, packet.TimeSpan);

            Storage.CreatureTemplateDifficultiesForever.Add(entry, new CreatureTemplateDifficultyForever
            {
                Entry = entry,
                DifficultyID = 0,
                HealthScalingExpansion = healthScalingExpansion,
                HealthModifier = hpMulti,
                ManaModifier = energyMulti,
                CreatureDifficultyID = creatureDifficultyId,
                TypeFlags = typeFlagsCount > 0 ? typeFlags[0] : null,
                TypeFlags2 = typeFlagsCount > 1 ? typeFlags[1] : null,
                TypeFlags3 = typeFlagsCount > 2 ? typeFlags[2] : null
            }, packet.TimeSpan);

            Storage.ObjectNames.Add(new ObjectName
            {
                ObjectType = StoreNameType.Unit,
                ID = (int)entry,
                Name = names[0]
            }, packet.TimeSpan);

            packet.AddSniffData(StoreNameType.Unit, (int)entry, "QUERY_RESPONSE");
        }
    }
}
