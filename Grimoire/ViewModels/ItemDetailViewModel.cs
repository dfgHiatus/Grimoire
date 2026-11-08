using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Services;

namespace Grimoire.ViewModels;

public sealed record DetailRow(string Label, string Value);

/// <summary>Full-screen view of a single library item.</summary>
public partial class ItemDetailViewModel : ViewModelBase
{
    private readonly ILibrarySource _source;
    private readonly Action _close;

    public ItemDetailViewModel(LibraryItemViewModel item, ILibrarySource source, Action close)
    {
        Item = item;
        _source = source;
        _close = close;
        HeroImage = item.Cover;
        ByAuthor = Localizer.Instance.Format("Detail_By", item.Author);
    }

    public LibraryItemViewModel Item { get; }
    public string ByAuthor { get; }

    public ObservableCollection<DetailRow> Fields { get; } = [];
    public ObservableCollection<VersionInfo> Versions { get; } = [];
    public ObservableCollection<string> Tags { get; } = [];

    [ObservableProperty]
    public partial Bitmap? HeroImage { get; set; }

    [ObservableProperty]
    public partial Bitmap? AuthorAvatar { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFields), nameof(HasVersions), nameof(HasTags))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStoreUrl))]
    public partial Uri? StoreUrl { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDownload), nameof(ShowNoDownloadNote))]
    public partial Uri? DownloadUrl { get; set; }

    public bool HasError => ErrorMessage is not null;
    public bool HasFields => Fields.Count > 0;
    public bool HasVersions => Versions.Count > 0;
    public bool HasTags => Tags.Count > 0;
    public bool HasStoreUrl => StoreUrl is not null;
    public bool HasDownload => DownloadUrl is not null;
    public bool ShowNoDownloadNote => !IsLoading && DownloadUrl is null;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        OnPropertyChanged(nameof(ShowNoDownloadNote));
        _ = LoadAvatarAsync();
        try
        {
            var details = await _source.GetDetailsAsync(Item.Id);

            Fields.Clear();
            foreach (var f in details.Fields)
                Fields.Add(new DetailRow(Localizer.Instance[f.LabelKey], f.Value));
            Versions.Clear();
            foreach (var v in details.Versions) Versions.Add(v);
            Tags.Clear();
            foreach (var t in details.Tags) Tags.Add(t);

            StoreUrl = TryUri(details.StoreUrl);
            DownloadUrl = TryUri(details.DownloadUrl);
            if (details.AcquiredAt is not null) Item.AcquiredAt = details.AcquiredAt;
            if (details.ImageUrl is not null)
            {
                Item.ImageUrl ??= details.ImageUrl;
                HeroImage = await ImageCache.GetAsync(details.ImageUrl, 1280) ?? HeroImage;
            }
        }
        catch (SignInException ex)
        {
            ErrorMessage = Localizer.Instance[ex.MessageKey];
        }
        catch (Exception)
        {
            ErrorMessage = Localizer.Instance["Err_Unknown"];
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowNoDownloadNote));
        }
    }

    [RelayCommand]
    private void Close() => _close();

    private async Task LoadAvatarAsync() =>
        AuthorAvatar = await ImageCache.GetAsync(Item.AuthorAvatarUrl, 96);

    private static Uri? TryUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
}
