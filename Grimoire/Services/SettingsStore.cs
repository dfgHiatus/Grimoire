using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grimoire.Services;

public enum AppTheme { System, Light, Dark }

public sealed class AppSettings
{
    /// <summary>
    /// Jinxxy API key. Stored in the per-user app data folder in plain text;
    /// see README for why OS keychain storage isn't used yet.
    /// </summary>
    public string? ApiKey { get; set; }
    public bool DemoMode { get; set; }
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>Two-letter language code, or null to follow the OS language.</summary>
    public string? Language { get; set; }

    /// <summary>Most recently viewed item IDs, newest first.</summary>
    public List<string> RecentlyViewed { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON. Falls back to memory-only when no writable folder exists (e.g. WebAssembly).</summary>
public sealed class SettingsStore
{
    private readonly string? _path;

    public SettingsStore()
    {
        try
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(root))
                _path = Path.Combine(root, "Grimoire", "settings.json");
        }
        catch (PlatformNotSupportedException) { }
    }

    public AppSettings Current { get; private set; } = new();

    public AppSettings Load()
    {
        try
        {
            if (_path is not null && File.Exists(_path))
                Current = JsonSerializer.Deserialize(File.ReadAllText(_path), SettingsJsonContext.Default.AppSettings) ?? new();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Current = new(); // corrupt or unreadable settings: start fresh rather than crash
        }
        return Current;
    }

    public void Save()
    {
        if (_path is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Current, SettingsJsonContext.Default.AppSettings));
            File.Move(tmp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persisting settings is best-effort.
        }
    }
}
