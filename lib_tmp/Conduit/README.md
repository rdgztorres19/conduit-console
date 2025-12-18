# Conduit

A modern, extensible .NET service bus library for multi-protocol messaging. Built with **Builder**, **Strategy**, and **Attribute-Based Discovery** patterns for a clean, intuitive developer experience.

## Features

- 🔌 **Multi-Protocol Support** - MQTT, ASComm (Allen-Bradley PLCs), and extensible to AMQP, Kafka, etc.
- 🏗️ **Fluent Builder API** - Intuitive configuration with IntelliSense support
- 🎯 **Attribute-Based Handlers** - Declare subscriptions with simple attributes
- 💉 **Dependency Injection** - Works with any DI container (Microsoft DI, Autofac, SimpleInjector, Ninject, Lamar, DryIoc)
- 🔄 **Auto-Reconnect** - Resilient connections with exponential backoff
- 📦 **Cross-Platform** - Works with Console, WPF, Windows Forms, ASP.NET Core
- 🎭 **Strongly-Typed Messages** - Type-safe message handling with automatic serialization

## Available Packages

| Package | Description |
|---------|-------------|
| `Conduit.Core` | Core abstractions and interfaces |
| `Conduit.Mqtt` | MQTT protocol support |
| `Conduit.AsComm` | Allen-Bradley PLC communication via ASComm IoT |
| `Conduit.DependencyInjection` | DI extensions for ASP.NET Core and Generic Host |

---

## 🏭 Conduit.AsComm - Allen-Bradley PLC Communication

Connect to Allen-Bradley PLCs using the ASComm IoT library from Automated Solutions.

### Supported PLC Families

- ControlLogix
- CompactLogix
- GuardPLC
- SoftLogix
- Micro800 (Micro820, Micro830, Micro850, Micro870, Micro880)

### Installation

```bash
# Add the AsComm package
dotnet add package Conduit.AsComm

# NOTE: You also need a valid ASComm IoT license from Automated Solutions
# Visit: https://automatedsolutions.com/products/iot/ascommiot/
```

### Quick Start - Reading and Writing Tags

```csharp
using Conduit.Core;
using Conduit.AsComm;

// Create and configure the PLC connection
var connection = AsCommClientBuilder.Create()
    .WithConnectionName("plc1")
    .WithPlc("192.168.1.10", cpuSlot: 0)
    .WithDefaultPollingInterval(100)
    .Build();

// Connect to the PLC
await connection.ConnectAsync();

// Read a tag
var temperature = await connection.ReadTagAsync<float>("Sensor_Temperature");
Console.WriteLine($"Temperature: {temperature.Value}°C (Quality: {temperature.Quality})");

// Write a tag
await connection.WriteTagAsync("Setpoint_Temperature", 75.5f);

// Read multiple tags
var values = await connection.ReadTagsAsync(new[] { "Tag1", "Tag2", "Tag3" });

// Clean shutdown
await connection.DisposeAsync();
```

### Attribute-Based Tag Subscriptions

```csharp
using Conduit.Core.Abstractions;
using Conduit.AsComm;
using Conduit.AsComm.Attributes;
using Conduit.AsComm.Messages;

// Define a handler that automatically subscribes to PLC tag changes
[AsCommSubscribe("plc1", "Sensor_Temperature", pollingIntervalMs: 100)]
public class TemperatureHandler : IMessageSubscriptionHandler<TagValue<float>>
{
    private readonly ILogger<TemperatureHandler> _logger;
    private readonly IConduit _conduit;

    public TemperatureHandler(ILogger<TemperatureHandler> logger, IConduit conduit)
    {
        _logger = logger;
        _conduit = conduit;
    }

    public async Task HandleAsync(
        TagValue<float> message,
        IMessageContext context,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Temperature changed: {Previous}°C → {Current}°C",
            message.PreviousValue,
            message.Value);

        // Write back to PLC if needed
        if (message.Value > 100f)
        {
            // No uses casts/instanceof con context: inyecta dependencias.
            var plc = _conduit.GetConnection<IAsCommConnection>();
            await plc.WriteTagAsync("Alarm_HighTemp", true, ct);
        }
    }
}
```

