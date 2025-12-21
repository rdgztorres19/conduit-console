using Conduit.EdgePlcDriver;
using Conduit.EdgePlcDriver.Messages;
using Conduit.Core.Abstractions;
using Conduit.Core.Enums;
using System.Collections.Generic;

namespace ConduitPlcDemo.Services;

/// <summary>
/// Null/Mock implementation of IEdgePlcDriver for when ASComm license is not available.
/// All operations will be handled via MQTT instead.
/// </summary>
public class NullEdgePlcDriver : IEdgePlcDriver
{
    private readonly NullEdgePlcDriverPublisher _publisher = new();

    public string ConnectionName => "null-plc";
    public string ConnectionId => Guid.NewGuid().ToString();
    public bool IsConnected => false;
    public ConnectionState State => ConnectionState.Disconnected;
    public string IpAddress => string.Empty;
    public string RoutePath => string.Empty;
    public IMessagePublisher Publisher => _publisher;
    IEdgePlcDriverPublisher IEdgePlcDriver.Publisher => _publisher;

    public event EventHandler<ConnectionStateChangedEventArgs>? StateChanged;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TagValue<T>> ReadTagAsync<T>(string tagName, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public Task<TagValue<object>> ReadTagRawAsync(string tagName, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public Task<IReadOnlyDictionary<string, object?>> ReadTagsAsync(
        IEnumerable<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public Task<IReadOnlyDictionary<string, T>> ReadTagsAsync<T>(
        IEnumerable<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public Task WriteTagAsync<T>(string tagName, T value, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public Task WriteTagsAsync(
        IReadOnlyDictionary<string, object> tagValues,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public Task<IAsyncDisposable> SubscribeAsync<T>(
        string tagName,
        Func<TagValue<T>, IEdgePlcDriverMessageContext, CancellationToken, Task> handler,
        int pollingIntervalMs = 100,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private class NullEdgePlcDriverPublisher : IEdgePlcDriverPublisher
    {
        public Task PublishAsync<TMessage>(
            string topic,
            TMessage message,
            QualityOfService qos = QualityOfService.AtLeastOnce,
            bool retain = false,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
        }

        public Task PublishAsync(
            string topic,
            ReadOnlyMemory<byte> payload,
            QualityOfService qos = QualityOfService.AtLeastOnce,
            bool retain = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
        }

        public Task WriteTagAsync<T>(string tagName, T value, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
        }

        public Task WriteTagsAsync(
            IReadOnlyDictionary<string, object> tagValues,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("PLC operations are not available. Use MQTT endpoints instead.");
        }
    }
}

