using System;
using WowPacketParser.Enums;

namespace WowPacketParserModule.V1_60_1_70888
{
    /// <summary>
    /// Reguły numeracji Forever (klient 1.60.1).
    ///
    /// Parser w tym module dostaje opcode w numeracji WIRE — takiej, jaka leci w snifie
    /// i jaka jest kluczem w tabeli Forever (WowPacketParser/Enums/Version/V1_60_1_69876/Opcodes.cs).
    /// Core (i moduły 11.x/12.x) pracują na numeracji "core": grupy o 1 niżej oraz inserty
    /// 0x98 / 0x13B / 0xED zniesione. <see cref="ForeverAsCoreOpcode"/> robi to tłumaczenie.
    ///
    /// Uwaga o bramkowaniu: [Parser] z gałęzią (ClientBranch.Forever) albo z wersją NIE działa
    /// dla Forever — atrybuty powstają przy skanowaniu typów, gdy ClientVersion.Build i Branch
    /// są jeszcze puste (Zero/Retail), więc warunek przepuszcza tylko wpisy bez bramki.
    /// Parser Forever musi mieć goły [Parser(Opcode.X)]; moduł Forever ładuje się po
    /// fallbackach i Handler.LoadHandlers nadpisuje ich wpisy.
    /// </summary>
    public static class ForeverOpcodeHelper
    {
        private const int ClientGroupFirst = 0x2A;
        private const int ClientGroupLast = 0x45;
        private const int ServerGroupFirst = 0x45;
        private const int ServerGroupLast = 0x6A;
        private const int ServerMainGroup = 0x47;

        // inserty wprowadzone w 70291 i 70790 (patrz ClassicOpcodes.cpp w tc-forever)
        private const int ClientMainGroupInsertIndex = 0x13B;
        private const int ServerMainGroupInsertIndex = 0x98;
        private const int ServerMainGroupSecondInsertIndex = 0xED;

        /// <summary>Numer Forever (wire) -> numer, pod którym ten opcode leży w tabeli Forever jako numer core'owy.</summary>
        public static int ForeverAsCoreOpcode(int foreverOpcode, Direction direction)
        {
            var group = foreverOpcode >> 16;
            var index = foreverOpcode & 0xFFFF;

            if (direction == Direction.ClientToServer)
            {
                if (group >= ClientGroupFirst && group <= ClientGroupLast)
                {
                    group -= 1;
                    if (group == ClientGroupLast - 1 && index > ClientMainGroupInsertIndex)
                        --index;
                }
                return (group << 16) | index;
            }

            if (group >= ServerGroupFirst && group <= ServerGroupLast)
            {
                if (group == ServerMainGroup)
                {
                    if (index >= ServerMainGroupSecondInsertIndex)
                        --index;
                    if (index >= ServerMainGroupInsertIndex)
                        --index;
                }
                group -= 1;
            }
            return (group << 16) | index;
        }
    }
}
