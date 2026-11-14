using WowPacketParser.Misc;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// GUID Forever (1.60.1) w numeracji wire.
    ///
    /// Uklad zmierzony na 538 pakietach SMSG_HIGHEST_THREAT_UPDATE i 621 pakietach
    /// SMSG_TRAINER_LIST ze sniffu 1.60.1.70862:
    ///
    ///   [u16 maska LE][tyle bajtow, ile bitow jest zapalonych w masce]
    ///
    /// Maska to 16-bitowa mapa obecnych bajtow GUID-a (bit 0 = najmlodszy bajt).
    /// UWAGA: to NIE jest ReadPackedGuid128 (tam maska to dwa bajty: low/high),
    /// dlatego SMSG_CHAT ma wlasny, osobny odczyt.
    ///
    /// Przyklad: maska 0xE006 = 1110 0000 0000 0110 -> 5 bitow -> 5 bajtow danych.
    /// </summary>
    public static class ForeverGuid
    {
        /// <summary>Liczba zapalonych bitow w masce = liczba bajtow GUID-a, ktore ida za maska.</summary>
        public static int ByteCount(ushort mask)
        {
            int count = 0;
            for (int i = 0; i < 16; ++i)
                if ((mask & (1 << i)) != 0)
                    ++count;
            return count;
        }

        /// <summary>
        /// Czyta GUID Forever: maska u16 LE, potem tyle bajtow, ile bitow w masce.
        /// Zwraca GUID zlozony z tych bajtow (bajt i trafia na pozycje i).
        /// </summary>
        public static WowGuid128 Read(Packet packet, string name, params object[] indexes)
        {
            var mask = packet.ReadUInt16(name + "Mask", indexes);
            var bytes = packet.ReadBytes(name + "Bytes", ByteCount(mask), indexes);

            ulong low = 0, high = 0;
            int b = 0;
            for (int i = 0; i < 16; ++i)
            {
                if ((mask & (1 << i)) == 0)
                    continue;

                if (i < 8)
                    low |= (ulong)bytes[b] << (8 * i);
                else
                    high |= (ulong)bytes[b] << (8 * (i - 8));
                ++b;
            }

            return new WowGuid128(low, high);
        }
    }
}
