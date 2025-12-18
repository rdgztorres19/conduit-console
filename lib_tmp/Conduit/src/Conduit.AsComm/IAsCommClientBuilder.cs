using System.Reflection;
using Conduit.AsComm.Configuration;
using Conduit.Core.Abstractions;

namespace Conduit.AsComm;

/// <summary>
/// Builder interface for configuring ASComm PLC connections.
/// </summary>
public interface IAsCommClientBuilder : IServiceBusBuilder<IAsCommClientBuilder, IAsCommConnection>
{
    /// <summary>
    /// Sets the logical name for this connection.
    /// Used to match handlers decorated with [AsCommSubscribe] to this connection.
    /// </summary>
    /// <param name="name">The connection name.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithConnectionName(string name);

    /// <summary>
    /// Configures the PLC endpoint.
    /// </summary>
    /// <param name="ipAddress">The IP address of the PLC or ENET/ENBT module.</param>
    /// <param name="cpuSlot">The slot number of the CPU. Default is 0.</param>
    /// <param name="backplane">The backplane number. Default is 1.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithPlc(string ipAddress, int cpuSlot = 0, int backplane = 1);

    /// <summary>
    /// Sets the default polling interval for tag subscriptions.
    /// </summary>
    /// <param name="intervalMs">Polling interval in milliseconds.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithDefaultPollingInterval(int intervalMs);

    /// <summary>
    /// Configures connection timeout.
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithConnectionTimeout(int timeoutSeconds);

    /// <summary>
    /// Configures automatic reconnection behavior.
    /// </summary>
    /// <param name="enabled">Whether to enable auto-reconnect.</param>
    /// <param name="maxDelaySeconds">Maximum delay between reconnection attempts.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithAutoReconnect(bool enabled = true, int maxDelaySeconds = 30);

    /// <summary>
    /// Configures a custom message serializer for UDT handling.
    /// </summary>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithSerializer(IMessageSerializer serializer);

    /// <summary>
    /// Configures the logger factory for internal logging.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithLoggerFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory);

    /// <summary>
    /// Configures the handler activator for dependency injection.
    /// </summary>
    /// <param name="activator">The handler activator.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithHandlerActivator(IHandlerActivator activator);

    /// <summary>
    /// Discovers and registers message handlers from the specified assemblies.
    /// Handlers must be decorated with [AsCommSubscribe] attribute.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithHandlersFromAssemblies(params Assembly[] assemblies);

    /// <summary>
    /// Discovers and registers message handlers from the entry assembly.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithHandlersFromEntryAssembly();

    /// <summary>
    /// Applies configuration from an options object.
    /// </summary>
    /// <param name="options">The connection options.</param>
    /// <returns>The builder for chaining.</returns>
    IAsCommClientBuilder WithOptions(AsCommConnectionOptions options);
}
