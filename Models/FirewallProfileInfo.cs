namespace LinkSentry.Models;

/// <summary>
/// Represents the firewall status for a single network profile (Domain, Private, Public).
/// </summary>
public class FirewallProfileInfo
{
    /// <summary>Profile name: Domain / Private / Public</summary>
    public string ProfileName { get; init; } = string.Empty;

    /// <summary>Localized display name (域网络 / 专用网络 / 公用网络)</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Whether the firewall is enabled for this profile</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Default action for inbound traffic (Allow / Block)</summary>
    public string InboundAction { get; init; } = string.Empty;

    /// <summary>Default action for outbound traffic (Allow / Block)</summary>
    public string OutboundAction { get; init; } = string.Empty;

    /// <summary>Color indicator based on enabled state</summary>
    public string StatusColor { get; set; } = "Gray";

    /// <summary>Status text</summary>
    public string StatusText { get; set; } = "未知";
}
