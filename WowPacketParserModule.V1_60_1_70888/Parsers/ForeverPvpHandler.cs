using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    public static class ForeverPvpHandler
    {
        /// <summary>
        /// SMSG_RATED_PVP_INFO: 9 bracketow po 18 x i32 + u8 + bit Disqualified (+ flush) = 74 B. 9 * 74 = 666 B (sniff 70938).
        /// Uklad z tc-forever BattlegroundPackets.cpp:350.
        /// </summary>
        [Parser(Opcode.SMSG_RATED_PVP_INFO)]
        public static void HandleRatedPvpInfo(Packet packet)
        {
            var count = (int)(packet.Length / 74);
            for (var i = 0; i < count; ++i)
            {
                packet.ReadInt32("PersonalRating", "Bracket", i);
                packet.ReadInt32("Ranking", "Bracket", i);
                packet.ReadInt32("SeasonPlayed", "Bracket", i);
                packet.ReadInt32("SeasonWon", "Bracket", i);
                packet.ReadInt32("SeasonFactionPlayed", "Bracket", i);
                packet.ReadInt32("SeasonFactionWon", "Bracket", i);
                packet.ReadInt32("WeeklyPlayed", "Bracket", i);
                packet.ReadInt32("WeeklyWon", "Bracket", i);
                packet.ReadInt32("RoundsSeasonPlayed", "Bracket", i);
                packet.ReadInt32("RoundsSeasonWon", "Bracket", i);
                packet.ReadInt32("RoundsWeeklyPlayed", "Bracket", i);
                packet.ReadInt32("RoundsWeeklyWon", "Bracket", i);
                packet.ReadInt32("BestWeeklyRating", "Bracket", i);
                packet.ReadInt32("LastWeeksBestRating", "Bracket", i);
                packet.ReadInt32("BestSeasonRating", "Bracket", i);
                packet.ReadInt32("PvpTierID", "Bracket", i);
                packet.ReadInt32("SeasonPvpTier", "Bracket", i);
                packet.ReadInt32("BestWeeklyPvpTier", "Bracket", i);
                packet.ReadByte("BestSeasonPvpTierEnum", "Bracket", i);
                packet.ResetBitReader();
                packet.ReadBit("Disqualified", "Bracket", i);
                packet.ResetBitReader();
            }
        }

        /// <summary>
        /// SMSG_REQUEST_PVP_REWARDS_RESPONSE (sniff 70938, 310 B): pierwsze nagrody (RandomBG), bajt bitow HasWon..., potem
        /// kolejne bloki nagrod po 22 B (u8 Mask, money, xp, 3 liczniki, bajt bitow). 1 + 14 * 22 = 309 B, ostatni bajt (0xF8 w sniffie)
        /// nie ma jeszcze nazwy. tc-forever nie ma tego pakietu, uklad liczony z dlugosci i bajtow.
        /// </summary>
        [Parser(Opcode.SMSG_REQUEST_PVP_REWARDS_RESPONSE)]
        public static void HandleRequestPvpRewardsResponse(Packet packet)
        {
            ReadReward(packet, "RandomBGRewards");
            packet.ResetBitReader();
            for (var i = 0; i < 6; ++i)
                packet.ReadBit("HasWon", i);
            packet.ResetBitReader();

            var count = (int)((packet.Length - packet.Position - 1) / 22);
            for (var i = 0; i < count; ++i)
                ReadReward(packet, "Rewards", i);

            while (packet.Position < packet.Length)
                packet.ReadByte("Unknown");
        }

        private static void ReadReward(Packet packet, params object[] idx)
        {
            packet.ReadByte("Mask", idx);
            packet.ReadInt32("RewardMoney", idx);
            packet.ReadInt32("RewardXP", idx);
            var items = packet.ReadUInt32("ItemCount", idx);
            var currencies = packet.ReadUInt32("CurrencyCount", idx);
            var bonus = packet.ReadUInt32("BonusCurrency", idx);
            for (var k = 0; k < items; ++k)
            {
                packet.ReadInt32("ItemID", idx, k);
                packet.ReadInt32("Quantity", idx, k);
            }
            for (var k = 0; k < currencies; ++k)
            {
                packet.ReadInt32("CurrencyID", idx, k);
                packet.ReadInt32("Quantity", idx, k);
            }
            for (var k = 0; k < bonus; ++k)
            {
                packet.ReadInt32("CurrencyID", idx, k);
                packet.ReadInt32("Quantity", idx, k);
            }
            packet.ResetBitReader();
            packet.ReadBit("HasRewardSpellID", idx);
            packet.ReadBit("Unused1", idx);
            packet.ReadBit("Unused2", idx);
            packet.ReadBit("HasHonor", idx);
            packet.ResetBitReader();
        }
    }
}
