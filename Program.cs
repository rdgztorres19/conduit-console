using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Sitas.Edge.Core;
using Sitas.Edge.EdgePlcDriver;
using Sitas.Edge.Mqtt;
using ConduitPlcDemo.Services;
using Sitas.Edge.Core.Events;
using ConduitPlcDemo.Handlers.Events;
using Microsoft.AspNetCore.Routing;

namespace ConduitPlcDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Sitas.Edge PLC Demo - Starting...\n");

        // ════════════════════════════════════════════════════════════════
        // CONFIGURACIÓN
        // ════════════════════════════════════════════════════════════════
        const string plcIp = "192.168.8.55";
        const int slot = 0;

        // ════════════════════════════════════════════════════════════════
        // WEB API SETUP
        // ════════════════════════════════════════════════════════════════
        var builder = WebApplication.CreateBuilder(args);

        // Configurar servicios de la aplicación
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Deshabilitar sesiones y cookies para evitar problemas de 403
        // NO agregar AddSession, AddAuthentication, AddAuthorization
        
        // Configurar CORS para permitir todas las solicitudes (desarrollo)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
        
        // NO registrar WebSocketManager aquí todavía - lo haremos después con una instancia específica

        // ════════════════════════════════════════════════════════════════
        // REGISTRAR SERVICIOS DE LA APLICACIÓN EN ASP.NET CORE
        // ════════════════════════════════════════════════════════════════
        // Registrar los servicios que normalmente se registran en DIContainerBuilder
        // para que estén disponibles en el mismo ServiceCollection que tiene SignalR, Controllers, etc.
        builder.Services.AddSingleton<IDataProcessingService, DataProcessingService>();

        // ════════════════════════════════════════════════════════════════
        // DEPENDENCY INJECTION
        // ════════════════════════════════════════════════════════════════
        // Crear DIContainerBuilder usando el ServiceCollection de ASP.NET Core
        // Esto permite que los handlers puedan resolver servicios como IHubContext, Controllers, etc.
        var diContainer = DIContainerBuilder.Create(builder.Services)
            .UseNativeDI()      // ← Cambiar a .UseAutofac() para usar Autofac
            .Build();

        var loggerFactory = diContainer.GetLoggerFactory();
        
        // IMPORTANTE: Crear una instancia única del WebSocketManager y registrarla explícitamente
        // para que tanto ASP.NET Core como Sitas.Edge usen la MISMA instancia
        var webSocketManagerInstance = new Services.WebSocketManager(
            loggerFactory.CreateLogger<Services.WebSocketManager>());
        Console.WriteLine($"🔧 WebSocketManager instance created: {webSocketManagerInstance.GetHashCode()}");
        
        // Registrar como instancia específica para garantizar que sea la misma en todos lados
        builder.Services.AddSingleton(webSocketManagerInstance);
        
        // CRÍTICO: Reconstruir el DIContainerBuilder DESPUÉS de registrar el WebSocketManager
        // para que el ServiceProvider de Sitas.Edge tenga acceso a la misma instancia
        diContainer = DIContainerBuilder.Create(builder.Services)
            .UseNativeDI()
            .Build();

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAR SITAS.EDGE CON PLC
        // ════════════════════════════════════════════════════════════════
        // Verificar que el activator use el mismo ServiceProvider
        var activator = diContainer.GetActivator();
        var testInstance = activator(typeof(Services.WebSocketManager));
        Console.WriteLine($"🔧 Testing activator: WebSocketManager instance from activator: {testInstance.GetHashCode()}");
        Console.WriteLine($"🔧 Expected instance: {webSocketManagerInstance.GetHashCode()}");
        Console.WriteLine($"🔧 Same instance? {testInstance == webSocketManagerInstance}");
        
        if (testInstance != webSocketManagerInstance)
        {
            Console.WriteLine($"❌ ERROR: WebSocketManager instances are DIFFERENT! This will cause sockets to be lost.");
        }

        var plcOptions = builder.Configuration.GetSection("Plc1").Get<Sitas.Edge.EdgePlcDriver.Configuration.EdgePlcDriverOptions>();
        var mqttOptions = builder.Configuration.GetSection("Mqtt").Get<Sitas.Edge.Mqtt.Configuration.MqttConnectionOptions>();

        if (plcOptions != null)
        {
            Console.WriteLine($"🔧 PLC Options loaded from appsettings.json:");
            Console.WriteLine($"   ConnectionName: {plcOptions.ConnectionName}");
            Console.WriteLine($"   IpAddress: {plcOptions.IpAddress}");
            Console.WriteLine($"   CpuSlot: {plcOptions.CpuSlot}");
            Console.WriteLine($"   Backplane: {plcOptions.Backplane}");
            Console.WriteLine($"   DefaultPollingIntervalMs: {plcOptions.DefaultPollingIntervalMs}");
            Console.WriteLine($"   ConnectionTimeoutSeconds: {plcOptions.ConnectionTimeoutSeconds}");
            Console.WriteLine($"   AutoReconnect: {plcOptions.AutoReconnect}");
            Console.WriteLine($"   MaxReconnectDelaySeconds: {plcOptions.MaxReconnectDelaySeconds}");
        }
        else
        {
            Console.WriteLine($"⚠️  PLC Options: NULL (not found in appsettings.json)");
        }

        if (mqttOptions != null)
        {
            Console.WriteLine($"🔧 MQTT Options loaded from appsettings.json:");
            Console.WriteLine($"   ConnectionName: {mqttOptions.ConnectionName}");
            Console.WriteLine($"   Host: {mqttOptions.Host}");
            Console.WriteLine($"   Port: {mqttOptions.Port}");
            Console.WriteLine($"   Username: {mqttOptions.Username}");
            Console.WriteLine($"   Password: {(string.IsNullOrEmpty(mqttOptions.Password) ? "(empty)" : "***")}");
            Console.WriteLine($"   UseTls: {mqttOptions.UseTls}");
            Console.WriteLine($"   KeepAliveSeconds: {mqttOptions.KeepAliveSeconds}");
            Console.WriteLine($"   AutoReconnect: {mqttOptions.AutoReconnect}");
            Console.WriteLine($"   ProtocolVersion: {mqttOptions.ProtocolVersion}");
        }
        else
        {
            Console.WriteLine($"⚠️  MQTT Options: NULL (not found in appsettings.json)");
        }
        Console.WriteLine();

        // var externalHandlersPath = Path.Combine(AppContext.BaseDirectory, "ExternalHandlers.dll");
        // var externalHandlersAssembly = Assembly.LoadFrom(externalHandlersPath);
        
        var conduit = SitasEdgeBuilder.Create()
            .WithActivator(activator)
            .AddEdgePlcDriver(plc => plc
                .WithConnectionName("plc1")
                .WithPlc(plcIp, cpuSlot: slot)
                .WithDefaultPollingInterval(100)
                .WithAutoReconnect(enabled: false, maxDelaySeconds: 30)
                .WithLoggerFactory(loggerFactory)
                .WithHandlersFromEntryAssembly())
            // .AddMqttConnection(mqtt => mqtt
            //     .WithConnectionName("mqtt")
            //     .WithBroker("66.179.188.92", 1883)
            //     .WithCredentials("admin", "sbrQp10")
            //     .WithTls(enabled: false)
            //     .WithClientId($"webapi-simpleinjector-{Environment.MachineName}-{Guid.NewGuid():N}"[..50])
            //     .WithAutoReconnect(enabled: true, maxDelaySeconds: 30)
            //     .WithKeepAlive(60)
            //     .WithHandlersFromEntryAssembly())
            .AddMqttConnection(mqtt => mqtt
                .WithOptions(mqttOptions)
                .WithHandlersFromEntryAssembly()
                // .WithHandlersFromAssemblies(
                //     Assembly.GetEntryAssembly()!,
                //     externalHandlersAssembly
                // )
            )
            .Build();

        var plcConnection = conduit.GetConnection<IEdgePlcDriver>();
        builder.Services.AddSingleton(plcConnection);
        
        // PLC deshabilitado en esta PC - sin licencia ASComm
        // Registrar NullEdgePlcDriver para que los controllers no fallen
        // Los handlers MQTT verificarán si el PLC está disponible antes de usarlo
        // En la otra PC con licencia ASComm, descomentar las líneas de arriba y comentar esta
        // var nullPlcConnection = new Services.NullEdgePlcDriver();
        // builder.Services.AddSingleton<IEdgePlcDriver>(nullPlcConnection);

        var mqttConnection = conduit.GetConnection<IMqttConnection>();
        builder.Services.AddSingleton(mqttConnection);

        builder.Services.AddSingleton(conduit);

        // ════════════════════════════════════════════════════════════════
        // CONECTAR AL MQTT
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine($"📡 Connecting to MQTT broker at 66.179.188.92:1883...");

        try
        {
            await conduit.ConnectAllAsync();
            await Task.Delay(500);

            if (!mqttConnection.IsConnected)
            {
                Console.WriteLine($"❌ MQTT Connection failed. State: {mqttConnection.State}");
                Console.WriteLine("⚠️  Possible causes:");
                Console.WriteLine("   - MQTT broker is not reachable");
                Console.WriteLine("   - Wrong credentials");
                Console.WriteLine("   - Firewall blocking connection");
                return;
            }

            // EventMediator.Global is now initialized after SitasEdgeBuilder.Build()
            // Test event emission
            //await EventMediator.Global.EmitAsync("tempChanged", new TemperatureChangedEvent(25.5f));

            Console.WriteLine($"✅ MQTT Connected! State: {mqttConnection.State}\n");

            // ════════════════════════════════════════════════════════════════
            // DEMO: Usar MqttSubscriptionService
            // ════════════════════════════════════════════════════════════════
            // var mqttSubscriptionService = new MqttSubscriptionService(mqttConnection);
            // await mqttSubscriptionService.StartAsync();

            // ════════════════════════════════════════════════════════════════
            // DEMO: Lectura directa con ASComm de ngpSampleCurrent.pallets
            // ════════════════════════════════════════════════════════════════
            // var palletsLogger = loggerFactory.CreateLogger<PalletsDirectReaderService>();
            // var palletsReader = new PalletsDirectReaderService(palletsLogger, plcIp, slot);
            // await palletsReader.ReadPalletsTagAsync();
            
            Console.WriteLine("\n");

            // ════════════════════════════════════════════════════════════════
            // DEMO: Usar AsCommDemoService
            // ════════════════════════════════════════════════════════════════
            var asCommDemoService = new AsCommDemoService(plcConnection);
            // await asCommDemoService.ReadSampleTagAsync();
            //await asCommDemoService.ReadMultipleSiteNumbersAsync();
            await asCommDemoService.StartSubscriptionAsync();
            asCommDemoService.StartPeriodicWrites();


            // ════════════════════════════════════════════════════════════════
            // DEMO: Emit temperature events every 5 seconds
            // ════════════════════════════════════════════════════════════════
            // var random = new Random();

            // var timer = new Timer(async _ =>
            // {
            //     await EventMediator.Global.EmitAsync("tempChanged", new TemperatureChangedEvent(random.Next(1, 101)));
            // }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection error: {ex.Message}");
            Console.WriteLine($"   Type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }
            return;
        }

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAR WEB API
        // ════════════════════════════════════════════════════════════════
        var app = builder.Build();

        // Configurar el pipeline HTTP
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        // Asegurar que NO haya middleware de autorización o autenticación
        // NO llamar a app.UseAuthentication() o app.UseAuthorization()

        // Servir archivos estáticos de Angular PRIMERO
        // Los archivos están en wwwroot/browser/ porque Angular 17 genera ahí
        var browserPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "browser");
        
        // Verificar que el directorio existe
        if (!Directory.Exists(browserPath))
        {
            Console.WriteLine($"⚠️ Warning: Angular build directory not found at: {browserPath}");
            Console.WriteLine("   Make sure to run 'npm run build' in the angular-app directory");
        }
        else
        {
            Console.WriteLine($"✅ Serving Angular app from: {browserPath}");
        }
        
        // Archivos estáticos - servir desde la raíz
        var fileProvider = new PhysicalFileProvider(browserPath);
        
        // Habilitar CORS PRIMERO (antes de cualquier otro middleware)
        app.UseCors();
        
        // Routing
        app.UseRouting();
        
        // Habilitar WebSockets (requerido para que el middleware funcione)
        app.UseWebSockets();
        
        // Mapear WebSocket endpoint PRIMERO (antes de controllers)
        app.Map("/ws/plctag", builder =>
        {
            builder.UseMiddleware<Middleware.WebSocketMiddleware>();
        });
        
        // Mapear endpoints de API (sin autorización - acceso público)
        app.MapControllers();
        
        // Middleware de fallback ANTES de UseStaticFiles (para cambiar path a /index.html si es necesario)
        app.Use(async (context, next) =>
        {
            // Si es una ruta de API, WebSocket o Swagger, NO hacer nada
            if (context.Request.Path.StartsWithSegments("/api") || 
                context.Request.Path.StartsWithSegments("/ws") ||
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                await next();
                return;
            }
            
            // Verificar si el archivo existe
            var fileInfo = fileProvider.GetFileInfo(context.Request.Path.Value ?? "/");
            if (!fileInfo.Exists || fileInfo.IsDirectory)
            {
                // Si no existe, servir index.html (SPA fallback)
                var indexFile = fileProvider.GetFileInfo("/index.html");
                if (indexFile.Exists)
                {
                    context.Request.Path = "/index.html";
                }
            }
            
            await next();
        });
        
        // Archivos estáticos DESPUÉS del middleware de fallback
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = "",
            OnPrepareResponse = ctx =>
            {
                // Agregar headers para evitar caché y permitir acceso
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
                ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }
        });

        // ════════════════════════════════════════════════════════════════
        // INFORMACIÓN DE INICIO
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("📡 MQTT Handlers active:");
        Console.WriteLine("   - MqttRealtimeHandler (attribute-based)");
        Console.WriteLine("   - MqttSubscriptionService (programmatic)");
        Console.WriteLine();
        Console.WriteLine("🌐 Web API running:");
        Console.WriteLine("   - Swagger UI: https://localhost:5001/swagger (or http://localhost:5000/swagger)");
        Console.WriteLine("   - API Base: /api/plc and /api/mqtt");
        Console.WriteLine();
        Console.WriteLine("Press CTRL+C to exit\n");
        Console.WriteLine("════════════════════════════════════════════════════════════════");

        // ════════════════════════════════════════════════════════════════
        // EJECUTAR WEB API Y ESPERAR
        // ════════════════════════════════════════════════════════════════
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            // Ejecutar Web API en background y esperar
            var webApiTask = app.RunAsync(cts.Token);
            await webApiTask;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("\n\n🛑 Shutting down...");
        }

        // ════════════════════════════════════════════════════════════════
        // CLEANUP
        // ════════════════════════════════════════════════════════════════
        await conduit.DisconnectAllAsync();
        await conduit.DisposeAsync();
        await app.DisposeAsync();

        Console.WriteLine("✅ Disconnected. Goodbye!");
    }
}
