using Sitas.Edge.EdgePlcDriver;
using Sitas.Edge.EdgePlcDriver.Messages;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Servicio de demostración para operaciones Edge PLC Driver (lectura y escritura al PLC).
/// Similar a MqttSubscriptionService, solo depende de la conexión PLC.
/// </summary>
public class AsCommDemoService
{
    private readonly IEdgePlcDriver _plcConnection;
    private readonly Random _random = new();
    private System.Threading.Timer? _writeTimer;
    private CancellationTokenSource? _cts;
    private IAsyncDisposable? _subscription;
    private int _updateCount = 0;

    public AsCommDemoService(IEdgePlcDriver plcConnection)
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
        Console.WriteLine("🚀 Starting Edge PLC Driver programmatic subscription...");

        // Reset counter
        _updateCount = 0;

        // Suscripción programática al tag (similar al atributo [EdgePlcDriverSubscribe])
        _subscription = await _plcConnection.SubscribeAsync<STRUCT_samples>(
            "ngpSampleCurrent",
            HandleSampleTagAsync,
            pollingIntervalMs: 1000,
            cancellationToken: cancellationToken);

        Console.WriteLine("✅ Edge PLC Driver subscription active: ngpSampleCurrent (1000ms polling)");
    }

    /// <summary>
    /// Handler que se ejecuta cada vez que el tag ngpSampleCurrent cambia.
    /// </summary>
    /// <param name="message">Valor del tag con metadata (Quality, Timestamp, etc.)</param>
    /// <param name="context">Contexto con métodos para leer/escribir tags al PLC</param>
    /// <param name="cancellationToken">
    /// ⚠️ IMPORTANTE: Este token NO es para hacer unsubscribe.
    /// 
    /// Para hacer UNSUBSCRIBE, usa: await _subscription.DisposeAsync()
    /// 
    /// El CancellationToken es para:
    /// 1. Cancelar operaciones asíncronas DENTRO del handler (ej: WriteTagAsync, ReadTagAsync)
    /// 2. Detectar si la suscripción fue cancelada externamente (si alguien llamó DisposeAsync)
    /// 3. Pasar el token a otras operaciones asíncronas para cancelación cooperativa
    /// 
    /// Ejemplos de uso:
    /// - await context.WriteTagAsync("SomeTag", value, cancellationToken);  // ✅ Pasar token a operaciones async
    /// - await context.ReadTagAsync<int>("SomeTag", cancellationToken);      // ✅ Pasar token a operaciones async
    /// - if (cancellationToken.IsCancellationRequested) return;              // ✅ Verificar si fue cancelado
    /// 
    /// Para UNSUBSCRIBE (fuera del handler):
    /// - await _subscription.DisposeAsync();  // ✅ Esto detiene la suscripción
    /// </param>
    private async Task HandleSampleTagAsync(
        TagValue<STRUCT_samples> message,
        IEdgePlcDriverMessageContext context,
        CancellationToken cancellationToken)
    {
        // Verificar si la operación fue cancelada antes de procesar
        cancellationToken.ThrowIfCancellationRequested();

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

        // Ejemplo: Si quisieras escribir un tag dentro del handler, usarías el cancellationToken:
        // await context.WriteTagAsync("SomeTag", value, cancellationToken);

        // After 5 prints, cancel the subscription
        if (_updateCount >= 5)
        {
            Console.WriteLine($"🛑 Reached 5 updates, stopping subscription...");
            if (_subscription != null)
            {
                await _subscription.DisposeAsync();
                _subscription = null;
                Console.WriteLine("✅ Subscription stopped");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Detiene la suscripción programática.
    /// </summary>
    public async Task StopSubscriptionAsync()
    {
        Console.WriteLine("🛑 Stopping Edge PLC Driver subscription...");
        
        if (_subscription != null)
        {
            await _subscription.DisposeAsync();
            _subscription = null;
        }

        Console.WriteLine("✅ Edge PLC Driver subscription stopped");
    }

    /// <summary>
    /// Lee el tag ngpSampleCurrent, modifica cavities[1].siteNumber a 5, y escribe la estructura completa de vuelta.
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

            // Mostrar info de las primeras 2 cavities si existen
            if (pallet.Cavities?.Length > 0)
            {
                var cavity0 = pallet.Cavities[0];
                Console.WriteLine($"      └─ Cavity[0] | ID: {cavity0.Identifier} | Site: {cavity0.SiteNumber} | Lot: {cavity0.LotNumber.Value}");
            }

            if (pallet.Cavities?.Length > 1)
            {
                var cavity1 = pallet.Cavities[1];
                Console.WriteLine($"      └─ Cavity[1] | ID: {cavity1.Identifier} | Site: {cavity1.SiteNumber} | Lot: {cavity1.LotNumber.Value}");

                // Modificar siteNumber de cavity[1] a 5
                Console.WriteLine($"\n✏️ Modificando Cavity[1].SiteNumber de {cavity1.SiteNumber} a 5...");
                cavity1.SiteNumber = 5;

                // Escribir la estructura completa de vuelta
                try
                {
                    await _plcConnection.WriteTagAsync(sampleTagName, sample);
                    Console.WriteLine("✅ Estructura completa escrita exitosamente");

                    // Leer de vuelta para confirmar
                    var readBack = await _plcConnection.ReadTagAsync<STRUCT_samples>(sampleTagName);
                    if (readBack.Quality == TagQuality.Good && readBack.Value.Pallets?.Length > 0 && readBack.Value.Pallets[0].Cavities?.Length > 1)
                    {
                        var newValue = readBack.Value.Pallets[0].Cavities[1].SiteNumber;
                        Console.WriteLine($"📖 Confirmación - Cavity[1].SiteNumber ahora es: {newValue}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error escribiendo estructura: {ex.Message}");
                }
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Lee múltiples tags siteNumber usando ReadTagsAsync<int> (batch read optimizado).
    /// Demuestra cómo leer varios tags primitivos del mismo tipo (int) en una sola operación.
    /// Usa la sobrecarga genérica que proporciona type-safety en tiempo de compilación.
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

        Console.WriteLine($"📖 Reading {tagNames.Length} siteNumber tags (batch read with type-safety)...");

        // Usa la sobrecarga genérica ReadTagsAsync<int> para type-safety
        // Ahora retorna IReadOnlyDictionary<string, TagValue<int>> con metadata completa
        var results = await _plcConnection.ReadTagsAsync<int>(tagNames);

        Console.WriteLine($"✅ Batch read completed: {results.Count} tags");
        Console.WriteLine();

        foreach (var tagName in tagNames)
        {
            if (results.TryGetValue(tagName, out var tagValue))
            {
                // tagValue es TagValue<int> con metadata (Quality, Timestamp, etc.)
                Console.WriteLine($"   ✓ {tagName}: {tagValue.Value} (Quality: {tagValue.Quality}, Timestamp: {tagValue.Timestamp:HH:mm:ss.fff})");
            }
            else
            {
                Console.WriteLine($"   ❌ {tagName}: not found in results");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Escribe el valor 5 al tag ngpSampleCurrent.pallets[0].cavities[1].siteNumber
    /// </summary>
    public async Task WriteSiteNumberToCavity1Async()
    {
        const string tagPath = "ngpSampleCurrent.pallets[0].cavities[1].siteNumber";
        const int newValue = 5;

        Console.WriteLine($"✏️ Writing {newValue} to {tagPath}");

        try
        {
            await _plcConnection.WriteTagAsync(tagPath, newValue);
            Console.WriteLine("✅ Write successful");

            // Leer de vuelta para confirmar
            var readBack = await _plcConnection.ReadTagAsync<int>(tagPath);
            if (readBack.Quality == TagQuality.Good)
            {
                Console.WriteLine($"📖 Read back value: {readBack.Value}");
            }
            else
            {
                Console.WriteLine($"⚠️ Read back quality: {readBack.Quality}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Write error: {ex.Message}");
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
                await _plcConnection.WriteTagAsync(tagPath, randomValue);

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
