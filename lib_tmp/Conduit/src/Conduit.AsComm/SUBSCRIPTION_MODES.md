# Subscription Modes in Conduit.AsComm

This document explains the two subscription modes available in Conduit.AsComm for monitoring PLC tags.

## Overview

Conduit.AsComm provides two modes for subscribing to PLC tag changes:
1. **Polling Mode (Default)** - Standard polling at configurable intervals
2. **Unsolicited Mode** - Fast polling (10ms) for near real-time response

## Mode Comparison

### Polling Mode

**How it works:** Tags are read at regular intervals (typically 100ms-5000ms)

```csharp
// Default polling mode with 100ms interval
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

**When to use:**
- ✅ Temperature sensors and analog values
- ✅ Production counters
- ✅ Status indicators
- ✅ Any non-critical monitoring

**Advantages:**
- Lower PLC CPU usage
- Lower network traffic
- Predictable bandwidth consumption
- Works with all PLC models

**Disadvantages:**
- Response latency equals polling interval
- May miss very fast changes between polls

### Unsolicited Mode

**How it works:** Uses very fast polling (10ms fixed) for near real-time response

```csharp
// Unsolicited mode - 10ms fast polling
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

**When to use:**
- ✅ Safety signals (E-stops, interlocks)
- ✅ Alarms and fault conditions
- ✅ Critical I/O requiring fast response
- ✅ Fast-changing digital inputs

**Advantages:**
- Lower latency (10-20ms response time)
- Captures fast changes
- Works with all PLC models

**Disadvantages:**
- Higher PLC CPU usage
- Higher network traffic
- Should be used selectively

## Implementation Details

Both modes use ASComm's polling mechanism internally:
- **Polling Mode**: Creates items in a polling group with user-specified update rate
- **Unsolicited Mode**: Creates items in a separate polling group with 10ms update rate

The "unsolicited" name reflects the near real-time response behavior, though it's implemented via fast polling rather than true PLC push notifications.

## Performance Comparison

| Metric | Polling (100ms) | Polling (1000ms) | Unsolicited (10ms) |
|--------|----------------|------------------|-------------------|
| Response Latency | 100ms | 1000ms | 10-20ms |
| PLC CPU Impact | Low | Very Low | High |
| Network Bandwidth | Low | Very Low | High |
| Tags per Group | Unlimited | Unlimited | Use sparingly |
| Typical Use | General monitoring | Slow sensors | Safety-critical |

## Best Practices

1. **Start with Polling Mode**
   - Use polling mode for all tags by default
   - Only switch to unsolicited mode when you have specific latency requirements

2. **Choose Appropriate Intervals**
   - Temperature sensors: 1000-5000ms
   - Digital status: 100-500ms
   - Analog sensors: 100-1000ms
   - Critical I/O: Unsolicited mode (10ms)

3. **Limit Unsolicited Mode Usage**
   - Use for < 10% of total tags
   - Only for safety-critical or fast-changing tags
   - Monitor PLC CPU usage when using many unsolicited tags

4. **Hybrid Approach**
   - Mix both modes in the same application
   - Critical tags: Unsolicited mode
   - Non-critical tags: Polling mode
   - Example: E-stop (unsolicited) + temperature sensors (polling)

## Example: Hybrid Application

```csharp
// Safety-critical: 10ms response
[AsCommSubscribe("plc1", "Emergency_Stop", mode: TagSubscriptionMode.Unsolicited)]
public class EmergencyStopHandler : IMessageSubscriptionHandler<TagValue<bool>>
{
    // ... immediate response handling
}

// High priority: 100ms response
[AsCommSubscribe("plc1", "Motor_Fault", pollingIntervalMs: 100)]
public class MotorFaultHandler : IMessageSubscriptionHandler<TagValue<bool>>
{
    // ... fault detection and response
}

// Normal priority: 1000ms response
[AsCommSubscribe("plc1", "Process_Temperature", pollingIntervalMs: 1000)]
public class TemperatureHandler : IMessageSubscriptionHandler<TagValue<float>>
{
    // ... temperature monitoring
}

// Low priority: 5000ms response
[AsCommSubscribe("plc1", "Ambient_Humidity", pollingIntervalMs: 5000)]
public class HumidityHandler : IMessageSubscriptionHandler<TagValue<float>>
{
    // ... environmental monitoring
}
```

## Configuration

No additional configuration is needed. Simply specify the mode in the attribute:

```csharp
// Polling mode (explicit)
[AsCommSubscribe("plc1", "TagName", pollingIntervalMs: 100, mode: TagSubscriptionMode.Polling)]

// Unsolicited mode
[AsCommSubscribe("plc1", "TagName", mode: TagSubscriptionMode.Unsolicited)]

// Polling mode (implicit default)
[AsCommSubscribe("plc1", "TagName", pollingIntervalMs: 100)]
```

## See Also

- [Conduit README](../README.md) - Full documentation
- [AsComm IoT Documentation](https://automatedsolutions.com/products/iot/ascommiot/)
- [Example Handlers](../../ConsoleWithAutofac/Handlers/AsComm/CriticalTagHandler.cs)
