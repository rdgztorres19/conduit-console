using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Conduit.Core;
using Conduit.AsComm;
using Conduit.Mqtt;
using ConduitPlcDemo.Services;
using Conduit.Core.Events;
using ConduitPlcDemo.Handlers.Events;

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
        // DEPENDENCY INJECTION
        // ════════════════════════════════════════════════════════════════
        var diContainer = DIContainerBuilder.Create()
            .UseSimpleInjector()      // ← Cambiar a .UseAutofac() para usar Autofac
            .Build();

        var loggerFactory = diContainer.GetLoggerFactory();
        var serviceProvider = diContainer.GetServiceProvider();

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAR CONDUIT CON PLC
        // ════════════════════════════════════════════════════════════════
        var conduit = ConduitBuilder.Create()
            .WithActivator(diContainer.GetActivator())
            // .AddAsCommConnection(plc => plc
            //     .WithConnectionName("plc1")
            //     .WithPlc(plcIp, cpuSlot: slot)
            //     .WithDefaultPollingInterval(100)
            //     .WithAutoReconnect(enabled: false, maxDelaySeconds: 30)
            //     .WithLoggerFactory(loggerFactory)
            //     .WithHandlersFromEntryAssembly())
            .AddMqttConnection(mqtt => mqtt
                .WithConnectionName("mqtt")
                .WithBroker("66.179.188.92", 1883)
                .WithCredentials("admin", "sbrQp10")
                .WithTls(enabled: false)
                .WithClientId($"console-simpleinjector-{Environment.MachineName}-{Guid.NewGuid():N}"[..50])
                .WithAutoReconnect(enabled: true, maxDelaySeconds: 30)
                .WithKeepAlive(60)
                .WithHandlersFromEntryAssembly())
            .Build();

        var mqttConnection = conduit.GetConnection<IMqttConnection>();

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
            // DEMO: Usar AsCommDemoService
            // ════════════════════════════════════════════════════════════════
            // var plcConnection = conduit.GetConnection<IAsCommConnection>();
            // var asCommDemoService = new AsCommDemoService(plcConnection);
            // await asCommDemoService.ReadSampleTagAsync();
            // await asCommDemoService.StartSubscriptionAsync();
            // asCommDemoService.StartPeriodicWrites();


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
        // ESPERAR MENSAJES MQTT
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("📡 MQTT Handlers active:");
        Console.WriteLine("   - MqttRealtimeHandler (attribute-based)");
        Console.WriteLine("   - MqttSubscriptionService (programmatic)");
        Console.WriteLine();
        Console.WriteLine("Press CTRL+C to exit\n");
        Console.WriteLine("════════════════════════════════════════════════════════════════");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
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

        Console.WriteLine("✅ Disconnected. Goodbye!");
    }
}
