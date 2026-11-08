using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Services;

/// <summary>One entry in the user's library, independent of where it came from.</summary>
public sealed record LibraryItem(
    string Id,
    string Name,
    string Author,
    string? AuthorAvatarUrl,
    string? ImageUrl,
    DateTimeOffset? AcquiredAt,
    int SourceOrder);

public sealed record DetailField(string LabelKey, string Value);

public sealed record VersionInfo(string Name, string? Price);

/// <summary>Extra information shown in the expanded item view.</summary>
public sealed record LibraryItemDetails(
    string Id,
    string? ImageUrl,
    DateTimeOffset? AcquiredAt,
    IReadOnlyList<DetailField> Fields,
    IReadOnlyList<VersionInfo> Versions,
    IReadOnlyList<string> Tags,
    string? StoreUrl,
    string? DownloadUrl);

/// <summary>
/// Abstraction over where library contents come from. The Jinxxy Creator API is the only
/// implementation today; a buyer-side source can be added here if Jinxxy ever exposes one.
/// </summary>
public interface ILibrarySource
{
    string AccountName { get; }
    string? AccountAvatarUrl { get; }
    bool IsDemo { get; }

    Task<IReadOnlyList<LibraryItem>> LoadAsync(CancellationToken ct = default);
    Task<LibraryItemDetails> GetDetailsAsync(string id, CancellationToken ct = default);
}

/// <summary>Sign-in failure with a localization key describing the reason.</summary>
public sealed class SignInException(string messageKey, Exception? inner = null) : Exception(messageKey, inner)
{
    public string MessageKey { get; } = messageKey;
}
