using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Conduit.AsComm.Attributes;
using Conduit.AsComm.Configuration;
using Conduit.AsComm.Internal;
using Conduit.Core.Abstractions;
using Conduit.Core.Activators;
using Conduit.Core.Internal;
using Conduit.Core.Serialization;

namespace Conduit.AsComm;

/// <summary>
/// Builder for configuring and creating ASComm PLC connections.
/// </summary>
public sealed class AsCommClientBuilder : IAsCommClientBuilder
{
    private readonly AsCommConnectionOptions _options = new();
    private readonly List<TagHandlerRegistration> _handlerRegistrations = [];

    private IMessageSerializer _serializer = JsonMessageSerializer.Default;
    private IHandlerResolver _handlerResolver = ActivatorHandlerResolver.Instance;
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    /// <summary>
    /// Creates a new ASComm client builder.
    /// </summary>
    public static IAsCommClientBuilder Create() => new AsCommClientBuilder();

    private AsCommClientBuilder()
    {
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithConnectionName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _options.ConnectionName = name;
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithPlc(string ipAddress, int cpuSlot = 0, int backplane = 1)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);
        _options.IpAddress = ipAddress;
        _options.CpuSlot = cpuSlot;
        _options.Backplane = backplane;
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithDefaultPollingInterval(int intervalMs)
    {
        if (intervalMs < 1)
            throw new ArgumentOutOfRangeException(nameof(intervalMs), "Polling interval must be at least 1ms");

        _options.DefaultPollingIntervalMs = intervalMs;
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithConnectionTimeout(int timeoutSeconds)
    {
        _options.ConnectionTimeoutSeconds = timeoutSeconds;
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithAutoReconnect(bool enabled = true, int maxDelaySeconds = 30)
    {
        _options.AutoReconnect = enabled;
        _options.MaxReconnectDelaySeconds = maxDelaySeconds;
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithSerializer(IMessageSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithHandlerActivator(IHandlerActivator activator)
    {
        ArgumentNullException.ThrowIfNull(activator);
        _handlerResolver = new HandlerActivatorAdapter(activator);
        return this;
    }

    /// <summary>
    /// Configures the logger factory for logging.
    /// </summary>
    public IAsCommClientBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithHandlersFromAssemblies(params Assembly[] assemblies)
    {
        var registrations = DiscoverHandlers(assemblies);

        // Filter registrations for this connection
        var relevantRegistrations = registrations
            .Where(r => r.ConnectionName.Equals(_options.ConnectionName, StringComparison.OrdinalIgnoreCase))
            .Select(r => new TagHandlerRegistration
            {
                TagName = r.TagName,
                HandlerType = r.HandlerType,
                MessageType = r.MessageType,
                PollingIntervalMs = r.PollingIntervalMs,
                OnChangeOnly = r.OnChangeOnly,
                Deadband = r.Deadband,
                Mode = r.Mode
            });

        _handlerRegistrations.AddRange(relevantRegistrations);
        return this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithHandlersFromEntryAssembly()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return entryAssembly is not null
            ? WithHandlersFromAssemblies(entryAssembly)
            : this;
    }

    /// <inheritdoc />
    public IAsCommClientBuilder WithOptions(AsCommConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options.ConnectionName = options.ConnectionName;
        _options.IpAddress = options.IpAddress;
        _options.CpuSlot = options.CpuSlot;
        _options.Backplane = options.Backplane;
        _options.DefaultPollingIntervalMs = options.DefaultPollingIntervalMs;
        _options.ConnectionTimeoutSeconds = options.ConnectionTimeoutSeconds;
        _options.AutoReconnect = options.AutoReconnect;
        _options.MaxReconnectDelaySeconds = options.MaxReconnectDelaySeconds;

        return this;
    }

    /// <summary>
    /// Builds the ASComm connection with the configured options.
    /// </summary>
    public IAsCommConnection Build()
    {
        if (string.IsNullOrEmpty(_options.IpAddress))
        {
            throw new InvalidOperationException(
                "PLC IP address must be configured. Call WithPlc() before Build().");
        }

        return new AsCommConnection(
            _options,
            _handlerRegistrations,
            _serializer,
            _handlerResolver,
            _loggerFactory.CreateLogger<AsCommConnection>());
    }

    IAsCommConnection IServiceBusBuilder<IAsCommClientBuilder, IAsCommConnection>.Build() => Build();

    private static IEnumerable<AsCommHandlerInfo> DiscoverHandlers(Assembly[] assemblies)
    {
        var handlerInterface = typeof(IMessageSubscriptionHandler<>);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                // 🚫 Skip handlers marked with [DisableHandler]
                if (type.IsDefined(typeof(Core.Attributes.DisableHandlerAttribute), inherit: false))
                    continue;

                var attributes = type.GetCustomAttributes<AsCommSubscribeAttribute>();

                foreach (var attr in attributes)
                {
                    // Find the IMessageHandler<T> interface to get the message type
                    var messageType = type.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                        .Select(i => i.GetGenericArguments()[0])
                        .FirstOrDefault();

                    if (messageType is null)
                        continue;

                    yield return new AsCommHandlerInfo
                    {
                        ConnectionName = attr.ConnectionName,
                        TagName = attr.Topic, // Topic is the tag name for AsComm
                        HandlerType = type,
                        MessageType = messageType,
                        PollingIntervalMs = attr.PollingIntervalMs,
                        OnChangeOnly = attr.OnChangeOnly,
                        Deadband = attr.Deadband,
                        Mode = attr.Mode
                    };
                }
            }
        }
    }

    private sealed class AsCommHandlerInfo
    {
        public required string ConnectionName { get; init; }
        public required string TagName { get; init; }
        public required Type HandlerType { get; init; }
        public required Type MessageType { get; init; }
        public int PollingIntervalMs { get; init; }
        public bool OnChangeOnly { get; init; }
        public double Deadband { get; init; }
        public Attributes.TagSubscriptionMode Mode { get; init; }
    }
}
