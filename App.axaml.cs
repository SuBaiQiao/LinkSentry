using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LinkSentry.Services;
using LinkSentry.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkSentry
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });

            services.AddSingleton<IDiagnosticLogger, DiagnosticLogger>();
            services.AddSingleton<SqliteDbFactory>();
            services.AddSingleton<ITrafficHistoryService, TrafficHistoryService>();
            services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<IFirewallService, FirewallService>();
            services.AddSingleton<IPortService, PortService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<SecurityViewModel>();

            Services = services.BuildServiceProvider();

            // Initialize SQLite DB
            Services.GetRequiredService<SqliteDbFactory>().Initialize();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}