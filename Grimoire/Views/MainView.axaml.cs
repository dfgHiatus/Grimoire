using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Grimoire.ViewModels;

namespace Grimoire.Views;

public partial class MainView : UserControl
{
    private TopLevel? _topLevel;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null)
            _topLevel.BackRequested += OnBackRequested;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_topLevel is not null)
            _topLevel.BackRequested -= OnBackRequested;
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    // Android back button / gesture: close the item overlay before leaving the app.
    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.HandleBack())
            e.Handled = true;
    }
}