### Handler Examples by Data Type

| Type | C# Handler | Example |
|------|------------|---------|
| `float/REAL` | `TagValue<float>` | Temperature sensor with alarm |
| `double/LREAL` | `TagValue<double>` | Motor speed with Deadband |
| `int/DINT` | `TagValue<int>` | Production counter with milestones |
| `bool/BOOL` | `TagValue<bool>` | Machine running status |
| `UDT` | `TagValue<MachineStatusUdt>` | Structured data - strongly typed |
| `Array` | `TagValue<float[]>` | Zone temperatures |
| `STRING` | `TagValue<LogixString>` | Status messages |

#### UDT Handler Example (Strongly Typed)
```csharp
// 1. Define UDT matching PLC structure
[StructLayout(LayoutKind.Sequential)]
public class MachineStatusUdt
{
    public Boolean running;
    public Boolean faulted;
    public Int32 productCount;
    public Single cycleTime;
    public LogixString operatorName = new();
}

// 2. Handler receives typed data directly
[AsCommSubscribe("plc1", "Machine_Status_UDT", pollingIntervalMs: 500)]
public class MachineStatusUdtHandler : IMessageSubscriptionHandler<TagValue<MachineStatusUdt>>
{
    public Task HandleAsync(TagValue<MachineStatusUdt> message, IMessageContext context, CancellationToken ct)
    {
        var status = message.Value; // Typed! No casting needed
        Console.WriteLine($"Running: {status.running} | Count: {status.productCount}");
        return Task.CompletedTask;
    }
}
```

#### Array Handler Example
```csharp
[AsCommSubscribe("plc1", "Zone_Temperatures", pollingIntervalMs: 500, OnChangeOnly = true)]
public class ZoneTemperaturesHandler : IMessageSubscriptionHandler<TagValue<float[]>>
{
    public Task HandleAsync(TagValue<float[]> message, IMessageContext context, CancellationToken ct)
    {
        var temps = message.Value;
        Console.WriteLine($"Avg: {temps.Average():F1}°C | Min: {temps.Min():F1}°C | Max: {temps.Max():F1}°C");
        return Task.CompletedTask;
    }
}
```

#### String Handler Example
```csharp
[AsCommSubscribe("plc1", "Status_Message", pollingIntervalMs: 1000, OnChangeOnly = true)]
public class StatusMessageHandler : IMessageSubscriptionHandler<TagValue<LogixString>>
{
    public Task HandleAsync(TagValue<LogixString> message, IMessageContext context, CancellationToken ct)
    {
        Console.WriteLine($"PLC says: \"{message.Value.Value}\"");
        return Task.CompletedTask;
    }
}
```

### Subscription Modes: Polling vs Unsolicited

Conduit.AsComm supports two subscription modes for monitoring PLC tag changes:

> **Implementation Note:** Both modes use ASComm's polling mechanism internally. "Unsolicited" mode achieves lower latency by using a very fast polling interval (10ms) compared to standard polling (100ms+). While not true PLC push notifications, this provides near real-time response for critical tags.

#### 📊 Polling Mode (Default)
**How it works:** Conduit reads tag values at regular intervals (e.g., every 100ms)

**Pros:**
- ✅ Lower PLC CPU overhead
- ✅ Predictable network traffic
- ✅ Works with all tag types
- ✅ Suitable for most scenarios

**Cons:**
- ⚠️ Higher latency (limited by polling interval)
- ⚠️ May miss very fast changes between polls

**Example:**
```csharp
// Polling mode (default) - checks every 100ms
[AsCommSubscribe("plc1", "Sensor_Temperature", pollingIntervalMs: 100)]
public class TemperatureHandler : IMessageSubscriptionHandler<TagValue<float>>
{
    public Task HandleAsync(TagValue<float> message, IMessageContext context, CancellationToken ct)
    {
        Console.WriteLine($"Temperature: {message.Value}°C");
        return Task.CompletedTask;
    }
}
```

