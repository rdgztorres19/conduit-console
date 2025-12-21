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
        _requestCount++;

        if (string.IsNullOrWhiteSpace(request.TagName))
        {
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

            await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
            return;
        }

        _logger.LogInformation(
            "📥 [#{Count}] Received tag read request | Tag: {TagName} | CorrelationId: {CorrelationId} | Topic: {Topic}",
            _requestCount,
            request.TagName,
            request.CorrelationId ?? "N/A",
            context.Topic);

        try
        {
            if (!_plcConnection.IsConnected)
            {
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

                await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
                return;
            }

            // Leer el tag del PLC dinámicamente
            // Intentar leer como STRUCT_samples si es ngpSampleCurrent, sino como object
            TagReadResponse response;

            if (request.TagName.Equals("ngpSampleCurrent", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _plcConnection.ReadTagAsync<STRUCT_samples>(request.TagName, cancellationToken);
                
                response = new TagReadResponse
                {
                    TagName = result.TagName,
                    Value = result.Value,
                    Quality = result.Quality.ToString(),
                    Timestamp = result.Timestamp,
                    CorrelationId = request.CorrelationId,
                    HasError = result.Quality != Conduit.EdgePlcDriver.Messages.TagQuality.Good
                };
            }
            else
            {
                // Para otros tags, leer como object
                var result = await _plcConnection.ReadTagAsync<object>(request.TagName, cancellationToken);
                
                response = new TagReadResponse
                {
                    TagName = result.TagName,
                    Value = result.Value,
                    Quality = result.Quality.ToString(),
                    Timestamp = result.Timestamp,
                    CorrelationId = request.CorrelationId,
                    HasError = result.Quality != Conduit.EdgePlcDriver.Messages.TagQuality.Good
                };
            }

            // Publicar la respuesta por MQTT
            await _mqtt.Publisher.PublishAsync("plc/read-response", response, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "📤 Published tag read response | Tag: {TagName} | Quality: {Quality} | CorrelationId: {CorrelationId}",
                response.TagName,
                response.Quality,
                response.CorrelationId ?? "N/A");
        }
        catch (Exception ex)
        {
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

            await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
        }
    }
}
