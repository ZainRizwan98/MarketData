using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MarketData.Parsing;
using MarketData.Domain;
using MarketData.Infrastructure;

namespace MarketData.Infrastructure;

public static class MarketDataWebSocket
{
    private static readonly InMemoryMessageStore _store = new InMemoryMessageStore();

    public static void Configure(WebApplication app)
    {
        app.UseWebSockets();

        app.MapGet("/", () => "MarketData WebSocket server");

        app.Map("/ws", async (HttpContext context) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket requests only");
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("WebSocket connection accepted");
            await HandleWebSocket(socket, context.RequestAborted);
        });
    }

    private static async Task HandleWebSocket(WebSocket socket, CancellationToken connectionToken)
    {
        var buffer = new byte[4 * 1024];
        using var ms = new MemoryStream();

        var lastReceived = DateTime.UtcNow;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(connectionToken);
        var linked = cts.Token;

        // start heartbeat sender and inactivity monitor
        var heartbeatTask = SendHeartbeats(socket, linked);
        var monitorTask = MonitorInactivity(socket, () => lastReceived, cts);

        while (socket.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(segment, linked);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine("WebSocket closed by client");
                break;
            }

            ms.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                var messageBytes = ms.ToArray();
                ms.SetLength(0);

                try
                {
                    var deserialized = FixParser.ParseAndDeserialize(messageBytes);

                    // update last received time
                    lastReceived = DateTime.UtcNow;

                    // Heartbeat messages do not consume sequence numbers and are not stored
                    if (deserialized is HeartbeatMessage hb)
                    {
                        Console.WriteLine($"Heartbeat received. TestReqID={hb.TestReqID}");
                        continue;
                    }

                    // For other messages assign a sequence number
                    var seq = SequenceGenerator.Current;
                    
                    // Store only order-related messages
                    if (deserialized is OrderInsertMessage or OrderDeleteMessage or OrderModifyMessage)
                    {
                        seq = SequenceGenerator.Next();
                        _store.Add(seq, messageBytes);
                    }

                    var header = new MessageHeader { SequenceNumber = seq, ReceivedAt = DateTime.UtcNow };
                    var envelope = new MessageEnvelope { Header = header, Payload = deserialized };
                    
                    switch (envelope.Payload)
                    {
                        case OrderInsertMessage oi:
                            Console.WriteLine($"[{envelope.Header.SequenceNumber}] OrderInsert: ClOrdID={oi.ClOrdID}, Symbol={oi.Symbol}, Side={oi.Side}, Qty={oi.OrderQty}, Price={oi.Price}");
                            break;
                        case OrderDeleteMessage od:
                            Console.WriteLine($"[{envelope.Header.SequenceNumber}] OrderDelete: OrigClOrdID={od.OrigClOrdID}, ClOrdID={od.ClOrdID}");
                            break;
                        case OrderModifyMessage om:
                            Console.WriteLine($"[{envelope.Header.SequenceNumber}] OrderModify: OrigClOrdID={om.OrigClOrdID}, ClOrdID={om.ClOrdID}, NewQty={om.OrderQty}, NewPrice={om.Price}");
                            break;
                        case ExecutionReportMessage er:
                            Console.WriteLine($"[{envelope.Header.SequenceNumber}] ExecutionReport: OrderID={er.OrderID}, ExecID={er.ExecID}, ExecType={er.ExecType}, LeavesQty={er.LeavesQty}, CumQty={er.CumQty}, AvgPx={er.AvgPx}");
                            break;
                        case ResetSequenceMessage rs:
                            Console.WriteLine($"[{envelope.Header.SequenceNumber}] ResetSequence: NewSeqNo={rs.NewSeqNo}, GapFillFlag={rs.GapFillFlag}");
                            // If a reset specifies a new sequence, set generator so next message will use that value
                            if (rs.NewSeqNo.HasValue)
                            {
                                // internal counter should be NewSeqNo - 1 so next Next() returns NewSeqNo
                                SequenceGenerator.Set(rs.NewSeqNo.Value - 1);
                                Console.WriteLine($"Sequence generator set so next seq will be {rs.NewSeqNo}");
                            }
                            break;
                        case ResendRequestMessage rr:
                            // Determine begin and end
                            var begin = rr.BeginSeqNo ?? 1;
                            var end = rr.EndSeqNo.HasValue && rr.EndSeqNo.Value > 0 ? rr.EndSeqNo.Value : SequenceGenerator.Current;
                            Console.WriteLine($"Resend request for range {begin}..{end}");
                            var toResend = _store.GetRange(begin, end);
                            foreach (var stored in toResend)
                            {
                                try
                                {
                                    // Prefix with sequence for clarity: SEQ=<seq>|<original message>
                                    var payload = Encoding.ASCII.GetString(stored.RawMessage);
                                    var outMsg = $"SEQ={stored.SequenceNumber}|{payload}";
                                    var outBytes = Encoding.ASCII.GetBytes(outMsg);
                                    await socket.SendAsync(new ArraySegment<byte>(outBytes), WebSocketMessageType.Text, true, linked);
                                    Console.WriteLine($"Resent seq={stored.SequenceNumber}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to resend seq={stored.SequenceNumber}: {ex.Message}");
                                }
                            }
                            break;
                        default:
                            Console.WriteLine($"[{envelope.Header.SequenceNumber}] Unknown or unsupported FIX message");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse FIX message: {ex.Message}");
                }
            }
        }

        // connection closed, cancel background tasks
        try
        {
            cts.Cancel();
            await Task.WhenAll(heartbeatTask, monitorTask).ConfigureAwait(false);
        }
        catch { }
    }

    private static async Task SendHeartbeats(WebSocket socket, CancellationToken token)
    {
        var interval = TimeSpan.FromSeconds(30);
        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            try
            {
                await Task.Delay(interval, token);
                if (token.IsCancellationRequested || socket.State != WebSocketState.Open) break;

                // simple FIX heartbeat (pipe-delimited for readability)
                var hb = "35=0|10=000|";
                var bytes = Encoding.ASCII.GetBytes(hb);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                Console.WriteLine("Sent heartbeat to client");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"Heartbeat sender error: {ex.Message}");
                break;
            }
        }
    }

    private static async Task MonitorInactivity(WebSocket socket, Func<DateTime> getLastReceived, CancellationTokenSource cts)
    {
        var timeout = TimeSpan.FromSeconds(90);
        var checkInterval = TimeSpan.FromSeconds(5);
        while (!cts.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            try
            {
                await Task.Delay(checkInterval, cts.Token);
                var last = getLastReceived();
                if (DateTime.UtcNow - last > timeout)
                {
                    Console.WriteLine("Connection inactive, closing socket due to missed heartbeats");
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Inactivity timeout", CancellationToken.None);
                    }
                    catch { }
                    cts.Cancel();
                    break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"Inactivity monitor error: {ex.Message}");
                break;
            }
        }
    }
}
