using AutomatedSolutions.ASCommStd;
using ABLogix = AutomatedSolutions.ASCommStd.AB.Logix;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Servicio que lee directamente el tag ngpSampleCurrent.pallets usando ASComm sin pasar por EdgePlcDriver.
/// Se ejecuta al iniciar la aplicación.
/// </summary>
public class PalletsDirectReaderService
{
    private readonly ILogger<PalletsDirectReaderService> _logger;
    private readonly string _plcIp;
    private readonly int _plcSlot;
    
    private ABLogix.Net.Channel? _channel;
    private ABLogix.Device? _device;
    private ABLogix.Item? _palletsItem;

    public PalletsDirectReaderService(ILogger<PalletsDirectReaderService> logger, string plcIp, int plcSlot)
    {
        _logger = logger;
        _plcIp = plcIp;
        _plcSlot = plcSlot;
    }

    /// <summary>
    /// Inicializa la conexión ASComm y lee el tag ngpSampleCurrent.pallets
    /// </summary>
    public async Task ReadPalletsTagAsync()
    {
        try
        {
            _logger.LogInformation("🔧 Inicializando conexión ASComm directa para leer ngpSampleCurrent.pallets...");

            // 1. Crear Channel (conexión Ethernet)
            _channel = new ABLogix.Net.Channel();
            _channel.Error += (sender, e) =>
            {
                _logger.LogError("❌ Channel Error: {Message}", e.Message);
            };

            // 2. Crear Device (IP,Slot,Port) - formato: "IP,Backplane,Slot"
            var routePath = $"{_plcIp},{_plcSlot},0";
            _device = new ABLogix.Device(routePath, ABLogix.Model.ControlLogix);
            _device.TimeoutConnect = 5000; // 5 segundos
            _device.TimeoutTransaction = 3000; // 3 segundos
            _device.Error += (sender, e) =>
            {
                _logger.LogError("❌ Device Error: {Message}", e.Message);
            };

            // 3. Crear Group (para contener items)
            var group = new ABLogix.Group(false, 1000); // active=false, updateRate=1000ms

            // 4. Crear Item para el tag ngpSampleCurrent.pallets
            _palletsItem = new ABLogix.Item("palletsItem", "ngpSampleCurrent.pallets");

            // 5. Conectar la jerarquía
            _channel.Devices.Add(_device);
            _device.Groups.Add(group);
            group.Items.Add(_palletsItem);

            _logger.LogInformation("✅ Jerarquía ASComm creada. Conectando al PLC en {RoutePath}...", routePath);

            // 6. Leer el tag de forma asíncrona
            await _palletsItem.ReadAsync();

            // 7. Procesar el resultado
            if (_palletsItem.Values != null && _palletsItem.Values.Length > 0)
            {
                var firstValue = _palletsItem.Values[0];
                var valueType = firstValue?.GetType();
                var quality = _palletsItem.Quality;

                _logger.LogInformation("═══════════════════════════════════════════════════════════");
                _logger.LogInformation("📦 LECTURA DIRECTA DE ngpSampleCurrent.pallets");
                _logger.LogInformation("═══════════════════════════════════════════════════════════");
                _logger.LogInformation("   Quality: {Quality}", quality);
                _logger.LogInformation("   Value Type: {Type}", valueType?.FullName ?? "null");
                _logger.LogInformation("   Values.Length: {Length}", _palletsItem.Values.Length);

                if (firstValue != null)
                {
                    if (valueType!.IsArray)
                    {
                        var array = (System.Array)firstValue;
                        _logger.LogInformation("   Array Length: {ArrayLength}", array.Length);
                        
                        // Mostrar los primeros elementos
                        var elementsToShow = Math.Min(20, array.Length);
                        var elements = new List<object?>();
                        for (int i = 0; i < elementsToShow; i++)
                        {
                            elements.Add(array.GetValue(i));
                        }
                        _logger.LogInformation("   First {Count} elements: [{Elements}]", 
                            elementsToShow, 
                            string.Join(", ", elements.Select(e => e?.ToString() ?? "null")));
                        
                        if (array.Length > 20)
                        {
                            _logger.LogInformation("   ... ({Total} total elements)", array.Length);
                        }
                    }
                    else if (firstValue is string strValue)
                    {
                        _logger.LogInformation("   String Value: \"{Value}\"", strValue);
                        _logger.LogInformation("   String Length: {Length}", strValue.Length);
                    }
                    else
                    {
                        _logger.LogInformation("   Value: {Value}", firstValue);
                    }
                }

                _logger.LogInformation("═══════════════════════════════════════════════════════════");

                // Intentar interpretar como estructura si es un array grande
                if (firstValue is System.Array bytesArray && bytesArray.Length >= 4)
                {
                    try
                    {
                        var elem0 = bytesArray.GetValue(0);
                        var elem1 = bytesArray.GetValue(1);
                        var elem2 = bytesArray.GetValue(2);
                        var elem3 = bytesArray.GetValue(3);
                        
                        if (elem0 != null && elem1 != null && elem2 != null && elem3 != null)
                        {
                            var b0 = Convert.ToByte(elem0);
                            var b1 = Convert.ToByte(elem1);
                            var b2 = Convert.ToByte(elem2);
                            var b3 = Convert.ToByte(elem3);
                            
                            var stringLength = BitConverter.ToInt32(new byte[] { b0, b1, b2, b3 }, 0);
                            
                            if (stringLength >= 0 && stringLength <= 200 && bytesArray.Length >= 4 + stringLength)
                            {
                                var stringBytes = new byte[stringLength];
                                for (int i = 0; i < stringLength && (i + 4) < bytesArray.Length; i++)
                                {
                                    var byteVal = bytesArray.GetValue(i + 4);
                                    if (byteVal != null)
                                    {
                                        stringBytes[i] = Convert.ToByte(byteVal);
                                    }
                                }
                                var decodedString = System.Text.Encoding.ASCII.GetString(stringBytes);
                                _logger.LogInformation("   🔍 Interpretado como LOGIX_STRING (length={Length}): \"{String}\"", 
                                    stringLength, decodedString);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "No se pudo interpretar como LOGIX_STRING");
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No se recibieron valores del tag");
            }

            _logger.LogInformation("✅ Lectura directa completada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al leer ngpSampleCurrent.pallets con ASComm directo");
        }
        finally
        {
            // Limpiar recursos
            Cleanup();
        }
    }

    private void Cleanup()
    {
        try
        {
            if (_palletsItem != null && _device != null && _device.Groups.Count > 0)
            {
                var group = _device.Groups[0];
                group.Items.Remove(_palletsItem);
                _palletsItem = null;
            }

            if (_device != null && _channel != null)
            {
                _channel.Devices.Remove(_device);
                _device = null;
            }

            _channel = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al limpiar recursos ASComm");
        }
    }
}

