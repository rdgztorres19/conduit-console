using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Gestiona las conexiones WebSocket activas y permite enviar mensajes a clientes específicos o a todos.
/// </summary>
public class WebSocketManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _tagSubscriptions = new(); // tagName -> connectionIds
    private readonly ILogger<WebSocketManager> _logger;

    public WebSocketManager(ILogger<WebSocketManager> logger)
    {
        _logger = logger;
        Console.WriteLine($"🔧 WebSocketManager constructor called. Instance ID: {GetHashCode()}");
    }

    /// <summary>
    /// Agrega una conexión WebSocket al manager.
    /// </summary>
    public void AddSocket(string connectionId, WebSocket socket)
    {
        Console.WriteLine($"🔌 AddSocket called: ConnectionId={connectionId}, SocketState={socket.State}");
        _sockets.TryAdd(connectionId, socket);
        Console.WriteLine($"✅ WebSocket connection added: {connectionId} (Total sockets: {_sockets.Count})");
        Console.WriteLine($"   Socket keys: {string.Join(", ", _sockets.Keys)}");
        _logger.LogInformation("✅ WebSocket connection added: {ConnectionId} (Total: {Count})", connectionId, _sockets.Count);
    }

    /// <summary>
    /// Remueve una conexión WebSocket del manager.
    /// </summary>
    public void RemoveSocket(string connectionId)
    {
        Console.WriteLine($"🗑️ RemoveSocket called: ConnectionId={connectionId}");
        if (_sockets.TryRemove(connectionId, out var socket))
        {
            Console.WriteLine($"❌ WebSocket connection removed: {connectionId} (Total sockets: {_sockets.Count})");
            _logger.LogInformation("❌ WebSocket connection removed: {ConnectionId} (Total: {Count})", connectionId, _sockets.Count);
            
            // Limpiar suscripciones de este socket
            foreach (var tagSubscriptions in _tagSubscriptions.Values)
            {
                tagSubscriptions.Remove(connectionId);
            }
        }
        else
        {
            Console.WriteLine($"⚠️ RemoveSocket: ConnectionId {connectionId} not found in sockets dictionary");
        }
    }

    /// <summary>
    /// Suscribe una conexión a un tag específico.
    /// </summary>
    public void SubscribeToTag(string connectionId, string tagName)
    {
        var groupName = $"tag:{tagName}";
        
        Console.WriteLine($"🔔 SubscribeToTag called: ConnectionId={connectionId}, TagName={tagName}, GroupName={groupName}");
        Console.WriteLine($"   Total sockets before: {_sockets.Count}");
        Console.WriteLine($"   Socket exists: {_sockets.ContainsKey(connectionId)}");
        
        _tagSubscriptions.AddOrUpdate(
            groupName,
            new HashSet<string> { connectionId },
            (key, existing) =>
            {
                existing.Add(connectionId);
                return existing;
            });
        
        Console.WriteLine($"   Total subscriptions after: {_tagSubscriptions.Count}");
        Console.WriteLine($"   Subscribers in group '{groupName}': {(_tagSubscriptions.TryGetValue(groupName, out var subs) ? subs.Count : 0)}");
        Console.WriteLine($"   All groups: {string.Join(", ", _tagSubscriptions.Keys)}");
        
        _logger.LogInformation("✅ Connection {ConnectionId} subscribed to tag '{TagName}' (Group: {GroupName})", 
            connectionId, tagName, groupName);
    }

    /// <summary>
    /// Desuscribe una conexión de un tag específico.
    /// </summary>
    public void UnsubscribeFromTag(string connectionId, string tagName)
    {
        var groupName = $"tag:{tagName}";
        if (_tagSubscriptions.TryGetValue(groupName, out var subscribers))
        {
            subscribers.Remove(connectionId);
            if (subscribers.Count == 0)
            {
                _tagSubscriptions.TryRemove(groupName, out _);
            }
            _logger.LogInformation("❌ Connection {ConnectionId} unsubscribed from tag '{TagName}'", connectionId, tagName);
        }
    }

    /// <summary>
    /// Envía un mensaje a todos los clientes suscritos a un tag específico.
    /// </summary>
    public async Task SendToTagAsync(string tagName, object message, CancellationToken cancellationToken = default)
    {
        var groupName = $"tag:{tagName}";
        
        if (!_tagSubscriptions.TryGetValue(groupName, out var subscribers) || subscribers.Count == 0)
        {
            await SendToAllAsync(message, cancellationToken);
            return;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        var json = JsonSerializer.Serialize(message, jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        Console.WriteLine($"📤 Message JSON: {json.Substring(0, Math.Min(200, json.Length))}...");
        Console.WriteLine($"   Message size: {bytes.Length} bytes");

        var tasks = new List<Task>();
        var deadConnections = new List<string>();

        foreach (var connectionId in subscribers)
        {
            if (_sockets.TryGetValue(connectionId, out var socket))
            {
                if (socket.State == WebSocketState.Open)
                {
                    Console.WriteLine($"   ✅ Sending to connection {connectionId} (State: {socket.State})");
                    tasks.Add(SendMessageAsync(socket, bytes, cancellationToken));
                }
                else
                {
                    Console.WriteLine($"   ❌ Connection {connectionId} is not open (State: {socket.State})");
                    deadConnections.Add(connectionId);
                }
            }
            else
            {
                Console.WriteLine($"   ❌ Connection {connectionId} not found in sockets dictionary");
                deadConnections.Add(connectionId);
            }
        }

        // Limpiar conexiones muertas
        foreach (var deadConnection in deadConnections)
        {
            subscribers.Remove(deadConnection);
            _sockets.TryRemove(deadConnection, out _);
            Console.WriteLine($"   🗑️ Removed dead connection: {deadConnection}");
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
        else
        {
            Console.WriteLine($"⚠️ No tasks to send (all connections were dead or not found)");
        }
    }

    /// <summary>
    /// Envía un mensaje a todos los clientes conectados.
    /// </summary>
    public async Task SendToAllAsync(object message, CancellationToken cancellationToken = default)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        var json = JsonSerializer.Serialize(message, jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        var tasks = new List<Task>();
        var deadConnections = new List<string>();

        foreach (var kvp in _sockets)
        {
            var socket = kvp.Value;
            if (socket.State == WebSocketState.Open)
            {
                tasks.Add(SendMessageAsync(socket, bytes, cancellationToken));
            }
            else
            {
                deadConnections.Add(kvp.Key);
            }
        }

        // Limpiar conexiones muertas
        foreach (var deadConnection in deadConnections)
        {
            RemoveSocket(deadConnection);
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendMessageAsync(WebSocket socket, byte[] bytes, CancellationToken cancellationToken)
    {
        try
        {
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error sending WebSocket message");
        }
    }

    /// <summary>
    /// Obtiene el número de conexiones activas.
    /// </summary>
    public int GetConnectionCount() => _sockets.Count;
}

