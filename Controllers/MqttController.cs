using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Conduit.Mqtt;

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
}
