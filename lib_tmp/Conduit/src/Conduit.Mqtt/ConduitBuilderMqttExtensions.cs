using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Conduit.Core;
using Conduit.Core.Abstractions;
using Conduit.Core.Activators;
using Conduit.Core.Events;
using Conduit.Mqtt.Events;

namespace Conduit.Mqtt;

/// <summary>
/// Extension methods for adding MQTT connections to ConduitBuilder.
/// </summary>
public static class ConduitBuilderMqttExtensions
{
    /// <summary>
    /// Adds an MQTT connection to the Nexus builder.
    /// </summary>
    /// <example>
    /// var nexus = ConduitBuilder.Create()
    ///     .WithServiceProvider(serviceProvider)
    ///     .AddMqttConnection(mqtt => mqtt
    ///         .WithConnectionName("mqtt")
    ///         .WithBroker("localhost", 1883)
    ///         .WithCredentials("user", "password")
    ///         .WithHandlersFromEntryAssembly())
    ///     .Build();
    /// </example>
    public static ConduitBuilder AddMqttConnection(
        this ConduitBuilder builder,
        Action<IMqttClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddConnection((activator, serviceProvider) =>
        {
            var mqttBuilder = MqttClientBuilder.Create();
            mqttBuilder.WithHandlerActivator(activator);
            
            // Configure logging if IServiceProvider is available
            if (serviceProvider is not null)
            {
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                {
                    ((MqttClientBuilder)mqttBuilder).WithLoggerFactory(loggerFactory);
                }
            }
            
            configure(mqttBuilder);
            var connection = mqttBuilder.Build();
            
            // Register MQTT event context factory if TagReaderProvider is available
            if (serviceProvider is not null && connection is IMqttConnection mqttConn)
            {
                var tagReaderProvider = serviceProvider.GetService<ITagReaderProvider>();
                if (tagReaderProvider is not null)
                {
                    // Get connection name from the builder's options
                    var connectionName = ((MqttClientBuilder)mqttBuilder)._options.ConnectionName;
                    if (!string.IsNullOrEmpty(connectionName))
                    {
                        tagReaderProvider.RegisterContextFactory(connectionName, eventName =>
                            new MqttEventContext(eventName, mqttConn.Publisher));
                    }
                }
            }
            
            return connection;
        });
    }

    /// <summary>
    /// Adds an MQTT connection with a specific connection name.
    /// </summary>
    public static ConduitBuilder AddMqttConnection(
        this ConduitBuilder builder,
        string connectionName,
        Action<IMqttClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(connectionName);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.AddMqttConnection(mqtt =>
        {
            mqtt.WithConnectionName(connectionName);
            configure(mqtt);
        });
    }
}
