using System;
using System.Collections.Generic;
using System.Linq;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Store.Objects;
using WowPacketParserModule.V1_60_1_70888.UpdateFields.V1_60_1_70888;
using UpdateFieldsHandler = WowPacketParserModule.V1_60_1_70888.UpdateFields.V1_60_1_70888.UpdateFieldHandler;

namespace WowPacketParserModule.V1_60_1_70888.UpdateFields
{
    /// <summary>
    /// Odczyt bloku wartosci UnitData dla Forever 1.60.1 (70338-70938), przepisany wprost
    /// z tc-forever: src/server/game/Entities/Object/Updates/UpdateFields.cpp,
    /// UnitData::WriteUpdate (linie 1405-2125).
    ///
    /// Uklad:
    ///   [8 bitow] ktore z 8 blokow maski sa zapisane
    ///   [tylko zapisane bloki, po 32 bity]
    ///   [1 bit]  IsBuild70291OrLater -> dla 70938 zawsze jest
    ///   ... bity pol (Field_314, liczniki, maski dynamicznych list)
    ///   FlushBits
    ///   ... maski dynamicznych list
    ///   FlushBits
    ///   ... wartosci pol w kolejnosci changesMask[n]
    ///   ... bit AssistActionData.has_value() jesli classicGroup128
    ///   FlushBits
    ///   ... dane AssistActionData, potem tablice (Power, VirtualItems, Stats...)
    ///
    /// Numery bitow w maskach sa klasyczne (klient), dlatego remapujemy je na numeracje
    /// TC (retail), ktora rozumie reszta parsera - patrz ForeverClassicBitRemap.
    /// </summary>
    public static class ForeverUnitDataReader
    {
        private const int BlockCount = 8;
        private const uint TcBitCount = 256;

