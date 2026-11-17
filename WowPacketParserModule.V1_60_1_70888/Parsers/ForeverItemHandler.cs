using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;

namespace WowPacketParserModule.V1_60_1_70888.Parsers
{
    /// <summary>
    /// CMSG_BUY_ITEM dla Forever 1.60.1 - UKLAD NIEZNANY, zostaje przy starym czytniku.
    ///
    /// W sniffie 17.11 11:53 sa trzy takie pakiety (43, 41 i 37 bajtow). Proby dopasowania:
    ///   - guid Forever (u16 maska + bajty) + Entry/Slot/Count: maski ef67/e667/e767 daja
    ///     12/10/11 bajtow GUID-a, co nie zostawia miejsca na reszte pol - nie pasuje;
    ///   - guid128 + Entry/Slot/Count + guid128 + bajt: daje 41 bajtow, a pakietow jest 37 i 43.
    /// Zadne z tych dwoch nie tlumaczy trzech roznych dlugosci, wiec NIE zgadujemy.
    ///
    /// Handler zostaje w WowPacketParser/Parsing/Parsers/ItemHandler.cs (czyta guid Forever,
    /// Type, Entry, Slot, Count, Bag GUID, Bag Slot) - pola Entry/Slot/Count wychodza w nim
    /// przesuniete, wiec te trzy pakiety trzeba potraktowac jako nierozwiazane i dopisac do
    /// DEVELOPER-REQUEST.md jako prosbe o uklad od dewelopera.
    ///
    /// Ten plik jest celowo pusty (bez [Parser]) - istnieje tylko jako notatka przy kodzie.
    /// </summary>
    public static class ForeverItemHandlerNote
    {
    }
}
