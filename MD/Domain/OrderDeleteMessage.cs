using System.Collections.Generic;

namespace MarketData.Domain;

public class OrderDeleteMessage
{
    public string? ClOrdID { get; set; } // 11
    public string? OrigClOrdID { get; set; } // 41
    public Dictionary<string, string>? Fields { get; set; }
}
