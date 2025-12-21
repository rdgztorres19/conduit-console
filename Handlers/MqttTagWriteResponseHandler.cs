using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Conduit.Core.Enums;
using Conduit.Mqtt;
using Conduit.Mqtt.Attributes;
using ConduitPlcDemo.Messages;
using ConduitPlcDemo.Services;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que se suscribe a MQTT para recibir respuestas de escritura de tags.
/// Envía las actualizaciones al UI mediante WebSocket.
/// 
/// Topic de suscripción: "plc/write-response"
/// </summary>
[MqttSubscribe("mqtt", "plc/write-response", QualityOfService.AtLeastOnce)]
public class MqttTagWriteResponseHandler : IMessageSubscriptionHandler<TagWriteResponse>
{
    private readonly ILogger<MqttTagWriteResponseHandler> _logger;
    private readonly Services.WebSocketManager _webSocketManager;
    private int _responseCount = 0;

    public MqttTagWriteResponseHandler(
        ILogger<MqttTagWriteResponseHandler> logger,
        Services.WebSocketManager webSocketManager)
    {
        _logger = logger;
        _webSocketManager = webSocketManager;
        _logger.LogInformation("✅ MqttTagWriteResponseHandler instantiated - ready to receive tag write responses");
    }

    public async Task HandleAsync(
        TagWriteResponse response,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _responseCount++;

        _logger.LogDebug(
            "🔔 MqttTagWriteResponseHandler.HandleAsync called | Response #{Count} | Tag: {TagName} | Path: {Path} | Success: {Success}",
            _responseCount,
            response.TagName,
            response.Path,
            response.Success);

        try
        {
            // Crear mensaje con tipo para que el cliente sepa qué es
            var message = new
            {
                type = "TagWriteResponse",
                tagName = response.TagName,
                path = response.Path,
                fullTagName = response.FullTagName,
                value = response.Value,
                success = response.Success,
                timestamp = response.Timestamp,
                correlationId = response.CorrelationId,
                errorMessage = response.ErrorMessage
            };
            
            // Enviar actualización por WebSocket a los clientes suscritos al tag
            await _webSocketManager.SendToTagAsync(response.TagName, message, cancellationToken);

            if (response.Success)
            {
                _logger.LogInformation(
                    "✅ [#{Count}] Tag write success | Tag: {TagName} | Path: {Path} | CorrelationId: {CorrelationId}",
                    _responseCount,
                    response.TagName,
                    response.Path,
                    response.CorrelationId ?? "N/A");
            }
            else
            {
                _logger.LogWarning(
                    "❌ [#{Count}] Tag write error | Tag: {TagName} | Path: {Path} | Error: {Error} | CorrelationId: {CorrelationId}",
                    _responseCount,
                    response.TagName,
                    response.Path,
                    response.ErrorMessage,
                    response.CorrelationId ?? "N/A");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error sending write response to WebSocket for tag {TagName}", response.TagName);
        }
    }
}

