using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Builder fluido para configurar contenedores DI (Microsoft.Extensions, Autofac o SimpleInjector).
/// </summary>
public class DIContainerBuilder
{
    private IServiceProvider? _serviceProvider;
    private IContainer? _autofacContainer;
    private Container? _simpleInjectorContainer;
    private readonly ServiceCollection _services = new();
    private ContainerBuilder? _autofacBuilder;
    private DIContainerType _containerType = DIContainerType.None;

    private enum DIContainerType
    {
        None,
        NativeDI,
        Autofac,
        SimpleInjector
    }

    private DIContainerBuilder()
    {
        // Configurar logging por defecto
        _services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }

    public static DIContainerBuilder Create() => new();

    /// <summary>
    /// Usa Microsoft.Extensions.DependencyInjection como contenedor DI.
    /// </summary>
    public DIContainerBuilder UseNativeDI()
    {
        _containerType = DIContainerType.NativeDI;
        
        // Registrar servicios de la aplicación
        // AsCommDemoService NO se registra (se crea manualmente con 'new')
        _services.AddSingleton<IDataProcessingService, DataProcessingService>();
        
        return this;
    }

    /// <summary>
    /// Usa Autofac como contenedor DI.
    /// </summary>
    public DIContainerBuilder UseAutofac()
    {
        _containerType = DIContainerType.Autofac;
        
        // Crear builder de Autofac
        _autofacBuilder = new ContainerBuilder();
        
        // Poblar con servicios de Microsoft.Extensions (logging, etc.)
        _autofacBuilder.Populate(_services);
        
        // Registrar servicios de la aplicación en Autofac
        // AsCommDemoService NO se registra (se crea manualmente con 'new')
        _autofacBuilder.RegisterType<DataProcessingService>().As<IDataProcessingService>().SingleInstance();
        
        return this;
    }

    /// <summary>
    /// Usa SimpleInjector como contenedor DI.
    /// </summary>
    public DIContainerBuilder UseSimpleInjector()
    {
        _containerType = DIContainerType.SimpleInjector;
        
        // Crear contenedor de SimpleInjector
        _simpleInjectorContainer = new Container();
        _simpleInjectorContainer.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();
        
        // Configurar logging de Microsoft.Extensions
        var serviceProvider = _services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        
        _simpleInjectorContainer.RegisterInstance(loggerFactory);
        _simpleInjectorContainer.Register(typeof(ILogger<>), typeof(Logger<>), Lifestyle.Singleton);
        
        // Registrar servicios de la aplicación
        // AsCommDemoService NO se registra (se crea manualmente con 'new')
        _simpleInjectorContainer.Register<IDataProcessingService, DataProcessingService>(Lifestyle.Singleton);
        
        return this;
    }

    /// <summary>
    /// Construye el contenedor DI.
    /// </summary>
    public DIContainerBuilder Build()
    {
        switch (_containerType)
        {
            case DIContainerType.Autofac:
                if (_autofacBuilder == null)
                    throw new InvalidOperationException("Autofac builder not initialized. Call UseAutofac() first.");
                    
                _autofacContainer = _autofacBuilder.Build();
                _serviceProvider = new AutofacServiceProvider(_autofacContainer);
                break;
                
            case DIContainerType.SimpleInjector:
                if (_simpleInjectorContainer == null)
                    throw new InvalidOperationException("SimpleInjector container not initialized. Call UseSimpleInjector() first.");
                    
                _simpleInjectorContainer.Verify();
                _serviceProvider = _simpleInjectorContainer;
                break;
                
            case DIContainerType.NativeDI:
                _serviceProvider = _services.BuildServiceProvider();
                break;
                
            default:
                throw new InvalidOperationException("No DI container selected. Call UseNativeDI(), UseAutofac(), or UseSimpleInjector() first.");
        }
        
        return this;
    }

    /// <summary>
    /// Obtiene el IServiceProvider construido.
    /// </summary>
    public IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Container not built. Call Build() first.");
            
        return _serviceProvider;
    }

    /// <summary>
    /// Obtiene la función activator para Conduit.
    /// </summary>
    public Func<Type, object> GetActivator()
    {
        var provider = GetServiceProvider();
        
        return _containerType switch
        {
            DIContainerType.Autofac => type => _autofacContainer!.Resolve(type),
            DIContainerType.SimpleInjector => type => _simpleInjectorContainer!.GetInstance(type),
            DIContainerType.NativeDI => type => ActivatorUtilities.GetServiceOrCreateInstance(provider, type),
            _ => throw new InvalidOperationException("Container not built properly.")
        };
    }

    /// <summary>
    /// Obtiene el ILoggerFactory.
    /// </summary>
    public ILoggerFactory GetLoggerFactory()
    {
        return GetServiceProvider().GetRequiredService<ILoggerFactory>();
    }
}
