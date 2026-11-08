using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Grimoire.ViewModels;
using Grimoire.Views;

namespace Grimoire;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var vm = new MainViewModel();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = vm };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activityLifetime)
        {
            activityLifetime.MainViewFactory = () => new MainView { DataContext = vm };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = vm };
        }

        _ = vm.InitializeAsync();
        base.OnFrameworkInitializationCompleted();
    }
}
