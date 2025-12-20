# Conduit PLC Demo - Web API

Esta aplicación ahora funciona como **aplicación híbrida**: consola + Web API.

## Características

- ✅ **Consola**: Mantiene toda la funcionalidad original de consola
- ✅ **Web API**: Expone endpoints REST para interactuar con PLC y MQTT
- ✅ **Swagger UI**: Documentación interactiva de la API

## Endpoints Disponibles

### PLC Controller (`/api/plc`)

#### GET `/api/plc/status`
Obtiene el estado de la conexión del PLC.

**Respuesta:**
```json
{
  "connectionName": "plc1",
  "isConnected": true,
  "state": "Connected",
  "ipAddress": "192.168.8.55",
  "routePath": "192.168.8.55,1,0"
}
```

#### GET `/api/plc/tags/{tagName}`
Lee un tag del PLC (tipo genérico `object`).

**Ejemplo:**
```
GET /api/plc/tags/Sensor_Temperature
```

**Respuesta:**
```json
{
  "tagName": "Sensor_Temperature",
  "value": 25.5,
  "quality": "Good",
  "timestamp": "2024-01-15T10:30:00Z",
  "previousValue": 25.0
}
```

#### POST `/api/plc/tags/{tagName}`
Escribe un valor a un tag del PLC.

**Ejemplo:**
```
POST /api/plc/tags/Motor_Speed
Content-Type: application/json

42
```

**Respuesta:**
```json
{
  "message": "Tag Motor_Speed written successfully",
  "value": 42
}
```

#### POST `/api/plc/tags/batch`
Lee múltiples tags en una sola operación.

**Ejemplo:**
```
POST /api/plc/tags/batch
Content-Type: application/json

["Sensor_Temperature", "Motor_Speed", "Pressure"]
```

**Respuesta:**
```json
{
  "Sensor_Temperature": 25.5,
  "Motor_Speed": 1500,
  "Pressure": 101.3
}
```

#### POST `/api/plc/tags/batch-write`
Escribe múltiples tags en una sola operación.

**Ejemplo:**
```
POST /api/plc/tags/batch-write
Content-Type: application/json

{
  "Motor_Speed": 1500,
  "Pressure_Setpoint": 100.0
}
```

### MQTT Controller (`/api/mqtt`)

#### GET `/api/mqtt/status`
Obtiene el estado de la conexión MQTT.

**Respuesta:**
```json
{
  "connectionName": "mqtt",
  "isConnected": true,
  "state": "Connected"
}
```

#### POST `/api/mqtt/publish/{topic}`
Publica un mensaje en un topic MQTT.

**Ejemplo:**
```
POST /api/mqtt/publish/sensors/temperature
Content-Type: application/json

{
  "value": 25.5,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Respuesta:**
```json
{
  "message": "Published to topic sensors/temperature",
  "topic": "sensors/temperature",
  "payload": {
    "value": 25.5,
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

## Ejecutar la Aplicación

```bash
dotnet run --project ConduitPlcDemo
```

La aplicación iniciará:
- ✅ Conexión al PLC
- ✅ Conexión a MQTT
- ✅ Servidor Web API en `http://localhost:5000` y `https://localhost:5001`
- ✅ Swagger UI en `http://localhost:5000/swagger` o `https://localhost:5001/swagger`

## Usar Swagger UI

1. Ejecuta la aplicación
2. Abre tu navegador en `http://localhost:5000/swagger`
3. Explora los endpoints disponibles
4. Prueba los endpoints directamente desde Swagger

## Ejemplos de Uso con cURL

### Leer estado del PLC
```bash
curl http://localhost:5000/api/plc/status
```

### Leer un tag
```bash
curl http://localhost:5000/api/plc/tags/Sensor_Temperature
```

### Escribir un tag
```bash
curl -X POST http://localhost:5000/api/plc/tags/Motor_Speed \
  -H "Content-Type: application/json" \
  -d "1500"
```

### Leer múltiples tags
```bash
curl -X POST http://localhost:5000/api/plc/tags/batch \
  -H "Content-Type: application/json" \
  -d '["Sensor_Temperature", "Motor_Speed"]'
```

### Publicar en MQTT
```bash
curl -X POST http://localhost:5000/api/mqtt/publish/sensors/data \
  -H "Content-Type: application/json" \
  -d '{"temperature": 25.5, "humidity": 60}'
```

## Notas

- La aplicación mantiene toda la funcionalidad de consola original
- Los handlers basados en atributos siguen funcionando
- La Web API se ejecuta en paralelo con la consola
- Usa CTRL+C para detener la aplicación (cierra tanto consola como Web API)
