using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Male pakiety Forever 70862, ktore w sniffie maja inna dlugosc niz w szablonach z innych wersji.
    /// Uklady wg tc-forever (Server/Packets) lub z dlugosci pakietu.
    /// </summary>
    public static class ForeverSmallPacketsHandler
    {
        /// <summary>SpellPackets.cpp:76: u64 Action + u16 Index (360 przyciskow).</summary>
        [Parser(Opcode.CMSG_SET_ACTION_BUTTON, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleSetActionButton(Packet packet)
        {
            var action = packet.ReadUInt64("Action");
            packet.AddValue("Type", (action >> 24) & 0xFF);
            packet.AddValue("ID", action & 0xFFFFFF);
            packet.ReadUInt16("Index");
        }

        /// <summary>Pakiet 1-2 bajty (bez bitowej dlugosci 7 + tekst jak w 3.4.4) - czytamy tylko to, co jest.</summary>
        [Parser(Opcode.CMSG_CHAT_CHANNEL_LIST, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleChannelList(Packet packet)
        {
            if (packet.Length > packet.Position)
            {
                var length = packet.ReadBits(7);
                packet.ResetBitReader();
                if (length > 0 && packet.Length - packet.Position >= length)
                    packet.ReadWoWString("ChannelName", length);
                else
                    packet.ReadToEnd();
            }
        }

        /// <summary>GuildPackets.cpp:593: CurrentVersion (time64) - w 70862 przychodzi 8 bajtow, a nie 4.</summary>
        [Parser(Opcode.CMSG_REQUEST_GUILD_REWARDS_LIST, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleRequestGuildRewardsList(Packet packet)
        {
            if (packet.Length - packet.Position >= 8)
                packet.ReadTime64("CurrentVersion");
            else
                packet.ReadTime("CurrentVersion");
        }

        [Parser(Opcode.CMSG_LOGOUT_CANCEL, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleLogoutCancel(Packet packet)
        {
            packet.ReadToEnd();
        }

        /// <summary>Pakiet ma 1 bajt (flush bitow), brak pol.</summary>
        [Parser(Opcode.SMSG_LOGOUT_COMPLETE, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleLogoutComplete(Packet packet)
        {
            packet.ReadToEnd();
        }

        /// <summary>ReputationPackets.cpp:56: bonus, licznik, (index, change, standing, 0) x licznik, bit ShowVisual.</summary>
        [Parser(Opcode.SMSG_SET_FACTION_STANDING, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleSetFactionStanding(Packet packet)
        {
            packet.ReadSingle("BonusFromAchievementSystem");
            var count = packet.ReadUInt32("Count");
            for (var i = 0; i < count; ++i)
            {
                packet.ReadInt32("Index", i);
                packet.ReadInt32("Change", i);
                packet.ReadInt32("Standing", i);
                packet.ReadInt32("Unused", i);
            }
            packet.ReadBit("ShowVisual");
        }
    }
}
