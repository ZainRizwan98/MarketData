namespace MarketData.Domain;

public class MessageHeader
{
    public long SequenceNumber { get; set; }
    public DateTime ReceivedAt { get; set; }
}
