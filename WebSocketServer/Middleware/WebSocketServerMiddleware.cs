using System.Net.WebSockets;
using System.Text;
using System.Linq;
using System.Text.Json;
using WebSocketServer.Models;

namespace WebSocketServer.Middleware;

public static class WebSocketServerMiddlewareExtensions 
{
    public static IApplicationBuilder UseWebSocketServer(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WebSocketServerMiddleware>();
    }
} 

public static class SocketExtensions
{
    public async static Task SendTextMessageAsync(this WebSocket socket, string message)
    {
        await socket.SendAsync(
            Encoding.UTF8.GetBytes(message),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    } 
}

public class WebSocketServerMiddleware
{
    private readonly RequestDelegate _next;

    private readonly WebSocketConnectionManager _manager;

    public WebSocketServerMiddleware(
        RequestDelegate next,
        WebSocketConnectionManager manager)
    {
        _next = next;
        _manager = manager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        WriteRequestParam(context);
        if(context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("WebSocket Connected");

            string connId = _manager.AddSocket(webSocket);
            await SendConnIdAsync(webSocket, connId);
            await RecieveMessage(webSocket, async (result, buffer) => 
            {
                if(result.MessageType == WebSocketMessageType.Text)
                {
                    Console.WriteLine("Message recieved");
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Message: {message}");
                    await RouteJsonMessageAsync(message);
                    return;
                }
                else if(result.MessageType == WebSocketMessageType.Close
                        && result.CloseStatus != null
                        && _manager.TryRemoveSocket(connId, out WebSocket? removedSocket))
                {
                    Console.WriteLine("Recieved Close message");
                    await removedSocket.CloseAsync(
                        result.CloseStatus.Value,
                        result.CloseStatusDescription,
                    CancellationToken.None);
                    return;
                }
            });
        }
        else 
        {        
            Console.WriteLine("Hello from 2nd request delegate!");
            await _next(context);   
        }
    }

    private async Task SendConnIdAsync(WebSocket socket, string connId)
    {
        var buffer = Encoding.UTF8.GetBytes("ConnId: "+ connId);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private void WriteRequestParam (HttpContext context) 
    {
        Console.WriteLine("Request Method: " + context.Request.Method);
        Console.WriteLine("Request Protocol: " + context.Request.Protocol);

        if(context.Request.Headers != null)
        {
            foreach(var h in context.Request.Headers)
            {
                Console.WriteLine("--> " + h.Key + " : " + h.Value);
            }
        }
    }

    private async static Task RecieveMessage(
        WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
    {
        var buffer = new byte[1024 *4];

        while(socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(
                buffer: new ArraySegment<byte>(buffer),
                CancellationToken.None);
                 
            handleMessage(result, buffer);
        }
    }

    public async Task RouteJsonMessageAsync(string messageInput)
    {
        var message = JsonSerializer.Deserialize<Message>(messageInput)!;

        if (Guid.TryParse(message.To, out Guid recipientGuid))
        {
            Console.WriteLine("Targeted");
            WebSocket reciever = _manager.GetSocket(message.To);
            if (reciever != null)
            {
                if (reciever.State == WebSocketState.Open)
                    await reciever.SendTextMessageAsync(message.Body);
            }
            else
            {
                WebSocket sender = _manager.GetSocket(message.From);
                string errorMessage = "Invalid Recipient";
                Console.WriteLine(errorMessage);
                await sender.SendTextMessageAsync(errorMessage);
            }
        }

        else
        {
            Console.WriteLine("Broadcast");
            foreach (var socket in _manager.GetAllSockets())
            {
                if (socket.State == WebSocketState.Open)
                    await socket.SendTextMessageAsync(message.Body); 
            }
        }
    }
}