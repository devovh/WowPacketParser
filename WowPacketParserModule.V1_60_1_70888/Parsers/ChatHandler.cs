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
            // Strumien bitowy Forever: 5 bajtow (40 bitow) po polach stalych.
            // Uklad ustalony na 13 pakietach snu 70938:
            //   [0:11]  SenderNameLength
            //   [11:18] ChannelNameLength   (7 bitow)
            //   [18:30] TextLength          (12 bitow)
            //   [30:34] flagi               (4 bity)
            //   [34:40] TargetNameLength    (6 bitow)
            // W tym buildzie nie ma pol target/prefix z Chat::Write z TrinityCore.
            packet.ResetBitReader();
            var senderNameLen = (int)packet.ReadBits("SenderNameLength", 11);
            var channelLen = (int)packet.ReadBits("ChannelNameLength", 7);
            var textLen = (int)packet.ReadBits("TextLength", 12);
            packet.ReadBits("ChatFlagsBits", 4);
            var targetNameLen = (int)packet.ReadBits("TargetNameLength", 6);
            packet.ResetBitReader();

            // Dlugosc tresci z bitow jest o 2-3 znaki za krotka dla kanalow General,
            // wiec tresc liczymy z reszty pakietu: kanal + tresc wypelniaja wszystko
            // miedzy polami stalymi a ogonem.
            var rawText = packet.ReadBytes((int)(packet.DeclaredLength - packet.Position));

            // Ogon na koncu pakietu: 0x01 <tag> <u16> <u32> <u32> 04 8C 6C 68.
            // Ma 8 bajtow dla Trade i 7 dla General - rozpoznajemy go po 0x01
            // na poczatku i odejmujemy od konca pakietu.
            var textEnd = rawText.Length - 8;
            if (textEnd > 0 && !(rawText[textEnd] == 0x01))
                textEnd = rawText.Length - 7;

            var textStart = senderNameLen + targetNameLen;
            if (textStart > textEnd)
                textStart = 0;

            if (textStart > 0)
                packet.AddValue("SenderNameRaw", System.Text.Encoding.UTF8.GetString(rawText, 0, textStart));

            var channelName = System.Text.Encoding.UTF8.GetString(rawText, textStart, Math.Min(channelLen, textEnd - textStart));
            packet.AddValue("Channel Name", channelName);

            var msgStart = textStart + channelLen;
            var msgLen = textEnd - msgStart;
            if (msgLen < 0)
                msgLen = 0;
            text.Text = System.Text.Encoding.UTF8.GetString(rawText, msgStart, msgLen);
            packet.AddValue("Text", text.Text);
            packet.AddValue("TextLengthBits", textLen);

            packet.AddValue("Tail", System.BitConverter.ToString(rawText, textEnd));

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
