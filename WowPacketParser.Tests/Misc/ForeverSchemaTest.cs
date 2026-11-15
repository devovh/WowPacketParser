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

            // column order and names taken verbatim from TDB_full_world_forever creature_template
            var expected = new[]
            {
                "entry", "KillCredit1", "KillCredit2", "name", "femaleName", "subname", "TitleAlt",
                "IconName", "RequiredExpansion", "VignetteID", "Classification", "unit_class", "family",
                "type", "RacialLeader", "movementId", "WidgetSetID", "WidgetSetUnitConditionID", "VerifiedBuild"
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

            Assert.That(Storage.CreatureTemplatesForever.ContainsKey(1234), Is.True);
            Assert.That(Storage.CreatureTemplatesForever.TryGetValue(1234, out CreatureTemplateForever template), Is.True);
            Assert.That(template.Name, Is.EqualTo("Test"));
        }
    }
}
