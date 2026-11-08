using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Grimoire.Services;

public sealed record GitHubContributor
{
    public string Login { get; init; } = "";
    public int Contributions { get; init; }
    public string? AvatarUrl { get; init; }
    public string? HtmlUrl { get; init; }
    public string? Type { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(List<GitHubContributor>))]
internal sealed partial class GitHubJsonContext : JsonSerializerContext;

public static class GitHubService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { BaseAddress = new Uri("https://api.github.com/"), Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Grimoire-App", ProjectInfo.Version));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return http;
    }

    /// <summary>Contributors to the project repository, most contributions first. Bots are excluded.</summary>
    public static async Task<IReadOnlyList<GitHubContributor>> GetContributorsAsync(CancellationToken ct = default)
    {
        var path = $"repos/{Uri.EscapeDataString(ProjectInfo.GitHubOwner)}/{Uri.EscapeDataString(ProjectInfo.GitHubRepo)}/contributors?per_page=100";
        using var response = await Http.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var list = await JsonSerializer.DeserializeAsync(stream, GitHubJsonContext.Default.ListGitHubContributor, ct) ?? [];
        return list
            .Where(c => !string.Equals(c.Type, "Bot", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Contributions)
            .ThenBy(c => c.Login, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
