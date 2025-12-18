using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Implementación del servicio de procesamiento de datos.
/// </summary>
public class DataProcessingService : IDataProcessingService
{
    private readonly ILogger<DataProcessingService> _logger;
    private int _counter;

    public DataProcessingService(ILogger<DataProcessingService> logger)
    {
        _logger = logger;
    }

    public int ProcessData<TData>(TData data, string source)
    {
        _counter++;
        _logger.LogInformation(
            "📊 Processed #{Count} | Source: {Source} | Type: {DataType}",
            _counter,
            source,
            typeof(TData).Name);
        return _counter;
    }

    public int GetCount() => _counter;
}
