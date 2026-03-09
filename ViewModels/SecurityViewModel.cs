using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkSentry.Models;
using LinkSentry.Services;

namespace LinkSentry.ViewModels;

public partial class SecurityViewModel : ObservableObject
{
    private readonly IFirewallService _firewallService;
    private readonly IPortService _portService;
    private readonly IDiagnosticLogger _diag;
    private List<PortConnectionInfo> _allConnections = new();
    private bool _isFirewallLoadingInternal;
    private bool _isPortLoadingInternal;

    // ============================
    //  Firewall
    // ============================

    public ObservableCollection<FirewallProfileInfo> FirewallProfiles { get; } = new();

    [ObservableProperty]
    private bool _isFirewallLoading;

    [ObservableProperty]
    private string _firewallOverallStatus = "检测中...";

    [ObservableProperty]
    private string _firewallOverallColor = "Gray";

    // ============================
    //  Ports
    // ============================

    public ObservableCollection<PortConnectionInfo> FilteredConnections { get; } = new();

    [ObservableProperty]
    private bool _isPortLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
    private int _totalConnectionCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectionSummary))]
    private int _filteredConnectionCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedProtocol = "全部";

    [ObservableProperty]
    private string _selectedState = "全部";

    public string[] ProtocolOptions { get; } = { "全部", "TCP", "UDP" };
    public string[] StateOptions { get; } = { "全部", "Listen", "Established", "TimeWait", "CloseWait", "Closed" };

    public string ConnectionSummary => $"共 {TotalConnectionCount} 条连接，当前显示 {FilteredConnectionCount} 条";

    [ObservableProperty]
    private PortConnectionInfo? _selectedConnection;

    [ObservableProperty]
    private string _logPath = "";

    public SecurityViewModel(IFirewallService firewallService, IPortService portService, IDiagnosticLogger diag)
    {
        _firewallService = firewallService;
        _portService = portService;
        _diag = diag;
        _diag.Log("SecurityViewModel: Constructor called.");

        LogPath = _diag.GetLogPath() ?? "N/A";
        _diag.Log($"SecurityViewModel: LogPath set to '{LogPath}'");

        // Schedule data loading — Post ensures it runs after the view is fully initialized
        // IMPORTANT: Post takes Action, so async lambda becomes async void.
        // Any unhandled exception in async void crashes the process!
        // Must wrap in try-catch.
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                _diag.Log("SecurityViewModel: Scheduled RefreshAllAsync starting now.");
                await RefreshAllAsync();
            }
            catch (Exception ex)
            {
                _diag.Log($"SecurityViewModel: CRITICAL - Initial load failed: {ex}");
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        _diag.Log("SecurityViewModel: RefreshAllAsync triggered.");
        var firewallTask = LoadFirewallAsync();
        var portsTask = LoadPortsAsync();
        await Task.WhenAll(firewallTask, portsTask);
        _diag.Log("SecurityViewModel: RefreshAllAsync completed.");
    }

    [RelayCommand]
    private async Task RefreshFirewallAsync()
    {
        _diag.Log("SecurityViewModel: RefreshFirewall manually triggered.");
        await LoadFirewallAsync();
    }

    [RelayCommand]
    private async Task RefreshPortsAsync()
    {
        _diag.Log("SecurityViewModel: RefreshPorts manually triggered.");
        await LoadPortsAsync();
    }

    [RelayCommand]
    private void OpenFileLocation(PortConnectionInfo? connection)
    {
        if (connection == null || string.IsNullOrEmpty(connection.ProcessPath)) return;
        _diag.Log($"SecurityViewModel: Opening location for {connection.ProcessName}");
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select, \"{connection.ProcessPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex) { _diag.Log($"SecurityViewModel: OpenLocation Error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task KillProcessAsync(PortConnectionInfo? connection)
    {
        if (connection == null || connection.ProcessId <= 4) return;
        _diag.Log($"SecurityViewModel: Killing process {connection.ProcessName} (PID {connection.ProcessId})");
        try
        {
            using var p = Process.GetProcessById(connection.ProcessId);
            p.Kill();
            await Task.Delay(500);
            await LoadPortsAsync();
        }
        catch (Exception ex) { _diag.Log($"SecurityViewModel: KillProcess Error: {ex.Message}"); }
    }

    // ============================
    //  Filter triggers
    // ============================

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedProtocolChanged(string value) => ApplyFilter();
    partial void OnSelectedStateChanged(string value) => ApplyFilter();

    // ============================
    //  Data loading
    // ============================

    private async Task LoadFirewallAsync()
    {
        if (_isFirewallLoadingInternal)
        {
            _diag.Log("SecurityViewModel: LoadFirewall skipped (already loading).");
            return;
        }
        _isFirewallLoadingInternal = true;
        _diag.Log("SecurityViewModel: LoadFirewallAsync started.");

        await Dispatcher.UIThread.InvokeAsync(() => IsFirewallLoading = true);

        try
        {
            _diag.Log("SecurityViewModel: Calling firewallService.GetFirewallStatusAsync()...");
            var profiles = await _firewallService.GetFirewallStatusAsync();
            _diag.Log($"SecurityViewModel: Firewall service returned {profiles?.Count ?? 0} profiles.");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _diag.Log("SecurityViewModel: Updating FirewallProfiles on UI thread.");
                FirewallProfiles.Clear();
                if (profiles != null && profiles.Count > 0)
                {
                    foreach (var p in profiles)
                        FirewallProfiles.Add(p);

                    var allEnabled = profiles.All(p => p.IsEnabled);
                    var anyEnabled = profiles.Any(p => p.IsEnabled);

                    if (allEnabled)
                    {
                        FirewallOverallStatus = "防火墙已全部开启";
                        FirewallOverallColor = "LimeGreen";
                    }
                    else if (anyEnabled)
                    {
                        FirewallOverallStatus = "防火墙部分开启";
                        FirewallOverallColor = "Orange";
                    }
                    else
                    {
                        FirewallOverallStatus = "防火墙已全部关闭";
                        FirewallOverallColor = "Red";
                    }
                }
                else
                {
                    FirewallProfiles.Add(new FirewallProfileInfo { DisplayName = "域网络", StatusText = "无数据", StatusColor = "Gray", InboundAction = "未知", OutboundAction = "未知" });
                    FirewallProfiles.Add(new FirewallProfileInfo { DisplayName = "专用网络", StatusText = "无数据", StatusColor = "Gray", InboundAction = "未知", OutboundAction = "未知" });
                    FirewallProfiles.Add(new FirewallProfileInfo { DisplayName = "公用网络", StatusText = "无数据", StatusColor = "Gray", InboundAction = "未知", OutboundAction = "未知" });
                    FirewallOverallStatus = "读取结果为空";
                    FirewallOverallColor = "Red";
                }
                _diag.Log($"SecurityViewModel: FirewallProfiles UI updated. Count={FirewallProfiles.Count}");
            });
        }
        catch (Exception ex)
        {
            _diag.Log($"SecurityViewModel: LoadFirewall Error: {ex}");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                FirewallOverallStatus = $"读取失败: {ex.Message}";
                FirewallOverallColor = "Red";
            });
        }
        finally
        {
            _isFirewallLoadingInternal = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsFirewallLoading = false;
                _diag.Log("SecurityViewModel: Firewall loading state finished.");
            });
        }
    }

    private async Task LoadPortsAsync()
    {
        if (_isPortLoadingInternal)
        {
            _diag.Log("SecurityViewModel: LoadPorts skipped (already loading).");
            return;
        }
        _isPortLoadingInternal = true;
        _diag.Log("SecurityViewModel: LoadPortsAsync started.");

        await Dispatcher.UIThread.InvokeAsync(() => IsPortLoading = true);

        try
        {
            _diag.Log("SecurityViewModel: Calling portService.GetActiveConnectionsAsync()...");
            var connections = await _portService.GetActiveConnectionsAsync();
            _diag.Log($"SecurityViewModel: Port service returned {connections?.Count ?? 0} connections.");
            _allConnections = connections ?? new List<PortConnectionInfo>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalConnectionCount = _allConnections.Count;
                _diag.Log("SecurityViewModel: Applying port filter on UI thread.");
                ApplyFilter();
                _diag.Log($"SecurityViewModel: Port filter applied. Filtered={FilteredConnectionCount}");
            });
        }
        catch (Exception ex)
        {
            _diag.Log($"SecurityViewModel: LoadPorts Error: {ex}");
            await Dispatcher.UIThread.InvokeAsync(() => TotalConnectionCount = 0);
        }
        finally
        {
            _isPortLoadingInternal = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsPortLoading = false;
                _diag.Log("SecurityViewModel: Port loading state finished.");
            });
        }
    }

    private void ApplyFilter()
    {
        try
        {
            var query = _allConnections.AsEnumerable();

            if (SelectedProtocol != "全部")
                query = query.Where(c => c.Protocol.Contains(SelectedProtocol, StringComparison.OrdinalIgnoreCase));

            if (SelectedState != "全部")
                query = query.Where(c => c.State.Equals(SelectedState, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.Trim();
                query = query.Where(c =>
                    (c.LocalAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.RemoteAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.ProcessName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    c.ProcessId.ToString().Contains(search));
            }

            var filtered = query.ToList();
            FilteredConnectionCount = filtered.Count;

            FilteredConnections.Clear();
            foreach (var item in filtered)
                FilteredConnections.Add(item);
        }
        catch (Exception ex)
        {
            _diag.Log($"SecurityViewModel: ApplyFilter Error: {ex.Message}");
        }
    }
}
