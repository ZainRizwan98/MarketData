using System.Collections.Generic;

namespace MarketData.Domain;

public class ResendRequestMessage
{
    // Tag 7 - BeginSeqNo
    public long? BeginSeqNo { get; set; }
    // Tag 16 - EndSeqNo
    public long? EndSeqNo { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
}
