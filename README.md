# ConduitPlcDemo - Standalone Application

Este proyecto ahora es **completamente portable**. Puedes copiarlo a cualquier lugar y funcionará sin dependencias externas.

## Estructura

```
ConduitPlcDemo/
├── Program.cs
├── UdtTypes.cs
├── Handlers/
│   ├── InterferometerAnalysisHandler.cs
│   └── SampleTagHandler.cs
├── libs/                          ← DLLs de Conduit incluidas
│   ├── Conduit.Core.dll
│   └── Conduit.EdgePlcDriver.dll
└── ConduitPlcDemo.csproj
```

## Configuración PLC

- **IP**: `192.168.8.55`
- **Slot**: `0`
- **Tags monitoreados**:
  - `Program:UDT_NGP_INTERFEROMETER_ANALYSIS_TAG` (Unsolicited - 10ms)
  - `ngpSampleCurrent` (Polling - 1000ms)

## Uso

```bash
# Ejecutar directamente
dotnet run

# O compilar y ejecutar el .exe
dotnet build
./bin/Debug/net8.0/ConduitPlcDemo
```

## Portabilidad

✅ **Este proyecto es auto-contenido**. Puedes:
- Copiarlo a cualquier carpeta
- Enviarlo por email/USB
- Compartirlo con otros desarrolladores
- Las DLLs de Conduit están en `libs/` y se copian automáticamente al output

## Dependencias

Solo necesitas:
- .NET 8.0 SDK/Runtime
- ASComm IoT con licencia válida (para comunicación con el PLC)

No necesitas el código fuente de Conduit ni referencias externas.
