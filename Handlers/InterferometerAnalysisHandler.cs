using Conduit.AsComm.Attributes;
using Conduit.AsComm.Messages;
using Conduit.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler para el tag de análisis del interferómetro.
/// Usa UNSOLICITED mode (10ms polling) para respuesta rápida.
/// 
/// Mismo tag que en el ejemplo de JNJ:
/// "Program:UDT_NGP_INTERFEROMETER_ANALYSIS_TAG"
/// </summary>
[AsCommSubscribe("plc1", "Program:UDT_NGP_INTERFEROMETER_ANALYSIS_TAG", mode: TagSubscriptionMode.Unsolicited)]
public class InterferometerAnalysisHandler : IMessageSubscriptionHandler<TagValue<STRUCT_interferometer_analysis>>
{
    private readonly ILogger<InterferometerAnalysisHandler> _logger;
    private int _updateCount = 0;

    public InterferometerAnalysisHandler(ILogger<InterferometerAnalysisHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        TagValue<STRUCT_interferometer_analysis> message,
        IMessageContext context,
        CancellationToken ct)
    {
        if (message.Quality != TagQuality.Good)
        {
            _logger.LogWarning("⚠️ Tag quality: {Quality}", message.Quality);
            return Task.CompletedTask;
        }

        _updateCount++;
        var analysis = message.Value;

        _logger.LogInformation(
            "📊 [#{Count}] Interferometer Analysis | MeasurementStatus: {Status} | Result: {Result}",
            _updateCount,
            analysis.MeasurementStatus,
            analysis.Result);

        // Detectar cambios
        if (message.PreviousValue != null)
        {
            var prev = message.PreviousValue;
            if (prev.MeasurementStatus != analysis.MeasurementStatus)
            {
                _logger.LogWarning(
                    "🔄 MeasurementStatus changed: {Old} → {New}",
                    prev.MeasurementStatus,
                    analysis.MeasurementStatus);
            }

            if (prev.Result != analysis.Result)
            {
                _logger.LogWarning(
                    "🔄 Result changed: {Old} → {New}",
                    prev.Result,
                    analysis.Result);
            }
        }

        return Task.CompletedTask;
    }
}