#### ⚡ Unsolicited Mode (Fast Polling)
**How it works:** Uses very fast polling (10ms) for near real-time response to tag changes

**Pros:**
- ✅ Lower latency (10ms vs 100-1000ms typical polling)
- ✅ Captures fast changes
- ✅ Works with all PLC models

**Cons:**
- ⚠️ Higher PLC CPU overhead due to faster polling
- ⚠️ More network traffic
- ⚠️ Use selectively for critical tags only

**Example:**
```csharp
using Conduit.AsComm.Attributes;

// Unsolicited mode - fast polling (10ms) for critical tags
[AsCommSubscribe("plc1", "Emergency_Stop", mode: TagSubscriptionMode.Unsolicited)]
public class EmergencyStopHandler : IMessageSubscriptionHandler<TagValue<bool>>
{
    public Task HandleAsync(TagValue<bool> message, IMessageContext context, CancellationToken ct)
    {
        if (message.Value)
        {
            Console.WriteLine("🚨 EMERGENCY STOP ACTIVATED!");
            // Take immediate action...
        }
        return Task.CompletedTask;
    }
}
```

#### When to Use Each Mode

| Scenario | Recommended Mode | Reasoning |
|----------|-----------------|-----------|
| **Safety signals** (E-stop, alarms) | **Unsolicited** | Critical - need 10ms response time |
| **Fast-changing values** (< 50ms) | **Unsolicited** | Fast polling captures rapid changes |
| **Temperature sensors** | **Polling** | Slow changes, standard polling sufficient |
| **Production counters** | **Polling** | Infrequent updates, lower overhead |
| **Status flags** | **Unsolicited** | State changes require quick response |
| **High-frequency data** (vibration) | **Polling** | Use data historian or buffering |

#### Hybrid Approach
You can mix both modes in the same application:

```csharp
// Critical tags use unsolicited mode (10ms fast polling)
[AsCommSubscribe("plc1", "Safety_Interlock", mode: TagSubscriptionMode.Unsolicited)]
public class InterlockHandler : IMessageSubscriptionHandler<TagValue<bool>>
{
    public Task HandleAsync(TagValue<bool> msg, IMessageContext ctx, CancellationToken ct)
    {
        // 10ms response time for safety-critical signals
        return Task.CompletedTask;
    }
}

// Non-critical tags use standard polling mode (custom interval)
[AsCommSubscribe("plc1", "Ambient_Temperature", pollingIntervalMs: 5000)]
public class AmbientTempHandler : IMessageSubscriptionHandler<TagValue<float>>
{
    public Task HandleAsync(TagValue<float> msg, IMessageContext ctx, CancellationToken ct)
    {
        // 5 second polling for ambient monitoring
        return Task.CompletedTask;
    }
}
```

#### Performance Comparison

| Mode | Polling Interval | Response Latency | PLC CPU | Network Traffic | Use Cases |
|------|-----------------|------------------|---------|----------------|-----------|
| **Polling** | 100-5000ms | 100-5000ms | Low | Low | Temperature, counters, status |
| **Polling (Custom)** | User-defined | Same as interval | Medium | Medium | Application-specific |
| **Unsolicited** | 10ms (fixed) | 10-20ms | High | High | Safety, alarms, critical I/O |

**Best Practice:** Start with polling mode for all tags. Add unsolicited mode only for tags that require < 50ms response time.

> 📖 **[Read Full Documentation](src/Conduit.AsComm/SUBSCRIPTION_MODES.md)** - Detailed guide with examples, best practices, and performance analysis.

### Configuration Options

```csharp
var connection = AsCommClientBuilder.Create()
    .WithConnectionName("plc1")                    // Logical name for handler matching
    .WithPlc("192.168.1.10", cpuSlot: 0, backplane: 1)  // PLC address
    .WithDefaultPollingInterval(100)               // Default polling rate (ms)
    .WithConnectionTimeout(10)                     // Connection timeout (seconds)
    .WithAutoReconnect(enabled: true, maxDelaySeconds: 30)
    .WithHandlersFromEntryAssembly()              // Auto-discover handlers
    .Build();
```

