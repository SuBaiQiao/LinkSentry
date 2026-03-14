using System;

namespace LinkSentry.Models;

/// <summary>
/// Represents a raw traffic measurement captured at a specific point in time.
/// </summary>
public record TrafficRecord
{
    /// <summary>Database primary key</summary>
    public long Id { get; init; }

    /// <summary>Unix epoch timestamp (seconds)</summary>
    public long Timestamp { get; init; }

    /// <summary>Name of the network interface</summary>
    public string InterfaceName { get; init; } = string.Empty;

    /// <summary>Upload speed in bytes per second</summary>
    public double SpeedSent { get; init; }

    /// <summary>Download speed in bytes per second</summary>
    public double SpeedReceived { get; init; }

    /// <summary>Total bytes sent since system start (optional context)</summary>
    public long? TotalSent { get; init; }

    /// <summary>Total bytes received since system start (optional context)</summary>
    public long? TotalReceived { get; init; }
}
