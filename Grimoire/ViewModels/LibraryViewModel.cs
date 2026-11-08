using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Services;

namespace Grimoire.ViewModels;

public enum LibrarySort { Date, Name, Author }

public partial class LibraryViewModel : ViewModelBase
{
    private const int MaxRecent = 12;

    private readonly ILibrarySource _source;
    private readonly AppSettings _settings;
    private readonly Action _saveSettings;
    private readonly Action<LibraryItemViewModel> _open;
    private List<LibraryItemViewModel> _all = [];
    private CancellationTokenSource? _loadCts;

    public LibraryViewModel(ILibrarySource source, AppSettings settings, Action saveSettings, Action<LibraryItemViewModel> open)
    {
        _source = source;
        _settings = settings;
        _saveSettings = saveSettings;
        _open = open;
    }

    public bool IsDemo => _source.IsDemo;

    public ObservableCollection<LibraryItemViewModel> Items { get; } = [];
    public ObservableCollection<LibraryItemViewModel> Recent { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResults), nameof(HasRecent))]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortByDate), nameof(SortByName), nameof(SortByAuthor))]
    public partial LibrarySort Sort { get; set; } = LibrarySort.Date;

    [ObservableProperty]
    public partial bool SortDescending { get; set; } = true;

    [ObservableProperty]
    public partial bool MatchName { get; set; } = true;

    [ObservableProperty]
    public partial bool MatchAuthor { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(HasNoResults), nameof(HasRecent))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(IsEmpty))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    public partial int ItemCount { get; set; }

    public bool HasItems => ItemCount > 0;

    public bool HasError => ErrorMessage is not null;
    public bool IsEmpty => !IsLoading && !HasError && _all.Count == 0;
    public bool HasNoResults => !IsLoading && _all.Count > 0 && Items.Count == 0;
    /// <summary>The gallery is hidden while searching so it doesn't compete with the results.</summary>
    public bool HasRecent => !IsLoading && Recent.Count > 0 && string.IsNullOrWhiteSpace(SearchText);

    // Radio-button friendly wrappers around Sort.
    public bool SortByDate { get => Sort == LibrarySort.Date; set { if (value) Sort = LibrarySort.Date; } }
    public bool SortByName { get => Sort == LibrarySort.Name; set { if (value) Sort = LibrarySort.Name; } }
    public bool SortByAuthor { get => Sort == LibrarySort.Author; set { if (value) Sort = LibrarySort.Author; } }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnSortChanged(LibrarySort value) => ApplyFilter();
    partial void OnSortDescendingChanged(bool value) => ApplyFilter();
    partial void OnMatchNameChanged(bool value) => ApplyFilter();
    partial void OnMatchAuthorChanged(bool value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var items = await _source.LoadAsync(ct);
            _all = items.Select(i => new LibraryItemViewModel(i)).ToList();
            ItemCount = _all.Count;
            ApplyFilter();
            RebuildRecent();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return;
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
            if (!ct.IsCancellationRequested) IsLoading = false;
        }
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(HasRecent));
    }

    [RelayCommand]
    private void Open(LibraryItemViewModel item)
    {
        _settings.RecentlyViewed.Remove(item.Id);
        _settings.RecentlyViewed.Insert(0, item.Id);
        if (_settings.RecentlyViewed.Count > MaxRecent)
            _settings.RecentlyViewed.RemoveRange(MaxRecent, _settings.RecentlyViewed.Count - MaxRecent);
        _saveSettings();

        _open(item);
        RebuildRecent(); // after opening so the gallery reshuffles behind the overlay, not in front of the user
    }

    [RelayCommand]
    private void ClearRecent()
    {
        _settings.RecentlyViewed.Clear();
        _saveSettings();
        RebuildRecent();
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = null;

    private void ApplyFilter()
    {
        var tokens = SearchMatcher.Tokenize(SearchText);
        // If the user un-ticks both "search in" boxes, searching nothing would be useless; search both instead.
        var useName = MatchName || !MatchAuthor;
        var useAuthor = MatchAuthor || !MatchName;

        IEnumerable<LibraryItemViewModel> query = _all;
        if (tokens.Length > 0)
        {
            query = query.Where(i => SearchMatcher.Matches(tokens,
                useName ? i.SearchName : "",
                useAuthor ? i.SearchAuthor : ""));
        }

        var comparer = StringComparer.CurrentCultureIgnoreCase;
        query = Sort switch
        {
            // SourceOrder 0 is the newest item, so "descending" (newest first) is ascending SourceOrder.
            LibrarySort.Date => SortDescending ? query.OrderBy(i => i.SourceOrder) : query.OrderByDescending(i => i.SourceOrder),
            LibrarySort.Name => SortDescending ? query.OrderByDescending(i => i.Name, comparer) : query.OrderBy(i => i.Name, comparer),
            _ => SortDescending
                ? query.OrderByDescending(i => i.Author, comparer).ThenBy(i => i.Name, comparer)
                : query.OrderBy(i => i.Author, comparer).ThenBy(i => i.Name, comparer),
        };

        var result = query.ToList();
        if (!result.SequenceEqual(Items))
        {
            Items.Clear();
            foreach (var item in result) Items.Add(item);
        }
        OnPropertyChanged(nameof(HasNoResults));
    }

    private void RebuildRecent()
    {
        var byId = _all.ToDictionary(i => i.Id);
        var recent = _settings.RecentlyViewed.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        if (!recent.SequenceEqual(Recent))
        {
            Recent.Clear();
            foreach (var item in recent) Recent.Add(item);
        }
        OnPropertyChanged(nameof(HasRecent));
    }
}
