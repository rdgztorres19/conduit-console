using Sitas.Edge.Mqtt;
using Sitas.Edge.Core.Enums;
using Sitas.Edge.Core.Abstractions;
using ConduitPlcDemo.Messages.Ignition;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Servicio que se suscribe programáticamente a topics MQTT.
/// Similar a los handlers con atributos, pero controlado por código.
/// </summary>
public class MqttSubscriptionService
{
    private readonly IMqttConnection _mqtt;
    private IAsyncDisposable? _subscription;

    public MqttSubscriptionService(IMqttConnection mqtt)
    {
        _mqtt = mqtt;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("🚀 Starting MQTT programmatic subscriptions...");

        // Suscripción programática al mismo topic que el handler
        _subscription = await _mqtt.SubscribeAsync<IgnitionRealtimeMessage>(
            "sorba_ignition/Ignition01/tags/inputs/realtime",
            HandleRealtimeMessageAsync,
            QualityOfService.AtLeastOnce,
            cancellationToken);

        Console.WriteLine("✅ MQTT subscription active: sorba_ignition/Ignition01/tags/inputs/realtime");
    }

    private async Task HandleRealtimeMessageAsync(
        IgnitionRealtimeMessage message,
        IMessageContext context,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"📥 [SERVICE] Received realtime message | Tags: {message.Count} | Topic: {context.Topic} | Path: {message.First().Path}");

        // Publicar respuesta
        var responseTopic = "sorba_ignition/Ignition01/tags/outputs/service_ack1";

        var ackPayload = new
        {
            receivedAtUtc = DateTimeOffset.UtcNow,
            sourceTopic = context.Topic,
            tagCount = message.Count,
            source = "MqttSubscriptionService"
        };

        await _mqtt.Publisher.PublishAsync(
            responseTopic,
            ackPayload,
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync()
    {
        Console.WriteLine("🛑 Stopping MQTT subscriptions...");
        
        if (_subscription != null)
        {
            await _subscription.DisposeAsync();
            _subscription = null;
        }

        Console.WriteLine("✅ MQTT subscriptions stopped");
    }
}
