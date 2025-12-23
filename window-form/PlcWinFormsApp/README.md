# PLC Windows Forms App

Aplicación Windows Forms que se conecta al PLC y lee el valor de `ngpSampleCurrent.pallets[0].cavities[0].lotNumber` cada 5 segundos, mostrándolo en un label.

## Requisitos

- .NET 8.0 SDK
- Windows (para compilar y ejecutar Windows Forms)
- Licencia ASComm IoT (para comunicación con el PLC)

## Configuración

La aplicación está configurada para conectarse al PLC en:
- IP: `192.168.8.55`
- Slot: `0`

Para cambiar la configuración, edita `MainForm.cs` línea 50-51.

## Compilar

```bash
dotnet build
```

## Ejecutar

```bash
dotnet run
```

O ejecuta el `.exe` generado en `bin/Debug/net8.0-windows/` o `bin/Release/net8.0-windows/`.

## Funcionalidad

- Se conecta automáticamente al PLC al iniciar
- Lee el tag `ngpSampleCurrent.pallets[0].cavities[0].lotNumber` cada 5 segundos
- Muestra el valor en un label en la interfaz
- Muestra el estado de conexión en otro label

## DLLs Incluidas

Las siguientes DLLs están en la carpeta `libs/`:
- `Sitas.Edge.Core.dll`
- `Sitas.Edge.EdgePlcDriver.dll`
- `Sitas.Edge.Mqtt.dll`
- `Sitas.Edge.DependencyInjection.dll`
- `AutomatedSolutions.ASCommStd.dll`