        public static UnitData Read(Packet packet, params object[] indexes)
        {
            var data = new UnitData();
            packet.ResetBitReader();

            // --- 1. naglowek maski zmian ---
            var blocksMask = packet.ReadBits("UnitBlocksMask", BlockCount);
            var classicBlocks = new ulong[4];
            for (var i = 0; i < BlockCount; ++i)
                if ((blocksMask & (1UL << i)) != 0)
                    classicBlocks[i / 2] |= (ulong)packet.ReadUInt32("UnitBlock", indexes, i) << (32 * (i % 2));

            // Maska w numeracji TC: ta sama informacja, ale pod numerami pol, ktore czyta kod 12.x.
            var tcBlocks = ForeverClassicBitRemap.ToRetailBits(classicBlocks, TcBitCount,
                ForeverClassicBitRemap.UnitData, ForeverClassicBitRemap.UnitDataGroups70425);

            bool Changed(int bit) => (tcBlocks[bit >> 6] & (1UL << (bit & 63))) != 0;

            // klasyczne bity grupowe - decyduja o dodatkowych bitach
            bool ClassicBit(uint classicBit)
            {
                if ((classicBit >> 6) >= (uint)classicBlocks.Length) return false;
                return (classicBlocks[classicBit >> 6] & (1UL << (int)(classicBit & 63))) != 0;
            }

            // Classic group 128 = bit 128 bloku 4 (u nas blok 4 -> i / 2 = 2, bit 0)
            var classicGroup128 = (classicBlocks[2] & 1UL) != 0;

            // --- 2. bit 70291 (dla 70938 zawsze wystepuje) ---
            packet.ReadBit("Unknown70291", indexes);

            // --- 3. bity pol ---
            var worldEffectIdsCount = 0;
            if (Changed(0))
            {
                if (Changed(1)) data.Field_314 = packet.ReadBit("Field_314", indexes);
                if (Changed(2)) worldEffectIdsCount = (int)packet.ReadBits("StateWorldEffectIDsCount", 32);
            }
            packet.ResetBitReader();

            // --- 4. maski dynamicznych list ---
            if (Changed(0))
            {
                if (Changed(3)) data.PassiveSpells.ReadUpdateMask(packet);
                if (Changed(4)) data.WorldEffects.ReadUpdateMask(packet);
                if (Changed(5)) data.ChannelObjects.ReadUpdateMask(packet);
            }
            packet.ResetBitReader();

            // --- 5. wartosci ---
            if (Changed(0))
            {
                for (var i = 0; i < worldEffectIdsCount; ++i)
                    packet.ReadUInt32("StateWorldEffectIDs", indexes, i);

                if (Changed(3))
                    for (var i = 0; i < data.PassiveSpells.Count; ++i)
                        if (data.PassiveSpells.UpdateMask[i])
                            data.PassiveSpells[i] = UpdateFieldsHandler.ReadUpdatePassiveSpellHistory(packet, indexes, "PassiveSpells", i);

                if (Changed(4))
                    for (var i = 0; i < data.WorldEffects.Count; ++i)
                        if (data.WorldEffects.UpdateMask[i])
                            data.WorldEffects[i] = packet.ReadInt32("WorldEffects", indexes, i);

                if (Changed(5))
                    for (var i = 0; i < data.ChannelObjects.Count; ++i)
                        if (data.ChannelObjects.UpdateMask[i])
                            data.ChannelObjects[i] = packet.ReadPackedGuid128("ChannelObjects", indexes, i);

                if (Changed(6)) data.DisplayID = packet.ReadInt32("DisplayID", indexes);
                if (Changed(7)) data.NpcFlags = packet.ReadUInt32("NpcFlags", indexes);
                if (Changed(8)) data.NpcFlags2 = packet.ReadUInt32("NpcFlags2", indexes);
                if (Changed(9)) data.StateSpellVisualID = packet.ReadUInt32("StateSpellVisualID", indexes);
                if (Changed(10)) data.StateAnimID = packet.ReadUInt32("StateAnimID", indexes);
                if (Changed(11)) data.StateAnimKitID = packet.ReadUInt32("StateAnimKitID", indexes);
                if (Changed(12)) data.StateWorldEffectsQuestObjectiveID = packet.ReadUInt32("StateWorldEffectsQuestObjectiveID", indexes);
                if (Changed(13)) data.SpellOverrideNameID = packet.ReadInt32("SpellOverrideNameID", indexes);
                if (Changed(14)) data.Charm = packet.ReadPackedGuid128("Charm", indexes);
                if (Changed(15)) data.Summon = packet.ReadPackedGuid128("Summon", indexes);
                if (Changed(16)) data.Critter = packet.ReadPackedGuid128("Critter", indexes);
                if (Changed(17)) data.CharmedBy = packet.ReadPackedGuid128("CharmedBy", indexes);
                if (Changed(18)) data.SummonedBy = packet.ReadPackedGuid128("SummonedBy", indexes);
                if (Changed(19)) data.CreatedBy = packet.ReadPackedGuid128("CreatedBy", indexes);
                if (Changed(20)) data.DemonCreator = packet.ReadPackedGuid128("DemonCreator", indexes);
                if (Changed(21)) data.LookAtControllerTarget = packet.ReadPackedGuid128("LookAtControllerTarget", indexes);
                if (Changed(22)) data.Target = packet.ReadPackedGuid128("Target", indexes);
                if (Changed(23)) data.BattlePetCompanionGUID = packet.ReadPackedGuid128("BattlePetCompanionGUID", indexes);
                if (Changed(24)) data.BattlePetDBID = packet.ReadUInt64("BattlePetDBID", indexes);
                if (Changed(25)) data.BattlePetAttachedToDecorGUID = packet.ReadPackedGuid128("BattlePetAttachedToDecorGUID", indexes);
                if (Changed(26)) data.BattlePetDecorHouseGUID = packet.ReadPackedGuid128("BattlePetDecorHouseGUID", indexes);
                if (Changed(27))
                    data.ChannelData = UpdateFieldsHandler.ReadCreateUnitChannel(packet, indexes, "ChannelData");
                if (Changed(28)) data.SpellEmpowerStage = packet.ReadSByte("SpellEmpowerStage", indexes);
                if (Changed(29)) data.SummonedByHomeRealm = packet.ReadUInt32("SummonedByHomeRealm", indexes);
                if (Changed(30)) data.Race = packet.ReadByte("Race", indexes);
                if (Changed(31)) data.ClassId = packet.ReadByte("ClassId", indexes);
            }

            if (Changed(32))
            {
                if (Changed(33)) data.PlayerClassId = packet.ReadByte("PlayerClassId", indexes);
                if (Changed(34)) data.Sex = packet.ReadByte("Sex", indexes);
                if (Changed(35)) data.CreatureType = packet.ReadByte("CreatureType", indexes);
                if (Changed(36)) data.DisplayPower = packet.ReadByte("DisplayPower", indexes);
                if (Changed(37)) data.OverrideDisplayPowerID = packet.ReadUInt32("OverrideDisplayPowerID", indexes);
                if (Changed(38)) data.Health = packet.ReadInt64("Health", indexes);
                if (Changed(39)) data.MaxHealth = packet.ReadInt64("MaxHealth", indexes);
                if (Changed(40)) data.Level = packet.ReadInt32("Level", indexes);
                if (Changed(41)) data.EffectiveLevel = packet.ReadInt32("EffectiveLevel", indexes);
                if (Changed(42)) data.ContentTuningID = packet.ReadInt32("ContentTuningID", indexes);
                if (Changed(43)) data.ScalingLevelMin = packet.ReadInt32("ScalingLevelMin", indexes);
                if (Changed(44)) data.ScalingLevelMax = packet.ReadInt32("ScalingLevelMax", indexes);
                if (Changed(45)) data.ScalingLevelDelta = packet.ReadInt32("ScalingLevelDelta", indexes);
                if (Changed(46)) data.ScalingFactionGroup = packet.ReadByte("ScalingFactionGroup", indexes);
                if (Changed(47)) data.FactionTemplate = packet.ReadInt32("FactionTemplate", indexes);
                if (Changed(48)) data.Flags = packet.ReadUInt32("Flags", indexes);
                if (Changed(49)) data.Flags2 = packet.ReadUInt32("Flags2", indexes);
                if (Changed(50)) data.Flags3 = packet.ReadUInt32("Flags3", indexes);
                if (Changed(51)) data.Flags4 = packet.ReadUInt32("Flags4", indexes);
                if (Changed(52)) data.AuraState = packet.ReadUInt32("AuraState", indexes);
                if (Changed(53)) data.RangedAttackRoundBaseTime = packet.ReadUInt32("RangedAttackRoundBaseTime", indexes);
                if (Changed(54)) data.BoundingRadius = packet.ReadSingle("BoundingRadius", indexes);
                if (Changed(55)) data.CombatReach = packet.ReadSingle("CombatReach", indexes);
                if (Changed(56)) data.DisplayScale = packet.ReadSingle("DisplayScale", indexes);
                if (Changed(57)) data.CreatureFamily = packet.ReadInt32("CreatureFamily", indexes);
                if (Changed(58)) data.OverrideCreatureType = packet.ReadByte("OverrideCreatureType", indexes);
                if (Changed(59)) data.NativeDisplayID = packet.ReadInt32("NativeDisplayID", indexes);
                if (Changed(60)) data.NativeXDisplayScale = packet.ReadSingle("NativeXDisplayScale", indexes);
                if (Changed(61)) data.MountDisplayID = packet.ReadInt32("MountDisplayID", indexes);
                if (Changed(62)) data.CosmeticMountDisplayID = packet.ReadInt32("CosmeticMountDisplayID", indexes);
                if (Changed(63)) data.MinDamage = packet.ReadSingle("MinDamage", indexes);
            }

            if (Changed(64))
            {
                if (Changed(65)) data.MaxDamage = packet.ReadSingle("MaxDamage", indexes);
                if (Changed(66)) data.MinOffHandDamage = packet.ReadSingle("MinOffHandDamage", indexes);
                if (Changed(67)) data.MaxOffHandDamage = packet.ReadSingle("MaxOffHandDamage", indexes);
                if (Changed(68)) data.StandState = packet.ReadByte("StandState", indexes);
                if (Changed(69)) data.PetTalentPoints = packet.ReadByte("PetTalentPoints", indexes);
                // Classic zamienia 70 i 71: VisFlags <-> AnimTier
                if (Changed(70)) data.VisFlags = packet.ReadByte("VisFlags", indexes);
                if (Changed(71)) data.AnimTier = packet.ReadByte("AnimTier", indexes);
                if (Changed(72)) data.PetNumber = packet.ReadUInt32("PetNumber", indexes);
                if (Changed(73)) data.PetNameTimestamp = packet.ReadUInt32("PetNameTimestamp", indexes);
                if (Changed(74)) data.PetExperience = packet.ReadUInt32("PetExperience", indexes);
                if (Changed(75)) data.PetNextLevelExperience = packet.ReadUInt32("PetNextLevelExperience", indexes);
                if (Changed(76)) data.ModCastingSpeed = packet.ReadSingle("ModCastingSpeed", indexes);
                if (Changed(77)) data.ModCastingSpeedNeg = packet.ReadSingle("ModCastingSpeedNeg", indexes);
                if (Changed(78)) data.ModSpellHaste = packet.ReadSingle("ModSpellHaste", indexes);
                if (Changed(79)) data.ModHaste = packet.ReadSingle("ModHaste", indexes);
                if (Changed(80)) data.ModRangedHaste = packet.ReadSingle("ModRangedHaste", indexes);
                if (Changed(81)) data.ModHasteRegen = packet.ReadSingle("ModHasteRegen", indexes);
                if (Changed(82)) data.ModTimeRate = packet.ReadSingle("ModTimeRate", indexes);
                if (Changed(83)) data.CreatedBySpell = packet.ReadInt32("CreatedBySpell", indexes);
                if (Changed(84)) data.EmoteState = packet.ReadInt32("EmoteState", indexes);
                if (Changed(231)) packet.ReadInt16("ClassicTrainingPointsUsed", indexes);
                if (Changed(232)) packet.ReadInt16("ClassicTrainingPointsTotal", indexes);
                if (Changed(85)) data.BaseMana = packet.ReadInt32("BaseMana", indexes);
                if (Changed(86)) data.BaseHealth = packet.ReadInt32("BaseHealth", indexes);
                if (Changed(87)) data.SheatheState = packet.ReadByte("SheatheState", indexes);
                if (Changed(88)) data.PvpFlags = packet.ReadByte("PvpFlags", indexes);
                if (Changed(89)) data.PetFlags = packet.ReadByte("PetFlags", indexes);
                if (Changed(90)) data.ShapeshiftForm = packet.ReadByte("ShapeshiftForm", indexes);
                if (Changed(91)) data.AttackPower = packet.ReadInt32("AttackPower", indexes);
                if (Changed(92)) data.AttackPowerModPos = packet.ReadInt32("AttackPowerModPos", indexes);
                if (Changed(93)) data.AttackPowerModNeg = packet.ReadInt32("AttackPowerModNeg", indexes);
                if (Changed(94)) data.AttackPowerMultiplier = packet.ReadSingle("AttackPowerMultiplier", indexes);
                if (Changed(95)) data.AttackPowerModSupport = packet.ReadInt32("AttackPowerModSupport", indexes);
            }

            if (Changed(96))
            {
                if (Changed(97)) data.RangedAttackPower = packet.ReadInt32("RangedAttackPower", indexes);
                if (Changed(98)) data.RangedAttackPowerModPos = packet.ReadInt32("RangedAttackPowerModPos", indexes);
                if (Changed(99)) data.RangedAttackPowerModNeg = packet.ReadInt32("RangedAttackPowerModNeg", indexes);
                if (Changed(100)) data.RangedAttackPowerMultiplier = packet.ReadSingle("RangedAttackPowerMultiplier", indexes);
                if (Changed(101)) data.RangedAttackPowerModSupport = packet.ReadInt32("RangedAttackPowerModSupport", indexes);
                if (Changed(102)) data.MainHandWeaponAttackPower = packet.ReadInt32("MainHandWeaponAttackPower", indexes);
                if (Changed(103)) data.OffHandWeaponAttackPower = packet.ReadInt32("OffHandWeaponAttackPower", indexes);
                if (Changed(104)) data.RangedWeaponAttackPower = packet.ReadInt32("RangedWeaponAttackPower", indexes);
                if (Changed(105)) data.SetAttackSpeedAura = packet.ReadInt32("SetAttackSpeedAura", indexes);
                if (Changed(106)) data.Lifesteal = packet.ReadSingle("Lifesteal", indexes);
                if (Changed(107)) data.MinRangedDamage = packet.ReadSingle("MinRangedDamage", indexes);
                if (Changed(108)) data.MaxRangedDamage = packet.ReadSingle("MaxRangedDamage", indexes);
                if (Changed(109)) data.ManaCostMultiplier = packet.ReadSingle("ManaCostMultiplier", indexes);
                if (Changed(110)) data.MaxHealthModifier = packet.ReadSingle("MaxHealthModifier", indexes);
                if (Changed(111)) data.HoverHeight = packet.ReadSingle("HoverHeight", indexes);
                if (Changed(112)) data.MinItemLevelCutoff = packet.ReadInt32("MinItemLevelCutoff", indexes);
                if (Changed(113)) data.MinItemLevel = packet.ReadInt32("MinItemLevel", indexes);
                if (Changed(114)) data.MaxItemLevel = packet.ReadInt32("MaxItemLevel", indexes);
                if (Changed(115)) data.AzeriteItemLevel = packet.ReadInt32("AzeriteItemLevel", indexes);
                if (Changed(116)) data.WildBattlePetLevel = packet.ReadInt32("WildBattlePetLevel", indexes);
                if (Changed(117)) data.BattlePetCompanionExperience = packet.ReadInt32("BattlePetCompanionExperience", indexes);
                if (Changed(118)) data.BattlePetCompanionNameTimestamp = packet.ReadUInt32("BattlePetCompanionNameTimestamp", indexes);
                if (Changed(119)) data.InteractSpellID = packet.ReadInt32("InteractSpellID", indexes);
                if (Changed(120)) data.ScaleDuration = packet.ReadInt32("ScaleDuration", indexes);
                if (Changed(121)) data.LooksLikeMountID = packet.ReadInt32("LooksLikeMountID", indexes);
                if (Changed(122)) data.LooksLikeCreatureID = packet.ReadInt32("LooksLikeCreatureID", indexes);
                if (Changed(123)) data.LookAtControllerID = packet.ReadInt32("LookAtControllerID", indexes);
                if (Changed(124)) data.PerksVendorItemID = packet.ReadInt32("PerksVendorItemID", indexes);
                if (Changed(125)) data.TaxiNodesID = packet.ReadInt32("TaxiNodesID", indexes);
                if (Changed(126)) data.GuildGUID = packet.ReadPackedGuid128("GuildGUID", indexes);
                if (Changed(127)) data.FlightCapabilityID = packet.ReadInt32("FlightCapabilityID", indexes);
            }

            var hasAssistActionData = false;
            if (Changed(128))
            {
                if (Changed(129)) data.GlideEventSpeedDivisor = packet.ReadSingle("GlideEventSpeedDivisor", indexes);
                if (Changed(130)) data.DriveCapabilityID = packet.ReadInt32("DriveCapabilityID", indexes);
                if (Changed(131)) data.MaxHealthModifierFlatNeg = packet.ReadInt32("MaxHealthModifierFlatNeg", indexes);
                if (Changed(132)) data.MaxHealthModifierFlatPos = packet.ReadInt32("MaxHealthModifierFlatPos", indexes);
                if (Changed(133)) data.SilencedSchoolMask = packet.ReadUInt32("SilencedSchoolMask", indexes);
                if (Changed(134)) data.CurrentAreaID = packet.ReadUInt32("CurrentAreaID", indexes);
                if (Changed(230)) packet.ReadPackedGuid128("ClassicComboTarget", indexes);
                if (Changed(135)) data.NameplateDistanceMod = packet.ReadSingle("NameplateDistanceMod", indexes);
                if (Changed(136)) data.AutoAttackRangeMod = packet.ReadSingle("AutoAttackRangeMod", indexes);
                if (Changed(137)) data.NameplateAttachToGUID = packet.ReadPackedGuid128("NameplateAttachToGUID", indexes);
            }
            if (classicGroup128)
                hasAssistActionData = packet.ReadBit("HasAssistActionData", indexes);
            packet.ResetBitReader();

            if (Changed(128))
                if (Changed(138) && hasAssistActionData)
                    packet.AddValue("AssistActionData", "<skipped>", indexes);

            if (Changed(139))
            {
                for (var i = 0; i < 10; ++i)
                {
                    if (Changed(140 + i)) data.Power[i] = packet.ReadInt32("Power", indexes, i);
                    if (Changed(150 + i)) data.MaxPower[i] = packet.ReadInt32("MaxPower", indexes, i);
                    if (Changed(160 + i)) data.PowerRegenFlatModifier[i] = packet.ReadSingle("PowerRegenFlatModifier", indexes, i);
                    if (Changed(170 + i)) data.PowerRegenInterruptedFlatModifier[i] = packet.ReadSingle("PowerRegenInterruptedFlatModifier", indexes, i);
                }
            }

            if (Changed(180))
                for (var i = 0; i < 3; ++i)
                    if (Changed(181 + i))
                        data.VirtualItems[i] = UpdateFieldsHandler.ReadUpdateVisibleItem(packet, indexes, "VirtualItems", i);

            if (Changed(184))
                for (var i = 0; i < 2; ++i)
                    if (Changed(185 + i))
                        data.AttackRoundBaseTime[i] = packet.ReadUInt32("AttackRoundBaseTime", indexes, i);

            if (Changed(187))
            {
                for (var i = 0; i < 5; ++i)
                {
                    if (Changed(188 + i)) data.Stats[i] = packet.ReadInt32("Stats", indexes, i);
                    if (Changed(193 + i)) data.StatPosBuff[i] = packet.ReadInt32("StatPosBuff", indexes, i);
                    if (Changed(198 + i)) data.StatNegBuff[i] = packet.ReadInt32("StatNegBuff", indexes, i);
                    if (Changed(203 + i)) data.StatSupportBuff[i] = packet.ReadInt32("StatSupportBuff", indexes, i);
                }
            }

            if (Changed(208))
            {
                for (var i = 0; i < 7; ++i)
                {
                    if (Changed(209 + i)) data.Resistances[i] = packet.ReadInt32("Resistances", indexes, i);
                    if (Changed(216 + i)) data.BonusResistanceMods[i] = packet.ReadInt32("BonusResistanceMods", indexes, i);
                    if (Changed(223 + i)) data.ManaCostModifier[i] = packet.ReadInt32("ManaCostModifier", indexes, i);
                }
            }

            return data;
        }
    }
}
