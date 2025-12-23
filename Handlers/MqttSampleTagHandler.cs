using Sitas.Edge.EdgePlcDriver.Messages;
using Sitas.Edge.Core.Abstractions;
using Sitas.Edge.Core.Attributes;
using Sitas.Edge.Core.Enums;
using Sitas.Edge.Mqtt;
using Sitas.Edge.Mqtt.Attributes;
using ConduitPlcDemo;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que se suscribe a MQTT para recibir actualizaciones del tag ngpSampleCurrent.
/// Este handler recibe los mensajes publicados por SampleTagHandler a través de MQTT.
/// 
/// Topic: "ngpSampleCurrent"
/// Mensaje: TagValue&lt;STRUCT_samples&gt;
/// </summary>
[MqttSubscribe("mqtt", "ngpSampleCurrent", QualityOfService.AtLeastOnce)]
public class MqttSampleTagHandler : IMessageSubscriptionHandler<TagValue<STRUCT_samples>>
{
    private int _receivedCount = 0;
    private readonly ILogger<MqttSampleTagHandler> _logger;

    public MqttSampleTagHandler(ILogger<MqttSampleTagHandler> logger)
    {
        _logger = logger;
        _logger.LogInformation("✅ MqttSampleTagHandler instantiated - ready to receive MQTT messages");
    }

    public async Task HandleAsync(
        TagValue<STRUCT_samples> message,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _receivedCount++;

        if (message.Quality != TagQuality.Good)
        {
            _logger.LogWarning("⚠️ Received MQTT message with quality: {Quality}", message.Quality);
            return;
        }

        var sample = message.Value;

        _logger.LogInformation(
            "📨 [#{Count}] MQTT Sample Update Received | SampleId: {SampleId} | SampledOn: {SampledOn} | SampledBy: {SampledBy} | Topic: {Topic}",
            _receivedCount,
            sample.Data.SampleId.Value,
            sample.Data.SampledOn.Value,
            sample.Data.SampledBy.Value,
            context.Topic);

        // Mostrar info del primer pallet si existe
        if (sample.Pallets?.Length > 0)
        {
            var pallet = sample.Pallets[0];
            _logger.LogInformation(
                "   └─ Pallet[0] | RFID: {Rfid} | Type: {Type} | Curvature: {Curvature}",
                pallet.Data.Rfid.Value,
                pallet.Data.CasetteType.Value,
                pallet.Data.Curvature.Value);

            // Mostrar info de la primera cavity si existe
            if (pallet.Cavities?.Length > 0)
            {
                var cavity = pallet.Cavities[0];
                _logger.LogInformation(
                    "      └─ Cavity[0] | ID: {Id} | Site: {Site} | Lot: {Lot}",
                    cavity.Identifier,
                    cavity.SiteNumber,
                    cavity.LotNumber.Value);
            }
        }

        // Aquí puedes agregar lógica adicional para procesar el mensaje recibido de MQTT
        // Por ejemplo: almacenar en base de datos, notificar a otros servicios, etc.
    }
}
