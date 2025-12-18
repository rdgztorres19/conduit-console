using System.Reflection;
using Conduit.Core.Abstractions;

namespace Conduit.Core.Activators;

/// <summary>
/// Handler activator that uses a delegate function.
/// Provides flexibility for any DI container integration.
/// </summary>
/// <remarks>
/// This activator automatically creates handler instances even if they are not
/// explicitly registered in the DI container. It resolves constructor dependencies
/// using the provided factory function.
/// </remarks>
/// <example>
/// // Autofac - handlers are created automatically, no registration needed
/// var activator = new FuncActivator(type => container.Resolve(type));
/// 
/// // SimpleInjector
/// var activator = new FuncActivator(type => container.GetInstance(type));
/// 
/// // Ninject
/// var activator = new FuncActivator(type => kernel.Get(type));
/// </example>
public sealed class FuncActivator : IHandlerActivator
{
    private readonly Func<Type, object> _factory;
    private IConduit? _conduitInstance;

    /// <summary>
    /// Creates a new activator using the specified factory delegate.
    /// </summary>
    /// <param name="factory">A function that resolves service instances by type.</param>
    public FuncActivator(Func<Type, object> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Sets the Conduit instance for auto-injection.
    /// Called internally by ConduitBuilder after creating the Conduit.
    /// </summary>
    internal void SetConduitInstance(IConduit conduit)
    {
        _conduitInstance = conduit;
    }

    /// <inheritdoc />
    public object CreateInstance(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        // 🎯 Auto-inyectar IConduit sin necesidad de registrarlo en el DI
        if (handlerType == typeof(IConduit))
        {
            if (_conduitInstance is null)
                throw new InvalidOperationException("IConduit requested but Conduit is not initialized yet.");
            return _conduitInstance;
        }

        // 1. Primero intentar resolver del contenedor directamente
        try
        {
            var handler = _factory(handlerType);
            if (handler is not null)
                return handler;
        }
        catch
        {
            // El tipo no está registrado, lo creamos manualmente
        }

        // 2. Crear la instancia manualmente, resolviendo dependencias del constructor
        return CreateInstanceWithDependencies(handlerType);
    }

    /// <summary>
    /// Creates an instance of the handler type, resolving all constructor dependencies.
    /// </summary>
    private object CreateInstanceWithDependencies(Type handlerType)
    {
        // Buscar el constructor con más parámetros (greedy)
        var constructors = handlerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        
        if (constructors.Length == 0)
        {
            throw new InvalidOperationException(
                $"Handler '{handlerType.Name}' has no public constructors.");
        }

        var constructor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            
            // 🎯 Interceptar IConduit también aquí (para dependencias del constructor)
            if (paramType == typeof(IConduit))
            {
                if (_conduitInstance is null)
                {
                    throw new InvalidOperationException(
                        $"Handler '{handlerType.Name}' requires IConduit, but Conduit is not initialized yet.");
                }
                args[i] = _conduitInstance;
                continue;
            }
            
            // 🎯 Auto-inyectar conexiones específicas (IMqttConnection, IAsCommConnection, etc.)
            if (paramType.Name.StartsWith("I") && paramType.Name.EndsWith("Connection") && paramType.IsInterface)
            {
                if (_conduitInstance is null)
                {
                    throw new InvalidOperationException(
                        $"Handler '{handlerType.Name}' requires {paramType.Name}, but Conduit is not initialized yet.");
                }
                
                try
                {
                    // Usar reflexión para llamar a GetConnection<T>()
                    var getConnectionMethod = typeof(IConduit).GetMethod("GetConnection")!.MakeGenericMethod(paramType);
                    args[i] = getConnectionMethod.Invoke(_conduitInstance, null)!;
                    continue;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Handler '{handlerType.Name}' requires {paramType.Name}, but no connection of that type is configured in Conduit. " +
                        $"Ensure you've added the corresponding connection (e.g., AddMqttConnection, AddAsCommConnection).", ex);
                }
            }
            
            // 🎯 Auto-inyectar publishers (IMessagePublisher, IMqttPublisher, IAsCommPublisher)
            if (paramType.Name.StartsWith("I") && paramType.Name.Contains("Publisher") && paramType.IsInterface)
            {
                if (_conduitInstance is null)
                {
                    throw new InvalidOperationException(
                        $"Handler '{handlerType.Name}' requires {paramType.Name}, but Conduit is not initialized yet.");
                }
                
                try
                {
                    // Intentar obtener el publisher de la primera conexión disponible
                    var connections = _conduitInstance.GetType().GetProperty("Connections")?.GetValue(_conduitInstance) as System.Collections.IEnumerable;
                    if (connections != null)
                    {
                        foreach (var conn in connections)
                        {
                            var publisherProp = conn.GetType().GetProperty("Publisher");
                            if (publisherProp != null)
                            {
                                var publisher = publisherProp.GetValue(conn);
                                if (publisher != null && paramType.IsAssignableFrom(publisher.GetType()))
                                {
                                    args[i] = publisher;
                                    goto nextParam;
                                }
                            }
                        }
                    }
                    
                    throw new InvalidOperationException(
                        $"Handler '{handlerType.Name}' requires {paramType.Name}, but no compatible connection with that publisher type is configured.");
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        $"Failed to resolve {paramType.Name} for handler '{handlerType.Name}'.", ex);
                }
            }
            nextParam:;
            
            // 🔇 Si es ILogger<> y no está registrado, usar NullLogger automáticamente
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Logging.ILogger<>))
            {
                try
                {
                    args[i] = _factory(paramType);
                }
                catch
                {
                    // No está registrado, crear NullLogger
                    var nullLoggerType = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>)
                        .MakeGenericType(paramType.GetGenericArguments()[0]);
                    args[i] = Activator.CreateInstance(nullLoggerType)!;
                }
                continue;
            }
            
            try
            {
                args[i] = _factory(paramType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve dependency '{paramType.Name}' for handler '{handlerType.Name}'. " +
                    $"Ensure the dependency is registered in your DI container.", ex);
            }

            if (args[i] is null)
            {
                throw new InvalidOperationException(
                    $"Dependency '{paramType.Name}' for handler '{handlerType.Name}' resolved to null. " +
                    $"Ensure the dependency is properly registered.");
            }
        }

        return constructor.Invoke(args);
    }

    /// <inheritdoc />
    public THandler CreateInstance<THandler>() where THandler : class
        => (THandler)CreateInstance(typeof(THandler));

    /// <inheritdoc />
    public IScopedHandler CreateScopedInstance(Type handlerType)
    {
        // FuncActivator doesn't support scopes, create without scope
        return new NoScopeHandler(CreateInstance(handlerType));
    }

    private sealed class NoScopeHandler : IScopedHandler
    {
        public object Handler { get; }

        public NoScopeHandler(object handler) => Handler = handler;

        public void Dispose() { } // No scope to dispose
    }
}
