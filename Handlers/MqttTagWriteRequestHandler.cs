using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Conduit.Core.Enums;
using Conduit.EdgePlcDriver;
using Conduit.Mqtt;
using Conduit.Mqtt.Attributes;
using ConduitPlcDemo.Messages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que se suscribe a MQTT para recibir peticiones de escritura de tags.
/// Cuando recibe una petición, escribe el valor al path especificado del PLC y publica la respuesta por MQTT.
/// 
/// Topic de suscripción: "plc/write-request"
/// Topic de respuesta: "plc/write-response"
/// </summary>
[MqttSubscribe("mqtt", "plc/write-request", QualityOfService.AtLeastOnce)]
public class MqttTagWriteRequestHandler : IMessageSubscriptionHandler<TagWriteRequest>
{
    private readonly ILogger<MqttTagWriteRequestHandler> _logger;
    private readonly IEdgePlcDriver _plcConnection;
    private readonly IMqttConnection _mqtt;
    private int _requestCount = 0;

    public MqttTagWriteRequestHandler(
        ILogger<MqttTagWriteRequestHandler> logger,
        IEdgePlcDriver plcConnection,
        IMqttConnection mqtt)
    {
        _logger = logger;
        _plcConnection = plcConnection;
        _mqtt = mqtt;

        // Verificar si el PLC está disponible (no es NullEdgePlcDriver)
        if (_plcConnection is Services.NullEdgePlcDriver || !_plcConnection.IsConnected)
        {
            _logger.LogWarning("⚠️ MqttTagWriteRequestHandler: PLC not available. This handler should run on a PC with ASComm license.");
        }
        else
        {
            _logger.LogInformation("✅ MqttTagWriteRequestHandler instantiated - ready to receive tag write requests");
        }
    }

    public async Task HandleAsync(
        TagWriteRequest request,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _requestCount++;

        if (string.IsNullOrWhiteSpace(request.TagName))
        {
            _logger.LogWarning("⚠️ Received empty tag name in write request #{Count}", _requestCount);
            
            var errorResponse = new TagWriteResponse
            {
                TagName = string.Empty,
                Path = request.Path,
                FullTagName = request.Path,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                ErrorMessage = "Tag name is required"
            };

            await _mqtt.Publisher.PublishAsync("plc/write-response", errorResponse, cancellationToken: cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Path))
        {
            _logger.LogWarning("⚠️ Received empty path in write request #{Count}", _requestCount);
            
            var errorResponse = new TagWriteResponse
            {
                TagName = request.TagName,
                Path = string.Empty,
                FullTagName = request.TagName,
                Success = false,
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                ErrorMessage = "Path is required"
            };

            await _mqtt.Publisher.PublishAsync("plc/write-response", errorResponse, cancellationToken: cancellationToken);
            return;
        }

        try
        {
            // Verificar si el PLC está disponible (no es NullEdgePlcDriver)
            if (_plcConnection is Services.NullEdgePlcDriver || !_plcConnection.IsConnected)
            {
                _logger.LogWarning("⚠️ PLC not available on this PC. This handler should run on a PC with ASComm license.");
                
                var errorResponse = new TagWriteResponse
                {
                    TagName = request.TagName,
                    Path = request.Path,
                    FullTagName = $"{request.TagName}.{request.Path}",
                    Success = false,
                    Timestamp = DateTimeOffset.UtcNow,
                    CorrelationId = request.CorrelationId,
                    ErrorMessage = "PLC not available on this PC. This handler should run on a PC with ASComm license."
                };

                await _mqtt.Publisher.PublishAsync("plc/write-response", errorResponse, cancellationToken: cancellationToken);
                return;
            }

            // Construir el tagName completo: tagName.path
            var fullTagName = $"{request.TagName}.{request.Path}";
            
            // Extraer el valor real si viene como JsonElement
            object? rawValue = request.Value;
            if (request.Value is JsonElement jsonElement)
            {
                rawValue = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetInt32(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => jsonElement.GetRawText()
                };
            }

            // Escribir al PLC
            await _plcConnection.WriteTagAsync(fullTagName, rawValue, cancellationToken);

            var successResponse = new TagWriteResponse
            {
                TagName = request.TagName,
                Path = request.Path,
                FullTagName = fullTagName,
                Value = request.Value,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId
            };

            await _mqtt.Publisher.PublishAsync("plc/write-response", successResponse, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error writing tag {TagName}.{Path} to PLC", request.TagName, request.Path);

            var errorResponse = new TagWriteResponse
            {
                TagName = request.TagName,
                Path = request.Path,
                FullTagName = $"{request.TagName}.{request.Path}",
                Success = false,
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                ErrorMessage = ex.Message
            };

            await _mqtt.Publisher.PublishAsync("plc/write-response", errorResponse, cancellationToken: cancellationToken);
        }
    }
}

