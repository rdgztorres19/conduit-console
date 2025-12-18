using Conduit.Core.Attributes;
using Conduit.Core.Enums;

namespace Conduit.AsComm.Attributes;

/// <summary>
/// Subscription mode for PLC tag monitoring.
/// </summary>
public enum TagSubscriptionMode
{
    /// <summary>
    /// Polling mode: Tag value is read at regular intervals (default).
    /// More CPU efficient for the PLC, suitable for most scenarios.
    /// </summary>
    Polling = 0,

    /// <summary>
    /// Unsolicited mode: PLC pushes value changes immediately.
    /// Lower latency but higher PLC overhead. Requires PLC support.
    /// </summary>
    Unsolicited = 1
}

/// <summary>
/// Marks a message handler class to subscribe to PLC tag changes via ASComm.
/// Multiple attributes can be applied to subscribe to multiple tags.
/// </summary>
/// <remarks>
/// Tag Name Examples:
/// - "MyTag" - Simple tag in controller scope
/// - "Program:MainProgram.MyTag" - Program-scoped tag
/// - "MyArray[0]" - Array element
/// - "MyUDT.Member" - UDT member access
/// </remarks>
/// <example>
/// <code>
/// [AsCommSubscribe("plc1", "Sensor_Temperature", pollingIntervalMs: 100)]
/// public class TemperatureHandler : IMessageHandler&lt;TagValue&lt;float&gt;&gt;
/// {
///     public async Task HandleAsync(TagValue&lt;float&gt; message, IMessageContext context, CancellationToken ct)
///     {
///         Console.WriteLine($"Temperature: {message.Value}");
///         
///         // Write back to PLC if needed
///         await context.Publisher.PublishAsync("Setpoint_Temperature", 75.5f, ct);
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AsCommSubscribeAttribute : SubscribeAttribute
{
    /// <summary>
    /// Gets or sets the polling interval in milliseconds for this specific tag.
    /// If not set, uses the connection's default polling interval.
    /// </summary>
    public int PollingIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the ASComm data type for the tag.
    /// If not specified, the type is inferred from the message type.
    /// </summary>
    public AsCommDataType DataType { get; set; } = AsCommDataType.Auto;

    /// <summary>
    /// Gets or sets whether to trigger handler only on value change (true) 
    /// or on every poll cycle (false).
    /// </summary>
    public bool OnChangeOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets the deadband for analog values.
    /// Handler is only triggered if value changes by more than this amount.
    /// Only applicable when OnChangeOnly is true.
    /// </summary>
    public double Deadband { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the subscription mode (Polling or Unsolicited).
    /// Polling (default): Tag is read at regular intervals.
    /// Unsolicited: PLC pushes value changes immediately (lower latency, higher PLC overhead).
    /// </summary>
    public TagSubscriptionMode Mode { get; set; } = TagSubscriptionMode.Polling;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsCommSubscribeAttribute"/> class.
    /// </summary>
    /// <param name="connectionName">
    /// The name of the ASComm connection to use. Must match a configured connection name.
    /// </param>
    /// <param name="tagName">
    /// The PLC tag name to subscribe to.
    /// </param>
    /// <param name="pollingIntervalMs">
    /// Optional polling interval in milliseconds. Uses connection default if not specified.
    /// </param>
    /// <param name="mode">
    /// Subscription mode: Polling (default) or Unsolicited.
    /// </param>
    public AsCommSubscribeAttribute(
        string connectionName,
        string tagName,
        int pollingIntervalMs = 0,
        TagSubscriptionMode mode = TagSubscriptionMode.Polling)
        : base(connectionName, tagName, QualityOfService.AtLeastOnce)
    {
        PollingIntervalMs = pollingIntervalMs;
        Mode = mode;
    }
}

/// <summary>
/// ASComm data types for PLC tags.
/// </summary>
public enum AsCommDataType
{
    /// <summary>
    /// Automatically infer type from the handler's message type.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Boolean (BOOL) - 1 bit
    /// </summary>
    BOOL,

    /// <summary>
    /// Signed 8-bit integer (SINT)
    /// </summary>
    SINT,

    /// <summary>
    /// Signed 16-bit integer (INT)
    /// </summary>
    INT,

    /// <summary>
    /// Signed 32-bit integer (DINT)
    /// </summary>
    DINT,

    /// <summary>
    /// Signed 64-bit integer (LINT)
    /// </summary>
    LINT,

    /// <summary>
    /// Unsigned 8-bit integer (USINT)
    /// </summary>
    USINT,

    /// <summary>
    /// Unsigned 16-bit integer (UINT)
    /// </summary>
    UINT,

    /// <summary>
    /// Unsigned 32-bit integer (UDINT)
    /// </summary>
    UDINT,

    /// <summary>
    /// Unsigned 64-bit integer (ULINT)
    /// </summary>
    ULINT,

    /// <summary>
    /// 32-bit floating point (REAL)
    /// </summary>
    REAL,

    /// <summary>
    /// 64-bit floating point (LREAL)
    /// </summary>
    LREAL,

    /// <summary>
    /// String
    /// </summary>
    STRING,

    /// <summary>
    /// User-Defined Type (structure)
    /// </summary>
    UDT
}
