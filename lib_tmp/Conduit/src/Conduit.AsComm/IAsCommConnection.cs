using Conduit.AsComm.Messages;
using Conduit.Core.Abstractions;
using Conduit.Core.Enums;

namespace Conduit.AsComm;

/// <summary>
/// Represents an active connection to a PLC via ASComm.
/// </summary>
public interface IAsCommConnection : IServiceBusConnection
{
    /// <summary>
    /// Gets the ASComm-specific publisher for writing tags to the PLC.
    /// </summary>
    new IAsCommPublisher Publisher { get; }

    /// <summary>
    /// Gets the IP address of the connected PLC.
    /// </summary>
    string IpAddress { get; }

    /// <summary>
    /// Gets the route path to the PLC (IP,Backplane,Slot format).
    /// </summary>
    string RoutePath { get; }

    /// <summary>
    /// Reads a tag value from the PLC.
    /// </summary>
    /// <typeparam name="T">The expected type of the tag value.</typeparam>
    /// <param name="tagName">The name of the tag to read.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The tag value with metadata.</returns>
    Task<TagValue<T>> ReadTagAsync<T>(string tagName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads multiple tag values from the PLC in a single operation.
    /// </summary>
    /// <param name="tagNames">The names of the tags to read.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary of tag names to their values.</returns>
    Task<IReadOnlyDictionary<string, object?>> ReadTagsAsync(
        IEnumerable<string> tagNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a value to a PLC tag.
    /// </summary>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    /// <param name="tagName">The name of the tag to write.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteTagAsync<T>(string tagName, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple tag values to the PLC in a single operation.
    /// </summary>
    /// <param name="tagValues">Dictionary of tag names to values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteTagsAsync(
        IReadOnlyDictionary<string, object> tagValues,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dynamically subscribes to tag changes.
    /// </summary>
    /// <typeparam name="T">The expected type of the tag value.</typeparam>
    /// <param name="tagName">The tag name to subscribe to.</param>
    /// <param name="handler">The handler to invoke when the tag value changes.</param>
    /// <param name="pollingIntervalMs">Polling interval in milliseconds.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    Task<IAsyncDisposable> SubscribeAsync<T>(
        string tagName,
        Func<TagValue<T>, IAsCommMessageContext, CancellationToken, Task> handler,
        int pollingIntervalMs = 100,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// ASComm-specific publisher for writing tags to PLCs.
/// </summary>
public interface IAsCommPublisher : IMessagePublisher
{
    /// <summary>
    /// Writes a value to a PLC tag.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="tagName">The tag name (used as topic).</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteTagAsync<T>(string tagName, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple tags in a single operation.
    /// </summary>
    /// <param name="tagValues">Dictionary of tag names to values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteTagsAsync(
        IReadOnlyDictionary<string, object> tagValues,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended message context for ASComm with PLC-specific operations.
/// </summary>
public interface IAsCommMessageContext : IMessageContext
{
    /// <summary>
    /// Gets the tag name that triggered this message.
    /// </summary>
    string TagName { get; }

    /// <summary>
    /// Gets the ASComm-specific publisher for writing back to the PLC.
    /// </summary>
    new IAsCommPublisher Publisher { get; }

    /// <summary>
    /// Writes a value back to a PLC tag.
    /// Convenience method that uses the connection's publisher.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="tagName">The tag name to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task WriteTagAsync<T>(string tagName, T value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a value from a PLC tag.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="tagName">The tag name to read.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<TagValue<T>> ReadTagAsync<T>(string tagName, CancellationToken cancellationToken = default);
}
