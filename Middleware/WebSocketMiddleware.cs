using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ConduitPlcDemo.Services;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Middleware;

/// <summary>
/// Middleware para manejar conexiones WebSocket.
/// </summary>
public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Services.WebSocketManager _webSocketManager;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(
        RequestDelegate next,
        Services.WebSocketManager webSocketManager,
        ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _webSocketManager = webSocketManager;
        _logger = logger;
        Console.WriteLine($"🔧 WebSocketMiddleware constructor called. WebSocketManager instance ID: {_webSocketManager.GetHashCode()}");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Si es una solicitud WebSocket, manejarla
        if (context.WebSockets.IsWebSocketRequest)
        {
            Console.WriteLine($"🔌 WebSocket connection request received from {context.Request.Path}");
            _logger.LogInformation("🔌 WebSocket connection request received from {Path}", context.Request.Path);
            
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = Guid.NewGuid().ToString();
            
            Console.WriteLine($"✅ WebSocket connection accepted: {connectionId}");
            _logger.LogInformation("✅ WebSocket connection accepted: {ConnectionId}", connectionId);
            _webSocketManager.AddSocket(connectionId, webSocket);
            
            try
            {
                await HandleWebSocketAsync(webSocket, connectionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in WebSocket connection {connectionId}: {ex.Message}");
                _logger.LogError(ex, "❌ Error in WebSocket connection {ConnectionId}", connectionId);
            }
            finally
            {
                _webSocketManager.RemoveSocket(connectionId);
                Console.WriteLine($"🔌 WebSocket connection closed: {connectionId}");
                _logger.LogInformation("🔌 WebSocket connection closed: {ConnectionId}", connectionId);
            }
        }
        else
        {
            // Si no es WebSocket, continuar con el siguiente middleware
            await _next(context);
        }
    }

    private async Task HandleWebSocketAsync(WebSocket webSocket, string connectionId)
    {
        var buffer = new byte[1024 * 4];
        
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Connection closed by client",
                    CancellationToken.None);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessageAsync(message, connectionId);
            }
        }
    }

    private async Task ProcessMessageAsync(string message, string connectionId)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();
                
                switch (type)
                {
                    case "subscribe":
                        if (root.TryGetProperty("tagName", out var tagNameElement))
                        {
                            var tagName = tagNameElement.GetString();
                            if (!string.IsNullOrEmpty(tagName))
                            {
                                Console.WriteLine($"📥 WebSocket subscribe request received: ConnectionId={connectionId}, TagName={tagName}");
                                _webSocketManager.SubscribeToTag(connectionId, tagName);
                                _logger.LogInformation("✅ WebSocket {ConnectionId} subscribed to tag '{TagName}'", connectionId, tagName);
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ Subscribe request received but tagName is null or empty");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Subscribe request received but no tagName property found");
                        }
                        break;

                    case "unsubscribe":
                        if (root.TryGetProperty("tagName", out var unsubscribeTagElement))
                        {
                            var unsubscribeTag = unsubscribeTagElement.GetString();
                            if (!string.IsNullOrEmpty(unsubscribeTag))
                            {
                                _webSocketManager.UnsubscribeFromTag(connectionId, unsubscribeTag);
                                _logger.LogInformation("❌ WebSocket {ConnectionId} unsubscribed from tag '{TagName}'", connectionId, unsubscribeTag);
                            }
                        }
                        break;

                    case "ping":
                        // Responder con pong para mantener la conexión viva
                        var pongResponse = JsonSerializer.Serialize(new { type = "pong" });
                        var pongBytes = Encoding.UTF8.GetBytes(pongResponse);
                        // No podemos enviar aquí directamente, pero el cliente puede verificar la conexión
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error processing WebSocket message from {ConnectionId}: {Message}", connectionId, message);
        }
    }
}

