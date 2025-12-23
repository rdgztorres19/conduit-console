using Sitas.Edge.EdgePlcDriver.Attributes;
using Sitas.Edge.EdgePlcDriver.Messages;
using Sitas.Edge.Core.Abstractions;
using Sitas.Edge.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler simple para leer el lotNumber de la primera cavity.
/// </summary>
[DisableHandler]
[EdgePlcDriverSubscribe("plc1", "ngpSampleCurrent.pallets[0].cavities[0].lotNumber", pollingIntervalMs: 1000, OnChangeOnly = false)]
public class LotNumberHandler : IMessageSubscriptionHandler<TagValue<LOGIX_STRING>>
{
    private readonly ILogger<LotNumberHandler> _logger;

    public LotNumberHandler(ILogger<LotNumberHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(
        TagValue<LOGIX_STRING> message,
        IMessageContext context,
        CancellationToken ct)
    {
        var lotNumber = message.Value?.Value ?? string.Empty;
        
        _logger.LogInformation("📦 Lot Number: {LotNumber}", lotNumber);

        return Task.CompletedTask;
    }
}
