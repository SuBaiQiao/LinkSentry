using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LinkSentry.Views;

public partial class LongTermHeatmapView : UserControl
{
    public LongTermHeatmapView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