### Dynamic Tag Subscriptions

```csharp
// Subscribe to a tag at runtime
var subscription = await connection.SubscribeAsync<int>(
    "Counter_Production",
    async (tagValue, context, ct) =>
    {
        Console.WriteLine($"Count: {tagValue.Value}");
        
        if (tagValue.Value >= 1000)
        {
            // Escribe usando la conexión (sin depender de context)
            await connection.WriteTagAsync("Counter_Production", 0, ct);
        }
    },
    pollingIntervalMs: 500);

// Later, unsubscribe
await subscription.DisposeAsync();
```

### Working with User-Defined Types (UDTs)

```csharp
using System.Runtime.InteropServices;
using Conduit.AsComm.DataTypes;

// Define a C# class that matches your PLC UDT structure
// IMPORTANT: Use [StructLayout(LayoutKind.Sequential)] and public FIELDS
[StructLayout(LayoutKind.Sequential)]
public class MachineStatus
{
    public Boolean running;           // BOOL
    public Boolean faulted;           // BOOL
    public Int32 productCount;        // DINT
    public Single cycleTime;          // REAL
    public LogixString operatorName = new();  // STRING
}

// Read a UDT - returns strongly typed!
var tag = await connection.ReadTagAsync<MachineStatus>("Machine1_Status");
Console.WriteLine($"Running: {tag.Value.running}, Count: {tag.Value.productCount}");

// Write a UDT
var newStatus = new MachineStatus
{
    running = true,
    faulted = false,
    productCount = 0,
    cycleTime = 2.5f
};
newStatus.operatorName.SetString("John Doe");

await connection.WriteTagAsync("Machine1_Status", newStatus);
```

#### UDT with Arrays
```csharp
[StructLayout(LayoutKind.Sequential)]
public class ProductionBatchUdt
{
    public LogixString batchId = new();       // STRING
    public LogixString productCode = new();   // STRING
    public Int32 targetQuantity;              // DINT
    public Int32 completedQuantity;           // DINT
    public Single[] hourlyProduction = new Single[24];  // REAL[24]
    public Boolean isComplete;                // BOOL
}

// Handler for UDT with arrays
[AsCommSubscribe("plc1", "Current_Batch", pollingIntervalMs: 2000)]
public class ProductionBatchHandler : IMessageSubscriptionHandler<TagValue<ProductionBatchUdt>>
{
    public Task HandleAsync(TagValue<ProductionBatchUdt> message, IMessageContext context, CancellationToken ct)
    {
        var batch = message.Value;
        Console.WriteLine($"Batch: {batch.batchId.Value} | Progress: {batch.completedQuantity}/{batch.targetQuantity}");
        
        // Access array elements
        for (int i = 0; i < batch.hourlyProduction.Length; i++)
        {
            if (batch.hourlyProduction[i] > 0)
                Console.WriteLine($"  Hour {i}: {batch.hourlyProduction[i]} units");
        }
        return Task.CompletedTask;
    }
}
```

### UDT Guidelines

| Rule | Description |
|------|-------------|
| ✅ Use `[StructLayout(LayoutKind.Sequential)]` | Required for proper memory layout |
| ✅ Use public **fields** | `public Int32 value;` |
| ✅ Match field order exactly | Same order as RSLogix/Studio 5000 |
| ✅ Initialize arrays and nested types | `public Int16[] data = new Int16[10];` |
| ❌ Don't use auto-properties | `public int Value { get; set; }` won't work |

### Data Type Mapping

| PLC Type | C# Type | Bytes |
|----------|---------|-------|
| BOOL | Boolean | 4 (in UDT) |
| SINT | SByte | 1 |
| INT | Int16 | 2 |
| DINT | Int32 | 4 |
| LINT | Int64 | 8 |
| USINT | Byte | 1 |
| UINT | UInt16 | 2 |
| UDINT | UInt32 | 4 |
| ULINT | UInt64 | 8 |
| REAL | Single/float | 4 |
| LREAL | Double | 8 |
| STRING | LogixString | 88 |

### Working with Arrays

