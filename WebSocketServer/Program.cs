using System.Net.WebSockets;
using WebSocketServer.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebSocketConnectionManager>();
var app = builder.Build();

app.UseWebSockets();
app.UseWebSocketServer();

// app.MapGet("/", () => "Hello World!");

app.Run(async context => 
{
    Console.WriteLine("Hello from 3rd request delegate!");
    await context.Response.WriteAsync("Response! Hello from 3rd request delegate!");
});

app.Run();
