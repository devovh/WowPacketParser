using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_TRAINER_LIST = 0x47018B w 70938 (tc-forever ma 0x45018A, serwer dodaje przesuniecie Classic +0x20001).
    /// Uklad od dewelopera (A, 5 list z zywego zrzutu 2026-11-15 zgadza sie co do bajta):
    /// guid, i8 TrainerType, u32 TrainerID, u32 SpellCount,
    /// po SpellCount x 33 B { i32 SpellID, i32 Unknown (0 lub 2), u32 MoneyCost, u8 ReqLevel, u32 ReqSkillLine, u32 ReqSkillRank, i32 ReqAbility[3] },
    /// 11 bitow GreetingLength + flush, Greeting.
    /// Zastapil wczesniejszy handler pod blednym numerem 0x47018D (to SMSG_CRITERIA_UPDATE).
    /// </summary>
    public static class ForeverTrainerHandler
    {
        [Parser(Opcode.SMSG_TRAINER_LIST)]
        public static void HandleTrainerList(Packet packet)
        {
            var trainer = new Trainer();

            ForeverGuid.Read(packet, "TrainerGUID");
            trainer.Type = (TrainerType)packet.ReadSByte("TrainerType");
            trainer.Id = packet.ReadUInt32("TrainerID");

            var count = packet.ReadUInt32("SpellCount");
            for (var i = 0; i < count; ++i)
            {
                var spell = new TrainerSpell { TrainerId = trainer.Id };
                spell.SpellId = packet.ReadUInt32<SpellId>("SpellID", i);
                packet.ReadInt32("Unknown", i);
                spell.MoneyCost = packet.ReadUInt32("MoneyCost", i);
                spell.ReqLevel = packet.ReadByte("ReqLevel", i);
                spell.ReqSkillLine = packet.ReadUInt32("ReqSkillLine", i);
                spell.ReqSkillRank = packet.ReadUInt32("ReqSkillRank", i);
                spell.ReqAbility = new uint[3];
                for (var j = 0; j < 3; ++j)
                    spell.ReqAbility[j] = packet.ReadUInt32("ReqAbility", i, j);
                spell.FactionHelper = "MoneyCost as sent (reputation discount not removed)";

                Storage.TrainerSpells.Add(spell, packet.TimeSpan);
            }

            packet.ResetBitReader();
            var greetingLength = packet.ReadBits("GreetingLength", 11);
            packet.ResetBitReader();
            trainer.Greeting = packet.ReadWoWString("Greeting", greetingLength);

            Storage.Trainers.Add(trainer, packet.TimeSpan);
            // creature_trainer: NPC bierzemy z ostatniego wyboru w gossip; bez niego (okno trenera bez rozmowy) pomijamy wiersz,
            // zamiast wywalac pakiet.
            var last = WowPacketParser.Parsing.Parsers.NpcHandler.LastGossipOption;
            if (last.Guid != null)
                WowPacketParser.Parsing.Parsers.NpcHandler.AddToCreatureTrainers(trainer.Id, packet.TimeSpan);
        }
    }
}
