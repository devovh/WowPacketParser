using System;
using System.Collections.Generic;

namespace WowPacketParserModule.V1_60_1_70888.UpdateFields
{
    /// <summary>
    /// Forever 1.60.1 (build 70338-70938): remap bitow maski zmian z numeracji retail (TC 12.x,
    /// ktora uzywa parser) na numeracje klienta Classic. Zrodlo: tc-forever,
    /// src/server/game/Entities/Object/Updates/UpdateFields.cpp — funkcje ClassicPlayerDataBit,
    /// ClassicUnitDataBit / ClassicUnitDataBit70009, ClassicActivePlayerDataBit /
    /// ClassicActivePlayerDataBit70291 oraz tablice Classic*DataGroups.
    ///
    /// Klient wysyla maske w numeracji TC, serwer przenumerowuje ja przez te funkcje i wysyla
    /// w numeracji Classic. Parser czyta wiec bity Classic i musi je z powrotem przelozyc na
    /// numeracje TC, zeby trafic w te same pola, ktore czyta kod 12.x.
    ///
    /// Build 70938 jest >= 70425 i >= 70291, wiec obie galezie "OrLater" sa aktywne.
    /// </summary>
    public static class ForeverClassicBitRemap
    {
        public const uint NoBit = uint.MaxValue;

        // ---- UnitData ----
        // ClassicUnitDataBit70009 + przesuniecia 70291 (>=140 -> +1) i 70425 (>=132 -> +3)
        private static uint UnitDataBit70009(uint bit)
        {
            if (bit == 230) return 141;      // ClassicComboTarget, guid po CurrentAreaID
            if (bit == 231) return 86;       // ClassicTrainingPointsUsed
            if (bit == 232) return 87;       // ClassicTrainingPointsTotal
            if (bit == 70) return 71;        // VisFlags
            if (bit == 71) return 70;        // AnimTier
            if (bit <= 71) return bit;
            if (bit <= 84) return bit + 1;
            if (bit <= 92) return bit + 3;
            if (bit <= 95) return bit + 4;
            if (bit == 96) return 96;
            if (bit <= 101) return bit + 3;
            if (bit <= 105) return bit + 4;
            if (bit <= 122) return bit + 5;
            if (bit <= 127) return bit + 6;
            if (bit == 128) return 128;
            if (bit <= 133) return bit + 5;
            if (bit == 134) return 140;
            return bit + 9;                  // 135-138 -> 144-147, tablice (139+) -> 148+
        }

        public static uint UnitData(uint bit)
        {
            uint classicBit = UnitDataBit70009(bit);
            if (classicBit >= 140) ++classicBit;   // 70291 OrLater
            if (classicBit >= 132) classicBit += 3; // 70425 OrLater
            return classicBit;
        }

        // ---- PlayerData ----
        public static uint PlayerData(uint bit)
        {
            if (bit <= 18) return bit;
            if (bit <= 30) return bit + 1;
            if (bit == 31) return 33;        // TaxiMountAnimKitID za bitem grupy 32
            if (bit == 32) return 32;
            if (bit <= 36) return bit + 1;
            if (bit <= 47) return bit + 2;
            if (bit <= 49) return bit + 3;
            return bit + 4;
        }

        // ---- PlayerData70959 ----
        // Build 70959 ma inna numerację bitów (inny "ogon" - brak 17-bajtowego structu classic-only).
        // Na razie kopia PlayerData (70938) - do zaktualizowania z tc-forever PlayerData::WriteUpdate.
        public static uint PlayerData70959(uint bit)
        {
            if (bit <= 18) return bit;
            if (bit <= 30) return bit + 1;
            if (bit == 31) return 33;
            if (bit == 32) return 32;
            if (bit <= 36) return bit + 1;
            if (bit <= 47) return bit + 2;
            if (bit <= 49) return bit + 3;
            return bit + 4;
        }

        // ---- ActivePlayerData ----
        private static uint ActivePlayerDataBit70291(uint bit)
        {
            if (bit == 401) return 159;      // NumStableSlots
            if (bit <= 107) return bit;
            if (bit <= 109) return bit + 1;
            if (bit <= 115) return bit + 2;
            if (bit == 116) return NoBit;    // PetSpellPower — nie istnieje w Classic
            if (bit <= 118) return bit + 1;
            if (bit <= 131) return bit + 2;
            if (bit == 132) return 135;      // NumBankSlots
            if (bit == 133) return 136;      // NumCharacterBankTabs
            if (bit == 134) return 134;
            if (bit == 135) return 137;      // NumAccountBankTabs
            if (bit <= 148) return bit + 2;
            if (bit <= 154) return bit + 3;
            if (bit == 155) return 158;      // PetStable
            if (bit <= 161) return bit + 4;
            if (bit <= 271) return bit + 5;
            return bit + 42;                 // tablice po InvSlots
        }

