using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketServer.Middleware;

public class WebSocketConnectionManager 
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets;
        
    public WebSocketConnectionManager()
    {
       _sockets = new ConcurrentDictionary<string, WebSocket>();
    }

    public IEnumerable<WebSocket> GetAllSockets()
    {
        return _sockets.Values;
    }

    public WebSocket GetSocket(string key)
    {
        return _sockets[key];
    }

    public string AddSocket(WebSocket socket)
    {
        string ConnId = Guid.NewGuid().ToString();
        _sockets.TryAdd(ConnId, socket);
        Console.WriteLine("Connection Added: " + ConnId);

        return ConnId;
    }

    public bool TryRemoveSocket(string id, out WebSocket? webSocket )
    {
        return _sockets.TryRemove(id, out webSocket);
    }
}