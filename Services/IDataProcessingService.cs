namespace ConduitPlcDemo.Services;

/// <summary>
/// Servicio genérico para procesamiento de datos.
/// </summary>
public interface IDataProcessingService
{
    /// <summary>
    /// Procesa datos y retorna el contador actual.
    /// </summary>
    int ProcessData<TData>(TData data, string source);

    /// <summary>
    /// Obtiene el contador actual de mensajes procesados.
    /// </summary>
    int GetCount();
}
