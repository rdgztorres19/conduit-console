using System.Text.Json.Serialization;

namespace ConduitPlcDemo.Messages.Ignition;

/// <summary>
/// Represents a batch of tag updates from Ignition SCADA.
/// The message is an array of IgnitionTagUpdate.
/// </summary>
public class IgnitionRealtimeMessage : List<IgnitionTagUpdate>
{
}

/// <summary>
/// Represents a single tag update from Ignition.
/// </summary>
public record IgnitionTagUpdate
{
    /// <summary>
    /// The full path of the tag (e.g., "MODELS_HERE/MODEL_C/RT_34/INPUTS/TORQUE").
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// The value container with quality and timestamp information.
    /// </summary>
    [JsonPropertyName("value")]
    public IgnitionTagValueContainer Value { get; init; } = new();
}

/// <summary>
/// Container for the tag value, quality, and timestamp.
/// </summary>
public record IgnitionTagValueContainer
{
    /// <summary>
    /// The actual value of the tag. Can be any type (bool, string, number, etc.).
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }

    /// <summary>
    /// Quality information for the tag value.
    /// </summary>
    [JsonPropertyName("quality")]
    public IgnitionQuality Quality { get; init; } = new();

    /// <summary>
    /// Timestamp when the value was recorded.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    /// <summary>
    /// Returns true if the quality code indicates a good value (192 = Good).
    /// </summary>
    public bool IsGoodQuality => Quality.Code == 192;
}

/// <summary>
/// Quality information for an Ignition tag value.
/// </summary>
public record IgnitionQuality
{
    /// <summary>
    /// Quality code. 192 = Good, negative values indicate errors.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; init; }

    /// <summary>
    /// Diagnostic message when quality is bad.
    /// </summary>
    [JsonPropertyName("diagnosticMessage")]
    public string? DiagnosticMessage { get; init; }

    /// <summary>
    /// Returns true if quality is good (code 192).
    /// </summary>
    public bool IsGood => Code == 192;
}
