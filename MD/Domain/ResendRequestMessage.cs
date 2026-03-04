using MarketData.Infrastructure;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace MarketData.Domain;

public class ResendRequestMessage
{
    // Tag 7 - BeginSeqNo
    public long? BeginSeqNo { get; set; }
    // Tag 16 - EndSeqNo
    public long? EndSeqNo { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
}
