using Conduit.AsComm.Messages;
using Conduit.Core.Abstractions;

namespace Conduit.AsComm.Internal;

/// <summary>
/// ASComm-specific message context implementation.
/// </summary>
internal sealed class AsCommMessageContext : IAsCommMessageContext
{
    private readonly IAsCommConnection _connection;

    public string Topic { get; }
    public string TagName { get; }
    public string? CorrelationId { get; }
    public DateTimeOffset ReceivedAt { get; }
    public ReadOnlyMemory<byte> RawPayload { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
    
    IMessagePublisher IMessageContext.Publisher => Publisher;
    public IAsCommPublisher Publisher { get; }

    public AsCommMessageContext(
        string tagName,
        ReadOnlyMemory<byte> rawPayload,
        IAsCommPublisher publisher,
        IAsCommConnection connection,
        string? correlationId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        TagName = tagName;
        Topic = tagName; // Tag name acts as the "topic" in PLC context
        RawPayload = rawPayload;
        Publisher = publisher;
        _connection = connection;
        CorrelationId = correlationId;
        ReceivedAt = DateTimeOffset.UtcNow;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    public Task WriteTagAsync<T>(string tagName, T value, CancellationToken cancellationToken = default)
    {
        return Publisher.WriteTagAsync(tagName, value, cancellationToken);
    }

    public Task<TagValue<T>> ReadTagAsync<T>(string tagName, CancellationToken cancellationToken = default)
    {
        return _connection.ReadTagAsync<T>(tagName, cancellationToken);
    }
}
