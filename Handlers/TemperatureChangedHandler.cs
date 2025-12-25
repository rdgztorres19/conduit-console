using Microsoft.Extensions.Logging;
using Sitas.Edge.Core.Events;
using Sitas.Edge.Core.Events.Attributes;
using Sitas.Edge.Core.Abstractions;
using Sitas.Edge.Mqtt;
using Sitas.Edge.EdgePlcDriver.Attributes;
using Sitas.Edge.EdgePlcDriver;

namespace ConduitPlcDemo.Handlers.Events;

/// <summary>
/// Event data for temperature change events.
/// </summary>
public record TemperatureChangedEvent(float Temperature);

/// <summary>
/// Handler que se ejecuta cuando se emite el evento "tempChanged".
/// Publica el cambio de temperatura a un tópico MQTT.
/// </summary>
/// <remarks>
/// Uso: await _mediator.EmitAsync("tempChanged", new TemperatureChangedEvent(25.5f));
/// </remarks>
[Event("tempChanged")]
[EdgePlcDriverRead("plc1", "Sensor_Temperature", typeof(float))]
public class TemperatureChangedHandler : IEventHandler<TemperatureChangedEvent>
{
    private readonly ILogger<TemperatureChangedHandler> _logger;
    private readonly ISitasEdge _conduit;

    public TemperatureChangedHandler(ILogger<TemperatureChangedHandler> logger, ISitasEdge conduit)
    {
        _logger = logger;
        _conduit = conduit;
    }

    public async Task HandleAsync(
        TemperatureChangedEvent eventData,
        TagReadResults tags,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🌡️ Temperature changed: {Temp:F2}°C", eventData.Temperature);
        _logger.LogInformation("🔖 PLC Read Temperature: {TempPLC:F2}°C", tags.Get<float>("Sensor_Temperature"));

        // Publicar a tópico MQTT usando dependencias inyectadas (sin casts/instanceof)
        IMqttConnection mqtt;
        try
        {
            mqtt = _conduit.GetConnection<IMqttConnection>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ No MQTT connection configured. Cannot publish temperature.");
            return;
        }

        var topic = "sensors/temperature";
        var message = new { temperature = eventData.Temperature };

        await mqtt.Publisher.PublishAsync(topic, message, cancellationToken: cancellationToken);
        
        _logger.LogInformation("✅ Published to MQTT topic: {Topic}", topic);
    }
}
