using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Conduit.Core.Enums;
using Conduit.Mqtt;
using Conduit.Mqtt.Attributes;
using ConduitPlcDemo.Messages;
using ConduitPlcDemo.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Linq;
using System.Text.Json.Serialization;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que se suscribe a MQTT para recibir respuestas de lectura de tags.
/// Envía las actualizaciones al UI mediante WebSocket y también imprime por consola.
/// 
/// Topic de suscripción: "plc/read-response"
/// </summary>
[MqttSubscribe("mqtt", "plc/read-response", QualityOfService.AtLeastOnce)]
public class MqttTagReadResponseHandler : IMessageSubscriptionHandler<TagReadResponse>
{
    private readonly ILogger<MqttTagReadResponseHandler> _logger;
    private readonly Services.WebSocketManager _webSocketManager;
    private int _responseCount = 0;

    public MqttTagReadResponseHandler(
        ILogger<MqttTagReadResponseHandler> logger,
        Services.WebSocketManager webSocketManager)
    {
        _logger = logger;
        _webSocketManager = webSocketManager;
        // Console.WriteLine($"🔧 MqttTagReadResponseHandler constructor called. WebSocketManager instance ID: {_webSocketManager.GetHashCode()}");
        // _logger.LogInformation("✅ MqttTagReadResponseHandler instantiated - ready to receive tag read responses");
    }

    public async Task HandleAsync(
        TagReadResponse response,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _responseCount++;

        // Console.WriteLine("═══════════════════════════════════════════════════════════");

        // _logger.LogDebug(
        //     "🔔 MqttTagReadResponseHandler.HandleAsync called | Response #{Count} | Topic: {Topic}",
        //     _responseCount,
        //     context.Topic);

        // Enviar actualización por WebSocket a los clientes suscritos al tag
        try
        {
            // Console.WriteLine($"📤 Sending TagReadResponse via WebSocket for tag '{response.TagName}'");
            // _logger.LogInformation("📤 Sending TagReadResponse via WebSocket for tag '{TagName}'", response.TagName);
            
            // Crear mensaje con tipo para que el cliente sepa qué es
            var message = new
            {
                type = "TagReadResponse",
                tagName = response.TagName,
                value = response.Value,
                quality = response.Quality,
                timestamp = response.Timestamp,
                correlationId = response.CorrelationId,
                hasError = response.HasError,
                errorMessage = response.ErrorMessage
            };
            
            // Enviar a todos los clientes suscritos al tag
            await _webSocketManager.SendToTagAsync(response.TagName, message, cancellationToken);
            
            // Console.WriteLine($"✅ TagReadResponse sent successfully via WebSocket for tag '{response.TagName}'");
            // _logger.LogInformation("✅ TagReadResponse sent successfully via WebSocket for tag '{TagName}'", response.TagName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending to WebSocket: {ex.Message}");
            Console.WriteLine($"   StackTrace: {ex.StackTrace}");
            _logger.LogError(ex, "❌ Error sending read response to WebSocket for tag {TagName}", response.TagName);
        }

        if (response.HasError)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"❌ [#{_responseCount}] TAG READ ERROR");
            Console.WriteLine($"   Tag: {response.TagName}");
            Console.WriteLine($"   Error: {response.ErrorMessage}");
            Console.WriteLine($"   Quality: {response.Quality}");
            Console.WriteLine($"   CorrelationId: {response.CorrelationId ?? "N/A"}");
            Console.WriteLine($"   Timestamp: {response.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"   Topic: {context.Topic}");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            
            _logger.LogWarning(
                "❌ [#{Count}] Tag read error | Tag: {TagName} | Error: {Error} | CorrelationId: {CorrelationId}",
                _responseCount,
                response.TagName,
                response.ErrorMessage,
                response.CorrelationId ?? "N/A");
        }
        else
        {
            // Formatear el valor para mostrar en consola
            string valueDisplay = FormatValue(response.Value);

            // Console.WriteLine("═══════════════════════════════════════════════════════════");
            // Console.WriteLine($"✅ [#{_responseCount}] TAG READ SUCCESS");
            // Console.WriteLine($"   Tag: {response.TagName}");
            // Console.WriteLine($"   Quality: {response.Quality}");
            // Console.WriteLine($"   Timestamp: {response.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            // Console.WriteLine($"   CorrelationId: {response.CorrelationId ?? "N/A"}");
            // Console.WriteLine($"   Topic: {context.Topic}");
            // Console.WriteLine($"   Value:");
            // Console.WriteLine($"   {valueDisplay}");
            // Console.WriteLine("═══════════════════════════════════════════════════════════");

            // _logger.LogInformation(
            //     "✅ [#{Count}] Tag read success | Tag: {TagName} | Quality: {Quality} | CorrelationId: {CorrelationId}",
            //     _responseCount,
            //     response.TagName,
            //     response.Quality,
            //     response.CorrelationId ?? "N/A");
        }
    }

    private string FormatValue(object? value)
    {
        if (value == null)
        {
            return "   null";
        }

        if (value is string str)
        {
            return $"   \"{str}\"";
        }

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var items = enumerable.Cast<object?>().Take(10).ToList();
            var itemsStr = string.Join(", ", items.Select(v => v?.ToString() ?? "null"));
            var more = items.Count == 10 ? "..." : "";
            return $"   [{itemsStr}{more}]";
        }

        // Para objetos complejos, serializar a JSON con indentación
        try
        {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                MaxDepth = 10
            });
            
            // Indentar cada línea
            var lines = json.Split('\n');
            return string.Join("\n   ", lines);
        }
        catch
        {
            return $"   {value}";
        }
    }
}
