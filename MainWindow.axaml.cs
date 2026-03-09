using System.ComponentModel;
using Avalonia.Controls;
using LinkSentry.ViewModels;
using LinkSentry.Views;

namespace LinkSentry
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += OnViewModelPropertyChanged;
                UpdatePage(vm.CurrentPage);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentPage) && DataContext is MainViewModel vm)
            {
                UpdatePage(vm.CurrentPage);
            }
        }

        private void UpdatePage(string page)
        {
            if (DataContext is not MainViewModel vm) return;

            UserControl? newPage = page switch
            {
                "dashboard" => new DashboardView { DataContext = vm },
                "security" => vm.SecurityViewModel != null
                    ? new SecurityView { DataContext = vm.SecurityViewModel }
                    : null,
                _ => null
            };

            if (newPage != null)
            {
                PageContent.Content = newPage;
                
                // Update title text block if found
                var titleBlock = this.FindControl<TextBlock>("PageTitleText");
                if (titleBlock != null)
                {
                    titleBlock.Text = page switch
                    {
                        "dashboard" => "仪表盘",
                        "security" => "安全与端口",
                        _ => page
                    };
                }
            }
        }

    }
}
