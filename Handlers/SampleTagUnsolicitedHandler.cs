using Sitas.Edge.EdgePlcDriver.Attributes;
using Sitas.Edge.EdgePlcDriver.Messages;
using Sitas.Edge.Core.Abstractions;
using Sitas.Edge.Core.Attributes;
using Sitas.Edge.Mqtt;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler para el tag de muestra actual (UDT completo) usando UNSOLICITED messages.
/// El PLC debe estar configurado con MSG instruction para enviar mensajes no solicitados.
/// 
/// Usa UNSOLICITED mode para recibir cambios inmediatamente cuando el PLC los envía (push).
/// Ideal para datos críticos que requieren latencia mínima.
/// 
/// Tag del ejemplo de JNJ: "ngpSampleCurrent"
/// Este es un UDT complejo con samples, pallets y cavities.
/// 
/// IMPORTANTE: El PLC debe tener configurada una MSG instruction que envíe mensajes
/// no solicitados al tag "ngpSampleCurrent" hacia la IP de esta aplicación.
/// </summary>
[DisableHandler] // Deshabilitado por defecto - habilitar cuando el PLC esté configurado
[EdgePlcDriverSubscribe("plc1", "ngpSampleCurrent", mode: TagSubscriptionMode.Unsolicited)]
public class SampleTagUnsolicitedHandler : IMessageSubscriptionHandler<TagValue<STRUCT_samples>>
{
    private int _updateCount = 0;
    private readonly IMqttConnection _mqtt;
    private readonly ILogger<SampleTagUnsolicitedHandler> _logger;

    // Constructor con DI (opcional, si se usa con DI)
    public SampleTagUnsolicitedHandler(ILogger<SampleTagUnsolicitedHandler> logger, IMqttConnection mqtt)
    {
        _mqtt = mqtt;
        _logger = logger;
        _logger.LogInformation("🚀 SampleTagUnsolicitedHandler instance created with DI");
    }

    public async Task HandleAsync(
        TagValue<STRUCT_samples> message,
        IMessageContext context,
        CancellationToken ct)
    {
        Console.WriteLine("📨 SampleTagUnsolicitedHandler invoked (PLC push)");

        if (message.Quality != TagQuality.Good)
        {
            _logger.LogWarning("⚠️ Sample tag quality: {Quality}", message.Quality);
            return;
        }

        _updateCount++;
        var sample = message.Value;

        _logger.LogInformation(
            "📨 [#{Count}] Sample Update (UNSOLICITED - Pushed from PLC) | SampleId: {SampleId} | SampledOn: {SampledOn} | SampledBy: {SampledBy}",
            _updateCount,
            sample.Data.SampleId.Value,
            sample.Data.SampledOn.Value,
            sample.Data.SampledBy.Value);

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

        // Publicar a MQTT
        await _mqtt.Publisher.PublishAsync("ngpSampleCurrent", message, cancellationToken: ct);
    }
}
