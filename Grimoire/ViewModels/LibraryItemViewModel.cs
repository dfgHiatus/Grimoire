using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Grimoire.Services;

namespace Grimoire.ViewModels;

/// <summary>A library entry as shown on cards in the gallery and grid.</summary>
public partial class LibraryItemViewModel : ViewModelBase
{
    private bool _coverRequested;

    public LibraryItemViewModel(LibraryItem item)
    {
        Id = item.Id;
        Name = item.Name;
        Author = item.Author;
        AuthorAvatarUrl = item.AuthorAvatarUrl;
        ImageUrl = item.ImageUrl;
        AcquiredAt = item.AcquiredAt;
        SourceOrder = item.SourceOrder;
        Initials = MakeInitials(item.Name);
        AuthorInitial = string.IsNullOrEmpty(item.Author) ? "?" : item.Author[..1].ToUpperInvariant();
        CoverBrush = MakeCoverBrush(item.Id);
        SearchName = SearchMatcher.Normalize(item.Name);
        SearchAuthor = SearchMatcher.Normalize(item.Author);
    }

    public string Id { get; }
    public string Name { get; }
    public string Author { get; }
    public string? AuthorAvatarUrl { get; }

    /// <summary>Position in the source's newest-first ordering; used as the date sort key.</summary>
    public int SourceOrder { get; }

    /// <summary>Shown on the placeholder cover when no image is available.</summary>
    public string Initials { get; }

    public string AuthorInitial { get; }

    /// <summary>Deterministic gradient so each item keeps its colour between sessions.</summary>
    public IBrush CoverBrush { get; }

    internal string SearchName { get; }
    internal string SearchAuthor { get; }

    [ObservableProperty]
    public partial string? ImageUrl { get; set; }

    [ObservableProperty]
    public partial Bitmap? Cover { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateText), nameof(HasDate))]
    public partial DateTimeOffset? AcquiredAt { get; set; }

    public bool HasDate => AcquiredAt is not null;
    public string? DateText => AcquiredAt?.ToLocalTime().ToString("d", CultureInfo.CurrentUICulture);

    /// <summary>Starts loading the cover image the first time a card is shown.</summary>
    public async void EnsureCover()
    {
        if (_coverRequested || string.IsNullOrEmpty(ImageUrl)) return;
        _coverRequested = true;
        Cover = await ImageCache.GetAsync(ImageUrl);
    }

    partial void OnImageUrlChanged(string? value)
    {
        _coverRequested = false;
        EnsureCover();
    }

    private static string MakeInitials(string name)
    {
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => char.IsLetterOrDigit(w[0]))
            .Take(2)
            .Select(w => char.ToUpper(w[0], CultureInfo.CurrentCulture));
        var initials = string.Concat(words);
        return initials.Length > 0 ? initials : "?";
    }

    private static IBrush MakeCoverBrush(string id)
    {
        // FNV-1a: stable across runs, unlike string.GetHashCode().
        uint hash = 2166136261;
        foreach (var c in id) hash = (hash ^ c) * 16777619;
        hash ^= hash >> 15; hash *= 0x2c1b3c6d; hash ^= hash >> 12; // spread similar IDs across the colour wheel

        var hue = hash % 360;
        var start = new HslColor(1, hue, 0.70, 0.62).ToRgb();
        var end = new HslColor(1, (hue + 40) % 360, 0.75, 0.48).ToRgb();
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(start, 0), new GradientStop(end, 1) },
        };
    }
}
