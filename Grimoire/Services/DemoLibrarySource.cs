using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Services;

/// <summary>Offline sample library so the app can be explored without an API key. Clearly labelled as demo data in the UI.</summary>
public sealed class DemoLibrarySource : ILibrarySource
{
    private static readonly (string Name, string Author, int DaysAgo, string Category, string[] Tags)[] Samples =
    [
        ("Moonlit Fox Avatar", "Kitsune Works", 3, "Avatar", ["fox", "quest", "physbones"]),
        ("Starlight Hoodie", "Velvet Thread", 9, "Clothing", ["hoodie", "outfit"]),
        ("Ramen Shop World Kit", "Pixel Lantern", 15, "World", ["world", "props", "food"]),
        ("Crystal Shader Pack", "Prism Lab", 21, "Shader", ["shader", "poiyomi"]),
        ("Cozy Cabin Props", "Pixel Lantern", 34, "Props", ["props", "furniture"]),
        ("Neon Wings", "Prism Lab", 40, "Accessory", ["wings", "glow"]),
        ("Mochi Cat Ears", "Kitsune Works", 52, "Accessory", ["ears", "cute"]),
        ("Gothic Lolita Dress", "Velvet Thread", 66, "Clothing", ["dress", "outfit"]),
        ("Dragon Base Avatar", "Scale & Ember", 80, "Avatar", ["dragon", "pc"]),
        ("Expression Menu Toolkit", "Gesture Garden", 97, "Tool", ["tool", "menu"]),
        ("Pastel Sneakers", "Velvet Thread", 120, "Clothing", ["shoes"]),
        ("Ambient Rain Audio", "Soundleaf", 145, "Audio", ["audio", "ambience"]),
        ("Holographic Visor", "Prism Lab", 170, "Accessory", ["visor", "glow"]),
        ("Ember Particle FX", "Scale & Ember", 201, "Effect", ["particles", "fire"]),
    ];

    public string AccountName => "Demo User";
    public string? AccountAvatarUrl => null;
    public bool IsDemo => true;

    public async Task<IReadOnlyList<LibraryItem>> LoadAsync(CancellationToken ct = default)
    {
        await Task.Delay(300, ct); // feel like a network call
        var now = DateTimeOffset.Now;
        return Samples
            .Select((s, i) => new LibraryItem($"demo-{i}", s.Name, s.Author, null, null, now.AddDays(-s.DaysAgo), i))
            .ToList();
    }

    public async Task<LibraryItemDetails> GetDetailsAsync(string id, CancellationToken ct = default)
    {
        await Task.Delay(200, ct);
        var index = int.Parse(id["demo-".Length..], CultureInfo.InvariantCulture);
        var s = Samples[index];
        var date = DateTimeOffset.Now.AddDays(-s.DaysAgo);
        return new LibraryItemDetails(
            id,
            null,
            date,
            [
                new("Detail_Added", date.ToString("D", CultureInfo.CurrentUICulture)),
                new("Detail_Category", s.Category),
                new("Detail_Platforms", "VRChat"),
            ],
            [new("Standard", "15.00 USD"), new("Deluxe", "25.00 USD")],
            s.Tags,
            "https://jinxxy.com",
            null);
    }
}
