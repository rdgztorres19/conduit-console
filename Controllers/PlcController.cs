using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sitas.Edge.EdgePlcDriver;
using Sitas.Edge.EdgePlcDriver.Messages;
using ConduitPlcDemo;
using System.Text.Json;

namespace ConduitPlcDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlcController : ControllerBase
{
    private readonly IEdgePlcDriver _plcConnection;
    private readonly ILogger<PlcController> _logger;

    public PlcController(IEdgePlcDriver plcConnection, ILogger<PlcController> logger)
    {
        _plcConnection = plcConnection;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado de la conexión del PLC
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            connectionName = _plcConnection.ConnectionName,
            isConnected = _plcConnection.IsConnected,
            state = _plcConnection.State.ToString(),
            ipAddress = _plcConnection.IpAddress,
            routePath = _plcConnection.RoutePath
        });
    }

    /// <summary>
    /// Lee un tag del PLC
    /// </summary>
    /// <param name="tagName">Nombre del tag a leer</param>
    [HttpGet("tags/{tagName}")]
    public async Task<IActionResult> ReadTag(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plcConnection.IsConnected)
            {
                return StatusCode(503, new { error = "PLC not connected", state = _plcConnection.State.ToString() });
            }

            // Si es ngpSampleCurrent, leer como STRUCT_samples
            if (tagName.Equals("ngpSampleCurrent", StringComparison.OrdinalIgnoreCase))
            {
                var result = await _plcConnection.ReadTagAsync<STRUCT_samples>(tagName, cancellationToken);
                
                return Ok(new
                {
                    tagName = result.TagName,
                    value = result.Value, // Esto será la estructura STRUCT_samples
                    quality = result.Quality.ToString(),
                    timestamp = result.Timestamp,
                    previousValue = result.PreviousValue
                });
            }
            
            // Para otros tags, intentar leer como object
            var genericResult = await _plcConnection.ReadTagAsync<object>(tagName, cancellationToken);
            
            return Ok(new
            {
                tagName = genericResult.TagName,
                value = genericResult.Value,
                quality = genericResult.Quality.ToString(),
                timestamp = genericResult.Timestamp,
                previousValue = genericResult.PreviousValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tag {TagName}", tagName);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lee un tag del PLC con tipo específico
    /// </summary>
    /// <typeparam name="T">Tipo del valor del tag</typeparam>
    /// <param name="tagName">Nombre del tag a leer</param>
    [HttpGet("tags/{tagName}/typed")]
    public async Task<IActionResult> ReadTagTyped<T>(string tagName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plcConnection.IsConnected)
            {
                return StatusCode(503, new { error = "PLC not connected", state = _plcConnection.State.ToString() });
            }

            var result = await _plcConnection.ReadTagAsync<T>(tagName, cancellationToken);
            
            return Ok(new
            {
                tagName = result.TagName,
                value = result.Value,
                quality = result.Quality.ToString(),
                timestamp = result.Timestamp,
                previousValue = result.PreviousValue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tag {TagName} as {Type}", tagName, typeof(T).Name);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lee múltiples tags del PLC
    /// </summary>
    /// <param name="tagNames">Lista de nombres de tags a leer</param>
    [HttpPost("tags/batch")]
    public async Task<IActionResult> ReadTags([FromBody] string[] tagNames, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plcConnection.IsConnected)
            {
                return StatusCode(503, new { error = "PLC not connected", state = _plcConnection.State.ToString() });
            }

            var results = await _plcConnection.ReadTagsAsync(tagNames, cancellationToken);
            
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tags batch");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Escribe un valor a un tag del PLC
    /// </summary>
    /// <param name="tagName">Nombre del tag a escribir</param>
    /// <param name="value">Valor a escribir</param>
    [HttpPost("tags/{tagName}")]
    public async Task<IActionResult> WriteTag(string tagName, [FromBody] object value, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plcConnection.IsConnected)
            {
                return StatusCode(503, new { error = "PLC not connected", state = _plcConnection.State.ToString() });
            }

            await _plcConnection.WriteTagAsync(tagName, value, cancellationToken);
            
            return Ok(new { message = $"Tag {tagName} written successfully", value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing tag {TagName}", tagName);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Escribe un valor primitivo a un path específico dentro de un tag estructurado
    /// </summary>
    /// <param name="tagName">Nombre del tag base (ej: ngpSampleCurrent)</param>
    /// <param name="request">Request con path y value</param>
    [HttpPost("tags/{tagName}/write-path")]
    public async Task<IActionResult> WriteTagPath(string tagName, [FromBody] WriteTagPathRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plcConnection.IsConnected)
            {
                return StatusCode(503, new { error = "PLC not connected", state = _plcConnection.State.ToString() });
            }

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            // Construir el tagName completo: tagName.path
            var fullTagName = $"{tagName}.{request.Path}";
            
            // Extraer el valor real si viene como JsonElement
            object? rawValue = request.Value;
            if (request.Value is JsonElement jsonElement)
            {
                rawValue = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetInt32(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => jsonElement.GetRawText()
                };
            }
            
            _logger.LogDebug("Writing to path: {FullTagName} with value: {Value} (raw: {RawValue})", fullTagName, request.Value, rawValue);

            await _plcConnection.WriteTagAsync(fullTagName, rawValue, cancellationToken);
            
            return Ok(new { 
                message = $"Path {request.Path} written successfully", 
                tagName = fullTagName,
                path = request.Path,
                value = request.Value 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing tag path {TagName}.{Path}", tagName, request.Path);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Request model para escribir un path específico
    /// </summary>
    public class WriteTagPathRequest
    {
        public string Path { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    /// <summary>
    /// Escribe múltiples tags del PLC
    /// </summary>
    /// <param name="tagValues">Diccionario de tag names y valores</param>
    [HttpPost("tags/batch-write")]
    public async Task<IActionResult> WriteTags([FromBody] Dictionary<string, object> tagValues, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_plcConnection.IsConnected)
            {
                return StatusCode(503, new { error = "PLC not connected", state = _plcConnection.State.ToString() });
            }

            await _plcConnection.WriteTagsAsync(tagValues, cancellationToken);
            
            return Ok(new { message = $"{tagValues.Count} tags written successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing tags batch");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
