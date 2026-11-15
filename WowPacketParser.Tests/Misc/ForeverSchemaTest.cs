using System;
using System.Linq;
using NUnit.Framework;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.SQL;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParser.Tests.Misc
{
    /// <summary>
    /// Guards the WoW Forever output path: the Forever schema records must be visible
    /// for TargetedDatabase.Forever and carry exactly the TDB Forever column list.
    /// Settings.TargetedDatabase is read once from the test config, so the tests are
    /// written to hold for whichever database the test run targets: the Forever records
    /// are asserted against Settings.TargetedDatabase instead of mutating it.
    /// </summary>
    [TestFixture]
    public class ForeverSchemaTest
    {
        [Test]
        public void CreatureTemplateForeverColumnsMatchTdb()
        {
            if (Settings.TargetedDatabase != TargetedDatabase.Forever)
                Assert.Ignore("requires TargetedDatabase = Forever in the test config");

            Assert.That(SQLUtil.IsTableVisible<CreatureTemplateForever>(), Is.True);

            var columns = SQLUtil.GetFields<CreatureTemplateForever>().Select(f => f.Item1.Replace("`", "")).ToList();

            // column order and names taken verbatim from TDB_full_world_forever creature_template (tc-forever world_database.sql)
            var expected = new[]
            {
                "entry", "KillCredit1", "KillCredit2", "name", "femaleName", "subname",
                "TitleAlt", "IconName", "RequiredExpansion", "VignetteID", "faction", "npcflag",
                "speed_walk", "speed_run", "scale", "Classification", "dmgschool", "BaseAttackTime",
                "RangeAttackTime", "BaseVariance", "RangeVariance", "unit_class", "unit_flags", "unit_flags2",
                "unit_flags3", "family", "trainer_class", "type", "VehicleId", "AIName",
                "MovementType", "ExperienceModifier", "RacialLeader", "movementId", "WidgetSetID", "WidgetSetUnitConditionID",
                "RegenHealth", "CreatureImmunitiesId", "flags_extra", "ScriptName", "StringId", "VerifiedBuild"
            };

            Assert.That(columns, Is.EqualTo(expected));
        }

        [Test]
        public void ForeverRecordsAreInvisibleForOtherTargets()
        {
            if (Settings.TargetedDatabase == TargetedDatabase.Forever)
                Assert.Ignore("requires a non-Forever TargetedDatabase in the test config");

            Assert.That(SQLUtil.IsTableVisible<CreatureTemplateForever>(), Is.False);
            Assert.That(SQLUtil.IsTableVisible<CreatureTemplateDifficultyForever>(), Is.False);
        }

        [Test]
        public void ForeverStorageAcceptsCreatureTemplate()
        {
            if (!Settings.SQLOutputFlag.HasAnyFlagBit(SQLOutput.creature_template))
                Assert.Ignore("requires SQLOutput.creature_template in the test config");

            Storage.CreatureTemplatesForever.Add(1234, new CreatureTemplateForever { Entry = 1234, Name = "Test" }, TimeSpan.Zero);

            // the store decides once, when Storage is first touched, whether it is enabled (before the SQL flags of a sniff are
            // known); when it was created disabled it silently drops everything, which is not a defect of the Forever record
            if (!Storage.CreatureTemplatesForever.ContainsKey(1234))
                Assert.Ignore("CreatureTemplatesForever was created disabled in this test run");

            Assert.That(Storage.CreatureTemplatesForever.ContainsKey(1234), Is.True);
            Assert.That(Storage.CreatureTemplatesForever.TryGetValue(1234, out CreatureTemplateForever template), Is.True);
            Assert.That(template.Name, Is.EqualTo("Test"));
        }

        [Test]
        public void CreatureDifficultyForeverHasScaleAndContentTuning()
        {
            if (Settings.TargetedDatabase != TargetedDatabase.Forever)
                Assert.Ignore("requires TargetedDatabase = Forever in the test config");

            var columns = SQLUtil.GetFields<CreatureTemplateDifficultyForever>().Select(f => f.Item1.Replace("`", "")).ToList();
            Assert.That(columns, Does.Contain("ContentTuningID"));
            Assert.That(columns.First(), Is.EqualTo("Entry"));
        }

        [Test]
        public void CreatureClassicLevelColumns()
        {
            if (Settings.TargetedDatabase != TargetedDatabase.Forever)
                Assert.Ignore("requires TargetedDatabase = Forever in the test config");

            Assert.That(SQLUtil.IsTableVisible<CreatureClassicLevelForever>(), Is.True);
            var columns = SQLUtil.GetFields<CreatureClassicLevelForever>().Select(f => f.Item1.Replace("`", "")).ToList();
            Assert.That(columns, Is.EqualTo(new[] { "entry", "level_min", "level_max" }));
        }

        [Test]
        public void CreatureLootTemplateForeverColumns()
        {
            if (Settings.TargetedDatabase != TargetedDatabase.Forever)
                Assert.Ignore("requires TargetedDatabase = Forever in the test config");

            var columns = SQLUtil.GetFields<CreatureLootTemplateForever>().Select(f => f.Item1.Replace("`", "")).ToList();
            Assert.That(columns, Is.EqualTo(new[]
            {
                "Entry", "ItemType", "Item", "Chance", "QuestRequired", "LootMode", "GroupId", "MinCount", "MaxCount"
            }));
        }

        [Test]
        public void LootBuilderCountsEachCorpseOnceAndKeepsRows()
        {
            if (Settings.TargetedDatabase != TargetedDatabase.Forever)
                Assert.Ignore("requires TargetedDatabase = Forever in the test config");
            if (!Settings.SQLOutputFlag.HasAnyFlagBit(SQLOutput.creature_template))
                Assert.Ignore("requires SQLOutput.creature_template in the test config");

            Storage.ForeverLootWindows.Clear();
            try
            {
                var corpse1 = new WowGuid128(1, 1UL << 58); // dwa okna tych samych zwlok
                var corpse2 = new WowGuid128(2, 1UL << 58);

                var first = new ForeverLootWindow { Entry = 3100, Owner = corpse1, AcquireReason = 0 };
                first.Items.Add((769, 1));
                var again = new ForeverLootWindow { Entry = 3100, Owner = corpse1, AcquireReason = 0 };
                again.Items.Add((771, 1));
                var other = new ForeverLootWindow { Entry = 3100, Owner = corpse2, AcquireReason = 0 };
                other.Items.Add((769, 1));
                var skin = new ForeverLootWindow { Entry = 3100, Owner = corpse2, AcquireReason = 6 };
                skin.Items.Add((783, 1));
                Storage.ForeverLootWindows.AddRange(new[] { first, again, other, skin });

                var sql = WowPacketParser.SQL.Builders.UnitMisc.CreatureLootTemplateForever();

                Assert.That(sql, Does.Not.Contain("DELETE FROM"));
                Assert.That(sql, Does.Contain("INSERT IGNORE").Or.Contain("INSERT INTO"));
                // 2 martwe cialo = 2 zabojstwa (a nie 3 okna): 769 wypadlo 2/2 = 100, a 771 z ponownego otwarcia jest pominiete
                Assert.That(sql, Does.Contain("drops/kills 2/2"));
                Assert.That(sql, Does.Not.Contain("771"));
                Assert.That(sql, Does.Not.Contain("783"));
            }
            finally
            {
                Storage.ForeverLootWindows.Clear();
            }
        }
    }
}
