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

namespace MarketData.Infrastructure;

public static class MarketDataWebSocket
{
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
            await HandleWebSocket(socket);
        });
    }

    private static async Task HandleWebSocket(WebSocket socket)
    {
        var buffer = new byte[4 * 1024];
        using var ms = new MemoryStream();

        while (socket.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            var result = await socket.ReceiveAsync(segment, CancellationToken.None);

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
                    Console.WriteLine($"Received FIX message type: {deserialized?.GetType().Name}");

                    switch (deserialized)
                    {
                        case OrderInsertMessage oi:
                            Console.WriteLine($"OrderInsert: ClOrdID={oi.ClOrdID}, Symbol={oi.Symbol}, Side={oi.Side}, Qty={oi.OrderQty}, Price={oi.Price}");
                            break;
                        case OrderDeleteMessage od:
                            Console.WriteLine($"OrderDelete: OrigClOrdID={od.OrigClOrdID}, ClOrdID={od.ClOrdID}");
                            break;
                        case OrderModifyMessage om:
                            Console.WriteLine($"OrderModify: OrigClOrdID={om.OrigClOrdID}, ClOrdID={om.ClOrdID}, NewQty={om.OrderQty}, NewPrice={om.Price}");
                            break;
                        case ExecutionReportMessage er:
                            Console.WriteLine($"ExecutionReport: OrderID={er.OrderID}, ExecID={er.ExecID}, ExecType={er.ExecType}, LeavesQty={er.LeavesQty}, CumQty={er.CumQty}, AvgPx={er.AvgPx}");
                            break;
                        default:
                            Console.WriteLine("Unknown or unsupported FIX message");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse FIX message: {ex.Message}");
                }
            }
        }
    }
}
