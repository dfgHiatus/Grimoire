using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jinxxy;
using Jinxxy.Models;

namespace Grimoire.Services;

/// <summary>Library backed by the Jinxxy Creator API: lists the products of the API key's owner.</summary>
public sealed class JinxxyLibrarySource : ILibrarySource, IDisposable
{
    private readonly JinxxyClient _client;
    private readonly CurrentUser _me;
    private readonly ConcurrentDictionary<string, Task<Product>> _products = new();

    private JinxxyLibrarySource(JinxxyClient client, CurrentUser me)
    {
        _client = client;
        _me = me;
    }

    public string AccountName => _me.DisplayName;
    public string? AccountAvatarUrl => _me.ProfileImage?.Url;
    public bool IsDemo => false;

    /// <summary>Validates the key with <c>GET /me</c> and checks that it can read products.</summary>
    public static async Task<JinxxyLibrarySource> SignInAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new SignInException("Err_EmptyKey");

        var client = new JinxxyClient(apiKey, options: new JinxxyClientOptions { UserAgent = $"Grimoire/{ProjectInfo.Version}" });
        try
        {
            var me = await client.GetMeAsync(ct);
            if (!me.Scopes.Contains(JinxxyScopes.ProductsRead))
                throw new SignInException("Err_MissingScope");
            return new JinxxyLibrarySource(client, me);
        }
        catch (Exception ex)
        {
            client.Dispose();
            throw Translate(ex);
        }
    }

    public async Task<IReadOnlyList<LibraryItem>> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            var items = new List<LibraryItem>();
            var query = new ListQuery { Limit = 100, SortField = ListSortField.CreatedAt, SortOrder = SortOrder.Descending };
            await foreach (var p in _client.EnumerateProductsAsync(query, ct))
            {
                // The list endpoint doesn't include dates; SourceOrder preserves the API's newest-first order
                // so date sorting is correct before details have been fetched.
                items.Add(new LibraryItem(p.Id, p.Name, AccountName, AccountAvatarUrl, null, null, items.Count));
            }
            return items;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw Translate(ex);
        }
    }

    public async Task<LibraryItemDetails> GetDetailsAsync(string id, CancellationToken ct = default)
    {
        Product product;
        try
        {
            product = await _products.GetOrAdd(id, key => _client.GetProductAsync(key, CancellationToken.None)).WaitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _products.TryRemove(id, out _); // allow retry
            throw Translate(ex);
        }

        var fields = new List<DetailField>();
        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) fields.Add(new DetailField(key, value!));
        }

        Add("Detail_Added", product.CreatedAt.ToLocalTime().ToString("D", CultureInfo.CurrentUICulture));
        Add("Detail_Updated", product.UpdatedAt.ToLocalTime().ToString("D", CultureInfo.CurrentUICulture));
        Add("Detail_Price", product.PwywPricing
            ? FormatPrice(product.BasePrice, product.CurrencyCode) + "+"
            : FormatPrice(product.BasePrice, product.CurrencyCode));
        Add("Detail_Category", Humanize(product.Category));
        Add("Detail_Type", Join(product.Type));
        Add("Detail_Platforms", Join(product.Platforms));
        Add("Detail_Gender", Humanize(product.Gender));
        Add("Detail_Visibility", Humanize(product.Visibility));
        Add("Detail_Restrictions", Join(product.Restrictions));

        return new LibraryItemDetails(
            product.Id,
            FindImageUrl(product),
            product.CreatedAt,
            fields,
            product.Versions.Select(v => new VersionInfo(v.Name, FormatPrice(v.Price, product.CurrencyCode))).ToList(),
            product.Tags,
            string.IsNullOrWhiteSpace(product.Url) ? null : product.Url,
            DownloadUrl: null); // The Creator API exposes no file downloads.
    }

    /// <summary>
    /// The documented schema has no image field. If the live API returns one anyway
    /// (captured in <see cref="Product.AdditionalData"/>), use it.
    /// </summary>
    private static string? FindImageUrl(Product product)
    {
        if (product.AdditionalData is null) return null;
        foreach (var (key, value) in product.AdditionalData)
        {
            if (!key.Contains("image", StringComparison.OrdinalIgnoreCase) &&
                !key.Contains("cover", StringComparison.OrdinalIgnoreCase) &&
                !key.Contains("thumbnail", StringComparison.OrdinalIgnoreCase))
                continue;

            var url = ExtractUrl(value);
            if (url is not null) return url;
        }
        return null;

        static string? ExtractUrl(JsonElement e) => e.ValueKind switch
        {
            JsonValueKind.String when Uri.TryCreate(e.GetString(), UriKind.Absolute, out var u) && u.Scheme.StartsWith("http") => u.ToString(),
            JsonValueKind.Object when e.TryGetProperty("url", out var inner) => ExtractUrl(inner),
            JsonValueKind.Array when e.GetArrayLength() > 0 => ExtractUrl(e[0]),
            _ => null,
        };
    }

    /// <summary>Prices are assumed to be in minor units (cents), matching the API's discount-code amounts.</summary>
    private static string FormatPrice(decimal minorUnits, string currency)
    {
        var amount = minorUnits / 100m;
        if (amount == 0) return Localizer.Instance["Detail_Free"];
        return string.IsNullOrWhiteSpace(currency)
            ? amount.ToString("0.00", CultureInfo.CurrentUICulture)
            : $"{amount.ToString("0.00", CultureInfo.CurrentUICulture)} {currency.ToUpperInvariant()}";
    }

    private static string? Join(IReadOnlyList<string>? values) =>
        values is null || values.Count == 0 ? null : string.Join(", ", values.Select(Humanize));

    private static string? Humanize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value!.Replace('_', ' ').ToLowerInvariant();
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static Exception Translate(Exception ex) => ex switch
    {
        SignInException => ex,
        JinxxyApiException { IsUnauthorized: true } => new SignInException("Err_InvalidKey", ex),
        JinxxyApiException { IsForbidden: true } => new SignInException("Err_MissingScope", ex),
        JinxxyApiException { IsRateLimited: true } => new SignInException("Err_RateLimited", ex),
        JinxxyApiException => new SignInException("Err_Server", ex),
        HttpRequestException or TaskCanceledException => new SignInException("Err_Network", ex),
        _ => new SignInException("Err_Unknown", ex),
    };

    public void Dispose() => _client.Dispose();
}
