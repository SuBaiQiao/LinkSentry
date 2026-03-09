using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LinkSentry.Models;
using Microsoft.Extensions.Logging;

namespace LinkSentry.Services;

/// <summary>
/// Reads Windows Defender Firewall status using netsh advfirewall.
/// Uses structured parsing with key=value pairs to avoid locale-dependent issues.
/// Reading firewall status does NOT require administrator privileges.
/// </summary>
public class FirewallService : IFirewallService
{
    private readonly ILogger<FirewallService> _logger;
    private readonly IDiagnosticLogger _diag;

    public FirewallService(ILogger<FirewallService> logger, IDiagnosticLogger diag)
    {
        _logger = logger;
        _diag = diag;
    }

    public async Task<List<FirewallProfileInfo>> GetFirewallStatusAsync()
    {
        _diag.Log("Firewall: Starting full status retrieval.");
        var tasks = new List<Task<FirewallProfileInfo>>
        {
            GetProfileAsync("domain", "域网络"),
            GetProfileAsync("private", "专用网络"),
            GetProfileAsync("public", "公用网络")
        };

        try
        {
            var results = await Task.WhenAll(tasks);
            _diag.Log($"Firewall: Retrieved {results.Length} profiles.");
            return new List<FirewallProfileInfo>(results);
        }
        catch (Exception ex)
        {
            _diag.Log($"Firewall Exception: {ex.Message}");
            _logger.LogError(ex, "Failed to retrieve firewall status");
            
            // Return dummy data to ensure UI doesn't hang
            return new List<FirewallProfileInfo>
            {
                new() { DisplayName = "域网络", StatusText = "读取失败", StatusColor = "Gray", InboundAction = "未知", OutboundAction = "未知" },
                new() { DisplayName = "专用网络", StatusText = "读取失败", StatusColor = "Gray", InboundAction = "未知", OutboundAction = "未知" },
                new() { DisplayName = "公用网络", StatusText = "读取失败", StatusColor = "Gray", InboundAction = "未知", OutboundAction = "未知" }
            };
        }
    }

    private async Task<FirewallProfileInfo> GetProfileAsync(string profileName, string displayName)
    {
        try
        {
            _diag.Log($"Firewall[{profileName}]: Executing netsh command.");
            var output = await RunNetshAsync($"advfirewall show {profileName}profile");
            _diag.Log($"Firewall[{profileName}]: Netsh finished. Length={output.Length}");

            // Parse the output: look for State/状态, Inbound/入站, Outbound/出站 lines
            bool isEnabled = false;
            string inboundAction = "未知";
            string outboundAction = "未知";

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // State line: matches both English "State" and Chinese "状态"
                if (line.StartsWith("State", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("状态"))
                {
                    isEnabled = line.Contains("ON", StringComparison.OrdinalIgnoreCase) ||
                                line.Contains("启用", StringComparison.Ordinal);
                    _diag.Log($"Firewall[{profileName}]: State parsed as {(isEnabled ? "ON" : "OFF")}. Raw: {line}");
                }
                // Firewall Policy / Inbound
                else if (line.StartsWith("Firewall Policy", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("防火墙策略"))
                {
                    // Format: "Firewall Policy  BlockInbound,AllowOutbound"
                    // or Chinese equivalent
                    var valuePart = GetValuePart(line);
                    if (valuePart.Contains("BlockInbound", StringComparison.OrdinalIgnoreCase) ||
                        valuePart.Contains("阻止入站", StringComparison.Ordinal))
                    {
                        inboundAction = "阻止";
                    }
                    else
                    {
                        inboundAction = "允许";
                    }

                    if (valuePart.Contains("AllowOutbound", StringComparison.OrdinalIgnoreCase) ||
                        valuePart.Contains("允许出站", StringComparison.Ordinal))
                    {
                        outboundAction = "允许";
                    }
                    else
                    {
                        outboundAction = "阻止";
                    }
                }
            }

            return new FirewallProfileInfo
            {
                ProfileName = profileName,
                DisplayName = displayName,
                IsEnabled = isEnabled,
                InboundAction = inboundAction,
                OutboundAction = outboundAction,
                StatusText = isEnabled ? "已开启" : "已关闭",
                StatusColor = isEnabled ? "LimeGreen" : "Red"
            };
        }
        catch (Exception ex)
        {
            _diag.Log($"Firewall[{profileName}] Error: {ex.Message}");
            return new FirewallProfileInfo
            {
                ProfileName = profileName,
                DisplayName = displayName,
                IsEnabled = false,
                InboundAction = "读取失败",
                OutboundAction = "读取失败",
                StatusText = "获取失败",
                StatusColor = "Gray"
            };
        }
    }

    private static string GetValuePart(string line)
    {
        // Handles both "Key    Value" (spaces) and "Key: Value" formats
        var colonIdx = line.IndexOf(':');
        if (colonIdx >= 0 && colonIdx < line.Length - 1)
            return line[(colonIdx + 1)..].Trim();

        // Fallback: split on multiple spaces
        var parts = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^1].Trim() : line;
    }

    private async Task<string> RunNetshAsync(string arguments)
    {
        _diag.Log($"Netsh: Starting command 'netsh {arguments}'");
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // CRITICAL: Read both stdout and stderr in PARALLEL, then wait for exit.
            // Sequential reads can deadlock if one stream's buffer fills while we're waiting on the other.
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            var output = await outputTask;
            var error = await errorTask;

            // Wait up to 10s for exit after reading all output
            var exited = process.WaitForExit(10000);
            if (!exited)
            {
                _diag.Log($"Netsh: Timeout waiting for exit. Killing process.");
                try { process.Kill(); } catch { }
            }

            _diag.Log($"Netsh: Finished. ExitCode={(!exited ? "TIMEOUT" : process.ExitCode.ToString())}, OutputLen={output.Length}, ErrorLen={error.Length}");
            if (output.Length < 500)
                _diag.Log($"Netsh: Raw output: [{output.Replace("\r", "").Replace("\n", "\\n")}]");
            if (error.Length > 0)
                _diag.Log($"Netsh: Stderr: [{error.Replace("\r", "").Replace("\n", "\\n")}]");

            return output;
        }
        catch (Exception ex)
        {
            _diag.Log($"Netsh Fatal Error: {ex.GetType().Name} - {ex.Message}");
            return string.Empty;
        }
    }
}
