using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// SMSG_QUERY_NPC_TEXT_RESPONSE (0x4B0008) for Forever 1.60.1.
    ///
    /// Layout (Server/Packets/QueryPackets.cpp QueryNPCTextResponse::Write):
    ///   [u32 TextID][bit Allow][FlushBits]
    ///   [i32 DataSize]  (8 * (4 + 4) = 64 when Allow, else 0)
    ///   if Allow: [8 x f32 Probability][8 x u32 BroadcastTextID]
    ///
    /// Stored in Storage.NpcTextsForever, which the npc_text builder writes for the Forever target.
    /// </summary>
    public static class ForeverQueryNpcTextHandler
    {
        private const int MaxNpcTextOptions = 8;

        [HasSniffData]
        [Parser(Opcode.SMSG_QUERY_NPC_TEXT_RESPONSE)]
        public static void HandleNpcTextResponse(Packet packet)
        {
            var id = packet.ReadUInt32("TextID");
            var allow = packet.ReadBit("Allow");
            packet.ResetBitReader();

            var size = packet.ReadInt32("DataSize");
            if (!allow || size != MaxNpcTextOptions * (sizeof(float) + sizeof(uint)))
                return;

            var npcText = new NpcTextForever
            {
                ID = id,
                Probability = new float?[MaxNpcTextOptions],
                BroadcastTextID = new uint?[MaxNpcTextOptions]
            };

            for (var i = 0; i < MaxNpcTextOptions; ++i)
                npcText.Probability[i] = packet.ReadSingle("Probability", i);

            for (var i = 0; i < MaxNpcTextOptions; ++i)
                npcText.BroadcastTextID[i] = packet.ReadUInt32("BroadcastTextID", i);

            packet.AddSniffData(StoreNameType.NpcText, (int)id, "QUERY_RESPONSE");
            Storage.NpcTextsForever.Add(npcText, packet.TimeSpan);
        }
    }
}
