using Conduit.Core.Abstractions;
using Conduit.Core.Activators;

namespace Conduit.Core;

/// <summary>
/// Main entry point for building Nexus Service Bus connections.
/// Provides a fluent API for configuring DI and adding protocol connections.
/// </summary>
/// <example>
/// // With IServiceProvider (any container)
/// var nexus = ConduitBuilder.Create()
///     .WithServiceProvider(serviceProvider)
///     .AddMqttConnection(mqtt => mqtt
///         .WithBroker("localhost", 1883)
///         .WithHandlersFromEntryAssembly())
///     .Build();
/// 
/// // With custom factory (Autofac, Ninject, etc.)
/// var nexus = ConduitBuilder.Create()
///     .WithActivator(type => container.Resolve(type))
///     .AddMqttConnection(mqtt => mqtt.WithBroker("localhost"))
///     .Build();
/// </example>
public sealed class ConduitBuilder
{
    private IHandlerActivator? _activator;
    private IServiceProvider? _serviceProvider;
    private readonly List<Func<IHandlerActivator, IServiceProvider?, object>> _connectionFactories = [];

    private ConduitBuilder() { }

    /// <summary>
    /// Gets the configured service provider, if available.
    /// </summary>
    public IServiceProvider? ServiceProvider => _serviceProvider;

    /// <summary>
    /// Creates a new Nexus builder instance.
    /// </summary>
    public static ConduitBuilder Create() => new();

    /// <summary>
    /// Configures the handler activator using an IServiceProvider.
    /// Works with any DI container that provides IServiceProvider.
    /// </summary>
    public ConduitBuilder WithServiceProvider(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
        _activator = new ServiceProviderActivator(serviceProvider);
        return this;
    }

    /// <summary>
    /// Configures the handler activator using a factory function.
    /// Use this for direct integration with any DI container.
    /// </summary>
    /// <example>
    /// // Autofac
    /// .WithActivator(type => container.Resolve(type))
    /// 
    /// // SimpleInjector  
    /// .WithActivator(type => container.GetInstance(type))
    /// 
    /// // Ninject
    /// .WithActivator(type => kernel.Get(type))
    /// </example>
    public ConduitBuilder WithActivator(Func<Type, object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _activator = new FuncActivator(factory);
        return this;
    }

    /// <summary>
    /// Configures the handler activator using a custom implementation.
    /// </summary>
    public ConduitBuilder WithActivator(IHandlerActivator activator)
    {
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        return this;
    }

    /// <summary>
    /// Registers a connection factory to be built.
    /// This is used by protocol-specific extension methods.
    /// </summary>
    public ConduitBuilder AddConnection<TConnection>(Func<IHandlerActivator, IServiceProvider?, TConnection> factory)
        where TConnection : class
    {
        _connectionFactories.Add((activator, sp) => factory(activator, sp));
        return this;
    }

    /// <summary>
    /// Builds all configured connections and returns the Nexus instance.
    /// </summary>
    public IConduit Build()
    {
        // Si no hay activator configurado, usar uno por defecto que usa Activator.CreateInstance
        var activator = _activator ?? new Activators.FuncActivator(type =>
        {
            try
            {
                return Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot create instance of '{type.Name}'. " +
                    $"Ensure it has a parameterless constructor or configure a DI container using WithActivator() or WithServiceProvider().", ex);
            }
        });

        var connections = new List<object>();
        
        foreach (var factory in _connectionFactories)
        {
            connections.Add(factory(activator, _serviceProvider));
        }

        var conduit = new Conduit(connections, activator);

        // 🎯 Si el activator es FuncActivator, configurarlo para auto-inyectar IConduit
        if (activator is Activators.FuncActivator funcActivator)
        {
            funcActivator.SetConduitInstance(conduit);
        }

        // Initialize global EventMediator automatically (no manual DI registration required)
        global::Conduit.Core.Events.EventMediator.SetGlobal(new global::Conduit.Core.Events.EventMediator(conduit));

        return conduit;
    }
}
