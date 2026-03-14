using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Data;

namespace LinkSentry.Views;

public partial class TrafficHeatmapView : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<TrafficHeatmapView, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public TrafficHeatmapView()
    {
        InitializeComponent();
        
        // Link the control's ItemsSource to the inner ItemsControl
        var itemsControl = this.FindControl<ItemsControl>("HeatmapItems");
        if (itemsControl != null)
        {
            itemsControl.Bind(ItemsControl.ItemsSourceProperty, this.GetBindingObservable(ItemsSourceProperty));
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
