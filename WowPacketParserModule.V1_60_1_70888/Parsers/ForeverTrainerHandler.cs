using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_TRAINER_LIST (0x47018D) dla Forever 1.60.1.
    ///
    /// Uklad zmierzony bajt po bajcie na 621 pakietach ze sniffu 1.60.1.70862.
    /// Wszystkie maja dokladnie 48 bajtow i domykaja sie bez reszty:
    ///
    ///   [0]  u32   TrainerID
    ///   [4]  u32   SpellID            (rosnie o 1 w kolejnych pakietach tego samego trenera)
    ///   [8]  u32   MoneyCost
    ///   [12] GUID  TrainerGUID        (maska 0xE006 = 5 bitow -> 7 B)
    ///   [19] u32   ReqSkillLine
    ///   [23] u32   ReqSkillRank
    ///   [27] u32   ReqAbility1
    ///   [31] u32   ReqAbility2        (serwer powtarza te sama wartosc)
    ///   [35] u32   ReqAbility3        (zawsze 0)
    ///   [39] u32   ReqAbility2        (duplikat)
    ///   [43] u32   ReqAbility3        (duplikat, zawsze 0)
    ///
    /// Parser 6.x czyta tu ReadPackedGuid128 na poczatku (maska 2-bajtowa low/high),
    /// co dla Forever przesuwa caly odczyt i konczy sie EndOfStream.
    ///
    /// Uwaga: Trainers/TrainerSpells nie sa dodawane do Storage — to wymagaloby
    /// wartosci pol w kolejnosci retail, a Forever trzyma je inaczej. Celem tego
    /// handlera jest poprawne rozlozenie pakietu, nie generowanie SQL.
    /// </summary>
    public static class ForeverTrainerHandler
    {
        [Parser(Opcode.SMSG_TRAINER_LIST)]
        public static void HandleServerTrainerList(Packet packet)
        {
            packet.ReadUInt32("TrainerID");
            packet.ReadUInt32("SpellID");
            packet.ReadUInt32("MoneyCost");

            ForeverGuid.Read(packet, "TrainerGUID");

            packet.ReadUInt32("ReqSkillLine");
            packet.ReadUInt32("ReqSkillRankField");
            packet.ReadByte("Padding");

            for (var i = 0; i < 3; ++i)
                packet.ReadUInt32("ReqAbility", i);

            // Forever dopisuje ogon: ReqAbility2/ReqAbility3 jeszcze raz.
            packet.ReadUInt32("ReqAbility2Copy");
            packet.ReadUInt32("ReqAbility3Copy");
        }
    }
}
