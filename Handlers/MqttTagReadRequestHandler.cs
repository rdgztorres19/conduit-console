using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Conduit.Core.Enums;
using Conduit.EdgePlcDriver;
using Conduit.Mqtt;
using Conduit.Mqtt.Attributes;
using ConduitPlcDemo.Messages;
using ConduitPlcDemo;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que se suscribe a MQTT para recibir peticiones de lectura de tags.
/// Cuando recibe una petición, lee el tag del PLC y publica la respuesta por MQTT.
/// 
/// Topic de suscripción: "plc/read-request"
/// Topic de respuesta: "plc/read-response"
/// </summary>
[MqttSubscribe("mqtt", "plc/read-request", QualityOfService.AtLeastOnce)]
public class MqttTagReadRequestHandler : IMessageSubscriptionHandler<TagReadRequest>
{
    private readonly ILogger<MqttTagReadRequestHandler> _logger;
    private readonly IEdgePlcDriver _plcConnection;
    private readonly IMqttConnection _mqtt;
    private int _requestCount = 0;

    public MqttTagReadRequestHandler(
        ILogger<MqttTagReadRequestHandler> logger,
        IEdgePlcDriver plcConnection,
        IMqttConnection mqtt)
    {
        _logger = logger;
        _plcConnection = plcConnection;
        _mqtt = mqtt;
        _logger.LogInformation("✅ MqttTagReadRequestHandler instantiated - ready to receive tag read requests");
    }

