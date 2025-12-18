using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Conduit.Core;
using Conduit.AsComm;

namespace ConduitPlcDemo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Conduit PLC Demo - Starting...\n");

        // ════════════════════════════════════════════════════════════════
        // CONFIGURACIÓN - Igual que el ejemplo de JNJ
        // ════════════════════════════════════════════════════════════════
        const string plcIp = "192.168.8.55";
        const int slot = 0;
        const string tagToRead = "Program:UDT_NGP_INTERFEROMETER_ANALYSIS_TAG";

        // ════════════════════════════════════════════════════════════════
        // DEPENDENCY INJECTION
        // ════════════════════════════════════════════════════════════════
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // ════════════════════════════════════════════════════════════════
        // CONFIGURAR CONEXIÓN PLC - Misma config que JNJ
        // ════════════════════════════════════════════════════════════════
        var plcConnection = AsCommClientBuilder.Create()
            .WithConnectionName("plc1")
            .WithPlc(plcIp, cpuSlot: slot)
            .WithDefaultPollingInterval(100) // 100ms default polling
            .WithAutoReconnect(enabled: false, maxDelaySeconds: 30) // Desactivar auto-reconnect para ver el error real
            .WithHandlersFromEntryAssembly()
            .Build();

        // Suscribirse a cambios de estado para debug
        plcConnection.StateChanged += (sender, e) =>
        {
            Console.WriteLine($"🔄 State changed: {e.PreviousState} → {e.CurrentState}");
            if (e.Exception != null)
            {
                Console.WriteLine($"   Error: {e.Exception.Message}");
            }
        };

        // ════════════════════════════════════════════════════════════════
        // CONECTAR
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine($"📡 Connecting to PLC at {plcIp}, slot {slot}...");
        
        try
        {
            await plcConnection.ConnectAsync();
            
            // Esperar un poco para ver si cambia de estado
            await Task.Delay(500);
            
            Console.WriteLine($"Connection state: {plcConnection.State}");
            
            if (!plcConnection.IsConnected)
            {
                Console.WriteLine($"❌ Connection failed. State: {plcConnection.State}");
                Console.WriteLine("⚠️  Possible causes:");
                Console.WriteLine("   - PLC is not reachable at this IP address");
                Console.WriteLine("   - Incorrect slot number");
                Console.WriteLine("   - ASComm IoT license not installed/valid");
                Console.WriteLine("   - Firewall blocking connection");
                Console.WriteLine($"\n💡 Verify: Can you ping {plcIp}?");
                return;
            }
            
            Console.WriteLine("✅ Connected!\n");
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
        // LEER TAG INDIVIDUAL (Interferometer Analysis)
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine($"📖 Reading tag: {tagToRead}");
        var result = await plcConnection.ReadTagAsync<STRUCT_interferometer_analysis>(tagToRead);
        
        Console.WriteLine($"   Quality: {result.Quality}");
        if (result.Quality == Conduit.AsComm.Messages.TagQuality.Good)
        {
            Console.WriteLine($"   MeasurementStatus: {result.Value.MeasurementStatus}");
            Console.WriteLine($"   Result: {result.Value.Result}");
        }
        Console.WriteLine();

        // LEER TAG DE SAMPLE (UDT completo)
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("📖 Reading tag: ngpSampleCurrent");
        var sampleResult = await plcConnection.ReadTagAsync<STRUCT_samples>("ngpSampleCurrent");
        
        Console.WriteLine($"   Quality: {sampleResult.Quality}");
        if (sampleResult.Quality == Conduit.AsComm.Messages.TagQuality.Good)
        {
            var s = sampleResult.Value;
            Console.WriteLine($"   SampleId: {s.Data.SampleId.Value}");
            Console.WriteLine($"   SampledOn: {s.Data.SampledOn.Value}");
            Console.WriteLine($"   SampledBy: {s.Data.SampledBy.Value}");
            if (s.Pallets?.Length > 0)
            {
                Console.WriteLine($"   Pallet[0] RFID: {s.Pallets[0].Data.Rfid.Value}");
            }
        }
        Console.WriteLine();

        // ════════════════════════════════════════════════════════════════
        // HANDLERS AUTOMÁTICOS
        // ════════════════════════════════════════════════════════════════
        Console.WriteLine("📡 Starting automatic handlers...");
        Console.WriteLine("   - InterferometerAnalysisHandler (Unsolicited mode - 10ms)");
        Console.WriteLine("   - SampleTagHandler (Polling mode - 1000ms)");
        Console.WriteLine();
        Console.WriteLine("Press CTRL+C to exit\n");
        Console.WriteLine("════════════════════════════════════════════════════════════════");

        // Mantener la aplicación corriendo
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
        await plcConnection.DisconnectAsync();
        await plcConnection.DisposeAsync();
        
        Console.WriteLine("✅ Disconnected. Goodbye!");
    }
}
