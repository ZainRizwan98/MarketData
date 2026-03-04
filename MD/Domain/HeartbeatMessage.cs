using System.Collections.Generic;

namespace MarketData.Domain;

public class HeartbeatMessage
{
    // Tag 112 - TestReqID (optional)
    public string? TestReqID { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
}
