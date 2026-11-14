using System;
using System.Text;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Proto;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_CHAT (0x4C0001) dla Forever 1.60.1 (buildy 70338-70938).
    ///
    /// Uklad ustalony bajt po bajcie na 118 pakietach ze sniffu 1.60.1.70862.
    /// 113 z 118 rozklada sie dokladnie; 5 najdluzszych wiadomosci z UTF-8
    /// rozpoznajemy po ogonie, nie po liczniku (opisane nizej).
    ///
    ///   [0]   u8   typ wiadomosci (ChatMessageTypeNew)
    ///   [1]   u8   jezyk
    ///   [2]   u16  zero
    ///   [4]   4 x GUID128 (kolejno: nadawca, gildia, konto, cel)
    ///   [36]  u32  ChatFlags
    ///   [40]  u8   chanBitsHi
    ///   [41]  u8   (chanBitsLo &lt;&lt; 3) | flagi
    ///   [42]  u8   dlugosc tekstu * 4 (dolne 8 bitow - przy dlugosciach &gt; 63 znakow nastepuje przepelnienie)
    ///   [43]  u8   0x40
    ///   [44]  kanal i tresc w UTF-8, sklejone bez separatora
    ///   [-]   ogon 8-10 B (dla wiadomosci kanalowych; konczy sie na 04 8C 6C 68)
    ///
    /// Liczniki:
    ///   kanal: chanBits = pre[4] * 32 + (pre[5] &gt;&gt; 3), dlugosc kanalu w ZNAKACH = chanBits / 8
    ///   tresc: liczba znakow = pre[6] / 4  (+ 64 * k przy przepelnieniu bajtu)
    ///   Dla wpisow NPC/addonow (chanBits = 0) tresc idzie do konca pakietu.
    ///
    /// Numer 0x4C0001 jest w numeracji WIRE, wiec parser musi mieszkac w tym module.
    /// </summary>
    public static class ChatHandler
    {
        private static readonly byte[] TailMark = { 0x04, 0x8C, 0x6C, 0x68 };

        // Forever (tc-forever, ObjectGuid.cpp) wysyla GUID-y tak jak ReadPackedGuid128:
        // bajt maski niskich 8 bajtow, bajt maski wysokich, potem tylko niezerowe bajty
        // w kolejnosci od najmlodszego.
        private static WowGuid128 ReadGuid128(Packet packet, string name, params object[] indexes)
        {
            var lowMask = packet.ReadByte(name + "LowMask", indexes);
            var highMask = packet.ReadByte(name + "HighMask", indexes);
            return new WowGuid128(packet.ReadPackedUInt64(lowMask), packet.ReadPackedUInt64(highMask));
        }

        /// <summary>
        /// Przesuwa pozycje o <paramref name="chars"/> znakow UTF-8, zwraca nowa pozycje.
        /// </summary>
        private static int SkipChars(byte[] data, int pos, int chars)
        {
            int count = 0;
            while (count < chars && pos < data.Length)
            {
                byte b = data[pos];
                int len;
                if (b < 0x80)
                    len = 1;
                else if ((b & 0xE0) == 0xC0)
                    len = 2;
                else if ((b & 0xF0) == 0xE0)
                    len = 3;
                else if ((b & 0xF8) == 0xF0)
                    len = 4;
                else
                    len = 1;

                if (pos + len > data.Length)
                    len = data.Length - pos;

                pos += len;
                count++;
            }

            return pos;
        }

        /// <summary>
        /// Czy od <paramref name="pos"/> do konca pakietu zostaje ogon: 8-10 bajtow,
        /// konczacy sie na 04 8C 6C 68.
        /// </summary>
        private static bool IsTail(byte[] data, int pos)
        {
            int remaining = data.Length - pos;
            if (remaining < 8 || remaining > 10)
                return false;

            for (int i = 0; i < TailMark.Length; i++)
                if (data[data.Length - TailMark.Length + i] != TailMark[i])
                    return false;

            return true;
        }

        [Parser(Opcode.SMSG_CHAT)]
        public static void HandleServerChatMessage(Packet packet)
        {
            var chatPacket = packet.Holder.Chat = new PacketChat();
            var text = new CreatureText();

            text.Type = (ChatMessageType)packet.ReadByteE<ChatMessageTypeNew>("SlashCmd");
            text.Language = (Language)packet.ReadUInt32E<Language>("Language");

            text.SenderGUID = ReadGuid128(packet, "SenderGUID");
            ReadGuid128(packet, "SenderGuildGUID");
            ReadGuid128(packet, "WowAccountGUID");
            text.ReceiverGUID = ReadGuid128(packet, "TargetGUID");

            var flags = packet.ReadUInt32("ChatFlags");
            var chanBitsHi = packet.ReadByte("ChannelBitsHigh");
            var chanBitsLo = packet.ReadByte("ChannelBitsLowAndFlags");
            var textLenTimes4 = packet.ReadByte("TextLengthTimes4");
            packet.ReadByte("TextLengthHigh");

            // Liczniki z naglowka (patrz opis klasy).
            var channelChars = ((chanBitsHi * 32) + (chanBitsLo >> 3)) / 8;
            var textCharsBase = textLenTimes4 / 4;

            var data = packet.ReadBytes((int)(packet.DeclaredLength - packet.Position));

            string channelName = string.Empty;
            string message;

            if (channelChars > 0 && channelChars < data.Length)
            {
                int channelEnd = SkipChars(data, 0, channelChars);
                channelName = Encoding.UTF8.GetString(data, 0, channelEnd);

                // Dlugosc tresci jest podana w bajcie jako (znaki * 4) &amp; 0xFF,
                // wiec przy wiadomosciach dluzszych niz 63 znaki trzeba dobrac wielokrotnosc 64.
                // Wybieramy ta, po ktorej zostaje prawidlowy ogon.
                int messageEnd = -1;
                for (int k = 0; k <= 8; k++)
                {
                    int chars = textCharsBase + (64 * k);
                    int candidate = SkipChars(data, channelEnd, chars);
                    if (IsTail(data, candidate))
                    {
                        messageEnd = candidate;
                        break;
                    }
                }

                // Gdy zaden wariant nie trafia w ogon (najdluzsze wiadomosci z UTF-8),
                // tresc bierzemy do poczatku ogona rozpoznanego od konca pakietu.
                if (messageEnd < 0)
                {
                    messageEnd = data.Length - 8;
                    if (messageEnd < channelEnd)
                        messageEnd = channelEnd;
                }
                message = Encoding.UTF8.GetString(data, channelEnd, messageEnd - channelEnd);
                packet.AddValue("Channel Name", channelName);
                packet.AddValue("Tail", BitConverter.ToString(data, messageEnd));
            }
            else
            {
                // Wpisy NPC i addonow: brak nazwy kanalu, tresc do konca pakietu.
                message = Encoding.UTF8.GetString(data);
            }

            text.Text = message;
            packet.AddValue("Text", message);

            if (text.SenderGUID != null && text.SenderGUID.GetObjectType() == ObjectType.Unit && text.SenderGUID.GetEntry() != 0)
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
