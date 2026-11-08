using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Grimoire.Services;

/// <summary>Downloads and decodes remote images once per URL for the lifetime of the app.</summary>
public static class ImageCache
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly ConcurrentDictionary<string, Task<Bitmap?>> Cache = new();

    public static Task<Bitmap?> GetAsync(string? url, int decodeWidth = 512)
    {
        if (string.IsNullOrWhiteSpace(url)) return Task.FromResult<Bitmap?>(null);
        return Cache.GetOrAdd($"{decodeWidth}|{url}", _ => LoadAsync(url!, decodeWidth));
    }

    private static async Task<Bitmap?> LoadAsync(string url, int decodeWidth)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(url);
            using var stream = new MemoryStream(bytes);
            return Bitmap.DecodeToWidth(stream, decodeWidth);
        }
        catch (Exception)
        {
            // Network failure or an image format Skia can't decode: show the placeholder instead.
            Cache.TryRemove($"{decodeWidth}|{url}", out _);
            return null;
        }
    }
}
