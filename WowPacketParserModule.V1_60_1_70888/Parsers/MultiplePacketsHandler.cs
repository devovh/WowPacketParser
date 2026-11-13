using System;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_MULTIPLE_PACKETS / SMSG_COMPRESSED_PACKET dla Forever.
    ///
    /// W 1.60.1 koperta siedzi na 0x4E000D i 0x50000D (w snifie 70938: 0x4E000D), a core'owy
    /// SMSG_MULTIPLE_PACKETS z 4.3+ ma inny numer i inny układ. Pakiet w środku to
    /// [u16 opcode][u16 dlugosc][dane] w numeracji WIRE, więc podpakiet trzeba przetłumaczyć
    /// na numerację core'ową, inaczej Handler.Parse nie znajdzie handlera.
    /// </summary>
    public static class MultiplePacketsHandler
    {
        [Parser(Opcode.SMSG_MULTIPLE_PACKETS_3)]
        public static void HandleMultiplePackets(Packet packet)
        {
            packet.WriteLine("{");
            var i = 0;
            while (packet.CanRead() && packet.Length - packet.Position >= 4)
            {
                var opcodeWire = packet.ReadUInt16();
                var len = packet.ReadUInt16();
                if (len == 0)
                    break;

                var bytes = packet.ReadBytes(len);
                var opcode = (ushort)ForeverOpcodeHelper.ForeverAsCoreOpcode(opcodeWire, packet.Direction);

                packet.Write("[{0}] ", i++);
                packet.AddValue("WireOpcode", $"0x{opcodeWire:X4}");

                using (var subPacket = new Packet(bytes, opcode, packet.Time, packet.Direction, packet.Number, packet.Writer, packet.FileName))
                    Handler.Parse(subPacket, true);
            }
            packet.WriteLine("}");
        }

        [Parser(Opcode.SMSG_COMPRESSED_PACKET)]
        public static void HandleCompressedPacket(Packet packet)
        {
            var decompressedLength = packet.ReadInt32();
            var opcodeWire = packet.ReadUInt16();
            var opcode = (ushort)ForeverOpcodeHelper.ForeverAsCoreOpcode(opcodeWire, packet.Direction);

            using (var inflated = packet.Inflate(decompressedLength))
            {
                inflated.Opcode = opcode;
                Handler.Parse(inflated, true);
            }
        }
    }
}