```csharp
// Read an array
var item = GetOrCreateItem("MyArray");
item.Elements = 10;  // Read 10 elements
await connection.ReadTagAsync<int[]>("MyIntArray");

// Write an array
await connection.WriteTagAsync("MyIntArray", new int[] { 1, 2, 3, 4, 5 });
```

### Route Path Format

The route path format is: `IP,Backplane,Slot`

| Component | Description | Example |
|-----------|-------------|---------|
| IP | PLC or ENET module IP address | 192.168.1.10 |
| Backplane | Port number (1 = backplane) | 1 |
| Slot | CPU slot number | 0 |

**Examples:**
- `192.168.1.10,1,0` - ControlLogix with CPU in slot 0
- `192.168.1.10,1,2` - ControlLogix with CPU in slot 2
- `192.168.1.20,1,0` - CompactLogix (slot is typically 0)

### Connection Lifecycle

```csharp
connection.StateChanged += (sender, e) =>
{
    Console.WriteLine($"PLC State: {e.PreviousState} → {e.CurrentState}");
    
    if (e.Exception is not null)
    {
        Console.WriteLine($"Error: {e.Exception.Message}");
    }
};

// States: Disconnected → Connecting → Connected → Disconnecting → Faulted
```

### Integration with Dependency Injection

```csharp
// Program.cs (ASP.NET Core / Generic Host)
builder.Services.AddConduit(conduit => conduit
    .AddAsCommConnection("plc1", "192.168.1.10", cpuSlot: 0, pollingIntervalMs: 100)
    .AddAsCommConnection("plc2", plc => plc
        .WithPlc("192.168.1.20", cpuSlot: 2)
        .WithDefaultPollingInterval(500)
        .WithAutoReconnect(true))
);

// Or with configuration file
builder.Services.AddConduit(conduit => conduit
    .AddAsCommConnection(plc => plc
        .WithOptions(builder.Configuration.GetSection("Plc1").Get<AsCommConnectionOptions>()!)
        .WithHandlersFromEntryAssembly())
);
```

**appsettings.json:**
```json
{
  "Plc1": {
    "ConnectionName": "plc1",
    "IpAddress": "192.168.1.10",
    "CpuSlot": 0,
    "Backplane": 1,
    "DefaultPollingIntervalMs": 100,
    "ConnectionTimeoutSeconds": 10,
    "AutoReconnect": true
  }
}
```

---

## � Auto-Injection - Zero Configuration Dependencies

Conduit automatically injects common dependencies into your handlers **without requiring manual DI registration**. This reduces boilerplate and makes your code cleaner.

### Automatically Injected Types

The following types are auto-injected by Conduit and don't need to be registered in your DI container:

| Type | Description |
|------|-------------|
| `IConduit` | The main Conduit instance |
| `IMqttConnection` | MQTT connection (if configured) |
| `IAsCommConnection` | PLC connection (if configured) |
| `IMessagePublisher` | Generic publisher from any configured connection |
| `IMqttPublisher` | MQTT-specific publisher |
| `IAsCommPublisher` | PLC-specific publisher |
| `ILogger<T>` | Logger (uses `NullLogger` if not registered) |

### Example: Handler with Auto-Injected Dependencies

```csharp
using Conduit.Mqtt;
using Conduit.Mqtt.Attributes;
using Microsoft.Extensions.Logging;

[MqttSubscribe("mqtt", "sensors/temperature")]
public class TemperatureHandler : IMessageSubscriptionHandler<TemperatureReading>
{
    private readonly ILogger<TemperatureHandler> _logger;  // Auto-injected (optional)
    private readonly IMqttConnection _mqtt;                 // Auto-injected
    private readonly IMyService _myService;                 // From your DI container
    
    public TemperatureHandler(
        ILogger<TemperatureHandler> logger,
        IMqttConnection mqtt,
        IMyService myService)
    {
        _logger = logger;      // ✅ NullLogger if not registered
        _mqtt = mqtt;          // ✅ Auto-injected by Conduit
        _myService = myService; // ✅ From your DI container
    }
    
    public async Task HandleAsync(
        TemperatureReading message,
        IMessageContext context,
        CancellationToken ct)
    {
        _logger.LogInformation("Temperature: {Value}°C", message.Value);
        
        // Publish to another topic using the injected connection
        await _mqtt.Publisher.PublishAsync("alerts/high-temp", 
            new { temp = message.Value }, 
            cancellationToken: ct);
    }
}
```

