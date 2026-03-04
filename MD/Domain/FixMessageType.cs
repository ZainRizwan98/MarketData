namespace MarketData.Domain;

public enum FixMessageType
{
    Unknown,
    Heartbeat,
    ResendRequest,
    OrderInsert,
    OrderDelete,
    ResetSequence,
    OrderModify,
    ExecutionReport
}
