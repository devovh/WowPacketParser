using System;
using System.Reflection;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_UPDATE_OBJECT dla Forever.
    ///
    /// Numer pakietu jest w numeracji WIRE (1.60.1), więc nie da się go podać do
    /// UpdateHandler z 12.x — ten czyta numer już przetłumaczony na numerację core'ową.
    /// Robimy więc wrapper: tłumaczymy numer na numerację core'ową i wołamy handler 12.x,
    /// a błędy czytania zostawiamy w tekście (bez wyjątku, który ucinałby cały pakiet).
    /// </summary>
    public static class UpdateHandler
    {
        private static readonly Action<Packet> Handler1200 =
            (Action<Packet>)Delegate.CreateDelegate(typeof(Action<Packet>),
                typeof(V12_0_0_65390.Parsers.UpdateHandler).GetMethod("HandleUpdateObject", BindingFlags.Public | BindingFlags.Static));

        [HasSniffData]
        [Parser(Opcode.SMSG_UPDATE_OBJECT)]
        public static void HandleUpdateObject(Packet packet)
        {
            var coreOpcode = ForeverOpcodeHelper.ForeverAsCoreOpcode(packet.Opcode, packet.Direction);
            packet.AddValue("ForeverOpcode", $"0x{packet.Opcode:X6}");
            packet.AddValue("CoreOpcode", $"0x{coreOpcode:X6}");

            var saved = packet.Opcode;
            packet.Opcode = coreOpcode;
            try
            {
                Handler1200(packet);
            }
            catch (Exception ex)
            {
                packet.WriteLine("SMSG_UPDATE_OBJECT: " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                packet.Opcode = saved;
            }
        }
    }
}