### Minimal Handler Configuration

With auto-injection, you can create handlers **without configuring any DI container**:

```csharp
using Conduit.Core;
using Conduit.Mqtt;

// No DI container needed!
var conduit = ConduitBuilder.Create()
    .AddMqttConnection(mqtt => mqtt
        .WithConnectionName("mqtt")
        .WithBroker("broker.hivemq.com", 1883)
        .WithHandlersFromEntryAssembly())
    .Build();  // ✅ No .WithActivator() needed

await conduit.ConnectAllAsync();
```

Handlers with only auto-injected dependencies (like `IConduit`, `IMqttConnection`, `ILogger<T>`) will work without any DI setup.

### Mixed Dependencies

If your handlers need custom services, configure a DI container:

```csharp
// With SimpleInjector
var container = new Container();
container.Register<IMyService, MyService>(Lifestyle.Singleton);

var conduit = ConduitBuilder.Create()
    .WithActivator(type => container.GetInstance(type))  // ← Only needed for custom services
    .AddMqttConnection(mqtt => mqtt
        .WithConnectionName("mqtt")
        .WithBroker("broker.hivemq.com", 1883)
        .WithHandlersFromEntryAssembly())
    .Build();
```

Conduit will:
1. ✅ Automatically inject `IConduit`, `IMqttConnection`, `ILogger<>`, etc.
2. ✅ Use your DI container for custom services (`IMyService`)
3. ✅ Use `NullLogger<T>` if `ILogger<T>` is not registered

---

## �📡 Conduit.Mqtt - MQTT Protocol

For MQTT messaging support, see the MQTT-specific examples below.

### Quick Start

```csharp
using Conduit.Mqtt;
using Conduit.Mqtt.Attributes;

[MqttSubscribe("mqtt", "sensors/+/temperature")]
public class TemperatureSensorHandler : IMessageSubscriptionHandler<SensorReading>
{
    public Task HandleAsync(SensorReading message, IMessageContext context, CancellationToken ct)
    {
        Console.WriteLine($"Temperature: {message.Value}°C");
        return Task.CompletedTask;
    }
}

// Configure
var connection = MqttClientBuilder.Create()
    .WithConnectionName("mqtt")
    .WithBroker("broker.hivemq.com", 1883)
    .WithHandlersFromEntryAssembly()
    .Build();

await connection.ConnectAsync();
```

---

## Error Handling

```csharp
try
{
    await connection.ConnectAsync();
}
catch (InvalidOperationException ex) when (ex.Message.Contains("license"))
{
    // ASComm IoT license issue
    Console.WriteLine("Please install a valid ASComm IoT license");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}

// Tag-level error handling
var result = await connection.ReadTagAsync<float>("MyTag");
if (result.Quality != TagQuality.Good)
{
    Console.WriteLine($"Read quality: {result.Quality}");
}
```

---

## 🎯 EventMediator - Internal Event System

Conduit includes a built-in event system for decoupling business logic and orchestrating workflows across handlers.

### Basic Event Usage

```csharp
using Conduit.Core.Events;
using Conduit.Core.Events.Attributes;

// 1. Define your event data
public record TemperatureChangedEvent(float Temperature);

// 2. Create an event handler
[Event("tempChanged")]
public class TemperatureChangedHandler : IEventHandler<TemperatureChangedEvent>
{
    private readonly IMqttConnection _mqtt;
    
    public TemperatureChangedHandler(IMqttConnection mqtt)
    {
        _mqtt = mqtt;  // Auto-injected
    }
    
    public async Task HandleAsync(
        TemperatureChangedEvent eventData,
        TagReadResults tags,
        CancellationToken ct = default)
    {
        Console.WriteLine($"Temperature: {eventData.Temperature}°C");
        
        // Publish to MQTT
        await _mqtt.Publisher.PublishAsync(
            "sensors/temperature", 
            new { temp = eventData.Temperature }, 
            cancellationToken: ct);
    }
}

// 3. Emit the event from anywhere in your code
using Conduit.Core.Events;

await EventMediator.Global.EmitAsync("tempChanged", new TemperatureChangedEvent(25.5f));
```

