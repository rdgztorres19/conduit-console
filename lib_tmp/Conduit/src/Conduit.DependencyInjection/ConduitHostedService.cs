using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Conduit.Core.Abstractions;

namespace Conduit.DependencyInjection;

/// <summary>
/// Hosted service that manages the lifecycle of all Nexus connections.
/// Automatically connects on startup and disconnects on shutdown.
/// </summary>
internal sealed class ConduitHostedService : IHostedService
{
    private readonly IConduit _nexus;
    private readonly ILogger<ConduitHostedService> _logger;

    public ConduitHostedService(IConduit nexus, ILogger<ConduitHostedService> logger)
    {
        _nexus = nexus;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Nexus Service Bus connections...");
        
        try
        {
            await _nexus.ConnectAllAsync(cancellationToken);
            _logger.LogInformation(
                "Nexus Service Bus started with {Count} connection(s)",
                _nexus.Connections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Nexus Service Bus connections");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Nexus Service Bus connections...");
        
        try
        {
            await _nexus.DisconnectAllAsync(cancellationToken);
            await _nexus.DisposeAsync();
            _logger.LogInformation("Nexus Service Bus stopped");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping Nexus Service Bus");
        }
    }
}
