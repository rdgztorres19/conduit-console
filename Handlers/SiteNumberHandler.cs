using Conduit.AsComm.Attributes;
using Conduit.AsComm.Messages;
using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que lee solo el campo siteNumber de la primera cavity del primer pallet.
/// Lee directamente el path: ngpSampleCurrent.pallets[0].cavities[0].siteNumber
/// </summary>
[DisableHandler] 
[AsCommSubscribe("plc1", "ngpSampleCurrent.pallets[0].cavities[0].siteNumber", pollingIntervalMs: 1000, OnChangeOnly = false)]
[AsCommSubscribe("plc1", "ngpSampleCurrent.pallets[0].cavities[0].siteNumber", pollingIntervalMs: 1000, OnChangeOnly = false)]
public class SiteNumberHandler : IMessageSubscriptionHandler<TagValue<string>>
{
    private readonly ILogger<SiteNumberHandler> _logger;
    private int _updateCount = 0;

    public SiteNumberHandler(ILogger<SiteNumberHandler> logger)
    {
        _logger = logger;
        _logger.LogInformation("🚀 SiteNumberHandler instance created");
    }

    public Task HandleAsync(
        TagValue<string> message,
        IMessageContext context,
        CancellationToken ct)
    {
        if (message.Quality != TagQuality.Good)
        {
            _logger.LogWarning("⚠️ SiteNumber tag quality: {Quality}", message.Quality);
            return Task.CompletedTask;
        }

        _updateCount++;
        var siteNumber = message.Value;
        var previousValue = message.PreviousValue;

        _logger.LogInformation(
            "🔢 [#{Count}] SiteNumber Update | Current: {Current} | Previous: {Previous} | Changed: {Changed}",
            _updateCount,
            siteNumber,
            previousValue,
            siteNumber != previousValue);

        return Task.CompletedTask;
    }
}