### Event Handler Priority

Control the execution order of handlers with the `Priority` parameter:

```csharp
[Event("orderProcessed", Priority = 100)]  // Runs first (higher priority)
public class ValidateOrderHandler : IEventHandler<OrderEvent>
{
    // ...
}

[Event("orderProcessed", Priority = 50)]   // Runs second
public class ProcessPaymentHandler : IEventHandler<OrderEvent>
{
    // ...
}

[Event("orderProcessed", Priority = 10)]   // Runs last (lower priority)
public class SendEmailHandler : IEventHandler<OrderEvent>
{
    // ...
}
```

### Auto-Reading PLC Tags

Event handlers can automatically read PLC tags before execution:

```csharp
[Event("getMachineData")]
public class MachineDataHandler : IEventHandler<object>
{
    [ReadTag("plc1", "Sensor_Temperature")]
    public float Temperature { get; set; }
    
    [ReadTag("plc1", "Motor_Speed")]
    public int Speed { get; set; }
    
    public Task HandleAsync(object eventData, TagReadResults tags, CancellationToken ct)
    {
        // Properties are auto-populated before this runs
        Console.WriteLine($"Temp: {Temperature}°C, Speed: {Speed} RPM");
        return Task.CompletedTask;
    }
}

// Emit - tags are read automatically
await EventMediator.Global.EmitAsync("getMachineData");
```

### Multiple Handlers for Same Event

Multiple handlers can listen to the same event:

```csharp
[Event("alarm")]
public class LogAlarmHandler : IEventHandler<AlarmEvent>
{
    public Task HandleAsync(AlarmEvent data, TagReadResults tags, CancellationToken ct)
    {
        // Log to database
        return Task.CompletedTask;
    }
}

[Event("alarm")]
public class NotifyOperatorHandler : IEventHandler<AlarmEvent>
{
    public Task HandleAsync(AlarmEvent data, TagReadResults tags, CancellationToken ct)
    {
        // Send email/SMS
        return Task.CompletedTask;
    }
}

// Both handlers execute when event is emitted
await EventMediator.Global.EmitAsync("alarm", new AlarmEvent("High temperature"));
```

### Required Usings

```csharp
using Conduit.Core.Events;              // For EventMediator
using Conduit.Core.Events.Attributes;   // For [Event], [ReadTag]
```

---

## 🚫 Disabling Handlers

You can temporarily disable handlers without removing them from your codebase using the `[DisableHandler]` attribute:

```csharp
using Conduit.Core.Attributes;
using Conduit.Mqtt.Attributes;

[DisableHandler]  // 🚫 This handler will NOT be discovered or registered
[MqttSubscribe("mqtt", "test/topic")]
public class DebugHandler : IMessageSubscriptionHandler<MyMessage>
{
    public Task HandleAsync(MyMessage message, IMessageContext context, CancellationToken ct)
    {
        // This handler is completely ignored during discovery
        return Task.CompletedTask;
    }
}
```

**Use Cases:**
- Temporarily disable handlers during development
- Conditionally exclude handlers without deleting code
- Keep experimental handlers in the codebase without activating them

Simply comment out or remove the `[DisableHandler]` attribute to re-enable the handler.

---

## Requirements

- .NET 8.0 or later
- For ASComm: Valid ASComm IoT license from [Automated Solutions](https://automatedsolutions.com/products/iot/ascommiot/)
- For MQTT: MQTTnet 4.x

## License

MIT License - See LICENSE file for details.

## Acknowledgments

- ASComm IoT by [Automated Solutions](https://automatedsolutions.com/)
- MQTTnet for MQTT protocol support
