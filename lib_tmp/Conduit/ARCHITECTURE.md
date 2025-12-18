# Nexus.ServiceBus - Architecture & Design Document

This document explains the architecture, design patterns, and rationale behind each component of the Nexus.ServiceBus library.

## Table of Contents

1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Design Patterns](#design-patterns)
4. [Core Abstractions](#core-abstractions)
5. [Dependency Injection](#dependency-injection)
6. [MQTT Implementation](#mqtt-implementation)
7. [Message Flow](#message-flow)
8. [Extending the Library](#extending-the-library)

---

## Overview

Nexus.ServiceBus is designed with these principles:

- **Separation of Concerns** - Core abstractions are protocol-agnostic
- **Extensibility** - New protocols can be added without modifying core code
- **DI Agnostic** - Works with any DI container (Microsoft DI, Autofac, SimpleInjector, Ninject, etc.)
- **Scoped Services** - Full support for Singleton, Scoped, and Transient lifetimes
- **Testability** - Interfaces everywhere enable mocking
- **Simplicity** - Fluent APIs hide complexity from consumers
- **Modern .NET** - Leverages C# 12, nullable references, and async patterns

---

## Project Structure

```
Nexus.ServiceBus/
├── src/
│   ├── Nexus.ServiceBus.Core/           # Protocol-agnostic abstractions
│   │   ├── Abstractions/                # Core interfaces
│   │   │   ├── IMessageHandler.cs       # Handler contract
│   │   │   ├── IMessageContext.cs       # Message metadata
│   │   │   ├── IMessagePublisher.cs     # Publishing contract
│   │   │   ├── IHandlerActivator.cs     # DI activation contract
│   │   │   ├── IHandlerResolver.cs      # Handler resolution
│   │   │   ├── IScopedHandler.cs        # Scoped handler wrapper
│   │   │   ├── IServiceBusConnection.cs # Connection contract
│   │   │   └── INexus.cs                # Multi-connection container
│   │   ├── Activators/                  # DI implementations
│   │   │   ├── ServiceProviderActivator.cs  # IServiceProvider-based
│   │   │   ├── FuncActivator.cs             # Lambda-based
│   │   │   └── HandlerActivatorAdapter.cs   # Adapter pattern
│   │   ├── Attributes/                  # Base subscription attribute
│   │   ├── Discovery/                   # Handler discovery via reflection
│   │   ├── Enums/                       # QoS, ConnectionState
│   │   ├── Internal/                    # Internal implementations
│   │   ├── Serialization/               # JSON serializer
│   │   ├── NexusBuilder.cs              # Main fluent builder
│   │   └── Nexus.cs                     # Multi-connection container
│   │
│   ├── Nexus.ServiceBus.Mqtt/           # MQTT protocol implementation
│   │   ├── Attributes/                  # MqttSubscribeAttribute
│   │   ├── Configuration/               # MqttConnectionOptions
│   │   ├── Internal/                    # TopicMatcher, helpers
│   │   ├── NexusBuilderMqttExtensions.cs # .AddMqttConnection()
│   │   ├── MqttClientBuilder.cs         # MQTT-specific builder
│   │   ├── MqttConnection.cs            # Connection implementation
│   │   └── MqttPublisher.cs             # Publishing logic
│   │
│   └── Nexus.ServiceBus.DependencyInjection/  # DI integration
│       ├── ServiceCollectionExtensions.cs      # .AddNexusServiceBus()
│       └── NexusHostedService.cs               # IHostedService
│
├── README.md                            # User documentation
└── ARCHITECTURE.md                      # This file
```

---

## Design Patterns

### 1. Nested Builder Pattern

**Location:** `NexusBuilder`, `IMqttClientBuilder`, `MqttClientBuilder`

**Purpose:** Provides a fluent, composable API for configuring the entire service bus.

```csharp
var nexus = NexusBuilder.Create()
    .WithServiceProvider(serviceProvider)    // Configure DI
    .AddMqttConnection(mqtt => mqtt          // Add MQTT (nested builder)
        .WithBroker("localhost", 1883)
        .WithCredentials("user", "pass")
        .WithHandlersFromEntryAssembly())
    .AddMqttConnection("secondary", mqtt => mqtt  // Add another connection
        .WithBroker("backup.server.com"))
    .Build();                                // Build all connections
```

**Architecture:**

```
┌─────────────────────────────────────────────────────────────┐
│                      NexusBuilder                            │
│  - Main entry point                                         │
│  - Configures DI (IHandlerActivator)                        │
│  - Collects connection factories                            │
├─────────────────────────────────────────────────────────────┤
│ + Create() → NexusBuilder                                   │
│ + WithServiceProvider(sp) → NexusBuilder                    │
│ + WithActivator(func) → NexusBuilder                        │
│ + AddConnection<T>(factory) → NexusBuilder                  │
│ + Build() → INexus                                          │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Extension Methods
                           ▼
┌─────────────────────────────────────────────────────────────┐
│            NexusBuilderMqttExtensions                        │
│  + AddMqttConnection(configure) → NexusBuilder              │
│  + AddMqttConnection(name, configure) → NexusBuilder        │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Creates
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   MqttClientBuilder                          │
│  - Protocol-specific configuration                          │
├─────────────────────────────────────────────────────────────┤
│ + WithBroker(host, port) → IMqttClientBuilder               │
│ + WithCredentials(user, pass) → IMqttClientBuilder          │
│ + WithTls(enabled) → IMqttClientBuilder                     │
│ + WithHandlerActivator(activator) → IMqttClientBuilder      │
│ + WithHandlersFromAssemblies(...) → IMqttClientBuilder      │
│ + Build() → IMqttConnection                                 │
└─────────────────────────────────────────────────────────────┘
```

**Benefits:**
- Single entry point (`NexusBuilder.Create()`)
- DI configured once, shared across all connections
- Protocol-specific options in nested builders
- Supports multiple connections of same or different protocols
- IntelliSense guides developers

---

### 2. Activator Pattern (DI Abstraction)

**Location:** `IHandlerActivator`, `ServiceProviderActivator`, `FuncActivator`

**Purpose:** Abstracts handler creation to work with ANY dependency injection container.

```
┌─────────────────────────────────────────────────────────────┐
│                    IHandlerActivator                         │
│  Defines how to create handler instances                     │
├─────────────────────────────────────────────────────────────┤
│ + CreateInstance(Type) → object                             │
│ + CreateScopedInstance(Type) → IScopedHandler               │
└─────────────────────────────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
┌─────────────────┐ ┌─────────────┐ ┌──────────────────┐
│ServiceProvider  │ │FuncActivator│ │Your Custom       │
│   Activator     │ │             │ │  Activator       │
├─────────────────┤ ├─────────────┤ ├──────────────────┤
│Uses IService-   │ │Uses a Func  │ │Implements        │
│Provider +       │ │<Type,object>│ │IHandlerActivator │
│IServiceScope-   │ │lambda       │ │for your container│
│Factory          │ │             │ │                  │
└─────────────────┘ └─────────────┘ └──────────────────┘
```

**Implementations:**

| Activator | Use Case | Scoped Support |
|-----------|----------|----------------|
| `ServiceProviderActivator` | Microsoft DI, Autofac (via IServiceProvider) | ✅ Full |
| `FuncActivator` | Any container via lambda | ⚠️ Container-dependent |
| Custom | Your own implementation | You decide |

**Usage Examples:**

```csharp
// Microsoft.Extensions.DependencyInjection
NexusBuilder.Create()
    .WithServiceProvider(serviceProvider)
    ...

// Autofac (via IServiceProvider)
var sp = new AutofacServiceProvider(container);
NexusBuilder.Create()
    .WithServiceProvider(sp)
    ...

// Autofac (via lambda)
NexusBuilder.Create()
    .WithActivator(type => container.Resolve(type))
    ...

// SimpleInjector
NexusBuilder.Create()
    .WithActivator(type => container.GetInstance(type))
    ...

// Ninject
NexusBuilder.Create()
    .WithActivator(type => kernel.Get(type))
    ...
```

---

### 3. Scoped Handler Pattern

**Location:** `IScopedHandler`, `ServiceProviderActivator.CreateScopedInstance`

**Purpose:** Enables proper lifetime management for scoped services in handlers.

```
┌──────────────────────────────────────────────────────────────┐
│                    IScopedHandler                             │
│  Wraps a handler with its DI scope                           │
├──────────────────────────────────────────────────────────────┤
│ + Handler: object       # The handler instance               │
│ + Dispose()             # Disposes the scope                 │
└──────────────────────────────────────────────────────────────┘
```

**Lifecycle:**

```
📨 Message arrives
       │
       ▼
┌──────────────────────────────────────┐
│ using var scoped = resolver          │ ← Create scope
│     .ResolveScoped(handlerType);     │
│                                      │
│ var handler = scoped.Handler;        │ ← Get handler
│                                      │
│ await handler.HandleAsync(...);      │ ← Execute
│                                      │
└──────────────────────────────────────┘
       │
       ▼ Dispose() called automatically
       │
       ▼ Scoped services cleaned up
```

**Supported Lifetimes:**

| Lifetime | Behavior per Message |
|----------|---------------------|
| `Singleton` | Same instance always |
| `Scoped` | New instance per message, disposed after |
| `Transient` | New instance every time |

---

### 4. Attribute-Based Discovery Pattern

**Location:** `SubscribeAttribute`, `MqttSubscribeAttribute`, `HandlerDiscoveryService`

**Purpose:** Declarative handler registration using .NET attributes.

```csharp
[MqttSubscribe("mqtt", "sensors/+/temperature", QualityOfService.AtLeastOnce)]
[MqttSubscribe("mqtt", "sensors/+/humidity", QualityOfService.AtLeastOnce)]
public class SensorHandler : IMessageHandler<SensorData>
{
    private readonly ILogger<SensorHandler> _logger;
    private readonly ISensorService _sensorService;  // Scoped service ✅

    public SensorHandler(ILogger<SensorHandler> logger, ISensorService sensorService)
    {
        _logger = logger;
        _sensorService = sensorService;
    }

    public async Task HandleAsync(SensorData message, IMessageContext context, CancellationToken ct)
    {
        await _sensorService.ProcessAsync(message);
        _logger.LogInformation("Processed sensor {Id}", message.SensorId);
    }
}
```

**Discovery Flow:**

```
1. Assembly Scan
   ├── Find types with [MqttSubscribe]
   ├── Validate implements IMessageHandler<T>
   └── Extract: Topic, QoS, ConnectionName, MessageType

2. HandlerRegistration Created
   {
       HandlerType: typeof(SensorHandler),
       MessageType: typeof(SensorData),
       Topic: "sensors/+/temperature",
       QualityOfService: AtLeastOnce,
       ConnectionName: "mqtt"
   }

3. On Connect
   ├── Filter registrations by ConnectionName
   ├── Subscribe to each unique topic
   └── Store for message dispatch
```

---

### 5. Adapter Pattern

**Location:** `HandlerActivatorAdapter`

**Purpose:** Bridges `IHandlerActivator` to `IHandlerResolver` for backwards compatibility.

```
┌─────────────────────────────────────────────────────────────┐
│                  HandlerActivatorAdapter                     │
│  Adapts IHandlerActivator → IHandlerResolver                │
├─────────────────────────────────────────────────────────────┤
│ - _activator: IHandlerActivator                             │
├─────────────────────────────────────────────────────────────┤
│ + Resolve(Type) → _activator.CreateInstance(Type)           │
│ + ResolveScoped(Type) → _activator.CreateScopedInstance()   │
└─────────────────────────────────────────────────────────────┘
```

---

## Core Abstractions

### INexus

Container for multiple connections:

```csharp
public interface INexus : IAsyncDisposable
{
    IReadOnlyList<object> Connections { get; }      // All connections
    IHandlerActivator Activator { get; }            // DI activator
    
    TConnection GetConnection<TConnection>();       // Get by type
    Task ConnectAllAsync(CancellationToken ct);     // Start all
    Task DisconnectAllAsync(CancellationToken ct);  // Stop all
}
```

**Purpose:** Manages multiple protocol connections with shared DI configuration.

---

### IHandlerActivator

The core DI abstraction:

```csharp
public interface IHandlerActivator
{
    /// <summary>
    /// Creates a handler instance (no scope management).
    /// Use for Singleton handlers only.
    /// </summary>
    object CreateInstance(Type handlerType);

    /// <summary>
    /// Creates a scoped handler instance.
    /// The returned IScopedHandler MUST be disposed after use.
    /// Supports Singleton, Scoped, and Transient lifetimes.
    /// </summary>
    IScopedHandler CreateScopedInstance(Type handlerType);
}

public interface IScopedHandler : IDisposable
{
    object Handler { get; }  // The handler instance
}
```

**Why this design:**
- `CreateInstance` - Simple, for singleton/transient without scope needs
- `CreateScopedInstance` - Creates proper scope for scoped services
- `IScopedHandler` - Ensures scope disposal after handler execution

---

### IMessageHandler<TMessage>

The fundamental interface for processing messages:

```csharp
public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(
        TMessage message,           // Deserialized payload
        IMessageContext context,    // Metadata + publisher
        CancellationToken ct);
}
```

**Constructor Injection:**
```csharp
public class MyHandler : IMessageHandler<MyMessage>
{
    // All of these work correctly:
    private readonly ILogger<MyHandler> _logger;           // Singleton
    private readonly IDbContext _dbContext;                // Scoped ✅
    private readonly IValidator _validator;                // Transient

    public MyHandler(
        ILogger<MyHandler> logger,
        IDbContext dbContext,        // Works with scoped services!
        IValidator validator)
    {
        _logger = logger;
        _dbContext = dbContext;
        _validator = validator;
    }
}
```

---

### IMessageContext

Provides message metadata and publishing capability:

```csharp
public interface IMessageContext
{
    string Topic { get; }                              // Received topic
    string? CorrelationId { get; }                     // For request-response
    DateTimeOffset ReceivedAt { get; }                 // Timestamp
    ReadOnlyMemory<byte> RawPayload { get; }          // Original bytes
    IMessagePublisher Publisher { get; }               // Send responses
    IReadOnlyDictionary<string, string> Metadata { get; }  // User properties
}
```

---

### IMessagePublisher

Abstraction for sending messages:

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        QualityOfService qos = QualityOfService.AtLeastOnce,
        bool retain = false,
        CancellationToken ct = default) where TMessage : class;
}
```

---

## Dependency Injection

### ServiceCollectionExtensions

Entry point for Microsoft.Extensions.DependencyInjection:

```csharp
// Basic usage - uses built-in IServiceProvider
services.AddNexusServiceBus(nexus => nexus
    .AddMqttConnection(mqtt => mqtt
        .WithBroker("localhost")
        .WithHandlersFromEntryAssembly())
);

// With custom activator (any DI container)
services.AddNexusServiceBus(
    type => myContainer.Resolve(type),  // Your container
    nexus => nexus.AddMqttConnection(mqtt => mqtt.WithBroker("localhost"))
);
```

**What it registers:**

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `INexus` | Singleton | Connection container |
| `IMqttConnection` | Singleton | MQTT connection |
| `IMessagePublisher` | Singleton | Publishing abstraction |
| `IMqttPublisher` | Singleton | MQTT-specific publishing |
| `NexusHostedService` | Singleton | Auto-connect on startup |
| Handler types | Transient | Discovered handlers |

---

### NexusHostedService

Manages connection lifecycle with `IHostedService`:

```csharp
internal sealed class NexusHostedService : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Nexus Service Bus connections...");
        await _nexus.ConnectAllAsync(ct);
        _logger.LogInformation("Nexus Service Bus started with {Count} connection(s)", 
            _nexus.Connections.Count);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Stopping Nexus Service Bus connections...");
        await _nexus.DisconnectAllAsync(ct);
        await _nexus.DisposeAsync();
    }
}
```

---

## MQTT Implementation

### MqttConnection

Core connection management with scoped handler support:

```
┌─────────────────────────────────────────────────────────────┐
│                      MqttConnection                          │
├─────────────────────────────────────────────────────────────┤
│ - _client: IMqttClient (MQTTnet)                            │
│ - _handlerRegistrations: IReadOnlyList<HandlerRegistration> │
│ - _handlerResolver: IHandlerResolver                        │
│ - _serializer: IMessageSerializer                           │
│ - _dynamicHandlers: Dictionary<string, List<Handler>>       │
├─────────────────────────────────────────────────────────────┤
│ + ConnectAsync() → Task                                     │
│ + DisconnectAsync() → Task                                  │
│ + SubscribeAsync(topic, handler) → IAsyncDisposable         │
│ - DispatchToHandlersAsync(topic, payload, context)          │
│ - OnMessageReceivedAsync(args)                              │
│ - HandleReconnectAsync()                                    │
└─────────────────────────────────────────────────────────────┘
```

**Key method - Scoped dispatch:**

```csharp
private async Task DispatchToHandlersAsync(
    string topic,
    ReadOnlyMemory<byte> payload,
    IMessageContext context)
{
    foreach (var registration in _handlerRegistrations)
    {
        if (!TopicMatcher.Matches(registration.Topic, topic))
            continue;

        try
        {
            // Create scope for this handler invocation
            using var scopedHandler = _handlerResolver.ResolveScoped(registration.HandlerType);
            var handler = scopedHandler.Handler;
            var message = _serializer.Deserialize(payload, registration.MessageType);

            var method = registration.HandlerType.GetMethod("HandleAsync");
            if (method is not null)
            {
                var task = (Task?)method.Invoke(handler, [message, context, _disposeCts.Token]);
                if (task is not null)
                {
                    await task.ConfigureAwait(false);
                }
            }
            // Scope disposed here - scoped services cleaned up
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching to handler {Handler}", 
                registration.HandlerType.Name);
        }
    }
}
```

---

## Message Flow

### Incoming Message Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                            INCOMING MESSAGE FLOW                          │
└──────────────────────────────────────────────────────────────────────────┘

   MQTT Broker                MqttConnection                 Handler
       │                           │                            │
       │ ─── PUBLISH ──────────►   │                            │
       │     topic: sensors/temp   │                            │
       │     payload: {...}        │                            │
       │                           │                            │
       │                    OnMessageReceivedAsync()            │
       │                           │                            │
       │                    Create IMessageContext              │
       │                           │                            │
       │                    For each HandlerRegistration:       │
       │                           │                            │
       │                    ┌──────┴──────┐                     │
       │                    │ Topic Match?│                     │
       │                    └──────┬──────┘                     │
       │                           │ YES                        │
       │                           ▼                            │
       │                    ┌─────────────────┐                 │
       │                    │ Create Scope    │  ← IServiceScope
       │                    └────────┬────────┘                 │
       │                             │                          │
       │                    ┌────────▼────────┐                 │
       │                    │ Resolve Handler │  ← With all dependencies
       │                    └────────┬────────┘                 │
       │                             │                          │
       │                    ┌────────▼────────┐                 │
       │                    │ Deserialize Msg │                 │
       │                    └────────┬────────┘                 │
       │                             │                          │
       │                             │ ─── HandleAsync() ───►   │
       │                             │                          │
       │                             │ ◄─── (may publish) ───   │
       │                             │                          │
       │                    ┌────────▼────────┐                 │
       │                    │ Dispose Scope   │  ← Cleanup scoped services
       │                    └─────────────────┘                 │
```

### Outgoing Message Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                            OUTGOING MESSAGE FLOW                          │
└──────────────────────────────────────────────────────────────────────────┘

   Application Code           MqttPublisher              MQTT Broker
       │                           │                         │
       │ ── PublishAsync() ───►    │                         │
       │    topic: "response"      │                         │
       │    message: {...}         │                         │
       │                           │                         │
       │                    Serialize to JSON/bytes          │
       │                           │                         │
       │                    Build MqttApplicationMessage     │
       │                           │                         │
       │                           │ ─── PUBLISH ──────────► │
       │                           │                         │
```

---

## Extending the Library

### Adding a New Protocol (e.g., AMQP)

1. **Create the project:**
   ```
   src/Nexus.ServiceBus.Amqp/
   ```

2. **Define the attribute:**
   ```csharp
   [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
   public sealed class AmqpSubscribeAttribute : SubscribeAttribute
   {
       public string Exchange { get; set; }
       public string RoutingKey { get; set; }
   }
   ```

3. **Create NexusBuilder extension:**
   ```csharp
   public static class NexusBuilderAmqpExtensions
   {
       public static NexusBuilder AddAmqpConnection(
           this NexusBuilder builder,
           Action<IAmqpClientBuilder> configure)
       {
           return builder.AddConnection((activator, serviceProvider) =>
           {
               var amqpBuilder = AmqpClientBuilder.Create();
               amqpBuilder.WithHandlerActivator(activator);
               
               // Configure logging if available
               if (serviceProvider is not null)
               {
                   var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                   if (loggerFactory is not null)
                       amqpBuilder.WithLoggerFactory(loggerFactory);
               }
               
               configure(amqpBuilder);
               return amqpBuilder.Build();
           });
       }
   }
   ```

4. **Usage:**
   ```csharp
   NexusBuilder.Create()
       .WithServiceProvider(sp)
       .AddMqttConnection(mqtt => mqtt.WithBroker("mqtt.server.com"))
       .AddAmqpConnection(amqp => amqp.WithHost("amqp.server.com"))
       .Build();
   ```

---

### Custom DI Container Integration

**Autofac with full scope support:**

```csharp
public class AutofacHandlerActivator : IHandlerActivator
{
    private readonly ILifetimeScope _rootScope;

    public AutofacHandlerActivator(ILifetimeScope rootScope)
    {
        _rootScope = rootScope;
    }

    public object CreateInstance(Type handlerType)
    {
        return _rootScope.Resolve(handlerType);
    }

    public IScopedHandler CreateScopedInstance(Type handlerType)
    {
        var scope = _rootScope.BeginLifetimeScope();
        try
        {
            var handler = scope.Resolve(handlerType);
            return new AutofacScopedHandler(handler, scope);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private sealed class AutofacScopedHandler : IScopedHandler
    {
        private readonly ILifetimeScope _scope;
        
        public object Handler { get; }

        public AutofacScopedHandler(object handler, ILifetimeScope scope)
        {
            Handler = handler;
            _scope = scope;
        }

        public void Dispose() => _scope.Dispose();
    }
}

// Usage
var container = containerBuilder.Build();
var nexus = NexusBuilder.Create()
    .WithActivator(new AutofacHandlerActivator(container))
    .AddMqttConnection(mqtt => mqtt.WithBroker("localhost"))
    .Build();
```

---

## Quality of Service Levels

| Level | Name | Description | Use Case |
|-------|------|-------------|----------|
| 0 | AtMostOnce | Fire and forget | Telemetry, logs |
| 1 | AtLeastOnce | Guaranteed delivery, may duplicate | Commands, alerts |
| 2 | ExactlyOnce | Guaranteed exactly once | Financial transactions |

---

## Thread Safety

- `MqttConnection` is thread-safe for publishing
- Each message creates its own scope (no shared state)
- State changes synchronized with `SemaphoreSlim`
- Dynamic handlers protected by lock

---

## Error Handling

- Connection errors trigger `StateChanged` event with `Faulted` state
- Handler exceptions are logged but don't crash the connection
- Auto-reconnect uses exponential backoff
- Graceful shutdown on `DisconnectAsync()`
- Scope disposed even if handler throws

---

## Testing Strategies

### Unit Testing Handlers

```csharp
[Fact]
public async Task Handler_Processes_Message_And_Responds()
{
    // Arrange
    var mockPublisher = new Mock<IMessagePublisher>();
    var mockContext = new Mock<IMessageContext>();
    mockContext.Setup(c => c.Publisher).Returns(mockPublisher.Object);
    mockContext.Setup(c => c.Topic).Returns("sensors/room1/temp");

    var handler = new TemperatureHandler(
        Mock.Of<ILogger<TemperatureHandler>>(),
        Mock.Of<ISensorService>());

    // Act
    await handler.HandleAsync(
        new TemperatureReading { Value = 25.5 },
        mockContext.Object,
        CancellationToken.None);

    // Assert
    mockPublisher.Verify(p => p.PublishAsync(
        It.Is<string>(t => t.StartsWith("sensors/")),
        It.IsAny<object>(),
        It.IsAny<QualityOfService>(),
        It.IsAny<bool>(),
        It.IsAny<CancellationToken>()));
}
```

### Integration Testing with Scoped Services

```csharp
[Fact]
public async Task Handler_Gets_Scoped_DbContext()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddDbContext<AppDbContext>(options => 
        options.UseInMemoryDatabase("test"));
    services.AddScoped<IRepository, Repository>();
    services.AddTransient<MyHandler>();

    var sp = services.BuildServiceProvider();
    
    var nexus = NexusBuilder.Create()
        .WithServiceProvider(sp)
        .AddMqttConnection(mqtt => mqtt
            .WithBroker("localhost")
            .WithHandlersFromAssemblies(typeof(MyHandler).Assembly))
        .Build();

    // Act
    await nexus.ConnectAllAsync();
    
    // Publish test message...
    
    // Assert - each handler invocation got its own DbContext scope
}
```

---

## Version History

| Version | Changes |
|---------|---------|
| 1.0.0 | Initial release with MQTT support |
| 1.1.0 | Added NexusBuilder pattern |
| 1.2.0 | Added IHandlerActivator for DI-agnostic design |
| 1.3.0 | Added IScopedHandler for proper scoped service support |

---

*Last updated: December 2024*
