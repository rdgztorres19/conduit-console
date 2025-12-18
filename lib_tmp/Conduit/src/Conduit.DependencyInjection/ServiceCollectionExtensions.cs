using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Conduit.Core;
using Conduit.Core.Abstractions;
using Conduit.Core.Activators;
using Conduit.Core.Discovery;
using Conduit.Mqtt;

namespace Conduit.DependencyInjection;

/// <summary>
/// Extension methods for configuring Nexus Service Bus with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Nexus Service Bus to the service collection using the fluent builder API.
    /// </summary>
    /// <example>
    /// services.AddConduit(nexus => nexus
    ///     .AddMqttConnection(mqtt => mqtt
    ///         .WithBroker("localhost", 1883)
    ///         .WithHandlersFromEntryAssembly()));
    /// </example>
    public static IServiceCollection AddConduit(
        this IServiceCollection services,
        Action<ConduitBuilder> configure)
    {
        return services.AddConduit(configure, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Adds Nexus Service Bus to the service collection with handler discovery from specified assemblies.
    /// </summary>
    public static IServiceCollection AddConduit(
        this IServiceCollection services,
        Action<ConduitBuilder> configure,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // Register handlers from assemblies
        RegisterHandlers(services, handlerAssemblies);

        // Build and register IConduit
        services.AddSingleton<IConduit>(sp =>
        {
            var builder = ConduitBuilder.Create()
                .WithServiceProvider(sp);
            
            configure(builder);
            
            return builder.Build();
        });

        // Register individual connections for easy access
        services.AddSingleton(sp => sp.GetRequiredService<IConduit>().GetConnection<IMqttConnection>());
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<IMqttConnection>().Publisher);
        services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<IMqttConnection>().Publisher);

        // Auto-connect on startup
        services.AddHostedService<ConduitHostedService>();

        return services;
    }

    /// <summary>
    /// Adds Nexus Service Bus with a custom activator for any DI container.
    /// </summary>
    /// <example>
    /// // Autofac
    /// services.AddConduit(
    ///     type => container.Resolve(type),
    ///     nexus => nexus.AddMqttConnection(mqtt => mqtt.WithBroker("localhost")));
    /// </example>
    public static IServiceCollection AddConduit(
        this IServiceCollection services,
        Func<Type, object> activator,
        Action<ConduitBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(activator);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton<IConduit>(sp =>
        {
            var builder = ConduitBuilder.Create()
                .WithActivator(activator);
            
            configure(builder);
            
            return builder.Build();
        });

        services.AddSingleton(sp => sp.GetRequiredService<IConduit>().GetConnection<IMqttConnection>());
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<IMqttConnection>().Publisher);
        services.AddSingleton<IMqttPublisher>(sp => sp.GetRequiredService<IMqttConnection>().Publisher);
        services.AddHostedService<ConduitHostedService>();

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var discoveryService = new HandlerDiscoveryService();
        var registrations = discoveryService.DiscoverHandlers(assemblies);

        foreach (var registration in registrations)
        {
            services.TryAddTransient(registration.HandlerType);
        }
    }
}
