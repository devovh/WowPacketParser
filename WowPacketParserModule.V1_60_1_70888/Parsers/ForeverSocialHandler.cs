using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    public static class ForeverSocialHandler
    {
        /// <summary>
        /// W sniffie 70862 pod opcodem 0x4400FF (nazwa CMSG_SEND_CONTACT_LIST pochodzi z 70938) przychodzi
        /// u32 licznik i licznik x u32 (np. 0x129531), a nie flagi kontaktow. Dlatego czytamy faktyczny uklad,
        /// a gdy dlugosc sie nie zgadza - starszy uklad (bajt + flagi) wg tc-forever SocialPackets.cpp:23.
        /// </summary>
        [Parser(Opcode.CMSG_SEND_CONTACT_LIST, ClientVersionBuild.V1_60_1_70862)]
        public static void HandleSendContactList(Packet packet)
        {
            if (packet.Length >= 4)
            {
                var start = packet.Position;
                var count = packet.ReadUInt32();
                if (count > 0 && packet.Length == start + 4 + 4L * count)
                {
                    packet.WriteLine("Count: " + count);
                    for (var i = 0; i < count; ++i)
                        packet.ReadUInt32("Id", i);
                    return;
                }
                packet.SetPosition(start);
            }

            if (packet.Length > 4)
                packet.ReadByte("Unknown");
            packet.ReadUInt32("Flags");
        }
    }
}
