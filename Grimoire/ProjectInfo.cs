using System;
using System.Reflection;

namespace Grimoire;

/// <summary>Project-wide constants. Update the GitHub owner/repo once the repository is published.</summary>
public static class ProjectInfo
{
    public const string GitHubOwner = "YOUR-GITHUB-USER-OR-ORG";
    public const string GitHubRepo = "Grimoire";

    public static string RepositoryUrl => $"https://github.com/{GitHubOwner}/{GitHubRepo}";
    public static string LicensesUrl => $"{RepositoryUrl}/blob/main/THIRD-PARTY-NOTICES.md";

    public const string JinxxyHomeUrl = "https://jinxxy.com";
    /// <summary>Explains how to create an API key in the creator dashboard.</summary>
    public const string JinxxyApiDocsUrl = "https://api.creators.jinxxy.com/v1/docs#description/introduction";

    public static string Version
    {
        get
        {
            var asm = typeof(ProjectInfo).Assembly;
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (string.IsNullOrEmpty(info)) return asm.GetName().Version?.ToString(3) ?? "1.0.0";
            var plus = info.IndexOf('+', StringComparison.Ordinal); // strip source-link commit hash
            return plus > 0 ? info[..plus] : info;
        }
    }
}
