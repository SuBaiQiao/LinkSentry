using System;
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

namespace LinkSentry.ViewModels;

public partial class DetailViewModel : ObservableObject
{
    private readonly INetworkService _networkService;
    
    [ObservableProperty]
    private NetworkInterfaceModel _networkInterface;

    [ObservableProperty]
    private bool _isBusy;

    public DetailViewModel(INetworkService networkService, NetworkInterfaceModel networkInterface)
    {
        _networkService = networkService;
        _networkInterface = networkInterface;
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
