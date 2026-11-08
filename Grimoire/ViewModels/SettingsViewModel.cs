using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Services;

namespace Grimoire.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    private readonly AppSettings _settings;

    public SettingsViewModel(MainViewModel main, AppSettings settings)
    {
        _main = main;
        _settings = settings;
        ThemeIndex = (int)settings.Theme;
        SelectedLanguage = Languages.FirstOrDefault(l => l.Code == settings.Language) ?? Languages[0];
        Localizer.Instance.LanguageChanged += (_, _) => OnPropertyChanged(nameof(SignedInAs));
    }

    public IReadOnlyList<LanguageOption> Languages => Localizer.Languages;

    public string SignedInAs => Localizer.Instance.Format("Set_SignedInAs", _main.Source?.AccountName ?? "");

    /// <summary>0 = system, 1 = light, 2 = dark (matches <see cref="AppTheme"/>).</summary>
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    [ObservableProperty]
    public partial LanguageOption SelectedLanguage { get; set; }

    partial void OnThemeIndexChanged(int value)
    {
        if (value < 0) return;
        _settings.Theme = (AppTheme)value;
        _main.ApplyTheme(_settings.Theme);
        _main.SaveSettings();
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value is null || value.Code == _settings.Language) return;
        _settings.Language = value.Code;
        Localizer.Instance.SetLanguage(value.Code);
        _main.SaveSettings();
    }

    public void RefreshAccount() => OnPropertyChanged(nameof(SignedInAs));

    [RelayCommand]
    private void SignOut() => _main.SignOut();
}
