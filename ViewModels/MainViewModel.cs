using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LinkSentry.Models;
using LinkSentry.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinkSentry.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INetworkService _networkService;
    private DispatcherTimer? _timer;

    public ObservableCollection<NetworkInterfaceModel> ConnectedInterfaces { get; } = new();
    public ObservableCollection<NetworkInterfaceModel> DisconnectedInterfaces { get; } = new();

    [ObservableProperty]
    private NetworkInterfaceModel? _selectedInterface;

    [ObservableProperty]
    private bool _isLoading;

    // ============================
    //  Page Navigation
    // ============================

    [ObservableProperty]
    private string _currentPage = "dashboard";

    [ObservableProperty]
    private SecurityViewModel? _securityViewModel;

    [RelayCommand]
    private void NavigateTo(string page)
    {
        // IMPORTANT: Create SecurityViewModel BEFORE setting CurrentPage.
        // Setting CurrentPage triggers PropertyChanged → UpdatePage,
        // which needs SecurityViewModel to already exist for correct DataContext.
        if (page == "security" && SecurityViewModel == null)
        {
            SecurityViewModel = App.Services?.GetService<SecurityViewModel>();
        }

        CurrentPage = page;
    }

    public MainViewModel(INetworkService networkService)
    {
        _networkService = networkService;
        // Wrap in try-catch because fire-and-forget async can crash if unhandled
        _ = SafeInitializeAsync();
    }

    private async Task SafeInitializeAsync()
    {
        try
        {
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializeAsync Error: {ex}");
        }
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _networkService.GetAllInterfacesAsync();
            Dispatcher.UIThread.Invoke(() =>
            {
                ConnectedInterfaces.Clear();
                DisconnectedInterfaces.Clear();
                foreach (var item in items)
                {
                    if (item.Status == System.Net.NetworkInformation.OperationalStatus.Up)
                        ConnectedInterfaces.Add(item);
                    else
                        DisconnectedInterfaces.Add(item);
                }
            });

            StartTimer();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void StartTimer()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _timer.Tick += async (s, e) =>
        {
            try
            {
                await RefreshTrafficAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshTrafficAsync Error: {ex}");
            }
        };
        _timer.Start();
    }

    private async Task RefreshTrafficAsync()
    {
        var all = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Concat(ConnectedInterfaces, DisconnectedInterfaces));
        await _networkService.UpdateTrafficStatisticsAsync(all);

        Dispatcher.UIThread.Post(() =>
        {
            var toConnect = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(DisconnectedInterfaces, x => x.Status == System.Net.NetworkInformation.OperationalStatus.Up));
            var toDisconnect = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(ConnectedInterfaces, x => x.Status != System.Net.NetworkInformation.OperationalStatus.Up));

            foreach (var item in toConnect)
            {
                DisconnectedInterfaces.Remove(item);
                ConnectedInterfaces.Add(item);
            }
            foreach (var item in toDisconnect)
            {
                ConnectedInterfaces.Remove(item);
                DisconnectedInterfaces.Add(item);
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        await InitializeAsync();
    }

    [RelayCommand]
    private void OpenDetails(NetworkInterfaceModel interfaceModel)
    {
        if (interfaceModel == null) return;
        
        var detailsWindow = new LinkSentry.Views.DetailWindow();
        var detailsViewModel = new DetailViewModel(_networkService, interfaceModel);
        detailsWindow.DataContext = detailsViewModel;

        // Optionally, make it a true dialog. We'll show it non-modal for now so they can see multiple interfaces.
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            detailsWindow.Show(desktop.MainWindow);
        }
        else
        {
            detailsWindow.Show();
        }
    }
}
