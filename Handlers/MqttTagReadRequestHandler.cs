using System.Reflection;
using Sitas.Edge.Core.Abstractions;
using Sitas.Edge.Core.Attributes;
using Sitas.Edge.Core.Enums;
using Sitas.Edge.EdgePlcDriver;
using Sitas.Edge.Mqtt;
using Sitas.Edge.Mqtt.Attributes;
using ConduitPlcDemo.Messages;
using ConduitPlcDemo;
using ConduitPlcDemo.Types;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Handlers;

/// <summary>
/// Handler que se suscribe a MQTT para recibir peticiones de lectura de tags.
/// Cuando recibe una petición, lee el tag del PLC y publica la respuesta por MQTT.
/// 
/// Topic de suscripción: "plc/read-request"
/// Topic de respuesta: "plc/read-response"
/// </summary>
[MqttSubscribe("mqtt", "plc/read-request", QualityOfService.AtLeastOnce)]
public class MqttTagReadRequestHandler : IMessageSubscriptionHandler<TagReadRequest>
{
    private readonly ILogger<MqttTagReadRequestHandler> _logger;
    private readonly IEdgePlcDriver _plcConnection;
    private readonly IMqttConnection _mqtt;
    private int _requestCount = 0;

    public MqttTagReadRequestHandler(
        ILogger<MqttTagReadRequestHandler> logger,
        IEdgePlcDriver plcConnection,
        IMqttConnection mqtt)
    {
        _logger = logger;
        _plcConnection = plcConnection;
        _mqtt = mqtt;
        
        // Verificar si el PLC está disponible (no es NullEdgePlcDriver)
        if (_plcConnection is Services.NullEdgePlcDriver || !_plcConnection.IsConnected)
        {
            _logger.LogWarning("⚠️ MqttTagReadRequestHandler: PLC not available. This handler should run on a PC with ASComm license.");
        }
        else
        {
            _logger.LogInformation("✅ MqttTagReadRequestHandler instantiated - ready to receive tag read requests");
        }
    }

    public async Task HandleAsync(
        TagReadRequest request,
        IMessageContext context,
        CancellationToken cancellationToken = default)
    {
        _requestCount++;

        if (string.IsNullOrWhiteSpace(request.TagName))
        {
            _logger.LogWarning("⚠️ Received empty tag name in request #{Count}", _requestCount);
            
            var errorResponse = new TagReadResponse
            {
                TagName = string.Empty,
                Quality = "Bad",
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                HasError = true,
                ErrorMessage = "Tag name is required"
            };

            await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
            return;
        }

        try
        {
            // Verificar si el PLC está disponible (no es NullEdgePlcDriver)
            if (_plcConnection is Services.NullEdgePlcDriver || !_plcConnection.IsConnected)
            {
                _logger.LogWarning("⚠️ PLC not available on this PC. This handler should run on a PC with ASComm license.");
                
                var errorResponse = new TagReadResponse
                {
                    TagName = request.TagName,
                    Quality = "CommError",
                    Timestamp = DateTimeOffset.UtcNow,
                    CorrelationId = request.CorrelationId,
                    HasError = true,
                    ErrorMessage = "PLC not available on this PC. This handler should run on a PC with ASComm license."
                };

                await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
                return;
            }

            // Leer el tag del PLC dinámicamente usando el factory
            TagReadResponse response;

            // Buscar el tipo UDT correspondiente al tagName en el factory
            var udtType = UdtTypeFactory.GetType(request.TagName);
            
            if (udtType != null)
            {
                // Usar reflexión para llamar ReadTagAsync<T> con el tipo obtenido del factory
                var method = typeof(IEdgePlcDriver).GetMethod(nameof(IEdgePlcDriver.ReadTagAsync));
                if (method == null)
                {
                    throw new InvalidOperationException("ReadTagAsync method not found on IEdgePlcDriver");
                }

                var genericMethod = method.MakeGenericMethod(udtType);
                var task = (Task?)genericMethod.Invoke(_plcConnection, new object[] { request.TagName, cancellationToken });
                
                if (task == null)
                {
                    throw new InvalidOperationException("ReadTagAsync returned null");
                }

                await task.ConfigureAwait(false);

                // Obtener el resultado de la tarea
                var resultProperty = task.GetType().GetProperty("Result");
                var result = resultProperty?.GetValue(task);
                
                if (result == null)
                {
                    throw new InvalidOperationException("ReadTagAsync result is null");
                }

                // Extraer propiedades del resultado usando reflexión
                var tagNameProp = result.GetType().GetProperty("TagName");
                var valueProp = result.GetType().GetProperty("Value");
                var qualityProp = result.GetType().GetProperty("Quality");
                var timestampProp = result.GetType().GetProperty("Timestamp");

                response = new TagReadResponse
                {
                    TagName = tagNameProp?.GetValue(result) as string ?? request.TagName,
                    Value = valueProp?.GetValue(result),
                    Quality = qualityProp?.GetValue(result)?.ToString() ?? "Bad",
                    Timestamp = timestampProp?.GetValue(result) as DateTimeOffset? ?? DateTimeOffset.UtcNow,
                    CorrelationId = request.CorrelationId,
                    HasError = qualityProp?.GetValue(result)?.ToString() != "Good"
                };

                _logger.LogDebug("✅ Read tag '{TagName}' using UDT type '{UdtType}' from factory", 
                    request.TagName, udtType.Name);
            }
            else
            {
                // No se encontró en el factory, usar object como fallback
                var result = await _plcConnection.ReadTagAsync<object>(request.TagName, cancellationToken);
                
                response = new TagReadResponse
                {
                    TagName = result.TagName,
                    Value = result.Value,
                    Quality = result.Quality.ToString(),
                    Timestamp = result.Timestamp,
                    CorrelationId = request.CorrelationId,
                    HasError = result.Quality != Sitas.Edge.EdgePlcDriver.Messages.TagQuality.Good
                };

                _logger.LogDebug("⚠️ Tag '{TagName}' not found in factory, using object type", request.TagName);
            }
            
            await _mqtt.Publisher.PublishAsync("plc/read-response", response, cancellationToken: cancellationToken);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error reading tag {TagName} from PLC", request.TagName);

            var errorResponse = new TagReadResponse
            {
                TagName = request.TagName,
                Quality = "CommError",
                Timestamp = DateTimeOffset.UtcNow,
                CorrelationId = request.CorrelationId,
                HasError = true,
                ErrorMessage = ex.Message
            };

            await _mqtt.Publisher.PublishAsync("plc/read-response", errorResponse, cancellationToken: cancellationToken);
        }
    }
}
