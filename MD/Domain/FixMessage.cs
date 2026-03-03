using System.Collections.Generic;

namespace MarketData.Domain;

public class FixMessage
{
    public string MsgType { get; set; } = string.Empty;
    public FixMessageType Type { get; set; } = FixMessageType.Unknown;
    public Dictionary<string, string> Fields { get; } = new Dictionary<string, string>();
}
