using System.Collections.Generic;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Store.Objects;
using WowPacketParserModule.V1_60_1_70888.UpdateFields.V1_60_1_70888;
using UpdateFieldsHandler = WowPacketParserModule.V1_60_1_70888.UpdateFields.V1_60_1_70888.UpdateFieldHandler;

namespace WowPacketParserModule.V1_60_1_70888.UpdateFields
{
    /// <summary>
    /// Odczyt bloku wartosci GameObjectData dla Forever 1.60.1, przepisany wprost z tc-forever:
    /// src/server/game/Entities/Object/Updates/UpdateFields.cpp, GameObjectData::WriteUpdate (7576).
    ///
    /// Uklad:
    ///   [28 bitow] maska zmian (klient czyta 28 bitow, retail ma 27 - patrz komentarz w kodzie serwera)
    ///   ... bity pol (licznik StateWorldEffectIDs)   FlushBits
    ///   ... maski dynamicznych list (EnableDoodadSets, WorldEffects)   FlushBits
    ///   ... wartosci pol
    ///   ... bit AssistActionData.has_value()   FlushBits
    ///   ... AssistActionData
    ///
    /// Kod serwera przenumerowuje maske retail -> Classic:
    ///   classicBits = (retail &amp; 0xFFFF) | ((retail &amp; ~0xFFFF) &lt;&lt; 1) | (retail[21] ? 1 &lt;&lt; 16 : 0)
    /// i potem zapisuje pole Level pod changesMask[21] DWA razy (raz na pozycji 16, raz na koncu).
    /// Dlatego tutaj czytamy maske klasyczna wprost i uzywamy numeracji klasycznej.
    /// </summary>
    public static class ForeverGameObjectDataReader
    {
        public static GameObjectData Read(Packet packet, params object[] indexes)
        {
            var data = new GameObjectData();
            var is70938 = ClientVersion.Build == ClientVersionBuild.V1_60_1_70938;
            packet.ResetBitReader();

            var bits = (uint)packet.ReadBits("GameObjectChangesMask", 28);
            bool Changed(int bit) => (bits & (1u << bit)) != 0;

            // --- bity pol ---
            var worldEffectIdsCount = 0;
            if (Changed(0))
            {
                if (Changed(1)) worldEffectIdsCount = (int)packet.ReadBits("StateWorldEffectIDsCount", 32);
                if (is70938)
                {
                    data.StateWorldEffectIDs = new uint?[worldEffectIdsCount];
                    for (var i = 0; i < worldEffectIdsCount; ++i)
                        data.StateWorldEffectIDs[i] = packet.ReadUInt32("StateWorldEffectIDs", indexes, i);
                }
            }
            packet.ResetBitReader();

            // --- maski dynamicznych list ---
            if (Changed(0))
            {
                if (Changed(2)) data.EnableDoodadSets.ReadUpdateMask(packet);
                if (Changed(3)) data.WorldEffects.ReadUpdateMask(packet);
            }
            packet.ResetBitReader();

            // --- wartosci ---
            if (Changed(0))
            {
                for (var i = 0; !is70938 && i < worldEffectIdsCount; ++i)
                    packet.ReadUInt32("StateWorldEffectIDs", indexes, i);

                if (Changed(2))
                    for (var i = 0; i < data.EnableDoodadSets.Count; ++i)
                        if (data.EnableDoodadSets.UpdateMask[i])
                            data.EnableDoodadSets[i] = packet.ReadInt32("EnableDoodadSets", indexes, i);

                if (Changed(3))
                    for (var i = 0; i < data.WorldEffects.Count; ++i)
                        if (data.WorldEffects.UpdateMask[i])
                            data.WorldEffects[i] = packet.ReadInt32("WorldEffects", indexes, i);

                if (Changed(4)) data.DisplayID = packet.ReadInt32("DisplayID", indexes);
                if (Changed(5)) data.SpellVisualID = packet.ReadUInt32("SpellVisualID", indexes);
                if (Changed(6)) data.StateSpellVisualID = packet.ReadUInt32("StateSpellVisualID", indexes);
                if (Changed(7)) data.SpawnTrackingStateAnimID = packet.ReadUInt32("SpawnTrackingStateAnimID", indexes);
                if (Changed(8)) data.SpawnTrackingStateAnimKitID = packet.ReadUInt32("SpawnTrackingStateAnimKitID", indexes);
                if (Changed(9)) data.StateWorldEffectsQuestObjectiveID = packet.ReadUInt32("StateWorldEffectsQuestObjectiveID", indexes);
                if (Changed(10)) data.CreatedBy = packet.ReadPackedGuid128("CreatedBy", indexes);
                if (Changed(11)) data.GuildGUID = packet.ReadPackedGuid128("GuildGUID", indexes);
                if (Changed(12)) data.Flags = packet.ReadUInt32("Flags", indexes);
                if (Changed(13)) data.FlagsB = packet.ReadUInt32("FlagsB", indexes);
                if (Changed(14)) data.ParentRotation = packet.ReadQuaternion("ParentRotation", indexes);
                if (Changed(15)) data.FactionTemplate = packet.ReadInt32("FactionTemplate", indexes);

                // bit 16 = pole Level w kliencie Classic (retail trzymal je na biciu 21)
                if (Changed(16)) data.Level = packet.ReadInt32("Level", indexes);

                if (Changed(17)) data.State = packet.ReadSByte("State", indexes);
                if (Changed(18)) data.TypeID = packet.ReadSByte("TypeID", indexes);
                if (Changed(19)) data.PercentHealth = packet.ReadByte("PercentHealth", indexes);
                if (Changed(20)) data.ArtKit = packet.ReadUInt32("ArtKit", indexes);
                if (Changed(21)) data.CustomParam = packet.ReadUInt32("CustomParam", indexes);
                // serwer zapisuje Level drugi raz pod changesMask[21] (patrz GameObjectData::WriteUpdate) -
                // tylko gdy bit 21 jest zapalony I nie byl juz uzyty jako pole Level.
                if (is70938)
                {
                    if (Changed(22)) data.Level = packet.ReadInt32("LevelLate", indexes);
                if (Changed(23)) data.AnimGroupInstance = packet.ReadUInt32("AnimGroupInstance", indexes);
                if (Changed(24)) data.UiWidgetItemID = packet.ReadUInt32("UiWidgetItemID", indexes);
                if (Changed(25)) data.UiWidgetItemQuality = packet.ReadUInt32("UiWidgetItemQuality", indexes);
                if (Changed(26)) data.UiWidgetItemCount = packet.ReadUInt32("UiWidgetItemCount", indexes);
                }
                else
                {
                if (Changed(22)) data.AnimGroupInstance = packet.ReadUInt32("AnimGroupInstance", indexes);
                if (Changed(23)) data.UiWidgetItemID = packet.ReadUInt32("UiWidgetItemID", indexes);
                if (Changed(24)) data.UiWidgetItemQuality = packet.ReadUInt32("UiWidgetItemQuality", indexes);
                if (Changed(25)) data.UiWidgetItemCount = packet.ReadUInt32("UiWidgetItemCount", indexes);
                }
            }

            var hasAssistActionData = (!is70938 || Changed(0)) && packet.ReadBit("HasAssistActionData", indexes);
            packet.ResetBitReader();

            if (Changed(0) && Changed(is70938 ? 27 : 26) && hasAssistActionData)
                data.AssistActionData = UpdateFieldsHandler.ReadUpdateGameObjectAssistActionData(packet, indexes, "AssistActionData");

            return data;
        }
    }
}
