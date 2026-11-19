using System;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// Pakiety sesji Forever (1.60.1): odpowiedzi logowania, kolejki, czas serwera.
    /// Wszystkie odczyty są w numeracji wire; patrz ForeverOpcodeHelper.
    /// </summary>
    public static class SessionHandler
    {
        [Parser(Opcode.SMSG_AUTH_CHALLENGE)]
        public static void HandleAuthChallenge(Packet packet)
        {
            // 70888: 32 B challenge. 70938 (sniff 2026-11-13): 65 B = 32 B challenge + 32 B nieznanych danych + 1 B,
            // dlatego reszta leci jako surowe bajty, zamiast udawać, że znamy ich znaczenie.
            packet.ReadBytes("Challenge", 32);

            if (packet.CanRead())
                packet.ReadBytes("Unknown", (int)(packet.Length - packet.Position));
        }

        [Parser(Opcode.SMSG_AUTH_RESPONSE)]
        public static void HandleAuthResponse(Packet packet)
        {
            // tc-forever AuthResponse::Write: [u32 Result][bit HasSuccessInfo][bit HasWaitInfo][flush] - Result PRZED bitami
            // (sniff 70938: 00 00 00 00 80 ...)
            packet.ReadUInt32E<BattlenetRpcErrorCode>("Result");
            packet.ResetBitReader();
            var ok = packet.ReadBit("HasSuccessInfo");
            var queued = packet.ReadBit("HasWaitInfo");

            if (ok)
            {
                packet.ReadUInt32("VirtualRealmAddress");
                var realms = packet.ReadUInt32("VirtualRealmCount");
                packet.ReadUInt32("TimeRested");
                packet.ReadByte("ActiveExpansionLevel");
                packet.ReadByte("AccountExpansionLevel");
                packet.ReadUInt32("TimeSecondsUntilPCKick");

                var classes = packet.ReadUInt32("AvailableClassesCount");
                var templates = packet.ReadUInt32("TemplatesCount");
                packet.ReadUInt32("AccountCurrency");

                ReadGameTimeInfo(packet, "GameTimeInfo");
                packet.ReadTime64("Time");

                for (var i = 0u; i < realms; ++i)
                    ReadVirtualRealmInfo(packet, "VirtualRealms", i);

                for (var i = 0u; i < classes; ++i)
                {
                    packet.ReadByteE<Race>("RaceID", "AvailableClasses", i);
                    var classesForRace = packet.ReadUInt32();
                    for (var j = 0u; j < classesForRace; ++j)
                    {
                        packet.ReadByteE<Class>("ClassID", "AvailableClasses", i, "Classes", j);
                        packet.ReadByteE<ClientType>("ActiveExpansionLevel", "AvailableClasses", i, "Classes", j);
                        packet.ReadByteE<ClientType>("AccountExpansionLevel", "AvailableClasses", i, "Classes", j);
                        packet.ReadByte("MinActiveExpansionLevel", "AvailableClasses", i, "Classes", j);
                    }
                }

                for (var i = 0u; i < templates; ++i)
                {
                    packet.ReadUInt32("Unk", i);
                    packet.ReadUInt32("TemplateSetId", i);
                    var templateClasses = packet.ReadUInt32();
                    for (var j = 0u; j < templateClasses; ++j)
                    {
                        packet.ReadByteE<Class>("Class", i, j);
                        packet.ReadByte("FactionGroup", i, j);
                    }

                    packet.ResetBitReader();
                    var nameLen = packet.ReadBits(7);
                    var descLen = packet.ReadBits(10);
                    packet.ReadWoWString("Name", nameLen, i);
                    packet.ReadWoWString("Description", descLen, i);
                }

                packet.ResetBitReader();
                packet.ReadBit("IsExpansionTrial");
                packet.ReadBit("ForceCharacterTemplate");
                var horde = packet.ReadBit("HasNumPlayersHorde");
                var alliance = packet.ReadBit("HasNumPlayersAlliance");
                var trialExpiration = packet.ReadBit("HasExpansionTrialExpiration");
                var hasNewBuildKeys = packet.ReadBit("HasNewBuildKeys");

                if (horde)
                    packet.ReadUInt16("NumPlayersHorde");

                if (alliance)
                    packet.ReadUInt16("NumPlayersAlliance");

                if (trialExpiration)
                    packet.ReadInt64("ExpansionTrialExpiration");

                if (hasNewBuildKeys)
                {
                    packet.ReadBytes("NewBuildKey", 16);
                    packet.ReadBytes("SomeKey", 16);
                }
            }

            if (queued)
            {
                packet.ReadUInt32("WaitCount");
                packet.ReadUInt32("WaitTime");
                packet.ReadByte("AllowedFactionGroupForCharacterCreate");
                packet.ResetBitReader();
                packet.ReadBit("HasFCM");
                packet.ReadBit("CanCreateOnlyIfExisting");
            }
        }

        public static void ReadGameTimeInfo(Packet packet, params object[] idx)
        {
            packet.ReadUInt32("BillingType", idx);
            packet.ReadUInt32("MinutesRemaining", idx);
            packet.ReadUInt32("RealBillingType", idx);

            packet.ResetBitReader();
            packet.ReadBit("IsInIGR", idx);
            packet.ReadBit("IsPaidForByIGR", idx);
            packet.ReadBit("IsCAISEnabled", idx);
        }

        public static void ReadVirtualRealmNameInfo(Packet packet, params object[] indexes)
        {
            packet.ResetBitReader();
            packet.ReadBit("IsLocal", indexes);
            packet.ReadBit("IsInternalRealm", indexes);

            var actualNameLength = packet.ReadBits(8);
            var normalizedNameLength = packet.ReadBits(8);

            packet.ReadWoWString("RealmNameActual", actualNameLength, indexes);
            packet.ReadWoWString("RealmNameNormalized", normalizedNameLength, indexes);
        }

        public static void ReadVirtualRealmInfo(Packet packet, params object[] indexes)
        {
            packet.ReadUInt32("RealmAddress", indexes);
            ReadVirtualRealmNameInfo(packet, indexes, "RealmNameInfo");
        }

        [Parser(Opcode.SMSG_WAIT_QUEUE_UPDATE)]
        public static void HandleWaitQueueUpdate(Packet packet)
        {
            packet.ReadInt32("WaitCount");
            packet.ReadInt32("WaitTime");
            packet.ReadByte("AllowedFactionGroupForCharacterCreate");

            packet.ResetBitReader();
            packet.ReadBit("HasFCM");
            packet.ReadBit("CanCreateOnlyIfExisting");
        }

        [Parser(Opcode.SMSG_WAIT_QUEUE_FINISH)]
        public static void HandleWaitQueueFinish(Packet packet)
        {
        }

        [Parser(Opcode.CMSG_AUTH_SESSION)]
        public static void HandleAuthSession(Packet packet)
        {
            packet.ReadUInt32("RegionID");
            packet.ReadUInt32("BattlegroupID");
            packet.ReadUInt32("RealmID");
            packet.ReadBytes("LocalChallenge", 32);
            packet.ReadBytes("Digest", 24);
            packet.ReadBit("UseIPv6");
            packet.ResetBitReader();
            // Forever 1.60.1: miedzy bitem UseIPv6 a rozmiarem biletu sa 4 bajty, ktorych
            // nie znamy. Zmierzone na trzech sniffach - rozmiar stoi zawsze @77, a
            // 81 + rozmiar = dlugosc pakietu co do bajtu:
            //   70959 18.11 len 935: @69..76 = 52 d0 db 48 7b f4 4e 00, @77 = 854 (81+854=935)
            //   70959 19.11 len 938: @69..76 = 15 e9 ec 8d de 42 5f 00, @77 = 857 (81+857=938)
            //   70938       len 932: @69..76 = 63 48 06 96 67 08 a3 00, @77 = 851 (81+851=932)
            // Ostatni bajt zawsze 00. Czytamy jako nieznane bajty, zeby sie przesunac.
            packet.ReadBytes("AuthSessionUnknown", 8);
            var realmJoinTicketSize = packet.ReadInt32();
            packet.ReadBytes("RealmJoinTicket", (int)System.Math.Min(realmJoinTicketSize, packet.Length - packet.Position));
            packet.ReadBytes("AddonInfo", (int)(packet.Length - packet.Position));
        }

        [Parser(Opcode.CMSG_PING)]
        public static void HandlePing(Packet packet)
        {
            packet.ReadUInt32("Ping");
            packet.ReadUInt32("Latency");
        }

        [Parser(Opcode.SMSG_PONG)]
        public static void HandlePong(Packet packet)
        {
            packet.ReadUInt32("Ping");
        }
    }
}
