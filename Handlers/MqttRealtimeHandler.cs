using ConduitPlcDemo.Messages.Ignition;
using Microsoft.Extensions.Logging;
using Sitas.Edge.Core.Abstractions;
using Sitas.Edge.Core.Attributes;
using Sitas.Edge.Core.Enums;
using Sitas.Edge.Mqtt;
using Sitas.Edge.Mqtt.Attributes;
using ConduitPlcDemo.Services;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handles realtime tag updates from Ignition SCADA system.
/// Subscribes to: sorba_ignition/Ignition01/tags/inputs/realtime
/// </summary>
/// 
[DisableHandler]  // 🚫 Uncomment to disable this handler
[MqttSubscribe("mqtt", "sorba_ignition/Ignition01/tags/inputs/realtime", QualityOfService.AtLeastOnce)]
public class MqttRealtimeHandler : IMessageSubscriptionHandler<IgnitionRealtimeMessage>
{
    private readonly ILogger<MqttRealtimeHandler> _logger;
    private readonly IMqttConnection _mqtt;
    private readonly IDataProcessingService _dataProcessingService;

    // 🔧 Conduit auto-inyecta IMqttConnection sin necesidad de registrarlo
    public MqttRealtimeHandler(
        ILogger<MqttRealtimeHandler> logger,
        IDataProcessingService dataProcessingService,
        IMqttConnection mqtt)
    {
        _logger = logger;
        _mqtt = mqtt;
        _dataProcessingService = dataProcessingService;
        _logger.LogInformation("✅ MqttRealtimeHandler instantiated - ready to receive messages");
    }

    public async Task HandleAsync(
        IgnitionRealtimeMessage message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {

        _logger.LogInformation(
            "🏭 Ignition realtime update received | Tags: {TagCount} | Topic: {Topic}",
            message.Count,
            context.Topic);

        // ✅ Publicar usando _mqtt directamente
        var ackTopic = "sorba_ignition/Ignition01/tags/outputs/realtime_ack";
    
        var ackPayload = new
        {
            receivedAtUtc = DateTimeOffset.UtcNow,
            sourceTopic = context.Topic,
            tagCount = message.Count
        };

        await _mqtt.Publisher.PublishAsync(ackTopic, ackPayload, cancellationToken: cancellationToken);

        // ✅ Procesar datos usando el servicio inyectado
        _dataProcessingService.ProcessData(message, "IgnitionRealtime");

        // foreach (var tagUpdate in message)
        // {
        //     ProcessTagUpdate(tagUpdate);
        // }

        // Si quieres publicar algo por cada tag, descomenta arriba y publica dentro de ProcessTagUpdate.
    }
}
