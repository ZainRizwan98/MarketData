using System.Collections.Generic;

namespace MarketData.Domain;

public class OrderModifyMessage
{
    public string? ClOrdID { get; set; } // 11
    public string? OrigClOrdID { get; set; } // 41
    public decimal? OrderQty { get; set; } // 38
    public decimal? Price { get; set; } // 44
    public Dictionary<string, string>? Fields { get; set; }
}
