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
        // Forever (tc-forever, ObjectGuid.cpp) wysyla GUID-y 128-bitowe tak samo jak
        // ReadPackedGuid128: bajt maski niskich 8 bajtow, bajt maski wysokich, potem
        // tylko niezerowe bajty w kolejnosci od najmlodszego.
        private static WowGuid128 ReadGuid128(Packet packet, string name, params object[] indexes)
        {
            var lowMask = packet.ReadByte(name + "LowMask", indexes);
            var highMask = packet.ReadByte(name + "HighMask", indexes);
            return new WowGuid128(packet.ReadPackedUInt64(lowMask), packet.ReadPackedUInt64(highMask));
        }

        [Parser(Opcode.SMSG_CHAT)]
        public static void HandleServerChatMessage(Packet packet)
        {
            var chatPacket = packet.Holder.Chat = new PacketChat();
            var text = new CreatureText();

            // Uklad z serwera Forever (tc-forever, ChatPackets.cpp, Chat::Write):
            //   u8 SlashCmd, u32 Language, 4x GUID128, u32 TargetVA, u32 SenderVA,
            //   i32 AchievementID, u32 ChatFlags, f32 DisplayTime, i32 SpellID
            text.Type = (ChatMessageType)packet.ReadByteE<ChatMessageTypeNew>("SlashCmd");
            text.Language = (Language)packet.ReadUInt32E<Language>("Language");

            text.SenderGUID = ReadGuid128(packet, "SenderGUID");
            ReadGuid128(packet, "SenderGuildGUID");
            ReadGuid128(packet, "WowAccountGUID");
            text.ReceiverGUID = ReadGuid128(packet, "TargetGUID");
            ReadGuid128(packet, "ChannelGUID");

            packet.ReadUInt32("TargetVirtualAddress");
            packet.ReadUInt32("SenderVirtualAddress");
            var achievementId = packet.ReadUInt32("AchievementID");
            var flags = packet.ReadUInt32("ChatFlags");
            packet.ReadSingle("DisplayTime");
            var spellId = packet.ReadInt32("SpellID");
            // Po polach stalych zostaje 1 bajt (0x00) i 4 bajty znacznika tekstow
            // (0x04 C0/80 98/54/.. 40). Dlugosci z bitow sa niemiarodajne, a teksty
            // leza plasko na koncu pakietu - dlatego czytamy je jako jeden ciag znakow.
            packet.ReadByte("UnkByte");
            packet.ReadBytes("TextHeader", 4);

            packet.ResetBitReader();
            var channelLen = (int)packet.ReadBits("ChannelNameLength", 7);
            packet.ResetBitReader();


            // Teksty leza plasko na koncu pakietu: kanal (np. "General - Orgrimmar")
            // skleja sie z trescia, bo dlugosci z bitow sa niemiarodajne.
            // Ogon na koncu: 0x01, ChatTag, u16, u32, u32, u32 (zmienny, 9+ bajtow).
            var tailLen = (int)packet.DeclaredLength - (int)packet.Position;
            var rawText = packet.ReadBytes(tailLen);

            // Koniec tekstu: ostatni bajt, ktory nie jest czescia ogona. Ogon zaczyna sie
            // od 0x01 i konczy sie stalymi 0x8C 0x6C 0x68 (0x04 0x8C 0x6C 0x68 dla innych).
            var textEndIndex = rawText.Length;
            if (rawText.Length >= 4 && rawText[rawText.Length - 3] == 0x8C && rawText[rawText.Length - 2] == 0x6C && rawText[rawText.Length - 1] == 0x68)
            {
                textEndIndex = rawText.Length - 3;
                while (textEndIndex > 0 && rawText[textEndIndex - 1] != 0x01)
                    --textEndIndex;
                if (textEndIndex > 0)
                    --textEndIndex;

                // Kanal to "General - Orgrimmar" / "Trade - English 41" - konczy sie na
                // malej literze, a tresc zaczyna sie od duzej litery, cyfry lub spacji.
                // Kanal to "General - Orgrimmar" / "Trade - English 41": konczy sie na
                // ostatniej malej literze przed ostatnim fragmentem, ktory jest nazwa
                // (miasto lub jezyk + numer kanalu).
                var channelCut = 0;
                for (var i = textEndIndex - 1; i > 0; --i)
                {
                    var cur = rawText[i];
                    if (cur >= (byte)'a' && cur <= (byte)'z')
                    {
                        channelCut = i + 1;
                        break;
                    }
                }

                channelCut = Math.Min(channelCut, textEndIndex);
                if (channelCut > 0)
                {
                    packet.AddValue("Channel Name", System.Text.Encoding.UTF8.GetString(rawText, 0, channelCut));
                    text.Text = System.Text.Encoding.UTF8.GetString(rawText, channelCut, textEndIndex - channelCut);
                    textEndIndex = rawText.Length - 3;
                }
            }

            if (text.Text == null)
                text.Text = System.Text.Encoding.UTF8.GetString(rawText, 0, textEndIndex);
            packet.AddValue("Text", text.Text);
            packet.AddValue("Tail", System.BitConverter.ToString(rawText, textEndIndex));

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
