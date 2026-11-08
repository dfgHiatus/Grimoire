using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Services;

namespace Grimoire.ViewModels;

public enum AppTab { Library, Settings, About }

/// <summary>Shell: owns sign-in state, bottom navigation and the full-screen item overlay.</summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsStore _store;

    public MainViewModel() : this(new SettingsStore()) { }

    public MainViewModel(SettingsStore store)
    {
        _store = store;
        _store.Load();
        Login = new LoginViewModel(this);
        SettingsPage = new SettingsViewModel(this, _store.Current);
        AboutPage = new AboutViewModel();
    }

    public LoginViewModel Login { get; }
    public SettingsViewModel SettingsPage { get; }
    public AboutViewModel AboutPage { get; }

    [ObservableProperty]
    public partial LibraryViewModel? LibraryPage { get; private set; }

    [ObservableProperty]
    public partial ILibrarySource? Source { get; private set; }

    [ObservableProperty]
    public partial bool IsSignedIn { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLibraryTab), nameof(IsSettingsTab), nameof(IsAboutTab))]
    public partial AppTab Tab { get; set; }

    [ObservableProperty]
    public partial ViewModelBase? CurrentPage { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetail))]
    public partial ItemDetailViewModel? Detail { get; private set; }

    public bool HasDetail => Detail is not null;
    public bool IsLibraryTab => Tab == AppTab.Library;
    public bool IsSettingsTab => Tab == AppTab.Settings;
    public bool IsAboutTab => Tab == AppTab.About;

    /// <summary>Applies saved preferences and signs in with a saved key, if any.</summary>
    public async Task InitializeAsync()
    {
        var settings = _store.Current;
        Localizer.Instance.SetLanguage(settings.Language);
        ApplyTheme(settings.Theme);

        if (settings.DemoMode)
            CompleteSignIn(new DemoLibrarySource(), apiKey: null);
        else if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            await Login.AutoSignInAsync(settings.ApiKey);
    }

    public void CompleteSignIn(ILibrarySource source, string? apiKey)
    {
        (Source as IDisposable)?.Dispose();
        Source = source;
        _store.Current.ApiKey = apiKey;
        _store.Current.DemoMode = source.IsDemo;
        SaveSettings();

        LibraryPage = new LibraryViewModel(source, _store.Current, SaveSettings, OpenItem);
        SettingsPage.RefreshAccount();
        Tab = AppTab.Library;
        OnTabChanged(Tab);
        IsSignedIn = true;
        _ = LibraryPage.LoadAsync();
    }

    public void SignOut()
    {
        Detail = null;
        (Source as IDisposable)?.Dispose();
        Source = null;
        LibraryPage = null;
        IsSignedIn = false;
        _store.Current.ApiKey = null;
        _store.Current.DemoMode = false;
        _store.Current.RecentlyViewed.Clear();
        SaveSettings();
    }

    public void ForgetSavedKey()
    {
        _store.Current.ApiKey = null;
        SaveSettings();
    }

    public void SaveSettings() => _store.Save();

    public void ApplyTheme(AppTheme theme)
    {
        if (Application.Current is null) return;
        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    [RelayCommand]
    private void Navigate(AppTab tab)
    {
        Detail = null;
        Tab = tab;
    }

    partial void OnTabChanged(AppTab value)
    {
        CurrentPage = value switch
        {
            AppTab.Settings => SettingsPage,
            AppTab.About => AboutPage,
            _ => LibraryPage,
        };
        if (value == AppTab.About) AboutPage.EnsureLoaded();
    }

    private void OpenItem(LibraryItemViewModel item)
    {
        if (Source is null) return;
        Detail = new ItemDetailViewModel(item, Source, CloseDetail);
        _ = Detail.LoadAsync();
    }

    [RelayCommand]
    private void CloseDetail() => Detail = null;

    [RelayCommand]
    private void Back() => HandleBack();

    /// <summary>Handles the system back gesture (Android back button, Escape on desktop).</summary>
    public bool HandleBack()
    {
        if (Detail is not null)
        {
            Detail = null;
            return true;
        }
        if (IsSignedIn && Tab != AppTab.Library)
        {
            Tab = AppTab.Library;
            return true;
        }
        return false;
    }
}
