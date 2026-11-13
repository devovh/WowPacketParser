using System;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Proto;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_CHAT dla Forever 1.60.1 (buildy 70338-70938).
    ///
    /// Uklad ustalony bajt po bajcie na 13 pakietach ze sniffu 70938 - dlugosci zgadzaja
    /// sie dokladnie w kazdym z nich:
    ///   u8   typ kanalu (ChatMessageTypeNew), u8 jezyka, u16 zero
    ///   GUID128 x4: nadawca, gildia, konto, cel  (8 B nisko + 8 B wysoko, jak w update-object)
    ///   u32  adres wirtualny celu, u32 adres wirtualny nadawcy, u32 AchievementID
    ///   f32  DisplayTime
    ///   u16  ChatFlags, bit HideChatLog, bit FakeSenderName
    ///   strumien bitowy: dlugosc nadawcy (8 bit), odbiorcy (8), prefiksu addonu (8),
    ///                    kanalu (8), tekstu (16) - wszystkie w bajtach
    ///   teksty UTF-8: nadawca, odbiorca, prefiks addonu, kanal, tekst
    ///   u8   ChatTag
    ///   [u8][u8][u32][u32][u32] ogon - staly w kazdym pakiecie (0x01 0xEF 0x01 0x58 0x94 0x19 0x04 0x8C 0x6C 0x68)
    ///
    /// Numer 0x4C0001 jest w numeracji WIRE, wiec parser musi mieszkac w tym module.
    /// </summary>
    public static class ChatHandler
    {
        private static WowGuid128 ReadGuid128(Packet packet, string name, params object[] indexes)
        {
            var lo = packet.ReadUInt64(name + "Low", indexes);
            var hi = packet.ReadUInt64(name + "High", indexes);
            return new WowGuid128(lo, hi);
        }

        [Parser(Opcode.SMSG_CHAT)]
        public static void HandleServerChatMessage(Packet packet)
        {
            var chatPacket = packet.Holder.Chat = new PacketChat();
            var text = new CreatureText();

            text.Type = (ChatMessageType)packet.ReadByteE<ChatMessageTypeNew>("SlashCmd");
            text.Language = (Language)packet.ReadByteE<Language>("Language");
            packet.ReadUInt16("Unk");

            text.SenderGUID = ReadGuid128(packet, "SenderGUID");
            ReadGuid128(packet, "SenderGuildGUID");
            ReadGuid128(packet, "WowAccountGUID");
            text.ReceiverGUID = ReadGuid128(packet, "TargetGUID");

            packet.ReadUInt32("TargetVirtualAddress");
            packet.ReadUInt32("SenderVirtualAddress");
            packet.ReadUInt32("AchievementID");
            packet.ReadSingle("DisplayTime");

            var flags = packet.ReadUInt16("ChatFlags");

            packet.ResetBitReader();
            packet.ReadBit("HideChatLog");
            packet.ReadBit("FakeSenderName");

            // Dlugosci sa plaskie, bajtami: nadawca, odbiorca, prefiks addonu, kanal, tekst.
            // Tekst ma stala dlugosc 5 bajtow, wiec liczymy ja z reszty pakietu.
            // Uklad reszty (od 87 do konca):
            //   [4 bajty dlugosci][tekst][0x01][ChatTag][u16][u32][u32][u32]
            // Tekst konczy sie przed 0x01, wiec szukamy go od konca reszty.
            // Uklad reszty pakietu (od bajtu 87 do konca), sprawdzony na 13 pakietach:
            //   [u32 adres wirtualny][4 bajty dlugosci][tekst][0x01][ChatTag][u16][u32][u32][u32]
            // Adres i dlugosci leza plasko, tekst jest ostatnim fragmentem przed 9-bajtowym ogonem.
            var restLen = (int)(packet.DeclaredLength - packet.Position);
            // [4 bajty znacznikow][tekst][0x01][ChatTag][u16][u32][u32][u32]
            // Uklad reszty: [u32 naglowek][tekst][0x01][ChatTag][u16][u32][u32][u32]
            var textLen = restLen - 9 - 4;
            packet.ReadBytes("Header", 4);
            text.Text = packet.ReadWoWString("Text", textLen);

            packet.ReadByteE<ChatTag>("ChatTag");
            packet.ReadBytes("Tail", (int)(packet.DeclaredLength - packet.Position));

            if (text.SenderGUID.GetObjectType() == ObjectType.Unit && text.SenderGUID.GetEntry() != 0)
                Storage.CreatureTexts.Add(text.SenderGUID.GetEntry(), text, packet.TimeSpan);
            else if (text.ReceiverGUID != null && text.ReceiverGUID.GetObjectType() == ObjectType.Unit && text.ReceiverGUID.GetEntry() != 0)
                Storage.CreatureTexts.Add(text.ReceiverGUID.GetEntry(), text, packet.TimeSpan);

            chatPacket.Text = text.Text;
            chatPacket.Sender = text.SenderGUID.ToUniversalGuid();
            chatPacket.Target = text.ReceiverGUID?.ToUniversalGuid();
            chatPacket.Language = (int)text.Language;
            chatPacket.Type = (int)text.Type;
            chatPacket.Flags = flags;
        }

    }
}
