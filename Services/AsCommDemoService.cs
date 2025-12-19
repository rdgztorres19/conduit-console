using Conduit.AsComm;
using Conduit.AsComm.Messages;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Servicio de demostración para operaciones ASComm (lectura y escritura al PLC).
/// Similar a MqttSubscriptionService, solo depende de la conexión PLC.
/// </summary>
public class AsCommDemoService
{
    private readonly IAsCommConnection _plcConnection;
    private readonly Random _random = new();
    private System.Threading.Timer? _writeTimer;
    private CancellationTokenSource? _cts;
    private IAsyncDisposable? _subscription;
    private int _updateCount = 0;

    public AsCommDemoService(IAsCommConnection plcConnection)
    {
        _plcConnection = plcConnection;

        plcConnection.StateChanged += (sender, e) =>
        {
            Console.WriteLine($"🔄 PLC State changed: {e.PreviousState} → {e.CurrentState}");
            if (e.Exception != null)
            {
                Console.WriteLine($"   Error: {e.Exception.Message}");
            }
        };
    }

    /// <summary>
    /// Inicia suscripción programática al tag del PLC.
    /// Similar a: await _mqtt.SubscribeAsync<TMessage>(topic, handler, qos)
    /// </summary>
    public async Task StartSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("🚀 Starting AsComm programmatic subscription...");

        // Suscripción programática al tag (similar al atributo [AsCommSubscribe])
        _subscription = await _plcConnection.SubscribeAsync<STRUCT_samples>(
            "ngpSampleCurrent",
            HandleSampleTagAsync,
            pollingIntervalMs: 1000,
            cancellationToken);

        Console.WriteLine("✅ AsComm subscription active: ngpSampleCurrent (1000ms polling)");
    }

    private async Task HandleSampleTagAsync(
        TagValue<STRUCT_samples> message,
        IAsCommMessageContext context,
        CancellationToken cancellationToken)
    {
        if (message.Quality != TagQuality.Good)
        {
            Console.WriteLine($"⚠️ [SERVICE] Sample tag quality: {message.Quality}");
            return;
        }

        _updateCount++;
        var sample = message.Value;

        Console.WriteLine($"📦 [SERVICE #{_updateCount}] Sample Update | SampleId: {sample.Data.SampleId.Value} | SampledOn: {sample.Data.SampledOn.Value}");

        // Mostrar info del primer pallet si existe
        if (sample.Pallets?.Length > 0)
        {
            var pallet = sample.Pallets[0];
            Console.WriteLine($"   └─ Pallet[0] | RFID: {pallet.Data.Rfid.Value} | Type: {pallet.Data.CasetteType.Value}");

            // Mostrar info de la primera cavity si existe
            if (pallet.Cavities?.Length > 0)
            {
                var cavity = pallet.Cavities[0];
                Console.WriteLine($"      └─ Cavity[0] | ID: {cavity.Identifier} | Site: {cavity.SiteNumber} | Lot: {cavity.LotNumber.Value}");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Detiene la suscripción programática.
    /// </summary>
    public async Task StopSubscriptionAsync()
    {
        Console.WriteLine("🛑 Stopping AsComm subscription...");
        
        if (_subscription != null)
        {
            await _subscription.DisposeAsync();
            _subscription = null;
        }

        Console.WriteLine("✅ AsComm subscription stopped");
    }

    /// <summary>
    /// Lee el tag ngpSampleCurrent y muestra información del sample.
    /// </summary>
    public async Task ReadSampleTagAsync()
    {
        const string sampleTagName = "ngpSampleCurrent";

        Console.WriteLine($"📖 Reading tag: {sampleTagName}");

        var sampleResult = await _plcConnection.ReadTagAsync<STRUCT_samples>(sampleTagName);

        if (sampleResult.Quality != TagQuality.Good)
        {
            Console.WriteLine($"⚠️ Sample tag quality: {sampleResult.Quality}");
            Console.WriteLine("   💡 Check if tag 'ngpSampleCurrent' exists in the PLC");
            Console.WriteLine("   💡 Verify it's in the correct scope (Controller vs Program scope)");
            Console.WriteLine();
            return;
        }

        var sample = sampleResult.Value;
        Console.WriteLine($"📦 Sample Update | SampleId: {sample.Data.SampleId.Value} | SampledOn: {sample.Data.SampledOn.Value}");

        // Mostrar info del primer pallet si existe
        if (sample.Pallets?.Length > 0)
        {
            var pallet = sample.Pallets[0];
            Console.WriteLine($"   └─ Pallet[0] | RFID: {pallet.Data.Rfid.Value} | Type: {pallet.Data.CasetteType.Value}");

            // Mostrar info de la primera cavity si existe
            if (pallet.Cavities?.Length > 0)
            {
                var cavity = pallet.Cavities[0];
                Console.WriteLine($"      └─ Cavity[0] | ID: {cavity.Identifier} | Site: {cavity.SiteNumber} | Lot: {cavity.LotNumber.Value}");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Lee múltiples tags siteNumber usando ReadTagsAsync (batch read optimizado).
    /// Demuestra cómo leer varios tags primitivos (int) en una sola operación.
    /// </summary>
    public async Task ReadMultipleSiteNumbersAsync()
    {
        var tagNames = new[]
        {
            "ngpSampleCurrent.pallets[0].cavities[0].siteNumber",
            "ngpSampleCurrent.pallets[0].cavities[1].siteNumber",
            "ngpSampleCurrent.pallets[0].cavities[2].siteNumber",
            "ngpSampleCurrent.pallets[0].cavities[3].siteNumber"
        };

        Console.WriteLine($"📖 Reading {tagNames.Length} siteNumber tags (batch read)...");

        var results = await _plcConnection.ReadTagsAsync(tagNames);

        Console.WriteLine($"✅ Batch read completed: {results.Count} tags");
        Console.WriteLine();

        foreach (var tagName in tagNames)
        {
            if (results.TryGetValue(tagName, out var value))
            {
                if (value != null)
                {
                    Console.WriteLine($"   ✓ {tagName}: {value}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ {tagName}: null (tag may not exist or bad quality)");
                }
            }
            else
            {
                Console.WriteLine($"   ❌ {tagName}: not found in results");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Inicia escritura periódica de valores aleatorios al tag siteNumber cada 5 segundos.
    /// </summary>
    public void StartPeriodicWrites()
    {
        _cts = new CancellationTokenSource();

        _writeTimer = new System.Threading.Timer(async _ =>
        {
            try
            {
                var randomValue = _random.Next(1, 100);
                var tagPath = "ngpSampleCurrent.pallets[0].cavities[0].siteNumber";

                Console.WriteLine($"✏️ Writing {randomValue} to {tagPath}");

                // Descomentar para activar escritura real:
                // await _plcConnection.WriteTagAsync(tagPath, randomValue);

                Console.WriteLine("✅ Write successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Write error: {ex.Message}");
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));

        Console.WriteLine("✏️ Writing random values to ngpSampleCurrent.pallets[0].cavities[0].siteNumber every 5 seconds");
    }

    /// <summary>
    /// Detiene la escritura periódica y libera recursos.
    /// </summary>
    public void StopPeriodicWrites()
    {
        _cts?.Cancel();
        _writeTimer?.Dispose();
        _writeTimer = null;
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Libera todos los recursos (suscripción + timer).
    /// </summary>
    public async Task DisposeAsync()
    {
        await StopSubscriptionAsync();
        StopPeriodicWrites();
    }
}
