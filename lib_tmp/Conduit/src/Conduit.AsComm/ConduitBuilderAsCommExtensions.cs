using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Conduit.Core;

namespace Conduit.AsComm;

/// <summary>
/// Extension methods for adding ASComm PLC connections to ConduitBuilder.
/// </summary>
public static class ConduitBuilderAsCommExtensions
{
    /// <summary>
    /// Adds an ASComm PLC connection to the Conduit builder.
    /// </summary>
    /// <example>
    /// <code>
    /// var conduit = ConduitBuilder.Create()
    ///     .WithServiceProvider(serviceProvider)
    ///     .AddAsCommConnection(plc => plc
    ///         .WithConnectionName("plc1")
    ///         .WithPlc("192.168.1.10", cpuSlot: 0)
    ///         .WithDefaultPollingInterval(100)
    ///         .WithHandlersFromEntryAssembly())
    ///     .Build();
    /// </code>
    /// </example>
    public static ConduitBuilder AddAsCommConnection(
        this ConduitBuilder builder,
        Action<IAsCommClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddConnection((activator, serviceProvider) =>
        {
            var asCommBuilder = AsCommClientBuilder.Create();
            asCommBuilder.WithHandlerActivator(activator);

            // Configure logging if IServiceProvider is available
            if (serviceProvider is not null)
            {
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                {
                    ((AsCommClientBuilder)asCommBuilder).WithLoggerFactory(loggerFactory);
                }
            }

            configure(asCommBuilder);
            return asCommBuilder.Build();
        });
    }

    /// <summary>
    /// Adds an ASComm PLC connection with a specific connection name.
    /// </summary>
    /// <param name="builder">The Conduit builder.</param>
    /// <param name="connectionName">The logical name for this connection.</param>
    /// <param name="configure">Configuration action for the connection.</param>
    /// <returns>The builder for chaining.</returns>
    public static ConduitBuilder AddAsCommConnection(
        this ConduitBuilder builder,
        string connectionName,
        Action<IAsCommClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(connectionName);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddAsCommConnection(plc =>
        {
            plc.WithConnectionName(connectionName);
            configure(plc);
        });
    }

    /// <summary>
    /// Adds an ASComm PLC connection with minimal configuration.
    /// </summary>
    /// <param name="builder">The Conduit builder.</param>
    /// <param name="connectionName">The logical name for this connection.</param>
    /// <param name="ipAddress">The IP address of the PLC.</param>
    /// <param name="cpuSlot">The CPU slot number (default 0).</param>
    /// <param name="pollingIntervalMs">Default polling interval in milliseconds (default 100).</param>
    /// <returns>The builder for chaining.</returns>
    public static ConduitBuilder AddAsCommConnection(
        this ConduitBuilder builder,
        string connectionName,
        string ipAddress,
        int cpuSlot = 0,
        int pollingIntervalMs = 100)
    {
        return builder.AddAsCommConnection(connectionName, plc => plc
            .WithPlc(ipAddress, cpuSlot)
            .WithDefaultPollingInterval(pollingIntervalMs)
            .WithHandlersFromEntryAssembly());
    }
}
