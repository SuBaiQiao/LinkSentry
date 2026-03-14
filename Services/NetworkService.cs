using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using LinkSentry.Models;
using Microsoft.Extensions.Logging;

namespace LinkSentry.Services;

public class NetworkService(ILogger<NetworkService> logger, ITrafficHistoryService trafficHistory) : INetworkService
{
    private readonly Dictionary<string, (long bytesSent, long bytesReceived, DateTime timestamp)> _previousStats = [];

    public Task<List<NetworkInterfaceModel>> GetAllInterfacesAsync()
    {
        return Task.Run(() =>
        {
            var result = new List<NetworkInterfaceModel>();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                try
                {
                    var ipProps = ni.GetIPProperties();
                    var ipv4 = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "N/A";
                    var ipv6 = ipProps.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)?.Address.ToString() ?? "N/A";

                    var model = new NetworkInterfaceModel
                    {
                        Id = ni.Id,
                        Name = ni.Name,
                        Description = ni.Description,
                        NetworkInterfaceType = ni.NetworkInterfaceType.ToString(),
                        MacAddress = string.Join(":", ni.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2"))),
                        Status = ni.OperationalStatus,
                        Speed = ni.Speed,
                        Ipv4Address = ipv4,
                        Ipv6Address = ipv6,
                        IsDhcpEnabled = ipProps.GetIPv4Properties()?.IsDhcpEnabled ?? false
                    };

                    foreach (var dns in ipProps.DnsAddresses)
                    {
                        model.DnsAddresses.Add(dns.ToString());
                    }

                    foreach (var gateway in ipProps.GatewayAddresses)
                    {
                        model.GatewayAddresses.Add(gateway.Address.ToString());
                    }

                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Loopback && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        var ipStats = ni.GetIPv4Statistics();
                        model.BytesSent = ipStats.BytesSent;
                        model.BytesReceived = ipStats.BytesReceived;

                        _previousStats[ni.Id] = (ipStats.BytesSent, ipStats.BytesReceived, DateTime.UtcNow);
                    }

                    result.Add(model);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to retrieve information for network interface {Name}", ni.Name);
                }
            }

            // Sort so that 'Up' interfaces are placed first, then sort by Name
            return result
                .OrderByDescending(ni => ni.Status == OperationalStatus.Up)
                .ThenBy(ni => ni.Name)
                .ToList();
        });
    }

    public Task UpdateTrafficStatisticsAsync(IEnumerable<NetworkInterfaceModel> models)
    {
        return Task.Run(() =>
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces().ToDictionary(ni => ni.Id);
            var now = DateTime.UtcNow;

            foreach (var model in models)
            {
                if (!interfaces.TryGetValue(model.Id, out var ni)) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                try
                {
                    var ipStats = ni.GetIPv4Statistics();
                    
                    if (_previousStats.TryGetValue(model.Id, out var prev))
                    {
                        var timeDiff = (now - prev.timestamp).TotalSeconds;
                        if (timeDiff > 0)
                        {
                            var sentSpeed = (long)((ipStats.BytesSent - prev.bytesSent) / timeDiff);
                            var recvSpeed = (long)((ipStats.BytesReceived - prev.bytesReceived) / timeDiff);

                            model.SendSpeedKbps = (sentSpeed * 8.0) / 1024.0;
                            model.ReceiveSpeedKbps = (recvSpeed * 8.0) / 1024.0;

                            // Record to SQLite
                            _ = trafficHistory.RecordAsync(ni.Name, sentSpeed, recvSpeed, ipStats.BytesSent, ipStats.BytesReceived);
                        }
                    }

                    model.BytesSent = ipStats.BytesSent;
                    model.BytesReceived = ipStats.BytesReceived;
                    model.Status = ni.OperationalStatus;

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        model.SendSpeedHistory.Add(model.SendSpeedKbps);
                        model.ReceiveSpeedHistory.Add(model.ReceiveSpeedKbps);
                        
                        if (model.SendSpeedHistory.Count > 30) model.SendSpeedHistory.RemoveAt(0);
                        if (model.ReceiveSpeedHistory.Count > 30) model.ReceiveSpeedHistory.RemoveAt(0);

                        var maxSend = model.SendSpeedHistory.Count > 0 ? model.SendSpeedHistory.Max() : 0;
                        var maxRecv = model.ReceiveSpeedHistory.Count > 0 ? model.ReceiveSpeedHistory.Max() : 0;
                        var maxValue = Math.Max(maxSend, maxRecv);
                        
                        var cleanMax = maxValue > 0 ? Math.Ceiling(maxValue / 50.0) * 50 : 100;
                        cleanMax = Math.Max(100, cleanMax);

                        model.YAxes[0].MaxLimit = cleanMax;
                        model.YAxes[0].MinStep = cleanMax;
                    });

                    _previousStats[model.Id] = (ipStats.BytesSent, ipStats.BytesReceived, now);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to update statistics for network interface {Name}", model.Name);
                }
            }
        });
    }

    public async Task<bool> EnableInterfaceAsync(string adapterName)
    {
        return await RunNetshCommandAsync($"interface set interface \"{adapterName}\" admin=enable");
    }

    public async Task<bool> DisableInterfaceAsync(string adapterName)
    {
        return await RunNetshCommandAsync($"interface set interface \"{adapterName}\" admin=disable");
    }

    public async Task RefreshDhcpAsync(string adapterName)
    {
        await RunCommandAsync("ipconfig", $"/renew \"{adapterName}\"");
    }

    public async Task FlushDnsAsync()
    {
        await RunCommandAsync("ipconfig", "/flushdns");
    }

    private async Task<bool> RunNetshCommandAsync(string arguments)
    {
        return await RunCommandAsync("netsh", arguments);
    }

    private async Task<bool> RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas", // Request admin privileges
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute command: {FileName} {Arguments}", fileName, arguments);
            return false;
        }
    }
}
