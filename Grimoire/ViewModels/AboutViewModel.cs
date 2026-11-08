using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Services;

namespace Grimoire.ViewModels;

public partial class ContributorViewModel : ViewModelBase
{
    public ContributorViewModel(int rank, GitHubContributor c)
    {
        Rank = rank;
        Login = c.Login;
        Contributions = c.Contributions;
        ProfileUrl = Uri.TryCreate(c.HtmlUrl, UriKind.Absolute, out var u) ? u : null;
        Initial = string.IsNullOrEmpty(c.Login) ? "?" : c.Login[..1].ToUpperInvariant();
        _ = LoadAvatarAsync(c.AvatarUrl);
    }

    public int Rank { get; }
    public string Login { get; }
    public int Contributions { get; }
    public string ContributionsText => Localizer.Instance.Format("About_Contributions", Contributions);
    public Uri? ProfileUrl { get; }
    public string Initial { get; }

    [ObservableProperty]
    public partial Bitmap? Avatar { get; set; }

    private async Task LoadAvatarAsync(string? url) => Avatar = await ImageCache.GetAsync(url, 96);
}

public partial class AboutViewModel : ViewModelBase
{
    private bool _loaded;

    public string VersionText => "v" + ProjectInfo.Version;
    public Uri ProjectUrl { get; } = new(ProjectInfo.RepositoryUrl);
    public string ProjectUrlText => ProjectInfo.RepositoryUrl.Replace("https://", "", StringComparison.Ordinal);
    public Uri LicensesUrl { get; } = new(ProjectInfo.LicensesUrl);

    public ObservableCollection<ContributorViewModel> Contributors { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    /// <summary>Loads contributors the first time the About page is shown.</summary>
    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _ = LoadContributorsAsync();
    }

    [RelayCommand]
    private async Task LoadContributorsAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            var list = await GitHubService.GetContributorsAsync();
            Contributors.Clear();
            var rank = 1;
            foreach (var c in list) Contributors.Add(new ContributorViewModel(rank++, c));
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
