using CommunityToolkit.Mvvm.ComponentModel;

namespace LinkSentry.Models;

/// <summary>
/// Represents a single active TCP/UDP connection or listening port.
/// </summary>
public partial class PortConnectionInfo : ObservableObject
{
    /// <summary>Protocol: TCP or UDP</summary>
    public string Protocol { get; init; } = string.Empty;

    /// <summary>Local endpoint address string (IP:Port)</summary>
    public string LocalAddress { get; init; } = string.Empty;

    /// <summary>Local port number (for sorting)</summary>
    public int LocalPort { get; init; }

    /// <summary>Remote endpoint address string (IP:Port), empty for UDP listeners</summary>
    public string RemoteAddress { get; init; } = string.Empty;

    /// <summary>Connection state (Listen, Established, TimeWait, CloseWait, etc.)</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Process ID owning this connection</summary>
    public int ProcessId { get; init; }

    /// <summary>Process name, or "System Process" / "Access Denied" on failure</summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>Full path to the process executable, empty if inaccessible</summary>
    public string ProcessPath { get; init; } = string.Empty;

    /// <summary>Color based on state for visual distinction</summary>
    public string StateColor => State switch
    {
        "Listen" => "DodgerBlue",
        "Established" => "LimeGreen",
        "TimeWait" or "Time_Wait" => "Orange",
        "CloseWait" or "Close_Wait" => "Red",
        _ => "Gray"
    };
}
