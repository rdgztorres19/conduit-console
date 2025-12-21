using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Conduit.Core;
using Conduit.EdgePlcDriver;
using Conduit.Mqtt;
using ConduitPlcDemo.Services;
using Conduit.Core.Events;
using ConduitPlcDemo.Handlers.Events;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitPlcDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Conduit PLC Demo - Starting...\n");

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

        // ════════════════════════════════════════════════════════════════
        // DEPENDENCY INJECTION
        // ════════════════════════════════════════════════════════════════
        var diContainer = DIContainerBuilder.Create()
            .UseSimpleInjector()      // ← Cambiar a .UseAutofac() para usar Autofac
            .Build();

        var loggerFactory = diContainer.GetLoggerFactory();
        var serviceProvider = diContainer.GetServiceProvider();
        
        // Agregar el serviceProvider personalizado a los servicios de Web API
        builder.Services.AddSingleton(serviceProvider);

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAR CONDUIT CON PLC
        // ════════════════════════════════════════════════════════════════
        var conduit = ConduitBuilder.Create()
            .WithActivator(diContainer.GetActivator())
            .AddEdgePlcDriver(plc => plc
                .WithConnectionName("plc1")
                .WithPlc(plcIp, cpuSlot: slot)
                .WithDefaultPollingInterval(100)
                .WithAutoReconnect(enabled: false, maxDelaySeconds: 30)
                .WithLoggerFactory(loggerFactory)
                .WithHandlersFromEntryAssembly())
            .AddMqttConnection(mqtt => mqtt
                .WithConnectionName("mqtt")
                .WithBroker("66.179.188.92", 1883)
                .WithCredentials("admin", "sbrQp10")
                .WithTls(enabled: false)
                .WithClientId($"webapi-simpleinjector-{Environment.MachineName}-{Guid.NewGuid():N}"[..50])
                .WithAutoReconnect(enabled: true, maxDelaySeconds: 30)
                .WithKeepAlive(60)
                .WithHandlersFromEntryAssembly())
            .Build();

        var plcConnection = conduit.GetConnection<IEdgePlcDriver>();
        builder.Services.AddSingleton(plcConnection);

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
            // var asCommDemoService = new AsCommDemoService(plcConnection);
            // await asCommDemoService.ReadSampleTagAsync();
            // await asCommDemoService.ReadMultipleSiteNumbersAsync();
            //await asCommDemoService.StartSubscriptionAsync();
            //asCommDemoService.StartPeriodicWrites();


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

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        
        // Servir archivos estáticos de Angular (después de los controladores)
        // Los archivos están en wwwroot/browser/ porque Angular 17 genera ahí
        var browserPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "browser");
        
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(browserPath),
            RequestPath = ""
        });
        
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(browserPath),
            RequestPath = ""
        });
        
        // Fallback a index.html para SPA routing
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(browserPath)
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
