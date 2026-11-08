using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Services;

namespace Grimoire.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly MainViewModel _main;

    public LoginViewModel(MainViewModel main) => _main = main;

    public Uri HelpUrl { get; } = new(ProjectInfo.JinxxyApiDocsUrl);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    public partial string? ApiKey { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand), nameof(UseDemoCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool ShowWhyNoPassword { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnApiKeyChanged(string? value) => ErrorMessage = null;

    private bool CanSignIn() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private Task SignInAsync() => SignInCoreAsync(ApiKey, isAutomatic: false);

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private void UseDemo()
    {
        ErrorMessage = null;
        _main.CompleteSignIn(new DemoLibrarySource(), apiKey: null);
    }

    [RelayCommand]
    private void ToggleWhyNoPassword() => ShowWhyNoPassword = !ShowWhyNoPassword;

    /// <summary>Used on startup with a saved key.</summary>
    public Task AutoSignInAsync(string apiKey)
    {
        ApiKey = apiKey;
        return SignInCoreAsync(apiKey, isAutomatic: true);
    }

    private async Task SignInCoreAsync(string? apiKey, bool isAutomatic)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var source = await JinxxyLibrarySource.SignInAsync(apiKey?.Trim() ?? "");
            _main.CompleteSignIn(source, apiKey!.Trim());
            ApiKey = null; // don't keep the secret in the login form after success
        }
        catch (SignInException ex)
        {
            ErrorMessage = Localizer.Instance[ex.MessageKey];
            // A saved key that is now rejected should be forgotten; network errors should not.
            if (isAutomatic && ex.MessageKey is "Err_InvalidKey" or "Err_MissingScope")
                _main.ForgetSavedKey();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
