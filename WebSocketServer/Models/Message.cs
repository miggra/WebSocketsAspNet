namespace WebSocketServer.Models;

public record Message (string From, string To, string Body);