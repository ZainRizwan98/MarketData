using System;
using System.Globalization;
using System.Text;
using MarketData.Domain;

namespace MarketData.Parsing;

public static class FixParser
{
    /// <summary>
    /// Parse raw bytes into a FixMessage. Supports standard FIX delimiter (SOH, 0x01) or pipe '|' for readability.
    /// </summary>
    public static FixMessage Parse(byte[] raw)
    {
        if (raw == null || raw.Length == 0)
            throw new ArgumentException("Empty message");

        string text;
        try
        {
            text = Encoding.ASCII.GetString(raw);
        }
        catch
        {
            text = Encoding.UTF8.GetString(raw);
        }

        // Choose delimiter: SOH (\x01) is standard; many test clients use '|'.
        char delimiter = text.Contains('\u0001') ? '\u0001' : (text.Contains('|') ? '|' : '\n');

        var msg = new FixMessage();

        var parts = text.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            var tag = part.Substring(0, idx);
            var val = part.Substring(idx + 1);
            msg.Fields[tag] = val;
            if (tag == "35")
            {
                msg.MsgType = val;
            }
        }

        // Map common FIX MsgType (tag 35) values to our domain types
        if (!string.IsNullOrEmpty(msg.MsgType))
        {
            switch (msg.MsgType)
            {
                case "D": // NewOrderSingle
                    msg.Type = FixMessageType.OrderInsert;
                    break;
                case "F": // OrderCancelRequest
                    msg.Type = FixMessageType.OrderDelete;
                    break;
                case "G": // OrderCancelReplaceRequest
                    msg.Type = FixMessageType.OrderModify;
                    break;
                case "8": // ExecutionReport
                    msg.Type = FixMessageType.ExecutionReport;
                    break;
                default:
                    msg.Type = FixMessageType.Unknown;
                    break;
            }
        }

        return msg;
    }

    public static object? ParseAndDeserialize(byte[] raw)
    {
        var msg = Parse(raw);
        return Deserialize(msg);
    }

    public static object? Deserialize(FixMessage msg)
    {
        if (msg == null) return null;

        string? Get(string tag) => msg.Fields.TryGetValue(tag, out var v) ? v : null;

        decimal? GetDecimal(string tag)
        {
            var s = Get(tag);
            if (string.IsNullOrEmpty(s)) return null;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
            return null;
        }

        switch (msg.Type)
        {
            case FixMessageType.OrderInsert:
                return new OrderInsertMessage
                {
                    ClOrdID = Get("11"),
                    Symbol = Get("55"),
                    Side = Get("54"),
                    OrderQty = GetDecimal("38"),
                    Price = GetDecimal("44"),
                    Fields = msg.Fields
                };
            case FixMessageType.OrderDelete:
                return new OrderDeleteMessage
                {
                    ClOrdID = Get("11"),
                    OrigClOrdID = Get("41"),
                    Fields = msg.Fields
                };
            case FixMessageType.OrderModify:
                return new OrderModifyMessage
                {
                    ClOrdID = Get("11"),
                    OrigClOrdID = Get("41"),
                    OrderQty = GetDecimal("38"),
                    Price = GetDecimal("44"),
                    Fields = msg.Fields
                };
            case FixMessageType.ExecutionReport:
                return new ExecutionReportMessage
                {
                    OrderID = Get("37"),
                    ExecID = Get("17"),
                    ExecType = Get("150"),
                    LeavesQty = GetDecimal("151"),
                    CumQty = GetDecimal("14"),
                    AvgPx = GetDecimal("6"),
                    Fields = msg.Fields
                };
            default:
                return msg; // return raw parsed message if unknown
        }
    }
}
