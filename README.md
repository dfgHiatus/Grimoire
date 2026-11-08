# Grimoire

A cross-platform (Windows, macOS, Linux, Android, iOS, browser) app for browsing a Jinxxy library, built with .NET 10 and [Avalonia](https://avaloniaui.net/).

## Projects

| Project | What it is |
|---|---|
| `external/Jinxxy.NET` | Git submodule: [Jinxxy.NET](external/Jinxxy.NET/README.md), the portable client for the [Jinxxy Creator API v1](https://api.creators.jinxxy.com/v1/docs). Lives in its own repository and will be consumed from NuGet once published. |
| `Grimoire` | Shared Avalonia UI: views, view models, services. |
| `Grimoire.Desktop` / `.Android` / `.iOS` / `.Browser` | Platform heads. |

## Getting the code

The API client is a git submodule, so clone recursively:

```sh
git clone --recursive <grimoire-repo-url>
# or, in an existing clone:
git submodule update --init
```

## Running

```sh
dotnet run --project Grimoire.Desktop
dotnet test external/Jinxxy.NET     # client library tests
```

On the sign-in screen, paste a Jinxxy API key that has the `products_read` scope, or choose **Explore with demo data** to try the UI with sample content.

## Using the client library

See the [Jinxxy.NET README](external/Jinxxy.NET/README.md).

## What the Jinxxy API can and can't do

The public Jinxxy API is a **creator** API, and that shapes the app:

- **Sign-in is by API key only.** There is no public endpoint for email/password or Discord sign-in, so the app doesn't ask for them. The sign-in screen explains why.
- **The library shows the key owner's own products.** The API has no buyer-side "my purchases" endpoint. The app reads from an `ILibrarySource` interface (`Grimoire/Services/LibrarySource.cs`), so a buyer source can be added without UI changes if Jinxxy ever offers one.
- **No images or downloads.** The product schema has no image or file fields. Cards use generated cover art, and the item view links to the product page on Jinxxy instead of a direct download. If the live API starts returning an image field, it's picked up automatically via `Product.AdditionalData`.
- **Prices are assumed to be in cents**, matching the documented discount-code amounts. This hasn't been confirmed against a live product.

## Configuration

- **GitHub links and contributors:** set `GitHubOwner` / `GitHubRepo` in `Grimoire/ProjectInfo.cs`. The About page loads contributors from the GitHub API, sorted by contribution count.
- **Languages:** English, Spanish, German and Japanese are in `Grimoire/Services/Strings.cs`. To add a language, copy the `en` block and translate it. Missing keys fall back to English.

## Security note

The API key is stored in plain text in the per-user app-data folder (`%LOCALAPPDATA%\Grimoire\settings.json` on Windows), alongside the theme and language preferences. Signing out deletes it. Moving it to the OS keychain (DPAPI, Keychain, Android Keystore) is a sensible next step before a public release.
