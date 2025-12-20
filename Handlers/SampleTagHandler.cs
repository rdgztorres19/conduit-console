using Conduit.EdgePlcDriver.Attributes;
using Conduit.EdgePlcDriver.Messages;
using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Conduit.Mqtt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler para el tag de muestra actual (UDT completo).
/// Usa POLLING mode estándar (1000ms) para monitoreo no crítico.
/// 
/// Tag del ejemplo de JNJ: "ngpSampleCurrent"
/// Este es un UDT complejo con samples, pallets y cavities.
/// </summary>
/// 
// [DisableHandler] 
[EdgePlcDriverSubscribe("plc1", "ngpSampleCurrent", pollingIntervalMs: 1000, OnChangeOnly = true)]
public class SampleTagHandler : IMessageSubscriptionHandler<TagValue<STRUCT_samples>>
{
    private int _updateCount = 0;
    private readonly IMqttConnection _mqtt;
    private readonly ILogger<SampleTagHandler> _logger;


    // Constructor con DI (opcional, si se usa con DI)
    public SampleTagHandler(ILogger<SampleTagHandler> logger, IMqttConnection mqtt)
    {
        _mqtt = mqtt;
        _logger = logger;
        _logger.LogInformation("🚀 SampleTagHandler instance created with DI");
    }

    public Task HandleAsync(
        TagValue<STRUCT_samples> message,
        IMessageContext context,
        CancellationToken ct)
    {
        Console.WriteLine("🔔 SampleTagHandler invoked");

        if (message.Quality != TagQuality.Good)
        {
            _logger.LogWarning("⚠️ Sample tag quality: {Quality}", message.Quality);
            return Task.CompletedTask;
        }

        _updateCount++;
        var sample = message.Value;

        _logger.LogInformation(
            "📦 [#{Count}] Sample Update | SampleId: {SampleId} | SampledOn: {SampledOn} | SampledBy: {SampledBy}",
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

        await _mqtt.Publisher.PublishAsync("ngpSampleCurrent", message, cancellationToken: ct);

        return Task.CompletedTask;
    }
}
