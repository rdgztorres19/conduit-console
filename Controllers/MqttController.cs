using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Conduit.Mqtt;
using ConduitPlcDemo.Messages;
using ConduitPlcDemo.Types;

namespace ConduitPlcDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MqttController : ControllerBase
{
    private readonly IMqttConnection _mqttConnection;
    private readonly ILogger<MqttController> _logger;

    public MqttController(IMqttConnection mqttConnection, ILogger<MqttController> logger)
    {
        _mqttConnection = mqttConnection;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado de la conexión MQTT
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            connectionName = _mqttConnection.ConnectionName,
            isConnected = _mqttConnection.IsConnected,
            state = _mqttConnection.State.ToString()
        });
    }

    /// <summary>
    /// Publica un mensaje en un topic MQTT
    /// </summary>
    /// <param name="topic">Topic donde publicar</param>
    /// <param name="message">Mensaje a publicar</param>
    [HttpPost("publish/{topic}")]
    public async Task<IActionResult> Publish(string topic, [FromBody] object message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_mqttConnection.IsConnected)
            {
                return StatusCode(503, new { error = "MQTT not connected", state = _mqttConnection.State.ToString() });
            }

            await _mqttConnection.Publisher.PublishAsync(topic, message, cancellationToken: cancellationToken);
            
            return Ok(new { message = $"Published to topic {topic}", topic, payload = message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to topic {Topic}", topic);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene la lista de tags root disponibles del UdtTypeFactory
    /// </summary>
    [HttpGet("tags/available")]
    public IActionResult GetAvailableTags()
    {
        try
        {
            // Obtener solo los tags root (líneas 17-24 del UdtTypeFactory)
            var rootTags = new[]
            {
                "ngpLotCurrent",
                "ngpSampleCurrent",
                "tagNgpInstrument",
                "tagNgpLot",
                "tagNgpLotBlank",
                "tagNgpSample",
                "tagNgpSampleBlank",
                "inDataFtoptix"
            };

            return Ok(new
            {
                tags = rootTags.Select(tag => new
                {
                    name = tag,
                    type = UdtTypeFactory.GetType(tag)?.Name ?? "Unknown"
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tags");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Envía una petición de lectura de tag por MQTT
    /// </summary>
    /// <param name="request">Request con tagName y correlationId opcional</param>
    [HttpPost("tags/read-request")]
    public async Task<IActionResult> SendTagReadRequest([FromBody] TagReadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_mqttConnection.IsConnected)
            {
                return StatusCode(503, new { error = "MQTT not connected", state = _mqttConnection.State.ToString() });
            }

            if (string.IsNullOrWhiteSpace(request.TagName))
            {
                return BadRequest(new { error = "TagName is required" });
            }

            // Generar correlationId si no se proporciona
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                request.CorrelationId = Guid.NewGuid().ToString();
            }

            // Publicar en el topic plc/read-request
            await _mqttConnection.Publisher.PublishAsync("plc/read-request", request, cancellationToken: cancellationToken);
            
            _logger.LogInformation("📤 Tag read request sent via MQTT | Tag: {TagName} | CorrelationId: {CorrelationId}", 
                request.TagName, request.CorrelationId);

            return Ok(new 
            { 
                message = $"Tag read request sent for {request.TagName}",
                tagName = request.TagName,
                correlationId = request.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tag read request for {TagName}", request.TagName);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Envía una petición de escritura de tag por MQTT
    /// </summary>
    /// <param name="request">Request con tagName, path, value y correlationId opcional</param>
    [HttpPost("tags/write-request")]
    public async Task<IActionResult> SendTagWriteRequest([FromBody] TagWriteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_mqttConnection.IsConnected)
            {
                return StatusCode(503, new { error = "MQTT not connected", state = _mqttConnection.State.ToString() });
            }

            if (string.IsNullOrWhiteSpace(request.TagName))
            {
                return BadRequest(new { error = "TagName is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            // Generar correlationId si no se proporciona
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                request.CorrelationId = Guid.NewGuid().ToString();
            }

            // Publicar en el topic plc/write-request
            await _mqttConnection.Publisher.PublishAsync("plc/write-request", request, cancellationToken: cancellationToken);
            
            _logger.LogInformation("📤 Tag write request sent via MQTT | Tag: {TagName} | Path: {Path} | CorrelationId: {CorrelationId}", 
                request.TagName, request.Path, request.CorrelationId);

            return Ok(new 
            { 
                message = $"Tag write request sent for {request.TagName}.{request.Path}",
                tagName = request.TagName,
                path = request.Path,
                correlationId = request.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending tag write request for {TagName}.{Path}", request.TagName, request.Path);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
