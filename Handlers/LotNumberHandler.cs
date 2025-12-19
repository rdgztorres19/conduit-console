using Conduit.AsComm.Attributes;
using Conduit.AsComm.Messages;
using Conduit.Core.Abstractions;
using Conduit.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler simple para leer el lotNumber de la primera cavity.
/// </summary>
// [DisableHandler]
[AsCommSubscribe("plc1", "ngpSampleCurrent.pallets[0].cavities[0].lotNumber", pollingIntervalMs: 1000, OnChangeOnly = false)]
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
