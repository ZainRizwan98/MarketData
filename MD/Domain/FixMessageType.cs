namespace MarketData.Domain;

public enum FixMessageType
{
    Unknown,
    OrderInsert,
    OrderDelete,
    OrderModify,
    ExecutionReport
}
