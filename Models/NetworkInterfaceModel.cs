using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using CommunityToolkit.Mvvm.Input;
using Avalonia;

namespace LinkSentry.Models;

public partial class NetworkInterfaceModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string NetworkInterfaceType { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayStatus))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(IsChartVisible))]
    private OperationalStatus _status;

    public bool IsChartVisible => Status == OperationalStatus.Up;

    public string DisplayStatus => Status switch
    {
        OperationalStatus.Up => "已连接",
        OperationalStatus.Down => "已断开",
        OperationalStatus.Testing => "测试中",
        OperationalStatus.Unknown => "未知",
        OperationalStatus.Dormant => "休眠",
        OperationalStatus.NotPresent => "设备不存在",
        OperationalStatus.LowerLayerDown => "底层已断开",
        _ => Status.ToString()
    };

    public string StatusColor => Status switch
    {
        OperationalStatus.Up => "LimeGreen",
        OperationalStatus.Down => "Red",
        _ => "Gray"
    };

    [ObservableProperty]
    private bool _copySuccess;

    [RelayCommand]
    private async System.Threading.Tasks.Task CopyIpv4Async()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null && !string.IsNullOrEmpty(Ipv4Address))
            {
                await clipboard.SetTextAsync(Ipv4Address);
                CopySuccess = true;
                
                // Auto-dismiss after 2 seconds
                _ = System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => CopySuccess = false);
                });
            }
        }
    }

    [ObservableProperty]
    private long _speed; // in bps

    [ObservableProperty]
    private string _ipv4Address = string.Empty;

    [ObservableProperty]
    private string _ipv6Address = string.Empty;

    [ObservableProperty]
    private long _bytesSent;

    [ObservableProperty]
    private long _bytesReceived;

    [ObservableProperty]
    private double _sendSpeedKbps;

    [ObservableProperty]
    private double _receiveSpeedKbps;
    
    public ObservableCollection<double> SendSpeedHistory { get; } = new();
    public ObservableCollection<double> ReceiveSpeedHistory { get; } = new();
    
    public ISeries[] TrafficSeries { get; }
    public Axis[] XAxes { get; }
    public Axis[] YAxes { get; }
    
    public NetworkInterfaceModel()
    {
        TrafficSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "上传",
                Values = SendSpeedHistory,
                Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(SKColors.LimeGreen.WithAlpha(40)),
                GeometrySize = 0,
                GeometryStroke = null,
                LineSmoothness = 0.6,
                YToolTipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name}: {chartPoint.Coordinate.PrimaryValue:F2} Kbps"
            },
            new LineSeries<double>
            {
                Name = "下载",
                Values = ReceiveSpeedHistory,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(40)),
                GeometrySize = 0,
                GeometryStroke = null,
                LineSmoothness = 0.6,
                YToolTipLabelFormatter = (chartPoint) => $"{chartPoint.Context.Series.Name}: {chartPoint.Coordinate.PrimaryValue:F2} Kbps"
            }
        };

        XAxes = new Axis[]
        {
            new Axis 
            { 
                IsVisible = false 
            }
        };

        YAxes = new Axis[]
        {
            new Axis 
            { 
                 IsVisible = true,
                 Name = "Kbps",
                 NameTextSize = 10,
                 TextSize = 10,
                 LabelsPaint = new SolidColorPaint(SKColors.Gray),
                 MinLimit = 0,
                 MinStep = 100,
                 MaxLimit = 100     
            }
        };
    }
    
    public ObservableCollection<string> DnsAddresses { get; } = [];
    public ObservableCollection<string> GatewayAddresses { get; } = [];
    
    [ObservableProperty]
    private bool _isDhcpEnabled;
}
