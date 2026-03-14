using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkSentry.Models;
using LinkSentry.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using Avalonia.Threading;

namespace LinkSentry.ViewModels;

public partial class DetailViewModel : ObservableObject
{
    private readonly INetworkService _networkService;
    private readonly ITrafficHistoryService _historyService;
    private readonly DispatcherTimer _refreshTimer;
    
    [ObservableProperty]
    private NetworkInterfaceModel _networkInterface;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _totalTrafficSummary = "正在计算...";

    [ObservableProperty]
    private string _mostActiveDayInfo = "正在分析...";

    [ObservableProperty]
    private string _month1Label = "";

    [ObservableProperty]
    private string _month2Label = "";

    [ObservableProperty]
    private string _month3Label = "";

    public ObservableCollection<HeatmapDataPoint> HeatmapData { get; } = new();
    
    public ObservableCollection<HeatmapDataPoint> LongTermHeatmapData { get; } = new();

    public DetailViewModel(INetworkService networkService, ITrafficHistoryService historyService, NetworkInterfaceModel networkInterface)
    {
        _networkService = networkService;
        _historyService = historyService;
        _networkInterface = networkInterface;

        // Setup 5-second polling timer
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _refreshTimer.Tick += async (s, e) => await LoadHeatmapAsync();
        _refreshTimer.Start();

        // Load heatmap on start
        _ = LoadHeatmapAsync();
    }

    [RelayCommand]
    public async Task LoadHeatmapAsync()
    {
        var endTime = DateTime.Now;
        var startTime = endTime.Date.AddDays(-6); // Last 7 days including today

        var rawData = await _historyService.GetHeatmapDataAsync(NetworkInterface.Name, startTime, endTime, "Hour");

        // We need 24 * 7 = 168 points to fill the grid perfectly (one for each hour)
        // Ensure every hour exists even if no data
        var filledData = new List<HeatmapDataPoint>();
        for (int d = 6; d >= 0; d--)
        {
            var day = endTime.Date.AddDays(-d);
            for (int h = 0; h < 24; h++)
            {
                var time = day.AddHours(h);
                var existing = rawData.FirstOrDefault(p => p.Time.Date == day && p.Time.Hour == h);
                
                filledData.Add(existing ?? new HeatmapDataPoint 
                { 
                    Time = time,
                    IntensityLevel = 0 
                });
            }
        }

        HeatmapData.Clear();
        foreach (var p in filledData) HeatmapData.Add(p);

        // Load 3-month summary
        await LoadLongTermStatsAsync();
    }

    private async Task LoadLongTermStatsAsync()
    {
        var endTime = DateTime.Now;
        var startTime = endTime.Date.AddDays(-90); // 3 months

        var dailyData = await _historyService.GetDailyTrafficAsync(NetworkInterface.Name, startTime, endTime);

        if (dailyData.Count == 0)
        {
            TotalTrafficSummary = "过去 3 个月总流量: 0 B";
            MostActiveDayInfo = "最活跃: 无数据";
            return;
        }

        // Calculate Summary
        double totalDownload = dailyData.Sum(p => p.AvgDownload);
        double totalUpload = dailyData.Sum(p => p.AvgUpload);
        var peakDay = dailyData.OrderByDescending(p => p.AvgDownload + p.AvgUpload).First();

        TotalTrafficSummary = $"过去 3 个月总流量: {FormatBytes(totalDownload + totalUpload)}";
        MostActiveDayInfo = $"最活跃: {peakDay.Time:MM月dd日} ({FormatBytes(peakDay.AvgDownload + peakDay.AvgUpload)})";

        Month3Label = endTime.ToString("M月");
        Month2Label = endTime.AddMonths(-1).ToString("M月");
        Month1Label = endTime.AddMonths(-2).ToString("M月");

        // Fills exactly 91 days (13 weeks) structured for a 13x7 UniformGrid
        var filledLongTerm = new List<HeatmapDataPoint>();
        var gridEndDate = DateTime.Now.Date;
        int todayRow = (int)gridEndDate.DayOfWeek; // 0=Sun, 1=Mon .. 6=Sat

        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 13; col++)
            {
                int daysOffset = (col - 12) * 7 + (row - todayRow);
                var cellDate = gridEndDate.AddDays(daysOffset);

                // If future or outside the 90 day window, add empty
                if (daysOffset > 0 || daysOffset < -90)
                {
                    filledLongTerm.Add(new HeatmapDataPoint { Time = cellDate, IntensityLevel = 0 });
                }
                else
                {
                    var existing = dailyData.FirstOrDefault(p => p.Time.Date == cellDate);
                    filledLongTerm.Add(existing ?? new HeatmapDataPoint { Time = cellDate, IntensityLevel = 0 });
                }
            }
        }

        LongTermHeatmapData.Clear();
        foreach (var p in filledLongTerm) LongTermHeatmapData.Add(p);
    }

    private string FormatBytes(double bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int unitIndex = 0;
        while (bytes >= 1024 && unitIndex < units.Length - 1)
        {
            bytes /= 1024;
            unitIndex++;
        }
        return $"{bytes:F2} {units[unitIndex]}";
    }

    [RelayCommand]
    private async Task EnableInterfaceAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _networkService.EnableInterfaceAsync(NetworkInterface.Name);
            // Optionally refresh the model here or let the main timer pick up the change
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisableInterfaceAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _networkService.DisableInterfaceAsync(NetworkInterface.Name);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshDhcpAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _networkService.RefreshDhcpAsync(NetworkInterface.Name);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FlushDnsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _networkService.FlushDnsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
