namespace MarketData.Domain;

public class MessageEnvelope
{
    public MessageHeader Header { get; set; } = new MessageHeader();
    public object? Payload { get; set; }
}
