using System.Collections.Generic;

namespace MarketData.Domain;

public class ResetSequenceMessage
{
    // Tag 36 - NewSeqNo
    public long? NewSeqNo { get; set; }
    // Tag 123 - GapFillFlag (Y/N)
    public string? GapFillFlag { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
}
