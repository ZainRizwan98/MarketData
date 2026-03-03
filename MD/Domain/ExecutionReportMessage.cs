using System.Collections.Generic;

namespace MarketData.Domain;

public class ExecutionReportMessage
{
    public string? OrderID { get; set; } // 37
    public string? ExecID { get; set; } // 17
    public string? ExecType { get; set; } // 150
    public decimal? LeavesQty { get; set; } // 151
    public decimal? CumQty { get; set; } // 14
    public decimal? AvgPx { get; set; } // 6
    public Dictionary<string, string>? Fields { get; set; }
}
