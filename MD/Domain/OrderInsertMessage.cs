using System.Collections.Generic;

namespace MarketData.Domain;

public class OrderInsertMessage
{
    public string? ClOrdID { get; set; } // 11
    public string? Symbol { get; set; } // 55
    public string? Side { get; set; } // 54
    public decimal? OrderQty { get; set; } // 38
    public decimal? Price { get; set; } // 44
    public Dictionary<string, string>? Fields { get; set; }
}
