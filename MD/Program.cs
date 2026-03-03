using Microsoft.AspNetCore.Builder;
using MarketData.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

MarketDataWebSocket.Configure(app);

app.Run();