        public static uint ActivePlayerData(uint bit)
        {
            uint classicBit = ActivePlayerDataBit70291(bit);
            if (classicBit == NoBit || classicBit <= 120 || classicBit == 134 || classicBit == 166)
                return classicBit;
            if (classicBit == 133) return 135;
            if (classicBit == 165) return 167;
            if (classicBit == 167) return 168;
            return classicBit >= 168 ? classicBit + 2 : classicBit + 1;
        }

        // ---- Grupy klasyczne (zakresy bitow TC, ktore trafiaja do jednego bitu grupowego) ----
        public static readonly (uint Group, uint Last)[] PlayerDataGroups =
        {
            (0, 31), (32, 53), (54, 56), (57, 232), (233, 252), (253, 259), (260, 292), (293, 309), (310, 329)
        };

        // PlayerDataGroups dla 70959 - na razie identyczne jak 70938, do zweryfikowania z tc-forever
        public static readonly (uint Group, uint Last)[] PlayerDataGroups70959 =
        {
            (0, 31), (32, 53), (54, 56), (57, 232), (233, 252), (253, 259), (260, 292), (293, 309), (310, 329)
        };

        public static readonly (uint Group, uint Last)[] UnitDataGroups70425 =
        {
            (0, 31), (32, 63), (64, 95), (96, 127), (128, 151), (152, 192), (193, 196), (197, 199), (200, 220), (221, 242)
        };

        public static readonly (uint Group, uint Last)[] ActivePlayerDataGroups70425 =
        {
            (0, 31), (32, 69), (70, 101), (102, 133), (134, 165), (166, 169), (170, 315),
            (316, 318), (319, 347), (348, 354), (355, 379), (380, 412), (413, 417), (418, 420), (421, 426), (427, 444)
        };

        /// <summary>
        /// Odtwarza maske bitow w numeracji TC z maski w numeracji Classic.
        /// Odwrotnosc WriteClassicChangesMaskHeader: dla kazdego bitu TC liczymy jego bit Classic
        /// i sprawdzamy, czy w masce klasycznej jest zapalony.
        /// </summary>
        public static ulong[] ToRetailBits(ulong[] classicBlocks, uint tcBitCount, Func<uint, uint> remap, (uint Group, uint Last)[] groups)
        {
            var set = new HashSet<uint>();
            int wordCount = (int)((tcBitCount + 63) / 64);
            var result = new ulong[wordCount];

            for (uint tcBit = 0; tcBit < tcBitCount; ++tcBit)
            {
                uint cb = remap(tcBit);
                if (cb == NoBit) continue;
                if ((cb >> 6) >= (uint)classicBlocks.Length) continue;
                if ((classicBlocks[cb >> 6] & (1UL << (int)(cb & 63))) == 0) continue;

                result[tcBit >> 6] |= 1UL << (int)(tcBit & 63);
                set.Add(tcBit);
            }

            // Bity grupowe: jesli pozycja grupowa jest zapalona, to znaczy "ktorys z zakresu sie zmienil".
            // Dla czytania pol to nie wystarcza — potrzebujemy konkretnych bitow. Dlatego dodatkowo
            // rozwiazujemy grupy: bit grupowy zapalony + brak szczegolowych -> traktujemy caly zakres jako zmieniony
            // (serwer wysyla wtedy wszystkie bity zakresu w blokach, wiec zwykle sa juz ustawione).
            foreach (var (group, last) in groups)
            {
                uint groupClassic = remap(group);
                if (groupClassic == NoBit) continue;
                if ((groupClassic >> 6) >= (uint)classicBlocks.Length) continue;
                if ((classicBlocks[groupClassic >> 6] & (1UL << (int)(groupClassic & 63))) == 0) continue;

                bool anyDetailed = false;
                for (uint tc = group + 1; tc <= last; ++tc)
                    if ((tc >> 6) < (uint)result.Length && (result[tc >> 6] & (1UL << (int)(tc & 63))) != 0)
                    { anyDetailed = true; break; }

                if (!anyDetailed)
                {
                    result[group >> 6] |= 1UL << (int)(group & 63);
                    set.Add(group);
                }
            }

            return result;
        }
    }
}