    public async Task HandleAsync(
        TagReadRequest request,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[MqttTagReadRequestHandler] 🚀 HandleAsync START - Request #{_requestCount + 1}");
        _requestCount++;

        _logger.LogInformation(
            "🔔 MqttTagReadRequestHandler.HandleAsync called | Request #{Count} | Topic: {Topic} | Request: {Request}",
            _requestCount,
            context.Topic,
            System.Text.Json.JsonSerializer.Serialize(request));
        
        Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Logged initial message | Request #{_requestCount} | Topic: {context.Topic}");
        Console.WriteLine($"[MqttTagReadRequestHandler] 📄 Request content: TagName='{request.TagName}', CorrelationId='{request.CorrelationId}'");
        
        // Log adicional para diagnóstico
        _logger.LogDebug(
            "📨 Message received | Payload length: {Length} | Topic: {Topic} | Request object: {RequestJson}",
            context is IMessageContext ctx ? ctx.RawPayload.Length : 0,
            context.Topic,
            System.Text.Json.JsonSerializer.Serialize(request));

        Console.WriteLine($"[MqttTagReadRequestHandler] 🔍 Checking if TagName is empty...");
        if (string.IsNullOrWhiteSpace(request.TagName))
        {
            Console.WriteLine($"[MqttTagReadRequestHandler] ⚠️ TagName is empty! Sending error response...");
            _logger.LogWarning("⚠️ Received empty tag name in request #{Count}", _requestCount);
            
            // Publicar respuesta de error
            var errorResponse = new TagReadResponse
            {
                TagName = string.Empty,
                Quality = "Bad",
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                HasError = true,
                ErrorMessage = "Tag name is required"
            };

            Console.WriteLine($"[MqttTagReadRequestHandler] 📤 Publishing error response to 'plc/read-response'...");
            await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
            Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Error response published. RETURNING.");
            return;
        }
        
        Console.WriteLine($"[MqttTagReadRequestHandler] ✅ TagName is valid: '{request.TagName}'");

        _logger.LogInformation(
            "📥 [#{Count}] Received tag read request | Tag: {TagName} | CorrelationId: {CorrelationId} | Topic: {Topic}",
            _requestCount,
            request.TagName,
            request.CorrelationId ?? "N/A",
            context.Topic);

        Console.WriteLine($"[MqttTagReadRequestHandler] 🔄 Starting PLC read process for tag: '{request.TagName}'");

        try
        {
            Console.WriteLine($"[MqttTagReadRequestHandler] 🔌 Checking PLC connection... IsConnected: {_plcConnection.IsConnected}, State: {_plcConnection.State}");
            if (!_plcConnection.IsConnected)
            {
                Console.WriteLine($"[MqttTagReadRequestHandler] ⚠️ PLC NOT CONNECTED! Sending error response...");
                _logger.LogWarning("⚠️ PLC not connected, cannot read tag {TagName}", request.TagName);
                
                var errorResponse = new TagReadResponse
                {
                    TagName = request.TagName,
                    Quality = "CommError",
                    Timestamp = DateTimeOffset.UtcNow,
                    CorrelationId = request.CorrelationId,
                    HasError = true,
                    ErrorMessage = $"PLC not connected. State: {_plcConnection.State}"
                };

                Console.WriteLine($"[MqttTagReadRequestHandler] 📤 Publishing PLC disconnected error to 'plc/read-response'...");
                await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
                Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Error response published. RETURNING.");
                return;
            }
            
            Console.WriteLine($"[MqttTagReadRequestHandler] ✅ PLC is connected! Proceeding to read tag...");

            // Leer el tag del PLC dinámicamente
            // Intentar leer como STRUCT_samples si es ngpSampleCurrent, sino como object
            TagReadResponse response;

            if (request.TagName.Equals("ngpSampleCurrent", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[MqttTagReadRequestHandler] 📖 Reading tag '{request.TagName}' as STRUCT_samples...");
                var result = await _plcConnection.ReadTagAsync<STRUCT_samples>(request.TagName, cancellationToken);
                Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Tag read successful! Quality: {result.Quality}, TagName: {result.TagName}");
                
                response = new TagReadResponse
                {
                    TagName = result.TagName,
                    Value = result.Value,
                    Quality = result.Quality.ToString(),
                    Timestamp = result.Timestamp,
                    CorrelationId = request.CorrelationId,
                    HasError = result.Quality != Conduit.EdgePlcDriver.Messages.TagQuality.Good
                };
                Console.WriteLine($"[MqttTagReadRequestHandler] 📝 Response created - HasError: {response.HasError}");
            }
            else
            {
                // Para otros tags, leer como object
                Console.WriteLine($"[MqttTagReadRequestHandler] 📖 Reading tag '{request.TagName}' as object...");
                var result = await _plcConnection.ReadTagAsync<object>(request.TagName, cancellationToken);
                Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Tag read successful! Quality: {result.Quality}, TagName: {result.TagName}");
                
                response = new TagReadResponse
                {
                    TagName = result.TagName,
                    Value = result.Value,
                    Quality = result.Quality.ToString(),
                    Timestamp = result.Timestamp,
                    CorrelationId = request.CorrelationId,
                    HasError = result.Quality != Conduit.EdgePlcDriver.Messages.TagQuality.Good
                };
                Console.WriteLine($"[MqttTagReadRequestHandler] 📝 Response created - HasError: {response.HasError}");
            }

            // Publicar la respuesta por MQTT
            Console.WriteLine($"[MqttTagReadRequestHandler] 📤 Publishing response to 'plc/read-response'...");
            await _mqtt.Publisher.PublishAsync("plc/read-response", response, cancellationToken: cancellationToken);
            Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Response published successfully!");

            _logger.LogInformation(
                "📤 Published tag read response | Tag: {TagName} | Quality: {Quality} | CorrelationId: {CorrelationId}",
                response.TagName,
                response.Quality,
                response.CorrelationId ?? "N/A");
            
            Console.WriteLine($"[MqttTagReadRequestHandler] 🎉 HandleAsync COMPLETED successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MqttTagReadRequestHandler] ❌ EXCEPTION caught: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[MqttTagReadRequestHandler] Stack trace: {ex.StackTrace}");
            _logger.LogError(ex, "❌ Error reading tag {TagName} from PLC", request.TagName);

            var errorResponse = new TagReadResponse
            {
                TagName = request.TagName,
                Quality = "CommError",
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                HasError = true,
                ErrorMessage = ex.Message
            };

            Console.WriteLine($"[MqttTagReadRequestHandler] 📤 Publishing exception error response to 'plc/read-response'...");
            await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
            Console.WriteLine($"[MqttTagReadRequestHandler] ✅ Exception error response published.");
        }
        
        Console.WriteLine($"[MqttTagReadRequestHandler] 🏁 HandleAsync END - Request #{_requestCount}");
    }
}
